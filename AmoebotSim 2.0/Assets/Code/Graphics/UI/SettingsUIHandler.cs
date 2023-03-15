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

        // Data
        public GameObject settingsPanel;
        public GameObject settingsParent;
        private Vector2Int nonFullScreenResolution = new Vector2Int(-1, -1);

        // References
        private ParticleUIExtensionSmoothLerp uiLerpScript;

        // Setting names (used to identify which setting has changed)
        private const string settingName_animationsOnOff = "Animations On/Off";
        private const string settingName_beepRepeatOnOff = "Beep Repeat On/Off";
        private const string settingName_beepRepeatTime = "Beep Repeat Time (s)";
        private const string settingName_fullscreen = "Fullscreen";
        private const string settingName_cameraAngle = "Camera Angle";
        private const string settingName_compassOvArrows = "Compass Ov. Arrows";
        private const string settingName_circuitBorder = "Circuit Border";
        private const string settingName_circularRing = "Circular Ring";
        private const string settingName_antiAliasing = "Anti Aliasing";
        private const string settingName_uiAnimations = "UI Animations";

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
        /// </summary>
        private void InitSettings()
        {
            // Defaults _________________________
            uiHandler.sim.renderSystem.SetAntiAliasing(8);
            nonFullScreenResolution = new Vector2Int(Screen.width, Screen.height);
            Screen.SetResolution(nonFullScreenResolution.x, nonFullScreenResolution.y, false);
            // Settings _________________________
            // Header
            UISetting_Header setting_header_animationsBeeps = new UISetting_Header(null, settingsParent.transform, "Animations and Circuits");
            // Animations On/Off
            UISetting_Toggle setting_animationsOnOff = new UISetting_Toggle(null, settingsParent.transform, settingName_animationsOnOff, RenderSystem.animationsOn);
            setting_animationsOnOff.onValueChangedEvent += SettingChanged_Toggle;
            // Circuit Beep Repeating
            UISetting_Toggle setting_beepRepeat = new UISetting_Toggle(null, settingsParent.transform, settingName_beepRepeatOnOff, RenderSystem.data_circuitBeepRepeatOn);
            setting_beepRepeat.onValueChangedEvent += SettingChanged_Toggle;
            // Circuit Beep Repeating Time
            UISetting_ValueSlider setting_beepRepeatTime = new UISetting_ValueSlider(null, settingsParent.transform, settingName_beepRepeatTime, new string[] { "1", "2", "4", "8", "16" }, 2);
            setting_beepRepeatTime.onValueChangedEventString += SettingChanged_Text;
            // Header
            UISetting_Spacing setting_spacing = new UISetting_Spacing(null, settingsParent.transform, "Spacing");
            UISetting_Header setting_header_graphics = new UISetting_Header(null, settingsParent.transform, "Graphics");
            // Fullscreen
            UISetting_Toggle setting_fullscreen = new UISetting_Toggle(null, settingsParent.transform, settingName_fullscreen, false);
            setting_fullscreen.onValueChangedEvent += SettingChanged_Toggle;
            // Camera Angle
            UISetting_Slider setting_cameraAngle = new UISetting_Slider(null, settingsParent.transform, settingName_cameraAngle, 0f, 11f, 0f, true);
            setting_cameraAngle.onValueChangedEvent += SettingChanged_Value;
            // Compass Dir Overlay Display
            UISetting_Toggle setting_compassDirOverlayDisplayType = new UISetting_Toggle(null, settingsParent.transform, settingName_compassOvArrows, WorldSpaceUIHandler.instance.showCompassDirArrows);
            setting_compassDirOverlayDisplayType.onValueChangedEvent += SettingChanged_Toggle;
            // Circuit Connections Look
            UISetting_Toggle setting_circuitConnectionBorders = new UISetting_Toggle(null, settingsParent.transform, settingName_circuitBorder, RenderSystem.flag_circuitBorderActive);
            setting_circuitConnectionBorders.onValueChangedEvent += SettingChanged_Toggle;
            // Graph View Outter Ring
            UISetting_Toggle setting_graphViewOutterRing = new UISetting_Toggle(null, settingsParent.transform, settingName_circularRing, RenderSystem.flag_showCircuitViewOutterRing);
            setting_graphViewOutterRing.onValueChangedEvent += SettingChanged_Toggle;
            // Anti Aliasing
            UISetting_ValueSlider setting_antiAliasing = new UISetting_ValueSlider(null, settingsParent.transform, settingName_antiAliasing, new string[] { "0", "2", "4", "8" }, 3);
            setting_antiAliasing.onValueChangedEventString += SettingChanged_Text;
            // UI Lerp
            uiLerpScript = FindObjectOfType<ParticleUIExtensionSmoothLerp>();
            if(uiLerpScript != null)
            {
                UISetting_Toggle setting_uiLerp = new UISetting_Toggle(null, settingsParent.transform, settingName_uiAnimations, uiLerpScript.GetLerpEnabled());
                setting_uiLerp.onValueChangedEvent += SettingChanged_Toggle;
            }
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

        /// <summary>
        /// Called by a setting callback when a setting has been changed.
        /// </summary>
        /// <param name="name">The name of the changed setting.</param>
        /// <param name="text">The setting's new string value.</param>
        private void SettingChanged_Text(string name, string text)
        {
            switch (name)
            {
                case settingName_beepRepeatTime:
                    float beepRepeatTime;
                    if (float.TryParse(text, out beepRepeatTime))
                    {
                        RenderSystem.data_circuitBeepRepeatDelay = beepRepeatTime;
                    }
                    break;
                case settingName_antiAliasing:
                    if (text.Equals("0") || text.Equals("2") || text.Equals("4") || text.Equals("8"))
                    {
                        int aa = int.Parse(text);
                        uiHandler.sim.renderSystem.SetAntiAliasing(aa);
                    }
                    else
                    {
                        Log.Error("Setting: AA: Wrong value for callback! (" + text + ")");
                    }
                    break;
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
                    RenderSystem.flag_showCircuitViewOutterRing = isOn;
                    break;
                case settingName_animationsOnOff:
                    RenderSystem.animationsOn = isOn;
                    break;
                case settingName_beepRepeatOnOff:
                    RenderSystem.data_circuitBeepRepeatOn = isOn;
                    break;
                case settingName_uiAnimations:
                    uiLerpScript.SetLerpEnabled(isOn);
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