using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Card for an empty pot: soil reading + the seeds you own. Tap a seed to plant it (the 3D
    /// plant starts as a tiny sprout). No seeds yet? A button opens the seed shop.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Empty Pot Card")]
    public class EmptyPotCard : PopupCardBase
    {
        public Text title;
        public Text subtitle;
        public RectTransform seedGrid;
        public GameObject emptyState;
        public Button shopButton;
        public Button closeButton;
        public SeedShopPanel shop;

        PlantUnit pot;
        PlantUnit watched;
        float timer;

        protected override bool WantsOpen
        {
            get { return pot != null; }
        }

        protected override Transform Target
        {
            get { return pot != null ? pot.transform : null; }
        }

        void OnEnable()
        {
            PlantSelection.Changed += OnSelectionChanged;
            if (shopButton != null) shopButton.onClick.AddListener(OpenShop);
            if (closeButton != null) closeButton.onClick.AddListener(PlantSelection.Clear);
            OnSelectionChanged(PlantSelection.Current);
        }

        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.Changed += Rebuild;
        }

        void OnDisable()
        {
            PlantSelection.Changed -= OnSelectionChanged;
            if (shopButton != null) shopButton.onClick.RemoveListener(OpenShop);
            if (closeButton != null) closeButton.onClick.RemoveListener(PlantSelection.Clear);
            Watch(null);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.Changed -= Rebuild;
        }

        void OpenShop()
        {
            if (shop != null) shop.Open();
        }

        void Watch(PlantUnit unit)
        {
            if (watched != null) watched.SpeciesChanged -= OnSpeciesChanged;
            watched = unit;
            if (watched != null) watched.SpeciesChanged += OnSpeciesChanged;
        }

        void OnSpeciesChanged(PlantUnit unit)
        {
            OnSelectionChanged(PlantSelection.Current);
        }

        void OnSelectionChanged(PlantUnit selected)
        {
            Watch(selected);
            bool hasGrowth = selected != null && selected.GetComponent<PlantGrowth>() != null;
            pot = selected != null && selected.IsEmpty && hasGrowth ? selected : null;
            if (pot == null) return;
            Rebuild();
            RestartPop();
            Position();
        }

        void Update()
        {
            if (pot == null) return;
            timer += Time.unscaledDeltaTime;
            if (timer < 0.5f) return;
            timer = 0f;
            RefreshSubtitle();
        }

        void RefreshSubtitle()
        {
            if (subtitle == null || pot == null) return;
            subtitle.text = pot.HasReading
                ? L.T("ดินตอนนี้: ความชื้น ", "Soil now: moisture ") + PlantHealth.Format(PlantMetric.Moisture, pot.Reading.moisture) +
                  "  ·  " + PlantHealth.Format(PlantMetric.Temperature, pot.Reading.temperature)
                : L.T("รอข้อมูลเซนเซอร์...", "Waiting for sensor data...");
        }

        void Rebuild()
        {
            if (pot == null || seedGrid == null) return;
            var skin = UISkin.Current;
            var gm = GameManager.Instance;
            var catalog = PlantCatalog.Current;
            if (title != null) title.text = L.EmptyPot + " · " + pot.potId;
            RefreshSubtitle();

            UIKit.Clear(seedGrid);
            int shown = 0;
            if (gm != null && catalog != null)
            {
                foreach (var s in catalog.All)
                {
                    int n = gm.SeedCount(s);
                    if (n <= 0) continue;
                    shown++;
                    var species = s;
                    var cardBg = UIFactory.Rounded(seedGrid, "Seed_" + s.Key, Color.white, 16f, true);
                    UIFactory.VColumn(cardBg.gameObject, 2f, new RectOffset(8, 8, 8, 8), TextAnchor.UpperCenter);
                    var img = UIFactory.Image(cardBg.transform, "Packet", catalog.SeedSpriteFor(s), Color.white);
                    img.preserveAspect = true;
                    UIFactory.Layout(img, -1f, 92f);
                    var nm = UIFactory.Text(cardBg.transform, "Name", s.DisplayName, 17, skin.textPrimary, TextAnchor.MiddleCenter, true);
                    UIFactory.Layout(nm, -1f, 26f);
                    var cnt = UIFactory.Text(cardBg.transform, "Count", L.Owned(n), 15, skin.textMuted, TextAnchor.MiddleCenter);
                    UIFactory.Layout(cnt, -1f, 20f);
                    var btn = cardBg.gameObject.AddComponent<Button>();
                    btn.targetGraphic = cardBg;
                    btn.onClick.AddListener(() => PlantNow(species));
                }
            }
            if (emptyState != null) emptyState.SetActive(shown == 0);
            seedGrid.gameObject.SetActive(shown > 0);
        }

        void PlantNow(PlantSpecies s)
        {
            var gm = GameManager.Instance;
            if (gm == null || pot == null) return;
            var growth = pot.GetComponent<PlantGrowth>();
            var unit = pot;
            string msg;
            bool ok = gm.PlantSeed(growth, s, out msg);
            if (NotificationCenter.Instance != null && !string.IsNullOrEmpty(msg))
                NotificationCenter.Instance.PostInfo(unit, ok ? L.Plant + " · " + s.DisplayName : L.EmptyPot, msg, ok ? AlertKind.Care : AlertKind.Refused);
            if (ok)
            {
                var fx = unit.GetComponent<PotInteraction>();
                if (EffectsLayer.Instance != null) EffectsLayer.Instance.Sparkles(unit.transform, UISkin.Current.leafColor, 8);
                if (fx != null) fx.Poke();
                // keep the pot selected: the info card takes over now that it has a plant
                PlantSelection.Clear();
                PlantSelection.Select(unit);
            }
        }
    }
}
