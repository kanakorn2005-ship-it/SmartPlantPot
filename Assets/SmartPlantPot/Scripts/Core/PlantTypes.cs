using System;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>The three values every Smart Plant Pot (ESP32) reports.</summary>
    public enum PlantMetric
    {
        Moisture = 0,
        Temperature = 1,
        Light = 2
    }

    /// <summary>Where a sensor value sits compared with the plant's ideal range.</summary>
    public enum MetricStatus
    {
        Normal = 0,
        Low = 1,
        High = 2
    }

    /// <summary>Virtual-care actions the user can trigger on a pot.</summary>
    public enum CareAction
    {
        Water = 0,
        Prune = 1
    }

    /// <summary>Ideal range for one metric. Values outside it raise an alert.</summary>
    [Serializable]
    public struct MetricRange
    {
        public float min;
        public float max;

        public MetricRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(float value)
        {
            return value >= min && value <= max;
        }
    }

    /// <summary>One packet of sensor data from a pot.</summary>
    [Serializable]
    public struct SensorReading
    {
        [Tooltip("Soil moisture in % (0-100)")]
        public float moisture;

        [Tooltip("Air temperature in °C")]
        public float temperature;

        [Tooltip("Light level in lux")]
        public float light;

        public SensorReading(float moisture, float temperature, float light)
        {
            this.moisture = moisture;
            this.temperature = temperature;
            this.light = light;
        }

        public float Get(PlantMetric metric)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: return moisture;
                case PlantMetric.Temperature: return temperature;
                default: return light;
            }
        }

        public void Set(PlantMetric metric, float value)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: moisture = value; break;
                case PlantMetric.Temperature: temperature = value; break;
                default: light = value; break;
            }
        }
    }

    public static class PlantMetrics
    {
        /// <summary>All metrics in display / priority order.</summary>
        public static readonly PlantMetric[] All =
        {
            PlantMetric.Moisture,
            PlantMetric.Temperature,
            PlantMetric.Light
        };
    }
}
