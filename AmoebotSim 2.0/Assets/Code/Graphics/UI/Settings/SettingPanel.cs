using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public abstract class SettingPanel
{
    // References
    protected SettingsHandler settings;

    // Data
    protected GameObject go;

    public SettingPanel(SettingsHandler settings)
    {
        this.settings = settings;
    }
}

public class SettingPanel_Slider : SettingPanel
{

    private Slider slider;

    public SettingPanel_Slider(SettingsHandler settings, GameObject parent, float minValue, float maxValue, float value, bool wholeNumbers) : base(settings)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.template_setting_slider, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Values
        slider = go.GetComponentInChildren<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.wholeNumbers = wholeNumbers;

        Log.Debug("Object Instantiated!");
    }

    public Slider GetSlider()
    {
        return slider;
    }
}

public class SettingPanel_Text
{

}