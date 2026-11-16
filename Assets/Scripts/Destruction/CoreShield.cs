using UnityEngine;

namespace PlanetScrappers.Destruction
{
    /// <summary>
    /// Protects the Mega-Factory's reactor core: while any of its pylons is standing the core
    /// cannot be damaged, and a shield bubble shows around it.
    /// </summary>
    public class CoreShield : MonoBehaviour
    {
        [SerializeField] DestructibleObject[] pylons;
        [SerializeField] GameObject bubble;

        public bool Active
        {
            get
            {
                if (pylons == null) return false;
                foreach (var p in pylons) if (p != null && !p.IsDismantled) return true;
                return false;
            }
        }

        public int PylonsStanding
        {
            get
            {
                int n = 0;
                if (pylons != null) foreach (var p in pylons) if (p != null && !p.IsDismantled) n++;
                return n;
            }
        }

        public void Setup(DestructibleObject[] supports, GameObject shieldBubble)
        {
            pylons = supports;
            bubble = shieldBubble;
        }

        void Update()
        {
            if (bubble == null) return;
            bool on = Active;
            if (bubble.activeSelf != on) bubble.SetActive(on);
            if (on) bubble.transform.Rotate(Vector3.up, 12f * Time.deltaTime, Space.World);
        }
    }
}
