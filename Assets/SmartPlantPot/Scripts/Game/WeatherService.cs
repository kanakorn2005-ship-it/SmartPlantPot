using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartPlantPot
{
    /// <summary>
    /// Real weather from Open-Meteo (free, no API key) for the shop's location - Bangkok by default.
    /// Feeds EnvironmentState (clouds dim the sun, rain starts the rain effect, outdoor temperature
    /// nudges the simulated indoor temperature) and can be overridden from the Mock panel.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    [AddComponentMenu("Smart Plant Pot/Weather Service")]
    public class WeatherService : MonoBehaviour
    {
        public static WeatherService Instance { get; private set; }

        public bool useRealWeather = true;
        public double latitude = 13.7563;
        public double longitude = 100.5018;
        public float refreshMinutes = 15f;

        [Tooltip("-1 = real weather; otherwise force a WeatherKind (Mock panel).")]
        public int overrideKind = -1;

        public bool HasRealData { get; private set; }
        public WeatherKind RealKind { get; private set; }
        public float RealTemperature { get; private set; }
        public float RealHumidity { get; private set; }
        public string LastError { get; private set; }
        public DateTime LastUpdate { get; private set; }

        public event Action Changed;

        [Serializable]
        class Current
        {
            public float temperature_2m;
            public float relative_humidity_2m;
            public int is_day;
            public int weather_code;
            public float cloud_cover;
        }

        [Serializable]
        class Response
        {
            public Current current;
        }

        void Awake()
        {
            Instance = this;
            RealKind = WeatherKind.Clear;
            LastError = "";
            ApplyState();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            StartCoroutine(Loop());
        }

        public void RefreshNow()
        {
            StopAllCoroutines();
            if (isActiveAndEnabled) StartCoroutine(Loop());
        }

        public void SetOverride(int kind)
        {
            overrideKind = kind;
            ApplyState();
        }

        IEnumerator Loop()
        {
            while (true)
            {
                if (useRealWeather) yield return Fetch();
                yield return new WaitForSecondsRealtime(Mathf.Max(1f, refreshMinutes) * 60f);
            }
        }

        IEnumerator Fetch()
        {
            var inv = CultureInfo.InvariantCulture;
            string url = "https://api.open-meteo.com/v1/forecast?latitude=" + latitude.ToString("0.####", inv) +
                         "&longitude=" + longitude.ToString("0.####", inv) +
                         "&current=temperature_2m,relative_humidity_2m,is_day,weather_code,cloud_cover&timezone=auto";
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    LastError = req.error;
                    ApplyState();
                    yield break;
                }
                Response r = null;
                try
                {
                    r = JsonUtility.FromJson<Response>(req.downloadHandler.text);
                }
                catch (Exception e)
                {
                    LastError = e.Message;
                }
                if (r != null && r.current != null)
                {
                    HasRealData = true;
                    RealKind = FromWmo(r.current.weather_code, r.current.cloud_cover);
                    RealTemperature = r.current.temperature_2m;
                    RealHumidity = r.current.relative_humidity_2m;
                    LastUpdate = DateTime.Now;
                    LastError = "";
                }
                ApplyState();
            }
        }

        public WeatherKind EffectiveKind
        {
            get
            {
                if (overrideKind >= 0) return (WeatherKind)overrideKind;
                return HasRealData && useRealWeather ? RealKind : WeatherKind.Clear;
            }
        }

        void ApplyState()
        {
            EnvironmentState.Weather = EffectiveKind;
            EnvironmentState.OutdoorTemperature = HasRealData && useRealWeather ? RealTemperature : float.NaN;
            EnvironmentState.OutdoorHumidity = HasRealData && useRealWeather ? RealHumidity : float.NaN;
            if (Changed != null) Changed();
        }

        /// <summary>WMO weather interpretation codes used by Open-Meteo.</summary>
        public static WeatherKind FromWmo(int code, float cloudCover)
        {
            if (code == 0) return cloudCover > 40f ? WeatherKind.PartlyCloudy : WeatherKind.Clear;
            if (code == 1 || code == 2) return WeatherKind.PartlyCloudy;
            if (code == 3) return WeatherKind.Cloudy;
            if (code == 45 || code == 48) return WeatherKind.Fog;
            if (code >= 51 && code <= 57) return WeatherKind.Drizzle;
            if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) return WeatherKind.Rain;
            if (code >= 95) return WeatherKind.Storm;
            return WeatherKind.Cloudy; // snow etc. - unlikely in Thailand
        }

        public static string Name(WeatherKind k)
        {
            switch (k)
            {
                case WeatherKind.Clear: return L.T("แจ่มใส", "Clear");
                case WeatherKind.PartlyCloudy: return L.T("มีเมฆบางส่วน", "Partly cloudy");
                case WeatherKind.Cloudy: return L.T("เมฆมาก", "Cloudy");
                case WeatherKind.Fog: return L.T("หมอก", "Fog");
                case WeatherKind.Drizzle: return L.T("ฝนปรอย", "Drizzle");
                case WeatherKind.Rain: return L.T("ฝนตก", "Rain");
                default: return L.T("พายุฝนฟ้าคะนอง", "Thunderstorm");
            }
        }
    }
}
