using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Points, seed inventory, planting, the two kinds of selling, daily rollover (streaks /
    /// mutations) and saving. One per scene, on the SmartPlantPotApp object.
    ///
    /// Selling:
    ///  1. Daily harvest - the plant stays, once per plant per game day, points from quality.
    ///  2. Sell the plant - it leaves the pot, price from growth x quality x streak x mutation x event.
    /// Points buy seeds (the 2D seed packets) for empty pots.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    [AddComponentMenu("Smart Plant Pot/Game Manager")]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlantCatalog catalog;
        public SimulatedPlantDataSource simulator;
        public HttpPlantDataSource http;
        public WeatherService weather;
        public DailyEvents events;

        [Header("Economy")]
        public int startingPoints = 150;

        [Header("Debug")]
        [Tooltip("Multiply growth speed (the Mock panel can change this).")]
        public float growthSpeedMultiplier = 1f;
        public string saveFileName = "smartplantpot_save.json";
        [Tooltip("Ignore the save file (always start fresh).")]
        public bool startFresh;

        public int Points { get; private set; }
        public GameSettings Settings { get; private set; }

        /// <summary>Points or inventory changed.</summary>
        public event Action Changed;

        readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
        readonly System.Random rng = new System.Random();
        SaveData loaded;
        string lastDay = "";
        bool dirty;
        float saveTimer;
        float dayTimer;

        public string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, saveFileName); }
        }

        void Awake()
        {
            Instance = this;
            if (catalog != null) PlantCatalog.Current = catalog;
            loaded = startFresh ? null : LoadFile();
            if (loaded == null) loaded = new SaveData { points = startingPoints };
            if (loaded.settings == null) loaded.settings = new GameSettings();
            Settings = loaded.settings;

            if (catalog != null) catalog.InitRuntime(loaded.species);
            Points = loaded.points;
            inventory.Clear();
            foreach (var it in loaded.inventory)
                if (it != null && !string.IsNullOrEmpty(it.id) && it.count > 0) inventory[it.id] = it.count;
            GameClock.DayOffset = loaded.dayOffset;
            lastDay = loaded.lastDay;
            ApplySettings();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            RestorePots();
            if (string.IsNullOrEmpty(lastDay)) lastDay = GameClock.TodayKey;
            PotServo.AutoWatered += OnAutoWatered;
        }

        void OnDisable()
        {
            PotServo.AutoWatered -= OnAutoWatered;
        }

        void OnAutoWatered(PotServo servo)
        {
            if (NotificationCenter.Instance != null && servo.plant != null)
                NotificationCenter.Instance.PostInfo(servo.plant, servo.plant.DisplayName + " · Servo",
                    L.T("ดินแห้ง วาล์วเปิดรดน้ำอัตโนมัติ " + servo.waterSeconds.ToString("0") + " วินาที",
                        "Soil dry - valve watered automatically for " + servo.waterSeconds.ToString("0") + "s"), AlertKind.Care);
        }

        void Update()
        {
            dayTimer += Time.unscaledDeltaTime;
            if (dayTimer >= 1f)
            {
                dayTimer = 0f;
                if (GameClock.TodayKey != lastDay) NewDay();
            }

            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= 15f || (dirty && saveTimer >= 2f))
            {
                saveTimer = 0f;
                Save();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit()
        {
            Save();
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        void RaiseChanged()
        {
            dirty = true;
            if (Changed != null) Changed();
        }

        // ------------------------------------------------------------------ economy

        float EventSell(PlantSpecies s)
        {
            return events != null ? events.SellMultiplier(s) : 1f;
        }

        public int SeedPrice(PlantSpecies s)
        {
            if (s == null) return 0;
            float m = events != null ? events.SeedPriceMultiplier : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(s.seedPrice * m));
        }

        public int SeedCount(PlantSpecies s)
        {
            int n;
            return s != null && inventory.TryGetValue(s.Key, out n) ? n : 0;
        }

        public int TotalSeeds
        {
            get
            {
                int n = 0;
                foreach (var kv in inventory) n += kv.Value;
                return n;
            }
        }

        public bool BuySeed(PlantSpecies s, out string message)
        {
            int price = SeedPrice(s);
            if (s == null)
            {
                message = "";
                return false;
            }
            if (Points < price)
            {
                message = L.T("แต้มไม่พอ ต้องใช้ " + price + " แต้ม", "Not enough points - need " + price);
                return false;
            }
            Points -= price;
            inventory[s.Key] = SeedCount(s) + 1;
            message = L.T("ซื้อเมล็ด" + s.DisplayName + "แล้ว (-" + price + " แต้ม)", "Bought " + s.DisplayName + " seeds (-" + price + ")");
            RaiseChanged();
            return true;
        }

        public bool PlantSeed(PlantGrowth pot, PlantSpecies s, out string message)
        {
            message = "";
            if (pot == null || s == null) return false;
            if (!pot.IsEmpty)
            {
                message = L.T("กระถางนี้มีต้นไม้อยู่แล้ว", "This pot already has a plant");
                return false;
            }
            if (SeedCount(s) <= 0)
            {
                message = L.T("ไม่มีเมล็ดนี้ ไปซื้อที่ร้านก่อน", "No seeds of this kind - visit the shop");
                return false;
            }
            inventory[s.Key] = SeedCount(s) - 1;
            if (inventory[s.Key] <= 0) inventory.Remove(s.Key);
            pot.Plant(s);
            message = L.T("ปลูก" + s.DisplayName + "ลงกระถาง " + pot.plant.potId + " แล้ว", "Planted " + s.DisplayName + " in " + pot.plant.potId);
            RaiseChanged();
            return true;
        }

        public int PreviewDailySell(PlantGrowth pot)
        {
            return pot == null ? 0 : pot.DailyPoints(EventSell(pot.Species));
        }

        public int PreviewSellPlant(PlantGrowth pot)
        {
            return pot == null ? 0 : pot.SellPrice(EventSell(pot.Species));
        }

        /// <summary>Sell type 1: today's harvest. The plant stays; once per plant per day.</summary>
        public bool DailySell(PlantGrowth pot, out int earned, out string message)
        {
            earned = 0;
            message = "";
            if (pot == null || pot.IsEmpty) return false;
            if (pot.SoldToday)
            {
                message = L.T("ขายผลผลิตต้นนี้ไปแล้ววันนี้ พรุ่งนี้มาใหม่นะ", "Already harvested today - come back tomorrow");
                return false;
            }
            if (pot.growth < 0.5f)
            {
                message = L.T("ต้นยังเล็กเกินไป ต้องโตถึงขั้นต้นอ่อนก่อน", "Too small - wait until it is a young plant");
                return false;
            }
            earned = PreviewDailySell(pot);
            Points += earned;
            pot.MarkDailySold();
            message = L.T("ขายผลผลิต" + pot.Species.DisplayName + " ได้ " + earned + " แต้ม", "Sold today's " + pot.Species.DisplayName + " harvest: +" + earned);
            RaiseChanged();
            return true;
        }

        /// <summary>Sell type 2: the plant leaves; price from how well it was cared for.</summary>
        public bool SellPlant(PlantGrowth pot, out int earned, out string message)
        {
            earned = 0;
            message = "";
            if (pot == null || pot.IsEmpty) return false;
            string name = pot.Species.DisplayName;
            earned = PreviewSellPlant(pot);
            Points += earned;
            pot.Clear();
            message = L.T("ขาย" + name + "ได้ " + earned + " แต้ม กระถางว่างแล้ว ปลูกต้นใหม่ได้เลย",
                "Sold the " + name + " for " + earned + " - the pot is free again");
            RaiseChanged();
            return true;
        }

        public void AddPoints(int amount)
        {
            Points = Mathf.Max(0, Points + amount);
            RaiseChanged();
        }

        // ------------------------------------------------------------------ days

        /// <summary>Debug: jump the game clock to tomorrow (tests daily selling, streaks, mutations).</summary>
        public void SkipDay()
        {
            GameClock.DayOffset++;
            NewDay();
        }

        void NewDay()
        {
            var nc = NotificationCenter.Instance;
            foreach (var pot in PlantGrowth.All)
            {
                var r = pot.OnNewDay(rng);
                if (!r.hadPlant || nc == null) continue;
                string name = pot.plant.DisplayName;
                if (r.newStreak > r.oldStreak && r.newStreak >= 2)
                    nc.PostInfo(pot.plant, name + " · Streak " + r.newStreak,
                        L.T("ดูแลดีต่อเนื่อง " + r.newStreak + " วัน ราคาขาย +" + (Mathf.Min(r.newStreak, 10) * 5) + "%",
                            r.newStreak + " good days in a row - sell price +" + (Mathf.Min(r.newStreak, 10) * 5) + "%"), AlertKind.GameEvent);
                else if (r.oldStreak >= 2 && r.newStreak == 0)
                    nc.PostInfo(pot.plant, name + " · Streak",
                        L.T("Streak หาย! เมื่อวานดูแลไม่ถึงเกณฑ์", "Streak lost - yesterday's care wasn't enough"), AlertKind.Warning, true);
                if (r.newMutation != Mutation.None)
                    nc.PostInfo(pot.plant, name + " · " + L.T("กลายพันธุ์!", "Mutation!"),
                        L.T("กลายเป็นพันธุ์" + MutationInfo.Name(r.newMutation) + " ราคา x" + MutationInfo.Multiplier(r.newMutation).ToString("0.#"),
                            "Became " + MutationInfo.Name(r.newMutation) + " - price x" + MutationInfo.Multiplier(r.newMutation).ToString("0.#")),
                        AlertKind.GameEvent, true);
            }
            lastDay = GameClock.TodayKey;
            if (events != null) events.Refresh(true);
            dirty = true;
        }

        // ------------------------------------------------------------------ settings

        /// <summary>Push Settings into the live systems (call after editing Settings).</summary>
        public void ApplySettings()
        {
            var s = Settings;
            GameClock.ManualHour = s.manualHour;
            growthSpeedMultiplier = Mathf.Max(0.1f, s.growthSpeed);
            if (http != null)
            {
                http.readingsUrl = s.readingsUrl;
                http.commandUrl = s.commandUrl;
            }
            if (simulator != null) simulator.enabled = !s.useRealSensors;
            if (http != null) http.enabled = s.useRealSensors;
            if (weather != null)
            {
                weather.useRealWeather = s.useRealWeather;
                weather.latitude = s.latitude;
                weather.longitude = s.longitude;
                weather.SetOverride(s.weatherOverride);
            }
            if (events != null && events.forceEvent != s.eventOverride)
            {
                events.forceEvent = s.eventOverride;
                events.Refresh(false);
            }
            if (NotificationCenter.Instance != null) NotificationCenter.Instance.remindAfterSeconds = s.remindAfterSeconds;
            dirty = true;
        }

        // ------------------------------------------------------------------ save / load

        SaveData LoadFile()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SmartPlantPot] Could not read save: " + e.Message);
                return null;
            }
        }

        void RestorePots()
        {
            foreach (var pot in PlantGrowth.All)
            {
                PotSave ps = null;
                foreach (var p in loaded.pots)
                    if (p != null && p.potId == pot.plant.potId) ps = p;
                if (ps == null)
                {
                    pot.Restore(null);
                    continue;
                }
                pot.growth = ps.growth;
                pot.quality = ps.quality;
                pot.streak = ps.streak;
                pot.mutation = (Mutation)ps.mutation;
                pot.lastDailySellDay = ps.lastDailySellDay ?? "";
                pot.lastFertilizeDay = ps.lastFertilizeDay ?? "";
                pot.plantedOn = ps.plantedOn ?? "";
                pot.daySum = ps.daySum;
                pot.daySeconds = ps.daySeconds;
                pot.fertilizerMinutesLeft = ps.fertilizerMinutesLeft;
                pot.Restore(catalog != null ? catalog.Find(ps.speciesId) : null);

                var servo = pot.GetComponent<PotServo>();
                if (servo != null)
                {
                    servo.autoWater = ps.servoAutoWater;
                    servo.waterSeconds = ps.servoWaterSeconds > 0f ? ps.servoWaterSeconds : servo.waterSeconds;
                    servo.fertilizerSeconds = ps.servoFertilizerSeconds > 0f ? ps.servoFertilizerSeconds : servo.fertilizerSeconds;
                }
                if (simulator != null && ps.simMoisture >= 0f)
                {
                    var sim = simulator.GetSim(pot.plant);
                    sim.manual = ps.simManual;
                    sim.moisture = ps.simMoisture;
                    if (ps.simTemperature > -50f) sim.temperature = ps.simTemperature;
                    if (ps.simLight >= 0f) sim.light = ps.simLight;
                }
            }
        }

        public void Save()
        {
            dirty = false;
            try
            {
                var d = new SaveData
                {
                    points = Points,
                    lastDay = lastDay,
                    dayOffset = GameClock.DayOffset,
                    settings = Settings,
                    species = catalog != null ? catalog.ToConfigs() : new List<SpeciesConfig>()
                };
                foreach (var kv in inventory) d.inventory.Add(new InventoryItem { id = kv.Key, count = kv.Value });
                foreach (var pot in PlantGrowth.All)
                {
                    var ps = new PotSave
                    {
                        potId = pot.plant.potId,
                        speciesId = pot.IsEmpty ? "" : pot.Species.Key,
                        growth = pot.growth,
                        quality = pot.quality,
                        streak = pot.streak,
                        mutation = (int)pot.mutation,
                        lastDailySellDay = pot.lastDailySellDay,
                        lastFertilizeDay = pot.lastFertilizeDay,
                        plantedOn = pot.plantedOn,
                        daySum = pot.daySum,
                        daySeconds = pot.daySeconds,
                        fertilizerMinutesLeft = pot.fertilizerMinutesLeft
                    };
                    var servo = pot.GetComponent<PotServo>();
                    if (servo != null)
                    {
                        ps.servoAutoWater = servo.autoWater;
                        ps.servoWaterSeconds = servo.waterSeconds;
                        ps.servoFertilizerSeconds = servo.fertilizerSeconds;
                    }
                    if (simulator != null)
                    {
                        var sim = simulator.GetSim(pot.plant);
                        ps.simManual = sim.manual;
                        ps.simMoisture = sim.moisture;
                        ps.simTemperature = sim.temperature;
                        ps.simLight = sim.light;
                    }
                    d.pots.Add(ps);
                }
                File.WriteAllText(SavePath, JsonUtility.ToJson(d, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SmartPlantPot] Could not save: " + e.Message);
            }
        }

        /// <summary>Debug: wipe the save and start over with empty pots.</summary>
        public void ResetSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SmartPlantPot] " + e.Message);
            }
            Points = startingPoints;
            inventory.Clear();
            GameClock.DayOffset = 0;
            loaded = new SaveData { points = startingPoints };
            Settings = loaded.settings;
            if (catalog != null) catalog.InitRuntime(null);
            foreach (var pot in PlantGrowth.All) pot.Clear();
            lastDay = GameClock.TodayKey;
            ApplySettings();
            RaiseChanged();
        }
    }
}
