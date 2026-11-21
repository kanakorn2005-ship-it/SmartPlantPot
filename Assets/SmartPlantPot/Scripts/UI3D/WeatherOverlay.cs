using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Soft "rain on the shop window" streaks drawn behind the UI, so drizzle / rain / storms are
    /// visible from inside the shop (the 3D rain particles fall outside the window). Storms add a
    /// brief white flash. Purely visual: no raycasts.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Weather Overlay")]
    public class WeatherOverlay : MonoBehaviour
    {
        public Sprite streakSprite;
        [Range(0f, 1f)] public float opacity = 0.22f;
        public int maxStreaks = 70;
        public float fallSpeed = 1700f;
        [Tooltip("Slant of the streaks in degrees.")]
        public float slant = 12f;

        class Streak
        {
            public RectTransform rt;
            public Image img;
            public float speed;
            public bool live;
        }

        readonly List<Streak> streaks = new List<Streak>();
        RectTransform area;
        Image flash;
        float flashAmount;
        float nextFlash = 6f;
        float spawnBudget;

        void Awake()
        {
            area = (RectTransform)transform;
            flash = UIFactory.Image(area, "Flash", null, new Color(1f, 1f, 1f, 0f));
            UIFactory.Stretch(flash.rectTransform);
            flash.raycastTarget = false;
        }

        int TargetCount
        {
            get
            {
                switch (EnvironmentState.Weather)
                {
                    case WeatherKind.Drizzle: return maxStreaks / 4;
                    case WeatherKind.Rain: return maxStreaks * 2 / 3;
                    case WeatherKind.Storm: return maxStreaks;
                    default: return 0;
                }
            }
        }

        Streak Take()
        {
            foreach (var s in streaks)
                if (!s.live) return s;
            var img = UIFactory.Image(area, "Streak", streakSprite, Color.white);
            img.raycastTarget = false;
            var st = new Streak { rt = img.rectTransform, img = img };
            st.rt.anchorMin = st.rt.anchorMax = new Vector2(0f, 0f);
            st.rt.pivot = new Vector2(0.5f, 0.5f);
            st.rt.localRotation = Quaternion.Euler(0f, 0f, slant);
            streaks.Add(st);
            flash.transform.SetAsLastSibling();
            return st;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Rect r = area.rect;
            int target = TargetCount;
            int live = 0;
            foreach (var s in streaks) if (s.live) live++;

            // spawn toward the target density
            if (live < target)
            {
                spawnBudget += dt * target * 1.6f;
                while (spawnBudget >= 1f && live < target)
                {
                    spawnBudget -= 1f;
                    var s = Take();
                    float len = Random.Range(40f, 110f);
                    s.rt.sizeDelta = new Vector2(Random.Range(2.5f, 4.5f), len);
                    s.rt.anchoredPosition = new Vector2(Random.Range(-0.1f, 1.1f) * r.width, r.height + len);
                    s.speed = fallSpeed * Random.Range(0.75f, 1.25f);
                    s.img.color = new Color(0.85f, 0.92f, 1f, opacity * Random.Range(0.5f, 1f));
                    s.live = true;
                    s.rt.gameObject.SetActive(true);
                    live++;
                }
            }
            else spawnBudget = 0f;

            float dx = Mathf.Tan(slant * Mathf.Deg2Rad);
            foreach (var s in streaks)
            {
                if (!s.live) continue;
                var p = s.rt.anchoredPosition;
                p.y -= s.speed * dt;
                p.x += s.speed * dt * dx;
                s.rt.anchoredPosition = p;
                if (p.y < -s.rt.sizeDelta.y)
                {
                    s.live = false;
                    s.rt.gameObject.SetActive(false);
                }
            }

            // storm lightning
            if (EnvironmentState.Weather == WeatherKind.Storm)
            {
                nextFlash -= dt;
                if (nextFlash <= 0f)
                {
                    flashAmount = 1f;
                    nextFlash = Random.Range(5f, 12f);
                }
            }
            flashAmount = Mathf.MoveTowards(flashAmount, 0f, dt * 3.5f);
            flash.color = new Color(1f, 1f, 1f, 0.28f * flashAmount * flashAmount);
        }
    }
}
