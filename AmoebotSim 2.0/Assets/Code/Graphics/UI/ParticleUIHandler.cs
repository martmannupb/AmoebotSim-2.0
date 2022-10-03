using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

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
    private Particle particle;
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

    public void Open(Particle p)
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

    public Particle GetShownParticle()
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
            }
            else
            {
                // Not in latest round
                LockAttributes();
            }
        }
    }

    private void ReinitParticlePanel(Particle p)
    {
        // Clear
        ClearPanel();

        // Reinit
        this.particle = p;
        // Header
        RefreshHeader();

        // Attributes
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
        if (particle != null) headerText.text = "ID: " + particle.id + "\n" + RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.jointMovementState.Description() +
                "\nExp or Contr Dir: "+ RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.globalExpansionOrContractionDir +
                "\nPos Cur (no jms): " + (RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.jointMovementState.isJointMovement ? RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.position1 - RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.jointMovementState.jointExpansionOffset : RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.position1) +
                "\nPos Cur: " + RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_cur.ToString() + "\nPos Prev: " + RendererParticles.instance.GetGraphicsAdapterImpl(particle).state_prev.ToString() +
                "\nPosition: (" + (particle.IsExpanded() ? (particle.Head().x + "," + particle.Head().y + "), (" + particle.Tail().x + "," + particle.Tail().y) : (particle.Head().x + "," + particle.Head().y)) + ")\n" + (particle.IsExpanded() ? "Expanded" : "Contracted") + "\nChirality: " + (particle.chirality ? "CC" : "C") + "\nCompass Dir: " + particle.comDir.ToString();
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
            UISetting_Toggle setting = new UISetting_Toggle(go_attributeParent, particleAttribute.ToString_AttributeName(), ((ParticleAttribute_Bool)particleAttribute).GetValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Toggle;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if(type == typeof(int))
        {
            UISetting_Text setting = new UISetting_Text(go_attributeParent, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Int);
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Text;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if(type == typeof(Direction))
        {
            string[] choices = System.Enum.GetNames(typeof(Direction));
            UISetting_Dropdown setting = new UISetting_Dropdown(go_attributeParent, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Dropdown;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
            settings.Add(particleAttribute.ToString_AttributeName(), setting);
        }
        else if(type.IsEnum) // Enum (other than Direction)
        {
            string[] choices = System.Enum.GetNames(type);
            UISetting_Dropdown setting = new UISetting_Dropdown(go_attributeParent, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Dropdown;
            setting.backgroundButton_onButtonPressedEvent += AttributeClicked;
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
        if(IsOpen())
        {
            IParticleAttribute attribute;
            attributeNameToIParticleAttribute.TryGetValue(name, out attribute);
            if(attribute != null)
            {
                // An attribute has been clicked in the open particle panel
                // Null Check
                if(WorldSpaceUIHandler.instance == null) 
                {
                    Log.Error("ParticleUIHandler: AttributeClicked: WorldSpaceUIHandler.instance is null!");
                    return;
                }

                WorldSpaceUIHandler.instance.DisplayText(WorldSpaceUIHandler.TextType.Attribute, name);
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
    }

    private bool AttributeChangeValid()
    {
        return go_particlePanel.activeSelf && sim.system.IsInLatestRound() && sim.running == false;
    }

}
