using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartPlantPot
{
    /// <summary>Tap handler for an alert animation popup (opens that plant).</summary>
    public class AlertAnimationClick : MonoBehaviour, IPointerClickHandler
    {
        public Action onClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null) onClick();
        }
    }
}
