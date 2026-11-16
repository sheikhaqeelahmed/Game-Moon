using PlanetScrappers.Core;
using PlanetScrappers.Interaction;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>The control panel of a built machine: E switches it on or off.</summary>
    public class MachineControl : Interactable
    {
        [SerializeField] Machine machine;

        public void Setup(Machine m) => machine = m;

        public override bool CanInteract => base.CanInteract && machine != null && machine.Built;
        public override string Prompt => $"{(machine.Powered ? "Switch off" : "Switch on")} {machine.DisplayName}  ·  {machine.Status}  ·  {machine.RunningCostPerMinute:0} cr/min while working";

        public override void Interact()
        {
            machine.SetPowered(!machine.Powered);
            AudioManager.Play(machine.Powered ? "purchase" : "ui_click", 0.6f, machine.Powered ? 1.2f : 0.8f);
            GameEvents.RaiseInteracted((machine.Powered ? "power_on_" : "power_off_") + machine.MachineId);
        }
    }
}
