using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NatoSquad
{
    /// <summary>UI colors.</summary>
    public static class Pal
    {
        public static readonly Color Bg = new Color(0.05f, 0.08f, 0.13f);
        public static readonly Color Panel = new Color(0.10f, 0.15f, 0.23f, 0.96f);
        public static readonly Color PanelLight = new Color(0.16f, 0.23f, 0.34f, 0.98f);
        public static readonly Color Accent = new Color(0.25f, 0.60f, 1f);
        public static readonly Color AccentDark = new Color(0.12f, 0.32f, 0.62f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.25f);
        public static readonly Color Green = new Color(0.30f, 0.78f, 0.40f);
        public static readonly Color Red = new Color(0.90f, 0.28f, 0.25f);
        public static readonly Color Text = new Color(0.93f, 0.95f, 0.98f);
        public static readonly Color TextDim = new Color(0.62f, 0.68f, 0.78f);
        public static readonly Color Disabled = new Color(0.25f, 0.28f, 0.33f);
    }

    /// <summary>Helpers that build uGUI elements from code (the whole UI is created at runtime).</summary>
    public static class UI
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>Places a rect by anchor point (0..1) with a size and offset in reference pixels (1920x1080).</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            return rt;
        }

        public static Image Img(Transform parent, string name, Color c, Sprite s = null, bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            if (sliced) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Image Box(Transform parent, string name, Color c) => Img(parent, name, c, Art.RoundedBox(), true);

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        public static Shadow AddShadow(Graphic g, float d = 3f)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.6f);
            s.effectDistance = new Vector2(d, -d);
            return s;
        }

        public static Button Button(Transform parent, string label, int fontSize, Color bg, Action onClick, Color? textColor = null)
        {
            var img = Box(parent, "Button " + label, bg);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                if (GameManager.I != null) GameManager.I.Audio.Play(Sfx.Click, 0.7f, 0f);
                onClick?.Invoke();
            });
            var t = Label(img.transform, label, fontSize, textColor ?? Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform, 10, 4, 10, 4);
            return btn;
        }

        public static Text BtnLabel(Button b) => b.GetComponentInChildren<Text>();

        public static void SetInteractable(Button b, bool on)
        {
            b.interactable = on;
            var t = BtnLabel(b);
            if (t != null) t.color = on ? Pal.Text : new Color(0.6f, 0.6f, 0.6f);
        }

        /// <summary>A vertical scroll view; returns the content rect (add rows to it; it sizes itself).</summary>
        public static RectTransform Scroll(Transform parent, string name, float spacing = 12f, int padding = 12)
        {
            var view = Box(parent, name, new Color(0, 0, 0, 0.25f));
            view.raycastTarget = true;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            var mask = view.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", view.transform);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing;
            vl.padding = new RectOffset(padding, padding, padding, padding);
            vl.childControlHeight = true; vl.childControlWidth = true;
            vl.childForceExpandHeight = false; vl.childForceExpandWidth = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            sr.viewport = (RectTransform)view.transform;
            return content;
        }

        /// <summary>A row inside a scroll list with a fixed height.</summary>
        public static RectTransform Row(Transform content, float height, Color bg)
        {
            var img = Box(content, "Row", bg);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return img.rectTransform;
        }

        public static Slider Slider(Transform parent, float value, UnityAction<float> onChange)
        {
            var root = Rect("Slider", parent);
            var s = root.gameObject.AddComponent<Slider>();
            var bg = Box(root, "Bg", new Color(0, 0, 0, 0.45f));
            Stretch(bg.rectTransform, 0, 14, 0, 14);
            var fillArea = Rect("Fill Area", root); Stretch(fillArea, 6, 14, 6, 14);
            var fill = Box(fillArea, "Fill", Pal.Accent); Stretch(fill.rectTransform);
            var handleArea = Rect("Handle Area", root); Stretch(handleArea, 16, 0, 16, 0);
            var handle = Box(handleArea, "Handle", Color.white);
            handle.rectTransform.sizeDelta = new Vector2(36, 0);
            handle.raycastTarget = true;
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0; s.maxValue = 1;
            s.value = value;
            s.onValueChanged.AddListener(onChange);
            return s;
        }

        public static string Gold(long g) => g.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>A row of three stars, the first n filled.</summary>
        public static RectTransform Stars(Transform parent, int n, float size)
        {
            var row = Rect("Stars", parent);
            for (int i = 0; i < 3; i++)
            {
                var s = Img(row, "Star", i < n ? Pal.Gold : new Color(1, 1, 1, 0.18f), Art.Star());
                Place(s.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(size, size), new Vector2((i - 1) * size * 1.1f, 0));
            }
            return row;
        }
    }
}
