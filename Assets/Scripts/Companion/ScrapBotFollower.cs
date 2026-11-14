using UnityEngine;

namespace PlanetScrappers.Companion
{
    /// <summary>
    /// Keeps SCRAP-BOT hovering ahead and to the right of the player, bobbing gently
    /// and turning to face the player's camera. No physics: it floats through props.
    /// </summary>
    public class ScrapBotFollower : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Transform playerCamera;

        [Tooltip("Hover position relative to the player's facing direction (x = right, y = up, z = forward).")]
        [SerializeField] Vector3 offset = new Vector3(1.7f, 1.9f, 3.2f);
        [SerializeField] float followSmoothTime = 0.3f;
        [SerializeField] float turnSpeed = 5f;
        [SerializeField] float bobAmplitude = 0.12f;
        [SerializeField] float bobFrequency = 0.6f;
        [Tooltip("If the bot falls this far behind (e.g. after a respawn), it snaps back.")]
        [SerializeField] float snapDistance = 30f;

        Vector3 velocity;
        Vector3 lastPlayerPosition;
        Vector3 playerVelocity;

        public void SetTarget(Transform target, Transform cameraTransform)
        {
            player = target;
            playerCamera = cameraTransform;
        }

        void OnEnable()
        {
            if (player != null) lastPlayerPosition = player.position;
        }

        void LateUpdate()
        {
            if (player == null) return;

            // Lead the target by the player's horizontal velocity so the smoothing lag
            // doesn't let the bot drift into the middle of the view while sprinting.
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 frameVelocity = (player.position - lastPlayerPosition) / dt;
            frameVelocity.y = 0f;
            lastPlayerPosition = player.position;
            if (frameVelocity.sqrMagnitude > 400f) frameVelocity = Vector3.zero; // teleport, ignore
            playerVelocity = Vector3.Lerp(playerVelocity, frameVelocity, 1f - Mathf.Exp(-10f * dt));

            Quaternion yaw = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
            float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            Vector3 desired = player.position + yaw * offset + playerVelocity * followSmoothTime + Vector3.up * bob;

            if ((transform.position - desired).sqrMagnitude > snapDistance * snapDistance)
            {
                transform.position = desired;
                velocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, followSmoothTime);
            }

            Vector3 lookTarget = playerCamera != null ? playerCamera.position : player.position + Vector3.up * 1.6f;
            Vector3 toTarget = lookTarget - transform.position;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion face = Quaternion.LookRotation(toTarget, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, face, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
            }
        }
    }
}
