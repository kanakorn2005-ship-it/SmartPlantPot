using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Full-screen overlay that plays the care effects (water drops + splashes, scissors snip +
    /// falling leaves, sparkles). Works for 2D or 3D targets via their on-screen rectangle.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Smart Plant Pot/Effects Layer")]
    public class EffectsLayer : MonoBehaviour
    {
        public static EffectsLayer Instance { get; private set; }

        RectTransform layer;

        void Awake()
        {
            Instance = this;
            layer = (RectTransform)transform;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Rect LocalRectOf(Transform target)
        {
            return ScreenSpace.ToLocal(layer, ScreenSpace.RectOf(target));
        }

        Image Spawn(Sprite sprite, Color color, Vector2 position, float size)
        {
            var img = UIFactory.Image(layer, "fx", sprite, color);
            img.preserveAspect = true;
            UIFactory.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
            return img;
        }

        public void PlayWater(Transform target)
        {
            var skin = UISkin.Current;
            if (skin == null || target == null) return;
            Rect r = LocalRectOf(target);
            float soilY = r.yMin + r.height * 0.47f;
            float dropSize = Mathf.Clamp(r.width * 0.09f, 18f, 48f);

            for (int i = 0; i < 12; i++)
            {
                float x = r.center.x + Random.Range(-0.2f, 0.2f) * r.width;
                float y0 = r.yMax + Random.Range(0.02f, 0.2f) * r.height;
                float size = dropSize * Random.Range(0.75f, 1.1f);
                float duration = 0.42f + Random.Range(0f, 0.12f);
                var img = Spawn(skin.drop, UISkin.WithAlpha(skin.water, 0f), new Vector2(x, y0), size);
                var rt = img.rectTransform;
                UITween.Run(this, duration, p =>
                {
                    rt.anchoredPosition = new Vector2(x, Mathf.Lerp(y0, soilY, UITween.EaseInQuad(p)));
                    float a = p <= 0f ? 0f : (p < 0.85f ? 1f : (1f - p) / 0.15f);
                    img.color = UISkin.WithAlpha(skin.water, a);
                }, () =>
                {
                    Splash(new Vector2(x, soilY), size, skin.water);
                    Destroy(img.gameObject);
                }, i * 0.07f);
            }

            UITween.Run(this, 0.01f, p => { }, () => Sparkles(target, skin.water, 6), 1.25f);
        }

        void Splash(Vector2 position, float size, Color color)
        {
            var skin = UISkin.Current;
            var ring = Spawn(skin.circle, color, position, size);
            ring.preserveAspect = false;
            UITween.Run(this, 0.35f, p =>
            {
                float s = Mathf.Lerp(0.4f, 1.8f, UITween.EaseOutCubic(p)) * size;
                ring.rectTransform.sizeDelta = new Vector2(s, s * 0.4f);
                ring.color = UISkin.WithAlpha(color, 0.55f * (1f - p));
            }, () => Destroy(ring.gameObject));
        }

        public void Sparkles(Transform target, Color color, int count)
        {
            var skin = UISkin.Current;
            if (skin == null || target == null) return;
            Rect r = LocalRectOf(target);
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(r.center.x + Random.Range(-0.45f, 0.45f) * r.width,
                    r.yMin + Random.Range(0.5f, 1.0f) * r.height);
                float size = Mathf.Clamp(r.width * Random.Range(0.08f, 0.14f), 16f, 52f);
                var img = Spawn(skin.sparkle, UISkin.WithAlpha(color, 0f), pos, size);
                var rt = img.rectTransform;
                float spin = Random.Range(-90f, 90f);
                UITween.Run(this, 0.7f, p =>
                {
                    float s = p < 0.4f ? UITween.EaseOutBack(p / 0.4f) : 1f;
                    rt.localScale = new Vector3(s, s, 1f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, spin * p);
                    float a = p <= 0f ? 0f : (p < 0.6f ? 1f : (1f - p) / 0.4f);
                    img.color = UISkin.WithAlpha(Color.Lerp(color, Color.white, 0.35f), a);
                }, () => Destroy(img.gameObject), i * 0.09f);
            }
        }

        public void PlayPrune(Transform target)
        {
            var skin = UISkin.Current;
            if (skin == null || target == null) return;
            Rect r = LocalRectOf(target);

            // Scissors pop in and snip twice.
            var snipPos = new Vector2(r.xMin + r.width * 0.2f, r.yMin + r.height * 0.8f);
            var scissors = Spawn(skin.iconScissors, skin.textPrimary, snipPos, Mathf.Clamp(r.width * 0.24f, 40f, 96f));
            var srt = scissors.rectTransform;
            UITween.Run(this, 0.9f, p =>
            {
                float s = p < 0.2f ? UITween.EaseOutBack(p / 0.2f) : 1f;
                srt.localScale = new Vector3(s, s, 1f);
                srt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 4f) * 22f);
                float a = p < 0.8f ? 1f : (1f - p) / 0.2f;
                scissors.color = UISkin.WithAlpha(skin.textPrimary, a);
            }, () => Destroy(scissors.gameObject));

            // A few leaves flutter down.
            for (int i = 0; i < 5; i++)
            {
                var start = new Vector2(r.center.x + Random.Range(-0.3f, 0.3f) * r.width,
                    r.yMin + Random.Range(0.62f, 0.9f) * r.height);
                float endY = r.yMin + Random.Range(-0.04f, 0.06f) * r.height;
                float size = Mathf.Clamp(r.width * Random.Range(0.1f, 0.15f), 18f, 56f);
                float spin = Random.Range(-220f, 220f);
                float drift = Random.Range(0.06f, 0.12f) * r.width;
                var leaf = Spawn(skin.leaf, UISkin.WithAlpha(skin.leafColor, 0f), start, size);
                var lrt = leaf.rectTransform;
                UITween.Run(this, 1.15f, p =>
                {
                    float y = Mathf.Lerp(start.y, endY, UITween.EaseInQuad(p));
                    float x = start.x + Mathf.Sin(p * Mathf.PI * 3f) * drift;
                    lrt.anchoredPosition = new Vector2(x, y);
                    lrt.localRotation = Quaternion.Euler(0f, 0f, spin * p);
                    float a = p <= 0f ? 0f : (p < 0.7f ? 1f : (1f - p) / 0.3f);
                    leaf.color = UISkin.WithAlpha(skin.leafColor, a);
                }, () => Destroy(leaf.gameObject), 0.25f + i * 0.1f);
            }
        }
    }
}
