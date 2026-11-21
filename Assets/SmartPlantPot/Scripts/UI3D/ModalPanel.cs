using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Base for full-screen modal panels (shop, settings, confirm). Dims the 3D view, closes with
    /// Esc / the X button, and only one is on top at a time.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ModalPanel : MonoBehaviour
    {
        static readonly List<ModalPanel> open = new List<ModalPanel>();

        public RectTransform window;
        public Button closeButton;

        protected CanvasGroup group;
        float amount;
        bool isOpen;

        public bool IsOpen
        {
            get { return isOpen; }
        }

        public static bool AnyOpen
        {
            get { return open.Count > 0; }
        }

        public static bool CloseTop()
        {
            if (open.Count == 0) return false;
            open[open.Count - 1].Close();
            return true;
        }

        protected virtual void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public virtual void Open()
        {
            if (!open.Contains(this)) open.Add(this);
            isOpen = true;
            transform.SetAsLastSibling();
            OnOpened();
        }

        public virtual void Close()
        {
            open.Remove(this);
            isOpen = false;
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        protected virtual void OnOpened()
        {
        }

        protected virtual void LateUpdate()
        {
            amount = Mathf.MoveTowards(amount, isOpen ? 1f : 0f, Time.unscaledDeltaTime / 0.18f);
            group.alpha = amount;
            group.blocksRaycasts = isOpen;
            group.interactable = isOpen;
            if (window != null)
            {
                float s = Mathf.LerpUnclamped(0.94f, 1f, UITween.EaseOutBack(amount));
                window.localScale = new Vector3(s, s, 1f);
            }
        }

        protected virtual void OnDestroy()
        {
            open.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            open.Clear();
        }
    }
}
