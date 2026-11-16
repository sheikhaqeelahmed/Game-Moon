using System;
using PlanetScrappers.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Player
{
    /// <summary>
    /// Moves the Rookie Scrapper relative to the camera: walk, sprint, low-gravity jump.
    /// In third person the body turns toward the direction of travel; in first person it
    /// always faces where the camera looks.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerCamera playerCamera;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float sprintSpeed = 7.5f;
        [Tooltip("How quickly the player reaches target speed on the ground. Higher is snappier.")]
        [SerializeField] float groundAcceleration = 14f;
        [Range(0f, 1f)] [SerializeField] float airControl = 0.3f;
        [Tooltip("How quickly the body turns toward the movement direction in third person.")]
        [SerializeField] float turnSharpness = 12f;
        [SerializeField] float jumpHeight = 1.3f;
        [Tooltip("Downward acceleration in m/s². Lighter than Earth so jumps float a little.")]
        [SerializeField] float gravity = 11f;
        [SerializeField] float groundedStickForce = 2f;

        [Header("Safety")]
        [Tooltip("If the player ever falls below this height, they return to the spawn point.")]
        [SerializeField] float fallResetY = -25f;

        CharacterController controller;
        InputAction moveAction, jumpAction, sprintAction;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        bool wasGrounded = true;
        Vector3 spawnPosition;
        Quaternion spawnRotation;

        /// <summary>Raised when the player leaves the ground by jumping.</summary>
        public event Action Jumped;
        /// <summary>Raised on touching down; the value is the downward speed at impact (m/s).</summary>
        public event Action<float> Landed;

        public bool InputEnabled { get; set; } = true;
        public bool IsGrounded => controller.isGrounded;
        public bool IsSprinting { get; private set; }
        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;
        public PlayerCamera Camera => playerCamera;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            SetSpawnPoint(transform.position, transform.rotation);

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");
        }

        void OnEnable() { moveAction.Enable(); jumpAction.Enable(); sprintAction.Enable(); }
        void OnDisable() { moveAction.Disable(); jumpAction.Disable(); sprintAction.Disable(); }
        void OnDestroy() { moveAction.Dispose(); jumpAction.Dispose(); sprintAction.Dispose(); }

        public void SetCamera(PlayerCamera cam) => playerCamera = cam;

        void Update()
        {
            if (transform.position.y < fallResetY)
            {
                Teleport(spawnPosition, spawnRotation);
                return;
            }

            float dt = Time.deltaTime;
            bool acceptInput = InputEnabled && CursorLock.IsLocked;
            Vector2 input = acceptInput ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;

            float cameraYaw = playerCamera != null ? playerCamera.Yaw : transform.eulerAngles.y;
            Quaternion cameraFlat = Quaternion.Euler(0f, cameraYaw, 0f);

            IsSprinting = acceptInput && sprintAction.IsPressed() && input.sqrMagnitude > 0.01f;
            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 wishVelocity = cameraFlat * new Vector3(input.x, 0f, input.y) * targetSpeed;

            bool grounded = controller.isGrounded;
            float accel = grounded ? groundAcceleration : groundAcceleration * airControl;
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, wishVelocity, 1f - Mathf.Exp(-accel * dt));

            // Facing
            bool firstPerson = playerCamera != null && playerCamera.Mode == CameraMode.FirstPerson;
            if (firstPerson)
            {
                transform.rotation = cameraFlat;
            }
            else if (wishVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion face = Quaternion.LookRotation(wishVelocity, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 1f - Mathf.Exp(-turnSharpness * dt));
            }

            // Vertical
            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -groundedStickForce;

            if (grounded && acceptInput && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * gravity);
                Jumped?.Invoke();
            }

            verticalVelocity -= gravity * dt;
            float impactSpeed = -verticalVelocity;

            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;

            bool nowGrounded = controller.isGrounded;
            if (nowGrounded && !wasGrounded && impactSpeed > 3f)
                Landed?.Invoke(impactSpeed);
            wasGrounded = nowGrounded;
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
            if (playerCamera != null) playerCamera.ResetBehindTarget();
        }
    }
}
