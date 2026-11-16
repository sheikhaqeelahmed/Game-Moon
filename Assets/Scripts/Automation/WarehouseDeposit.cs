using PlanetScrappers.Core;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>Base warehouse: E moves the whole backpack into storage, to sell later at the depot.</summary>
    public class WarehouseDeposit : Interactable
    {
        [SerializeField] ScrapStorage storage;

        public override string Prompt => $"Store backpack in the warehouse  ({storage.TotalUnits}/{storage.Capacity})";

        public void Setup(ScrapStorage s) => storage = s;

        public override void Interact()
        {
            var inv = PlayerInventory.Instance;
            if (inv.TotalUnits == 0) { GameEvents.Say("Nothing to store. Your backpack is empty.", 4f); return; }
            int moved = storage.MoveFrom(inv);
            if (moved > 0)
            {
                AudioManager.Play("pickup_tink", 0.7f, 0.9f);
                GameEvents.Say($"Stored {moved} units. Sell the lot at the Recycling Depot whenever you like.", 5f);
            }
            else GameEvents.Say("The warehouse is full. Sell some at the depot.", 4f);
        }
    }
}
