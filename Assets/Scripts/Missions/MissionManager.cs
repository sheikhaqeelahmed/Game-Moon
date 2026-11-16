using System;
using System.Collections;
using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using UnityEngine;

namespace PlanetScrappers.Missions
{
    /// <summary>
    /// Runs the mission chain in order: listens for game events, counts progress on the current
    /// mission, pays the reward and introduces the next one through SCRAP-BOT.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public int Index { get; private set; }
        public int Progress { get; private set; }
        public event Action Changed;

        MissionDefinition[] Missions => GameDatabase.Instance.missions;
        public MissionDefinition Current => Index < Missions.Length ? Missions[Index] : null;
        public bool AllComplete => Index >= Missions.Length;
        public int CompletedCount => Mathf.Min(Index, Missions.Length);

        void Awake() => Instance = this;

        void OnEnable()
        {
            GameEvents.Interacted += OnInteracted;
            GameEvents.ObjectDismantled += OnDismantled;
            GameEvents.ScrapCollected += OnCollected;
            GameEvents.ScrapSold += OnSold;
            GameEvents.ToolPurchased += OnToolBought;
            GameEvents.BackpackUpgraded += OnBackpack;
            GameEvents.ScrapStored += OnStored;
        }

        void OnDisable()
        {
            GameEvents.Interacted -= OnInteracted;
            GameEvents.ObjectDismantled -= OnDismantled;
            GameEvents.ScrapCollected -= OnCollected;
            GameEvents.ScrapSold -= OnSold;
            GameEvents.ToolPurchased -= OnToolBought;
            GameEvents.BackpackUpgraded -= OnBackpack;
            GameEvents.ScrapStored -= OnStored;
        }

        public void SetState(int index, int progress)
        {
            Index = Mathf.Clamp(index, 0, Missions.Length);
            Progress = Mathf.Max(0, progress);
            Changed?.Invoke();
        }

        /// <summary>Called once a game has started, to brief the current mission.</summary>
        public void Brief(float delay)
        {
            if (Current != null) StartCoroutine(SayLater(Current.botIntro, delay, 8f));
        }

        void OnInteracted(string id) => Count(ObjectiveType.Interact, id, 1);
        void OnDismantled(DestructibleObject o) => Count(ObjectiveType.Dismantle, o.Definition.id, 1);
        void OnCollected(ResourceDefinition r, int amount) => Count(ObjectiveType.CollectResource, r.id, amount);
        void OnSold(int credits) => Count(ObjectiveType.SellAny, "", 1);
        void OnToolBought(ToolDefinition t) => Count(ObjectiveType.BuyTool, t.id, 1);
        void OnBackpack(int level) => Count(ObjectiveType.BuyBackpack, "", 1);
        void OnStored(string storageId, ResourceDefinition r, int amount) => Count(ObjectiveType.CollectToStorage, storageId, amount);

        void Count(ObjectiveType type, string id, int amount)
        {
            var m = Current;
            if (m == null || m.type != type) return;
            if (!string.IsNullOrEmpty(m.targetId) && m.targetId != id) return;
            Progress += amount;
            if (Progress >= m.count) Complete(m);
            else Changed?.Invoke();
        }

        void Complete(MissionDefinition m)
        {
            Index++;
            Progress = 0;
            if (m.rewardCredits > 0) Wallet.Instance?.Add(m.rewardCredits, $"Mission {m.number} reward");
            AudioManager.Play("mission_complete", 0.8f);
            GameEvents.Say(m.botComplete, 6f);
            Changed?.Invoke();
            GameEvents.RaiseMissionCompleted(m);

            if (Current != null) StartCoroutine(SayLater(Current.botIntro, 6.5f, 9f));
            else StartCoroutine(SayLater("Every contract done. Keep salvaging, keep earning. Captain Bolt drops fresh junk every few minutes.", 6.5f, 9f));
        }

        IEnumerator SayLater(string line, float delay, float seconds)
        {
            yield return new WaitForSeconds(delay);
            if (!string.IsNullOrEmpty(line)) GameEvents.Say(line, seconds);
        }
    }
}
