using PlanetScrappers.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Player
{
    public enum CameraMode { ThirdPerson, FirstPerson }

    /// <summary>
    /// Over-the-shoulder third-person camera (default) with a first-person mode on V.
    /// Owns look input; the player controller moves relative to this camera's yaw.
    /// The camera pulls in when a rock or building would block the view.
    /// </summary>
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerVisual visual;
        [SerializeField] CameraMode mode = CameraMode.ThirdPerson;

        [Header("Third person")]
        [SerializeField] float pivotHeight = 1.6f;
        [SerializeField] float shoulderOffset = 0.55f;
        [SerializeField] float distance = 4.2f;
        [SerializeField] float minDistance = 0.8f;
        [SerializeField] float collisionRadius = 0.25f;
        [SerializeField] float zoomOutSpeed = 6f;
        [SerializeField] float thirdMinPitch = -35f;
        [SerializeField] float thirdMaxPitch = 65f;

        [Header("First person")]
        [SerializeField] float eyeHeight = 1.65f;
        [SerializeField] float firstMinPitch = -85f;
        [SerializeField] float firstMaxPitch = 85f;

        [Header("Look")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] float mouseSensitivity = 0.08f;
        [Tooltip("Degrees per second at full stick tilt.")]
        [SerializeField] float stickSensitivity = 160f;
        [SerializeField] bool invertY;

        InputAction lookMouse, lookStick, toggleView;
        float yaw, pitch = 12f, currentDistance;
        readonly RaycastHit[] hits = new RaycastHit[16];

        public CameraMode Mode => mode;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = Mathf.Max(0.001f, value); }
        public bool InvertY { get => invertY; set => invertY = value; }

        void Awake()
        {
            if (target != null) yaw = target.eulerAngles.y;
            currentDistance = distance;

            lookMouse = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");
            toggleView = new InputAction("ToggleView", InputActionType.Button, "<Keyboard>/v");
            toggleView.AddBinding("<Gamepad>/rightStickPress");
        }

        void OnEnable()
        {
            lookMouse.Enable(); lookStick.Enable(); toggleView.Enable();
            ApplyModeToVisual();
        }

        void OnDisable()
        {
            lookMouse.Disable(); lookStick.Disable(); toggleView.Disable();
        }

        void OnDestroy()
        {
            lookMouse.Dispose(); lookStick.Dispose(); toggleView.Dispose();
        }

        public void SetTarget(Transform newTarget, PlayerVisual newVisual)
        {
            target = newTarget;
            visual = newVisual;
            if (target != null) yaw = target.eulerAngles.y;
        }

        public void SetMode(CameraMode newMode)
        {
            mode = newMode;
            ClampPitch();
            ApplyModeToVisual();
        }

        /// <summary>Faces the camera the same way as the target, e.g. after a respawn.</summary>
        public void ResetBehindTarget()
        {
            if (target == null) return;
            yaw = target.eulerAngles.y;
            pitch = mode == CameraMode.ThirdPerson ? 12f : 0f;
            currentDistance = distance;
            SnapToPose();
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (CursorLock.IsLocked)
            {
                if (toggleView.WasPressedThisFrame())
                    SetMode(mode == CameraMode.ThirdPerson ? CameraMode.FirstPerson : CameraMode.ThirdPerson);

                Vector2 delta = lookMouse.ReadValue<Vector2>() * mouseSensitivity
                              + lookStick.ReadValue<Vector2>() * (stickSensitivity * Time.deltaTime);
                yaw += delta.x;
                pitch += invertY ? delta.y : -delta.y;
                ClampPitch();
            }

            UpdatePose(Time.deltaTime);
        }

        void ClampPitch()
        {
            pitch = mode == CameraMode.ThirdPerson
                ? Mathf.Clamp(pitch, thirdMinPitch, thirdMaxPitch)
                : Mathf.Clamp(pitch, firstMinPitch, firstMaxPitch);
        }

        void ApplyModeToVisual()
        {
            if (visual != null) visual.SetFirstPerson(mode == CameraMode.FirstPerson);
        }

        /// <summary>Places the camera immediately, without smoothing (used by the scene builder too).</summary>
        public void SnapToPose()
        {
            currentDistance = distance;
            UpdatePose(0f);
        }

        void UpdatePose(float dt)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (mode == CameraMode.FirstPerson)
            {
                transform.SetPositionAndRotation(target.position + Vector3.up * eyeHeight, rotation);
                return;
            }

            Vector3 pivot = target.position + Vector3.up * pivotHeight
                          + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * shoulderOffset;
            Vector3 back = rotation * Vector3.back;

            float allowed = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, back, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(target)) continue; // ignore the player's own colliders
                if (hit.distance <= 0f) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(minDistance, hit.distance));
            }

            // Pull in instantly so nothing ever blocks the view; ease back out.
            currentDistance = allowed < currentDistance || dt <= 0f
                ? allowed
                : Mathf.MoveTowards(currentDistance, allowed, zoomOutSpeed * dt);

            transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
        }
    }
}
