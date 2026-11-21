using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Real-time day and night: the sun follows the local clock (or the Mock panel's hour), its
    /// colour warms at sunrise/sunset, the shop lamps switch on in the evening, the sky darkens at
    /// night and clouds/rain from WeatherService dim everything. Writes EnvironmentState.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [AddComponentMenu("Smart Plant Pot/Day Night Cycle")]
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        public Light sun;
        public Light[] lamps;
        public float lampIntensity = 1.6f;

        [Header("Sun path")]
        [Tooltip("Yaw the sunlight comes FROM at noon (aim it through the window).")]
        public float noonYaw = 180f;
        public float sunriseHour = 6f;
        public float sunsetHour = 18.25f;
        public float maxElevation = 65f;

        [Header("Light")]
        public float sunIntensity = 1.6f;
        public float moonIntensity = 0.12f;
        public Color dayColor = new Color(1f, 0.96f, 0.9f);
        public Color dawnColor = new Color(1f, 0.62f, 0.38f);
        public Color nightColor = new Color(0.5f, 0.6f, 0.9f);

        [Header("Ambient (trilight)")]
        public Color daySky = new Color(0.82f, 0.88f, 0.95f);
        public Color dayEquator = new Color(0.78f, 0.75f, 0.7f);
        public Color dayGround = new Color(0.42f, 0.38f, 0.32f);
        public Color nightSky = new Color(0.16f, 0.19f, 0.3f);
        public Color nightEquator = new Color(0.14f, 0.13f, 0.18f);
        public Color nightGround = new Color(0.07f, 0.07f, 0.08f);

        Material sky;
        float flash;
        float nextFlash;

        void Awake()
        {
            Instance = this;
            EnvironmentState.HasEnvironment = true;
            if (RenderSettings.skybox != null)
            {
                sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            if (sun != null) RenderSettings.sun = sun;
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            Apply();
        }

        /// <summary>0..1 position of the sun across the sky (negative / >1 = night).</summary>
        public float DayProgress
        {
            get { return (GameClock.HourOfDay - sunriseHour) / Mathf.Max(1f, sunsetHour - sunriseHour); }
        }

        public static string PartOfDay(float hour)
        {
            if (hour >= 5f && hour < 11f) return L.T("เช้า", "Morning");
            if (hour >= 11f && hour < 13f) return L.T("เที่ยง", "Noon");
            if (hour >= 13f && hour < 17f) return L.T("บ่าย", "Afternoon");
            if (hour >= 17f && hour < 19f) return L.T("เย็น", "Evening");
            return L.T("กลางคืน", "Night");
        }

        void Apply()
        {
            float t = DayProgress;
            bool day = t > 0f && t < 1f;
            float elev01 = day ? Mathf.Sin(t * Mathf.PI) : 0f;
            // twilight: a little light just before sunrise / after sunset
            float twilight = day ? 0f : Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(t), Mathf.Abs(t - 1f)) * 12f);
            float dayness = Mathf.Clamp01(Mathf.Max(elev01 * 2.2f, twilight * 0.35f));
            float cloud = EnvironmentState.CloudFactor;

            EnvironmentState.IsNight = !day;
            EnvironmentState.Daylight01 = dayness * (1f - 0.5f * cloud);

            if (sun != null)
            {
                float yaw = day ? noonYaw + Mathf.Lerp(-75f, 75f, t) : noonYaw + 160f;
                float pitch = day ? Mathf.Lerp(6f, maxElevation, elev01) : 40f;
                sun.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                float intensity = day ? Mathf.Lerp(0.25f, sunIntensity, elev01) * (1f - 0.75f * cloud)
                                      : moonIntensity * (1f - 0.6f * cloud);
                Color c = day ? Color.Lerp(dawnColor, dayColor, Mathf.Clamp01(elev01 * 2.5f)) : nightColor;
                c = Color.Lerp(c, new Color(0.75f, 0.78f, 0.82f), cloud * 0.6f);

                // thunderstorm flashes
                if (EnvironmentState.Weather == WeatherKind.Storm)
                {
                    if (Time.time > nextFlash)
                    {
                        flash = 1f;
                        nextFlash = Time.time + Random.Range(6f, 14f);
                    }
                }
                flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 4f);
                sun.intensity = intensity + flash * 2.5f;
                sun.color = Color.Lerp(c, Color.white, flash);
            }

            float amb = dayness * (1f - 0.35f * cloud);
            RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, amb);
            RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, amb);
            RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, amb);

            if (lamps != null)
            {
                float on = Mathf.Clamp01((0.45f - dayness) / 0.45f);
                foreach (var l in lamps)
                    if (l != null) l.intensity = lampIntensity * on;
            }

            if (sky != null)
            {
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(0.08f, 1.25f, dayness) * (1f - 0.4f * cloud));
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", Color.Lerp(new Color(0.5f, 0.5f, 0.5f), new Color(0.62f, 0.62f, 0.62f), cloud));
            }
        }
    }
}
