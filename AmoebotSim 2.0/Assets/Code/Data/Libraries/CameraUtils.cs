using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AS2
{

    public static class CameraUtils
    {

        // Camera World Position -----

        /// <summary>
        /// Bottom left world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns></returns>
        public static Vector2 MainCamera_WorldPosition_BottomLeft()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
            return worldCoordinates;
        }

        /// <summary>
        /// Bottom right world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns></returns>
        public static Vector2 MainCamera_WorldPosition_BottomRight()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0f, 0f));
            return worldCoordinates;
        }

        /// <summary>
        /// Top right world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns></returns>
        public static Vector2 MainCamera_WorldPosition_TopRight()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight, 0));
            return worldCoordinates;
        }

        /// <summary>
        /// Top left world position of the main camera. (Unity World Coordinates)
        /// </summary>
        /// <returns></returns>
        public static Vector2 MainCamera_WorldPosition_TopLeft()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(new Vector3(0f, Camera.main.pixelHeight, 0));
            return worldCoordinates;
        }

        public static Vector2 GetLowestXYCameraWorldPositions()
        {
            Vector2 camPosBL = MainCamera_WorldPosition_BottomLeft();
            Vector2 camPosBR = MainCamera_WorldPosition_BottomRight();
            Vector2 camPosTL = MainCamera_WorldPosition_TopLeft();
            Vector2 camPosTR = MainCamera_WorldPosition_TopRight();
            return new Vector2(Mathf.Min(camPosBL.x, camPosBR.x, camPosTL.x, camPosTR.x), Mathf.Min(camPosBL.y, camPosBR.y, camPosTL.y, camPosTR.y));
        }

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
        /// Mouse world position.
        /// </summary>
        /// <returns></returns>
        public static Vector2 MainCamera_Mouse_WorldPosition()
        {
            Vector3 worldCoordinates = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return new Vector2(worldCoordinates.x, worldCoordinates.y);
        }
    }

} // namespace AS2
