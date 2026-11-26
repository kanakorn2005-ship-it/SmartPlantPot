using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// The 2D team's mascot in the corner. Gives onboarding hints, reads out new warnings in a
    /// speech bubble, and when tapped jumps to the plant that needs help most.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Mascot Helper")]
    public class MascotHelper : MonoBehaviour, IPointerClickHandler
    {
        public RectTransform body;
        public RectTransform bubble;
        public Text bubbleText;
        public CanvasGroup bubbleGroup;
        public PlantInfoCard infoCard;

        [Tooltip("How long a warning stays in the bubble before going back to the summary.")]
        public float alertSeconds = 6f;

        NotificationCenter center;
        Image bodyImage;
        Sprite shownSprite;
        bool cameraHintGiven;
        float messageUntil;
        float jumpStart = -10f;
        bool hiddenByCard;
        float bubbleAmount = 1f;

        void Start()
        {
            center = NotificationCenter.Instance;
            if (center != null) center.AlertRaised += OnAlert;
            if (body != null) bodyImage = body.GetComponent<Image>();
            if (GameManager.Instance != null) GameManager.Instance.SkinChanged += OnSkinChanged;
            Say(L.HintTapPlant, 5f);
        }

        void OnSkinChanged()
        {
            jumpStart = Time.unscaledTime;   // hop in the new outfit
            Say(L.T("ชุดใหม่สวยไหม~", "Like my new look?"), 2.5f);
        }

        /// <summary>The equipped skin (3D scene); faces away while the camera looks around the shop.</summary>
        void ApplySkin()
        {
            if (bodyImage == null || GameManager.Instance == null) return;
            var skin = GameManager.Instance.CurrentSkin;
            if (skin == null) return;
            bool lookingAround = CameraRig.Instance != null && CameraRig.Instance.IsFreeLook;
            Sprite want = lookingAround && skin.back != null ? skin.back : skin.front;
            if (want == null || want == shownSprite) return;
            shownSprite = want;
            bodyImage.sprite = want;
        }

        void OnDestroy()
        {
            if (center != null) center.AlertRaised -= OnAlert;
            if (GameManager.Instance != null) GameManager.Instance.SkinChanged -= OnSkinChanged;
        }

        void OnAlert(PlantAlert alert)
        {
            if (alert.kind != AlertKind.Warning || alert.plant == null) return;
            Say(alert.plant.DisplayName + ": " + L.Advice(alert.metric, alert.status), alertSeconds);
        }

        void Say(string text, float seconds)
        {
            if (bubbleText != null) bubbleText.text = ThaiShaper.Fix(text);
            messageUntil = Time.unscaledTime + seconds;
            jumpStart = Time.unscaledTime;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            ApplySkin();

            // 3D shop: tell once that the room can be explored
            if (!cameraHintGiven && CameraRig.Instance != null && t > 9f && t > messageUntil)
            {
                cameraHintGiven = true;
                Say(L.T("ลากหน้าจอเพื่อหมุนกล้องดูร้านได้นะ", "Drag the screen to look around the shop"), 5f);
            }

            if (t > messageUntil && bubbleText != null)
            {
                int issues = 0;
                foreach (var p in PlantUnit.All)
                    if (!p.IsHealthy) issues++;
                string summary = issues == 0 ? L.AllHappy : L.NeedsAttention(issues);
                if (bubbleText.text != summary) bubbleText.text = summary;
            }

            // Idle bob + a hop when there's something new to say.
            if (body != null)
            {
                float j = t - jumpStart;
                float hop = j < 0.5f ? Mathf.Sin(j / 0.5f * Mathf.PI) * 26f : 0f;
                float bob = Mathf.Sin(t * 2.2f) * 4f;
                body.anchoredPosition = new Vector2(0f, bob + hop);
                float squash = 1f + Mathf.Sin(t * 2.2f + 1.2f) * 0.015f;
                body.localScale = new Vector3(1f / squash, squash, 1f);
            }

            // In portrait the info card sheet covers the corner, so step aside.
            bool portrait = ResponsiveLayout.Instance != null && ResponsiveLayout.Instance.IsPortrait;
            hiddenByCard = portrait && infoCard != null && infoCard.IsOpen;
            bubbleAmount = Mathf.MoveTowards(bubbleAmount, hiddenByCard ? 0f : 1f, Time.unscaledDeltaTime / 0.2f);
            if (bubbleGroup != null) bubbleGroup.alpha = bubbleAmount;
            if (body != null)
            {
                var img = body.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 1f, 1f, Mathf.Max(0.0f, bubbleAmount));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlantUnit worst = null;
            float worstScore = 2f;
            foreach (var p in PlantUnit.All)
            {
                if (p.IsHealthy) continue;
                float s = p.HealthScore;
                if (s < worstScore)
                {
                    worstScore = s;
                    worst = p;
                }
            }
            if (worst != null)
            {
                PlantSelection.Select(worst);
                PlantMetric m;
                MetricStatus st;
                if (worst.TryGetMainIssue(out m, out st)) Say(worst.DisplayName + ": " + L.Advice(m, st), alertSeconds);
            }
            else
            {
                Say(L.AllHappy, 3f);
            }
        }
    }
}
