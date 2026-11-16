using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Economy;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>
    /// First automation: a pad in a salvage area where a Scrap Collector can be built.
    /// Once built it pulls loose scrap within its radius into its hopper; press E to empty it.
    /// Foundation for later machines with input, processing, output, capacity and running costs.
    /// </summary>
    public class CollectorPad : Interactable
    {
        public static readonly System.Collections.Generic.List<CollectorPad> Pads = new System.Collections.Generic.List<CollectorPad>();

        [SerializeField] string padId = "collector_graveyard";
        [SerializeField] int price = 800;
        [SerializeField] GameObject model;
        [SerializeField] Transform dish;
        [SerializeField] ScrapStorage hopper;
        [SerializeField] StorageScrapSink magnet;

        public string PadId => padId;
        public bool Built { get; private set; }
        public ScrapStorage Hopper => hopper;

        public override string Prompt => Built
            ? $"Empty Scrap Collector into backpack  ({hopper.TotalUnits}/{hopper.Capacity})"
            : $"Build Scrap Collector  ({price:N0} cr)";

        protected override void OnEnable() { base.OnEnable(); Pads.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Pads.Remove(this); }

        void Start() => SetBuilt(Built);

        public override void Interact()
        {
            if (!Built)
            {
                if (!Wallet.Instance.TrySpend(price, "Built Scrap Collector"))
                {
                    AudioManager.Play("hit_bonk", 0.6f);
                    GameEvents.Say($"A collector costs {price:N0} credits. Bolt doesn't do credit. Ironic.", 5f);
                    return;
                }
                SetBuilt(true);
                AudioManager.Play("purchase", 0.9f);
                GameEvents.RaiseInteracted("build_collector");
                GameEvents.Say("Collector online. It pulls in loose scrap within about 15 metres. Come back and press E to empty it.", 7f);
                return;
            }

            var inv = PlayerInventory.Instance;
            int moved = hopper.MoveInto(inv);
            if (moved > 0) AudioManager.Play("pickup_tink", 0.7f, 1.2f);
            else GameEvents.Say(hopper.TotalUnits == 0 ? "It's empty. Break things nearby and it'll fill up." : "Your backpack's full.", 4f);
        }

        public void SetBuilt(bool built)
        {
            Built = built;
            if (model != null) model.SetActive(built);
            if (magnet != null) magnet.Active = built;
        }

        void Update()
        {
            if (Built && dish != null) dish.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
        }
    }
}
