using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Header bell: unread-count badge, wiggles on new warnings, opens the history panel.</summary>
    [AddComponentMenu("Smart Plant Pot/Notification Bell")]
    public class NotificationBell : MonoBehaviour
    {
        public Button button;
        public RectTransform icon;
        public GameObject badge;
        public Text badgeText;
        public NotificationHistoryPanel panel;

        NotificationCenter center;
        float wiggleStart = -10f;

        void Start()
        {
            center = NotificationCenter.Instance;
            if (center != null)
            {
                center.HistoryChanged += Refresh;
                center.AlertRaised += OnAlert;
            }
            if (button != null) button.onClick.AddListener(OnClick);
            Refresh();
        }

        void OnDestroy()
        {
            if (center != null)
            {
                center.HistoryChanged -= Refresh;
                center.AlertRaised -= OnAlert;
            }
        }

        void OnClick()
        {
            if (panel != null) panel.Toggle();
        }

        void OnAlert(PlantAlert alert)
        {
            if (alert.kind == AlertKind.Warning) wiggleStart = Time.unscaledTime;
        }

        void Refresh()
        {
            int n = center != null ? center.UnreadCount : 0;
            if (badge != null) badge.SetActive(n > 0);
            if (badgeText != null) badgeText.text = n > 9 ? "9+" : n.ToString();
        }

        void Update()
        {
            if (icon == null) return;
            float t = Time.unscaledTime - wiggleStart;
            float angle = t < 0.8f ? Mathf.Sin(t * 30f) * 18f * (1f - t / 0.8f) : 0f;
            icon.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
