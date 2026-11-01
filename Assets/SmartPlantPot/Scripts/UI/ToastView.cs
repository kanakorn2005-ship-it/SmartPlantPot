using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>A single toast. Built from code so it can be spawned at runtime.</summary>
    public class ToastView : MonoBehaviour, IPointerClickHandler
    {
        PlantAlert alert;
        CanvasGroup group;
        bool dismissing;

        public static Color KindColor(PlantAlert a, UISkin skin)
        {
            switch (a.kind)
            {
                case AlertKind.Warning: return skin.StatusColor(a.status);
                case AlertKind.Recovered: return skin.statusNormal;
                case AlertKind.Care: return skin.accent;
                default: return skin.statusLow;
            }
        }

        public static Sprite KindIcon(PlantAlert a, UISkin skin)
        {
            switch (a.kind)
            {
                case AlertKind.Warning: return skin.iconAlert;
                case AlertKind.Recovered: return skin.iconCheck;
                case AlertKind.Care: return skin.iconCare;
                default: return skin.iconInfo;
            }
        }

        public static ToastView Create(RectTransform parent, PlantAlert alert)
        {
            var skin = UISkin.Current;
            Color c = KindColor(alert, skin);

            var bg = UIFactory.Rounded(parent, "Toast", skin.surface, 22f, true);
            UIFactory.Shadow(bg.rectTransform, 22f, 0.14f, -6f);
            UIFactory.HRow(bg.gameObject, 14f, new RectOffset(14, 20, 14, 14), TextAnchor.MiddleLeft);

            var stripe = UIFactory.Rounded(bg.transform, "Stripe", c, 4f);
            UIFactory.Layout(stripe, 8f, -1f, -1f, 1f);

            var iconBg = UIFactory.Circle(bg.transform, "IconBg", UISkin.WithAlpha(c, 0.16f), 52f);
            UIFactory.Layout(iconBg, 52f, 52f);
            var icon = UIFactory.Icon(iconBg.transform, "Icon", KindIcon(alert, skin), c, 30f);
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));

            var col = UIFactory.Rect(bg.transform, "Text");
            UIFactory.VColumn(col.gameObject, 2f, null);
            UIFactory.Layout(col, -1f, -1f, 1f, -1f);
            UIFactory.Text(col, "Title", alert.title, 23, skin.textPrimary, TextAnchor.UpperLeft, true);
            UIFactory.Text(col, "Message", alert.message, 20, skin.textMuted, TextAnchor.UpperLeft);

            var view = bg.gameObject.AddComponent<ToastView>();
            view.alert = alert;
            view.group = bg.gameObject.AddComponent<CanvasGroup>();
            view.group.alpha = 0f;
            return view;
        }

        public void Play(float seconds)
        {
            var rt = (RectTransform)transform;
            rt.localScale = Vector3.one * 0.9f;
            UITween.Run(this, 0.25f, p =>
            {
                if (dismissing) return;
                group.alpha = p;
                float s = Mathf.LerpUnclamped(0.9f, 1f, UITween.EaseOutBack(p));
                rt.localScale = new Vector3(s, s, 1f);
            });
            UITween.Run(this, 0.01f, p => { }, Dismiss, seconds);
        }

        public void Dismiss()
        {
            if (dismissing || this == null) return;
            dismissing = true;
            float start = group != null ? group.alpha : 1f;
            UITween.Run(this, 0.2f, p =>
            {
                if (group != null) group.alpha = start * (1f - p);
            }, () =>
            {
                if (this != null) Destroy(gameObject);
            });
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (alert != null && alert.plant != null) PlantSelection.Select(alert.plant);
            Dismiss();
        }
    }
}
