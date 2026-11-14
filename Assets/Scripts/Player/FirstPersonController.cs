using PlanetScrappers.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Player
{
    /// <summary>
    /// First-person movement for the Rookie Scrapper: walk, sprint, jump and look,
    /// with keyboard/mouse and gamepad bindings defined in code.
    /// Kessler-9 has low gravity, so jumps are a little floaty but stay controllable.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraRoot;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float sprintSpeed = 7.5f;
        [Tooltip("How quickly the player reaches target speed on the ground. Higher is snappier.")]
        [SerializeField] float groundAcceleration = 14f;
        [Tooltip("Fraction of ground acceleration available in the air.")]
        [Range(0f, 1f)] [SerializeField] float airControl = 0.3f;
        [SerializeField] float jumpHeight = 1.3f;
        [Tooltip("Downward acceleration in m/s². Earth is 9.81; Kessler-9 is a bit lighter and the arc feels better slightly higher than real lunar gravity.")]
        [SerializeField] float gravity = 11f;
        [SerializeField] float groundedStickForce = 2f;

        [Header("Look")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] float mouseSensitivity = 0.08f;
        [Tooltip("Degrees per second at full stick tilt.")]
        [SerializeField] float stickSensitivity = 160f;
        [SerializeField] bool invertY;
        [SerializeField] float minPitch = -85f;
        [SerializeField] float maxPitch = 85f;

        [Header("Safety")]
        [Tooltip("If the player ever falls below this height, they are returned to the spawn point.")]
        [SerializeField] float fallResetY = -25f;

        CharacterController controller;
        InputAction moveAction, lookMouseAction, lookStickAction, jumpAction, sprintAction;

        Vector3 horizontalVelocity;
        float verticalVelocity;
        float pitch;
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        /// <summary>Set false to freeze the player, e.g. while a shop or menu is open.</summary>
        public bool InputEnabled { get; set; } = true;
        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = Mathf.Max(0.001f, value); }
        public bool InvertY { get => invertY; set => invertY = value; }
        public bool IsGrounded => controller.isGrounded;
        public bool IsSprinting { get; private set; }
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
        public Transform CameraRoot => cameraRoot;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraRoot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraRoot = cam.transform;
            }

            SetSpawnPoint(transform.position, transform.rotation);
            CreateInputActions();
        }

        void CreateInputActions()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookMouseAction = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStickAction = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");
        }

        void OnEnable()
        {
            moveAction.Enable();
            lookMouseAction.Enable();
            lookStickAction.Enable();
            jumpAction.Enable();
            sprintAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            lookMouseAction.Disable();
            lookStickAction.Disable();
            jumpAction.Disable();
            sprintAction.Disable();
        }

        void OnDestroy()
        {
            moveAction.Dispose();
            lookMouseAction.Dispose();
            lookStickAction.Dispose();
            jumpAction.Dispose();
            sprintAction.Dispose();
        }

        void Update()
        {
            if (transform.position.y < fallResetY)
            {
                Teleport(spawnPosition, spawnRotation);
                return;
            }

            bool acceptInput = InputEnabled && CursorLock.IsLocked;
            UpdateLook(acceptInput);
            UpdateMovement(acceptInput);
        }

        void UpdateLook(bool acceptInput)
        {
            if (!acceptInput) return;

            Vector2 delta = lookMouseAction.ReadValue<Vector2>() * mouseSensitivity
                          + lookStickAction.ReadValue<Vector2>() * (stickSensitivity * Time.deltaTime);

            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch + (invertY ? delta.y : -delta.y), minPitch, maxPitch);
            if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void UpdateMovement(bool acceptInput)
        {
            float dt = Time.deltaTime;
            Vector2 input = acceptInput ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

            IsSprinting = acceptInput && sprintAction.IsPressed() && input.y > 0.1f;
            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 wishVelocity = (transform.right * input.x + transform.forward * input.y) * targetSpeed;

            bool grounded = controller.isGrounded;
            float accel = grounded ? groundAcceleration : groundAcceleration * airControl;
            // Frame-rate independent exponential approach to the target velocity.
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, wishVelocity, 1f - Mathf.Exp(-accel * dt));

            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -groundedStickForce;

            if (grounded && acceptInput && jumpAction.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * gravity);

            verticalVelocity -= gravity * dt;

            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }

        public void SetSpawnPoint(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        }

        /// <summary>Moves the player instantly, clearing momentum. Used for respawn and, later, save loading.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
            controller.enabled = true;

            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            pitch = 0f;
            if (cameraRoot != null) cameraRoot.localRotation = Quaternion.identity;
        }
    }
}
