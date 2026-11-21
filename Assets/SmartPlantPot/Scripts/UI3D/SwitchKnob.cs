using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Animates the knob of a UIKit switch.</summary>
    public class SwitchKnob : MonoBehaviour
    {
        public RectTransform knob;
        float target;
        float current;

        public void Snap(bool on)
        {
            target = current = on ? 1f : 0f;
            Place();
        }

        public void Set(bool on)
        {
            target = on ? 1f : 0f;
        }

        void Update()
        {
            if (Mathf.Approximately(current, target)) return;
            current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * 8f);
            Place();
        }

        void Place()
        {
            if (knob != null) knob.anchoredPosition = new Vector2(Mathf.Lerp(17f, 47f, current), 0f);
        }
    }
}
