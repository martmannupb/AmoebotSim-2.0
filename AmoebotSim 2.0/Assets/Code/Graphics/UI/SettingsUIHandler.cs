using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class SettingsUIHandler : MonoBehaviour
{

    // References
    private UIHandler uiHandler;

    // Data
    public GameObject settingsPanel;
    public GameObject settingsParent;
    private Vector2Int nonFullScreenResolution = new Vector2Int(-1, -1);

    public enum SettingType
    {
        Slider, TextInt, TextFloat
    }

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
        UISetting_Toggle setting_animationsOnOff = new UISetting_Toggle(null, settingsParent.transform, "Animations On/Off", RenderSystem.animationsOn);
        setting_animationsOnOff.onValueChangedEvent += SettingChanged_Toggle;
        // Circuit Beep Repeating
        UISetting_Toggle setting_beepRepeat = new UISetting_Toggle(null, settingsParent.transform, "Beep Repeat On/Off", RenderSystem.data_circuitBeepRepeatOn);
        setting_beepRepeat.onValueChangedEvent += SettingChanged_Toggle;
        // Circuit Beep Repeating Time
        UISetting_ValueSlider setting_beepRepeatTime = new UISetting_ValueSlider(null, settingsParent.transform, "Beep Repeat Time (s)", new string[] { "1", "2", "4", "8", "16" }, 2);
        setting_beepRepeatTime.onValueChangedEventString += SettingChanged_Text;
        // Header
        UISetting_Spacing setting_spacing = new UISetting_Spacing(null, settingsParent.transform, "Spacing");
        UISetting_Header setting_header_graphics = new UISetting_Header(null, settingsParent.transform, "Graphics");
        // Fullscreen
        UISetting_Toggle setting_fullscreen = new UISetting_Toggle(null, settingsParent.transform, "Fullscreen", false);
        setting_fullscreen.onValueChangedEvent += SettingChanged_Toggle;
        // Camera Angle
        UISetting_Slider setting_cameraAngle = new UISetting_Slider(null, settingsParent.transform, "Camera Angle", 0f, 11f, 0f, true);
        setting_cameraAngle.onValueChangedEvent += SettingChanged_Value;
        // Circuit Connections Look
        UISetting_Toggle setting_circuitConnectionBorders = new UISetting_Toggle(null, settingsParent.transform, "Circuit Border", RenderSystem.flag_circuitBorderActive);
        setting_circuitConnectionBorders.onValueChangedEvent += SettingChanged_Toggle;
        // Graph View Outter Ring
        UISetting_Toggle setting_graphViewOutterRing = new UISetting_Toggle(null, settingsParent.transform, "Circular Ring", RenderSystem.flag_showCircuitViewOutterRing);
        setting_graphViewOutterRing.onValueChangedEvent += SettingChanged_Toggle;
        // Anti Aliasing
        UISetting_ValueSlider setting_antiAliasing = new UISetting_ValueSlider(null, settingsParent.transform, "Anti Aliasing", new string[] { "0", "2", "4", "8" }, 3);
        setting_antiAliasing.onValueChangedEventString += SettingChanged_Text;
    }

    /// <summary>
    /// Called by a setting callback when a setting has been changed.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    private void SettingChanged_Value(string name, float value)
    {
        switch (name)
        {
            case "Camera Angle":
                float cameraRotationDegrees = 30f * value;
                Camera.main.transform.rotation = Quaternion.Euler(0, 0, cameraRotationDegrees);
                // Notify Systems
                if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.SetCameraRotation(cameraRotationDegrees);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Called by a setting callback when a setting has been changed.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="text"></param>
    private void SettingChanged_Text(string name, string text)
    {
        switch (name)
        {
            case "Beep Repeat Time (s)":
                float beepRepeatTime;
                if(float.TryParse(text, out beepRepeatTime))
                {
                    RenderSystem.data_circuitBeepRepeatDelay = beepRepeatTime;
                }
                break;
            case "Anti Aliasing":
                if(text.Equals("0") || text.Equals("2") || text.Equals("4") || text.Equals("8"))
                {
                    int aa = int.Parse(text);
                    uiHandler.sim.renderSystem.SetAntiAliasing(aa);
                }
                else
                {
                    Log.Error("Setting: AA: Wrong Value for callback!");
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Called by a setting callback when a setting has been changed.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isOn"></param>
    private void SettingChanged_Toggle(string name, bool isOn)
    {
        switch (name)
        {
            case "Fullscreen":
                if(isOn)
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
            case "Circuit Border":
                RenderSystem.flag_circuitBorderActive = isOn;
                // Reinit RenderBatches to apply changes
                uiHandler
                    .sim
                    .renderSystem
                    .rendererP
                    .circuitAndBondRenderer
                    .ReinitBatches();
                break;
            case "Circular Ring":
                RenderSystem.flag_showCircuitViewOutterRing = isOn;
                break;
            case "Animations On/Off":
                RenderSystem.animationsOn = isOn;
                break;
            case "Beep Repeat On/Off":
                RenderSystem.data_circuitBeepRepeatOn = isOn;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Called by a setting callback when a setting has been changed.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
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
