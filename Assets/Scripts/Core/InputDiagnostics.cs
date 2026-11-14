using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Writes the connected input devices to the player log at start-up, so QA logs show
    /// which controllers were present. Launch with -inputdebug to also log look input
    /// several times a second.
    /// </summary>
    public class InputDiagnostics : MonoBehaviour
    {
        bool verbose;
        float nextLog;

        void Start()
        {
            verbose = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-inputdebug");
            foreach (var device in InputSystem.devices)
                Debug.Log($"[Input] Device: {device.displayName} ({device.layout}) {device.description.product}");
            InputSystem.onDeviceChange += (device, change) => Debug.Log($"[Input] {change}: {device.displayName} ({device.layout})");
        }

        void Update()
        {
            if (!verbose || Time.unscaledTime < nextLog) return;
            nextLog = Time.unscaledTime + 0.25f;
            var mouse = Mouse.current;
            var pad = Gamepad.current;
            Debug.Log($"[Input] t={Time.time:0.00} mouseDelta={(mouse != null ? mouse.delta.ReadValue() : Vector2.zero)} " +
                      $"rightStick={(pad != null ? pad.rightStick.ReadValue() : Vector2.zero)} pad={(pad != null ? pad.displayName : "none")} " +
                      $"locked={CursorLock.IsLocked} focus={Application.isFocused}");
        }
    }
}
