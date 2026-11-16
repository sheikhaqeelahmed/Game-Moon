using PlanetScrappers.Core;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Automation
{
    /// <summary>
    /// A conveyor from a scrap collector to the warehouse. Moves scrap steadily while there is
    /// something in the collector and room in the warehouse; crates visibly ride the belt.
    /// </summary>
    public class ConveyorLink : Machine
    {
        [SerializeField] CollectorPad source;
        [SerializeField] ScrapStorage destination;
        [SerializeField] float unitsPerSecond = 2f;
        [SerializeField] Transform[] crates;
        [SerializeField] Vector3 start, end;

        float budget, scroll;

        public CollectorPad Source => source;
        public override bool CanBuild => source != null && source.Built;
        public override string CannotBuildReason => "Build the collector first";
        protected override string IdleReason => source != null && source.Hopper.TotalUnits == 0 ? "Collector is empty" : "Warehouse is full";

        public void Setup(string id, string name, CollectorPad from, ScrapStorage to, Vector3 a, Vector3 b, Transform[] crateVisuals)
        {
            machineId = id;
            displayName = name;
            source = from;
            destination = to;
            start = a;
            end = b;
            crates = crateVisuals;
        }

        protected override bool Process(float dt)
        {
            var hopper = source != null ? source.Hopper : null;
            bool work = hopper != null && hopper.TotalUnits > 0 && destination != null && !destination.IsFull;
            if (work)
            {
                budget += unitsPerSecond * dt;
                while (budget >= 1f)
                {
                    budget -= 1f;
                    foreach (var r in GameDatabase.Instance.resources)
                    {
                        if (hopper.Count(r) <= 0) continue;
                        hopper.SetCount(r, hopper.Count(r) - 1);
                        destination.Add(r, 1);
                        break;
                    }
                }
                scroll += dt * 0.08f;
            }

            if (crates != null)
            {
                for (int i = 0; i < crates.Length; i++)
                {
                    if (crates[i] == null) continue;
                    crates[i].gameObject.SetActive(work);
                    float t = Mathf.Repeat(scroll + i / (float)crates.Length, 1f);
                    crates[i].position = Vector3.Lerp(start, end, t) + Vector3.up * 0.9f;
                }
            }
            return work;
        }
    }
}
