using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Price multipliers and names for mutations.</summary>
    public static class MutationInfo
    {
        public static float Multiplier(Mutation m)
        {
            switch (m)
            {
                case Mutation.Golden: return 2f;
                case Mutation.Variegated: return 1.5f;
                case Mutation.Giant: return 1.3f;
                case Mutation.Rainbow: return 3f;
                default: return 1f;
            }
        }

        public static string Name(Mutation m)
        {
            switch (m)
            {
                case Mutation.Golden: return L.T("ทองคำ", "Golden");
                case Mutation.Variegated: return L.T("ใบด่าง", "Variegated");
                case Mutation.Giant: return L.T("ยักษ์", "Giant");
                case Mutation.Rainbow: return L.T("สายรุ้ง", "Rainbow");
                default: return "";
            }
        }

        public static Mutation Roll(System.Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.05) return Mutation.Rainbow;
            if (r < 0.25) return Mutation.Giant;
            if (r < 0.60) return Mutation.Variegated;
            return Mutation.Golden;
        }
    }

    /// <summary>
    /// The game side of one pot: what is planted, how big it has grown, care quality, the daily
    /// care streak, mutations and sell prices. Spawns and scales the 3D plant model.
    /// Growth and quality come from the real (or simulated) sensor health, so caring for the real
    /// plant well makes the in-game plant grow faster, sell for more and keep its streak.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlantUnit))]
    [AddComponentMenu("Smart Plant Pot/Plant Growth")]
    public class PlantGrowth : MonoBehaviour
    {
        public static readonly List<PlantGrowth> All = new List<PlantGrowth>();

        public PlantUnit plant;
        public PotInteraction interaction;
        [Tooltip("Where the plant model is spawned (the pot's soil centre is at local 0,0,0 of the prefab root).")]
        public Transform modelAnchor;
        [Tooltip("Flat glow under the pot coloured by plant status.")]
        public SpriteRenderer statusGlow;

        [Header("Tuning")]
        [Tooltip("Minutes for quality to catch up with the current health.")]
        public float qualityTimeConstantMinutes = 6f;
        [Tooltip("Average health over a day needed to keep the streak.")]
        [Range(0f, 1f)] public float streakHealthNeeded = 0.75f;
        public int streakForMutation = 3;
        [Range(0f, 1f)] public float mutationChance = 0.2f;

        // ---- saved state ----
        [NonSerialized] public float growth;          // 0..1
        [NonSerialized] public float quality = 60f;   // 0..100
        [NonSerialized] public int streak;
        [NonSerialized] public Mutation mutation;
        [NonSerialized] public string lastDailySellDay = "";
        [NonSerialized] public string lastFertilizeDay = "";
        [NonSerialized] public string plantedOn = "";
        [NonSerialized] public double daySum;
        [NonSerialized] public double daySeconds;
        [NonSerialized] public float fertilizerMinutesLeft;

        public event Action<PlantGrowth> Changed;

        GameObject model;
        Transform growthPivot;
        Transform wobble;
        readonly List<Renderer> foliage = new List<Renderer>();
        readonly List<Color[]> baseColors = new List<Color[]>();
        MaterialPropertyBlock block;
        float shownScale = 0.1f;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsEmpty
        {
            get { return plant == null || plant.species == null; }
        }

        public PlantSpecies Species
        {
            get { return plant != null ? plant.species : null; }
        }

        public GrowthStage Stage
        {
            get
            {
                if (IsEmpty) return GrowthStage.Empty;
                if (growth >= 1f) return GrowthStage.Mature;
                if (growth >= 0.5f) return GrowthStage.Young;
                if (growth >= 0.15f) return GrowthStage.Seedling;
                return GrowthStage.Seed;
            }
        }

        public static string StageName(GrowthStage s)
        {
            switch (s)
            {
                case GrowthStage.Seed: return L.T("เพิ่งเพาะเมล็ด", "Just sown");
                case GrowthStage.Seedling: return L.T("ต้นกล้า", "Seedling");
                case GrowthStage.Young: return L.T("ต้นอ่อน", "Young plant");
                case GrowthStage.Mature: return L.T("โตเต็มที่", "Fully grown");
                default: return L.EmptyPot;
            }
        }

        public bool IsFertilized
        {
            get { return fertilizerMinutesLeft > 0f; }
        }

        public bool CanFertilize
        {
            get { return !IsEmpty && growth < 1f && lastFertilizeDay != GameClock.TodayKey; }
        }

        public bool CanDailySell
        {
            get { return !IsEmpty && growth >= 0.5f && lastDailySellDay != GameClock.TodayKey; }
        }

        public bool SoldToday
        {
            get { return lastDailySellDay == GameClock.TodayKey; }
        }

        public float QualityFactor
        {
            get { return 0.3f + 0.7f * Mathf.Clamp01(quality / 100f); }
        }

        public float GrowthFactor
        {
            get { return growth >= 1f ? 1f : growth >= 0.5f ? 0.5f : 0.2f; }
        }

        public float StreakMultiplier
        {
            get { return 1f + Mathf.Min(streak, 10) * 0.05f; }
        }

        public float TodayAverageHealth
        {
            get { return daySeconds > 1.0 ? (float)(daySum / daySeconds) : (plant != null && plant.HasReading ? plant.HealthScore : 0f); }
        }

        void Reset()
        {
            plant = GetComponent<PlantUnit>();
            interaction = GetComponent<PotInteraction>();
            modelAnchor = transform;
        }

        void Awake()
        {
            if (plant == null) plant = GetComponent<PlantUnit>();
            if (interaction == null) interaction = GetComponent<PotInteraction>();
            if (modelAnchor == null) modelAnchor = transform;
            block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        // ------------------------------------------------------------------ planting / selling

        public void Plant(PlantSpecies species)
        {
            if (species == null) return;
            growth = 0.02f;
            quality = 60f;
            streak = 0;
            mutation = Mutation.None;
            daySum = 0;
            daySeconds = 0;
            fertilizerMinutesLeft = 0f;
            lastDailySellDay = "";
            lastFertilizeDay = "";
            plantedOn = GameClock.TodayKey;
            plant.SetSpecies(species);
            SpawnModel();
            shownScale = ScaleFor(growth);
            RaiseChanged();
        }

        public void Clear()
        {
            DestroyModel();
            growth = 0f;
            quality = 60f;
            streak = 0;
            mutation = Mutation.None;
            fertilizerMinutesLeft = 0f;
            plantedOn = "";
            if (plant != null) plant.SetSpecies(null);
            if (interaction != null) interaction.SetArt(null);
            RaiseChanged();
        }

        /// <summary>Restore from a save without resetting progress.</summary>
        public void Restore(PlantSpecies species)
        {
            plant.SetSpecies(species);
            if (species != null) SpawnModel();
            else DestroyModel();
            shownScale = ScaleFor(growth);
            RaiseChanged();
        }

        public void Fertilize()
        {
            lastFertilizeDay = GameClock.TodayKey;
            fertilizerMinutesLeft = 10f;
            RaiseChanged();
        }

        public void MarkDailySold()
        {
            lastDailySellDay = GameClock.TodayKey;
            RaiseChanged();
        }

        public int SellPrice(float eventMultiplier)
        {
            if (IsEmpty) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(Species.sellPrice * GrowthFactor * QualityFactor * StreakMultiplier *
                                                 MutationInfo.Multiplier(mutation) * eventMultiplier));
        }

        public int DailyPoints(float eventMultiplier)
        {
            if (IsEmpty || growth < 0.5f) return 0;
            float stage = growth >= 1f ? 1f : 0.5f;
            return Mathf.Max(1, Mathf.RoundToInt(Species.dailyPoints * stage * QualityFactor * StreakMultiplier *
                                                 MutationInfo.Multiplier(mutation) * eventMultiplier));
        }

        // ------------------------------------------------------------------ daily rollover

        public struct DayResult
        {
            public bool hadPlant;
            public int oldStreak;
            public int newStreak;
            public Mutation newMutation;
        }

        /// <summary>Called once when the game date changes: update the streak and maybe mutate.</summary>
        public DayResult OnNewDay(System.Random rng)
        {
            var r = new DayResult { hadPlant = !IsEmpty, oldStreak = streak };
            if (!IsEmpty)
            {
                float avg = TodayAverageHealth;
                streak = avg >= streakHealthNeeded ? streak + 1 : 0;
                if (mutation == Mutation.None && growth >= 1f && streak >= streakForMutation &&
                    rng.NextDouble() < mutationChance + 0.05 * (streak - streakForMutation))
                {
                    mutation = MutationInfo.Roll(rng);
                    r.newMutation = mutation;
                    ApplyMutationLook();
                }
            }
            r.newStreak = streak;
            daySum = 0;
            daySeconds = 0;
            RaiseChanged();
            return r;
        }

        /// <summary>Debug: force a mutation on this plant.</summary>
        public void ForceMutation(Mutation m)
        {
            if (IsEmpty) return;
            mutation = m;
            ApplyMutationLook();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (IsEmpty)
            {
                UpdateGlow();
                return;
            }

            float dt = Time.deltaTime;
            float health = plant.HasReading ? plant.HealthScore : 0.7f;

            // Growth: healthy plants grow at full speed, struggling ones slow down, very sick ones stop.
            float speed = Mathf.InverseLerp(0.35f, 0.9f, health);
            float mult = 1f;
            if (GameManager.Instance != null) mult *= GameManager.Instance.growthSpeedMultiplier;
            if (DailyEvents.Instance != null) mult *= DailyEvents.Instance.GrowthMultiplier;
            if (fertilizerMinutesLeft > 0f)
            {
                mult *= 2f;
                fertilizerMinutesLeft = Mathf.Max(0f, fertilizerMinutesLeft - dt / 60f);
            }
            float minutes = Mathf.Max(0.5f, Species.growMinutes);
            if (growth < 1f) growth = Mathf.Min(1f, growth + dt / (minutes * 60f) * speed * mult);

            // Quality follows health smoothly.
            float k = Mathf.Clamp01(dt / Mathf.Max(1f, qualityTimeConstantMinutes * 60f));
            quality = Mathf.Lerp(quality, health * 100f, k);

            // Today's average health decides tomorrow's streak.
            daySum += health * dt;
            daySeconds += dt;

            // Visual size eases toward the growth stage.
            float target = ScaleFor(growth);
            shownScale = Mathf.MoveTowards(shownScale, target, dt * 0.5f);
            if (growthPivot != null) growthPivot.localScale = Vector3.one * shownScale;
            if (mutation == Mutation.Rainbow) ApplyMutationLook();
            UpdateGlow();
        }

        float ScaleFor(float g)
        {
            float s = Mathf.Lerp(0.12f, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(g), 2f));
            if (mutation == Mutation.Giant) s *= 1.3f;
            return s;
        }

        void UpdateGlow()
        {
            var skin = UISkin.Current;
            if (statusGlow == null || skin == null) return;
            bool selected = plant != null && PlantSelection.Current == plant;
            Color c;
            float a;
            PlantMetric m;
            MetricStatus st;
            if (!IsEmpty && plant.TryGetMainIssue(out m, out st))
            {
                c = skin.StatusColor(st);
                a = 0.6f;
            }
            else if (selected)
            {
                c = skin.accent;
                a = 0.55f;
            }
            else
            {
                c = IsEmpty ? Color.white : skin.statusNormal;
                a = IsEmpty ? 0.12f : 0.3f;
            }
            if (selected) a *= 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
            c.a = a;
            statusGlow.color = c;
        }

        void RaiseChanged()
        {
            if (Changed != null) Changed(this);
        }

        // ------------------------------------------------------------------ model

        void SpawnModel()
        {
            DestroyModel();
            var catalog = PlantCatalog.Current;
            var prefab = catalog != null ? catalog.ModelFor(Species) : (Species != null ? Species.modelPrefab : null);
            if (prefab == null)
            {
                Debug.LogWarning("[SmartPlantPot] No 3D model for " + (Species != null ? Species.Key : "?"));
                return;
            }
            model = Instantiate(prefab, modelAnchor, false);
            model.name = "Plant_" + Species.Key;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            growthPivot = model.transform.Find("Growth");
            if (growthPivot == null) growthPivot = model.transform;
            wobble = growthPivot.Find("Wobble");
            if (wobble == null) wobble = growthPivot;
            if (interaction != null) interaction.SetArt(wobble);

            foliage.Clear();
            baseColors.Clear();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.gameObject.activeSelf) continue;
                foliage.Add(r);
                var mats = r.sharedMaterials;
                var cols = new Color[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    cols[i] = mat == null ? Color.white
                        : mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId)
                        : mat.HasProperty(ColorId) ? mat.GetColor(ColorId) : Color.white;
                }
                baseColors.Add(cols);
            }
            if (growthPivot != null) growthPivot.localScale = Vector3.one * ScaleFor(growth);
            ApplyMutationLook();
        }

        void DestroyModel()
        {
            if (model != null) Destroy(model);
            model = null;
            growthPivot = null;
            wobble = null;
            foliage.Clear();
            baseColors.Clear();
        }

        void ApplyMutationLook()
        {
            if (block == null) block = new MaterialPropertyBlock();
            for (int r = 0; r < foliage.Count; r++)
            {
                var rend = foliage[r];
                if (rend == null) continue;
                var cols = baseColors[r];
                for (int i = 0; i < cols.Length; i++)
                {
                    Color c = Tint(cols[i], mutation, r * 0.13f + i * 0.07f);
                    rend.GetPropertyBlock(block, i);
                    block.SetColor(BaseColorId, c);
                    block.SetColor(ColorId, c);
                    rend.SetPropertyBlock(block, i);
                }
            }
        }

        static Color Tint(Color c, Mutation m, float offset)
        {
            switch (m)
            {
                case Mutation.Golden:
                    return Color.Lerp(c, new Color(1f, 0.8f, 0.25f, c.a), 0.6f);
                case Mutation.Variegated:
                    return Color.Lerp(c, new Color(0.96f, 0.95f, 0.72f, c.a), 0.45f);
                case Mutation.Rainbow:
                    var hue = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.15f + offset, 1f), 0.6f, 1f);
                    hue.a = c.a;
                    return Color.Lerp(c, hue, 0.6f);
                default:
                    return c;
            }
        }
    }
}
