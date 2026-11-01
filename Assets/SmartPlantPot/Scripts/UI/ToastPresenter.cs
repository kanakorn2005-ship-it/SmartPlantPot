using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Shows alerts as stacked toasts (newest on top). Tap a toast to jump to that plant.</summary>
    [AddComponentMenu("Smart Plant Pot/Toast Presenter")]
    public class ToastPresenter : MonoBehaviour
    {
        [Tooltip("Vertical layout container for the toasts.")]
        public RectTransform stack;
        public int maxVisible = 3;
        public float warningSeconds = 6f;
        public float infoSeconds = 3.5f;

        readonly List<ToastView> live = new List<ToastView>();
        NotificationCenter center;

        void Start()
        {
            if (stack == null) stack = (RectTransform)transform;
            center = NotificationCenter.Instance;
            if (center != null) center.AlertRaised += Show;
        }

        void OnDestroy()
        {
            if (center != null) center.AlertRaised -= Show;
        }

        void Show(PlantAlert alert)
        {
            live.RemoveAll(t => t == null);
            var toast = ToastView.Create(stack, alert);
            toast.transform.SetAsFirstSibling();
            live.Insert(0, toast);
            while (live.Count > maxVisible)
            {
                live[live.Count - 1].Dismiss();
                live.RemoveAt(live.Count - 1);
            }
            toast.Play(alert.kind == AlertKind.Warning ? warningSeconds : infoSeconds);
        }
    }
}
