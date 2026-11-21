using System;
using System.Globalization;

namespace SmartPlantPot
{
    /// <summary>
    /// The game's "now". Follows the real clock by default (day/night, daily selling, streaks), with
    /// two test controls: a fixed hour of day, and a day offset ("skip day" in the Mock panel).
    /// </summary>
    public static class GameClock
    {
        /// <summary>-1 = real time; 0..24 = pretend it is this hour today.</summary>
        public static float ManualHour = -1f;

        /// <summary>Whole days added to the real date (used to test daily selling / streaks).</summary>
        public static int DayOffset;

        public static DateTime Now
        {
            get
            {
                DateTime real = DateTime.Now.AddDays(DayOffset);
                if (ManualHour < 0f) return real;
                return real.Date.AddHours(Math.Max(0f, Math.Min(23.99f, ManualHour)));
            }
        }

        public static float HourOfDay
        {
            get
            {
                var n = Now;
                return n.Hour + n.Minute / 60f + n.Second / 3600f;
            }
        }

        /// <summary>yyyy-MM-dd of the game date - used as the key for once-a-day rules.</summary>
        public static string TodayKey
        {
            get { return Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        }
    }
}
