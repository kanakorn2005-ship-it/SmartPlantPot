using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Fake ESP32 packets so the UI can be built and demoed before the hardware is connected.
    /// Soil dries over time, temperature and light drift, and watering raises moisture.
    /// Tick "manual" on a pot in the Inspector (Play mode) to drag its values by hand and test alerts.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Simulated Plant Data Source")]
    public class SimulatedPlantDataSource : PlantDataSource
    {
        [Serializable]
        public class PotSim
        {
            public PlantUnit plant;

            [Tooltip("Tick to set this pot's values yourself with the sliders (Play mode).")]
            public bool manual;

            [Range(0f, 100f)] public float moisture = 50f;
            [Range(-5f, 50f)] public float temperature = 27f;
            [Range(0f, 5000f)] public float light = 500f;

            [NonSerialized] public float pendingWater;
            [NonSerialized] public float seed;
            [NonSerialized] public float baseTemperature;
            [NonSerialized] public float baseLight;
            [NonSerialized] public bool initialised;
        }

        [Tooltip("Seconds between sensor packets (the ESP32 send rate).")]
        public float sendInterval = 1f;

        [Tooltip("How fast soil dries, % per minute.")]
        public float dryingPerMinute = 1.5f;

        [Tooltip("How much one watering raises soil moisture, %.")]
        public float waterAmount = 25f;

        [Tooltip("± °C the temperature drifts around the start value.")]
        public float temperatureSwing = 3.5f;

        [Tooltip("± fraction the light drifts around the start value (0.35 = ±35%).")]
        public float lightSwing = 0.35f;

        [Tooltip("Speed up the simulated world for demos.")]
        public float timeScale = 1f;

        public List<PotSim> pots = new List<PotSim>();

        float timer;
        float simTime;

        public override bool IsConnected { get { return true; } }
        public override string ConnectionLabel { get { return L.SimulatedData; } }

        [Tooltip("Delay before the first packet, so every UI listener is ready and the scene is visible first.")]
        public float firstPacketDelay = 1.2f;

        void Start()
        {
            foreach (var p in PlantUnit.All)
                GetSim(p);
            timer = sendInterval - firstPacketDelay;
        }

        void Update()
        {
            float dt = Time.deltaTime * timeScale;
            simTime += dt;

            foreach (var plant in PlantUnit.All)
            {
                var s = GetSim(plant);

                // Water soaks in over a couple of seconds instead of jumping.
                if (s.pendingWater > 0f)
                {
                    float add = Mathf.Min(s.pendingWater, 12f * Time.deltaTime);
                    s.pendingWater -= add;
                    s.moisture = Mathf.Min(100f, s.moisture + add);
                }

                if (s.manual) continue;

                s.moisture = Mathf.Max(0f, s.moisture - dryingPerMinute / 60f * dt);

                float wave = Mathf.Sin(simTime * (Mathf.PI * 2f / 180f) + s.seed * 6.283f);
                float jitter = (Mathf.PerlinNoise(simTime * 0.05f, s.seed * 10f) - 0.5f) * 0.6f;
                s.temperature = s.baseTemperature + wave * temperatureSwing + jitter;

                float l = Mathf.PerlinNoise(simTime * 0.04f + 100f, s.seed * 7f) * 2f - 1f;
                s.light = Mathf.Max(0f, s.baseLight * (1f + l * lightSwing));
            }

            timer += Time.deltaTime;
            if (timer >= sendInterval)
            {
                timer = 0f;
                SendAll();
            }
        }

        void SendAll()
        {
            foreach (var plant in PlantUnit.All)
            {
                var s = GetSim(plant);
                plant.ApplyReading(new SensorReading(s.moisture, s.temperature, s.light));
            }
        }

        PotSim GetSim(PlantUnit plant)
        {
            PotSim sim = null;
            foreach (var p in pots)
            {
                if (p.plant == plant)
                {
                    sim = p;
                    break;
                }
            }
            if (sim == null)
            {
                sim = new PotSim { plant = plant };
                if (plant.species != null)
                {
                    sim.moisture = plant.species.demoReading.moisture;
                    sim.temperature = plant.species.demoReading.temperature;
                    sim.light = plant.species.demoReading.light;
                }
                pots.Add(sim);
            }
            if (!sim.initialised)
            {
                sim.initialised = true;
                sim.seed = UnityEngine.Random.value;
                sim.baseTemperature = sim.temperature;
                sim.baseLight = Mathf.Max(1f, sim.light);
            }
            return sim;
        }

        public override void SendCareCommand(PlantUnit plant, CareAction action)
        {
            if (plant == null) return;
            if (action == CareAction.Water)
                GetSim(plant).pendingWater += waterAmount;
            // Pruning has no sensor effect; the visual is handled by PotInteraction.
        }

        [ContextMenu("Dry out every pot (test moisture alerts)")]
        void DryAll()
        {
            foreach (var plant in PlantUnit.All) GetSim(plant).moisture = 15f;
        }

        [ContextMenu("Heat wave (test temperature alerts)")]
        void HeatWave()
        {
            foreach (var plant in PlantUnit.All) GetSim(plant).baseTemperature = 36f;
        }

        [ContextMenu("Cloudy day (test light alerts)")]
        void Cloudy()
        {
            foreach (var plant in PlantUnit.All) GetSim(plant).baseLight = 120f;
        }

        [ContextMenu("Reset to species start values")]
        void ResetValues()
        {
            foreach (var p in pots)
            {
                if (p.plant == null || p.plant.species == null) continue;
                p.moisture = p.plant.species.demoReading.moisture;
                p.temperature = p.baseTemperature = p.plant.species.demoReading.temperature;
                p.light = p.baseLight = p.plant.species.demoReading.light;
            }
        }
    }
}
