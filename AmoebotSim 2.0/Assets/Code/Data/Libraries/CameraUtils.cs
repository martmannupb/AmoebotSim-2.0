// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AS2
{

    /// <summary>
    /// Static class providing various camera utility functions.
    /// </summary>
    public static class CameraUtils
    {

        // Camera World Position -----

        /// <summary>
        /// Bottom left world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns>The (x, y) world coordinates of the active camera's bottom left corner.</returns>
        public static Vector2 MainCamera_WorldPosition_BottomLeft()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
            return worldCoordinates;
        }

        /// <summary>
        /// Bottom right world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns>The (x, y) world coordinates of the active camera's bottom right corner.</returns>
        public static Vector2 MainCamera_WorldPosition_BottomRight()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0f, 0f));
            return worldCoordinates;
        }

        /// <summary>
        /// Top right world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns>The (x, y) world coordinates of the active camera's top right corner.</returns>
        public static Vector2 MainCamera_WorldPosition_TopRight()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight, 0));
            return worldCoordinates;
        }

        /// <summary>
        /// Top left world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns>The (x, y) world coordinates of the active camera's top left corner.</returns>
        public static Vector2 MainCamera_WorldPosition_TopLeft()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(0f, Camera.main.pixelHeight, 0));
            return worldCoordinates;
        }

        /// <summary>
        /// Computes the lower left corner of the active camera's bounding box
        /// in world coordinates.
        /// </summary>
        /// <returns>The (x, y) coordinates of the active camera's bounding box's
        /// lower left corner.</returns>
        public static Vector2 GetLowestXYCameraWorldPositions()
        {
            Vector2 camPosBL = MainCamera_WorldPosition_BottomLeft();
            Vector2 camPosBR = MainCamera_WorldPosition_BottomRight();
            Vector2 camPosTL = MainCamera_WorldPosition_TopLeft();
            Vector2 camPosTR = MainCamera_WorldPosition_TopRight();
            return new Vector2(Mathf.Min(camPosBL.x, camPosBR.x, camPosTL.x, camPosTR.x), Mathf.Min(camPosBL.y, camPosBR.y, camPosTL.y, camPosTR.y));
        }

        /// <summary>
        /// Computes the top right corner of the active camera's bounding box
        /// in world coordinates.
        /// </summary>
        /// <returns>The (x, y) coordinates of the active camera's bounding box's
        /// top right corner.</returns>
        public static Vector2 GetHightestXYCameraWorldPositions()
        {
            Vector2 camPosBL = MainCamera_WorldPosition_BottomLeft();
            Vector2 camPosBR = MainCamera_WorldPosition_BottomRight();
            Vector2 camPosTL = MainCamera_WorldPosition_TopLeft();
            Vector2 camPosTR = MainCamera_WorldPosition_TopRight();
            return new Vector2(Mathf.Max(camPosBL.x, camPosBR.x, camPosTL.x, camPosTR.x), Mathf.Max(camPosBL.y, camPosBR.y, camPosTL.y, camPosTR.y));
        }

        // Mouse Position -----

        /// <summary>
        /// Computes the current world position of the mouse cursor.
        /// </summary>
        /// <returns>The (x, y) world coordinates of the current mouse position.</returns>
        public static Vector2 MainCamera_Mouse_WorldPosition()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return new Vector2(worldCoordinates.x, worldCoordinates.y);
        }
    }

} // namespace AS2
