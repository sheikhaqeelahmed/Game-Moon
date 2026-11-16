using System;
using System.Collections.Generic;
using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using UnityEngine;

namespace PlanetScrappers.Inventory
{
    /// <summary>
    /// A container for scrap outside the backpack: the truck's bin, a scrap collector's hopper
    /// or the base warehouse. Identified by a stable id for saving and missions.
    /// </summary>
    public class ScrapStorage : MonoBehaviour
    {
        public static readonly List<ScrapStorage> All = new List<ScrapStorage>();

        [SerializeField] string storageId = "warehouse";
        [SerializeField] string displayName = "Warehouse";
        [SerializeField] int capacity = 600;

        readonly Dictionary<ResourceDefinition, int> counts = new Dictionary<ResourceDefinition, int>();

        public event Action Changed;
        public string StorageId => storageId;
        public string DisplayName => displayName;
        public int Capacity => capacity;
        public int TotalUnits { get; private set; }
        public int FreeSpace => Mathf.Max(0, capacity - TotalUnits);
        public bool IsFull => FreeSpace <= 0;

        public void Setup(string id, string name, int cap)
        {
            storageId = id;
            displayName = name;
            capacity = cap;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static ScrapStorage Find(string id) => All.Find(s => s.storageId == id);

        public int Count(ResourceDefinition r) => r != null && counts.TryGetValue(r, out int n) ? n : 0;

        public int Add(ResourceDefinition r, int amount)
        {
            int added = Mathf.Min(amount, FreeSpace);
            if (r == null || added <= 0) return 0;
            counts[r] = Count(r) + added;
            TotalUnits += added;
            Changed?.Invoke();
            GameEvents.RaiseScrapStored(storageId, r, added);
            return added;
        }

        public int TotalValue()
        {
            int total = 0;
            foreach (var kv in counts) total += kv.Key.unitValue * kv.Value;
            return total;
        }

        /// <summary>Empties the storage and returns what was in it.</summary>
        public Dictionary<ResourceDefinition, int> TakeAll()
        {
            var copy = new Dictionary<ResourceDefinition, int>(counts);
            counts.Clear();
            TotalUnits = 0;
            Changed?.Invoke();
            return copy;
        }

        /// <summary>Moves as much as fits from this storage into the backpack.</summary>
        public int MoveInto(PlayerInventory inv)
        {
            int moved = 0;
            foreach (var r in GameDatabase.Instance.resources)
            {
                int n = Count(r);
                if (n <= 0) continue;
                int added = inv.Add(r, n);
                if (added <= 0) continue;
                counts[r] = n - added;
                TotalUnits -= added;
                moved += added;
            }
            if (moved > 0) Changed?.Invoke();
            return moved;
        }

        /// <summary>Moves as much as fits from the backpack into this storage.</summary>
        public int MoveFrom(PlayerInventory inv)
        {
            int moved = 0;
            foreach (var r in GameDatabase.Instance.resources)
            {
                int n = Mathf.Min(inv.Count(r), FreeSpace);
                if (n <= 0) continue;
                inv.Remove(r, n);
                moved += Add(r, n);
            }
            return moved;
        }

        public void SetCount(ResourceDefinition r, int amount)
        {
            if (r == null) return;
            TotalUnits += amount - Count(r);
            counts[r] = amount;
            Changed?.Invoke();
        }

        public void Clear()
        {
            counts.Clear();
            TotalUnits = 0;
            Changed?.Invoke();
        }
    }
}
