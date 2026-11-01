using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// One smart pot in the scene (the digital twin of one ESP32 pot).
    /// Put it on the 2D plant slot today, or on the 3D plant model later - nothing else changes.
    /// Data sources call <see cref="ApplyReading"/>; UI listens to the events.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Smart Plant Pot/Plant Unit")]
    public class PlantUnit : MonoBehaviour
    {
        public static readonly List<PlantUnit> All = new List<PlantUnit>();
        public static event Action<PlantUnit> Registered;
        public static event Action<PlantUnit> Unregistered;

        [Tooltip("ID of the physical pot. Must match the id the backend / ESP32 sends.")]
        public string potId = "pot-01";

        public PlantSpecies species;

        /// <summary>Fired after every new sensor packet.</summary>
        public event Action<PlantUnit> ReadingChanged;

        /// <summary>Fired when a metric moves between Normal / Low / High. Args: plant, metric, old, new.</summary>
        public event Action<PlantUnit, PlantMetric, MetricStatus, MetricStatus> StatusChanged;

        readonly MetricStatus[] statuses = new MetricStatus[3];

        public SensorReading Reading { get; private set; }
        public bool HasReading { get; private set; }
        public DateTime LastUpdate { get; private set; }

        public string DisplayName
        {
            get { return species != null ? species.DisplayName : name; }
        }

        public MetricStatus GetStatus(PlantMetric metric)
        {
            return statuses[(int)metric];
        }

        public bool IsHealthy
        {
            get
            {
                foreach (var s in statuses)
                    if (s != MetricStatus.Normal) return false;
                return true;
            }
        }

        public int IssueCount
        {
            get
            {
                int n = 0;
                foreach (var s in statuses)
                    if (s != MetricStatus.Normal) n++;
                return n;
            }
        }

        public float HealthScore
        {
            get { return HasReading ? PlantHealth.Score(Reading, species) : 1f; }
        }

        /// <summary>The most important problem (moisture first, then temperature, then light).</summary>
        public bool TryGetMainIssue(out PlantMetric metric, out MetricStatus status)
        {
            foreach (var m in PlantMetrics.All)
            {
                if (statuses[(int)m] != MetricStatus.Normal)
                {
                    metric = m;
                    status = statuses[(int)m];
                    return true;
                }
            }
            metric = PlantMetric.Moisture;
            status = MetricStatus.Normal;
            return false;
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (Registered != null) Registered(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            if (Unregistered != null) Unregistered(this);
        }

        public void ApplyReading(SensorReading reading)
        {
            Reading = reading;
            HasReading = true;
            LastUpdate = DateTime.Now;

            List<PlantMetric> changed = null;
            MetricStatus[] previous = null;
            if (species != null)
            {
                foreach (var m in PlantMetrics.All)
                {
                    int i = (int)m;
                    var old = statuses[i];
                    float shown = PlantHealth.RoundForDisplay(m, reading.Get(m));
                    var now = PlantHealth.Evaluate(shown, species.GetRange(m), old, PlantHealth.Hysteresis(m));
                    if (now == old) continue;
                    if (changed == null)
                    {
                        changed = new List<PlantMetric>();
                        previous = (MetricStatus[])statuses.Clone();
                    }
                    statuses[i] = now;
                    changed.Add(m);
                }
            }

            if (ReadingChanged != null) ReadingChanged(this);

            if (changed != null && StatusChanged != null)
            {
                foreach (var m in changed)
                    StatusChanged(this, m, previous[(int)m], statuses[(int)m]);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            Registered = null;
            Unregistered = null;
        }
    }
}
