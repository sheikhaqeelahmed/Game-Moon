using System.Collections.Generic;
using PlanetScrappers.Economy;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>A storage container (truck bin, collector) acting as a scrap magnet.</summary>
    public class StorageScrapSink : ScrapSink
    {
        [SerializeField] ScrapStorage storage;
        [SerializeField] Transform attractPoint;
        [SerializeField] float radius = 8f;

        /// <summary>Set by the truck: the bin only pulls scrap while someone is driving.</summary>
        public bool Active { get; set; } = true;

        public void Setup(ScrapStorage s, Transform point, float r)
        {
            storage = s;
            attractPoint = point;
            radius = r;
        }

        public override Vector3 AttractPoint => attractPoint != null ? attractPoint.position : transform.position;
        public override float Radius => radius;
        public override bool CanAccept => Active && isActiveAndEnabled && storage != null && !storage.IsFull;
        public override int Accept(ResourceDefinition r, int amount) => storage.Add(r, amount);
    }
}
