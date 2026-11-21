using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// One place for the app's fonts, sprites and colours. Edit Assets/SmartPlantPot/Data/UISkin.asset
    /// to restyle everything (rebuild the scene to apply colours to static parts).
    /// </summary>
    [CreateAssetMenu(menuName = "Smart Plant Pot/UI Skin", fileName = "UISkin")]
    public class UISkin : ScriptableObject
    {
        public static UISkin Current;

        [Header("Fonts (Prompt - same as the design deck)")]
        public Font font;
        public Font fontBold;

        [Header("Shapes")]
        public Sprite rounded;
        public Sprite circle;
        public Sprite shadow;
        public Sprite glow;
        public Sprite sparkle;
        public Sprite drop;
        public Sprite leaf;

        [Header("Icons")]
        public Sprite iconMoisture;
        public Sprite iconTemperature;
        public Sprite iconLight;
        public Sprite iconBell;
        public Sprite iconScissors;
        public Sprite iconClose;
        public Sprite iconAlert;
        public Sprite iconCheck;
        public Sprite iconSprout;
        public Sprite iconInfo;
        public Sprite iconCare;

        [Header("Game icons (3D shop)")]
        public Sprite iconCoins;
        public Sprite iconStore;
        public Sprite iconSettings;
        public Sprite iconFlame;
        public Sprite iconSparkles;
        public Sprite iconFertilizer;
        public Sprite iconGauge;
        public Sprite iconPlus;
        public Sprite iconTrash;
        public Sprite iconCalendar;
        public Sprite iconClock;
        public Sprite iconWifi;
        public Sprite iconFastForward;
        public Sprite iconMoon;
        public Sprite iconCloud;
        public Sprite iconCloudSun;
        public Sprite iconCloudRain;
        public Sprite iconCloudStorm;
        public Sprite iconCloudFog;
        public Sprite iconStar;
        public Sprite iconPackage;
        public Sprite iconBag;
        public Sprite iconRefresh;
        public Sprite iconSliders;
        public Sprite iconWrench;

        [Header("Palette")]
        public Color background = Hex("EEF4EC");
        public Color floor = Hex("E8DCC6");
        public Color shelf = Hex("B98A5E");
        public Color shelfShadow = Hex("8C6440");
        public Color surface = Color.white;
        public Color infoCard = Hex("E3F9FB");
        public Color textPrimary = Hex("1F3A2E");
        public Color textMuted = Hex("62776D");
        public Color accent = Hex("2E8C85");
        public Color accentDark = Hex("1E6B66");
        public Color statusNormal = Hex("3BA55C");
        public Color statusLow = Hex("F0A030");
        public Color statusHigh = Hex("E5534B");
        public Color water = Hex("4FA9E8");
        public Color leafColor = Hex("5DAA4A");
        public Color gold = Hex("E6A817");
        public Color flame = Hex("FF6B2C");
        public Color eventColor = Hex("8E5BD9");
        public Color panelDim = new Color(0.06f, 0.12f, 0.09f, 0.45f);

        public Color StatusColor(MetricStatus status)
        {
            switch (status)
            {
                case MetricStatus.Low: return statusLow;
                case MetricStatus.High: return statusHigh;
                default: return statusNormal;
            }
        }

        public Sprite MetricIcon(PlantMetric metric)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: return iconMoisture;
                case PlantMetric.Temperature: return iconTemperature;
                default: return iconLight;
            }
        }

        public Sprite WeatherIcon(WeatherKind kind, bool night)
        {
            switch (kind)
            {
                case WeatherKind.Clear: return night ? iconMoon : iconLight;
                case WeatherKind.PartlyCloudy: return night ? iconMoon : iconCloudSun;
                case WeatherKind.Cloudy: return iconCloud;
                case WeatherKind.Fog: return iconCloudFog;
                case WeatherKind.Storm: return iconCloudStorm;
                default: return iconCloudRain;
            }
        }

        public Font GetFont(bool bold)
        {
            if (bold && fontBold != null) return fontBold;
            if (font != null) return font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString("#" + hex, out c) ? c : Color.magenta;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
