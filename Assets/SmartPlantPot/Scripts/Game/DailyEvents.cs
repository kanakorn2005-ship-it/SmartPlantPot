using System;
using UnityEngine;

namespace SmartPlantPot
{
    public enum DayEventKind
    {
        Normal = 0,
        PlantMarket = 1,
        SeedSale = 2,
        CustomerWants = 3,
        Festival = 4,
        SunnyDay = 5,
        RainyDay = 6
    }

    /// <summary>
    /// One event per game day, picked from the date (same for everyone, stable all day) and nudged
    /// by the real weather. Each event changes sell prices, seed prices or growth speed.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Daily Events")]
    public class DailyEvents : MonoBehaviour
    {
        public static DailyEvents Instance { get; private set; }

        [Tooltip("-1 = pick by date; otherwise force a DayEventKind (Mock panel).")]
        public int forceEvent = -1;

        public DayEventKind Today { get; private set; }
        public PlantSpecies WantedSpecies { get; private set; }

        public event Action Changed;

        string computedFor = "";
        bool computedRainy;
        bool computedClear;
        float timer;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            Refresh(true);
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 2f) return;
            timer = 0f;
            bool rainy = EnvironmentState.IsRainy;
            bool clear = EnvironmentState.Weather == WeatherKind.Clear || EnvironmentState.Weather == WeatherKind.PartlyCloudy;
            if (GameClock.TodayKey != computedFor) Refresh(true);
            else if (rainy != computedRainy || clear != computedClear) Refresh(false);
        }

        static uint Hash(string s)
        {
            uint h = 2166136261;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619;
            }
            return h;
        }

        public void Refresh(bool announce)
        {
            var previous = Today;
            computedFor = GameClock.TodayKey;
            computedRainy = EnvironmentState.IsRainy;
            computedClear = EnvironmentState.Weather == WeatherKind.Clear || EnvironmentState.Weather == WeatherKind.PartlyCloudy;

            uint h = Hash(computedFor + "|smartplantpot");
            if (forceEvent >= 0)
            {
                Today = (DayEventKind)forceEvent;
            }
            else
            {
                // weights: Normal 3, Market 2, Seed sale 2, Customer 2, Festival 1
                int roll = (int)(h % 10u);
                Today = roll < 3 ? DayEventKind.Normal
                    : roll < 5 ? DayEventKind.PlantMarket
                    : roll < 7 ? DayEventKind.SeedSale
                    : roll < 9 ? DayEventKind.CustomerWants
                    : DayEventKind.Festival;
                if (Today == DayEventKind.Normal && EnvironmentState.HasEnvironment)
                {
                    if (computedRainy) Today = DayEventKind.RainyDay;
                    else if (computedClear && !EnvironmentState.IsNight) Today = DayEventKind.SunnyDay;
                }
            }

            WantedSpecies = null;
            var catalog = PlantCatalog.Current;
            if (Today == DayEventKind.CustomerWants && catalog != null && catalog.All.Count > 0)
                WantedSpecies = catalog.All[(int)((h / 10u) % (uint)catalog.All.Count)];

            if (Changed != null) Changed();
            if ((announce || previous != Today) && Today != DayEventKind.Normal && NotificationCenter.Instance != null)
                NotificationCenter.Instance.PostInfo(null, L.T("อีเวนต์วันนี้: ", "Today: ") + Title, Description, AlertKind.GameEvent);
        }

        public float SellMultiplier(PlantSpecies s)
        {
            switch (Today)
            {
                case DayEventKind.PlantMarket: return 1.5f;
                case DayEventKind.Festival: return 1.25f;
                case DayEventKind.CustomerWants: return s != null && WantedSpecies != null && s.Key == WantedSpecies.Key ? 2f : 1f;
                default: return 1f;
            }
        }

        public float SeedPriceMultiplier
        {
            get { return Today == DayEventKind.SeedSale ? 0.7f : Today == DayEventKind.Festival ? 0.9f : 1f; }
        }

        public float GrowthMultiplier
        {
            get { return Today == DayEventKind.SunnyDay ? 1.5f : Today == DayEventKind.RainyDay ? 1.25f : Today == DayEventKind.Festival ? 1.2f : 1f; }
        }

        public string Title
        {
            get
            {
                switch (Today)
                {
                    case DayEventKind.PlantMarket: return L.T("ตลาดนัดต้นไม้", "Plant market");
                    case DayEventKind.SeedSale: return L.T("เมล็ดลดราคา", "Seed sale");
                    case DayEventKind.CustomerWants: return L.T("ลูกค้าตามหา", "Customer request");
                    case DayEventKind.Festival: return L.T("เทศกาลสวนสวย", "Garden festival");
                    case DayEventKind.SunnyDay: return L.T("แดดดี", "Sunny day");
                    case DayEventKind.RainyDay: return L.T("ฝนชุ่มฉ่ำ", "Rainy day");
                    default: return L.T("วันธรรมดา", "Ordinary day");
                }
            }
        }

        public string Description
        {
            get
            {
                switch (Today)
                {
                    case DayEventKind.PlantMarket: return L.T("ราคาขายทุกต้น x1.5 ทั้งวัน", "All sell prices x1.5 today");
                    case DayEventKind.SeedSale: return L.T("เมล็ดทุกชนิดลด 30%", "All seeds 30% off");
                    case DayEventKind.CustomerWants:
                        return WantedSpecies != null
                            ? L.T("ลูกค้าอยากได้" + WantedSpecies.DisplayName + " ขายต้นนี้ได้ราคา x2", "A customer wants " + WantedSpecies.DisplayName + " - it sells for x2")
                            : L.T("ลูกค้าตามหาต้นไม้พิเศษ", "A customer is looking for something special");
                    case DayEventKind.Festival: return L.T("ขาย x1.25, เมล็ดลด 10%, โตเร็วขึ้น 20%", "Sell x1.25, seeds -10%, growth +20%");
                    case DayEventKind.SunnyDay: return L.T("อากาศดี ต้นไม้โตเร็วขึ้น 50%", "Good weather - plants grow 50% faster");
                    case DayEventKind.RainyDay: return L.T("ฝนตก อากาศชื้น ต้นไม้โตเร็วขึ้น 25%", "Rain - plants grow 25% faster");
                    default: return L.T("ดูแลต้นไม้ให้ดีเพื่อสะสม Streak", "Care well to build your streak");
                }
            }
        }
    }
}
