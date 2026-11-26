namespace SmartPlantPot
{
    public enum WeatherKind
    {
        Clear = 0,
        PartlyCloudy = 1,
        Cloudy = 2,
        Fog = 3,
        Drizzle = 4,
        Rain = 5,
        Storm = 6
    }

    /// <summary>
    /// Shared snapshot of the outside world (written by DayNightCycle / WeatherService, read by the
    /// simulator, alerts and events). Lives in Core so the 2D scene works without those systems.
    /// </summary>
    public static class EnvironmentState
    {
        /// <summary>True between sunset and sunrise. Light alerts are paused at night.</summary>
        public static bool IsNight;

        /// <summary>0 at night .. 1 at noon on a clear day.</summary>
        public static float Daylight01 = 1f;

        public static WeatherKind Weather = WeatherKind.Clear;

        /// <summary>Real outdoor temperature from the weather service; NaN until known.</summary>
        public static float OutdoorTemperature = float.NaN;

        public static float OutdoorHumidity = float.NaN;

        /// <summary>True once DayNightCycle runs (the 2D scene leaves this false).</summary>
        public static bool HasEnvironment;

        // ---- the real room, from the ESP32 sensors (HttpPlantDataSource) ----

        /// <summary>True while real sensor readings are arriving. The room then follows the
        /// sensors (scene brightness, header temperature, simulated pots) instead of the weather API.</summary>
        public static bool SensorLive;

        /// <summary>Room temperature from the sensors, °C (NaN until known).</summary>
        public static float SensorTemperature = float.NaN;

        /// <summary>Light at the sensors, lux (NaN until known).</summary>
        public static float SensorLux = float.NaN;

        /// <summary>Air humidity from the DHT sensor, % (NaN until known).</summary>
        public static float SensorHumidity = float.NaN;

        /// <summary>Temperature the room is at: the sensors when live, else the outdoor weather.</summary>
        public static float RoomTemperature
        {
            get { return SensorLive && !float.IsNaN(SensorTemperature) ? SensorTemperature : OutdoorTemperature; }
        }

        /// <summary>0..1 brightness from the sensor lux on a log scale (~3 lux = 0, ~300 = 0.5, 20,000+ = 1).</summary>
        public static float SensorBrightness01
        {
            get
            {
                if (float.IsNaN(SensorLux)) return 1f;
                float l = UnityEngine.Mathf.Log10(UnityEngine.Mathf.Max(1f, SensorLux));
                return UnityEngine.Mathf.Clamp01((l - 0.5f) / (4.3f - 0.5f));
            }
        }

        /// <summary>True when the scene lighting should follow the light sensor.</summary>
        public static bool LightFromSensor
        {
            get { return SensorLive && !float.IsNaN(SensorLux); }
        }

        public static bool IsRainy
        {
            get { return Weather == WeatherKind.Drizzle || Weather == WeatherKind.Rain || Weather == WeatherKind.Storm; }
        }

        /// <summary>0..1 how much clouds block the sun.</summary>
        public static float CloudFactor
        {
            get
            {
                switch (Weather)
                {
                    case WeatherKind.PartlyCloudy: return 0.25f;
                    case WeatherKind.Cloudy: return 0.55f;
                    case WeatherKind.Fog: return 0.6f;
                    case WeatherKind.Drizzle: return 0.6f;
                    case WeatherKind.Rain: return 0.7f;
                    case WeatherKind.Storm: return 0.85f;
                    default: return 0f;
                }
            }
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsNight = false;
            Daylight01 = 1f;
            Weather = WeatherKind.Clear;
            OutdoorTemperature = float.NaN;
            OutdoorHumidity = float.NaN;
            HasEnvironment = false;
            SensorLive = false;
            SensorTemperature = float.NaN;
            SensorLux = float.NaN;
            SensorHumidity = float.NaN;
            GameClock.ManualHour = -1f;
            GameClock.DayOffset = 0;
        }
    }
}
