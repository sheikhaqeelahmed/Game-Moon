using System;
using System.Collections.Generic;
using PlanetScrappers.Core;
using UnityEngine;

namespace PlanetScrappers.Economy
{
    [Serializable]
    public struct TransactionRecord
    {
        public string label;
        public int amount;
        public string when;
    }

    /// <summary>The player's credits and a log of every sale and purchase.</summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }
        const int MaxLog = 30;

        public int Credits { get; private set; }
        public List<TransactionRecord> Log { get; } = new List<TransactionRecord>();
        public event Action Changed;

        void Awake() => Instance = this;

        public void Add(int amount, string label)
        {
            if (amount <= 0) return;
            Credits += amount;
            Record(label, amount);
            GameEvents.RaiseCreditsGained(amount, label);
        }

        public bool CanAfford(int amount) => Credits >= amount;

        public bool TrySpend(int amount, string label)
        {
            if (amount < 0 || Credits < amount) return false;
            Credits -= amount;
            Record(label, -amount);
            return true;
        }

        public void SetState(int credits, List<TransactionRecord> log)
        {
            Credits = Mathf.Max(0, credits);
            Log.Clear();
            if (log != null) Log.AddRange(log);
            Changed?.Invoke();
        }

        void Record(string label, int amount)
        {
            Log.Insert(0, new TransactionRecord { label = label, amount = amount, when = DateTime.Now.ToString("HH:mm") });
            if (Log.Count > MaxLog) Log.RemoveAt(Log.Count - 1);
            Changed?.Invoke();
        }
    }
}
