using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Card "จัดเรียงรูป 2D ให้เข้ากับหน้าจอ": fits the 2D plant art and UI to any screen.
    /// - Swaps the CanvasScaler reference between landscape (1920x1080) and portrait (1080x1920)
    /// - Sizes every plant slot so all pots fit on one shelf without distortion
    /// - Moves the mascot, toasts and notification panel for portrait vs landscape
    /// Re-runs automatically whenever the screen / window size changes.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Smart Plant Pot/Responsive Layout")]
    public class ResponsiveLayout : MonoBehaviour
    {
        public static ResponsiveLayout Instance { get; private set; }
        public static event Action<bool> OrientationChanged;

        [Header("Canvas scaling")]
        public CanvasScaler scaler;
        public Vector2 landscapeReference = new Vector2(1920f, 1080f);
        public Vector2 portraitReference = new Vector2(1080f, 1920f);
        [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

        [Header("Plant shelf")]
        public RectTransform shelfArea;
        public HorizontalLayoutGroup plantRow;
        public RectTransform plank;
        public RectTransform plankLip;
        [Tooltip("Width / height of the plant art (all plant PNGs share one canvas size).")]
        public float plantAspect = 364f / 532f;
        public float labelHeight = 64f;
        public float maxPlantHeight = 560f;
        [Range(0f, 0.2f)] public float spacingRatio = 0.06f;

        [Header("Landscape margins")]
        public float headerHeight = 120f;
        public float landscapeSideGutter = 250f;
        public float landscapeBottom = 40f;

        [Header("Portrait margins")]
        public float portraitSide = 32f;
        public float portraitBottom = 290f;

        [Header("Pieces that move between orientations")]
        public GameObject headerSubtitle;
        public RectTransform mascotArea;
        public RectTransform toastStack;
        public RectTransform historyPanel;
        [Tooltip("In portrait the shelf slides up while this card's bottom sheet is open.")]
        public PlantInfoCard infoCard;

        public bool IsPortrait { get; private set; }

        RectTransform root;
        Vector2 lastRootSize;
        Vector2Int lastScreen;
        bool hasLaidOut;
        float portraitBottomNow = -1f;

        void Awake()
        {
            Instance = this;
            root = (RectTransform)transform;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            Relayout();
        }

        void LateUpdate()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!hasLaidOut || screen != lastScreen || root.rect.size != lastRootSize)
                Relayout();
            else if (IsPortrait)
                SlideShelfForSheet();
        }

        /// <summary>Portrait only: lift the plants above the info-card bottom sheet while it's open.</summary>
        void SlideShelfForSheet()
        {
            float target = portraitBottom;
            if (infoCard != null && infoCard.IsOpen)
                target = Mathf.Max(portraitBottom, ((RectTransform)infoCard.transform).rect.height + 48f - labelHeight * 0.5f);
            if (Mathf.Abs(portraitBottomNow - target) < 0.5f) return;
            portraitBottomNow = Mathf.Lerp(portraitBottomNow, target, Mathf.Min(1f, Time.unscaledDeltaTime * 10f));
            if (Mathf.Abs(portraitBottomNow - target) < 0.5f) portraitBottomNow = target;
            if (shelfArea != null) shelfArea.offsetMin = new Vector2(shelfArea.offsetMin.x, portraitBottomNow);
            LayoutPlants();
        }

        [ContextMenu("Relayout now")]
        public void Relayout()
        {
            if (root == null) root = (RectTransform)transform;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            bool portrait = Screen.height > Screen.width;

            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.referenceResolution = portrait ? portraitReference : landscapeReference;
                scaler.matchWidthOrHeight = matchWidthOrHeight;
            }

            LayoutChrome(portrait);
            LayoutPlants();
            lastRootSize = root.rect.size;

            if (!hasLaidOut || portrait != IsPortrait)
            {
                IsPortrait = portrait;
                hasLaidOut = true;
                if (OrientationChanged != null) OrientationChanged(portrait);
            }
        }

        void LayoutChrome(bool portrait)
        {
            float top = headerHeight + 30f;

            if (shelfArea != null)
            {
                float side = portrait ? portraitSide : landscapeSideGutter;
                if (portrait && portraitBottomNow < 0f) portraitBottomNow = portraitBottom;
                float bottom = portrait ? portraitBottomNow : landscapeBottom;
                shelfArea.anchorMin = Vector2.zero;
                shelfArea.anchorMax = Vector2.one;
                shelfArea.offsetMin = new Vector2(side, bottom);
                shelfArea.offsetMax = new Vector2(-side, -top);
            }

            if (headerSubtitle != null) headerSubtitle.SetActive(!portrait);

            if (mascotArea != null)
            {
                float size = portrait ? 170f : 200f;
                UIFactory.Place(mascotArea, Vector2.zero, Vector2.zero, new Vector2(24f, 16f), new Vector2(size, size));
            }

            if (toastStack != null)
            {
                if (portrait)
                {
                    toastStack.anchorMin = new Vector2(0f, 1f);
                    toastStack.anchorMax = new Vector2(1f, 1f);
                    toastStack.pivot = new Vector2(0.5f, 1f);
                    toastStack.anchoredPosition = new Vector2(0f, -top);
                    toastStack.sizeDelta = new Vector2(-48f, toastStack.sizeDelta.y);
                }
                else
                {
                    toastStack.anchorMin = new Vector2(1f, 1f);
                    toastStack.anchorMax = new Vector2(1f, 1f);
                    toastStack.pivot = new Vector2(1f, 1f);
                    toastStack.anchoredPosition = new Vector2(-32f, -top);
                    toastStack.sizeDelta = new Vector2(540f, toastStack.sizeDelta.y);
                }
            }

            if (historyPanel != null)
            {
                if (portrait)
                {
                    historyPanel.anchorMin = new Vector2(0f, 0f);
                    historyPanel.anchorMax = new Vector2(1f, 0.75f);
                    historyPanel.pivot = new Vector2(0.5f, 0f);
                    historyPanel.offsetMin = new Vector2(16f, 16f);
                    historyPanel.offsetMax = new Vector2(-16f, 0f);
                }
                else
                {
                    historyPanel.anchorMin = new Vector2(1f, 0f);
                    historyPanel.anchorMax = new Vector2(1f, 1f);
                    historyPanel.pivot = new Vector2(1f, 0.5f);
                    historyPanel.offsetMin = new Vector2(-24f - 600f, 24f);
                    historyPanel.offsetMax = new Vector2(-24f, -top + 6f);
                }
            }
        }

        void LayoutPlants()
        {
            if (shelfArea == null || plantRow == null) return;
            var rowRt = (RectTransform)plantRow.transform;

            int n = 0;
            foreach (Transform child in rowRt)
                if (child.gameObject.activeSelf) n++;
            if (n == 0) return;

            Rect area = shelfArea.rect;
            float spacing = Mathf.Clamp(area.width * spacingRatio, 16f, 120f);
            float hFromWidth = (area.width - spacing * (n - 1)) / n / plantAspect;
            float hFromHeight = area.height - labelHeight;
            float h = Mathf.Max(80f, Mathf.Min(hFromWidth, Mathf.Min(hFromHeight, maxPlantHeight)));
            float w = h * plantAspect;

            plantRow.spacing = spacing;
            foreach (Transform child in rowRt)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = w;
                le.minHeight = le.preferredHeight = h + labelHeight;
            }

            if (plank != null)
            {
                float rowWidth = n * w + (n - 1) * spacing;
                float plankHeight = Mathf.Max(14f, h * 0.045f);
                UIFactory.Place(plank, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                    new Vector2(0f, labelHeight + h * 0.035f), new Vector2(rowWidth + w * 0.5f, plankHeight));
                if (plankLip != null)
                {
                    plankLip.anchorMin = new Vector2(0f, 0f);
                    plankLip.anchorMax = new Vector2(1f, 0f);
                    plankLip.pivot = new Vector2(0.5f, 1f);
                    plankLip.anchoredPosition = new Vector2(0f, plankHeight * 0.25f);
                    plankLip.sizeDelta = new Vector2(-w * 0.08f, plankHeight * 0.75f);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowRt);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            OrientationChanged = null;
        }
    }
}
