using UnityEngine;

namespace PlanetScrappers.Core
{
    /// <summary>
    /// Small version label in the corner so every QA screenshot shows which build it came from.
    /// Also shows the cursor hint while the mouse is released.
    /// </summary>
    public class BuildInfoOverlay : MonoBehaviour
    {
        [SerializeField] bool showFps = true;

        GUIStyle style;
        float smoothedDelta = 1f / 60f;

        void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.05f);
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(11, Screen.height / 70),
                normal = { textColor = new Color(1f, 1f, 1f, 0.75f) }
            };

            string text = GameVersion.Label;
            if (showFps) text += $"  ·  {1f / smoothedDelta:0} fps";
            float h = style.fontSize * 1.8f;
            GUI.Label(new Rect(10, Screen.height - h - 6, 600, h), text, style);

            if (!CursorLock.IsLocked)
            {
                var center = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = style.fontSize + 4 };
                GUI.Label(new Rect(0, Screen.height * 0.5f - 20, Screen.width, 40), "Click to play  ·  Esc releases the mouse", center);
            }
        }
    }
}
