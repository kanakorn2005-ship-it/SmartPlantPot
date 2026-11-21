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
            GameClock.ManualHour = -1f;
            GameClock.DayOffset = 0;
        }
    }
}
