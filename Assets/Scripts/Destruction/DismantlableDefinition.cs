using System;
using PlanetScrappers.Economy;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    [Serializable]
    public struct ScrapYield
    {
        public ResourceDefinition resource;
        public int amount;
        [Range(0f, 1f)] public float chance;
    }

    /// <summary>Balance data for one kind of dismantlable object (oil drum, Mule-7 truck, ...).</summary>
    [CreateAssetMenu(menuName = "Planet Scrappers/Dismantlable", fileName = "Dismantlable")]
    public class DismantlableDefinition : ScriptableObject
    {
        public string id = "oil_drum";
        public string displayName = "Oil Drum";
        [Tooltip("Minimum tool tier needed.")]
        public int tier = 1;
        public float durability = 30f;
        public ScrapYield[] yields;
        [Tooltip("Seconds before Captain Bolt's crews drop a replacement. 0 = never.")]
        public float respawnSeconds = 240f;
    }
}
