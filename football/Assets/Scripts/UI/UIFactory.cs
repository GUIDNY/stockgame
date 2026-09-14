using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StrikerFive.UI
{
    public static class UIFactory
    {
        public static readonly Color Bg = new Color(0.04f, 0.05f, 0.09f, 0.88f);
        public static readonly Color Accent = new Color(0.35f, 0.95f, 0.5f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
        public static readonly Color TextColor = new Color(0.97f, 0.97f, 0.97f);
        public static readonly Color TextDim = new Color(0.7f, 0.72f, 0.78f);
        public static readonly Color ButtonBg = new Color(0.16f, 0.18f, 0.26f, 1f);
        public static readonly Color ButtonSelected = new Color(0.15f, 0.55f, 0.3f, 1f);

        private static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return _font;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return canvas;
        }

        public static RectTransform Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>(); img.color = color; img.raycastTarget = color.a > 0.5f;
            return rt;
        }

        public static RectTransform Full(Transform parent, string name, Color color) => Panel(parent, name, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = DefaultFont; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = style; t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return t;
        }

        public static Text Shadowed(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var t = Label(parent, text, size, color, anchor, FontStyle.Bold);
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(2, -2);
            return t;
        }

        public static Button Button(Transform parent, string label, UnityEngine.Events.UnityAction onClick, int fontSize = 24, Color? bg = null, float height = 52f)
        {
            var go = new GameObject("Button " + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = bg ?? ButtonBg;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors; colors.highlightedColor = new Color(0.9f, 0.9f, 1f); colors.pressedColor = new Color(0.7f, 0.7f, 0.8f); btn.colors = colors;
            btn.onClick.AddListener(onClick);
            var t = Label(go.transform, label, fontSize, TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            var trt = (RectTransform)t.transform; trt.offsetMin = new Vector2(12, 4); trt.offsetMax = new Vector2(-12, -4);
            var le = go.AddComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height; le.flexibleWidth = 1;
            return btn;
        }

        public static VerticalLayoutGroup VLayout(RectTransform rt, float spacing = 8f, int padding = 10)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(padding, padding, padding, padding);
            v.childForceExpandHeight = false; v.childForceExpandWidth = true; v.childControlHeight = true; v.childControlWidth = true;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(RectTransform rt, float spacing = 8f, int padding = 0)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(padding, padding, padding, padding);
            h.childForceExpandHeight = true; h.childForceExpandWidth = true; h.childControlHeight = true; h.childControlWidth = true;
            return h;
        }

        public static Text Paragraph(Transform parent, string text, int size = 20, Color? color = null, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal, float minHeight = 0f)
        {
            var t = Label(parent, text, size, color ?? TextColor, anchor, style);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1; le.minHeight = minHeight > 0f ? minHeight : size + 8;
            return t;
        }

        public static RectTransform Row(RectTransform parent, float height = 48f)
        {
            var row = Panel(parent, "Row", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            HLayout(row, 8f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height;
            return row;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
