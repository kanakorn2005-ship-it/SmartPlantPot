using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartPlantPot
{
    /// <summary>Tapping empty space closes the info card and the notification panel.</summary>
    [AddComponentMenu("Smart Plant Pot/Background Click Catcher")]
    public class BackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public NotificationHistoryPanel historyPanel;

        public void OnPointerClick(PointerEventData eventData)
        {
            PlantSelection.Clear();
            if (historyPanel != null) historyPanel.Close();
        }
    }
}
