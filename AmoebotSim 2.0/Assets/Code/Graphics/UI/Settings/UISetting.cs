using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine;

namespace AS2.UI
{

    /// <summary>
    /// Listener for the background button for each setting.
    /// </summary>
    public class ButtonHoldTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler // Note: MonoBehaviours + Interfaces replace EventTrigger (which prevented scrolling)
    {
        private float timestampPointerDown = 0f;
        private bool pressed = false;

        /// <summary>
        /// Callback for when the button has been pressed and has
        /// just been released. The float parameter specifies the
        /// time in seconds for which the button was held down.
        /// </summary>
        public Action<float> mouseClickEvent;

        public void OnPointerDown(PointerEventData eventData)
        {
            timestampPointerDown = Time.timeSinceLevelLoad;
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressed)
            {
                if (mouseClickEvent != null) mouseClickEvent(Time.timeSinceLevelLoad - timestampPointerDown);
                pressed = false;
            }
        }

        /// <summary>
        /// Button press duration.
        /// </summary>
        /// <returns>Returns the time the button is being held down. <c>-1</c> if not pressed.</returns>
        public float GetPressedTime()
        {
            if (pressed) return Time.timeSinceLevelLoad - timestampPointerDown;
            else return -1;
        }
    }

    /// <summary>
    /// Superclass of all implemented settings (like dropdowns, toggles, text boxes, etc.).
    /// Used together with the setting prefab GameObjects.
    /// </summary>
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
        protected bool buttonHoldEnabled = true;

        protected void InitBackgroundButton()
        {
            button = go.GetComponent<Button>();
            if (button != null)
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
            if (button != null)
            {
                float pressedTime = buttonTrigger.GetPressedTime();
                if (pressedTime == -1 || pressedTime <= 0.2f || locked || buttonHoldEnabled == false)
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
            if (backgroundButton_onButtonPressedEvent != null) backgroundButton_onButtonPressedEvent(name);
        }

        public Action<string, float> backgroundButton_onButtonPressedLongEvent;
        private void OnButtonPressedLong(float duration)
        {
            if (backgroundButton_onButtonPressedLongEvent != null && duration >= 2 && buttonHoldEnabled) backgroundButton_onButtonPressedLongEvent(name, duration);
        }

        /// <summary>
        /// Access to the current value.
        /// </summary>
        /// <returns>The current value of the setting as a string.</returns>
        public abstract string GetValueString();
        /// <summary>
        /// The inverse of GetValueString().
        /// If calling both methods after each other, nothing should change.
        /// </summary>
        /// <param name="input">String representation of the setting's new value.</param>
        public abstract void SetValueString(string input);

        /// <summary>
        /// Locks the setting so that its value cannot be changed anymore.
        /// </summary>
        /// <param name="lockButton">Indicates whether the background button
        /// should be locked as well.</param>
        public void Lock(bool lockButton = true)
        {
            LockSetting();
            if (button != null && lockButton) button.enabled = false;
            locked = true;
        }

        /// <summary>
        /// Unlocks the setting so that its value can be changed again.
        /// </summary>
        /// <param name="unlockButton">Indicates whether the background
        /// button should be unlocked as well.</param>
        public void Unlock(bool unlockButton = true)
        {
            UnlockSetting();
            if (button != null && unlockButton) button.enabled = true;
            locked = false;
        }

        /// <summary>
        /// Enables the background button hold feature.
        /// </summary>
        public void EnableButtonHold()
        {
            buttonHoldEnabled = true;
        }

        /// <summary>
        /// Disables the background button hold feature.
        /// </summary>
        public void DisableButtonHold()
        {
            buttonHoldEnabled = false;
        }

        /// <summary>
        /// Sets the interactable state of the setting and its background button.
        /// </summary>
        /// <param name="interactable">The new interactable state of the setting.</param>
        /// <param name="backgroundInteractable">The new interactable state of the button.</param>
        public void SetInteractable(bool interactable, bool backgroundInteractable = true)
        {
            button.interactable = backgroundInteractable;
            SetInteractableState(interactable);
        }

        /// <summary>
        /// Locks the setting to prevent changes.
        /// </summary>
        protected abstract void LockSetting();
        /// <summary>
        /// Unlocks the setting to allow changes again after locking.
        /// </summary>
        protected abstract void UnlockSetting();
        /// <summary>
        /// Sets the setting's interactable state.
        /// </summary>
        /// <param name="interactable">The new interactable state.</param>
        protected abstract void SetInteractableState(bool interactable);
        /// <summary>
        /// Clears the callback events of the setting.
        /// </summary>
        public void Clear()
        {
            backgroundButton_onButtonPressedEvent = null;
            backgroundButton_onButtonPressedLongEvent = null;
            ClearRefs();
        }
        /// <summary>
        /// Clears all subclass-specific callback events.
        /// </summary>
        protected abstract void ClearRefs();
    }

    /// <summary>
    /// <see cref="UISetting"/> subclass representing a simple text
    /// to describe the following section of settings.
    /// </summary>
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

        public override void SetValueString(string input)
        {
            // empty
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

    /// <summary>
    /// <see cref="UISetting"/> subclass that only serves as
    /// spacing between other settings. Can be used to create
    /// groups of settings that are visually separated.
    /// </summary>
    public class UISetting_Spacing : UISetting
    {
        protected static int id = 0;

        public UISetting_Spacing(GameObject go, Transform parentTransform, string name)
        {
            // Add GameObject
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_spacing, Vector3.zero, Quaternion.identity, parentTransform);
            else this.go = go;
            InitBackgroundButton();
            // Set Name
            this.name = name + " (" + id++ + ")";
        }

        public override string GetValueString()
        {
            return "";
        }

        public override void SetValueString(string input)
        {
            // empty
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

    /// <summary>
    /// <see cref="UISetting"/> subclass representing a setting that has a
    /// float or integer value with a lower and upper limit so that it can
    /// be set using a slider.
    /// </summary>
    public class UISetting_Slider : UISetting
    {

        private Slider slider;

        public UISetting_Slider(GameObject go, Transform parentTransform, string name, float minValue, float maxValue, float value, bool wholeNumbers)
        {
            // Add GameObject
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_slider, Vector3.zero, Quaternion.identity, parentTransform);
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

        public override void SetValueString(string input)
        {
            TypeConverter.ConversionResult res = TypeConverter.ConvertStringToFloat(input);
            if (res.conversionSuccessful == false)
            {
                Log.Error("UISetting_Slider: SetValueString: Conversion to float failed for string: " + input + ".");
                return;
            }
            slider.value = (float)res.obj;
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
            if (onValueChangedEvent != null) onValueChangedEvent(this.name, slider.value);
        }

        public Slider GetSlider()
        {
            return slider;
        }
    }

    /// <summary>
    /// <see cref="UISetting"/> subclass representing a setting
    /// with a numeric or string value using a simple text input field.
    /// Can be used to read in integer and float values as strings.
    /// </summary>
    public class UISetting_Text : UISetting
    {
        private TMP_InputField input;
        private InputType inputType;
        private string prevText;

        /// <summary>
        /// Possible format types of the text field.
        /// </summary>
        public enum InputType
        {
            Text, Int, Float
        }

        public UISetting_Text(GameObject go, Transform parentTransform, string name, string text, InputType inputType)
        {
            // Add GameObject
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_text, Vector3.zero, Quaternion.identity, parentTransform);
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

        public override void SetValueString(string input)
        {
            if (inputType == InputType.Float)
            {
                TypeConverter.ConversionResult res = TypeConverter.ConvertStringToFloat(input);
                if (res.conversionSuccessful == false)
                {
                    Log.Error("UISetting_Text: SetValueString: Conversion to float failed for string: " + input + ".");
                    return;
                }
                this.input.text = ((float)res.obj).ToString();
            }
            else this.input.text = input;
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
            if (IsInputValid(text) == false)
            {
                // Input not valid, reset to old value
                input.text = prevText;
            }
            else
            {
                // Input valid, continue
                prevText = input.text;
                if (inputType == InputType.Float) text = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text);
                if (onValueChangedEvent != null) onValueChangedEvent(this.name, text);
            }
        }
    }

    /// <summary>
    /// <see cref="UISetting"/> subclass for settings that have a fixed number of
    /// possible values, like enums. The setting is represented as a dropdown menu.
    /// </summary>
    public class UISetting_Dropdown : UISetting
    {
        private TMP_Dropdown dropdown;
        private List<string> options;

        public UISetting_Dropdown(GameObject go, Transform parentTransform, string name, string[] choices, string initialChoice)
        {
            // Add GameObject
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_dropdown, Vector3.zero, Quaternion.identity, parentTransform);
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
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_dropdown, Vector3.zero, Quaternion.identity, parentTransform);
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

        public override void SetValueString(string input)
        {
            // Find dropdown value with given input
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                if (dropdown.options[i].text.Equals(input))
                {
                    dropdown.value = i;
                    return;
                }
            }
            Log.Error("UISetting_Dropdown: SetValueString: Value " + input + " not found in dropdown.");
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
                if (option.text.Equals(value))
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
            if (onValueChangedEvent != null) onValueChangedEvent(this.name, dropdown.options[dropdown.value].text);
        }
    }

    /// <summary>
    /// <see cref="UISetting"/> subclass for simple Boolean settings.
    /// The setting's value can be changed with a toggle button.
    /// </summary>
    public class UISetting_Toggle : UISetting
    {
        private Toggle toggle;

        public UISetting_Toggle(GameObject go, Transform parentTransform, string name, bool isOn)
        {
            // Add GameObject
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_toggle, Vector3.zero, Quaternion.identity, parentTransform);
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

        public override void SetValueString(string input)
        {
            if (input.Equals("True") || input.Equals("true")) toggle.isOn = true;
            else if (input.Equals("False") || input.Equals("false")) toggle.isOn = false;
            else Log.Error("UISetting_Toggle: SetValueString: Input " + input + " could not be converted to true or false.");
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
            if (onValueChangedEvent != null) onValueChangedEvent(this.name, toggle.isOn);
        }
    }

    /// <summary>
    /// <see cref="UISetting"/> subclass for settings that have a range of values
    /// and should be selectable by a slider. The values can be float or integer
    /// numbers or a list of strings for which the slider selects the index.
    /// </summary>
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
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_slider, Vector3.zero, Quaternion.identity, parentTransform);
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
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_valueSlider, Vector3.zero, Quaternion.identity, parentTransform);
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
            if (mappingActive)
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

        public override void SetValueString(string input)
        {
            if (mappingActive)
            {
                // Find matching string in mapping
                for (int i = 0; i < mapping.Length; i++)
                {
                    string curVal = mapping[i];
                    if (curVal.Equals(input))
                    {
                        slider.value = i;
                        return;
                    }
                }
                Log.Error("UISetting_ValueSlider: SetValueString: Value " + input + " not found in mapping.");
            }
            else
            {
                TypeConverter.ConversionResult res = TypeConverter.ConvertStringToFloat(input);
                if (res.conversionSuccessful == false)
                {
                    Log.Error("UISetting_ValueSlider: SetValueString: Conversion to float failed for string: " + input + ".");
                    return;
                }
                slider.value = (float)res.obj;
            }
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
            if (mappingActive == false)
            {
                Log.Error("ValueSlider: Cannot set a text for a ValueSlider with no mapping!");
                return;
            }

            for (int i = 0; i < mapping.Length; i++)
            {
                if (text.Equals(mapping[i]))
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
            if (mappingActive)
            {
                // Mapping active (use mapping to get value)
                if (onValueChangedEventString != null) onValueChangedEventString(this.name, mapping[(int)slider.value]);
            }
            else
            {
                // No Mapping (1-1 conversion from input field)
                if (onValueChangedEvent != null) onValueChangedEvent(this.name, slider.value);
            }
        }

        public Slider GetSlider()
        {
            return slider;
        }
    }

    /// <summary>
    /// <see cref="UISetting"/> subclass for settings that are value
    /// ranges. A value range simply defines a minimum and a maximum
    /// value as either a float or an integer.
    /// </summary>
    public class UISetting_MinMax : UISetting
    {

        private TMP_InputField input1;
        private TMP_InputField input2;
        private InputType inputType;

        // Prev values
        private string input1prev;
        private string input2prev;

        public enum InputType
        {
            Int, Float
        }

        public UISetting_MinMax(GameObject go, Transform parentTransform, string name, float inputMin, float inputMax, InputType inputType)
        {
            // Add GameObject
            if (go == null) this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_minMax, Vector3.zero, Quaternion.identity, parentTransform);
            else this.go = go;
            // Set Name
            this.name = name;
            TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = name;
            // Set Values
            this.inputType = inputType;
            if (inputType == InputType.Int)
            {
                inputMin = (int)inputMin;
                inputMax = (int)inputMax;
            }
            TMP_InputField[] inputFields = this.go.GetComponentsInChildren<TMP_InputField>();
            input1 = inputFields[0];
            input2 = inputFields[1];
            input1.text = "" + inputMin;
            input2.text = "" + inputMax;
            input1prev = input1.text;
            input2prev = input2.text;

            // Add Callbacks
            input1.onValueChanged.AddListener(delegate { OnValueChanged(); });
        }

        public UISetting_MinMax(GameObject go, Transform parentTransform, string name, MinMax minMax) : this(go, parentTransform, name, minMax.min, minMax.max, minMax.wholeNumbersOnly ? InputType.Int : InputType.Float) { }

        protected bool IsInputValid(string input)
        {
            switch (inputType)
            {
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
            string text1 = input1.text;
            string text2 = input2.text;
            if (inputType == InputType.Float)
            {
                text1 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text1);
                text2 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text2);
            }
            string text = text1 + "-" + text2;
            return text;
        }

        public override void SetValueString(string input)
        {
            if (input.Contains('-') == false)
            {
                Log.Error("Setting_MinMax: SetValueString: No - in input!");
                return;
            }
            string text1 = input.Substring(0, input.IndexOf('-'));
            string text2 = input.Substring(input.IndexOf('-') + 1);
            TypeConverter.ConversionResult res1 = TypeConverter.ConvertStringToFloat(text1);
            TypeConverter.ConversionResult res2 = TypeConverter.ConvertStringToFloat(text2);
            if (res1.conversionSuccessful == false || res2.conversionSuccessful == false)
            {
                Log.Error("Setting_MinMax: SetValueString: Conversion to float failed for " + text1 + " and/or " + text2);
                return;
            }
            input1.text = res1.ToString();
            input2.text = res2.ToString();
        }

        protected override void LockSetting()
        {
            input1.enabled = false;
            input2.enabled = false;
        }

        protected override void UnlockSetting()
        {
            input1.enabled = true;
            input2.enabled = true;
        }

        protected override void SetInteractableState(bool interactable)
        {
            input1.interactable = interactable;
            input2.interactable = interactable;
        }

        protected override void ClearRefs()
        {
            onValueChangedEvent = null;
        }

        // Callbacks

        public Action<string, string> onValueChangedEvent;
        private void OnValueChanged()
        {
            bool isValid = true;
            string text1 = input1.text;
            if (IsInputValid(text1) == false)
            {
                // Input not valid, reset to old value
                input1.text = input1prev;
                isValid = false;
            }
            string text2 = input2.text;
            if (IsInputValid(text2) == false)
            {
                // Input not valid, reset to old value
                input2.text = input2prev;
                isValid = false;
            }
            input1prev = input1.text;
            input2prev = input2.text;

            if (isValid)
            {
                // Input valid, continue
                if (inputType == InputType.Float)
                {
                    text1 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text1);
                    text2 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text2);
                }
                string text = input1.text + "-" + input2.text;
                if (onValueChangedEvent != null) onValueChangedEvent(this.name, text);
            }
        }

    }

    /// <summary>
    /// <see cref="UISetting"/> subclass for setting a color.
    /// Colors are represented in RGB format, either as
    /// integers from 0 to 255 or as floats from 0 to 1.
    /// </summary>
    public class UISetting_Color : UISetting
    {
        private TMP_InputField inputR;
        private TMP_InputField inputG;
        private TMP_InputField inputB;
        private InputType inputType;

        // Prev values
        private string inputRprev;
        private string inputGprev;
        private string inputBprev;

        public enum InputType
        {
            Int, Float
        }

        public UISetting_Color(GameObject go, Transform parentTransform, string name, Color start, InputType inputType)
        {
            // Add GameObject
            if (go == null)
                this.go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_setting_color, Vector3.zero, Quaternion.identity, parentTransform);
            else
                this.go = go;
            
            // Set Name
            this.name = name;
            TextMeshProUGUI tmpro = this.go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = name;

            TMP_InputField[] inputFields = this.go.GetComponentsInChildren<TMP_InputField>();
            inputR = inputFields[0];
            inputG = inputFields[1];
            inputB = inputFields[2];
            // Set initial values
            this.inputType = inputType;
            if (inputType == InputType.Int)
            {
                inputR.text = "" + (int)(start.r * 255);
                inputG.text = "" + (int)(start.g * 255);
                inputB.text = "" + (int)(start.b * 255);
            }
            else
            {
                inputR.text = "" + start.r;
                inputR.text = "" + start.g;
                inputR.text = "" + start.b;
            }
            inputRprev = inputR.text;
            inputGprev = inputG.text;
            inputBprev = inputB.text;

            // Add Callbacks
            inputR.onValueChanged.AddListener(delegate { OnValueChanged(); });
            inputG.onValueChanged.AddListener(delegate { OnValueChanged(); });
            inputB.onValueChanged.AddListener(delegate { OnValueChanged(); });
        }

        protected bool IsInputValid(string input)
        {
            switch (inputType)
            {
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

        // TODO

        public override string GetValueString()
        {
            string text1 = inputR.text;
            string text2 = inputB.text;
            if (inputType == InputType.Float)
            {
                text1 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text1);
                text2 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text2);
            }
            string text = text1 + "-" + text2;
            return text;
        }

        public override void SetValueString(string input)
        {
            if (input.Contains('-') == false)
            {
                Log.Error("Setting_MinMax: SetValueString: No - in input!");
                return;
            }
            string text1 = input.Substring(0, input.IndexOf('-'));
            string text2 = input.Substring(input.IndexOf('-') + 1);
            TypeConverter.ConversionResult res1 = TypeConverter.ConvertStringToFloat(text1);
            TypeConverter.ConversionResult res2 = TypeConverter.ConvertStringToFloat(text2);
            if (res1.conversionSuccessful == false || res2.conversionSuccessful == false)
            {
                Log.Error("Setting_MinMax: SetValueString: Conversion to float failed for " + text1 + " and/or " + text2);
                return;
            }
            inputR.text = res1.ToString();
            inputB.text = res2.ToString();
        }

        protected override void LockSetting()
        {
            inputR.enabled = false;
            inputB.enabled = false;
        }

        protected override void UnlockSetting()
        {
            inputR.enabled = true;
            inputB.enabled = true;
        }

        protected override void SetInteractableState(bool interactable)
        {
            inputR.interactable = interactable;
            inputB.interactable = interactable;
        }

        protected override void ClearRefs()
        {
            onValueChangedEvent = null;
        }

        // Callbacks

        public Action<string, string> onValueChangedEvent;
        private void OnValueChanged()
        {
            bool isValid = true;
            string text1 = inputR.text;
            if (IsInputValid(text1) == false)
            {
                // Input not valid, reset to old value
                inputR.text = inputRprev;
                isValid = false;
            }
            string text2 = inputB.text;
            if (IsInputValid(text2) == false)
            {
                // Input not valid, reset to old value
                inputB.text = inputGprev;
                isValid = false;
            }
            inputRprev = inputR.text;
            inputGprev = inputB.text;

            if (isValid)
            {
                // Input valid, continue
                if (inputType == InputType.Float)
                {
                    text1 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text1);
                    text2 = TypeConverter.ConvertStringInStringThatCanBeConvertedToFloat(text2);
                }
                string text = inputR.text + "-" + inputB.text;
                if (onValueChangedEvent != null) onValueChangedEvent(this.name, text);
            }
        }

    }

}