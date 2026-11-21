using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Header additions for the 3D shop: points (counts up when you earn), live weather with the
    /// part of day, today's event, and buttons for the seed shop and the settings / Mock panel.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Header Game Chips")]
    public class HeaderGameChips : MonoBehaviour
    {
        public Text pointsText;
        public RectTransform pointsChip;
        public Image weatherIcon;
        public Text weatherText;
        public Button eventChip;
        public Text eventText;
        public Button shopButton;
        public Button settingsButton;
        public SeedShopPanel shop;
        public SettingsPanel settings;
        [Tooltip("Header pieces hidden in portrait so the header fits a phone (clock, connection pill, weather text).")]
        public GameObject[] hideInPortrait;

        float shownPoints = -1f;
        float bump;
        float timer = 99f;

        void Start()
        {
            if (shopButton != null) shopButton.onClick.AddListener(() => { if (shop != null) shop.Open(); });
            if (settingsButton != null) settingsButton.onClick.AddListener(() => { if (settings != null) settings.Open(); });
            if (eventChip != null) eventChip.onClick.AddListener(ShowEvent);
            if (GameManager.Instance != null) GameManager.Instance.Changed += OnPoints;
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.Changed -= OnPoints;
        }

        void OnPoints()
        {
            bump = 1f;
        }

        void ShowEvent()
        {
            var ev = DailyEvents.Instance;
            if (ev != null && NotificationCenter.Instance != null)
                NotificationCenter.Instance.PostInfo(null, L.T("อีเวนต์วันนี้: ", "Today: ") + ev.Title, ev.Description, AlertKind.GameEvent);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && pointsText != null)
            {
                if (shownPoints < 0f) shownPoints = gm.Points;
                shownPoints = Mathf.MoveTowards(shownPoints, gm.Points, Mathf.Max(1f, Mathf.Abs(gm.Points - shownPoints)) * Time.unscaledDeltaTime * 4f);
                pointsText.text = Mathf.RoundToInt(shownPoints).ToString();
            }
            bool portrait = ResponsiveLayout.Instance != null && ResponsiveLayout.Instance.IsPortrait;
            if (hideInPortrait != null)
                foreach (var go in hideInPortrait)
                    if (go != null && go.activeSelf == portrait) go.SetActive(!portrait);
            if (pointsChip != null)
            {
                bump = Mathf.MoveTowards(bump, 0f, Time.unscaledDeltaTime * 3f);
                float s = 1f + 0.15f * Mathf.Sin(bump * Mathf.PI);
                pointsChip.localScale = new Vector3(s, s, 1f);
            }

            timer += Time.unscaledDeltaTime;
            if (timer < 1f) return;
            timer = 0f;

            var skin = UISkin.Current;
            if (skin != null && weatherIcon != null)
            {
                weatherIcon.sprite = skin.WeatherIcon(EnvironmentState.Weather, EnvironmentState.IsNight);
                weatherIcon.color = EnvironmentState.IsNight ? skin.accentDark : skin.gold;
            }
            if (weatherText != null)
            {
                string t = DayNightCycle.PartOfDay(GameClock.HourOfDay) + " · " + WeatherService.Name(EnvironmentState.Weather);
                if (!float.IsNaN(EnvironmentState.OutdoorTemperature))
                    t = Mathf.RoundToInt(EnvironmentState.OutdoorTemperature) + "°C · " + t;
                weatherText.text = ThaiShaper.Fix(t);
            }
            var ev = DailyEvents.Instance;
            if (eventText != null && ev != null) eventText.text = ThaiShaper.Fix(ev.Title);
        }
    }
}
