using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>Header: live clock and the data-source connection pill.</summary>
    [AddComponentMenu("Smart Plant Pot/Header Bar")]
    public class HeaderBar : MonoBehaviour
    {
        public Text clock;
        public Image connectionDot;
        public Text connectionLabel;

        float timer = 999f;

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 0.5f) return;
            timer = 0f;

            if (clock != null) clock.text = DateTime.Now.ToString("HH:mm");

            var skin = UISkin.Current;
            var source = PlantDataSource.Active;
            bool ok = source != null && source.IsConnected;
            if (connectionLabel != null) connectionLabel.text = source != null ? source.ConnectionLabel : L.Offline;
            if (connectionDot != null && skin != null)
            {
                Color c = ok ? skin.statusNormal : skin.statusHigh;
                c.a = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2f));
                connectionDot.color = c;
            }
        }
    }
}
