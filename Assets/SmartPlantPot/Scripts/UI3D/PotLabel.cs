using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Floating name tag above a 3D pot: status dot, name (or "กระถางว่าง"), growth bar, streak
    /// flame and mutation sparkle. Tap it to select the pot. Built by PotLabelLayer.
    /// </summary>
    public class PotLabel : MonoBehaviour, IPointerClickHandler
    {
        public PlantGrowth pot;
        public Camera cam;
        public RectTransform space;
        public float worldLift = 0.03f;

        public Image dot;
        public Text nameText;
        public GameObject streakGroup;
        public Text streakText;
        public GameObject mutationIcon;
        public RectTransform growthFill;
        public GameObject growthBar;
        public Image servoIcon;

        RectTransform rt;
        CanvasGroup group;
        float timer;

        void Awake()
        {
            rt = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        void LateUpdate()
        {
            if (pot == null || cam == null || space == null) return;

            Bounds b;
            Vector3 world = ScreenSpace.WorldBounds(pot.transform, out b)
                ? new Vector3(b.center.x, b.max.y + worldLift, b.center.z)
                : pot.transform.position + Vector3.up * 0.3f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0f;
            group.alpha = visible ? 1f : 0f;
            if (!visible) return;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(space, sp, null, out local);
            rt.anchoredPosition = local;

            timer += Time.unscaledDeltaTime;
            if (timer >= 0.2f)
            {
                timer = 0f;
                Refresh();
            }
        }

        void Refresh()
        {
            var skin = UISkin.Current;
            if (skin == null) return;
            var unit = pot.plant;
            bool empty = pot.IsEmpty;
            bool selected = PlantSelection.Current == unit;

            nameText.text = empty ? L.EmptyPot + " · " + unit.potId : unit.DisplayName;

            PlantMetric m;
            MetricStatus st;
            Color c = empty ? skin.textMuted : (unit.TryGetMainIssue(out m, out st) ? skin.StatusColor(st) : skin.statusNormal);
            dot.color = c;
            var bg = GetComponent<Image>();
            if (bg != null) bg.color = selected ? skin.accent : skin.surface;
            nameText.color = selected ? Color.white : skin.textPrimary;

            if (growthBar != null) growthBar.SetActive(!empty);
            if (growthFill != null)
            {
                growthFill.anchorMax = new Vector2(Mathf.Clamp01(pot.growth), 1f);
                growthFill.GetComponent<Image>().color = pot.growth >= 1f ? skin.gold : skin.leafColor;
            }

            if (streakGroup != null) streakGroup.SetActive(!empty && pot.streak > 0);
            if (streakText != null) streakText.text = pot.streak.ToString();
            if (mutationIcon != null) mutationIcon.SetActive(!empty && pot.mutation != Mutation.None);

            var servo = pot.GetComponent<PotServo>();
            if (servoIcon != null) servoIcon.gameObject.SetActive(servo != null && servo.IsOpen);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (pot != null) PlantSelection.Toggle(pot.plant);
        }
    }
}
