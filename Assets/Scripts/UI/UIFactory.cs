using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echobound.UI
{
    /// <summary>Helpers to build uGUI at runtime so no prefabs or scene assets are required for the vertical slice.</summary>
    public static class UIFactory
    {
        public static readonly Color Bg = new Color(0.06f, 0.07f, 0.09f, 0.94f);
        public static readonly Color BgSoft = new Color(0.1f, 0.11f, 0.14f, 0.9f);
        public static readonly Color Accent = new Color(0.27f, 0.88f, 0.57f);
        public static readonly Color AccentRed = new Color(1f, 0.55f, 0.5f);
        public static readonly Color AccentBlue = new Color(0.76f, 0.76f, 1f);
        public static readonly Color TextColor = new Color(0.92f, 0.92f, 0.9f);
        public static readonly Color TextDim = new Color(0.65f, 0.66f, 0.68f);
        public static readonly Color ButtonBg = new Color(0.16f, 0.18f, 0.22f, 1f);

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

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
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
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            t.supportRichText = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityEngine.Events.UnityAction onClick, int fontSize = 22, Color? bg = null, float height = 44f)
        {
            var go = new GameObject("Button " + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = bg ?? ButtonBg;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors; colors.highlightedColor = new Color(0.25f, 0.3f, 0.36f); colors.pressedColor = Accent * 0.6f; colors.disabledColor = new Color(0.12f, 0.12f, 0.14f, 0.6f); btn.colors = colors;
            btn.onClick.AddListener(onClick);
            var t = Label(go.transform, label, fontSize, TextColor, TextAnchor.MiddleCenter);
            var trt = (RectTransform)t.transform; trt.offsetMin = new Vector2(10, 4); trt.offsetMax = new Vector2(-10, -4);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var le = go.AddComponent<LayoutElement>(); le.minHeight = height; le.preferredHeight = height; le.flexibleWidth = 1;
            return btn;
        }

        public static InputField Input(Transform parent, string placeholder, int fontSize = 22)
        {
            var go = new GameObject("Input", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.12f, 0.13f, 0.16f, 1f);
            var field = go.AddComponent<InputField>();
            var text = Label(go.transform, "", fontSize, TextColor, TextAnchor.MiddleLeft);
            var trt = (RectTransform)text.transform; trt.offsetMin = new Vector2(12, 4); trt.offsetMax = new Vector2(-12, -4);
            text.supportRichText = false;
            var ph = Label(go.transform, placeholder, fontSize, TextDim, TextAnchor.MiddleLeft, FontStyle.Italic);
            var prt = (RectTransform)ph.transform; prt.offsetMin = new Vector2(12, 4); prt.offsetMax = new Vector2(-12, -4);
            field.textComponent = text; field.placeholder = ph; field.lineType = InputField.LineType.SingleLine; field.characterLimit = 200;
            var le = go.AddComponent<LayoutElement>(); le.minHeight = 44; le.preferredHeight = 44; le.flexibleWidth = 1;
            return field;
        }

        public static Image FillBar(Transform parent, Color color, Color back)
        {
            var bg = new GameObject("Bar", typeof(RectTransform)); bg.transform.SetParent(parent, false);
            var bgImg = bg.AddComponent<Image>(); bgImg.color = back;
            var rt = (RectTransform)bg.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform)); fill.transform.SetParent(bg.transform, false);
            var fillImg = fill.AddComponent<Image>(); fillImg.color = color;
            var frt = (RectTransform)fill.transform; frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1, 1); frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero; frt.pivot = new Vector2(0, 0.5f);
            return fillImg;
        }

        public static void SetFill(Image fill, float fraction)
        {
            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        public static VerticalLayoutGroup VLayout(RectTransform rt, float spacing = 6f, int padding = 8, bool childForceExpandHeight = false)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(padding, padding, padding, padding);
            v.childForceExpandHeight = childForceExpandHeight; v.childForceExpandWidth = true; v.childControlHeight = true; v.childControlWidth = true;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(RectTransform rt, float spacing = 6f, int padding = 0)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(padding, padding, padding, padding);
            h.childForceExpandHeight = true; h.childForceExpandWidth = true; h.childControlHeight = true; h.childControlWidth = true;
            return h;
        }

        /// <summary>Vertical scroll view. Returns the content transform to add children to.</summary>
        public static RectTransform ScrollView(Transform parent, string name, out ScrollRect scroll)
        {
            var root = Panel(parent, name, new Color(0, 0, 0, 0.25f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Panel(root, "Viewport", new Color(1, 1, 1, 0.01f), Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            VLayout(content, 6f, 10);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        public static Text Paragraph(Transform parent, string text, int size = 20, Color? color = null, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var t = Label(parent, text, size, color ?? TextColor, anchor, style);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1; le.minHeight = size + 6;
            return t;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
