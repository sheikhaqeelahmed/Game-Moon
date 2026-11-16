using UnityEngine;

namespace PlanetScrappers.Missions
{
    public enum ObjectiveType { Interact, Dismantle, CollectResource, SellAny, BuyTool, BuyBackpack }

    /// <summary>One tutorial or contract mission. Text and rewards live in the asset.</summary>
    [CreateAssetMenu(menuName = "Planet Scrappers/Mission", fileName = "Mission")]
    public class MissionDefinition : ScriptableObject
    {
        public int number = 1;
        public string title = "Welcome to Rust Valley";
        [TextArea] public string description;
        public string objectiveText = "Check in at Captain Bolt's terminal";
        public ObjectiveType type;
        [Tooltip("Station, object, resource or tool id the objective counts. Empty = any.")]
        public string targetId;
        public int count = 1;
        public int rewardCredits;
        [TextArea] public string botIntro;
        [TextArea] public string botComplete;
    }
}
