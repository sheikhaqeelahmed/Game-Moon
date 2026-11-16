using System;
using System.Collections.Generic;
using PlanetScrappers.Core;
using PlanetScrappers.Economy;
using UnityEngine;

namespace PlanetScrappers.Inventory
{
    /// <summary>The Rookie's backpack: resource counts limited by a capacity in units.</summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        readonly Dictionary<ResourceDefinition, int> counts = new Dictionary<ResourceDefinition, int>();

        public event Action Changed;
        public int CapacityLevel { get; private set; }
        public int TotalUnits { get; private set; }

        GameDatabase Db => GameDatabase.Instance;
        public int Capacity => Db.backpackCapacities[Mathf.Clamp(CapacityLevel, 0, Db.backpackCapacities.Length - 1)];
        public int FreeSpace => Mathf.Max(0, Capacity - TotalUnits);
        public bool IsFull => FreeSpace <= 0;
        public bool CanUpgrade => CapacityLevel < Db.backpackCapacities.Length - 1;
        public int NextCapacity => CanUpgrade ? Db.backpackCapacities[CapacityLevel + 1] : Capacity;
        public int NextUpgradePrice => CanUpgrade ? Db.backpackPrices[CapacityLevel + 1] : 0;

        void Awake() => Instance = this;

        public int Count(ResourceDefinition resource) =>
            resource != null && counts.TryGetValue(resource, out int n) ? n : 0;

        /// <summary>Adds up to the free space; returns how many units actually fitted.</summary>
        public int Add(ResourceDefinition resource, int amount)
        {
            int added = Mathf.Min(amount, FreeSpace);
            if (resource == null || added <= 0) return 0;
            counts[resource] = Count(resource) + added;
            TotalUnits += added;
            Changed?.Invoke();
            GameEvents.RaiseScrapCollected(resource, added);
            return added;
        }

        public int Remove(ResourceDefinition resource, int amount)
        {
            int removed = Mathf.Min(amount, Count(resource));
            if (removed <= 0) return 0;
            counts[resource] = Count(resource) - removed;
            TotalUnits -= removed;
            Changed?.Invoke();
            return removed;
        }

        /// <summary>Total credits the backpack would sell for at current prices.</summary>
        public int TotalValue()
        {
            int total = 0;
            foreach (var kv in counts) total += kv.Key.unitValue * kv.Value;
            return total;
        }

        public void SetCapacityLevel(int level)
        {
            CapacityLevel = Mathf.Clamp(level, 0, Db.backpackCapacities.Length - 1);
            Changed?.Invoke();
        }

        /// <summary>Drops a share of every stack, e.g. after a suit failure.</summary>
        public void LoseFraction(float fraction)
        {
            foreach (var r in Db.resources) Remove(r, Mathf.CeilToInt(Count(r) * fraction));
        }

        public void Clear()
        {
            counts.Clear();
            TotalUnits = 0;
            Changed?.Invoke();
        }

        /// <summary>Sets a count directly, without events. Used when loading a save.</summary>
        public void SetCount(ResourceDefinition resource, int amount)
        {
            if (resource == null) return;
            TotalUnits += amount - Count(resource);
            counts[resource] = amount;
            Changed?.Invoke();
        }
    }
}
