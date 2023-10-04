using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AS2.UI
{

    /// <summary>
    /// The behavior script for GameObjects that should have a tooltip.
    /// </summary>
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string message;

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipHandler.Instance.Open(message);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipHandler.Instance.Close();
        }

        public void ChangeMessage(string message)
        {
            this.message = message;
            TooltipHandler.Instance.ChangeMessage(message);
        }
    }
} // namespace AS2.UI
