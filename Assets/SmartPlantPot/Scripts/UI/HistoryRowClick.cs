using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartPlantPot
{
    /// <summary>Tap a history row to open that plant's info card.</summary>
    public class HistoryRowClick : MonoBehaviour, IPointerClickHandler
    {
        public PlantUnit plant;
        public NotificationHistoryPanel panel;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (plant != null) PlantSelection.Select(plant);
            if (panel != null) panel.Close();
        }
    }
}
