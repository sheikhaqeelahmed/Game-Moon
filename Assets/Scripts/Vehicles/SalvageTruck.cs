using PlanetScrappers.Companion;
using PlanetScrappers.Core;
using PlanetScrappers.Destruction;
using PlanetScrappers.Interaction;
using PlanetScrappers.Inventory;
using PlanetScrappers.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Vehicles
{
    /// <summary>
    /// The SH-01 starter salvage truck. E to get in and out; W/S throttle, A/D steer, Space brake.
    /// While driving, its bin pulls in loose scrap around it. Arcade handling tuned for low gravity.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SalvageTruck : Interactable
    {
        public static SalvageTruck Instance { get; private set; }

        [SerializeField] PlayerController player;
        [SerializeField] ScrapStorage bin;
        [SerializeField] StorageScrapSink binMagnet;
        [SerializeField] Transform exitPoint;
        [SerializeField] Transform[] wheels;

        [Header("Handling")]
        [SerializeField] float maxForward = 14f;
        [SerializeField] float maxReverse = 5f;
        [SerializeField] float acceleration = 7f;
        [SerializeField] float braking = 18f;
        [SerializeField] float turnRate = 70f;
        [SerializeField] float gravity = 11f;

        CharacterController controller;
        InputAction move, brake, exit;
        float speed, yaw, verticalVelocity;
        int enteredFrame = -1;

        public bool Driving { get; private set; }
        public float Speed => speed;
        public ScrapStorage Bin => bin;
        public override string Prompt => $"Drive the salvage truck  (bin {bin.TotalUnits}/{bin.Capacity})";
        public override bool CanInteract => base.CanInteract && !Driving;

        void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            move = new InputAction("Drive", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            brake = new InputAction("Brake", InputActionType.Button, "<Keyboard>/space");
            brake.AddBinding("<Gamepad>/buttonSouth");
            exit = new InputAction("Exit", InputActionType.Button, "<Keyboard>/e");
            exit.AddBinding("<Gamepad>/buttonWest");
            if (binMagnet != null) binMagnet.Active = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            move.Enable(); brake.Enable(); exit.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            move.Disable(); brake.Disable(); exit.Disable();
        }

        void OnDestroy() { move.Dispose(); brake.Dispose(); exit.Dispose(); }

        public override void Interact() => Enter();

        public void Enter()
        {
            if (Driving) return;
            Driving = true;
            enteredFrame = Time.frameCount;
            if (binMagnet != null) binMagnet.Active = true;

            var cam = player.Camera;
            player.gameObject.SetActive(false);
            cam.SetTarget(transform, null);
            cam.SetFollowProfile(9f, 3.2f, 0f);
            cam.SetMode(CameraMode.ThirdPerson);
            FollowBot(transform, 2.4f);

            AudioManager.Play("purchase", 0.5f, 0.6f);
            GameEvents.RaiseInteracted("truck");
            GameEvents.RaiseVehicleEntered("truck");
            GameEvents.Say("Truck's yours. W and S for speed, A and D to steer, Space to brake. Drive over scrap to vacuum it into the bin. E to get out.", 8f);
        }

        public void Exit()
        {
            if (!Driving) return;
            Driving = false;
            speed = 0f;
            if (binMagnet != null) binMagnet.Active = false;

            Vector3 spot = exitPoint != null ? exitPoint.position : transform.position - transform.right * 2.5f;
            spot.y = Mathf.Max(spot.y, 0.1f);
            player.gameObject.SetActive(true);
            player.Teleport(spot, transform.rotation);

            var cam = player.Camera;
            cam.SetTarget(player.transform, player.GetComponentInChildren<PlayerVisual>());
            cam.SetFollowProfile(-1f, -1f, -1f);
            SettingsManager.Apply(cam);
            FollowBot(player.transform, 1f);
        }

        void FollowBot(Transform target, float scale)
        {
            var bot = FindAnyObjectByType<ScrapBotFollower>();
            if (bot != null) bot.SetTarget(target, player.Camera.transform, scale);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 input = Vector2.zero;
            bool brakeHeld = false;

            if (Driving && CursorLock.IsLocked && Time.timeScale > 0f)
            {
                input = move.ReadValue<Vector2>();
                brakeHeld = brake.IsPressed();
                if (exit.WasPressedThisFrame() && Time.frameCount != enteredFrame)
                {
                    Exit();
                    return;
                }
            }

            float target = input.y >= 0f ? input.y * maxForward : input.y * maxReverse;
            bool opposing = Mathf.Abs(speed) > 0.1f && Mathf.Sign(target) != Mathf.Sign(speed) && Mathf.Abs(target) > 0.1f;
            float rate = brakeHeld || opposing ? braking : (Mathf.Abs(target) > 0.01f ? acceleration : acceleration * 0.6f);
            if (brakeHeld) target = 0f;
            speed = Mathf.MoveTowards(speed, target, rate * dt);

            // Steering scales with speed and reverses when backing up, like a real truck.
            float steerFactor = Mathf.Clamp(speed / 6f, -1f, 1f);
            yaw += input.x * turnRate * steerFactor * dt;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= gravity * dt;
            var flags = controller.Move((transform.forward * speed + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Sides) != 0) speed *= 0.6f;

            if (wheels != null)
                foreach (var w in wheels)
                    if (w != null) w.Rotate(Vector3.up, speed / 0.45f * Mathf.Rad2Deg * dt, Space.Self);
        }

        /// <summary>Places the parked truck (save loading).</summary>
        public void Place(Vector3 position, float yawDegrees)
        {
            controller.enabled = false;
            yaw = yawDegrees;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            controller.enabled = true;
        }
    }
}
