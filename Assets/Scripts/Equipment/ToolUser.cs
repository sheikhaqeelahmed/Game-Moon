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

        InputAction attack, scan, nextTool, prevTool;
        bool toggledOn;
        readonly InputAction[] toolKeys = new InputAction[5];
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
            for (int i = 0; i < toolKeys.Length; i++)
                toolKeys[i] = new InputAction("Tool" + (i + 1), InputActionType.Button, "<Keyboard>/" + (i + 1));
            nextTool = new InputAction("NextTool", InputActionType.Button, "<Gamepad>/dpad/right");
            nextTool.AddBinding("<Mouse>/forwardButton");
            prevTool = new InputAction("PrevTool", InputActionType.Button, "<Gamepad>/dpad/left");
            prevTool.AddBinding("<Mouse>/backButton");
        }

        void OnEnable() { attack.Enable(); scan.Enable(); nextTool.Enable(); prevTool.Enable(); foreach (var k in toolKeys) k.Enable(); }
        void OnDisable() { attack.Disable(); scan.Disable(); nextTool.Disable(); prevTool.Disable(); foreach (var k in toolKeys) k.Disable(); }
        void OnDestroy() { attack.Dispose(); scan.Dispose(); nextTool.Dispose(); prevTool.Dispose(); foreach (var k in toolKeys) k.Dispose(); }

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

            for (int i = 0; i < toolKeys.Length; i++)
                if (toolKeys[i].WasPressedThisFrame()) SwitchTo(i + 1);
            if (nextTool.WasPressedThisFrame()) Cycle(1);
            if (prevTool.WasPressedThisFrame()) Cycle(-1);

            if (scan.WasPressedThisFrame())
            {
                AudioManager.Play("ui_click", 0.5f, 1.4f);
                GameEvents.Say(Target != null ? Target.ScanText(equipment.Equipped) : "Nothing salvageable there. Point me at some junk.", 6f);
            }

            var tool = equipment.Equipped;
            bool usingTool;
            if (SettingsManager.Current.toggleToolUse)
            {
                if (attack.WasPressedThisFrame()) toggledOn = !toggledOn;
                usingTool = toggledOn;
            }
            else usingTool = attack.IsPressed();
            if (!usingTool) return;

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

        void Cycle(int dir)
        {
            int current = equipment.Equipped != null ? equipment.Equipped.level : 0;
            for (int step = 1; step <= 6; step++)
            {
                int level = ((current - 1 + dir * step) % 6 + 6) % 6 + 1;
                if (equipment.OwnedToolAtLevel(level) != null) { SwitchTo(level); return; }
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
