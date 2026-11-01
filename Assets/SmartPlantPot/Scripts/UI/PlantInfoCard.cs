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
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Smart Plant Pot/Plant Info Card")]
    public class PlantInfoCard : MonoBehaviour
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

        [Header("Placement")]
        public float landscapeWidth = 600f;
        public float gap = 28f;
        public float margin = 24f;
        [Tooltip("Space kept free at the top for the header.")]
        public float topReserved = 140f;

        RectTransform card;
        CanvasGroup group;
        PlantUnit plant;
        float openAmount;

        public PlantUnit Plant
        {
            get { return plant; }
        }

        public bool IsOpen
        {
            get { return plant != null; }
        }

        void Awake()
        {
            card = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            card.localScale = Vector3.one * 0.94f;
        }

        void OnEnable()
        {
            PlantSelection.Changed += OnSelectionChanged;
            if (waterButton != null) waterButton.onClick.AddListener(OnWater);
            if (pruneButton != null) pruneButton.onClick.AddListener(OnPrune);
            if (closeButton != null) closeButton.onClick.AddListener(PlantSelection.Clear);
            OnSelectionChanged(PlantSelection.Current);
        }

        void OnDisable()
        {
            PlantSelection.Changed -= OnSelectionChanged;
            if (waterButton != null) waterButton.onClick.RemoveListener(OnWater);
            if (pruneButton != null) pruneButton.onClick.RemoveListener(OnPrune);
            if (closeButton != null) closeButton.onClick.RemoveListener(PlantSelection.Clear);
            if (plant != null) plant.ReadingChanged -= OnReading;
        }

        void OnSelectionChanged(PlantUnit selected)
        {
            if (plant != null) plant.ReadingChanged -= OnReading;
            plant = selected;
            if (plant == null) return;

            plant.ReadingChanged += OnReading;
            if (rows != null)
                foreach (var r in rows)
                    if (r != null) r.SnapNext();
            Refresh();
            // Restart the pop-in so switching plants feels responsive.
            openAmount = Mathf.Min(openAmount, 0.35f);
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

        void LateUpdate()
        {
            float target = plant != null ? 1f : 0f;
            openAmount = Mathf.MoveTowards(openAmount, target, Time.unscaledDeltaTime / 0.2f);
            group.alpha = openAmount;
            group.blocksRaycasts = openAmount > 0.5f && plant != null;
            group.interactable = group.blocksRaycasts;
            float s = Mathf.LerpUnclamped(0.94f, 1f, UITween.EaseOutBack(openAmount));
            card.localScale = new Vector3(s, s, 1f);

            if (plant != null) Position();
        }

        void Position()
        {
            if (card == null || plant == null) return;
            var parent = card.parent as RectTransform;
            if (parent == null) return;
            Rect pr = parent.rect;
            bool portrait = ResponsiveLayout.Instance != null && ResponsiveLayout.Instance.IsPortrait;

            if (portrait)
            {
                card.anchorMin = new Vector2(0f, 0f);
                card.anchorMax = new Vector2(1f, 0f);
                card.pivot = new Vector2(0.5f, 0f);
                card.sizeDelta = new Vector2(-margin * 2f, card.sizeDelta.y);
                card.anchoredPosition = new Vector2(0f, margin);
                return;
            }

            card.anchorMin = Vector2.zero;
            card.anchorMax = Vector2.zero;
            card.sizeDelta = new Vector2(landscapeWidth, card.sizeDelta.y);

            Rect target = ScreenSpace.ToLocal(parent, ScreenSpace.RectOf(plant.transform));
            float w = landscapeWidth;
            float h = card.rect.height;
            float minY = margin + h * 0.5f;
            float maxY = Mathf.Max(minY, pr.height - topReserved - h * 0.5f);
            float y = Mathf.Clamp(target.center.y - pr.yMin, minY, maxY);

            float rightX = target.xMax - pr.xMin + gap;
            float leftX = target.xMin - pr.xMin - gap;
            if (rightX + w <= pr.width - margin)
            {
                card.pivot = new Vector2(0f, 0.5f);
                card.anchoredPosition = new Vector2(rightX, y);
            }
            else if (leftX - w >= margin)
            {
                card.pivot = new Vector2(1f, 0.5f);
                card.anchoredPosition = new Vector2(leftX, y);
            }
            else
            {
                card.pivot = new Vector2(0.5f, 0.5f);
                float x = Mathf.Clamp(target.center.x - pr.xMin, margin + w * 0.5f, Mathf.Max(margin + w * 0.5f, pr.width - margin - w * 0.5f));
                card.anchoredPosition = new Vector2(x, y);
            }
        }

        void OnWater()
        {
            DoCare(CareAction.Water);
        }

        void OnPrune()
        {
            DoCare(CareAction.Prune);
        }

        void DoCare(CareAction action)
        {
            PotInteraction.PerformAndNotify(plant, action);
        }
    }
}
