using PlanetScrappers.Core;
using PlanetScrappers.Equipment;
using UnityEngine;

namespace PlanetScrappers.Missions
{
    /// <summary>Opens the Machine Row gate once the player owns a tier 3 tool (Mission 8).</summary>
    public class MachineRowGate : MonoBehaviour
    {
        [SerializeField] int requiredTier = 3;
        [SerializeField] float sinkDepth = 3.2f;

        Vector3 closedPosition;
        bool open;

        void Awake() => closedPosition = transform.position;

        void Update()
        {
            var eq = PlayerEquipment.Instance;
            if (!open && eq != null)
            {
                foreach (var t in GameDatabase.Instance.tools)
                {
                    if (t != null && t.tier >= requiredTier && eq.Owns(t))
                    {
                        Open();
                        break;
                    }
                }
            }

            // Slide down into the ground when open.
            Vector3 target = open ? closedPosition + Vector3.down * sinkDepth : closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, 1.5f * Time.deltaTime);
        }

        void Open()
        {
            open = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            AudioManager.Play("purchase", 0.6f, 0.7f);
        }
    }
}
