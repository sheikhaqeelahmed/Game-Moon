using PlanetScrappers.Core;
using PlanetScrappers.Equipment;
using UnityEngine;

namespace PlanetScrappers.Interaction
{
    /// <summary>A tool lying in the world (the rusty wrench on its crate). Picking it up fits it to the salvage arm.</summary>
    public class ToolPickup : Interactable
    {
        [SerializeField] ToolDefinition tool;
        [SerializeField] GameObject visual;

        public override string Prompt => tool != null ? "Pick up " + tool.displayName : "Pick up";
        public override bool CanInteract => base.CanInteract && tool != null && PlayerEquipment.Instance != null && !PlayerEquipment.Instance.Owns(tool);

        public void Setup(ToolDefinition t, GameObject model)
        {
            tool = t;
            visual = model;
        }

        void Update()
        {
            // Hide once owned (also after loading a save).
            bool owned = PlayerEquipment.Instance != null && PlayerEquipment.Instance.Owns(tool);
            if (visual != null && visual.activeSelf == owned) visual.SetActive(!owned);
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            PlayerEquipment.Instance.Grant(tool, true);
            AudioManager.Play("purchase", 0.7f);
            GameEvents.RaiseInteracted("pickup_" + tool.id);
            GameEvents.Say("A wrench! It's rusty, it's heavy, it's yours. Hold the left mouse button on a drum. F scans things.", 7f);
        }
    }
}
