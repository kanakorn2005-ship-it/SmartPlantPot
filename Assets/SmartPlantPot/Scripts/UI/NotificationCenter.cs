using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    public enum AlertKind
    {
        Warning = 0,   // a value went too low / too high
        Recovered = 1, // a value came back into range
        Care = 2,      // the user watered / pruned
        Refused = 3    // a care action was refused (e.g. soil still wet)
    }

    public class PlantAlert
    {
        public PlantUnit plant;
        public PlantMetric metric;
        public MetricStatus status;
        public AlertKind kind;
        public float value;
        public string title;
        public string message;
        public DateTime time;
        public bool isReminder;
        public bool read;
    }

    /// <summary>
    /// Card "ระบบการแจ้งเตือนแต่ละค่าถ้ามากหรือน้อยไป".
    /// Watches every PlantUnit; when moisture / temperature / light leaves the species' ideal range it
    /// raises an alert (toast + bell badge + history + mascot), with:
    /// - hysteresis so values sitting on the limit don't spam (see PlantHealth.Hysteresis)
    /// - a reminder if a problem is still not fixed after <see cref="remindAfterSeconds"/>
    /// - an optional "back to normal" message when it is fixed
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Notification Center")]
    public class NotificationCenter : MonoBehaviour
    {
        public static NotificationCenter Instance { get; private set; }

        [Tooltip("Repeat an alert if the value is still out of range after this many seconds. 0 = never.")]
        public float remindAfterSeconds = 120f;

        [Tooltip("Also tell the user when a value returns to normal.")]
        public bool notifyWhenBackToNormal = true;

        public int maxHistory = 60;

        [Tooltip("Optional sound for warnings.")]
        public AudioSource alertSound;

        public readonly List<PlantAlert> History = new List<PlantAlert>();
        public int UnreadCount { get; private set; }

        public event Action<PlantAlert> AlertRaised;
        public event Action HistoryChanged;

        // When each plant/metric last warned (-1 = not warning). Indexed by (int)PlantMetric.
        readonly Dictionary<PlantUnit, float[]> lastWarningTime = new Dictionary<PlantUnit, float[]>();
        float reminderCheck;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            foreach (var p in PlantUnit.All) Hook(p);
            PlantUnit.Registered += Hook;
            PlantUnit.Unregistered += Unhook;
        }

        void OnDisable()
        {
            PlantUnit.Registered -= Hook;
            PlantUnit.Unregistered -= Unhook;
            foreach (var p in PlantUnit.All) Unhook(p);
        }

        void Hook(PlantUnit p)
        {
            p.StatusChanged -= OnStatusChanged;
            p.StatusChanged += OnStatusChanged;
        }

        void Unhook(PlantUnit p)
        {
            p.StatusChanged -= OnStatusChanged;
        }

        float[] WarningTimes(PlantUnit p)
        {
            float[] times;
            if (!lastWarningTime.TryGetValue(p, out times))
            {
                times = new[] { -1f, -1f, -1f };
                lastWarningTime[p] = times;
            }
            return times;
        }

        void OnStatusChanged(PlantUnit plant, PlantMetric metric, MetricStatus old, MetricStatus now)
        {
            if (now != MetricStatus.Normal)
            {
                Raise(MakeWarning(plant, metric, now, false));
            }
            else
            {
                WarningTimes(plant)[(int)metric] = -1f;
                if (notifyWhenBackToNormal && old != MetricStatus.Normal)
                {
                    Raise(new PlantAlert
                    {
                        plant = plant,
                        metric = metric,
                        status = MetricStatus.Normal,
                        kind = AlertKind.Recovered,
                        value = plant.Reading.Get(metric),
                        title = L.RecoveredTitle(plant.DisplayName, metric),
                        message = L.MetricName(metric) + " " + PlantHealth.Format(metric, plant.Reading.Get(metric)),
                        time = DateTime.Now,
                        read = true
                    });
                }
            }
        }

        PlantAlert MakeWarning(PlantUnit plant, PlantMetric metric, MetricStatus status, bool reminder)
        {
            float v = plant.Reading.Get(metric);
            WarningTimes(plant)[(int)metric] = Time.unscaledTime;
            string msg = L.Advice(metric, status) + "  ·  " + PlantHealth.Format(metric, v);
            if (plant.species != null)
                msg += "  (" + L.IdealRange(PlantHealth.FormatRange(metric, plant.species.GetRange(metric))) + ")";
            return new PlantAlert
            {
                plant = plant,
                metric = metric,
                status = status,
                kind = AlertKind.Warning,
                value = v,
                title = L.AlertTitle(plant.DisplayName, metric, status) + (reminder ? " " + L.Reminder : ""),
                message = msg,
                time = DateTime.Now,
                isReminder = reminder
            };
        }

        void Update()
        {
            if (remindAfterSeconds <= 0f) return;
            reminderCheck += Time.unscaledDeltaTime;
            if (reminderCheck < 1f) return;
            reminderCheck = 0f;

            foreach (var plant in PlantUnit.All)
            {
                foreach (var m in PlantMetrics.All)
                {
                    var s = plant.GetStatus(m);
                    if (s == MetricStatus.Normal) continue;
                    float last = WarningTimes(plant)[(int)m];
                    if (last >= 0f && Time.unscaledTime - last >= remindAfterSeconds)
                    {
                        Raise(MakeWarning(plant, m, s, true));
                    }
                }
            }
        }

        /// <summary>Feedback after Water / Prune (shown as a toast, kept in history, not counted as unread).</summary>
        public void PostCareFeedback(PlantUnit plant, CareAction action, string message, bool succeeded)
        {
            Raise(new PlantAlert
            {
                plant = plant,
                metric = PlantMetric.Moisture,
                status = MetricStatus.Normal,
                kind = succeeded ? AlertKind.Care : AlertKind.Refused,
                title = plant != null ? plant.DisplayName + " · " + (action == CareAction.Water ? L.Water : L.Prune) : L.CareTitle,
                message = message,
                time = DateTime.Now,
                read = true
            });
        }

        void Raise(PlantAlert alert)
        {
            History.Insert(0, alert);
            while (History.Count > maxHistory) History.RemoveAt(History.Count - 1);
            if (!alert.read) UnreadCount++;
            if (alert.kind == AlertKind.Warning && alertSound != null) alertSound.Play();
            if (AlertRaised != null) AlertRaised(alert);
            if (HistoryChanged != null) HistoryChanged();
        }

        public void MarkAllRead()
        {
            foreach (var a in History) a.read = true;
            UnreadCount = 0;
            if (HistoryChanged != null) HistoryChanged();
        }

        public void ClearHistory()
        {
            History.Clear();
            UnreadCount = 0;
            if (HistoryChanged != null) HistoryChanged();
        }
    }
}
