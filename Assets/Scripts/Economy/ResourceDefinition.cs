using UnityEngine;

namespace PlanetScrappers.Economy
{
    /// <summary>A sellable material such as iron or copper. Values are tuned in the asset, not in code.</summary>
    [CreateAssetMenu(menuName = "Planet Scrappers/Resource", fileName = "Resource")]
    public class ResourceDefinition : ScriptableObject
    {
        public string id = "iron";
        public string displayName = "Iron";
        [TextArea] public string description;
        [Tooltip("Credits paid per unit at the Recycling Depot.")]
        public int unitValue = 2;
        public int stackLimit = 50;
        public string rarity = "Common";
        public Color color = Color.grey;
    }
}
