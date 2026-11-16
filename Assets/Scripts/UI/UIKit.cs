using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PlanetScrappers.Core;

namespace PlanetScrappers.UI
{
    /// <summary>
    /// Small helper for building the light, frosted-panel UI from code, following the approved
    /// screen designs: white panels, dark text, orange actions, rounded corners.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.98f, 0.985f, 0.99f, 0.94f);
        public static readonly Color Ink = Hex("#1A2027");
        public static readonly Color Muted = Hex("#5B6772");
        public static readonly Color Accent = Hex("#F07A2E");
        public static readonly Color AccentText = Hex("#B9480F");
        public static readonly Color Amber = Hex("#8A5F00");
        public static readonly Color Cyan = Hex("#0A6FA8");
        public static readonly Color Green = Hex("#1E7A34");
        public static readonly Color Teal = Hex("#0F6E68");
        public static readonly Color Line = new Color(0.1f, 0.12f, 0.15f, 0.14f);
        public static readonly Color Track = new Color(0.1f, 0.12f, 0.15f, 0.12f);
        public static readonly Color Veil = new Color(0.93f, 0.945f, 0.96f, 0.45f);
        public static readonly Color ButtonNeutral = new Color(1f, 1f, 1f, 0.75f);

        public static Font Font { get; private set; }
        static Sprite rounded;

        public static void Init()
        {
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (rounded == null) rounded = MakeRounded(48, 12);
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool roundedCorners = true)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (roundedCorners)
            {
                img.sprite = rounded;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Image Outline(Image panel, Color border)
        {
            var o = panel.gameObject.AddComponent<Outline>();
            o.effectColor = border;
            o.effectDistance = new Vector2(1.5f, -1.5f);
            return panel;
        }

        public static Text Label(Transform parent, string text, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rt = Node("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            return t;
        }

        public static Button Button(Transform parent, string text, UnityAction onClick, bool primary, int fontSize = 22, float height = 52f, bool enabled = true)
        {
            var img = Panel(parent, "Button " + text, primary ? Accent : ButtonNeutral);
            if (!primary) Outline(img, Line);
            var button = img.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = primary ? new Color(1f, 0.92f, 0.86f) : new Color(1f, 0.9f, 0.82f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;
            button.interactable = enabled;
            if (onClick != null)
            {
                button.onClick.AddListener(() => AudioManager.Play("ui_click", 0.6f));
                button.onClick.AddListener(onClick);
            }
            var label = Label(img.transform, text, fontSize, primary ? Hex("#14100C") : Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch(6f);
            Size(img.gameObject, -1, height, -1, height);
            return button;
        }

        public static Image Bar(Transform parent, Color fill, float height, out Image fillImage)
        {
            var track = Panel(parent, "Bar", Track);
            Size(track.gameObject, -1, height, -1, height);
            fillImage = Panel(track.transform, "Fill", fill);
            fillImage.rectTransform.Stretch();
            return track;
        }

        /// <summary>Sets a bar's fill (0..1) by stretching the fill image.</summary>
        public static void SetFill(Image fill, float fraction)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fill.enabled = fraction > 0.001f;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing, int padding, TextAnchor align = TextAnchor.UpperLeft)
        {
            var l = go.AddComponent<VerticalLayoutGroup>();
            l.spacing = spacing;
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.childAlignment = align;
            l.childControlWidth = true;
            l.childControlHeight = true;
            l.childForceExpandWidth = true;
            l.childForceExpandHeight = false;
            return l;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing, int padding, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var l = go.AddComponent<HorizontalLayoutGroup>();
            l.spacing = spacing;
            l.padding = new RectOffset(padding, padding, padding, padding);
            l.childAlignment = align;
            l.childControlWidth = true;
            l.childControlHeight = true;
            l.childForceExpandWidth = false;
            l.childForceExpandHeight = false;
            return l;
        }

        public static LayoutElement Size(GameObject go, float minW, float minH, float prefW, float prefH, float flexW = -1f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            return le;
        }

        /// <summary>A horizontal row container inside a vertical layout.</summary>
        public static RectTransform Row(Transform parent, float spacing = 12f, float height = -1f)
        {
            var rt = Node("Row", parent);
            HLayout(rt.gameObject, spacing, 0);
            if (height > 0) Size(rt.gameObject, -1, height, -1, height);
            return rt;
        }

        public static Image Swatch(Transform parent, Color color, float size = 22f)
        {
            var s = Panel(parent, "Swatch", color);
            Size(s.gameObject, size, size, size, size);
            return s;
        }

        public static Text Flex(this Text t, float flexW = 1f)
        {
            Size(t.gameObject, -1, -1, -1, -1, flexW);
            return t;
        }

        public static Text Width(this Text t, float width)
        {
            Size(t.gameObject, width, -1, width, -1, 0f);
            return t;
        }
    }
}
