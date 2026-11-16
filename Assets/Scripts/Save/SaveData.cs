using System;
using System.Collections.Generic;
using PlanetScrappers.Economy;

namespace PlanetScrappers.Save
{
    /// <summary>
    /// Everything a save file stores. Plain serialisable data with ids instead of object references,
    /// so the same shape can later be sent to a server for multiplayer persistence.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string gameVersion;
        public string savedAt;
        public float playSeconds;

        public float[] position = new float[3];
        public float yaw;
        public float health = 100f;
        public float oxygen = 100f;

        public int credits;
        public List<TransactionRecord> transactions = new List<TransactionRecord>();
        public List<ResourceAmount> inventory = new List<ResourceAmount>();
        public int backpackLevel;

        public List<string> ownedTools = new List<string>();
        public string equippedTool;

        public int missionIndex;
        public int missionProgress;

        public List<ObjectState> objects = new List<ObjectState>();
    }

    [Serializable]
    public struct ResourceAmount
    {
        public string id;
        public int amount;
    }

    [Serializable]
    public struct ObjectState
    {
        public string id;
        public bool dismantled;
        public float durability;
        public float respawn;
    }
}
