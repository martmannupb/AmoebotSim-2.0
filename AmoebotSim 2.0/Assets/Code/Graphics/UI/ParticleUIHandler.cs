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
    private UIHandler uiHandler;
    // UI References
    public GameObject go_particlePanel;
    public GameObject go_attributeParent;

    // Data
    private Particle particle;
    private Dictionary<string, IParticleAttribute> attributeNameToIParticleAttribute = new Dictionary<string, IParticleAttribute>();

    private void Start()
    {
        // Singleton
        instance = this;

        // Hide Panel
        HideParticlePanel();
        // Clear Panel
        ClearAttributes();
    }

    private void ReinitParticlePanel(Particle p)
    {
        // Clear
        ClearAttributes();

        // Reinit
        this.particle = p;
        foreach (var attribute in p.GetAttributes())
        {
            AddAttribute(attribute);
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
        }
        else if(type == typeof(int))
        {
            UISetting_Text setting = new UISetting_Text(go_attributeParent, particleAttribute.ToString_AttributeName(), particleAttribute.ToString_AttributeValue(), UISetting_Text.InputType.Int);
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Text;
        }
        else if(type == typeof(Direction))
        {
            string[] choices = System.Enum.GetNames(typeof(Direction));
            UISetting_Dropdown setting = new UISetting_Dropdown(go_attributeParent, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Dropdown;
        }
        else if(type.IsEnum) // Enum (other than Direction)
        {
            string[] choices = System.Enum.GetNames(type);
            UISetting_Dropdown setting = new UISetting_Dropdown(go_attributeParent, particleAttribute.ToString_AttributeName(), choices, particleAttribute.ToString_AttributeValue());
            setting.GetGameObject().name = particleAttribute.ToString_AttributeName();
            setting.onValueChangedEvent += SettingChanged_Dropdown;
        }
    }

    /// <summary>
    /// Updates the values in the particle panel. Helpful when values have changed (e.g. after a round).
    /// </summary>
    public void UpdateValues()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Locks the editing of the attributes.
    /// </summary>
    public void LockAttributes()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Unlocks the editing of the attributes.
    /// </summary>
    public void UnlockAttributes()
    {
        throw new System.NotImplementedException();
    }

    public void ClearAttributes()
    {
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
                return;
            }
        }
    }

    public void ShowParticlePanel(Particle p)
    {
        ReinitParticlePanel(p);
        go_particlePanel.SetActive(true);
    }

    public void HideParticlePanel()
    {
        go_particlePanel.SetActive(false);
    }



    // Callbacks

    private void SettingChanged_Value(string name, float value)
    {
        foreach (var attribute in particle.GetAttributes())
        {
            if (attribute.ToString_AttributeName().Equals(name))
            {
                attribute.UpdateAttributeValue(value.ToString());
                return;
            }
        }
    }

    private void SettingChanged_Text(string name, string text)
    {
        foreach (var attribute in particle.GetAttributes())
        {
            if(attribute.ToString_AttributeName().Equals(name))
            {
                attribute.UpdateAttributeValue(text);
                return;
            }
        }
    }

    private void SettingChanged_Toggle(string name, bool isOn)
    {
        foreach (var attribute in particle.GetAttributes())
        {
            if (attribute.ToString_AttributeName().Equals(name))
            {
                attribute.UpdateAttributeValue(isOn ? "True" : "False");
                return;
            }
        }
    }

    private void SettingChanged_Dropdown(string name, string value)
    {
        foreach (var attribute in particle.GetAttributes())
        {
            if (attribute.ToString_AttributeName().Equals(name))
            {
                attribute.UpdateAttributeValue(value);
                return;
            }
        }
    }

}
