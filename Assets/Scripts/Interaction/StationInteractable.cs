using PlanetScrappers.Core;
using PlanetScrappers.UI;
using UnityEngine;

namespace PlanetScrappers.Interaction
{
    /// <summary>Opens a base station screen: "depot" (selling), "shop" (Nova's upgrades) or "terminal" (missions).</summary>
    public class StationInteractable : Interactable
    {
        [SerializeField] string stationId = "depot";
        [SerializeField] string prompt = "Sell scrap";

        public string StationId => stationId;
        public override string Prompt => prompt;

        public void Setup(string id, string promptText)
        {
            stationId = id;
            prompt = promptText;
        }

        public override void Interact()
        {
            GameEvents.RaiseInteracted(stationId);
            UIManager.Instance?.OpenStation(stationId);
        }
    }
}
