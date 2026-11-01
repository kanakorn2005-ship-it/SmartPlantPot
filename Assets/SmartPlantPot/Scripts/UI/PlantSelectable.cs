using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartPlantPot
{
    /// <summary>
    /// Card "ระบบการกดที่ต้นไม้เพื่อแสดงค่า": tap / click a plant to open its info card.
    /// Works on 2D UI plants (GraphicRaycaster) and on future 3D models with a Collider
    /// (the camera has a PhysicsRaycaster) - just add this component next to the PlantUnit.
    /// Also forwards hover / press to <see cref="PotInteraction"/> for squash-and-stretch feedback.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Plant Selectable")]
    public class PlantSelectable : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public PlantUnit plant;
        public PotInteraction interaction;

        void Awake()
        {
            if (plant == null) plant = GetComponentInParent<PlantUnit>();
            if (interaction == null) interaction = GetComponentInParent<PotInteraction>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || plant == null) return;
            // The squash-and-release bounce comes from OnPointerDown / OnPointerUp.
            PlantSelection.Toggle(plant);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (interaction != null) interaction.SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (interaction != null)
            {
                interaction.SetHover(false);
                interaction.SetPressed(false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && interaction != null)
                interaction.SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (interaction != null) interaction.SetPressed(false);
        }
    }
}
