// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


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
            if (message is not null && message.Length > 0)
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
