using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Equipment
{
    /// <summary>
    /// Aims at whatever is under the screen centre, swings the fitted tool while the attack
    /// button is held, and lets SCRAP-BOT scan the target (F). 1/2/3 switch owned tools.
    /// </summary>
    public class ToolUser : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] PlayerVisual visual;
        [SerializeField] PlayerEquipment equipment;
        [SerializeField] float aimDistance = 30f;

        InputAction attack, scan, tool1, tool2, tool3;
        readonly RaycastHit[] hits = new RaycastHit[16];
        float cooldown;
        float noToolHintCooldown;

        public DestructibleObject Target { get; private set; }
        public bool TargetInRange { get; private set; }

        void Awake()
        {
            attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/rightTrigger");
            scan = new InputAction("Scan", InputActionType.Button, "<Keyboard>/f");
            scan.AddBinding("<Gamepad>/buttonNorth");
            tool1 = new InputAction("Tool1", InputActionType.Button, "<Keyboard>/1");
            tool2 = new InputAction("Tool2", InputActionType.Button, "<Keyboard>/2");
            tool3 = new InputAction("Tool3", InputActionType.Button, "<Keyboard>/3");
        }

        void OnEnable() { attack.Enable(); scan.Enable(); tool1.Enable(); tool2.Enable(); tool3.Enable(); }
        void OnDisable() { attack.Disable(); scan.Disable(); tool1.Disable(); tool2.Disable(); tool3.Disable(); }
        void OnDestroy() { attack.Dispose(); scan.Dispose(); tool1.Dispose(); tool2.Dispose(); tool3.Dispose(); }

        void Update()
        {
            cooldown -= Time.deltaTime;
            noToolHintCooldown -= Time.deltaTime;

            if (!controller.InputEnabled || !CursorLock.IsLocked || controller.Camera == null)
            {
                Target = null;
                TargetInRange = false;
                return;
            }

            FindTarget(out Vector3 point, out Vector3 normal);

            if (tool1.WasPressedThisFrame()) SwitchTo(1);
            if (tool2.WasPressedThisFrame()) SwitchTo(2);
            if (tool3.WasPressedThisFrame()) SwitchTo(3);

            if (scan.WasPressedThisFrame())
            {
                AudioManager.Play("ui_click", 0.5f, 1.4f);
                GameEvents.Say(Target != null ? Target.ScanText(equipment.Equipped) : "Nothing salvageable there. Point me at some junk.", 6f);
            }

            var tool = equipment.Equipped;
            if (!attack.IsPressed()) return;

            if (tool == null)
            {
                if (noToolHintCooldown <= 0f)
                {
                    GameEvents.Say("Bare hands? Bold. The rusty wrench is on the crate with the warm light, west of the road. Press E to grab it.", 6f);
                    noToolHintCooldown = 8f;
                }
                return;
            }

            if (cooldown > 0f) return;
            cooldown = 1f / Mathf.Max(0.1f, tool.hitsPerSecond);
            controller.FaceCameraFor(0.6f);
            visual.PlaySwing(Mathf.Min(0.45f, cooldown * 0.9f));

            if (Target != null && TargetInRange)
            {
                var result = Target.Hit(tool, point, normal);
                if (result == HitResult.TooWeak && noToolHintCooldown <= 0f)
                {
                    GameEvents.Say($"Bonk. The {Target.Definition.displayName} needs a tier {Target.Definition.tier} tool. Nova sells those.", 5f);
                    noToolHintCooldown = 6f;
                }
            }
            else
            {
                AudioManager.PlayVaried("swing_whoosh", 0.5f);
            }
        }

        void SwitchTo(int level)
        {
            var t = equipment.OwnedToolAtLevel(level);
            if (t == null || t == equipment.Equipped) return;
            equipment.Equip(t);
            AudioManager.Play("ui_click", 0.6f);
        }

        void FindTarget(out Vector3 point, out Vector3 normal)
        {
            point = normal = Vector3.zero;
            Target = null;
            TargetInRange = false;

            var cam = controller.Camera.transform;
            int count = Physics.RaycastNonAlloc(cam.position, cam.forward, hits, aimDistance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.distance >= best) continue;
                best = h.distance;
                Target = h.collider.GetComponentInParent<DestructibleObject>();
                point = h.point;
                normal = h.normal;
            }

            if (Target != null && Target.IsDismantled) Target = null;
            if (Target == null) return;

            float reach = (equipment.Equipped != null ? equipment.Equipped.range : 2.2f) + 0.6f;
            Vector3 chest = transform.position + Vector3.up * 1.2f;
            TargetInRange = (point - chest).magnitude <= reach;
        }
    }
}
