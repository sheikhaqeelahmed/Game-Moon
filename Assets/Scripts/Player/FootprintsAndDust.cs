using PlanetScrappers.Core;
using UnityEngine;

namespace PlanetScrappers.Player
{
    /// <summary>
    /// Leaves boot prints in the regolith and kicks up slow, low-gravity dust on every step
    /// and landing. Both use one pooled particle system each, so there is no per-step
    /// allocation; prints fade out after about 25 seconds.
    /// </summary>
    public class FootprintsAndDust : MonoBehaviour
    {
        [SerializeField] PlayerVisual visual;
        [SerializeField] ParticleSystem prints;
        [SerializeField] ParticleSystem dust;
        [SerializeField] float printSize = 0.95f;
        [SerializeField] int dustPerStep = 5;

        void OnEnable()
        {
            if (visual != null) visual.Footstep += OnFootstep;
        }

        void OnDisable()
        {
            if (visual != null) visual.Footstep -= OnFootstep;
        }

        void OnFootstep(Vector3 footPosition, bool left)
        {
            Vector3 ground = footPosition;
            if (Physics.Raycast(footPosition + Vector3.up * 0.5f, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                ground = hit.point;

            AudioManager.PlayVaried("footstep", 0.3f, 0.15f);

            if (prints != null)
            {
                var p = new ParticleSystem.EmitParams
                {
                    position = ground + Vector3.up * 0.015f,
                    startSize = printSize,
                    rotation = transform.eulerAngles.y + (left ? -4f : 4f),
                    applyShapeToPosition = false
                };
                prints.Emit(p, 1);
            }

            if (dust != null)
            {
                for (int i = 0; i < dustPerStep; i++)
                {
                    Vector3 v = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.4f, 1.1f), Random.Range(-0.6f, 0.6f));
                    var d = new ParticleSystem.EmitParams
                    {
                        position = ground + Vector3.up * 0.05f,
                        velocity = v,
                        applyShapeToPosition = false
                    };
                    dust.Emit(d, 1);
                }
            }
        }
    }
}
