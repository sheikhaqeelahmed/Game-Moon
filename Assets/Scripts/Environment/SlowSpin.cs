using UnityEngine;

namespace PlanetScrappers.Environment
{
    /// <summary>Rotates an object slowly around its own axis. Used for Halcyon in the sky.</summary>
    public class SlowSpin : MonoBehaviour
    {
        [SerializeField] Vector3 degreesPerSecond = new Vector3(0f, 0.6f, 0f);

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
