using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Keeps UI inside the phone's safe area (notch / rounded corners / home bar).</summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Smart Plant Pot/Safe Area Fitter")]
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect lastSafeArea;
        Vector2Int lastScreen;

        void OnEnable()
        {
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
                Apply();
        }

        void Apply()
        {
            lastSafeArea = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            var rt = (RectTransform)transform;
            Vector2 min = lastSafeArea.position;
            Vector2 max = lastSafeArea.position + lastSafeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
