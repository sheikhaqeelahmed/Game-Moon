using PlanetScrappers.Core;
using PlanetScrappers.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Interaction
{
    /// <summary>Finds the nearest usable interactable around the player and uses it on E.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        InputAction use;

        public Interactable Current { get; private set; }

        void Awake()
        {
            use = new InputAction("Use", InputActionType.Button, "<Keyboard>/e");
            use.AddBinding("<Gamepad>/buttonWest");
        }

        void OnEnable() => use.Enable();
        void OnDisable() => use.Disable();
        void OnDestroy() => use.Dispose();

        void Update()
        {
            Current = null;
            if (!controller.InputEnabled || !CursorLock.IsLocked) return;

            float best = float.MaxValue;
            Vector3 p = transform.position;
            foreach (var i in Interactable.All)
            {
                if (!i.CanInteract) continue;
                Vector3 d = i.transform.position - p;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist <= i.Radius && dist < best)
                {
                    best = dist;
                    Current = i;
                }
            }

            if (Current != null && use.WasPressedThisFrame())
            {
                AudioManager.Play("ui_click", 0.7f);
                Current.Interact();
            }
        }
    }
}
