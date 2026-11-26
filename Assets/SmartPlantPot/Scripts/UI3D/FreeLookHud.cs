using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Shown while the camera looks around the shop: a "back to the plants" button plus a one-line
    /// hint of the controls. Hidden in the normal plant view.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Free Look HUD")]
    public class FreeLookHud : MonoBehaviour
    {
        public CameraRig rig;
        public Button resetButton;
        public Text hintText;

        CanvasGroup group;
        float shown;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        void Start()
        {
            if (rig == null) rig = CameraRig.Instance;
            if (resetButton != null) resetButton.onClick.AddListener(() => { if (rig != null) rig.ResetView(); });
            if (hintText != null)
                hintText.text = ThaiShaper.Fix(L.T("ลากเพื่อหมุน · ล้อเมาส์/สองนิ้วเพื่อซูม · คลิกขวาลากเพื่อเลื่อน · R กลับ",
                    "Drag to orbit · wheel / pinch to zoom · right-drag to move · R to return"));
        }

        void Update()
        {
            bool on = rig != null && rig.IsFreeLook && !ModalPanel.AnyOpen;
            shown = Mathf.MoveTowards(shown, on ? 1f : 0f, Time.unscaledDeltaTime / 0.2f);
            group.alpha = shown;
            group.blocksRaycasts = shown > 0.5f;
            var rt = (RectTransform)transform;
            rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, shown);
        }
    }
}
