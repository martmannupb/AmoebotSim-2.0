using AS2.Visuals;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{

    /// <summary>
    /// Controls the setting panel.
    /// </summary>
    public class SettingsUIHandler : MonoBehaviour
    {

        // References
        private UIHandler uiHandler;

        private UISetting_Text setting_cameraPosX;
        private UISetting_Text setting_cameraPosY;
        private UISetting_Text setting_cameraSize;
        private UISetting_Text setting_beepFailureProb;

        // Data
        public GameObject settingsPanel;
        public GameObject settingsParent;
        private Vector2Int nonFullScreenResolution = new Vector2Int(-1, -1);
        private bool camPosInGridCoords = false;    // Stores whether camera coordinates are in grid or world system

        // Setting names (used to identify which setting has changed)
        private const string settingName_animationsOnOff = "Animations On/Off";
        private const string settingName_fullscreen = "Fullscreen";
        private const string settingName_cameraAngle = "Camera Angle";
        private const string settingName_cameraPosX = "Camera Pos. X";
        private const string settingName_cameraPosY = "Camera Pos. Y";
        private const string settingName_cameraPosWorldOrGrid = "Grid Coordinates";
        private const string settingName_cameraSize = "Camera Size";
        private const string settingName_compassOvArrows = "Compass Ov. Arrows";
        private const string settingName_circuitBorder = "Circuit Border";
        private const string settingName_circularRing = "Circular Ring";
        private const string settingName_toggleTooltips = "Tooltips";
        private const string settingName_beepFailureProb = "Beep Failure Prob.";

        private void Start()
        {
            // Set References
            uiHandler = FindObjectOfType<UIHandler>();
            if (uiHandler == null) Log.Error("Could not find UIHandler.");
            // Init
            settingsPanel.SetActive(false);
            InitSettings();
        }

        /// <summary>
        /// Initializes the settings UI. Dynamically sets up all the settings with all their input fields.
        /// Initial values are loaded from the configuration file if they are not available elsewhere.
        /// </summary>
        private void InitSettings()
        {
            // Defaults _________________________
            uiHandler.sim.renderSystem.SetAntiAliasing(8);
            nonFullScreenResolution = new Vector2Int(Screen.width, Screen.height);
            Screen.SetResolution(nonFullScreenResolution.x, nonFullScreenResolution.y, false);

            // Settings _________________________
            // Header: Camera Controls
            UISetting_Header setting_header_camera_ctrl = new UISetting_Header(null, settingsParent.transform, "Camera Controls");

            // Camera Angle
            UISetting_Slider setting_cameraAngle = new UISetting_Slider(null, settingsParent.transform, settingName_cameraAngle, 0f, 11f, 0f, true);
            setting_cameraAngle.onValueChangedEvent += SettingChanged_Value;
            // Camera Position x and y
            setting_cameraPosX = new UISetting_Text(null, settingsParent.transform, settingName_cameraPosX, Camera.main.transform.position.x.ToString(), UISetting_Text.InputType.Float);
            setting_cameraPosY = new UISetting_Text(null, settingsParent.transform, settingName_cameraPosY, Camera.main.transform.position.y.ToString(), UISetting_Text.InputType.Float);
            // Toggle between world and grid coordinates
            UISetting_Toggle setting_cameraPosWorldOrGrid = new UISetting_Toggle(null, settingsParent.transform, settingName_cameraPosWorldOrGrid, false);
            setting_cameraPosWorldOrGrid.onValueChangedEvent += SettingChanged_Toggle;
            camPosInGridCoords = false;
            // Camera Size
            setting_cameraSize = new UISetting_Text(null, settingsParent.transform, settingName_cameraSize, Camera.main.orthographicSize.ToString(), UISetting_Text.InputType.Float);

            // Button to apply camera settings
            GameObject go_button_apply = Instantiate(UIDatabase.prefab_ui_button, settingsParent.transform);
            TextMeshProUGUI tmpro = go_button_apply.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = "Apply";
            Tooltip tt = go_button_apply.GetComponentInChildren<Tooltip>();
            tt.ChangeMessage("Move the camera to the specified location and zoom level");
            Button button_apply = go_button_apply.GetComponentInChildren<Button>();
            button_apply.onClick.AddListener(delegate { Button_CameraApply(); });

            // Header: Visualization
            UISetting_Spacing setting_spacing = new UISetting_Spacing(null, settingsParent.transform, "Spacing");
            UISetting_Header setting_header_graphics = new UISetting_Header(null, settingsParent.transform, "Visualization");

            // Animations On/Off
            UISetting_Toggle setting_animationsOnOff = new UISetting_Toggle(null, settingsParent.transform, settingName_animationsOnOff, RenderSystem.animationsOn);
            setting_animationsOnOff.onValueChangedEvent += SettingChanged_Toggle;
            // Compass Dir Overlay Display
            UISetting_Toggle setting_compassDirOverlayDisplayType = new UISetting_Toggle(null, settingsParent.transform, settingName_compassOvArrows, Config.ConfigData.settingsMenu.drawCompassOverlayAsArrows);
            setting_compassDirOverlayDisplayType.onValueChangedEvent += SettingChanged_Toggle;
            // Circuit Connections Border
            UISetting_Toggle setting_circuitConnectionBorders = new UISetting_Toggle(null, settingsParent.transform, settingName_circuitBorder, RenderSystem.flag_circuitBorderActive);
            setting_circuitConnectionBorders.onValueChangedEvent += SettingChanged_Toggle;
            // Graph View Outer Ring
            UISetting_Toggle setting_graphViewOutterRing = new UISetting_Toggle(null, settingsParent.transform, settingName_circularRing, RenderSystem.flag_showCircuitViewOuterRing);
            setting_graphViewOutterRing.onValueChangedEvent += SettingChanged_Toggle;
            // Fullscreen
            UISetting_Toggle setting_fullscreen = new UISetting_Toggle(null, settingsParent.transform, settingName_fullscreen, Config.ConfigData.settingsMenu.fullscreen);
            setting_fullscreen.onValueChangedEvent += SettingChanged_Toggle;

            // Header: Other
            UISetting_Spacing setting_spacing2 = new UISetting_Spacing(null, settingsParent.transform, "Spacing2");
            UISetting_Header setting_header_other = new UISetting_Header(null, settingsParent.transform, "Other");

            // Tooltips On/Off
            UISetting_Toggle setting_tooltipsOnOff = new UISetting_Toggle(null, settingsParent.transform, settingName_toggleTooltips, Config.ConfigData.settingsMenu.showTooltips);
            setting_tooltipsOnOff.onValueChangedEvent += SettingChanged_Toggle;

            // Beep failure probability
            uiHandler.sim.system.BeepFailureProb = Config.ConfigData.settingsMenu.beepFailureProbability;
            setting_beepFailureProb = new UISetting_Text(null, settingsParent.transform, settingName_beepFailureProb, uiHandler.sim.system.BeepFailureProb.ToString(), UISetting_Text.InputType.Float);
            setting_beepFailureProb.onValueChangedEvent += SettingChanged_BeepFailureProb;

            // Button to save the current settings
            GameObject go_button_save = Instantiate(UIDatabase.prefab_ui_button, settingsParent.transform);
            tmpro = go_button_save.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = "Save Settings";
            tt = go_button_save.GetComponentInChildren<Tooltip>();
            tt.ChangeMessage("Save the current settings in the configuration file");
            Button button_save = go_button_save.GetComponentInChildren<Button>();
            button_save.onClick.AddListener(delegate { Button_SaveSettings(); });
        }

        /// <summary>
        /// Called by a setting callback when a setting has been changed.
        /// </summary>
        /// <param name="name">The name of the changed setting.</param>
        /// <param name="value">The setting's new float value.</param>
        private void SettingChanged_Value(string name, float value)
        {
            switch (name)
            {
                case settingName_cameraAngle:
                    float cameraRotationDegrees = 30f * value;
                    Camera.main.transform.rotation = Quaternion.Euler(0, 0, cameraRotationDegrees);
                    // Notify Systems
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.SetCameraRotation(cameraRotationDegrees);
                    if (WorldSpaceBackgroundUIHandler.instance != null) WorldSpaceBackgroundUIHandler.instance.SetCameraRotation(cameraRotationDegrees);
                    break;
                default:
                    break;
            }
        }

        // Use this for handling ValueSlider settings with string values
        /// <summary>
        /// Called by a setting callback when a setting has been changed.
        /// </summary>
        /// <param name="name">The name of the changed setting.</param>
        /// <param name="text">The setting's new string value.</param>
        private void SettingChanged_Text(string name, string text)
        {
            switch (name)
            {
                //case settingName_beepRepeatTime:
                //    float beepRepeatTime;
                //    if (float.TryParse(text, out beepRepeatTime))
                //    {
                //        RenderSystem.data_circuitBeepRepeatDelay = beepRepeatTime;
                //    }
                //    break;
                //case settingName_antiAliasing:
                //    if (text.Equals("0") || text.Equals("2") || text.Equals("4") || text.Equals("8"))
                //    {
                //        int aa = int.Parse(text);
                //        uiHandler.sim.renderSystem.SetAntiAliasing(aa);
                //    }
                //    else
                //    {
                //        Log.Error("Setting: AA: Wrong value for callback! (" + text + ")");
                //    }
                //    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Called by a setting callback when a setting has been changed.
        /// </summary>
        /// <param name="name">The name of the changed setting.</param>
        /// <param name="isOn">The setting's new bool value.</param>
        private void SettingChanged_Toggle(string name, bool isOn)
        {
            switch (name)
            {
                case settingName_fullscreen:
                    if (isOn)
                    {
                        // Enable Fullscreen
                        if (Screen.fullScreen == false)
                        {
                            nonFullScreenResolution = new Vector2Int(Screen.width, Screen.height);
                            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                        }
                    }
                    else
                    {
                        // Disable Fullscreen
                        if (Screen.fullScreen)
                        {
                            Screen.SetResolution(nonFullScreenResolution.x, nonFullScreenResolution.y, false);
                        }
                    }
                    break;
                case settingName_circuitBorder:
                    RenderSystem.flag_circuitBorderActive = isOn;
                    // Reinit RenderBatches to apply changes
                    uiHandler.sim.renderSystem.rendererP.circuitAndBondRenderer.ReinitBatches();
                    break;
                case settingName_compassOvArrows:
                    WorldSpaceUIHandler.instance.showCompassDirArrows = isOn;
                    WorldSpaceUIHandler.instance.Refresh();
                    break;
                case settingName_circularRing:
                    RenderSystem.flag_showCircuitViewOuterRing = isOn;
                    break;
                case settingName_animationsOnOff:
                    RenderSystem.animationsOn = isOn;
                    break;
                case settingName_cameraPosWorldOrGrid:
                    ToggleCamPositionWorldGrid(isOn);
                    break;
                case settingName_toggleTooltips:
                    TooltipHandler.Instance.Enabled = isOn;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Called by a setting callback when a setting has been changed.
        /// </summary>
        /// <param name="name">The name of the changed setting.</param>
        /// <param name="value">The setting's new string value.</param>
        private void SettingChanged_Dropdown(string name, string value)
        {
            switch (name)
            {
                default:
                    break;
            }
        }

        private void SettingChanged_BeepFailureProb(string name, string value)
        {
            uiHandler.sim.system.BeepFailureProb = float.Parse(setting_beepFailureProb.GetValueString());
            setting_beepFailureProb.SetValueString(uiHandler.sim.system.BeepFailureProb.ToString());
        }

        /// <summary>
        /// Reads the current position and size values from the
        /// Settings Panel and applies them to the camera.
        /// </summary>
        private void Button_CameraApply()
        {
            float x = float.Parse(setting_cameraPosX.GetValueString());
            float y = float.Parse(setting_cameraPosY.GetValueString());
            // Convert to world coordinates if necessary
            if (camPosInGridCoords)
            {
                Vector2 pos = new Vector2(x, y);
                pos = AmoebotFunctions.GridToWorldPositionVector2(pos);
                x = pos.x;
                y = pos.y;
            }
            float size = float.Parse(setting_cameraSize.GetValueString());
            MouseController.instance.SetCameraPosition(x, y);
            MouseController.instance.SetOrthographicSize(size);
        }

        /// <summary>
        /// Stores the current settings in the configuration and
        /// saves the config file.
        /// </summary>
        private void Button_SaveSettings()
        {
            // Copy current settings to config
            ConfigData data = Config.ConfigData;
            data.settingsMenu.movementAnimationsOn = RenderSystem.animationsOn;
            data.settingsMenu.drawCompassOverlayAsArrows = WorldSpaceUIHandler.instance.showCompassDirArrows;
            data.settingsMenu.drawCircuitBorder = RenderSystem.flag_circuitBorderActive;
            data.settingsMenu.drawParticleRing = RenderSystem.flag_showCircuitViewOuterRing;
            data.settingsMenu.fullscreen = Screen.fullScreen;
            data.settingsMenu.showTooltips = TooltipHandler.Instance.Enabled;
            data.settingsMenu.beepFailureProbability = uiHandler.sim.system.BeepFailureProb;

            // Save configuration file
            Config.SaveConfigData();
        }

        /// <summary>
        /// Changes the displayed camera coordinates to be
        /// world or grid coordinates.
        /// </summary>
        /// <param name="grid">If <c>true</c>, display grid
        /// coordinates, otherwise display world coordinates.</param>
        private void ToggleCamPositionWorldGrid(bool grid)
        {
            if (grid == camPosInGridCoords)
                return;

            if (grid)
            {
                // Convert world to grid coordinates
                Vector2 worldPos = new Vector2(float.Parse(setting_cameraPosX.GetValueString()), float.Parse(setting_cameraPosY.GetValueString()));
                Vector2 gridPos = AmoebotFunctions.WorldToGridPositionF(worldPos);
                setting_cameraPosX.SetValueString(gridPos.x.ToString());
                setting_cameraPosY.SetValueString(gridPos.y.ToString());
            }
            else
            {
                // Convert grid to world coordinates
                Vector2 gridPos = new Vector2(float.Parse(setting_cameraPosX.GetValueString()), float.Parse(setting_cameraPosY.GetValueString()));
                Vector2 worldPos = AmoebotFunctions.GridToWorldPositionVector2(gridPos);
                setting_cameraPosX.SetValueString(worldPos.x.ToString());
                setting_cameraPosY.SetValueString(worldPos.y.ToString());
            }
            camPosInGridCoords = grid;
        }

        /// <summary>
        /// Updates the fields related to the camera position and
        /// size in the Settings Panel.
        /// </summary>
        /// <param name="x">The x world coordinate of the camera.</param>
        /// <param name="y">The y world coordinate of the camera.</param>
        /// <param name="size">The orthographic size of the camera.</param>
        public void UpdateCameraData(float x, float y, float size)
        {
            // Convert to grid coordinates if necessary
            if (camPosInGridCoords)
            {
                Vector2 pos = new Vector2(x, y);
                pos = AmoebotFunctions.WorldToGridPositionF(pos);
                x = pos.x;
                y = pos.y;
            }
            setting_cameraPosX.SetValueString(x.ToString());
            setting_cameraPosY.SetValueString(y.ToString());
            setting_cameraSize.SetValueString(size.ToString());
        }

        /// <summary>
        /// Activates/Deactivates the settings panel depending on its active state.
        /// Connected to the Settings button in the Scene.
        /// </summary>
        public void Button_SettingsPressed()
        {
            settingsPanel.SetActive(!settingsPanel.activeInHierarchy);
        }

        /// <summary>
        /// Closes the settings panel.
        /// </summary>
        public void Close()
        {
            settingsPanel.SetActive(false);
        }

    }
}