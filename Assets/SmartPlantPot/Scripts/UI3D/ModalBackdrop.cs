using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartPlantPot
{
    /// <summary>Clicking the dim background of a modal closes it.</summary>
    public class ModalBackdrop : MonoBehaviour, IPointerClickHandler
    {
        public ModalPanel panel;

        public void OnPointerClick(PointerEventData eventData)
        {
            // Clicks on empty parts of the window bubble up to this handler as well:
            // only close when the dim area itself was hit.
            if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
            if (panel != null) panel.Close();
        }
    }
}
