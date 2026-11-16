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

        /// <summary>Where the current objective is, for the HUD marker. False when there is no single place.</summary>
        public bool TryGetTarget(Vector3 from, out Vector3 target)
        {
            target = from;
            var m = Current;
            if (m == null) return false;
            switch (m.type)
            {
                case ObjectiveType.Dismantle: return Nearest(from, m.targetId, out target);
                case ObjectiveType.SellAny: return Station("depot", out target);
                case ObjectiveType.BuyTool:
                case ObjectiveType.BuyBackpack: return Station("shop", out target);
                case ObjectiveType.CollectResource: return NearestWith(from, m.targetId, out target);
                case ObjectiveType.CollectToStorage: return NearestWith(from, "iron", out target);
                case ObjectiveType.Interact:
                    if (m.targetId == "terminal") return Station("terminal", out target);
                    if (m.targetId == "build_factory" || m.targetId == "buy_robot") return Station("shop", out target);
                    if (m.targetId == "truck" && PlanetScrappers.Vehicles.SalvageTruck.Instance != null)
                    {
                        target = PlanetScrappers.Vehicles.SalvageTruck.Instance.transform.position;
                        return true;
                    }
                    if (m.targetId == "build_collector")
                    {
                        float best = float.MaxValue;
                        foreach (var pad in PlanetScrappers.Automation.CollectorPad.Pads)
                        {
                            float d = (pad.transform.position - from).sqrMagnitude;
                            if (!pad.Built && d < best) { best = d; target = pad.transform.position; }
                        }
                        return best < float.MaxValue;
                    }
                    return false;
            }
            return false;
        }

        static bool Station(string id, out Vector3 pos)
        {
            foreach (var s in UnityEngine.Object.FindObjectsByType<PlanetScrappers.Interaction.StationInteractable>(FindObjectsSortMode.None))
                if (s.StationId == id) { pos = s.transform.position; return true; }
            pos = Vector3.zero;
            return false;
        }

        static bool Nearest(Vector3 from, string defId, out Vector3 pos)
        {
            pos = from;
            float best = float.MaxValue;
            foreach (var o in DestructibleObject.All)
            {
                if (o.IsDismantled || o.Definition.id != defId) continue;
                float d = (o.Center - from).sqrMagnitude;
                if (d < best) { best = d; pos = o.Center; }
            }
            return best < float.MaxValue;
        }

        static bool NearestWith(Vector3 from, string resourceId, out Vector3 pos)
        {
            pos = from;
            float best = float.MaxValue;
            var tool = PlayerEquipment.Instance != null ? PlayerEquipment.Instance.Equipped : null;
            int tier = tool != null ? tool.tier : 1;
            foreach (var o in DestructibleObject.All)
            {
                if (o.IsDismantled || o.Definition.tier > tier) continue;
                bool has = false;
                foreach (var y in o.Definition.yields) if (y.resource != null && y.resource.id == resourceId) has = true;
                if (!has) continue;
                float d = (o.Center - from).sqrMagnitude;
                if (d < best) { best = d; pos = o.Center; }
            }
            return best < float.MaxValue;
        }

        IEnumerator SayLater(string line, float delay, float seconds)
        {
            yield return new WaitForSeconds(delay);
            if (!string.IsNullOrEmpty(line)) GameEvents.Say(line, seconds);
        }
    }
}
