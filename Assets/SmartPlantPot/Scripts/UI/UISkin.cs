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
