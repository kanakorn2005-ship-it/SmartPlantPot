using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Creates a PotLabel for every pot in the scene (runs at start).</summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Smart Plant Pot/Pot Label Layer")]
    public class PotLabelLayer : MonoBehaviour
    {
        public Camera worldCamera;

        void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            foreach (var pot in PlantGrowth.All) Create(pot);
        }

        void Create(PlantGrowth pot)
        {
            var skin = UISkin.Current;
            var space = (RectTransform)transform;

            var bg = UIFactory.Rounded(space, "Label_" + pot.plant.potId, skin.surface, 20f, true);
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            UIFactory.Shadow(rt, 14f, 0.14f, -3f);
            UIFactory.VColumn(bg.gameObject, 4f, new RectOffset(16, 16, 8, 10), TextAnchor.MiddleCenter);
            UIFactory.Fit(bg.gameObject, true, true);

            var row = UIFactory.Rect(rt, "Row");
            UIFactory.HRow(row.gameObject, 8f, new RectOffset(), TextAnchor.MiddleCenter);
            var dot = UIFactory.Circle(row, "Dot", skin.statusNormal, 14f);
            UIFactory.Layout(dot, 14f, 14f);
            var name = UIFactory.Text(row, "Name", "", 22, skin.textPrimary, TextAnchor.MiddleCenter, true);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var streak = UIFactory.Rect(row, "Streak");
            UIFactory.HRow(streak.gameObject, 2f, new RectOffset(), TextAnchor.MiddleCenter);
            var flame = UIFactory.Icon(streak, "Flame", skin.iconFlame, skin.flame, 22f);
            UIFactory.Layout(flame, 22f, 22f);
            var streakText = UIFactory.Text(streak, "Count", "0", 20, skin.flame, TextAnchor.MiddleLeft, true);
            streakText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var mutation = UIFactory.Icon(row, "Mutation", skin.iconSparkles, skin.gold, 22f);
            UIFactory.Layout(mutation, 22f, 22f);
            var servo = UIFactory.Icon(row, "Servo", skin.iconGauge, skin.water, 22f);
            UIFactory.Layout(servo, 22f, 22f);

            var bar = UIFactory.Rounded(rt, "GrowthBar", UISkin.WithAlpha(skin.textPrimary, 0.1f), 3f);
            UIFactory.Layout(bar, -1f, 7f);
            var fill = UIFactory.Rounded(bar.transform, "Fill", skin.leafColor, 3f);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.1f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var label = bg.gameObject.AddComponent<PotLabel>();
            label.pot = pot;
            label.cam = worldCamera;
            label.space = space;
            label.dot = dot;
            label.nameText = name;
            label.streakGroup = streak.gameObject;
            label.streakText = streakText;
            label.mutationIcon = mutation.gameObject;
            label.growthBar = bar.gameObject;
            label.growthFill = fill.rectTransform;
            label.servoIcon = servo;
        }
    }
}
