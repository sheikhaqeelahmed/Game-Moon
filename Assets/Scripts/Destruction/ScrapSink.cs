using System.Collections.Generic;
using PlanetScrappers.Economy;
using PlanetScrappers.Inventory;
using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>
    /// Anything that pulls loose scrap towards itself: the player's backpack, the truck bin
    /// while driving, a built scrap collector. Scrap pieces fly to the nearest one in range.
    /// </summary>
    public abstract class ScrapSink : MonoBehaviour
    {
        public static readonly List<ScrapSink> All = new List<ScrapSink>();

        public abstract Vector3 AttractPoint { get; }
        public abstract float Radius { get; }
        public abstract bool CanAccept { get; }
        public abstract int Accept(ResourceDefinition resource, int amount);

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        /// <summary>Nearest sink whose radius reaches the position and that has room.</summary>
        public static ScrapSink Nearest(Vector3 position)
        {
            ScrapSink best = null;
            float bestDist = float.MaxValue;
            foreach (var s in All)
            {
                if (!s.CanAccept) continue;
                float d = (s.AttractPoint - position).sqrMagnitude;
                if (d <= s.Radius * s.Radius && d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }
            return best;
        }
    }
}
