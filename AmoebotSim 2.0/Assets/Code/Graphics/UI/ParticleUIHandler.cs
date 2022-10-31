using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System;

public class ParticleUIHandler : MonoBehaviour
{

    // Singleton
    public static ParticleUIHandler instance;
    
    // References
    private AmoebotSimulator sim;

    // UI References
    public TMPro.TextMeshProUGUI headerText;
    public GameObject go_particlePanel;
    public GameObject go_attributeParent;

    // Data
    private IParticleState particle;
    private Dictionary<string, IParticleAttribute> attributeNameToIParticleAttribute = new Dictionary<string, IParticleAttribute>();
    private Dictionary<string, UISetting> settings = new Dictionary<string, UISetting>();

    private void Start()
    {
        // Singleton
        instance = this;

        // References
        sim = AmoebotSimulator.instance;

        // Find elements
        for (int i = 0; i < go_attributeParent.transform.childCount; i++)
        {
            GameObject go = go_attributeParent.transform.GetChild(i).gameObject;
            if (go.name.Equals("Description"))
            {
                headerText = go.GetComponent<TextMeshProUGUI>();
            }
        }

        // Register Events
        EventDatabase.event_sim_startedStopped += SimState_RunningToggled;

        // Hide Panel
        Close();
        // Clear Panel
        ClearPanel();
    }

    public void Open(IParticleState p)
    {
        bool inInitMode = sim.uiHandler.initializationUI.IsOpen();
        ReinitParticlePanel(p);
        go_particlePanel.SetActive(true);
    }

    public void Close()
    {
        go_particlePanel.SetActive(false);
        ClearPanel();
    }

    public bool IsOpen()
    {
        return go_particlePanel.activeSelf;
    }

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
            if (sim.system.IsInLatestRound())
            {
                // In Latest round
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

    private void ReinitParticlePanel(IParticleState p)
    {
        // Clear
        ClearPanel();

        // Reinit
        this.particle = p;
        // Header
        RefreshHeader();

        // Attributes
        AddAttributes_ChiralityAndCompassDir(p);
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
        if(particle != null)
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
    /// Updates the header.
    /// </summary>
    private void RefreshHeader()
    {
        if (particle != null) headerText.text = "Position: (" + (particle.IsExpanded() ? (particle.Head().x + "," + particle.Head().y + "), (" + particle.Tail().x + "," + particle.Tail().y) : (particle.Head().x + "," + particle.Head().y))
                + ")\n" + (particle.IsExpanded() ? "Expanded" : "Contracted");
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
    /// <param name="particleAttribute"></param>
    private void RefreshAttribute(IParticleAttribute particleAttribute)
    {
        if(attributeNameToIParticleAttribute.ContainsKey(particleAttribute.ToString_AttributeName()))
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

    public void AddAttributes_ChiralityAndCompassDir(IParticleState p)
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
        // Compass Dir
        Direction compassDir = p.CompassDir();
        choices = System.Enum.GetNames(typeof(Direction));
        setting = new UISetting_Dropdown(null, go_attributeParent.transform, "Compass Dir", choices, compassDir.ToString());
        setting.GetGameObject().name = "Compass Dir";
        setting.onValueChangedEvent += SettingChanged_Dropdown;
        setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
        setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
        settings.Add("Compass Dir", setting);
    }

    public void AddAttribute(IParticleAttribute particleAttribute)
    {
        // Duplicate Check
        if(attributeNameToIParticleAttribute.ContainsKey(particleAttribute.ToString_AttributeName()))
        {
            Log.Error("ParticleUIHandler: AddAttribute: Attribute " + particleAttribute.ToString_AttributeName() + " is a duplicate!");
            return;
        }

        // Add to Dictionary
        attributeNameToIParticleAttribute.Add(particleAttribute.ToString_AttributeName(), particleAttribute);
        // Spawn Prefab
        System.Type type = particleAttribute.GetAttributeType();
        if(type == typeof(bool))
        {
            UISetting_Toggle setting = new UISetting_Toggle(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), ((ParticleAttribute_Bool)particleAttribute).GetValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Toggle;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if(type == typeof(int))
        {
            UISetting_Text setting = new UISetting_Text(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Int);
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Text;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if (type == typeof(float))
        {
            UISetting_Text setting = new UISetting_Text(go_attributeParent, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Float);
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Text;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if (type == typeof(string))
        {
            UISetting_Text setting = new UISetting_Text(go_attributeParent, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Text);
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Text;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if(type == typeof(Direction))
        {
            string[] choices = System.Enum.GetNames(typeof(Direction));
            UISetting_Dropdown setting = new UISetting_Dropdown(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Dropdown;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if(type.IsEnum) // Enum (other than Direction)
        {
            string[] choices = System.Enum.GetNames(type);
            UISetting_Dropdown setting = new UISetting_Dropdown(null, go_attributeParent.transform, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Dropdown;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            setting.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else
        {
            // Remove Attribute (since we dont display it)
            attributeNameToIParticleAttribute.Remove(particleAttribute.ToString_AttributeName());
        }
    }

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
            if(attribute != null)
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
            setting.Lock();
        }
    }

    /// <summary>
    /// Unlocks the editing of the attributes.
    /// </summary>
    public void UnlockAttributes()
    {
        foreach (var setting in settings.Values)
        {
            setting.Unlock();
        }
    }

    public void ClearPanel()
    {
        particle = null;
        for (int i = 0; i < go_attributeParent.transform.childCount; i++)
        {
            if (go_attributeParent.transform.GetChild(i).gameObject.name.Equals("Delimiter"))
            {
                // Found the Delimiter
                // Delete following children
                for (int j = go_attributeParent.transform.childCount-1; j > i; j--)
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
                return;
            }
        }
    }

    private bool IsParticlePanelActive()
    {
        return go_particlePanel.activeSelf;
    }

    public void SimState_RunningToggled(bool running)
    {
        if(running)
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

    public void SimState_RoundChanged()
    {
        // Refresh Attributes
        RefreshParticlePanel();
    }

    

    // Callbacks

    private void SettingHeldDown(string name, float duration)
    {
        if(IsOpen() && duration >= 2f)
        {
            // Setting held down long enough to apply attribute value to all particles (of same type)
            
            string attributeValue = "";
            IParticleAttribute attribute = particle.TryGetAttributeByName(name);
            if (name.Equals("Chirality"))
            {
                if(sim.uiHandler.initializationUI.IsOpen()) // only editable in init mode
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
                if(sim.uiHandler.initializationUI.IsOpen()) // only editable in init mode
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

    private void SettingChanged_Text(string name, string text)
    {
        if (AttributeChangeValid() == false) return;
        
        foreach (var attribute in particle.GetAttributes())
        {
            if(attribute.ToString_AttributeName().Equals(name))
            {
                attribute.UpdateAttributeValue(text);
                if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
                return;
            }
        }
    }

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

    private bool AttributeChangeValid()
    {
        return go_particlePanel.activeSelf && sim.system.IsInLatestRound() && sim.running == false;
    }

}
