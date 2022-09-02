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
        InitSettings();
    }

    private void InitSettings()
    {
        // Test
        SettingPanel testPanel = new SettingPanel_Slider(this, settingsParent, 0f, 10f, 5f, true);
    }

    private void AddSetting()
    {

    }

}
