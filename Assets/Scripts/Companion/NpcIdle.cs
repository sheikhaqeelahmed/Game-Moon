using UnityEngine;

namespace PlanetScrappers.Companion
{
    /// <summary>
    /// Simple idle life for station NPCs (Nova, Captain Bolt): a slow breath on the body
    /// and the head turning to watch the player when they come close.
    /// </summary>
    public class NpcIdle : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] float lookRange = 9f;
        [SerializeField] float maxHeadYaw = 70f;

        Transform player;
        Vector3 bodyBase;
        Quaternion headBase;
        float seed;

        void Awake()
        {
            if (body != null) bodyBase = body.localPosition;
            if (head != null) headBase = head.localRotation;
            seed = Random.value * 10f;
        }

        public void Setup(Transform bodyPivot, Transform headPivot)
        {
            body = bodyPivot;
            head = headPivot;
        }

        void Update()
        {
            bool still = Core.SettingsManager.Current != null && Core.SettingsManager.Current.reduceMotion;
            if (body != null && !still)
                body.localPosition = bodyBase + Vector3.up * (Mathf.Sin((Time.time + seed) * 1.6f) * 0.012f);

            if (head == null) return;
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            Quaternion target = headBase;
            if (player != null)
            {
                Vector3 to = player.position - transform.position;
                to.y = 0f;
                if (to.magnitude < lookRange && to.sqrMagnitude > 0.01f)
                {
                    float yaw = Vector3.SignedAngle(transform.forward, to, Vector3.up);
                    target = headBase * Quaternion.Euler(0f, Mathf.Clamp(yaw, -maxHeadYaw, maxHeadYaw), 0f);
                }
            }
            head.localRotation = Quaternion.Slerp(head.localRotation, target, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }
    }
}
