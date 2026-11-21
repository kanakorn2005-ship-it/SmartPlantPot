using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Card "Interaction กับกระถางต้นไม้": makes the pot feel alive and handles virtual care.
    /// - Hover grows the pot, press squashes it, release springs back
    /// - Idle sway; a thirsty plant droops and leans; a watered plant sways happily
    /// - Water / Prune play effects, send the command to the data source, and refuse
    ///   watering (with a head-shake) when the soil is already wet
    /// Works on a 2D RectTransform or a 3D model (set "art" to the model, pivot at the pot base).
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Pot Interaction")]
    public class PotInteraction : MonoBehaviour
    {
        public PlantUnit plant;

        [Tooltip("The visual that squashes and sways. Its pivot should be at the base of the pot.")]
        public Transform art;

        [Header("Feel")]
        public float hoverScale = 1.05f;
        public float selectedScale = 1.03f;
        public Vector2 pressScale = new Vector2(1.07f, 0.92f);
        public bool sway = true;
        public float swayDegrees = 1.2f;
        public float swaySpeed = 1.4f;
        public float stiffness = 280f;
        public float damping = 16f;

        [Header("Care")]
        public float waterCooldown = 1.8f;
        public float pruneCooldown = 1.2f;
        [Tooltip("Refuse watering when moisture is within this many % of the species' maximum.")]
        public float wetMargin = 2f;

        Vector2 scale = Vector2.one;
        Vector2 velocity;
        Vector3 baseScale = Vector3.one;
        Quaternion baseRotation = Quaternion.identity;
        bool hover;
        bool pressed;
        float happyUntil;
        float busyUntil;
        float shakeStart = -10f;
        float phase;
        float lean;

        public bool IsBusy
        {
            get { return Time.unscaledTime < busyUntil; }
        }

        void Awake()
        {
            if (plant == null) plant = GetComponent<PlantUnit>();
            if (art == null) art = transform;
            baseScale = art.localScale;
            baseRotation = art.localRotation;
            phase = Random.value * 10f;
        }

        /// <summary>Point the wobble at a different visual (e.g. the 3D plant after planting a seed).</summary>
        public void SetArt(Transform newArt)
        {
            if (art != null && art != newArt)
            {
                art.localScale = baseScale;
                art.localRotation = baseRotation;
            }
            art = newArt != null ? newArt : transform;
            baseScale = art.localScale;
            baseRotation = art.localRotation;
            scale = Vector2.one;
            velocity = Vector2.zero;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Time.unscaledTime;
            bool selected = plant != null && PlantSelection.Current == plant;
            bool thirsty = plant != null && plant.GetStatus(PlantMetric.Moisture) == MetricStatus.Low;

            Vector2 target = pressed ? pressScale : (hover ? Vector2.one * hoverScale : Vector2.one);
            if (selected && !pressed) target *= selectedScale;
            if (thirsty) target.y *= 0.97f;

            // Damped spring gives a squash-and-stretch wobble for free.
            Vector2 force = (target - scale) * stiffness - velocity * damping;
            velocity += force * dt;
            scale += velocity * dt;
            if (art == null) return;
            art.localScale = new Vector3(baseScale.x * scale.x, baseScale.y * scale.y, baseScale.z * scale.x);

            float angle = 0f;
            if (sway)
            {
                bool happy = t < happyUntil;
                float amplitude = swayDegrees * (happy ? 3f : 1f) * (thirsty ? 0.5f : 1f);
                lean = Mathf.Lerp(lean, thirsty ? -3.5f : 0f, dt * 2f);
                angle = lean + Mathf.Sin(t * swaySpeed * (happy ? 2.2f : 1f) + phase) * amplitude;
            }
            float s = t - shakeStart;
            if (s < 0.5f) angle += Mathf.Sin(s * 50f) * 7f * (1f - s / 0.5f);
            art.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
        }

        public void SetHover(bool value)
        {
            hover = value;
        }

        public void SetPressed(bool value)
        {
            if (pressed && !value) Kick(new Vector2(-0.6f, 1.4f));
            pressed = value;
        }

        public void Kick(Vector2 impulse)
        {
            velocity += impulse;
        }

        /// <summary>Small bounce used when a plant is selected without the pointer (keyboard).</summary>
        public void Poke()
        {
            Kick(new Vector2(-0.9f, 1.8f));
        }

        /// <summary>"No" head-shake.</summary>
        public void Shake()
        {
            shakeStart = Time.unscaledTime;
        }

        /// <summary>Runs a care action on a plant and posts the result as a toast.</summary>
        public static void PerformAndNotify(PlantUnit plant, CareAction action)
        {
            if (plant == null) return;
            var interaction = plant.GetComponent<PotInteraction>();
            if (interaction == null) return;
            string message;
            bool ok = interaction.TryCare(action, out message);
            if (!string.IsNullOrEmpty(message) && NotificationCenter.Instance != null)
                NotificationCenter.Instance.PostCareFeedback(plant, action, message, ok);
        }

        /// <summary>Runs a care action. Returns false (with a message) if it was refused.</summary>
        public bool TryCare(CareAction action, out string message)
        {
            message = null;
            if (plant == null || IsBusy) return false;
            if (plant.IsEmpty)
            {
                message = L.T("ปลูกเมล็ดก่อนนะ", "Plant a seed first");
                Shake();
                return false;
            }

            var servo = GetComponent<PotServo>();

            if (action == CareAction.Fertilize)
            {
                var growth = GetComponent<PlantGrowth>();
                if (growth == null) return false;
                if (!growth.CanFertilize)
                {
                    message = L.T("ให้ปุ๋ยไปแล้ววันนี้ พรุ่งนี้ค่อยให้ใหม่", "Already fertilized today - try tomorrow");
                    Shake();
                    return false;
                }
                growth.Fertilize();
                busyUntil = Time.unscaledTime + waterCooldown;
                happyUntil = busyUntil + 2f;
                if (servo != null) servo.Pulse(ServoAction.Fertilize);
                else if (PlantDataSource.Active != null) PlantDataSource.Active.SendCareCommand(plant, CareAction.Fertilize);
                if (EffectsLayer.Instance != null) EffectsLayer.Instance.Sparkles(art, UISkin.Current != null ? UISkin.Current.leafColor : Color.green, 8);
                Kick(new Vector2(-0.4f, 1.0f));
                message = L.T("ให้ปุ๋ย" + plant.DisplayName + "แล้ว โตเร็วขึ้น 2 เท่า 10 นาที", "Fertilized - grows 2x faster for 10 min");
                return true;
            }

            if (action == CareAction.Water)
            {
                if (plant.species != null && plant.HasReading &&
                    plant.Reading.moisture >= plant.species.moisture.max - wetMargin)
                {
                    message = L.TooWetToWater;
                    Shake();
                    return false;
                }
                busyUntil = Time.unscaledTime + waterCooldown;
                happyUntil = busyUntil + 2f;
                if (EffectsLayer.Instance != null) EffectsLayer.Instance.PlayWater(art);
                Kick(new Vector2(0.5f, -0.6f));
                // With a servo valve the water really flows through it; otherwise send a one-off command.
                if (servo != null) servo.Pulse(ServoAction.Water);
                else if (PlantDataSource.Active != null) PlantDataSource.Active.SendCareCommand(plant, CareAction.Water);
                message = L.Watered(plant.DisplayName);
                return true;
            }

            busyUntil = Time.unscaledTime + pruneCooldown;
            happyUntil = busyUntil + 1f;
            if (EffectsLayer.Instance != null) EffectsLayer.Instance.PlayPrune(art);
            Kick(new Vector2(0.9f, -0.9f));
            if (PlantDataSource.Active != null) PlantDataSource.Active.SendCareCommand(plant, CareAction.Prune);
            message = L.Pruned(plant.DisplayName);
            return true;
        }
    }
}
