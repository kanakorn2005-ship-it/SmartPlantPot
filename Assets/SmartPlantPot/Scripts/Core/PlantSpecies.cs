using System;
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
        [Header("Identity")]
        [Tooltip("Stable key used in save files and configs, e.g. tomato. Never change it after release.")]
        public string id = "";

        [Header("Names")]
        public string nameThai = "ต้นไม้";
        public string nameEnglish = "Plant";

        [Header("2D art (from the 2D team)")]
        public Sprite sprite;

        [Tooltip("Seed packet picture shown in the seed shop.")]
        public Sprite seedSprite;

        [Header("3D (from the 3D team)")]
        [Tooltip("Prefab built by Smart Plant Pot > Build 3D Shop Scene: Root > Growth > Wobble > model.")]
        public GameObject modelPrefab;

        [Tooltip("For custom species: id of the built-in species whose 3D model is reused.")]
        public string modelId = "";

        [Header("Game economy (points)")]
        public int seedPrice = 40;

        [Tooltip("Points for selling the whole plant when fully grown, before quality/streak/mutation bonuses.")]
        public int sellPrice = 120;

        [Tooltip("Points per day from the once-a-day sale (the plant stays).")]
        public int dailyPoints = 15;

        [Tooltip("Minutes of good care from seed to fully grown.")]
        public float growMinutes = 20f;

        [NonSerialized] public bool isCustom;

        [Header("Ideal ranges - outside these the app sends an alert")]
        [Tooltip("Soil moisture % (capacitive sensor, 0-100)")]
        public MetricRange moisture = new MetricRange(40f, 75f);

        [Tooltip("Air temperature °C")]
        public MetricRange temperature = new MetricRange(18f, 32f);

        [Tooltip("Light in lux")]
        public MetricRange light = new MetricRange(200f, 10000f);

        [Header("Simulator start values (used until real sensors are connected)")]
        public SensorReading demoReading = new SensorReading(55f, 27f, 600f);

        public string Key
        {
            get { return string.IsNullOrEmpty(id) ? name : id; }
        }

        public string DisplayName
        {
            get { return L.Thai ? ThaiShaper.Fix(nameThai) : nameEnglish; }
        }

        public void SetRange(PlantMetric metric, MetricRange range)
        {
            switch (metric)
            {
                case PlantMetric.Moisture: moisture = range; break;
                case PlantMetric.Temperature: temperature = range; break;
                default: light = range; break;
            }
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
