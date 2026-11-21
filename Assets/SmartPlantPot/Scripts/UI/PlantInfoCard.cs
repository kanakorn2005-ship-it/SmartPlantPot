using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Card "ระบบการกดที่ต้นไม้เพื่อแสดงค่า เรื่องความชื้น แสง อุณหภูมิ".
    /// Opens when a plant is selected and shows live moisture / temperature / light with the ideal
    /// range, an overall status line (e.g. "ควรรดน้ำ") and the Water / Prune buttons.
    /// Landscape: pops up beside the plant. Portrait: slides up as a bottom sheet.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Plant Info Card")]
    public class PlantInfoCard : PopupCardBase
    {
        [Header("Parts")]
        public Image titleDot;
        public Text title;
        public Text subtitle;
        public MetricRowView[] rows;
        public Image statusBanner;
        public Image statusIcon;
        public Text statusText;
        public Button waterButton;
        public Button pruneButton;
        public Button closeButton;
        [Tooltip("Optional (3D shop): fertilize button.")]
        public Button fertilizeButton;

        PlantUnit plant;
        PlantUnit watched;

        public PlantUnit Plant
        {
            get { return plant; }
        }

        protected override bool WantsOpen
        {
            get { return plant != null; }
        }

        protected override Transform Target
        {
            get { return plant != null ? plant.transform : null; }
        }

        void OnEnable()
        {
            PlantSelection.Changed += OnSelectionChanged;
            if (waterButton != null) waterButton.onClick.AddListener(OnWater);
            if (pruneButton != null) pruneButton.onClick.AddListener(OnPrune);
            if (fertilizeButton != null) fertilizeButton.onClick.AddListener(OnFertilize);
            if (closeButton != null) closeButton.onClick.AddListener(PlantSelection.Clear);
            OnSelectionChanged(PlantSelection.Current);
        }

        void OnDisable()
        {
            PlantSelection.Changed -= OnSelectionChanged;
            if (waterButton != null) waterButton.onClick.RemoveListener(OnWater);
            if (pruneButton != null) pruneButton.onClick.RemoveListener(OnPrune);
            if (fertilizeButton != null) fertilizeButton.onClick.RemoveListener(OnFertilize);
            if (closeButton != null) closeButton.onClick.RemoveListener(PlantSelection.Clear);
            Watch(null);
            if (plant != null) plant.ReadingChanged -= OnReading;
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
            if (plant != null) plant.ReadingChanged -= OnReading;
            // Empty pots get the seed-planting card instead (3D shop).
            plant = selected != null && !selected.IsEmpty ? selected : null;
            if (plant == null) return;

            plant.ReadingChanged += OnReading;
            if (rows != null)
                foreach (var r in rows)
                    if (r != null) r.SnapNext();
            Refresh();
            RestartPop();
            Position();
        }

        void OnReading(PlantUnit p)
        {
            Refresh();
        }

        void Refresh()
        {
            var skin = UISkin.Current;
            if (plant == null || skin == null) return;

            if (title != null) title.text = plant.DisplayName;
            if (subtitle != null)
            {
                string time = plant.HasReading ? plant.LastUpdate.ToString("HH:mm:ss") : "--:--:--";
                subtitle.text = plant.potId + "  ·  " + L.Updated(time);
            }

            if (rows != null)
                foreach (var r in rows)
                    if (r != null) r.Set(plant.Reading, plant.species, plant.GetStatus(r.metric));

            PlantMetric metric;
            MetricStatus status;
            bool issue = plant.TryGetMainIssue(out metric, out status);
            Color c = issue ? skin.StatusColor(status) : skin.statusNormal;
            if (titleDot != null) titleDot.color = c;
            if (statusBanner != null) statusBanner.color = UISkin.WithAlpha(c, 0.16f);
            if (statusIcon != null)
            {
                statusIcon.sprite = issue ? skin.iconAlert : skin.iconCheck;
                statusIcon.color = c;
            }
            if (statusText != null)
            {
                string text = issue ? L.Advice(metric, status) : L.T("ปกติ · ต้นไม้สบายดี", "Normal · this plant is happy");
                int extra = plant.IssueCount - 1;
                if (extra > 0) text += L.T("  (+อีก " + extra + " เรื่อง)", "  (+" + extra + " more)");
                statusText.text = text;
                statusText.color = issue ? Color.Lerp(c, skin.textPrimary, 0.35f) : skin.textPrimary;
            }
        }

        void OnWater()
        {
            PotInteraction.PerformAndNotify(plant, CareAction.Water);
        }

        void OnPrune()
        {
            PotInteraction.PerformAndNotify(plant, CareAction.Prune);
        }

        void OnFertilize()
        {
            PotInteraction.PerformAndNotify(plant, CareAction.Fertilize);
        }
    }
}
