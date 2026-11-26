using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// The 2D team's frame-by-frame mascot animations, popped up above the pot that has the problem:
    /// too hot, too cold, soil too wet, soil too dry. They play when the alert is raised (and on each
    /// reminder) and are separate from the equipped mascot skin. Tap one to open that plant.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Smart Plant Pot/Alert Animation Layer")]
    public class AlertAnimationLayer : MonoBehaviour
    {
        public static AlertAnimationLayer Instance { get; private set; }

        public Camera worldCamera;

        [Header("Frames (played forward and back)")]
        public Sprite[] tooHot;
        public Sprite[] tooCold;
        public Sprite[] tooWet;
        public Sprite[] tooDry;

        [Header("Look")]
        public float framesPerSecond = 7f;
        public float showSeconds = 5.5f;
        [Tooltip("Size of the animation on a 1080-high canvas.")]
        public float size = 240f;
        [Tooltip("Pixels above the top of the pot / plant (leaves room for the name tag).")]
        public float screenLift = 115f;
        [Tooltip("Keep the animation below the header bar (canvas pixels from the top).")]
        public float topMargin = 140f;

        class Popup
        {
            public PlantUnit plant;
            public RectTransform rt;
            public Image image;
            public CanvasGroup group;
            public Sprite[] frames;
            public float start;
            public bool active;
        }

        readonly List<Popup> popups = new List<Popup>();
        NotificationCenter center;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            center = NotificationCenter.Instance;
            if (center != null) center.AlertRaised += OnAlert;
        }

        void OnDestroy()
        {
            if (center != null) center.AlertRaised -= OnAlert;
            if (Instance == this) Instance = null;
        }

        void OnAlert(PlantAlert alert)
        {
            if (alert == null || alert.kind != AlertKind.Warning || alert.plant == null) return;
            Play(alert.plant, alert.metric, alert.status);
        }

        /// <summary>The frames for a problem, or null if there is no animation for it (e.g. light).</summary>
        public Sprite[] FramesFor(PlantMetric metric, MetricStatus status)
        {
            if (metric == PlantMetric.Temperature)
                return status == MetricStatus.High ? tooHot : status == MetricStatus.Low ? tooCold : null;
            if (metric == PlantMetric.Moisture)
                return status == MetricStatus.High ? tooWet : status == MetricStatus.Low ? tooDry : null;
            return null;
        }

        /// <summary>Show the animation for this problem above the pot (the test tab uses this too).</summary>
        public void Play(PlantUnit plant, PlantMetric metric, MetricStatus status)
        {
            var frames = FramesFor(metric, status);
            if (plant == null || frames == null || frames.Length == 0) return;

            Popup p = null;
            foreach (var x in popups)
                if (x.plant == plant) p = x;
            if (p == null)
                foreach (var x in popups)
                    if (!x.active) p = x;
            if (p == null)
            {
                p = Create();
                popups.Add(p);
            }
            p.plant = plant;
            p.frames = frames;
            p.start = Time.unscaledTime;
            p.active = true;
            p.rt.gameObject.SetActive(true);
            p.image.sprite = frames[0];
            p.rt.SetAsLastSibling();
        }

        Popup Create()
        {
            var space = (RectTransform)transform;
            var img = UIFactory.Image(space, "AlertAnim", null, Color.white, true);
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(size, size);
            var group = img.gameObject.AddComponent<CanvasGroup>();
            var popup = new Popup { rt = rt, image = img, group = group };
            var click = img.gameObject.AddComponent<AlertAnimationClick>();
            click.onClick = () =>
            {
                if (popup.plant != null) PlantSelection.Select(popup.plant);
                popup.start = Mathf.Min(popup.start, Time.unscaledTime - showSeconds + 0.3f);   // fade out
            };
            return popup;
        }

        void LateUpdate()
        {
            var space = (RectTransform)transform;
            float now = Time.unscaledTime;
            foreach (var p in popups)
            {
                if (!p.active) continue;
                float t = now - p.start;
                if (t >= showSeconds || p.plant == null || worldCamera == null)
                {
                    p.active = false;
                    p.rt.gameObject.SetActive(false);
                    continue;
                }

                // forward and back: 0 1 2 3 4 3 2 1 0 ...
                int n = p.frames.Length;
                int step = Mathf.FloorToInt(t * framesPerSecond);
                int cycle = Mathf.Max(1, 2 * n - 2);
                int k = step % cycle;
                int index = n <= 1 ? 0 : (k < n ? k : cycle - k);
                p.image.sprite = p.frames[Mathf.Clamp(index, 0, n - 1)];

                // above the pot (and its plant) on screen
                Bounds b;
                Vector3 world = ScreenSpace.WorldBounds(p.plant.transform, out b)
                    ? new Vector3(b.center.x, b.max.y, b.center.z)
                    : p.plant.transform.position + Vector3.up * 0.3f;
                Vector3 sp = worldCamera.WorldToScreenPoint(world);
                bool visible = sp.z > 0f;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(space, sp, null, out local);

                // pop in, gentle bob, fade out
                float pop = Mathf.Clamp01(t / 0.3f);
                float scale = pop < 1f ? EaseOutBack(pop) : 1f;
                float fade = Mathf.Clamp01((showSeconds - t) / 0.5f);
                float bob = Mathf.Sin(t * 3f) * 5f;
                Vector2 at = local + new Vector2(0f, screenLift + bob);
                Rect area = space.rect;
                at.y = Mathf.Min(at.y, area.yMax - topMargin - size);               // not under the header
                at.x = Mathf.Clamp(at.x, area.xMin + size * 0.5f, area.xMax - size * 0.5f);
                p.rt.anchoredPosition = at;
                p.rt.localScale = new Vector3(scale, scale, 1f);
                p.group.alpha = visible ? fade * Mathf.Clamp01(t / 0.15f) : 0f;
                p.group.blocksRaycasts = visible && fade > 0.5f;
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
