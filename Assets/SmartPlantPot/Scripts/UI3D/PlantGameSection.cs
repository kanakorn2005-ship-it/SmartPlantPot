using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Game part of the plant info card (3D shop): growth stage, quality, care streak, mutation,
    /// servo valve state, and the two ways to sell (daily harvest / sell the whole plant).
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Plant Game Section")]
    public class PlantGameSection : MonoBehaviour
    {
        public Text stageText;
        public RectTransform growthFill;
        public Text growthText;
        public RectTransform qualityFill;
        public Text qualityText;
        public GameObject streakChip;
        public Text streakText;
        public GameObject mutationChip;
        public Text mutationText;
        public Text servoText;
        public Button harvestButton;
        public Button sellButton;

        PlantGrowth pot;
        float timer;

        void OnEnable()
        {
            PlantSelection.Changed += OnSelectionChanged;
            if (harvestButton != null) harvestButton.onClick.AddListener(OnHarvest);
            if (sellButton != null) sellButton.onClick.AddListener(OnSell);
            OnSelectionChanged(PlantSelection.Current);
        }

        void OnDisable()
        {
            PlantSelection.Changed -= OnSelectionChanged;
            if (harvestButton != null) harvestButton.onClick.RemoveListener(OnHarvest);
            if (sellButton != null) sellButton.onClick.RemoveListener(OnSell);
        }

        void OnSelectionChanged(PlantUnit unit)
        {
            pot = unit != null ? unit.GetComponent<PlantGrowth>() : null;
            Refresh();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 0.25f) return;
            timer = 0f;
            Refresh();
        }

        void Refresh()
        {
            var skin = UISkin.Current;
            if (pot == null || pot.IsEmpty || skin == null) return;
            var gm = GameManager.Instance;

            if (stageText != null) stageText.text = ThaiShaper.Fix(PlantGrowth.StageName(pot.Stage));

            SetBar(growthFill, pot.growth, pot.growth >= 1f ? skin.gold : skin.leafColor);
            if (growthText != null) growthText.text = Mathf.RoundToInt(pot.growth * 100f) + "%";
            Color qc = pot.quality >= 75f ? skin.statusNormal : pot.quality >= 45f ? skin.statusLow : skin.statusHigh;
            SetBar(qualityFill, pot.quality / 100f, qc);
            if (qualityText != null) qualityText.text = Mathf.RoundToInt(pot.quality) + "%";

            if (streakChip != null) streakChip.SetActive(true);
            if (streakText != null)
                streakText.text = ThaiShaper.Fix(pot.streak > 0
                    ? L.T("Streak " + pot.streak + " วัน  +" + Mathf.Min(pot.streak, 10) * 5 + "%", "Streak " + pot.streak + " days  +" + Mathf.Min(pot.streak, 10) * 5 + "%")
                    : L.T("วันนี้ดูแลดี " + Mathf.RoundToInt(pot.TodayAverageHealth * 100f) + "% (ต้องได้ 75%)", "Today's care " + Mathf.RoundToInt(pot.TodayAverageHealth * 100f) + "% (need 75%)"));

            if (mutationChip != null) mutationChip.SetActive(pot.mutation != Mutation.None);
            if (mutationText != null && pot.mutation != Mutation.None)
                mutationText.text = ThaiShaper.Fix(L.T("กลายพันธุ์: ", "Mutation: ") + MutationInfo.Name(pot.mutation) + "  x" + MutationInfo.Multiplier(pot.mutation).ToString("0.#"));

            var servo = pot.GetComponent<PotServo>();
            if (servoText != null && servo != null)
            {
                string s = "Servo " + Mathf.RoundToInt(servo.Angle) + "°  ·  " + PotServo.StateName(servo.Current);
                if (servo.IsOpen && servo.SecondsLeft > 0f) s += " (" + servo.SecondsLeft.ToString("0.0") + "s)";
                if (servo.autoWater) s += L.T("  ·  รดน้ำอัตโนมัติ", "  ·  auto-water");
                if (pot.IsFertilized) s += L.T("  ·  ปุ๋ยโตเร็ว x2 อีก ", "  ·  fertilizer x2 for ") + Mathf.CeilToInt(pot.fertilizerMinutesLeft) + L.T(" นาที", " min");
                servoText.text = ThaiShaper.Fix(s);
            }

            if (gm != null)
            {
                if (harvestButton != null)
                {
                    bool can = pot.CanDailySell;
                    harvestButton.interactable = can;
                    UIKit.SetButtonLabel(harvestButton, pot.SoldToday ? L.SoldToday
                        : pot.growth < 0.5f ? L.T("ขายผลผลิตได้เมื่อเป็นต้นอ่อน", "Harvest from the young stage")
                        : L.SellHarvest(gm.PreviewDailySell(pot)));
                }
                if (sellButton != null) UIKit.SetButtonLabel(sellButton, L.SellPlant(gm.PreviewSellPlant(pot)));
            }
        }

        static void SetBar(RectTransform fill, float value, Color color)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            var img = fill.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        void OnHarvest()
        {
            var gm = GameManager.Instance;
            if (gm == null || pot == null) return;
            int earned;
            string msg;
            bool ok = gm.DailySell(pot, out earned, out msg);
            if (NotificationCenter.Instance != null && !string.IsNullOrEmpty(msg))
                NotificationCenter.Instance.PostInfo(pot.plant, ok ? "+" + earned + " " + L.Points : pot.plant.DisplayName, msg, ok ? AlertKind.Reward : AlertKind.Refused);
            if (ok && EffectsLayer.Instance != null) EffectsLayer.Instance.Sparkles(pot.transform, UISkin.Current.gold, 10);
            Refresh();
        }

        void OnSell()
        {
            var gm = GameManager.Instance;
            if (gm == null || pot == null || pot.IsEmpty) return;
            var target = pot;
            int price = gm.PreviewSellPlant(target);
            string detail = L.T(
                "คุณภาพ " + Mathf.RoundToInt(target.quality) + "%  ·  " + PlantGrowth.StageName(target.Stage) +
                (target.streak > 0 ? "  ·  Streak " + target.streak + " วัน" : "") +
                (target.mutation != Mutation.None ? "  ·  " + MutationInfo.Name(target.mutation) : "") +
                "\nต้นไม้จะถูกขายออกไปและกระถางจะว่าง",
                "Quality " + Mathf.RoundToInt(target.quality) + "%  ·  " + PlantGrowth.StageName(target.Stage) +
                (target.streak > 0 ? "  ·  Streak " + target.streak : "") +
                (target.mutation != Mutation.None ? "  ·  " + MutationInfo.Name(target.mutation) : "") +
                "\nThe plant leaves and the pot becomes empty.");
            ConfirmDialog.Show(L.T("ขาย" + target.Species.DisplayName + " " + price + " แต้ม?", "Sell the " + target.Species.DisplayName + " for " + price + "?"),
                detail, L.T("ขายเลย", "Sell"), () =>
                {
                    int earned;
                    string msg;
                    var unit = target.plant;
                    bool ok = gm.SellPlant(target, out earned, out msg);
                    if (NotificationCenter.Instance != null && !string.IsNullOrEmpty(msg))
                        NotificationCenter.Instance.PostInfo(unit, "+" + earned + " " + L.Points, msg, ok ? AlertKind.Reward : AlertKind.Refused);
                    if (ok && EffectsLayer.Instance != null) EffectsLayer.Instance.Sparkles(unit.transform, UISkin.Current.gold, 14);
                });
        }
    }
}
