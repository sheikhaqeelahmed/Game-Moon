using System;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Equipment;
using PlanetScrappers.Missions;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Game-wide events so systems stay decoupled: missions, audio, UI and saving listen here
    /// instead of referencing each other. Cleared on every scene load.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<DestructibleObject> ObjectDismantled;
        public static event Action<ResourceDefinition, int> ScrapCollected;
        public static event Action<int> ScrapSold;
        public static event Action<ToolDefinition> ToolPurchased;
        public static event Action<int> BackpackUpgraded;
        public static event Action<string> Interacted;
        public static event Action<MissionDefinition> MissionCompleted;
        public static event Action<string, float> BotSays;
        public static event Action<int, string> CreditsGained;

        public static void RaiseObjectDismantled(DestructibleObject o) => ObjectDismantled?.Invoke(o);
        public static void RaiseScrapCollected(ResourceDefinition r, int amount) => ScrapCollected?.Invoke(r, amount);
        public static void RaiseScrapSold(int credits) => ScrapSold?.Invoke(credits);
        public static void RaiseToolPurchased(ToolDefinition t) => ToolPurchased?.Invoke(t);
        public static void RaiseBackpackUpgraded(int level) => BackpackUpgraded?.Invoke(level);
        public static void RaiseInteracted(string id) => Interacted?.Invoke(id);
        public static void RaiseMissionCompleted(MissionDefinition m) => MissionCompleted?.Invoke(m);
        /// <summary>Shows a SCRAP-BOT line on the HUD for the given number of seconds.</summary>
        public static void Say(string line, float seconds = 5f) => BotSays?.Invoke(line, seconds);
        public static void RaiseCreditsGained(int amount, string label) => CreditsGained?.Invoke(amount, label);

        public static void ClearAll()
        {
            ObjectDismantled = null; ScrapCollected = null; ScrapSold = null; ToolPurchased = null;
            BackpackUpgraded = null; Interacted = null; MissionCompleted = null; BotSays = null; CreditsGained = null;
        }
    }
}
