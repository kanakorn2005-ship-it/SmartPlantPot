using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// The look of one plant on the shelf: art, name pill, status glow and the "!" badge that stays
    /// until a problem is fixed (toasts disappear, this doesn't).
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Plant Slot View")]
    public class PlantSlotView : MonoBehaviour
    {
        public PlantUnit plant;
        public Image art;
        public Image statusGlow;
        public Image namePill;
        public Text nameLabel;
        public RectTransform alertBadge;
        public Image alertBadgeBackground;

        bool selected;
        float glowAlpha;

        void Reset()
        {
            plant = GetComponent<PlantUnit>();
        }

        void OnEnable()
        {
            if (plant == null) plant = GetComponent<PlantUnit>();
            if (plant != null) plant.ReadingChanged += OnReading;
            PlantSelection.Changed += OnSelection;
            selected = PlantSelection.Current == plant && plant != null;
            Refresh();
        }

        void OnDisable()
        {
            if (plant != null) plant.ReadingChanged -= OnReading;
            PlantSelection.Changed -= OnSelection;
        }

        void OnReading(PlantUnit p)
        {
            Refresh();
        }

        void OnSelection(PlantUnit p)
        {
            selected = p == plant && plant != null;
            Refresh();
        }

        public void Refresh()
        {
            var skin = UISkin.Current;
            if (plant == null || skin == null) return;

            if (art != null && plant.species != null && plant.species.sprite != null && art.sprite != plant.species.sprite)
                art.sprite = plant.species.sprite;
            if (nameLabel != null) nameLabel.text = plant.DisplayName;

            PlantMetric metric;
            MetricStatus status;
            bool hasIssue = plant.TryGetMainIssue(out metric, out status);

            Color glow = hasIssue ? skin.StatusColor(status) : (selected ? skin.accent : skin.statusNormal);
            glowAlpha = hasIssue ? 0.55f : (selected ? 0.5f : 0.22f);
            if (statusGlow != null) statusGlow.color = UISkin.WithAlpha(glow, glowAlpha);

            if (namePill != null) namePill.color = selected ? skin.accent : skin.surface;
            if (nameLabel != null) nameLabel.color = selected ? Color.white : skin.textPrimary;

            if (alertBadge != null)
            {
                alertBadge.gameObject.SetActive(hasIssue);
                if (alertBadgeBackground != null) alertBadgeBackground.color = skin.StatusColor(status);
            }
        }

        void Update()
        {
            float t = Time.unscaledTime;
            if (alertBadge != null && alertBadge.gameObject.activeSelf)
            {
                float s = 1f + 0.08f * Mathf.Sin(t * 6f);
                alertBadge.localScale = new Vector3(s, s, 1f);
            }
            if (statusGlow != null && selected)
            {
                var c = statusGlow.color;
                c.a = glowAlpha * (0.75f + 0.25f * Mathf.Sin(t * 3f));
                statusGlow.color = c;
            }
        }
    }
}
