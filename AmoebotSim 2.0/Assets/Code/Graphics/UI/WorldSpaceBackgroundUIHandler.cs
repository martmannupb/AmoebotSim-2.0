// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using AS2.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{

    /// <summary>
    /// Controls the background grid (with the grid coordinates).
    /// </summary>
    public class WorldSpaceBackgroundUIHandler : MonoBehaviour
    {

        // Singleton
        public static WorldSpaceBackgroundUIHandler instance;

        // World Space UI
        public GameObject go_worldSpaceBackgroundUI;

        // State
        private bool isActive = false;
        private CellRect activeRect = new CellRect(0, 0, 0, 0);
        private float activeCameraRotation = 0f;
        // Data
        private List<GameObject> uiElements = new List<GameObject>();
        private bool uiElementsActive = false;

        public WorldSpaceBackgroundUIHandler()
        {
            // Singleton
            instance = this;
        }

        public void Update()
        {
            UpdateSystem();
        }

        /// <summary>
        /// Updates the background grid camera rotation.
        /// </summary>
        /// <param name="rotationDegrees">The camera rotation in degrees.</param>
        public void SetCameraRotation(float rotationDegrees)
        {
            if (activeCameraRotation != rotationDegrees)
            {
                activeCameraRotation = rotationDegrees;
                // Update UI
                foreach (var item in uiElements)
                {
                    item.transform.rotation = Quaternion.Euler(0f, 0f, activeCameraRotation);
                }
            }
        }

        /// <summary>
        /// Updates the background grid. Call this once per frame.
        /// Note: This method only takes a lot of time if the system is displayed and the camera changes, so everything is regenerated.
        /// Recommendation: Disable camera movement if this is active.
        /// </summary>
        public void UpdateSystem()
        {
            // Check if active
            if (isActive)
            {
                // Amoebot Border Coordinates
                Vector2 camBL = CameraUtils.MainCamera_WorldPosition_BottomLeft() + (Vector2)(Quaternion.Euler(0f, 0f, activeCameraRotation) * ((Vector3)new Vector2(-3f, -3f)));
                Vector2 camBR = CameraUtils.MainCamera_WorldPosition_BottomRight() + (Vector2)(Quaternion.Euler(0f, 0f, activeCameraRotation) * ((Vector3)new Vector2(3f, -3f)));
                Vector2 camTL = CameraUtils.MainCamera_WorldPosition_TopLeft() + (Vector2)(Quaternion.Euler(0f, 0f, activeCameraRotation) * ((Vector3)new Vector2(-3f, 3f)));
                Vector2 camTR = CameraUtils.MainCamera_WorldPosition_TopRight() + (Vector2)(Quaternion.Euler(0f, 0f, activeCameraRotation) * ((Vector3)new Vector2(3f, 3f)));
                //Vector2 camMinCoordinates = new Vector2(Mathf.Min(camBL.x, camBR.x, camTL.x, camTR.x), Mathf.Min(camBL.y, camBR.y, camTL.y, camTR.y));
                //Vector2 camMaxCoordinates = new Vector2(Mathf.Max(camBL.x, camBR.x, camTL.x, camTR.x), Mathf.Max(camBL.y, camBR.y, camTL.y, camTR.y));
                Vector2Int amoebotBL = AmoebotFunctions.WorldToGridPosition(camBL);
                Vector2Int amoebotBR = AmoebotFunctions.WorldToGridPosition(camBR);
                Vector2Int amoebotTL = AmoebotFunctions.WorldToGridPosition(camTL);
                Vector2Int amoebotTR = AmoebotFunctions.WorldToGridPosition(camTR);
                // Convert to Min/Max (check all because of unknown rotation)
                Vector2Int amoebotMinCoordinates = new Vector2Int(Mathf.Min(amoebotBL.x, amoebotBR.x, amoebotTL.x, amoebotTR.x), Mathf.Min(amoebotBL.y, amoebotBR.y, amoebotTL.y, amoebotTR.y));
                Vector2Int amoebotMaxCoordinates = new Vector2Int(Mathf.Max(amoebotBL.x, amoebotBR.x, amoebotTL.x, amoebotTR.x), Mathf.Max(amoebotBL.y, amoebotBR.y, amoebotTL.y, amoebotTR.y));
                CellRect curRect = new CellRect(amoebotMinCoordinates.x, amoebotMinCoordinates.y, amoebotMaxCoordinates.x - amoebotMinCoordinates.x, amoebotMaxCoordinates.y - amoebotMinCoordinates.y);
                if (activeRect != curRect || uiElementsActive == false)
                {
                    // Different Rect
                    activeRect = curRect;
                    int amount = activeRect.Width * activeRect.Height;
                    // Create/redefine GameObjects for UI
                    for (int i = 0; i < amount; i++)
                    {
                        GameObject go;
                        int x = i % activeRect.Width;
                        int y = i / activeRect.Width;
                        Vector2Int amoebotPosition = new Vector2Int(activeRect.minX + x, activeRect.minY + y);
                        Vector3 amoebotWorldPosition = AmoebotFunctions.GridToWorldPositionVector3(amoebotPosition.x, amoebotPosition.y);
                        amoebotWorldPosition.z = RenderSystem.zLayer_background - 0.1f;
                        if (i < uiElements.Count)
                        {
                            // Element is in list
                            go = uiElements[i];
                            go.transform.position = amoebotWorldPosition;
                            go.transform.rotation = Quaternion.Euler(0f, 0f, activeCameraRotation);
                        }
                        else
                        {
                            // Instantiate Go
                            go = Instantiate<GameObject>(UIDatabase.prefab_worldSpace_backgroundTextUI, amoebotWorldPosition, Quaternion.identity, go_worldSpaceBackgroundUI.transform);
                            go.transform.rotation = Quaternion.Euler(0f, 0f, activeCameraRotation);
                            uiElements.Add(go);
                        }
                        go.GetComponentInChildren<TextMeshProUGUI>().text = amoebotPosition.x + "," + amoebotPosition.y;
                        if (go.activeSelf == false) go.SetActive(true);
                    }
                    // Disable additional GameObjects in list (so we can reuse them later)
                    for (int i = amount; i < uiElements.Count; i++)
                    {
                        if (uiElements[i].activeSelf) uiElements[i].SetActive(false);
                    }
                    uiElementsActive = true;
                }
                else
                {
                    // Same Rect, dont update
                }
            }
            else
            {
                // System not active, hide if necessary
                if (uiElementsActive)
                {
                    foreach (var item in uiElements)
                    {
                        if (item.activeSelf) item.SetActive(false);
                    }
                    uiElementsActive = false;
                }
            }


        }

        /// <summary>
        /// Toggles the background grid on/off.
        /// Camera movements are disabled while the grid is toggled on.
        /// </summary>
        public void ToggleBackgroundGrid()
        {
            if (isActive == false)
            {
                if (MouseController.instance != null) MouseController.instance.LockCameraMovement();
                Log.Warning("The background grid is active, camera movement has been disabled.");
                //if(Input.GetKey(KeyCode.LeftControl) == false)
                //{
                //    Log.Warning("Do you really want to show the background grid? If yes, press Ctrl while clicking that button!");
                //    return;
                //}
            }
            else
            {
                if (MouseController.instance != null) MouseController.instance.UnlockCameraMovement();
                Log.Entry("Camera movement has been enabled again.");
            }
            isActive = !isActive;
        }

        /// <summary>
        /// Checks if the grid is active.
        /// </summary>
        /// <returns><c>true</c> if and only if the grid is currently active.</returns>
        public bool IsActive()
        {
            return isActive;
        }

    }

}