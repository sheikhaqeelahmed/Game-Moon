using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanetScrappers.Player
{
    /// <summary>
    /// Procedural animation for the placeholder Rookie: leg and arm swing scaled by speed,
    /// a gentle bob, a tucked pose in the air and a squash on landing. Raises a footstep
    /// event each time a foot plants, which drives dust and footprints.
    /// Replaced by Mixamo clips on the real model in the art pass; the events stay.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] Transform body;
        [SerializeField] Transform leftLeg, rightLeg, leftArm, rightArm;
        [SerializeField] Transform leftFoot, rightFoot;

        [SerializeField] float strideLength = 1.3f;
        [SerializeField] float legSwing = 32f;
        [SerializeField] float armSwing = 26f;
        [SerializeField] float bobHeight = 0.05f;
        [SerializeField] float idleBreath = 0.012f;

        /// <summary>Raised when a foot plants: world position of the foot, true for the left foot.</summary>
        public event Action<Vector3, bool> Footstep;

        Renderer[] renderers;
        Vector3 bodyBase;
        float phase, airBlend, squash;
        float lastSin;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            if (body != null) bodyBase = body.localPosition;
        }

        void OnEnable()
        {
            if (controller != null) controller.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (controller != null) controller.Landed -= OnLanded;
        }

        public void Bind(PlayerController owner) => controller = owner;

        float swingTime, swingDuration = 0.4f;

        /// <summary>Plays one tool swing on the salvage arm.</summary>
        public void PlaySwing(float duration)
        {
            swingDuration = Mathf.Max(0.12f, duration);
            swingTime = swingDuration;
        }

        /// <summary>In first person the body still casts a shadow but is not drawn.</summary>
        public void SetFirstPerson(bool firstPerson)
        {
            renderers ??= GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.shadowCastingMode = firstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }

        void OnLanded(float impactSpeed)
        {
            squash = Mathf.Clamp01(impactSpeed / 8f);
            Footstep?.Invoke(leftFoot != null ? leftFoot.position : transform.position, true);
            Footstep?.Invoke(rightFoot != null ? rightFoot.position : transform.position, false);
        }

        void Update()
        {
            if (controller == null) return;
            float dt = Time.deltaTime;
            float speed = controller.HorizontalSpeed;
            bool grounded = controller.IsGrounded;

            airBlend = Mathf.MoveTowards(airBlend, grounded ? 0f : 1f, dt * 6f);
            float moveBlend = Mathf.Clamp01(speed / Mathf.Max(0.01f, controller.WalkSpeed));
            float sprintBlend = Mathf.InverseLerp(controller.WalkSpeed, controller.SprintSpeed, speed);

            if (grounded) phase += speed / strideLength * Mathf.PI * dt;
            float s = Mathf.Sin(phase);

            float swing = s * Mathf.Lerp(legSwing, legSwing * 1.35f, sprintBlend) * moveBlend;
            float arm = s * Mathf.Lerp(armSwing, armSwing * 1.4f, sprintBlend) * moveBlend;

            // Air pose: knees up, arms out.
            float legAir = -28f * airBlend;
            float armAir = -55f * airBlend;
            SetX(leftLeg, swing * (1f - airBlend) + legAir);
            SetX(rightLeg, -swing * (1f - airBlend) + legAir * 0.6f);
            SetX(leftArm, -arm * (1f - airBlend) + armAir);
            if (swingTime > 0f)
            {
                // Tool swing on the salvage arm: wind up, strike, recover.
                swingTime -= dt;
                float t = 1f - Mathf.Clamp01(swingTime / swingDuration);
                float angle = t < 0.35f ? Mathf.Lerp(0f, -150f, t / 0.35f) : Mathf.Lerp(-150f, -20f, (t - 0.35f) / 0.65f);
                SetX(rightArm, angle);
            }
            else
            {
                SetX(rightArm, arm * (1f - airBlend) + armAir);
            }

            if (body != null)
            {
                float bob = Mathf.Abs(Mathf.Cos(phase)) * bobHeight * moveBlend;
                float breath = Mathf.Sin(Time.time * 1.8f) * idleBreath * (1f - moveBlend);
                squash = Mathf.MoveTowards(squash, 0f, dt * 4f);
                body.localPosition = bodyBase + Vector3.up * (bob + breath - squash * 0.12f);
                body.localScale = new Vector3(1f + squash * 0.12f, 1f - squash * 0.12f, 1f + squash * 0.12f);
                body.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 10f, sprintBlend) * moveBlend, 0f, 0f);
            }

            // A foot plants each time the swing crosses zero.
            if (grounded && speed > 0.6f && Mathf.Sign(s) != Mathf.Sign(lastSin))
            {
                bool left = s > 0f;
                var foot = left ? leftFoot : rightFoot;
                Footstep?.Invoke(foot != null ? foot.position : transform.position, left);
            }
            lastSin = s;
        }

        static void SetX(Transform t, float degrees)
        {
            if (t != null) t.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
