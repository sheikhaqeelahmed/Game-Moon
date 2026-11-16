using System.Collections.Generic;
using PlanetScrappers.Economy;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>The Rookie's backpack as a scrap magnet.</summary>
    public class PlayerScrapSink : ScrapSink
    {
        [SerializeField] PlayerInventory inventory;
        [SerializeField] float radius = 5.5f;

        public override Vector3 AttractPoint => transform.position + Vector3.up * 1.1f;
        public override float Radius => radius;
        public override bool CanAccept => isActiveAndEnabled && inventory != null && !inventory.IsFull;
        public override int Accept(ResourceDefinition r, int amount) => inventory.Add(r, amount);
    }
}
