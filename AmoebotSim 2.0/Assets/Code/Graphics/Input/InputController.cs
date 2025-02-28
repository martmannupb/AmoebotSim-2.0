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
    /// Reads in the inputs from the mouse and passes them to the InputHandler.
    /// </summary>
    public class InputController : MonoBehaviour
    {

        // Debug Mode
        private bool debugMode = false;
        // Mouse State
        private InputHandler.MouseState mouseState;

        // Start is called before the first frame update
        void Start()
        {
            mouseState = new InputHandler.MouseState();
        }

        /// <summary>
        /// Call this once per frame.
        /// Reads the information from the mouse and keyboard and sends it to the InputHandler.
        /// </summary>
        public void ManUpdate()
        {
            // Debugging
            if (debugMode) DebugMouseInformation();

            // Create Mouse Information
            // We pass all information to the InputHandler
            // Camera Data
            mouseState.cameraScreenSize = new Vector2Int(Camera.main.pixelWidth, Camera.main.pixelHeight);
            // Mouse Position
            mouseState.mouse_positionWorld = CameraUtils.MainCamera_Mouse_WorldPosition();
            mouseState.mouse_overUI = EventSystem.current.IsPointerOverGameObject();
            // Left Mouse Button
            mouseState.mouseLeft_clicked = Input.GetMouseButtonDown(0);
            mouseState.mouseLeft_hold = Input.GetMouseButton(0);
            mouseState.mouseLeft_released = Input.GetMouseButtonUp(0);
            // Right Mouse Button
            mouseState.mouseRight_clicked = Input.GetMouseButtonDown(1);
            mouseState.mouseRight_hold = Input.GetMouseButton(1);
            mouseState.mouseRight_released = Input.GetMouseButtonUp(1);
            // Middle Mouse Button
            mouseState.mouseMiddle_clicked = Input.GetMouseButtonDown(2);
            mouseState.mouseMiddle_hold = Input.GetMouseButton(2);
            mouseState.mouseMiddle_released = Input.GetMouseButtonUp(2);
            mouseState.mouseMiddle_value = Input.mouseScrollDelta.y;

            // Pass Information to InputHandler
            InputHandler.InputTick(mouseState);
        }






        // Debugging ===============

        public void DebugMouseInformation()
        {
            // Clicking
            if (Input.GetMouseButtonDown(0))
            {
                Log.Debug("Left Mouse Button clicked.");
            }
            if (Input.GetMouseButtonDown(1))
            {
                Log.Debug("Right Mouse Button clicked.");
            }
            if (Input.GetMouseButtonDown(2))
            {
                Log.Debug("Middle Mouse Button clicked.");
            }
            // Holding
            if (Input.GetMouseButton(0))
            {
                Log.Debug("Left Mouse Button held down.");
            }
            if (Input.GetMouseButton(1))
            {
                Log.Debug("Right Mouse Button held down.");
            }
            if (Input.GetMouseButton(2))
            {
                Log.Debug("Middle Mouse Button held down.");
            }
            // Releasing
            if (Input.GetMouseButtonUp(0))
            {
                Log.Debug("Left Mouse Button released.");
            }
            if (Input.GetMouseButtonUp(1))
            {
                Log.Debug("Right Mouse Button released.");
            }
            if (Input.GetMouseButtonUp(2))
            {
                Log.Debug("Middle Mouse Button released.");
            }
            if (Input.mouseScrollDelta.y != 0f)
            {
                Log.Debug("Middle Mouse Scroll: " + Input.mouseScrollDelta.y);
            }
        }
    }

}