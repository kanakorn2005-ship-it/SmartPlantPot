using System;
using UnityEngine;

namespace SmartPlantPot
{
    public enum ServoAction
    {
        Close = 0,
        Water = 1,
        Fertilize = 2
    }

    /// <summary>
    /// The pot's servo valve (from the deck: "สั่งการรดน้ำหรือให้ปุ๋ยได้").
    /// 0° = closed, 90° = water line open, 180° = fertilizer line open.
    /// Moves at a real servo's speed, closes itself after a pulse, can water automatically when the
    /// soil gets dry, and sends every move to the active data source (ESP32 over HTTP, or the
    /// simulator, which adds moisture while the water line is open).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Smart Plant Pot/Pot Servo")]
    public class PotServo : MonoBehaviour
    {
        public PlantUnit plant;

        [Header("Angles (°)")]
        public float closedAngle = 0f;
        public float waterAngle = 90f;
        public float fertilizerAngle = 180f;
        [Tooltip("Degrees per second (SG90 ≈ 600°/s unloaded; slower looks nicer).")]
        public float speed = 240f;

        [Header("Pulses (seconds)")]
        public float waterSeconds = 3f;
        public float fertilizerSeconds = 2f;

        [Header("Automatic watering")]
        [Tooltip("Open the water line by itself when soil moisture is below the species minimum.")]
        public bool autoWater;
        public float autoCooldownSeconds = 90f;

        public float Angle { get; private set; }
        public float TargetAngle { get; private set; }
        public ServoAction Current { get; private set; }

        /// <summary>Raised whenever the target changes (UI refresh).</summary>
        public event Action<PotServo> Changed;

        /// <summary>Raised when auto-watering kicks in (for a toast).</summary>
        public static event Action<PotServo> AutoWatered;

        float closeAt = float.MaxValue;
        float lastAuto = -999f;

        public bool IsWatering
        {
            get { return Current == ServoAction.Water && Mathf.Abs(Angle - waterAngle) < 15f; }
        }

        public bool IsOpen
        {
            get { return Current != ServoAction.Close; }
        }

        public float SecondsLeft
        {
            get { return closeAt == float.MaxValue ? 0f : Mathf.Max(0f, closeAt - Time.unscaledTime); }
        }

        void Reset()
        {
            plant = GetComponent<PlantUnit>();
        }

        void Awake()
        {
            if (plant == null) plant = GetComponent<PlantUnit>();
            Angle = TargetAngle = closedAngle;
        }

        public float AngleFor(ServoAction a)
        {
            return a == ServoAction.Water ? waterAngle : a == ServoAction.Fertilize ? fertilizerAngle : closedAngle;
        }

        /// <summary>Open a line for its pulse time, then close automatically.</summary>
        public void Pulse(ServoAction action)
        {
            float seconds = action == ServoAction.Fertilize ? fertilizerSeconds : waterSeconds;
            MoveTo(action, AngleFor(action), seconds);
        }

        /// <summary>Hold the servo at an angle until told otherwise (Mock panel slider).</summary>
        public void SetManualAngle(float degrees)
        {
            degrees = Mathf.Clamp(degrees, 0f, 180f);
            ServoAction a = Mathf.Abs(degrees - waterAngle) < 20f ? ServoAction.Water
                : Mathf.Abs(degrees - fertilizerAngle) < 20f ? ServoAction.Fertilize : ServoAction.Close;
            MoveTo(a, degrees, 0f);
        }

        public void Close()
        {
            MoveTo(ServoAction.Close, closedAngle, 0f);
        }

        void MoveTo(ServoAction action, float angle, float seconds)
        {
            Current = action;
            TargetAngle = angle;
            closeAt = seconds > 0f ? Time.unscaledTime + seconds : float.MaxValue;
            if (PlantDataSource.Active != null && plant != null) PlantDataSource.Active.SendServo(plant, angle, seconds);
            if (Changed != null) Changed(this);
        }

        void Update()
        {
            Angle = Mathf.MoveTowards(Angle, TargetAngle, speed * Time.unscaledDeltaTime);

            if (Current != ServoAction.Close && Time.unscaledTime >= closeAt) Close();

            if (autoWater && plant != null && !plant.IsEmpty && Current == ServoAction.Close &&
                plant.GetStatus(PlantMetric.Moisture) == MetricStatus.Low &&
                Time.unscaledTime - lastAuto >= autoCooldownSeconds)
            {
                lastAuto = Time.unscaledTime;
                Pulse(ServoAction.Water);
                var fx = GetComponent<PotInteraction>();
                if (EffectsLayer.Instance != null && fx != null && fx.art != null) EffectsLayer.Instance.PlayWater(fx.art);
                if (AutoWatered != null) AutoWatered(this);
            }
        }

        public static string StateName(ServoAction a)
        {
            switch (a)
            {
                case ServoAction.Water: return L.T("เปิดน้ำ", "Water open");
                case ServoAction.Fertilize: return L.T("เปิดปุ๋ย", "Fertilizer open");
                default: return L.T("ปิด", "Closed");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            AutoWatered = null;
        }
    }
}
