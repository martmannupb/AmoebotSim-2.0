using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class SettingsHandler : MonoBehaviour
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
        settingsParent.SetActive(false);
        InitSettings();
    }

    private void InitSettings()
    {
        // Test
        SettingPanel_Slider testPanel = new SettingPanel_Slider(this, settingsParent, "Test Value", 0f, 10f, 5f, true);
        
    }

    private void AddSetting()
    {

    }

    public void Button_SettingsPressed()
    {
        settingsParent.SetActive(!settingsParent.activeInHierarchy);
    }








    // Setting Callbacks =========================

    

}
