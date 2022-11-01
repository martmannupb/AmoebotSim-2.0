using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine;

public class ButtonHoldTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler // Note: MonoBehavious + Interfaces replace EventTrigger (which prevented scrolling)
{
    private float timestampPointerDown = 0f;
    private bool pressed = false;

    public Action<float> mouseClickEvent;

    public void OnPointerDown(PointerEventData eventData)
    {
        timestampPointerDown = Time.timeSinceLevelLoad;
        pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(pressed)
        {
            if(mouseClickEvent != null) mouseClickEvent(Time.timeSinceLevelLoad - timestampPointerDown);
            pressed = false;
        }
    }

    /// <summary>
    /// Button press duration.
    /// </summary>
    /// <returns>Returns the time the button is being held down. -1 if not pressed.</returns>
    public float GetPressedTime()
    {
        if (pressed) return Time.timeSinceLevelLoad - timestampPointerDown;
        else return -1;
    }
}

public abstract class UISetting
{
    // Data
    protected GameObject go;
    protected string name;
    // Background Button + Components
    protected Button button;
    protected ButtonHoldTrigger buttonTrigger;
    protected Image buttonBackgroundImage;

    // State
    protected bool locked = false;

    protected void InitBackgroundButton()
    {
        button = go.GetComponent<Button>();
        if(button != null)
        {
            button.onClick.AddListener(delegate { OnButtonPressed(); });
            buttonTrigger = go.AddComponent<ButtonHoldTrigger>();
            buttonTrigger.mouseClickEvent += OnButtonPressedLong;
            buttonBackgroundImage = go.GetComponent<Image>();
        }
    }

    public GameObject GetGameObject()
    {
        return go;
    }

    public string GetName()
    {
        return name;
    }

    public Button GetBackgroundButton()
    {
        return button;
    }

    /// <summary>
    /// Call this each frame if you want support for the interactive bar.
    /// </summary>
    public void InteractiveBarUpdate()
    {
        if(button != null)
        {
            float pressedTime = buttonTrigger.GetPressedTime();
            if(pressedTime == -1 || pressedTime <= 0.2f || locked)
            {
                buttonBackgroundImage.fillAmount = 1f;
            }
            else
            {
                buttonBackgroundImage.fillAmount = Mathf.Clamp(pressedTime / 2f, 0f, 1f);
            }
        }
    }

    // Callbacks

    public Action<string> backgroundButton_onButtonPressedEvent;
    private void OnButtonPressed()
    {
        if(backgroundButton_onButtonPressedEvent != null) backgroundButton_onButtonPressedEvent(name);
    }

    public Action<string, float> backgroundButton_onButtonPressedLongEvent;
    private void OnButtonPressedLong(float duration)
    {
        if (backgroundButton_onButtonPressedLongEvent != null && duration >= 2) backgroundButton_onButtonPressedLongEvent(name, duration);
    }

    /// <summary>
    /// Access to the current value.
    /// </summary>
    /// <returns>The current value of the setting as a string.</returns>
    public abstract string GetValueString();

    public void Lock()
    {
        LockSetting();
        if (button != null) button.enabled = false;
        locked = true;
    }

    public void Unlock()
    {
        UnlockSetting();
        if (button != null) button.enabled = true;
        locked = false;
    }

    public void SetInteractable(bool interactable, bool backgroundInteractable = true)
    {
        button.interactable = backgroundInteractable;
        SetInteractableState(interactable);
    }

    protected abstract void LockSetting();
    protected abstract void UnlockSetting();
    protected abstract void SetInteractableState(bool interactable);
    public void Clear()
    {
        backgroundButton_onButtonPressedEvent = null;
        backgroundButton_onButtonPressedLongEvent = null;
        ClearRefs();
    }
    protected abstract void ClearRefs();
}

public class UISetting_Header : UISetting
{
    /// <summary>
    /// Sets up the logic for the setting.
    /// </summary>
    /// <param name="go">If null, a GameObject is instantiated, otherwise the given object is used.</param>
    /// <param name="parentTransform">If go == null, this is the parent of the newly instantiated GameObject.</param>
    /// <param name="name">The name of the setting.</param>
    public UISetting_Header(GameObject go, Transform parentTransform, string name)
    {
        // Add GameObject
        if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_header, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
    }

    public override string GetValueString()
    {
        return name;
    }

    protected override void LockSetting()
    {
        // empty
    }

    protected override void UnlockSetting()
    {
        // empty
    }

    protected override void SetInteractableState(bool interactable)
    {
        // empty
    }

    protected override void ClearRefs()
    {
        // empty
    }
}

public class UISetting_Spacing : UISetting
{
    protected static int id = 0;

    public UISetting_Spacing(GameObject go, Transform parentTransform, string name)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_spacing, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name + " (" + id++ + ")";
    }

    public override string GetValueString()
    {
        return "";
    }

    protected override void LockSetting()
    {
        // empty
    }

    protected override void UnlockSetting()
    {
        // empty
    }

    protected override void SetInteractableState(bool interactable)
    {
        // empty
    }

    protected override void ClearRefs()
    {
        // empty
    }
}

public class UISetting_Slider : UISetting
{

    private Slider slider;

    public UISetting_Slider(GameObject go, Transform parentTransform, string name, float minValue, float maxValue, float value, bool wholeNumbers)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_slider, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        slider = this.go.GetComponentInChildren<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.wholeNumbers = wholeNumbers;

        // Add Callbacks
        slider.onValueChanged.AddListener(delegate { OnValueChanged(); });

    }

    public override string GetValueString()
    {
        return slider.value.ToString();
    }

    protected override void LockSetting()
    {
        slider.enabled = false;
    }

    protected override void UnlockSetting()
    {
        slider.enabled = true;
    }

    protected override void SetInteractableState(bool interactable)
    {
        slider.interactable = interactable;
    }

    protected override void ClearRefs()
    {
        onValueChangedEvent = null;
    }

    public void UpdateValue(float value)
    {
        slider.value = value;
    }

    // Callbacks

    public Action<string, float> onValueChangedEvent;
    private void OnValueChanged()
    {
        if(onValueChangedEvent != null) onValueChangedEvent(this.name, slider.value);
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

    public UISetting_Text(GameObject go, Transform parentTransform, string name, string text, InputType inputType)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_text, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        input = this.go.GetComponentInChildren<TMP_InputField>();
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
                if (TypeConverter.ConvertStringToFloat(input).conversionSuccessful) return true;
                else return false;
            default:
                return false;
        }
    }

    public override string GetValueString()
    {
        string text = input.text;
        if (inputType == InputType.Float) text = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text);
        return text;
    }

    protected override void LockSetting()
    {
        input.enabled = false;
    }

    protected override void UnlockSetting()
    {
        input.enabled = true;
    }

    protected override void SetInteractableState(bool interactable)
    {
        input.interactable = interactable;
    }

    protected override void ClearRefs()
    {
        onValueChangedEvent = null;
    }

    public void UpdateValue(string text)
    {
        input.text = text;
    }

    // Callbacks

    public Action<string, string> onValueChangedEvent;
    private void OnValueChanged()
    {
        string text = input.text;
        if(IsInputValid(text) == false)
        {
            // Input not valid, reset to old value
            input.text = prevText;
        }
        else
        {
            // Input valid, continue
            if (inputType == InputType.Float) text = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text);
            if (onValueChangedEvent != null) onValueChangedEvent(this.name, text);
        }
    }
}

public class UISetting_Dropdown : UISetting
{
    private TMP_Dropdown dropdown;
    private List<string> options;

    public UISetting_Dropdown(GameObject go, Transform parentTransform, string name, string[] choices, string initialChoice)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_dropdown, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        InitDropdown(choices, initialChoice);
        
        // Add Callbacks
        dropdown.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    public UISetting_Dropdown(GameObject go, Transform parentTransform, string name, Enum[] choices, Enum initialChoice)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_dropdown, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
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
        options = new List<string>();
        foreach (string c in choices) options.Add(c);
        dropdown.AddOptions(options);
        if (options.Contains(initialChoice) == false) Log.Error("Setting_Dropdown: Constructor: choice not contained in choices!");
        dropdown.value = options.IndexOf(initialChoice);
    }

    public void SetValue(Enum value)
    {
        SetValue(value.ToString());
    }

    public void SetValue(string value)
    {
        if (options.Contains(value) == false)
        {
            Log.Error("Setting_Dropdown: SetValue: choice not contained in choices!");
            return;
        }
        dropdown.value = options.IndexOf(value);
    }

    public override string GetValueString()
    {
        return dropdown.options[dropdown.value].text;
    }

    protected override void LockSetting()
    {
        dropdown.enabled = false;
    }

    protected override void UnlockSetting()
    {
        dropdown.enabled = true;
    }

    protected override void SetInteractableState(bool interactable)
    {
        dropdown.interactable = interactable;
    }

    protected override void ClearRefs()
    {
        onValueChangedEvent = null;
    }

    public void UpdateValue(string value)
    {
        List<TMPro.TMP_Dropdown.OptionData> options = dropdown.options;
        for (int i = 0; i < options.Count; i++)
        {
            TMPro.TMP_Dropdown.OptionData option = options[i];
            if(option.text.Equals(value))
            {
                dropdown.value = i;
                return;
            }
        }
        Log.Error("Dropdown Value could not be updated, value not found!");
    }

    // Callbacks

    public Action<string, string> onValueChangedEvent;
    private void OnValueChanged()
    {
        if(onValueChangedEvent != null) onValueChangedEvent(this.name, dropdown.options[dropdown.value].text);
    }
}

public class UISetting_Toggle : UISetting
{
    private Toggle toggle;

    public UISetting_Toggle(GameObject go, Transform parentTransform, string name, bool isOn)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_toggle, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        toggle = this.go.GetComponentInChildren<Toggle>();
        toggle.isOn = isOn;

        // Add Callbacks
        toggle.onValueChanged.AddListener(delegate { OnValueChanged(); });
    }

    public override string GetValueString()
    {
        return toggle.isOn ? "True" : "False";
    }

    protected override void LockSetting()
    {
        toggle.enabled = false;
    }

    protected override void UnlockSetting()
    {
        toggle.enabled = true;
    }

    protected override void SetInteractableState(bool interactable)
    {
        toggle.interactable = interactable;
    }

    protected override void ClearRefs()
    {
        onValueChangedEvent = null;
    }

    public void UpdateValue(bool isOn)
    {
        toggle.isOn = isOn;
    }

    // Callbacks

    public Action<string, bool> onValueChangedEvent;
    private void OnValueChanged()
    {
        if(onValueChangedEvent != null) onValueChangedEvent(this.name, toggle.isOn);
    }
}

public class UISetting_ValueSlider : UISetting
{

    private Slider slider;
    private TMP_InputField input;

    // Mapping
    private bool mappingActive = false;
    private string[] mapping;

    public UISetting_ValueSlider(GameObject go, Transform parentTransform, string name, float minValue, float maxValue, float value, bool wholeNumbers)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_slider, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        InitBackgroundButton();
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        slider = this.go.GetComponentInChildren<Slider>();
        mappingActive = false;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        slider.wholeNumbers = wholeNumbers;
        input = this.go.GetComponentInChildren<TMP_InputField>();
        input.enabled = false;
        input.text = wholeNumbers ? ((int)value).ToString() : value.ToString();

        // Add Callbacks
        slider.onValueChanged.AddListener(delegate { OnValueChanged(); });

    }

    public UISetting_ValueSlider(GameObject go, Transform parentTransform, string name, string[] values, float initialIndex)
    {
        // Add GameObject
        if(go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_valueSlider, Vector3.zero, Quaternion.identity, parentTransform);
        else this.go = go;
        // Set Name
        this.name = name;
        TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        tmpro.text = name;
        // Set Values
        slider = this.go.GetComponentInChildren<Slider>();
        mappingActive = true;
        mapping = values;
        slider.minValue = 0;
        slider.maxValue = values.Length - 1;
        slider.value = initialIndex;
        slider.wholeNumbers = true;
        input = this.go.GetComponentInChildren<TMP_InputField>();
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

    public override string GetValueString()
    {
        if (mappingActive) return mapping[(int)slider.value];
        else return "" + slider.value;
    }

    protected override void LockSetting()
    {
        slider.enabled = false;
    }

    protected override void UnlockSetting()
    {
        slider.enabled = true;
    }

    protected override void SetInteractableState(bool interactable)
    {
        slider.interactable = interactable;
    }

    protected override void ClearRefs()
    {
        onValueChangedEvent = null;
        onValueChangedEventString = null;
    }

    public void UpdateValue(string text)
    {
        if(mappingActive == false)
        {
            Log.Error("ValueSlider: Cannot set a text for a ValueSlider with no mapping!");
            return;
        }

        for (int i = 0; i < mapping.Length; i++)
        {
            if(text.Equals(mapping[i]))
            {
                slider.value = i;
                return;
            }
        }
        Log.Error("ValueSlider: Value could not be updated, value not found!");
    }

    public void UpdateValue(float value)
    {
        if (mappingActive)
        {
            Log.Error("ValueSlider: Cannot set a float for a ValueSlider with a mapping!");
            return;
        }

        slider.value = value;
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