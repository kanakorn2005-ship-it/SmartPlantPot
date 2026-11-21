using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Shared behaviour for cards that pop up next to a plant: pop-in animation, beside-the-plant
    /// placement in landscape, bottom sheet in portrait. Works for 2D slots and 3D pots.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupCardBase : MonoBehaviour
    {
        [Header("Placement")]
        public float landscapeWidth = 600f;
        public float gap = 28f;
        public float margin = 24f;
        [Tooltip("Space kept free at the top for the header.")]
        public float topReserved = 140f;

        protected RectTransform card;
        protected CanvasGroup group;
        protected float openAmount;

        /// <summary>Should the card be visible right now?</summary>
        protected abstract bool WantsOpen { get; }

        /// <summary>The object the card sits next to.</summary>
        protected abstract Transform Target { get; }

        public bool IsOpen
        {
            get { return WantsOpen; }
        }

        protected virtual void Awake()
        {
            card = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            card.localScale = Vector3.one * 0.94f;
        }

        /// <summary>Replay the pop-in, e.g. when switching to another plant.</summary>
        protected void RestartPop()
        {
            openAmount = Mathf.Min(openAmount, 0.35f);
        }

        protected virtual void LateUpdate()
        {
            bool open = WantsOpen;
            openAmount = Mathf.MoveTowards(openAmount, open ? 1f : 0f, Time.unscaledDeltaTime / 0.2f);
            group.alpha = openAmount;
            group.blocksRaycasts = openAmount > 0.5f && open;
            group.interactable = group.blocksRaycasts;
            float s = Mathf.LerpUnclamped(0.94f, 1f, UITween.EaseOutBack(openAmount));
            card.localScale = new Vector3(s, s, 1f);
            if (open) Position();
        }

        protected void Position()
        {
            var target = Target;
            if (card == null || target == null) return;
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

            Rect t = ScreenSpace.ToLocal(parent, ScreenSpace.RectOf(target));
            float w = landscapeWidth;
            float h = card.rect.height;
            float minY = margin + h * 0.5f;
            float maxY = Mathf.Max(minY, pr.height - topReserved - h * 0.5f);
            float y = Mathf.Clamp(t.center.y - pr.yMin, minY, maxY);

            float rightX = t.xMax - pr.xMin + gap;
            float leftX = t.xMin - pr.xMin - gap;
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
                float x = Mathf.Clamp(t.center.x - pr.xMin, margin + w * 0.5f, Mathf.Max(margin + w * 0.5f, pr.width - margin - w * 0.5f));
                card.anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
