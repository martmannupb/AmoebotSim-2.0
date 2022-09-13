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
    public GameObject settingsParent;

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
        settingsParent.SetActive(false);
        InitSettings();
    }

    private void InitSettings()
    {
        // Settings _________________________
        // Animations On/Off
        UISetting_Toggle setting_animationsOnOff = new UISetting_Toggle(settingsParent, "Animations On/Off", RenderSystem.animationsOn);
        setting_animationsOnOff.onValueChangedEvent += SettingChanged_Toggle;
        // Camera Angle
        UISetting_Slider setting_cameraAngle = new UISetting_Slider(settingsParent, "Camera Angle", 0f, 11f, 0f, true);
        setting_cameraAngle.onValueChangedEvent += SettingChanged_Value;
        // Circuit Connections Look
        UISetting_Toggle setting_circuitConnectionBorders = new UISetting_Toggle(settingsParent, "Circuit Border", SettingsGlobal.circuitBorderActive);
        setting_circuitConnectionBorders.onValueChangedEvent += SettingChanged_Toggle;
        // Anti Aliasing
        UISetting_ValueSlider setting_antiAliasing = new UISetting_ValueSlider(settingsParent, "Anti Aliasing", new string[] { "0", "2", "4", "8" }, 3);
        setting_antiAliasing.onValueChangedEventString += SettingChanged_Text;
        uiHandler.SetAA(8);
    }

    private void SettingChanged_Value(string name, float value)
    {
        switch (name)
        {
            case "Camera Angle":
                float cameraRotation = 30f * value;
                Camera.main.transform.rotation = Quaternion.Euler(0, 0, cameraRotation);
                break;
            default:
                break;
        }
    }

    private void SettingChanged_Text(string name, string text)
    {
        switch (name)
        {
            case "Anti Aliasing":
                if(text.Equals("0") || text.Equals("2") || text.Equals("4") || text.Equals("8"))
                {
                    int aa = int.Parse(text);
                    uiHandler.SetAA(aa);
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


    private void SettingChanged_Toggle(string name, bool isOn)
    {
        switch (name)
        {
            case "Circuit Border":
                SettingsGlobal.circuitBorderActive = isOn;
                // Reinit RenderBatches to apply changes
                uiHandler
                    .sim
                    .renderSystem
                    .rendererP
                    .circuitRenderer
                    .ReinitBatches();
                break;
            case "Animations On/Off":
                RenderSystem.animationsOn = isOn;
                break;
            default:
                break;
        }
    }

    private void SettingChanged_Dropdown(string name, string value)
    {
        switch (name)
        {
            default:
                break;
        }
    }


    public void Button_SettingsPressed()
    {
        settingsParent.SetActive(!settingsParent.activeInHierarchy);
    }








    // Setting Callbacks =========================

    

}
