using System;
using System.IO;
using PlanetScrappers.Player;
using UnityEngine;

namespace PlanetScrappers.Core
{
    [Serializable]
    public class GameSettings
    {
        public int resolutionWidth;
        public int resolutionHeight;
        public int displayMode;          // 0 borderless, 1 exclusive full screen, 2 windowed
        public int quality = -1;         // -1 = project default
        public int frameRateCap = 60;    // 0 = unlimited
        public bool vSync = true;
        public float mouseSensitivity = 1f;
        public bool invertY;
        public bool firstPerson;
        public float masterVolume = 0.8f;
    }

    /// <summary>Loads, applies and saves player settings (settings.json beside the save file).</summary>
    public static class SettingsManager
    {
        public static readonly int[] FrameRateOptions = { 30, 60, 120, 144, 0 };
        public static readonly string[] DisplayModeNames = { "Borderless full screen", "Full screen", "Windowed" };

        static string Path => System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
        public static GameSettings Current { get; private set; } = new GameSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(Path)) Current = JsonUtility.FromJson<GameSettings>(File.ReadAllText(Path)) ?? new GameSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] Using defaults: " + e.Message);
                Current = new GameSettings();
            }
            if (Current.resolutionWidth <= 0)
            {
                Current.resolutionWidth = Screen.currentResolution.width;
                Current.resolutionHeight = Screen.currentResolution.height;
            }
            if (Current.quality < 0) Current.quality = QualitySettings.GetQualityLevel();
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(Current, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] Could not save: " + e.Message);
            }
        }

        /// <summary>Applies display, quality, frame rate, audio and camera settings.</summary>
        public static void Apply(PlayerCamera playerCamera)
        {
            var s = Current;
            var mode = s.displayMode switch
            {
                1 => FullScreenMode.ExclusiveFullScreen,
                2 => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };
            if (!Application.isEditor)
                Screen.SetResolution(s.resolutionWidth, s.resolutionHeight, mode);

            if (s.quality >= 0 && s.quality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(s.quality, true);
            QualitySettings.vSyncCount = s.vSync ? 1 : 0;
            Application.targetFrameRate = s.vSync || s.frameRateCap <= 0 ? -1 : s.frameRateCap;
            AudioManager.SetMasterVolume(s.masterVolume);

            if (playerCamera != null)
            {
                playerCamera.MouseSensitivity = 0.08f * s.mouseSensitivity;
                playerCamera.InvertY = s.invertY;
                playerCamera.SetMode(s.firstPerson ? CameraMode.FirstPerson : CameraMode.ThirdPerson);
            }
        }
    }
}
