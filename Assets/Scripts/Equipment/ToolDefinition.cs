using UnityEngine;

namespace PlanetScrappers.Equipment
{
    /// <summary>A dismantling tool that mounts on the Rookie's salvage arm.</summary>
    [CreateAssetMenu(menuName = "Planet Scrappers/Tool", fileName = "Tool")]
    public class ToolDefinition : ScriptableObject
    {
        public string id = "rusty_wrench";
        public string displayName = "Rusty Wrench";
        [TextArea] public string description;
        [Tooltip("Upgrade level 1-6, also the hotkey order.")]
        public int level = 1;
        [Tooltip("Highest object tier this tool can dismantle.")]
        public int tier = 1;
        public float damage = 10f;
        public float hitsPerSecond = 1.2f;
        [Tooltip("Reach in metres from the player's chest.")]
        public float range = 2.2f;
        [Tooltip("Extra scrap per object, 0.1 = +10%.")]
        public float recoveryBonus;
        public int price;
        public bool availableInPrototype = true;
        public string lockedReason = "Coming in a later phase";

        public float DamagePerSecond => damage * hitsPerSecond;
    }
}
