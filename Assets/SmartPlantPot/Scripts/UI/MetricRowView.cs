using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// One line on the info card: icon, name, value, Low/Normal/High chip, and a bar showing the
    /// current value against the species' ideal range (green band).
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Metric Row View")]
    public class MetricRowView : MonoBehaviour
    {
        public PlantMetric metric;
        public Image iconBackground;
        public Image icon;
        public Text label;
        public Text value;
        public Image statusChip;
        public Text statusText;
        public RectTransform rangeBand;
        public Image rangeBandImage;
        public RectTransform marker;
        public Image markerImage;
        public Text rangeText;

        float markerX = -1f;
        float markerTarget;

        public void Set(SensorReading reading, PlantSpecies species, MetricStatus status)
        {
            var skin = UISkin.Current;
            if (skin == null) return;
            float v = reading.Get(metric);
            Color sc = skin.StatusColor(status);

            if (icon != null) icon.sprite = skin.MetricIcon(metric);
            if (iconBackground != null) iconBackground.color = sc;
            if (label != null) label.text = L.MetricName(metric);
            if (value != null)
            {
                value.text = PlantHealth.Format(metric, v);
                value.color = status == MetricStatus.Normal ? skin.textPrimary : sc;
            }
            if (statusChip != null) statusChip.color = UISkin.WithAlpha(sc, 0.16f);
            if (statusText != null)
            {
                statusText.text = L.StatusName(status);
                statusText.color = sc;
            }

            if (species != null)
            {
                var range = species.GetRange(metric);
                if (rangeBand != null)
                {
                    rangeBand.anchorMin = new Vector2(PlantHealth.ToBar01(metric, range.min), 0f);
                    rangeBand.anchorMax = new Vector2(PlantHealth.ToBar01(metric, range.max), 1f);
                    rangeBand.offsetMin = Vector2.zero;
                    rangeBand.offsetMax = Vector2.zero;
                }
                if (rangeBandImage != null) rangeBandImage.color = UISkin.WithAlpha(skin.statusNormal, 0.35f);
                if (rangeText != null) rangeText.text = L.IdealRange(PlantHealth.FormatRange(metric, range));
            }

            if (markerImage != null) markerImage.color = sc;
            markerTarget = PlantHealth.ToBar01(metric, v);
            if (markerX < 0f) SetMarker(markerTarget);
        }

        void Update()
        {
            if (marker == null || markerX < 0f) return;
            if (Mathf.Abs(markerX - markerTarget) > 0.0005f)
                SetMarker(Mathf.Lerp(markerX, markerTarget, Mathf.Min(1f, Time.unscaledDeltaTime * 8f)));
        }

        void SetMarker(float x)
        {
            markerX = x;
            if (marker == null) return;
            marker.anchorMin = new Vector2(x, 0.5f);
            marker.anchorMax = new Vector2(x, 0.5f);
            marker.anchoredPosition = Vector2.zero;
        }

        /// <summary>Call when switching plants so the marker jumps instead of sliding.</summary>
        public void SnapNext()
        {
            markerX = -1f;
        }
    }
}
