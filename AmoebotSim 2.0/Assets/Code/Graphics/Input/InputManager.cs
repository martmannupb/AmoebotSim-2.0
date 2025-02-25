// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AS2.UI
{

    /// <summary>
    /// Receives the standardized inputs from the <see cref="InputHandler"/>
    /// and passes them to the simulation environment.
    /// </summary>
    public class InputManager
    {

        private static InputManager instance;

        // Actions for Callbacks
        /// <summary>
        /// Action triggered when a mouse click or drag event is registered.
        /// </summary>
        public Action<ClickAction> clickActionEvent;
        //public Action<InputAction> inputActionEvent;

        private bool debug = false;

        private InputManager()
        {
            instance = this;
        }

        public static InputManager GetCurrentInstance()
        {
            return instance;
        }

        public static InputManager CreateInstance()
        {
            if (instance == null) instance = new InputManager();
            return instance;
        }


        /// <summary>
        /// Receives actions from the <see cref="InputHandler"/> and handles them accordingly.
        /// </summary>
        /// <param name="inputAction">The input action to be processed.</param>
        public void ProcessInput(InputAction inputAction)
        {
            switch (inputAction.inputType)
            {
                case InputAction.InputType.Mouse:
                    ProcessInput_Mouse((ClickAction)inputAction);
                    break;
                case InputAction.InputType.Keyboard:
                    ProcessInput_Keyboard();
                    break;
                default:
                    break;
            }
        }


        // Mouse ClickActions ===============

        /// <summary>
        /// Receives mouse actions and processes inputs.
        /// Addition: A "clickActionEvent" is called that other classes can
        /// subscribe to in order to receive updates of the input.
        /// </summary>
        /// <param name="clickAction">The mouse click action to be processed.</param>
        private void ProcessInput_Mouse(ClickAction clickAction)
        {
            switch (clickAction.clickButton)
            {
                case ClickAction.ClickButton.LeftMouse:
                    ProcessInput_LeftMouse(clickAction);
                    break;
                case ClickAction.ClickButton.MiddleMouse:
                    ProcessInput_MiddleMouse(clickAction);
                    break;
                case ClickAction.ClickButton.RightMouse:
                    ProcessInput_RightMouse(clickAction);
                    break;
                default:
                    break;
            }
            this.clickActionEvent(clickAction);
        }

        private void ProcessInput_LeftMouse(ClickAction clickAction)
        {
            if (debug) if (clickAction.ongoing == false) Log.Debug("LeftMouse ClickAction is processed ... ");
        }

        private void ProcessInput_RightMouse(ClickAction clickAction)
        {
            if (debug) if (clickAction.ongoing == false) Log.Debug("RightMouse ClickAction is processed ... ");
        }

        private void ProcessInput_MiddleMouse(ClickAction clickAction)
        {
            throw new System.NotImplementedException();
        }



        // Keyboard KeyActions ===============

        private void ProcessInput_Keyboard()
        {
            throw new System.NotImplementedException();
        }

    }

}