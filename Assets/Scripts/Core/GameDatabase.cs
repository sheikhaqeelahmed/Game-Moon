using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Missions;
using UnityEngine;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// All tunable game data in one place (Resources/GameDatabase.asset).
    /// Change prices, durability or rewards in the asset; no code changes needed.
    /// </summary>
    [CreateAssetMenu(menuName = "Planet Scrappers/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public int startingCredits = 100;
        [Tooltip("Backpack capacity in units for each upgrade level.")]
        public int[] backpackCapacities = { 30, 50, 80, 120 };
        [Tooltip("Price of each backpack level; level 0 is the starter pack.")]
        public int[] backpackPrices = { 0, 300, 900, 2500 };

        [Header("Suit")]
        public float oxygenSeconds = 420f;
        public float oxygenRefillPerSecond = 12f;
        public float baseSafeRadius = 20f;

        public ResourceDefinition[] resources;
        public ToolDefinition[] tools;
        public DismantlableDefinition[] dismantlables;
        public MissionDefinition[] missions;

        static GameDatabase instance;
        public static GameDatabase Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameDatabase>("GameDatabase");
                return instance;
            }
        }

        public ResourceDefinition Resource(string id)
        {
            foreach (var r in resources) if (r != null && r.id == id) return r;
            return null;
        }

        public ToolDefinition Tool(string id)
        {
            foreach (var t in tools) if (t != null && t.id == id) return t;
            return null;
        }

        public DismantlableDefinition Dismantlable(string id)
        {
            foreach (var d in dismantlables) if (d != null && d.id == id) return d;
            return null;
        }
    }
}
