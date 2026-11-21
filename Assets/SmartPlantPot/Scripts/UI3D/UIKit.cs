using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Runtime-built controls for the settings / shop panels (slider, switch, text field, section
    /// headers), styled with the UISkin. Everything is created in code so lists can be rebuilt
    /// from data (species, pots) at any time.
    /// </summary>
    public static class UIKit
    {
        static UISkin Skin
        {
            get { return UISkin.Current; }
        }

        public static RectTransform Column(Transform parent, string name, float spacing = 10f, RectOffset padding = null)
        {
            var rt = UIFactory.Rect(parent, name);
            UIFactory.VColumn(rt.gameObject, spacing, padding ?? new RectOffset());
            var f = UIFactory.Fit(rt.gameObject, false, true);
            f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            return rt;
        }

        public static RectTransform Row(Transform parent, string name, float height, float spacing = 10f)
        {
            var rt = UIFactory.Rect(parent, name);
            UIFactory.HRow(rt.gameObject, spacing, new RectOffset(), TextAnchor.MiddleLeft);
            UIFactory.Layout(rt, -1f, height);
            return rt;
        }

        public static Text Heading(Transform parent, string text, int size = 26)
        {
            var t = UIFactory.Text(parent, "Heading", text, size, Skin.textPrimary, TextAnchor.MiddleLeft, true);
            UIFactory.Layout(t, -1f, size + 18f);
            return t;
        }

        public static Text Note(Transform parent, string text, int size = 18)
        {
            var t = UIFactory.Text(parent, "Note", text, size, Skin.textMuted, TextAnchor.UpperLeft);
            return t;
        }

        public static Image Card(Transform parent, string name, float spacing = 10f)
        {
            var bg = UIFactory.Rounded(parent, name, new Color(1f, 1f, 1f, 0.8f), 18f);
            UIFactory.VColumn(bg.gameObject, spacing, new RectOffset(18, 18, 14, 16));
            return bg;
        }

        public static Button SmallButton(Transform parent, string label, Sprite icon, Color bg, Color fg, Action onClick, float height = 46f)
        {
            var b = UIFactory.Button(parent, "Btn_" + label, label, icon, bg, fg, 19, 18f, height);
            UIFactory.Layout(b, -1f, height, 0f, -1f);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static void SetButtonLabel(Button b, string text)
        {
            var t = b.transform.Find("Label");
            if (t != null) t.GetComponent<Text>().text = ThaiShaper.Fix(text);
        }

        /// <summary>Label + live value on one line, slider underneath.</summary>
        public static Slider LabeledSlider(Transform parent, string label, float min, float max, float value, bool whole,
            Func<float, string> format, Action<float> onChange, Color fill)
        {
            Text unused;
            return LabeledSlider(parent, label, min, max, value, whole, format, onChange, fill, out unused);
        }

        public static Slider LabeledSlider(Transform parent, string label, float min, float max, float value, bool whole,
            Func<float, string> format, Action<float> onChange, Color fill, out Text valueText)
        {
            var col = UIFactory.Rect(parent, "Slider_" + label);
            UIFactory.VColumn(col.gameObject, 4f, new RectOffset());
            var top = Row(col, "Top", 28f);
            var lbl = UIFactory.Text(top, "Label", label, 19, Skin.textMuted, TextAnchor.MiddleLeft);
            UIFactory.Layout(lbl, -1f, -1f, 1f, -1f);
            var val = UIFactory.Text(top, "Value", format != null ? format(value) : value.ToString("0.##"), 20, Skin.textPrimary, TextAnchor.MiddleRight, true);
            val.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText = val;
            var s = Slider(col, min, max, value, whole, fill);
            s.onValueChanged.AddListener(v =>
            {
                val.text = ThaiShaper.Fix(format != null ? format(v) : v.ToString("0.##"));
                if (onChange != null) onChange(v);
            });
            return s;
        }

        public static Slider Slider(Transform parent, float min, float max, float value, bool whole, Color fill)
        {
            var root = UIFactory.Rect(parent, "Slider");
            UIFactory.Layout(root, -1f, 32f, 1f, -1f);

            var track = UIFactory.Rounded(root, "Background", UISkin.WithAlpha(Skin.textPrimary, 0.1f), 6f);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, 12f);
            track.rectTransform.anchoredPosition = Vector2.zero;

            var fillArea = UIFactory.Rect(root, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(-16f, 12f);
            fillArea.anchoredPosition = Vector2.zero;
            var fillImg = UIFactory.Rounded(fillArea, "Fill", fill, 6f);
            fillImg.rectTransform.sizeDelta = new Vector2(10f, 0f);

            var handleArea = UIFactory.Rect(root, "Handle Slide Area");
            UIFactory.Stretch(handleArea, 14f, 14f, 0f, 0f);
            var handle = UIFactory.Circle(handleArea, "Handle", Color.white, 30f, true);
            handle.rectTransform.sizeDelta = new Vector2(30f, 0f);
            var ring = UIFactory.Circle(handle.transform, "Dot", fill, 14f);
            UIFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
            var shadow = handle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -2f);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fillImg.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = min;
            s.maxValue = max;
            s.wholeNumbers = whole;
            s.SetValueWithoutNotify(value);
            return s;
        }

        /// <summary>iOS-style switch with a label.</summary>
        public static Toggle Switch(Transform parent, string label, bool value, Action<bool> onChange)
        {
            var row = Row(parent, "Switch_" + label, 44f, 12f);
            var t = UIFactory.Text(row, "Label", label, 20, Skin.textPrimary, TextAnchor.MiddleLeft);
            UIFactory.Layout(t, -1f, -1f, 1f, -1f);

            var bg = UIFactory.Rounded(row, "Track", UISkin.WithAlpha(Skin.textPrimary, 0.18f), 16f, true);
            UIFactory.Layout(bg, 64f, 34f);
            var on = UIFactory.Rounded(bg.transform, "On", Skin.accent, 16f);
            UIFactory.Stretch(on.rectTransform);
            var knob = UIFactory.Circle(bg.transform, "Knob", Color.white, 28f);
            UIFactory.Place(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(17f, 0f), new Vector2(28f, 28f));

            var tg = bg.gameObject.AddComponent<Toggle>();
            tg.targetGraphic = bg;
            tg.graphic = on;
            var view = bg.gameObject.AddComponent<SwitchKnob>();
            view.knob = knob.rectTransform;
            tg.SetIsOnWithoutNotify(value);
            view.Snap(value);
            tg.onValueChanged.AddListener(v =>
            {
                view.Set(v);
                if (onChange != null) onChange(v);
            });
            return tg;
        }

        public static InputField TextField(Transform parent, string label, string value, string placeholder,
            InputField.ContentType type, Action<string> onEnd)
        {
            var col = UIFactory.Rect(parent, "Field_" + label);
            UIFactory.VColumn(col.gameObject, 4f, new RectOffset());
            if (!string.IsNullOrEmpty(label))
            {
                var lbl = UIFactory.Text(col, "Label", label, 18, Skin.textMuted, TextAnchor.MiddleLeft);
                UIFactory.Layout(lbl, -1f, 26f);
            }
            var bg = UIFactory.Rounded(col, "Input", Color.white, 12f, true);
            UIFactory.Layout(bg, -1f, 48f);
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = UISkin.WithAlpha(Skin.textPrimary, 0.15f);

            var text = UIFactory.Text(bg.transform, "Text", "", 20, Skin.textPrimary, TextAnchor.MiddleLeft);
            UIFactory.Stretch(text.rectTransform, 14f, 14f, 4f, 4f);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ph = UIFactory.Text(bg.transform, "Placeholder", placeholder, 20, UISkin.WithAlpha(Skin.textMuted, 0.6f), TextAnchor.MiddleLeft);
            UIFactory.Stretch(ph.rectTransform, 14f, 14f, 4f, 4f);

            var f = bg.gameObject.AddComponent<InputField>();
            f.textComponent = text;
            f.placeholder = ph;
            f.contentType = type;
            f.lineType = InputField.LineType.SingleLine;
            f.SetTextWithoutNotify(value ?? "");
            if (onEnd != null) f.onEndEdit.AddListener(v => onEnd(v));
            return f;
        }

        /// <summary>Button that cycles through a list of options (stands in for a dropdown).</summary>
        public static Button Cycle(Transform parent, string label, string[] options, int index, Action<int> onChange)
        {
            var row = Row(parent, "Cycle_" + label, 48f, 12f);
            var t = UIFactory.Text(row, "Label", label, 20, Skin.textPrimary, TextAnchor.MiddleLeft);
            UIFactory.Layout(t, -1f, -1f, 1f, -1f);
            int current = Mathf.Clamp(index, 0, options.Length - 1);
            var b = UIFactory.Button(row, "Value", options[current], Skin.iconRefresh, UISkin.WithAlpha(Skin.accent, 0.14f), Skin.accentDark, 19, 16f, 42f);
            UIFactory.Layout(b, 260f, 42f, 0f, -1f);
            b.onClick.AddListener(() =>
            {
                current = (current + 1) % options.Length;
                SetButtonLabel(b, options[current]);
                if (onChange != null) onChange(current);
            });
            return b;
        }

        /// <summary>Log-scale lux slider helpers (0..100,000 lux on a 0..1 slider).</summary>
        public static float LuxFromSlider(float t)
        {
            return Mathf.Round(Mathf.Pow(10f, t * 5f) - 1f);
        }

        public static float SliderFromLux(float lux)
        {
            return Mathf.Clamp01(Mathf.Log10(Mathf.Max(0f, lux) + 1f) / 5f);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
