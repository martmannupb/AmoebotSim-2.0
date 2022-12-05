using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System;

namespace AS2.UI
{

    public class WorldSpaceBackgroundUIHandler : MonoBehaviour
    {

        // Singleton
        public static WorldSpaceBackgroundUIHandler instance;

        // World Space UI
        public GameObject go_worldSpaceBackgroundUI;

        // State
        private bool isActive = false;
        private CellRect activeRect = new CellRect(0, 0, 0, 0);
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

        public void UpdateSystem()
        {
            // Check if active
            if (isActive)
            {
                // Amoebot Border Coordinates
                Vector2Int amoebotBL = AmoebotFunctions.GetGridPositionFromWorldPosition(CameraUtils.MainCamera_WorldPosition_BottomLeft() + new Vector2(-3f, -3f));
                Vector2Int amoebotBR = AmoebotFunctions.GetGridPositionFromWorldPosition(CameraUtils.MainCamera_WorldPosition_BottomRight() + new Vector2(3f, -3f));
                Vector2Int amoebotTL = AmoebotFunctions.GetGridPositionFromWorldPosition(CameraUtils.MainCamera_WorldPosition_TopLeft() + new Vector2(-3f, 3f));
                Vector2Int amoebotTR = AmoebotFunctions.GetGridPositionFromWorldPosition(CameraUtils.MainCamera_WorldPosition_TopRight() + new Vector2(3f, 3f));
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
                        Vector3 amoebotWorldPosition = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(amoebotPosition.x, amoebotPosition.y);
                        amoebotWorldPosition.z = RenderSystem.zLayer_background - 0.1f;
                        if (i < uiElements.Count)
                        {
                            // Element is in list
                            go = uiElements[i];
                            go.transform.position = amoebotWorldPosition;
                        }
                        else
                        {
                            // Instantiate Go
                            go = Instantiate<GameObject>(UIDatabase.prefab_worldSpace_backgroundTextUI, amoebotWorldPosition, Quaternion.identity, go_worldSpaceBackgroundUI.transform);
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

        public bool IsActive()
        {
            return isActive;
        }

    }

} // namespace AS2.UI
