using System;
using System.Collections.Generic;
using System.IO;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Inventory;
using PlanetScrappers.Player;
using UnityEngine;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Local achievements, stored per PC in achievements.json (not per save, like Steam).
    /// The ids are stable so they can be mapped 1:1 to Steam achievements at release.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public struct Definition
        {
            public string id, title, description;
            public Definition(string id, string title, string description) { this.id = id; this.title = title; this.description = description; }
        }

        public static readonly Definition[] All =
        {
            new Definition("first_scrap", "First Scrap", "Dismantle anything."),
            new Definition("full_pockets", "Full Pockets", "Fill your backpack to the brim."),
            new Definition("cash_in", "Cash In", "Sell scrap at the Recycling Depot."),
            new Definition("hammer_time", "Hammer Time", "Buy the Scrap Hammer."),
            new Definition("road_trip", "Road Trip", "Drive the salvage truck."),
            new Definition("full_haul", "Full Haul", "Fill the truck's bin to 150 units."),
            new Definition("hands_free", "Hands Free", "Build a Scrap Collector."),
            new Definition("industrialist", "Industrialist", "Build the Recycling Factory."),
            new Definition("robot_crew", "Robot Crew", "Own three salvage robots."),
            new Definition("tier_five", "Tier Five", "Own the Industrial Crusher."),
            new Definition("freighter_down", "Freighter Down", "Dismantle the freighter's engine."),
            new Definition("deep_diver", "Deep Diver", "Enter the Deep Core."),
            new Definition("scrap_tycoon", "Scrap Tycoon", "Hold 25,000 credits at once."),
            new Definition("centurion", "Centurion", "Dismantle 100 objects in one game."),
            new Definition("planet_cleared", "Planet Cleared", "Take apart the Mega-Factory reactor core.")
        };

        [Serializable]
        class Store
        {
            public List<string> unlocked = new List<string>();
            public List<string> dates = new List<string>();
        }

        public static AchievementManager Instance { get; private set; }
        public static string FilePath => Path.Combine(Application.persistentDataPath, "achievements.json");

        Store store = new Store();
        float checkTimer;

        public event Action<Definition> Unlocked;
        public int UnlockedCount => store.unlocked.Count;
        public bool IsUnlocked(string id) => store.unlocked.Contains(id);

        public string UnlockDate(string id)
        {
            int i = store.unlocked.IndexOf(id);
            return i >= 0 && i < store.dates.Count ? store.dates[i] : "";
        }

        void Awake()
        {
            Instance = this;
            try
            {
                if (File.Exists(FilePath)) store = JsonUtility.FromJson<Store>(File.ReadAllText(FilePath)) ?? new Store();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Achievements] Could not read: " + e.Message);
                store = new Store();
            }
        }

        void OnEnable()
        {
            GameEvents.ObjectDismantled += OnDismantled;
            GameEvents.ScrapSold += OnSold;
            GameEvents.ToolPurchased += OnTool;
            GameEvents.Interacted += OnInteracted;
            GameEvents.VehicleEntered += OnVehicle;
        }

        void OnDisable()
        {
            GameEvents.ObjectDismantled -= OnDismantled;
            GameEvents.ScrapSold -= OnSold;
            GameEvents.ToolPurchased -= OnTool;
            GameEvents.Interacted -= OnInteracted;
            GameEvents.VehicleEntered -= OnVehicle;
        }

        void OnDismantled(DestructibleObject o)
        {
            Unlock("first_scrap");
            if (o.Definition.id == "ship_engine") Unlock("freighter_down");
            if (o.Definition.id == "reactor_core") Unlock("planet_cleared");
            if (GameManager.Instance != null && GameManager.Instance.TotalDismantled >= 100) Unlock("centurion");
        }

        void OnSold(int credits) => Unlock("cash_in");

        void OnTool(ToolDefinition t)
        {
            if (t.id == "scrap_hammer") Unlock("hammer_time");
        }

        void OnInteracted(string id)
        {
            if (id == "build_collector") Unlock("hands_free");
            if (id == "build_factory") Unlock("industrialist");
        }

        void OnVehicle(string id) => Unlock("road_trip");

        void Update()
        {
            checkTimer -= Time.unscaledDeltaTime;
            if (checkTimer > 0f || GameManager.Instance == null || !GameManager.Instance.InGame) return;
            checkTimer = 0.5f;

            var inv = PlayerInventory.Instance;
            if (inv != null && inv.IsFull) Unlock("full_pockets");
            var truckBin = ScrapStorage.Find("truck");
            if (truckBin != null && truckBin.IsFull) Unlock("full_haul");
            var dock = Automation.RobotDock.Instance;
            if (dock != null && dock.Count >= 3) Unlock("robot_crew");
            if (Wallet.Instance != null && Wallet.Instance.Credits >= 25000) Unlock("scrap_tycoon");
            if (SuitSystems.Instance != null && SuitSystems.Instance.isActiveAndEnabled && SuitSystems.Instance.InDeepCore) Unlock("deep_diver");
            var eq = PlayerEquipment.Instance;
            if (eq != null && eq.Owns(GameDatabase.Instance.Tool("industrial_crusher"))) Unlock("tier_five");
        }

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || store.unlocked.Contains(id)) return;
            var def = Array.Find(All, d => d.id == id);
            if (def.id == null) return;
            store.unlocked.Add(id);
            store.dates.Add(DateTime.Now.ToString("yyyy-MM-dd"));
            Save();
            AudioManager.Play("mission_complete", 0.5f, 1.25f);
            Unlocked?.Invoke(def);
            // Steam release: SteamUserStats.SetAchievement(id); SteamUserStats.StoreStats();
        }

        void Save()
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(store, true)); }
            catch (Exception e) { Debug.LogWarning("[Achievements] Could not save: " + e.Message); }
        }
    }
}
