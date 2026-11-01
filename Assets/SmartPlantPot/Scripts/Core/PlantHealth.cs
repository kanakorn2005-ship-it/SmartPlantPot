using System;
using System.Globalization;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Rules that turn raw sensor values into Low / Normal / High and a 0-1 health score.
    /// </summary>
    public static class PlantHealth
    {
        /// <summary>
        /// How far back inside the range a value must come before an alert clears.
        /// Stops alerts flickering on/off when a sensor hovers right at the limit.
        /// </summary>
        public static float Hysteresis(PlantMetric metric)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: return 2f;      // %
                case PlantMetric.Temperature: return 0.5f; // °C
                default: return 30f;                       // lux
            }
        }

        public static MetricStatus Evaluate(float value, MetricRange range, MetricStatus previous, float hysteresis)
        {
            if (value < range.min) return MetricStatus.Low;
            if (value > range.max) return MetricStatus.High;

            // Inside the range, but keep an existing alert until the value is safely back in.
            if (previous == MetricStatus.Low && value < range.min + hysteresis) return MetricStatus.Low;
            if (previous == MetricStatus.High && value > range.max - hysteresis) return MetricStatus.High;
            return MetricStatus.Normal;
        }

        /// <summary>
        /// Rounds a value to the precision the UI shows (35%, 29.4°C, 520 lux), so the status always
        /// agrees with the number on screen (30.04°C shows "30.0°C" and is not flagged above 30).
        /// </summary>
        public static float RoundForDisplay(PlantMetric metric, float value)
        {
            int digits = metric == PlantMetric.Temperature ? 1 : 0;
            return (float)Math.Round(value, digits, MidpointRounding.AwayFromZero);
        }

        /// <summary>0 (very unhealthy) .. 1 (everything in range).</summary>
        public static float Score(SensorReading reading, PlantSpecies species)
        {
            if (species == null) return 1f;
            float total = 0f;
            foreach (var m in PlantMetrics.All)
            {
                var r = species.GetRange(m);
                float v = reading.Get(m);
                float width = Mathf.Max(0.001f, r.max - r.min);
                float outside = v < r.min ? (r.min - v) / width : v > r.max ? (v - r.max) / width : 0f;
                total += Mathf.Clamp01(1f - outside * 2f);
            }
            return total / PlantMetrics.All.Length;
        }

        /// <summary>Maps a value onto a 0-1 bar. Light uses a log scale because lux spans 0 to 100,000.</summary>
        public static float ToBar01(PlantMetric metric, float value)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: return Mathf.InverseLerp(0f, 100f, value);
                case PlantMetric.Temperature: return Mathf.InverseLerp(0f, 50f, value);
                default: return Mathf.Clamp01(Mathf.Log10(Mathf.Max(0f, value) + 1f) / 5f);
            }
        }

        public static string Format(PlantMetric metric, float value)
        {
            var inv = CultureInfo.InvariantCulture;
            switch (metric)
            {
                case PlantMetric.Moisture: return Mathf.RoundToInt(value).ToString(inv) + "%";
                case PlantMetric.Temperature: return value.ToString("0.0", inv) + "°C";
                default: return Mathf.RoundToInt(value).ToString("N0", inv) + " lux";
            }
        }

        public static string FormatRange(PlantMetric metric, MetricRange range)
        {
            var inv = CultureInfo.InvariantCulture;
            switch (metric)
            {
                case PlantMetric.Moisture:
                    return range.min.ToString("0", inv) + "–" + range.max.ToString("0", inv) + "%";
                case PlantMetric.Temperature:
                    return range.min.ToString("0", inv) + "–" + range.max.ToString("0", inv) + "°C";
                default:
                    return range.min.ToString("N0", inv) + "–" + range.max.ToString("N0", inv) + " lux";
            }
        }
    }
}
