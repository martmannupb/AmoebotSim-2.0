using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using System;

public class WorldSpaceUIHandler : MonoBehaviour
{

    // Singleton
    public static WorldSpaceUIHandler instance;

    // World Space UI
    public GameObject go_worldSpaceUI;
    // Buttons
    public Button button_hideOverlay;

    public struct ParticleTextUIData
    {
        public GameObject go;
        public Vector2 pos;
        public bool isVisible;

        public ParticleTextUIData(GameObject go, Vector2 pos, bool isVisible)
        {
            this.go = go;
            this.pos = pos;
            this.isVisible = isVisible;
        }
    }


    // Data
    public Dictionary<IParticleState, ParticleTextUIData> particleTextUIData = new Dictionary<IParticleState, ParticleTextUIData>();
    // State
    private bool display_isVisible = true;
    private TextType display_type;
    private string display_identifier;

    // Temporary Data
    private Stack<IParticleState> tempParticleStack = new Stack<IParticleState>();

    // Defaults
    private Color color_particleTextBackgroundDefault = new Color(1f, 1f, 1f, 172f / 255f);
    private Color color_particleTextBackgroundTrue = new Color(90f / 255f, 255f / 255f, 99f / 255f, 172f / 255f);
    private Color color_particleTextBackgroundFalse = new Color(255f / 255f, 101f / 255f, 90f / 255f, 172f / 255f);

    // Pooling
    private Stack<GameObject> pool_particleTextUI = new Stack<GameObject>();

    public WorldSpaceUIHandler()
    {
        // Singleton
        instance = this;
    }

    private void Start()
    {
        // Disable Hide Button
        button_hideOverlay.interactable = false;

        // Test
        DisplayText(TextType.Text, "Contract");
        HideAll();
    }

    public enum TextType
    {
        Attribute, Chirality, CompassDir, Text
    }

    /// <summary>
    /// Displays the overlay over every particle that has the given attribute/value/text.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="identifier"></param>
    /// <param name="showOverlay"></param>
    public void DisplayText(TextType type, string identifier, bool showOverlay = true)
    {
        // Save what we display
        this.display_type = type;
        this.display_identifier = identifier;
        // Update Texts
        foreach (var particle in particleTextUIData.Keys)
        {
            tempParticleStack.Push(particle);
        }
        while(tempParticleStack.Count > 0)
        {
            IParticleState particle = tempParticleStack.Pop();
            DisplayTextForParticle(particle);
        }
        tempParticleStack.Clear();
        // Print Message
        //switch (type)
        //{
        //    case TextType.Attribute:
        //        Log.Entry("Showing: Overlay for particle attribute " + identifier + ".");
        //        break;
        //    case TextType.Chirality:
        //        Log.Entry("Showing: Overlay for particle chiralities.");
        //        break;
        //    case TextType.CompassDir:
        //        Log.Entry("Showing: Overlay for compass direction.");
        //        break;
        //    case TextType.Text:
        //        //Log.Entry("This is text..");
        //        break;
        //    default:
        //        break;
        //}
        // Show
        if(showOverlay) ShowVisible();
    }

    /// <summary>
    /// Refreshes the overlay if it is already shown. Call this when something has changed in the shown attributes.
    /// </summary>
    public void Refresh()
    {
        if(display_isVisible)
        {
            DisplayText(display_type, display_identifier);
        }
    }

    private void DisplayTextForParticle(IParticleState particle)
    {
        ParticleTextUIData data = particleTextUIData[particle];

        switch (this.display_type)
        {
            case TextType.Attribute:
                IParticleAttribute attribute = particle.TryGetAttributeByName(display_identifier);
                if(attribute != null)
                {
                    // Show attribute
                    if (attribute is ParticleAttribute_Bool) UpdateParticleText(particle, ((ParticleAttribute_Bool)attribute).GetValue());
                    else UpdateParticleText(particle, attribute.ToString_AttributeValue());
                    data.isVisible = true;
                }
                else
                {
                    // Attribute not available
                    data.isVisible = false;
                }
                break;
            case TextType.Chirality:
                break;
            case TextType.CompassDir:
                break;
            case TextType.Text:
                UpdateParticleText(particle, display_identifier);
                data.isVisible = true;
                break;
            default:
                break;
        }

        particleTextUIData[particle] = data;
    }

    /// <summary>
    /// Called by every particle after their regular movement updates.
    /// </summary>
    /// <param name="particle"></param>
    public void ParticleUpdate(IParticleState particle, Vector3 curPos)
    {
        ParticleTextUIData data = particleTextUIData[particle];
        if(data.pos.x != curPos.x || data.pos.y != curPos.y)
        {
            UpdateParticleTextPosition(particle, curPos);
        }
    }

    public GameObject AddParticleTextUI(IParticleState particle, Vector2 particlePosition, bool isVisible = true)
    {
        GameObject go = PoolCreate_particleTextUI(particle, isVisible);
        UpdateParticleTextPosition(particle, particlePosition);
        DisplayTextForParticle(particle);
        return go;
    }

    public bool RemoveParticleTextUI(IParticleState particle)
    {
        if(particleTextUIData.ContainsKey(particle) == false)
        {
            Log.Error("WorldSpaceUIHandler: RemoveParticleTextUI: Particle text UI cannot be found in list!");
            return false;
        }
        // Pool Release
        PoolRealease_particleTextUI(particle);
        return true;
    }

    private void UpdateParticleText(IParticleState particle, string text)
    {
        ParticleTextUIData data = particleTextUIData[particle];
        // Set Color
        Color color = color_particleTextBackgroundDefault;
        data.go.GetComponent<Image>().color = color;
        // Set Text
        data.go.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    private void UpdateParticleText(IParticleState particle, bool isTrue)
    {
        ParticleTextUIData data = particleTextUIData[particle];
        // Set Color
        Color color;
        if (isTrue) color = color_particleTextBackgroundTrue;
        else color = color_particleTextBackgroundFalse;
        data.go.GetComponent<Image>().color = color;
        // Set Text
        data.go.GetComponentInChildren<TextMeshProUGUI>().text = isTrue ? "True" : "False";
    }

    private void UpdateParticleTextPosition(IParticleState particle, Vector2 particlePosition)
    {
        ParticleTextUIData data = particleTextUIData[particle];
        data.go.transform.position = new Vector3(particlePosition.x, particlePosition.y, 0f); //RenderSystem.zLayer_worldSpaceUI); // this somehow prevents automatic batching
        data.pos = new Vector2(particlePosition.x, particlePosition.y);
    }

    /// <summary>
    /// Shows the world space UI (all elements that are flagged as visible). This is the default value.
    /// Call Hide() to hide it.
    /// </summary>
    public void ShowVisible()
    {
        foreach (var item in particleTextUIData.Values)
        {
            if(item.isVisible) item.go.SetActive(true);
        }
        display_isVisible = true;
        button_hideOverlay.interactable = true;
    }

    /// <summary>
    /// Hides the world space UI.
    /// Call Show() to show it again.
    /// </summary>
    public void HideAll()
    {
        foreach (var item in particleTextUIData.Values)
        {
            item.go.SetActive(false);
        }
        display_isVisible = false;
        button_hideOverlay.interactable = false;
    }






    // Pooling ===================

    public GameObject PoolCreate_particleTextUI(IParticleState particle, bool isVisible)
    {
        GameObject go;
        if (pool_particleTextUI.Count > 0) go = pool_particleTextUI.Pop();
        else go = Instantiate<GameObject>(UIDatabase.prefab_worldSpace_particleTextUI, new Vector3(0f, 0f, 0f), Quaternion.identity, go_worldSpaceUI.transform);
        go.SetActive(display_isVisible && isVisible);
        particleTextUIData.Add(particle, new ParticleTextUIData(go, Vector2.zero, isVisible));
        return go;
    }

    public void PoolRealease_particleTextUI(IParticleState particle)
    {
        // Hide
        ParticleTextUIData data = particleTextUIData[particle];
        data.isVisible = false;
        particleTextUIData[particle] = data;
        data.go.SetActive(false);
        // Remove Link
        particleTextUIData.Remove(particle);
        // Pool
        pool_particleTextUI.Push(data.go);
    }

}
