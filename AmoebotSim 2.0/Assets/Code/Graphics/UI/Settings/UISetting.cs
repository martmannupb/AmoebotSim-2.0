using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public abstract class UISetting
{
    // Data
    protected GameObject go;
    protected string name;

    public GameObject GetGameObject()
    {
        return go;
    }
}

public class UISetting_Slider : UISetting
{

    private Slider slider;

    public UISetting_Slider(GameObject parent, string name, float minValue, float maxValue, float value, bool wholeNumbers)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_slider, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        slider = go.GetComponentInChildren<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.wholeNumbers = wholeNumbers;

        // Add Callbacks
        slider.onValueChanged.AddListener(delegate { OnValueChanged(); });

    }

    // Callbacks

    public Action<string, float> onValueChangedEvent;
    private void OnValueChanged()
    {
        onValueChangedEvent(this.name, slider.value);
    }

    public Slider GetSlider()
    {
        return slider;
    }
}

public class UISetting_Text : UISetting
{
    private TMP_InputField input;
    private InputType inputType;
    private string prevText;

    public enum InputType
    {
        Text, Int, Float
    }

    public UISetting_Text(GameObject parent, string name, string text, InputType inputType)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_text, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        input = go.GetComponentInChildren<TMP_InputField>();
        input.text = text;
        // Store Data
        this.inputType = inputType;
        this.prevText = text;
        if (IsInputValid(this.prevText) == false) Log.Error("Setting_Text: Constructor: Input not valid for given input type!");

        // Add Callbacks
        input.onEndEdit.AddListener(delegate { OnValueChanged(); });

    }

    protected bool IsInputValid(string input)
    {
        switch (inputType)
        {
            case InputType.Text:
                return true;
            case InputType.Int:
                int i;
                if (int.TryParse(input, out i)) return true;
                else return false;
            case InputType.Float:
                float f;
                if (float.TryParse(input, out f)) return true;
                else return false;
            default:
                return false;
        }
    }

    // Callbacks

    public Action<string, string> onValueChangedEvent;
    private void OnValueChanged()
    {
        string newInput = input.text;
        if(IsInputValid(newInput) == false)
        {
            // Input not valid, reset to old value
            input.text = prevText;
        }
        else
        {
            // Input valid, continue
            onValueChangedEvent(this.name, input.text);
        }
    }
}

public class UISetting_Dropdown : UISetting
{
    private TMP_Dropdown dropdown;

    public UISetting_Dropdown(GameObject parent, string name, string[] choices, string initialChoice)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_dropdown, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        InitDropdown(choices, initialChoice);
        
        // Add Callbacks
        dropdown.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    public UISetting_Dropdown(SettingsUIHandler settings, GameObject parent, string name, Enum[] choices, Enum initialChoice)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_dropdown, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Values
        string[] stringChoices = new string[choices.Length];
        for (int i = 0; i < choices.Length; i++)
        {
            stringChoices[i] = choices[i].ToString();
        }
        InitDropdown(stringChoices, initialChoice.ToString());

        // Add Callbacks
        dropdown.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }
    protected void InitDropdown(string[] choices, string initialChoice)
    {
        dropdown = go.GetComponentInChildren<TMP_Dropdown>();
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (string c in choices) options.Add(c);
        dropdown.AddOptions(options);
        if (options.Contains(initialChoice) == false) Log.Error("Setting_Dropdown: Constructor: choice not contained in choices!");
        dropdown.value = options.IndexOf(initialChoice);
    }

    // Callbacks

    public Action<string, string> onValueChangedEvent;
    private void OnValueChanged()
    {
        onValueChangedEvent(this.name, dropdown.options[dropdown.value].text);
    }
}

public class UISetting_Toggle : UISetting
{
    private Toggle toggle;

    public UISetting_Toggle(GameObject parent, string name, bool isOn)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_toggle, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        toggle = go.GetComponentInChildren<Toggle>();
        toggle.isOn = isOn;

        // Add Callbacks
        toggle.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    // Callbacks

    public Action<string, bool> onValueChangedEvent;
    private void OnValueChanged()
    {
        onValueChangedEvent(this.name, toggle.isOn);
    }
}

public class UISetting_ValueSlider : UISetting
{

    private Slider slider;
    private TMP_InputField input;

    // Mapping
    private bool mappingActive = false;
    private string[] mapping;

    public UISetting_ValueSlider(GameObject parent, string name, float minValue, float maxValue, float value, bool wholeNumbers)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_slider, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        slider = go.GetComponentInChildren<Slider>();
        mappingActive = false;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.wholeNumbers = wholeNumbers;
        input = go.GetComponentInChildren<TMP_InputField>();
        input.enabled = false;
        input.text = wholeNumbers ? ((int)value).ToString() : value.ToString();

        // Add Callbacks
        slider.onValueChanged.AddListener(delegate { OnValueChanged(); });

    }

    public UISetting_ValueSlider(GameObject parent, string name, string[] values, float initialIndex)
    {
        // Add GameObject
        go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_valueSlider, Vector3.zero, Quaternion.identity, parent.transform);
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        slider = go.GetComponentInChildren<Slider>();
        mappingActive = true;
        mapping = values;
        slider.minValue = 0;
        slider.maxValue = values.Length - 1;
        slider.value = initialIndex;
        slider.wholeNumbers = true;
        input = go.GetComponentInChildren<TMP_InputField>();
        input.enabled = false;
        UpdateInputField();

        // Add Callbacks
        slider.onValueChanged.AddListener(delegate { OnValueChanged(); });

    }

    private void UpdateInputField()
    {
        if(mappingActive)
        {
            // Mapping active (use mapping to get value)
            input.text = mapping[(int)slider.value];
        }
        else
        {
            // No Mapping (1-1 conversion from input field)
            input.text = slider.wholeNumbers ? ((int)slider.value).ToString() : slider.value.ToString();
        }
    }

    // Callbacks

    public Action<string, float> onValueChangedEvent;
    public Action<string, string> onValueChangedEventString;
    private void OnValueChanged()
    {
        UpdateInputField();
        if(mappingActive)
        {
            // Mapping active (use mapping to get value)
            if(onValueChangedEventString != null) onValueChangedEventString(this.name, mapping[(int)slider.value]);
        }
        else
        {
            // No Mapping (1-1 conversion from input field)
            if(onValueChangedEvent != null) onValueChangedEvent(this.name, slider.value);
        }
    }

    public Slider GetSlider()
    {
        return slider;
    }
}