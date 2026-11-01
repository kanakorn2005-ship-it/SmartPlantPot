using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Scrollable list of past alerts and care actions. Opened from the bell.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Smart Plant Pot/Notification History Panel")]
    public class NotificationHistoryPanel : MonoBehaviour
    {
        public RectTransform content;
        public ScrollRect scroll;
        public GameObject emptyState;
        public Button clearButton;
        public Button closeButton;

        CanvasGroup group;
        NotificationCenter center;
        bool open;
        bool dirty = true;
        float amount;

        public bool IsOpen
        {
            get { return open; }
        }

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void Start()
        {
            center = NotificationCenter.Instance;
            if (center != null) center.HistoryChanged += OnHistoryChanged;
            if (clearButton != null) clearButton.onClick.AddListener(OnClear);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        void OnDestroy()
        {
            if (center != null) center.HistoryChanged -= OnHistoryChanged;
        }

        void OnHistoryChanged()
        {
            dirty = true;
            // Alerts that arrive while the list is open count as seen (guarded: MarkAllRead raises this event too).
            if (open && center != null && center.UnreadCount > 0) center.MarkAllRead();
        }

        void OnClear()
        {
            if (center != null) center.ClearHistory();
        }

        public void Toggle()
        {
            if (open) Close();
            else Open();
        }

        public void Open()
        {
            open = true;
            transform.SetAsLastSibling();
            dirty = true;
            if (center != null) center.MarkAllRead();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            open = false;
        }

        void LateUpdate()
        {
            amount = Mathf.MoveTowards(amount, open ? 1f : 0f, Time.unscaledDeltaTime / 0.18f);
            group.alpha = amount;
            group.blocksRaycasts = open;
            group.interactable = open;
            float s = Mathf.Lerp(0.96f, 1f, UITween.EaseOutCubic(amount));
            transform.localScale = new Vector3(s, s, 1f);

            if (open && dirty) Rebuild();
        }

        void Rebuild()
        {
            dirty = false;
            if (content == null || center == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            if (emptyState != null) emptyState.SetActive(center.History.Count == 0);
            foreach (var alert in center.History)
                CreateRow(alert);
        }

        void CreateRow(PlantAlert alert)
        {
            var skin = UISkin.Current;
            Color c = ToastView.KindColor(alert, skin);

            var bg = UIFactory.Rounded(content, "Row", UISkin.WithAlpha(skin.background, 0.9f), 18f, true);
            UIFactory.HRow(bg.gameObject, 14f, new RectOffset(16, 16, 14, 14), TextAnchor.MiddleLeft);

            var iconBg = UIFactory.Circle(bg.transform, "IconBg", UISkin.WithAlpha(c, 0.18f), 46f);
            UIFactory.Layout(iconBg, 46f, 46f);
            var icon = UIFactory.Icon(iconBg.transform, "Icon", ToastView.KindIcon(alert, skin), c, 26f);
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));

            var col = UIFactory.Rect(bg.transform, "Text");
            UIFactory.VColumn(col.gameObject, 2f, null);
            UIFactory.Layout(col, -1f, -1f, 1f, -1f);
            UIFactory.Text(col, "Title", alert.title, 21, skin.textPrimary, TextAnchor.UpperLeft, true);
            UIFactory.Text(col, "Message", alert.message, 18, skin.textMuted, TextAnchor.UpperLeft);

            var time = UIFactory.Text(bg.transform, "Time", alert.time.ToString("HH:mm"), 18, skin.textMuted, TextAnchor.UpperRight);
            UIFactory.Layout(time, 64f, -1f);

            var click = bg.gameObject.AddComponent<HistoryRowClick>();
            click.plant = alert.plant;
            click.panel = this;
        }
    }
}
