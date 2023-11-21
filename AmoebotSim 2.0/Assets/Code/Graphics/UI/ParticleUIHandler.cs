using AS2.Sim;
using AS2.Visuals;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{

    /// <summary>
    /// Controls the particle panel.
    /// </summary>
    public class ParticleUIHandler : MonoBehaviour
    {

        // Singleton
        public static ParticleUIHandler instance;

        // References
        private AmoebotSimulator sim;

        // UI References
        private TMPro.TextMeshProUGUI headerText;
        public GameObject go_particlePanel;
        private CanvasGroup canvasGroup_particlePanel;
        public GameObject go_contentParent;
        public GameObject go_attributeParent;
        public GameObject go_attributeRandomizationParent;
        public Button button_rootParticle;
        private Image image_rootParticle;

        // Colors
        public Color button_color_default;
        public Color button_color_active;

        // Data
        private IParticleState particle;
        private Dictionary<string, IParticleAttribute> attributeNameToIParticleAttribute = new Dictionary<string, IParticleAttribute>();
        private Dictionary<string, UISetting> settings = new Dictionary<string, UISetting>();
        private Dictionary<string, GameObject> settings_randomization = new Dictionary<string, GameObject>();

        private void Start()
        {
            // Singleton
            instance = this;

            // References
            sim = AmoebotSimulator.instance;

            // Find elements
            canvasGroup_particlePanel = go_particlePanel.GetComponent<CanvasGroup>();
            for (int i = 0; i < go_attributeParent.transform.childCount; i++)
            {
                GameObject go = go_attributeParent.transform.GetChild(i).gameObject;
                if (go.name.Equals("Description"))
                {
                    headerText = go.GetComponent<TextMeshProUGUI>();
                }
            }
            image_rootParticle = button_rootParticle.gameObject.GetComponent<Image>();

            // Register Events
            EventDatabase.event_sim_startedStopped += SimState_RunningToggled;

            // Hide Panel
            Close();
            // Clear Panel
            ClearPanel();
        }

        /// <summary>
        /// Opens the particle panel for the defined particle.
        /// </summary>
        /// <param name="p">The particle that should be shown.</param>
        public void Open(IParticleState p)
        {
            bool inInitMode = sim.uiHandler.initializationUI.IsOpen();
            ReinitParticlePanel(p, inInitMode);
            //go_particlePanel.SetActive(true);
            canvasGroup_particlePanel.alpha = 1f;
            canvasGroup_particlePanel.interactable = true;
            canvasGroup_particlePanel.blocksRaycasts = true;

            // I don't know why this is necessary, but it somehow forces a size update for the content of the scroll view
            go_particlePanel.SetActive(false);
            go_particlePanel.SetActive(true);

            // Event
            EventDatabase.event_particleUI_particlePanelOpenClose?.Invoke(true);
        }

        /// <summary>
        /// Closes the particle panel.
        /// </summary>
        public void Close()
        {
            //go_particlePanel.SetActive(false);
            canvasGroup_particlePanel.alpha = 0f;
            canvasGroup_particlePanel.interactable = false;
            canvasGroup_particlePanel.blocksRaycasts = false;
            ClearPanel();

            // Event
            EventDatabase.event_particleUI_particlePanelOpenClose?.Invoke(false);
        }

        /// <summary>
        /// Checks if the particle panel is open.
        /// </summary>
        /// <returns><c>true</c> if and only if the panel is open, indicated
        /// by its alpha value being 1 (i.e. the panel is not transparent).</returns>
        public bool IsOpen()
        {
            return canvasGroup_particlePanel.alpha == 1f;
        }

        /// <summary>
        /// Returns the currently shown particle in the panel.
        /// Before usage, please check if the panel is open and only use this method if this is the case.
        /// </summary>
        /// <returns>The particle currently associated to the panel.</returns>
        public IParticleState GetShownParticle()
        {
            return particle;
        }

        /// <summary>
        /// Called every frame. Used for dynamic updates of the visual interface.
        /// </summary>
        public void UpdateUI()
        {
            if (go_particlePanel.activeSelf)
            {
                // Read Sim State
                if (!sim.running && sim.system.IsInLatestRound())
                {
                    // In Latest round and paused
                    UnlockAttributes();
                    foreach (var setting in settings.Values)
                    {
                        setting.InteractiveBarUpdate();
                    }
                }
                else
                {
                    // Not in latest round
                    LockAttributes();
                }
            }
        }

        /// <summary>
        /// Internal method. Reopens the particle panel with the specified particle. Called by the Open(..) method.
        /// </summary>
        /// <param name="p">The particle for which the panel should be initialized.</param>
        /// <param name="initMode">Indicates whether or not the system is in Init Mode.</param>
        private void ReinitParticlePanel(IParticleState p, bool initMode)
        {
            // Clear
            ClearPanel();

            // Reinit
            this.particle = p;
            // Header
            RefreshHeader();

            // Status info methods
            if (!initMode)
                AddStatusInfoButtons(p);

            // Attributes
            AddAttributes_ChiralityAndCompassDir(p, initMode);
            foreach (var attribute in p.GetAttributes())
            {
                AddAttribute(attribute);
            }
        }

        /// <summary>
        /// Updates the values in the particle panel when shown. Helpful when values have changed (e.g. after a round).
        /// </summary>
        private void RefreshParticlePanel()
        {
            if (particle != null)
            {
                RefreshHeader();
                RefreshAttributes_ChiralityAndCompassDir();
                foreach (var attribute in particle.GetAttributes())
                {
                    RefreshAttribute(attribute);
                }
            }
        }

        /// <summary>
        /// Updates the header (position, expansion and anchor information).
        /// </summary>
        private void RefreshHeader()
        {

            // Text
            if (particle != null)
            {
                // Text
                headerText.text = "Position: (" + (particle.IsExpanded() ?
                    (particle.Head().x + "," + particle.Head().y + "), (" + particle.Tail().x + "," + particle.Tail().y)
                    : (particle.Head().x + "," + particle.Head().y)) + ")\n" + (particle.IsExpanded() ? "Expanded" : "Contracted");
                // Color
                if (particle.IsAnchor())
                {
                    image_rootParticle.color = button_color_active;
                    button_rootParticle.interactable = false;
                }
                else
                {
                    image_rootParticle.color = button_color_default;
                    button_rootParticle.interactable = true;
                }
                if (sim.system.IsInLatestRound() == false) button_rootParticle.interactable = false;
            }
        }

        /// <summary>
        /// Updates chirality and compass dir.
        /// </summary>
        private void RefreshAttributes_ChiralityAndCompassDir()
        {
            if (IsOpen() == false) return;

            UISetting settingDef;
            if (settings.TryGetValue("Chirality", out settingDef))
            {
                UISetting_Dropdown setting = (UISetting_Dropdown)settingDef;
                setting.UpdateValue(particle.Chirality() ? "CounterClockwise" : "Clockwise");
            }
            if (settings.TryGetValue("Compass Dir", out settingDef))
            {
                UISetting_Dropdown setting = (UISetting_Dropdown)settingDef;
                setting.UpdateValue(particle.CompassDir().ToString());
            }
        }

        /// <summary>
        /// Updates the specific attribute.
        /// </summary>
        /// <param name="particleAttribute">The attribute to be refreshed.</param>
        private void RefreshAttribute(IParticleAttribute particleAttribute)
        {
            if (attributeNameToIParticleAttribute.ContainsKey(particleAttribute.ToString_AttributeName()))
            {
                // Attribute is listed
                System.Type type = particleAttribute.GetAttributeType();
                if (type == typeof(bool))
                {
                    UISetting_Toggle setting = (UISetting_Toggle)settings[particleAttribute.ToString_AttributeName()];
                    setting.UpdateValue(((ParticleAttribute_Bool)particleAttribute).GetValue());
                }
                else if (type == typeof(int))
                {
                    UISetting_Text setting = (UISetting_Text)settings[particleAttribute.ToString_AttributeName()];
                    setting.UpdateValue(particleAttribute.ToString_AttributeValue());
                }
                else if (type == typeof(float))
                {
                    UISetting_Text setting = (UISetting_Text)settings[particleAttribute.ToString_AttributeName()];
                    setting.UpdateValue(particleAttribute.ToString_AttributeValue());
                }
                else if (type == typeof(string))
                {
                    UISetting_Text setting = (UISetting_Text)settings[particleAttribute.ToString_AttributeName()];
                    setting.UpdateValue(particleAttribute.ToString_AttributeValue());
                }
                else if (type == typeof(Direction))
                {
                    UISetting_Dropdown setting = (UISetting_Dropdown)settings[particleAttribute.ToString_AttributeName()];
                    setting.UpdateValue(particleAttribute.ToString_AttributeValue());
                }
                else if (type.IsEnum) // Enum (other than Direction)
                {
                    UISetting_Dropdown setting = (UISetting_Dropdown)settings[particleAttribute.ToString_AttributeName()];
                    setting.UpdateValue(particleAttribute.ToString_AttributeValue());
                }
            }
        }

        /// <summary>
        /// Adds the chirality and compass dir attributes to the particle panel attributes.
        /// These attributes are contained in all possible particles.
        /// </summary>
        /// <param name="p">The selected particle.</param>
        /// <param name="initMode">Indicates whether or not the system is in Init Mode.</param>
        public void AddAttributes_ChiralityAndCompassDir(IParticleState p, bool initMode)
        {
            // Chirality
            bool chirality = p.Chirality();
            string[] choices = new string[] { "Clockwise", "CounterClockwise" };
            UISetting_Dropdown setting = new UISetting_Dropdown(null, go_attributeParent.transform, "Chirality", choices, chirality ? "CounterClockwise" : "Clockwise");
            setting.GetGameObject().name = "Chirality";
            setting.onValueChangedEvent += SettingChanged_Dropdown;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add("Chirality", setting);
            if(initMode)
            {
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add("Chirality", go_randomization);
                InitRandomizationGameObject(go_randomization, "Chirality");
            }
            else
            {
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_placeholder, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add("Chirality", go_randomization);
            }
            // Compass Dir
            Direction compassDir = p.CompassDir();
            string[] choicesTemp = System.Enum.GetNames(typeof(Initialization.Compass));
            choices = new string[choicesTemp.Length - 1];
            int curIndex = 0;
            for (int i = 0; i < choicesTemp.Length; i++)
            {
                if (choicesTemp[i].Equals("Random") == false)
                {
                    choices[curIndex] = choicesTemp[i];
                    curIndex++;
                }
            }
            setting = new UISetting_Dropdown(null, go_attributeParent.transform, "Compass Dir", choices, compassDir.ToString());
            setting.GetGameObject().name = "Compass Dir";
            setting.onValueChangedEvent += SettingChanged_Dropdown;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add("Compass Dir", setting);
            if(initMode)
            {
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add("Compass Dir", go_randomization);
                InitRandomizationGameObject(go_randomization, "Compass Dir");
            }
            else
            {
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_placeholder, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add("Compass Dir", go_randomization);
            }
        }

        /// <summary>
        /// Adds a particle attribute to the particle panel.
        /// </summary>
        /// <param name="particleAttribute">The attribute to be added.</param>
        public void AddAttribute(IParticleAttribute particleAttribute)
        {
            // Duplicate Check
            if (attributeNameToIParticleAttribute.ContainsKey(particleAttribute.ToString_AttributeName()))
            {
                Log.Error("ParticleUIHandler: AddAttribute: Attribute " + particleAttribute.ToString_AttributeName() + " is a duplicate!");
                return;
            }

            // Add to Dictionary
            attributeNameToIParticleAttribute.Add(particleAttribute.ToString_AttributeName(), particleAttribute);
            // Spawn Prefab
            System.Type type = particleAttribute.GetAttributeType();
            if (type == typeof(bool))
            {
                UISetting_Toggle setting = new UISetting_Toggle(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), ((ParticleAttribute_Bool)particleAttribute).GetValue());
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Toggle;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
                InitRandomizationGameObject(go_randomization, particleAttribute.ToString_AttributeName());

            }
            else if (type == typeof(int))
            {
                UISetting_Text setting = new UISetting_Text(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Int);
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Text;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_placeholder, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
            }
            else if (type == typeof(float))
            {
                UISetting_Text setting = new UISetting_Text(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Float);
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Text;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_placeholder, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
            }
            else if (type == typeof(string))
            {
                UISetting_Text setting = new UISetting_Text(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Text);
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Text;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_placeholder, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
            }
            else if (type == typeof(Direction))
            {
                string[] choices = System.Enum.GetNames(typeof(Direction));
                UISetting_Dropdown setting = new UISetting_Dropdown(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Dropdown;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
                InitRandomizationGameObject(go_randomization, particleAttribute.ToString_AttributeName());
            }
            else if (type == typeof(MinMax))
            {
                UISetting_MinMax setting = new UISetting_MinMax(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), MinMax.Parse(particleAttribute.ToString_AttributeValue()));
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Text;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
                InitRandomizationGameObject(go_randomization, particleAttribute.ToString_AttributeName());
            }
            else if (type.IsEnum) // Enum (other than Direction)
            {
                string[] choices = System.Enum.GetNames(type);
                UISetting_Dropdown setting = new UISetting_Dropdown(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
                setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
                setting.onValueChangedEvent += SettingChanged_Dropdown;
                setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
                setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
                settings.Add(particleAttribute.ToString_AttributeName(), setting);
                GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
                settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
                InitRandomizationGameObject(go_randomization, particleAttribute.ToString_AttributeName());
            }
            else
            {
                // Remove Attribute (since we dont display it)
                attributeNameToIParticleAttribute.Remove(particleAttribute.ToString_AttributeName());
            }
        }

        /// <summary>
        /// Adds buttons for calling the current algorithm's status
        /// info methods, in case it has defined any.
        /// </summary>
        /// <param name="p">The currently selected particle.</param>
        private void AddStatusInfoButtons(IParticleState p)
        {
            // Find the currently selected algorithm
            string algo = p.AlgorithmName();
            if (!AlgorithmManager.Instance.IsAlgorithmKnown(algo))
            {
                Log.Error("Current algorithm '" + algo + "' is not known");
                return;
            }
            StatusInfoAttribute[] statusInfoMethods = AlgorithmManager.Instance.GetStatusInfoMethods(algo);

            if (statusInfoMethods is not null && statusInfoMethods.Length > 0)
            {
                for (int i = 0; i < statusInfoMethods.Length; i++)
                {
                    StatusInfoAttribute attr = statusInfoMethods[i];
                    GameObject go_button_toggle = Instantiate(UIDatabase.prefab_button_toggle, go_attributeParent.transform);
                    TextMeshProUGUI tmpro = go_button_toggle.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    tmpro.text = attr.name;
                    Button button_call = go_button_toggle.GetComponentInChildren<Button>();
                    int idx = i;    // Have to do this to capture the correct index in the closure
                                    // Otherwise, the index i will be updated for *every* delegate
                    button_call.onClick.AddListener(delegate { AlgorithmManager.Instance.CallStatusInfoMethod(algo, idx, sim.system, (Particle)p); });

                    Tooltip tt = button_call.gameObject.GetComponentInChildren<Tooltip>();
                    tt.ChangeMessage(attr.tooltip);

                    Toggle toggle_auto = go_button_toggle.GetComponentInChildren<Toggle>();
                    toggle_auto.isOn = attr.autocall;
                    toggle_auto.onValueChanged.AddListener(delegate (bool isOn) { attr.autocall = isOn; });
                    tt = toggle_auto.gameObject.GetComponentInChildren<Tooltip>();
                    tt.ChangeMessage("Auto-Call");
                }
                UISetting_Spacing setting_spacing = new UISetting_Spacing(null, go_attributeParent.transform, "Spacing");
            }
        }

        /// <summary>
        /// Called when an attribute has been clicked. Opens the world space UI overlay
        /// which shows this attribute's state for all particles.
        /// </summary>
        /// <param name="name">The name of the clicked attribute.</param>
        public void AttributeClicked(string name)
        {
            // Null Check
            if (WorldSpaceUIHandler.instance == null)
            {
                Log.Error("ParticleUIHandler: AttributeClicked: WorldSpaceUIHandler.instance is null!");
                return;
            }

            if (IsOpen())
            {
                IParticleAttribute attribute;
                attributeNameToIParticleAttribute.TryGetValue(name, out attribute);
                if (attribute != null)
                {
                    // An attribute has been clicked in the open particle panel
                    WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.Attribute, name);
                }
                else
                {
                    if (name.Equals("Chirality")) WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.Chirality, "Chirality");
                    else if (name.Equals("Compass Dir")) WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.CompassDir, "Compass Dir");
                }
            }
        }

        /// <summary>
        /// Locks the editing of the attributes.
        /// </summary>
        public void LockAttributes()
        {
            foreach (var setting in settings.Values)
            {
                setting.Lock(false);
            }
            foreach (var setting in settings_randomization.Values)
            {
                Button[] buttons = setting.GetComponentsInChildren<Button>(); // first button is random, second is randomize all
                if(buttons.Length > 1)
                {
                    buttons[0].interactable = false;
                    buttons[1].interactable = false;
                }
            }
        }

        /// <summary>
        /// Unlocks the editing of the attributes.
        /// </summary>
        public void UnlockAttributes()
        {
            foreach (var setting in settings.Values)
            {
                setting.Unlock(false);
            }
            foreach (var setting in settings_randomization.Values)
            {
                Button[] buttons = setting.GetComponentsInChildren<Button>(); // first button is random, second is randomize all
                if(buttons.Length > 1)
                {
                    buttons[0].interactable = true;
                    buttons[1].interactable = true;
                }
            }
        }

        /// <summary>
        /// Clears the particle panel. Removes all attributes and texts after the delimiter, also clears all internally stored attributes.
        /// </summary>
        public void ClearPanel()
        {
            particle = null;
            // Clear Settings + Lists
            for (int i = 0; i < go_attributeParent.transform.childCount; i++)
            {
                if (go_attributeParent.transform.GetChild(i).gameObject.name.Equals("Delimiter"))
                {
                    // Found the Delimiter
                    // Delete following children
                    for (int j = go_attributeParent.transform.childCount - 1; j > i; j--)
                    {
                        GameObject go = go_attributeParent.transform.GetChild(j).gameObject;
                        if (attributeNameToIParticleAttribute.ContainsKey(go.name))
                        {
                            // Attribute was properly registered
                            attributeNameToIParticleAttribute.Remove(go.name);
                        }
                        Destroy(go);
                    }
                    // Remove Callbacks + Clear References
                    foreach (var setting in settings.Values)
                    {
                        setting.Clear();
                    }
                    settings.Clear();
                    break;
                }
            }
            // Clear Randomization GameObject
            for (int i = 0; i < go_attributeRandomizationParent.transform.childCount; i++)
            {
                if (go_attributeRandomizationParent.transform.GetChild(i).gameObject.name.Equals("Delimiter"))
                {
                    // Found the Delimiter
                    // Delete following children
                    for (int j = go_attributeRandomizationParent.transform.childCount - 1; j > i; j--)
                    {
                        GameObject go = go_attributeRandomizationParent.transform.GetChild(j).gameObject;
                        Destroy(go);
                    }
                    // Clear References
                    settings_randomization.Clear();
                    break;
                }
            }
        }

        /// <summary>
        /// Notifies the particle panel that the system is running/not running.
        /// Locks or unlocks the particle attributes, so that changes during runtime are not possible.
        /// </summary>
        /// <param name="running"></param>
        public void SimState_RunningToggled(bool running)
        {
            if (running)
            {
                // Running Sim
                LockAttributes();
            }
            else
            {
                // Paused Sim
                UnlockAttributes();
            }
        }

        /// <summary>
        /// Notifies the particle panel that the round has been changed, so it can refresh.
        /// </summary>
        public void SimState_RoundChanged()
        {
            // Refresh Attributes
            RefreshParticlePanel();
        }



        // Callbacks

        /// <summary>
        /// Called when a particle attribute has been clicked and held for some time.
        /// This should take the value of the attribute and set it at all other particles.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="duration">The duration for which it was held down.</param>
        private void SettingHeldDown(string name, float duration)
        {
            if (IsOpen() && duration >= 2f)
            {
                // Setting held down long enough to apply attribute value to all particles (of same type)

                string attributeValue = "";
                IParticleAttribute attribute = particle.TryGetAttributeByName(name);
                if (name.Equals("Chirality"))
                {
                    if (sim.uiHandler.initializationUI.IsOpen()) // only editable in init mode
                    {
                        attributeValue = particle.Chirality() ? "CounterClockwise" : "Clockwise";
                        sim.system.SetSystemChirality(particle.Chirality() ? Initialization.Chirality.CounterClockwise : Initialization.Chirality.Clockwise);
                        Log.Entry("Chirality with value " + attributeValue + " has been applied to all particles of the same type.");
                    }
                    else
                    {
                        Log.Warning("Sorry, the chirality can only be set during init mode.");
                    }
                }
                else if (name.Equals("Compass Dir"))
                {
                    if (sim.uiHandler.initializationUI.IsOpen()) // only editable in init mode
                    {
                        attributeValue = particle.CompassDir().ToString();
                        sim.system.SetSystemCompassDir((Initialization.Compass)particle.CompassDir().ToInt());
                        Log.Entry("Compass Dir with value " + attributeValue + " has been applied to all particles of the same type.");
                    }
                    else
                    {
                        Log.Warning("Sorry, the compass dir can only be set during init mode.");
                    }
                }
                else if (attribute != null)
                {
                    attributeValue = attribute.ToString_AttributeValue();
                    sim.system.ApplyAttributeValueToAllParticles(particle, name);
                    Log.Entry("Setting " + name + " with value " + attributeValue + " has been applied to all particles of the same type.");
                }
                else
                {
                    Log.Error("Setting " + name + " could not be applied since it has not been found!");
                }

                // Refresh UI
                if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
            }
        }

        /// <summary>
        /// Called when one attribute has been changed by the user. Forwards this to the particle system and refreshes the relevant UI.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="value">The new value of the attribute.</param>
        private void SettingChanged_Value(string name, float value)
        {
            if (AttributeChangeValid() == false) return;

            foreach (var attribute in particle.GetAttributes())
            {
                if (attribute.ToString_AttributeName().Equals(name))
                {
                    attribute.UpdateAttributeValue(value.ToString());
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                    return;
                }
            }
        }

        /// <summary>
        /// Called when one attribute has been changed by the user. Forwards this to the particle system and refreshes the relevant UI.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="text">The new value of the attribute.</param>
        private void SettingChanged_Text(string name, string text)
        {
            if (AttributeChangeValid() == false) return;

            foreach (var attribute in particle.GetAttributes())
            {
                if (attribute.ToString_AttributeName().Equals(name))
                {
                    attribute.UpdateAttributeValue(text);
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                    return;
                }
            }
        }

        /// <summary>
        /// Called when one attribute has been changed by the user. Forwards this to the particle system and refreshes the relevant UI.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="text">The new value of the attribute.</param>
        private void SettingChanged_Toggle(string name, bool isOn)
        {
            if (AttributeChangeValid() == false) return;

            foreach (var attribute in particle.GetAttributes())
            {
                if (attribute.ToString_AttributeName().Equals(name))
                {
                    attribute.UpdateAttributeValue(isOn ? "True" : "False");
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                    return;
                }
            }
        }

        /// <summary>
        /// Called when one attribute has been changed by the user. Forwards this to the particle system and refreshes the relevant UI.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="text">The new value of the attribute.</param>
        private void SettingChanged_Dropdown(string name, string value)
        {
            if (AttributeChangeValid() == false) return;

            foreach (var attribute in particle.GetAttributes())
            {
                if (attribute.ToString_AttributeName().Equals(name))
                {
                    attribute.UpdateAttributeValue(value);
                    if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                    return;
                }
            }
            if (name.Equals("Chirality"))
            {
                particle.SetChirality(value.Equals("CounterClockwise") ? true : false);
                if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                return;
            }
            if (name.Equals("Compass Dir"))
            {
                particle.SetCompassDir((Direction)Enum.Parse(typeof(Direction), value));
                if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                return;
            }
        }

        /// <summary>
        /// Returns true if the attribute is allowed to be changed. The particle panel must be
        /// active and the system has to be in the latest round while being paused to do so.
        /// </summary>
        /// <returns></returns>
        private bool AttributeChangeValid()
        {
            return go_particlePanel.activeSelf && sim.system.IsInLatestRound() && sim.running == false;
        }

        /// <summary>
        /// Called when the root particle button has been pressed.
        /// Sets the current particle as root particle.
        /// </summary>
        public void ButtonPressed_RootParticle()
        {
            if(IsOpen() && sim.system.IsInLatestRound())
            {
                particle.MakeAnchor();
                RefreshParticlePanel();
            }
            else if(sim.system.IsInLatestRound() == false)
            {
                Log.Warning("You cannot set the anchor when you are not in the latest round!");
            }
        }

        /// <summary>
        /// Adds listeners to the given randomization button panel.
        /// </summary>
        /// <param name="dicePanel">The panel holding two randomization buttons,
        /// one for setting a single random value and one for setting all random values.</param>
        /// <param name="attributeName">The name of the attribute affected by the randomization.</param>
        public void InitRandomizationGameObject(GameObject dicePanel, string attributeName)
        {
            Button[] buttons = dicePanel.GetComponentsInChildren<Button>();
            buttons[0].onClick.AddListener(delegate { ButtonPressed_Randomize(attributeName, false); });
            buttons[1].onClick.AddListener(delegate { ButtonPressed_Randomize(attributeName, true); });
        }

        /// <summary>
        /// Callback for when a randomization button has been pressed.
        /// Gives the associated attribute a random value on either just
        /// the selected particle or all particles.
        /// </summary>
        /// <param name="attributeName">The name of the associated attribute.</param>
        /// <param name="randomizeAll">Indicates whether all particles should
        /// get a random value for this attribute.</param>
        public void ButtonPressed_Randomize(string attributeName, bool randomizeAll)
        {
            if(IsOpen() && sim.system.IsInLatestRound())
            {
                if(randomizeAll)
                {
                    // Find random values for all attributes
                    bool updateSuccessful = false;
                    if(attributeName.Equals("Chirality"))
                    {
                        updateSuccessful = sim.system.SetSystemChirality(Initialization.Chirality.Random);
                        if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.Chirality, attributeName);
                    }
                    else if(attributeName.Equals("Compass Dir"))
                    {
                        updateSuccessful = sim.system.SetSystemCompassDir(Initialization.Compass.Random);
                        if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.CompassDir, attributeName);
                    }
                    else
                    {
                        sim.system.SetSystemAttributeRandom(attributeName);
                        updateSuccessful = true;
                        if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.Attribute, attributeName);
                    }
                    if(updateSuccessful) Log.Entry("Setting " + attributeName + " has been randomized for all particles of the same type.");
                }
                else
                {
                    // Only find random values for current attribute
                    if(attributeName.Equals("Chirality"))
                    {
                        particle.SetChiralityRandom();
                    }
                    else if (attributeName.Equals("Compass Dir"))
                    {
                        particle.SetCompassDirRandom();
                    }
                    else
                    {
                        IParticleAttribute attribute = particle.TryGetAttributeByName(attributeName);
                        if(attribute != null) attribute.SetRandomValue();
                    }
                }
                if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                RefreshParticlePanel();
            }
        }

    }

}