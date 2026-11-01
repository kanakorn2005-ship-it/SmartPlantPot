using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Helpers that create uGUI objects from code. Used by the editor scene builder and at runtime
    /// (toasts, notification rows, particles). Everything uses <see cref="UISkin.Current"/>.
    /// </summary>
    public static class UIFactory
    {
        const int UILayer = 5;

        static UISkin Skin
        {
            get { return UISkin.Current; }
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>Rounded rectangle; radius in canvas units.</summary>
        public static Image Rounded(Transform parent, string name, Color color, float radius, bool raycast = false)
        {
            var img = Image(parent, name, Skin != null ? Skin.rounded : null, color, raycast);
            SetRadius(img, radius);
            return img;
        }

        public static void SetRadius(Image img, float radius)
        {
            img.type = UnityEngine.UI.Image.Type.Sliced;
            // ui_rounded.png has a 40px corner on a 44px border.
            img.pixelsPerUnitMultiplier = 40f / Mathf.Max(1f, radius);
        }

        public static Image Circle(Transform parent, string name, Color color, float size, bool raycast = false)
        {
            var img = Image(parent, name, Skin != null ? Skin.circle : null, color, raycast);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var img = Image(parent, name, sprite, color);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>Soft drop shadow placed behind a panel (ignored by layout groups).</summary>
        public static Image Shadow(RectTransform panel, float spread = 26f, float alpha = 0.16f, float yOffset = -8f)
        {
            if (Skin == null || Skin.shadow == null) return null;
            var img = Image(panel, "Shadow", Skin.shadow, new Color(0.05f, 0.15f, 0.1f, alpha));
            img.type = UnityEngine.UI.Image.Type.Sliced;
            Stretch(img.rectTransform, -spread, -spread, -spread - yOffset, -spread + yOffset);
            img.transform.SetAsFirstSibling();
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            return img;
        }

        public static Text Text(Transform parent, string name, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Skin != null ? Skin.GetFont(bold) : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = ThaiShaper.Fix(content);
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static LayoutElement Layout(Component c, float prefWidth = -1f, float prefHeight = -1f,
            float flexWidth = -1f, float flexHeight = -1f)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefWidth;
            le.preferredHeight = prefHeight;
            le.flexibleWidth = flexWidth;
            le.flexibleHeight = flexHeight;
            if (prefWidth >= 0f) le.minWidth = prefWidth;
            if (prefHeight >= 0f) le.minHeight = prefHeight;
            return le;
        }

        public static HorizontalLayoutGroup HRow(GameObject go, float spacing, RectOffset padding,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = go.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup VColumn(GameObject go, float spacing, RectOffset padding,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var g = go.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static ContentSizeFitter Fit(GameObject go, bool horizontal, bool vertical)
        {
            var f = go.AddComponent<ContentSizeFitter>();
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        public static RectOffset Pad(int all)
        {
            return new RectOffset(all, all, all, all);
        }

        public static RectOffset Pad(int horizontal, int vertical)
        {
            return new RectOffset(horizontal, horizontal, vertical, vertical);
        }

        /// <summary>Pill / rounded button with optional icon and label, laid out in a row.</summary>
        public static Button Button(Transform parent, string name, string label, Sprite icon, Color background,
            Color foreground, int fontSize, float radius, float height)
        {
            var img = Rounded(parent, name, background, radius, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            HRow(img.gameObject, 10f, Pad(22, 0), TextAnchor.MiddleCenter);
            Layout(img, -1f, height, 1f, -1f);
            if (icon != null)
            {
                var ic = Icon(img.transform, "Icon", icon, foreground, fontSize + 8f);
                Layout(ic, fontSize + 8f, fontSize + 8f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(img.transform, "Label", label, fontSize, foreground, TextAnchor.MiddleCenter, true);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            return btn;
        }

        /// <summary>Round icon-only button.</summary>
        public static Button IconButton(Transform parent, string name, Sprite icon, Color background, Color foreground, float size)
        {
            var bg = Circle(parent, name, background, size, true);
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var ic = Icon(bg.transform, "Icon", icon, foreground, size * 0.5f);
            Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
            return btn;
        }
    }

    /// <summary>Screen-space helpers that work for UI elements and 3D objects alike.</summary>
    public static class ScreenSpace
    {
        static readonly Vector3[] Corners = new Vector3[4];

        /// <summary>Screen-pixel rectangle covering a UI element or a 3D object's renderers.</summary>
        public static Rect RectOf(Transform target)
        {
            var rt = target as RectTransform;
            if (rt != null)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                Camera cam = null;
                if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    cam = canvas.rootCanvas.worldCamera;
                rt.GetWorldCorners(Corners);
                Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, Corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, Corners[2]);
                return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            }

            var main = Camera.main;
            var rend = target.GetComponentInChildren<Renderer>();
            if (main == null) return new Rect(0f, 0f, 100f, 100f);
            if (rend == null)
            {
                Vector3 p = main.WorldToScreenPoint(target.position);
                return new Rect(p.x - 60f, p.y - 60f, 120f, 120f);
            }

            var bounds = rend.bounds;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 s = main.WorldToScreenPoint(c);
                min = Vector2.Min(min, s);
                max = Vector2.Max(max, s);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>Converts a screen rect into the local space of a RectTransform (overlay canvas).</summary>
        public static Rect ToLocal(RectTransform space, Rect screenRect)
        {
            var canvas = space.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.rootCanvas.worldCamera;
            Vector2 a, b;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screenRect.min, cam, out a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screenRect.max, cam, out b);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
    }
}
