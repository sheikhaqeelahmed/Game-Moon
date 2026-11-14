using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Locks the mouse while playing. Esc releases it, a left click locks it again.
    /// Temporary until the pause menu (Module 4) takes over Esc.
    /// </summary>
    public class CursorLock : MonoBehaviour
    {
        [SerializeField] bool lockOnStart = true;

        public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        void Start()
        {
            if (lockOnStart) Lock();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (IsLocked && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Unlock();
            else if (!IsLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
                Lock();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Unlock();
        }

        public static void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
