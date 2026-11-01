using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Per-species data: names, 2D art and the ideal sensor ranges used for alerts.
    /// Create more via Assets > Create > Smart Plant Pot > Plant Species.
    /// </summary>
    [CreateAssetMenu(menuName = "Smart Plant Pot/Plant Species", fileName = "PlantSpecies")]
    public class PlantSpecies : ScriptableObject
    {
        [Header("Names")]
        public string nameThai = "ต้นไม้";
        public string nameEnglish = "Plant";

        [Header("2D art (from the 2D team)")]
        public Sprite sprite;

        [Header("Ideal ranges - outside these the app sends an alert")]
        [Tooltip("Soil moisture % (capacitive sensor, 0-100)")]
        public MetricRange moisture = new MetricRange(40f, 75f);

        [Tooltip("Air temperature °C")]
        public MetricRange temperature = new MetricRange(18f, 32f);

        [Tooltip("Light in lux")]
        public MetricRange light = new MetricRange(200f, 10000f);

        [Header("Simulator start values (used until real sensors are connected)")]
        public SensorReading demoReading = new SensorReading(55f, 27f, 600f);

        public string DisplayName
        {
            get { return L.Thai ? ThaiShaper.Fix(nameThai) : nameEnglish; }
        }

        public MetricRange GetRange(PlantMetric metric)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: return moisture;
                case PlantMetric.Temperature: return temperature;
                default: return light;
            }
        }
    }
}
