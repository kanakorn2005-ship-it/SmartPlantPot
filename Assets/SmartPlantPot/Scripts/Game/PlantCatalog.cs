using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Everything about a species that the in-game settings can change (saved as JSON).</summary>
    [Serializable]
    public class SpeciesConfig
    {
        public string id;
        public bool custom;
        public string modelId;
        public string nameThai;
        public string nameEnglish;
        public float moistureMin, moistureMax;
        public float temperatureMin, temperatureMax;
        public float lightMin, lightMax;
        public int seedPrice;
        public int sellPrice;
        public int dailyPoints;
        public float growMinutes;
    }

    /// <summary>
    /// All plant species available in the game. The built-in list is set in the editor; at runtime
    /// it is copied so the alert thresholds can be edited in-game, and custom species can be added
    /// (they reuse one of the built-in 3D models). Saved by GameManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Smart Plant Pot/Plant Catalog", fileName = "PlantCatalog")]
    public class PlantCatalog : ScriptableObject
    {
        public static PlantCatalog Current;

        [Tooltip("Species that ship with the game (each with a 3D model and a seed picture).")]
        public List<PlantSpecies> builtIn = new List<PlantSpecies>();

        [Tooltip("Empty pot prefab placed in every slot.")]
        public GameObject potPrefab;

        [Tooltip("Seed picture for custom species (the empty seed bag).")]
        public Sprite customSeedSprite;

        [NonSerialized] readonly List<PlantSpecies> runtime = new List<PlantSpecies>();
        [NonSerialized] bool initialised;

        public event Action Changed;

        /// <summary>Runtime species list (copies - editing them never touches the assets).</summary>
        public IList<PlantSpecies> All
        {
            get
            {
                if (!initialised) InitRuntime(null);
                return runtime;
            }
        }

        public void InitRuntime(List<SpeciesConfig> saved)
        {
            foreach (var s in runtime)
                if (s != null) Destroy(s);
            runtime.Clear();
            foreach (var b in builtIn)
            {
                if (b == null) continue;
                var copy = Instantiate(b);
                copy.name = b.name;
                copy.id = b.Key;
                runtime.Add(copy);
            }
            initialised = true;

            if (saved != null)
            {
                foreach (var cfg in saved)
                {
                    if (cfg == null || string.IsNullOrEmpty(cfg.id)) continue;
                    var s = Find(cfg.id);
                    if (s == null && cfg.custom)
                    {
                        var template = Find(cfg.modelId);
                        if (template == null && runtime.Count > 0) template = runtime[0];
                        if (template == null) continue;
                        s = MakeCustom(template, cfg.id);
                    }
                    if (s != null) Apply(s, cfg);
                }
            }
            RaiseChanged();
        }

        public PlantSpecies Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in All)
                if (s != null && s.Key == id) return s;
            return null;
        }

        public PlantSpecies FindBuiltInAsset(string id)
        {
            foreach (var b in builtIn)
                if (b != null && b.Key == id) return b;
            return null;
        }

        /// <summary>The 3D prefab for a species (custom species borrow a built-in model).</summary>
        public GameObject ModelFor(PlantSpecies s)
        {
            if (s == null) return null;
            if (s.modelPrefab != null) return s.modelPrefab;
            var template = Find(s.modelId);
            return template != null ? template.modelPrefab : null;
        }

        public Sprite SeedSpriteFor(PlantSpecies s)
        {
            if (s == null) return customSeedSprite;
            return s.seedSprite != null ? s.seedSprite : customSeedSprite;
        }

        /// <summary>Create a new species that copies a built-in one (model, thresholds, prices).</summary>
        public PlantSpecies AddCustom(PlantSpecies template, string nameThai, string nameEnglish)
        {
            if (template == null) return null;
            var s = MakeCustom(template, "custom_" + DateTime.Now.Ticks.ToString("x"));
            s.nameThai = string.IsNullOrEmpty(nameThai) ? template.nameThai + " (ใหม่)" : nameThai;
            s.nameEnglish = string.IsNullOrEmpty(nameEnglish) ? template.nameEnglish + " (new)" : nameEnglish;
            RaiseChanged();
            return s;
        }

        PlantSpecies MakeCustom(PlantSpecies template, string id)
        {
            var s = Instantiate(template);
            s.id = id;
            s.name = id;
            s.isCustom = true;
            s.modelId = template.isCustom ? template.modelId : template.Key;
            s.modelPrefab = null;
            s.seedSprite = customSeedSprite;
            runtime.Add(s);
            return s;
        }

        public bool Remove(PlantSpecies s)
        {
            if (s == null || !s.isCustom) return false;
            runtime.Remove(s);
            RaiseChanged();
            return true;
        }

        /// <summary>Put a built-in species back to the values set in the editor.</summary>
        public void ResetToDefault(PlantSpecies s)
        {
            if (s == null) return;
            var asset = FindBuiltInAsset(s.Key);
            if (asset == null) return;
            Apply(s, ToConfig(asset));
            RaiseChanged();
        }

        public void RaiseChanged()
        {
            if (Changed != null) Changed();
        }

        public static SpeciesConfig ToConfig(PlantSpecies s)
        {
            return new SpeciesConfig
            {
                id = s.Key,
                custom = s.isCustom,
                modelId = s.modelId,
                nameThai = s.nameThai,
                nameEnglish = s.nameEnglish,
                moistureMin = s.moisture.min,
                moistureMax = s.moisture.max,
                temperatureMin = s.temperature.min,
                temperatureMax = s.temperature.max,
                lightMin = s.light.min,
                lightMax = s.light.max,
                seedPrice = s.seedPrice,
                sellPrice = s.sellPrice,
                dailyPoints = s.dailyPoints,
                growMinutes = s.growMinutes
            };
        }

        public static void Apply(PlantSpecies s, SpeciesConfig c)
        {
            if (!string.IsNullOrEmpty(c.nameThai)) s.nameThai = c.nameThai;
            if (!string.IsNullOrEmpty(c.nameEnglish)) s.nameEnglish = c.nameEnglish;
            if (c.moistureMax > c.moistureMin) s.moisture = new MetricRange(c.moistureMin, c.moistureMax);
            if (c.temperatureMax > c.temperatureMin) s.temperature = new MetricRange(c.temperatureMin, c.temperatureMax);
            if (c.lightMax > c.lightMin) s.light = new MetricRange(c.lightMin, c.lightMax);
            if (c.seedPrice > 0) s.seedPrice = c.seedPrice;
            if (c.sellPrice > 0) s.sellPrice = c.sellPrice;
            if (c.dailyPoints > 0) s.dailyPoints = c.dailyPoints;
            if (c.growMinutes > 0f) s.growMinutes = c.growMinutes;
        }

        public List<SpeciesConfig> ToConfigs()
        {
            var list = new List<SpeciesConfig>();
            foreach (var s in All)
                if (s != null) list.Add(ToConfig(s));
            return list;
        }
    }
}
