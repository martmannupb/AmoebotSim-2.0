// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


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
    /// Controls the object panel.
    /// </summary>
    public class ObjectUIHandler : MonoBehaviour
    {

        // Singleton
        public static ObjectUIHandler instance;

        // References
        private AmoebotSimulator sim;

        // UI References
        private TMPro.TextMeshProUGUI headerText;
        public GameObject go_objectPanel;
        private CanvasGroup canvasGroup_objectPanel;
        public GameObject go_contentParent;
        public GameObject go_attributeParent;
        public GameObject go_attributeRandomizationParent;
        public Button button_anchor;
        private Image image_anchor;

        // Colors
        public Color button_color_default;
        public Color button_color_active;

        // Data
        private IObjectInfo obj;
        private Dictionary<string, IParticleAttribute> attributeNameToIParticleAttribute = new Dictionary<string, IParticleAttribute>();
        private Dictionary<string, UISetting> settings = new Dictionary<string, UISetting>();
        private Dictionary<string, GameObject> settings_randomization = new Dictionary<string, GameObject>();
        private UISetting_Text settingID;
        private UISetting_Color settingColor;

        private readonly string attributeName_Identifier = "Identifier";
        private readonly string attributeName_Color = "Color";

        private void Start()
        {
            // Singleton
            instance = this;

            // References
            sim = AmoebotSimulator.instance;

            // Find elements
            canvasGroup_objectPanel = go_objectPanel.GetComponent<CanvasGroup>();
            for (int i = 0; i < go_attributeParent.transform.childCount; i++)
            {
                GameObject go = go_attributeParent.transform.GetChild(i).gameObject;
                if (go.name.Equals("Description"))
                {
                    headerText = go.GetComponent<TextMeshProUGUI>();
                }
            }
            image_anchor = button_anchor.gameObject.GetComponent<Image>();

            // Register Events
            EventDatabase.event_sim_startedStopped += SimState_RunningToggled;

            // Hide Panel
            Close();
            // Clear Panel
            ClearPanel();
        }

        /// <summary>
        /// Opens the object panel for the selected object.
        /// </summary>
        /// <param name="obj">The object that should be shown.</param>
        public void Open(IObjectInfo obj)
        {
            bool inInitMode = sim.uiHandler.initializationUI.IsOpen();
            ReinitObjectPanel(obj, inInitMode);
            canvasGroup_objectPanel.alpha = 1f;
            canvasGroup_objectPanel.interactable = true;
            canvasGroup_objectPanel.blocksRaycasts = true;

            // I don't know why this is necessary, but it somehow forces a size update for the content of the scroll view
            go_objectPanel.SetActive(false);
            go_objectPanel.SetActive(true);

            // Event
            EventDatabase.event_objectUI_objectPanelOpenClose?.Invoke(true);
        }

        /// <summary>
        /// Closes the object panel.
        /// </summary>
        public void Close()
        {
            canvasGroup_objectPanel.alpha = 0f;
            canvasGroup_objectPanel.interactable = false;
            canvasGroup_objectPanel.blocksRaycasts = false;
            ClearPanel();

            // Event
            EventDatabase.event_objectUI_objectPanelOpenClose?.Invoke(false);
        }

        /// <summary>
        /// Checks if the object panel is open.
        /// </summary>
        /// <returns><c>true</c> if and only if the panel is open, indicated
        /// by its alpha value being 1 (i.e. the panel is not transparent).</returns>
        public bool IsOpen()
        {
            return canvasGroup_objectPanel.alpha == 1f;
        }

        /// <summary>
        /// Returns the currently shown object in the panel.
        /// Before usage, please check if the panel is open and only use this method if this is the case.
        /// </summary>
        /// <returns>The object currently associated to the panel.</returns>
        public IObjectInfo GetShownObject()
        {
            return obj;
        }

        /// <summary>
        /// Called every frame. Used for dynamic updates of the visual interface.
        /// </summary>
        public void UpdateUI()
        {
            if (go_objectPanel.activeSelf)
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
        /// Internal method. Reopens the object panel with the specified object. Called by the Open(..) method.
        /// </summary>
        /// <param name="obj">The object for which the panel should be initialized.</param>
        /// <param name="initMode">Indicates whether or not the system is in Init Mode.</param>
        private void ReinitObjectPanel(IObjectInfo obj, bool initMode)
        {
            // Clear
            ClearPanel();

            // Reinit
            this.obj = obj;
            // Header
            RefreshHeader();

            AddAttributes_IDAndColor(obj, initMode);
        }

        /// <summary>
        /// Updates the values in the object panel when shown.
        /// Helpful when values have changed (e.g. after a round).
        /// </summary>
        public void RefreshObjectPanel()
        {
            if (obj != null)
            {
                RefreshHeader();
                RefreshAttributes_IDAndColor();
            }
        }

        /// <summary>
        /// Updates the header (position, size and anchor information).
        /// </summary>
        private void RefreshHeader()
        {
            // Text
            if (obj != null)
            {
                // Text
                headerText.text = "Position: " + obj.Position
                    + "\nSize: " + obj.Size;
                // Button
                if (obj.IsAnchor())
                {
                    image_anchor.color = button_color_active;
                    button_anchor.interactable = false;
                }
                else
                {
                    image_anchor.color = button_color_default;
                    button_anchor.interactable = true;
                }
                if (sim.system.IsInLatestRound() == false) button_anchor.interactable = false;
            }
        }

        /// <summary>
        /// Updates the ID and color settings.
        /// </summary>
        private void RefreshAttributes_IDAndColor()
        {
            if (!IsOpen()) return;

            settingID.UpdateValue(obj.Identifier.ToString());
            settingColor.UpdateValue(obj.Color);
        }

        /// <summary>
        /// Adds the ID and color attributes to the object panel.
        /// </summary>
        /// <param name="obj">The selected object</param>
        /// <param name="initMode">Indicates whether or not the system is
        /// in Init Mode.</param>
        private void AddAttributes_IDAndColor(IObjectInfo obj, bool initMode)
        {
            // ID
            settingID = new UISetting_Text(null, go_attributeParent.transform, attributeName_Identifier, obj.Identifier.ToString(), UISetting_Text.InputType.Int);
            settingID.GetGameObject().name = attributeName_Identifier;
            settingID.onValueChangedEvent += SettingChanged_Text;
            //settingID.backgroundButton_onButtonPressedEvent += AttributeClicked;
            //settingID.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(attributeName_Identifier, settingID);

            // Color
            settingColor = new UISetting_Color(null, go_attributeParent.transform, attributeName_Color, obj.Color, UISetting_Color.InputType.Int);
            settingColor.GetGameObject().name = attributeName_Color;
            settingColor.onValueChangedEvent += SettingChanged_Color;
            //settingColor.backgroundButton_onButtonPressedEvent += AttributeClicked;
            //settingColor.backgroundButton_onButtonPressedLongEvent += SettingHeldDown;
            settings.Add(attributeName_Color, settingColor);
            //GameObject go_randomization = Instantiate<GameObject>(UIDatabase.prefab_randomization_dices, Vector3.zero, Quaternion.identity, go_attributeRandomizationParent.transform);
            //settings_randomization.Add(particleAttribute.ToString_AttributeName(), go_randomization);
            //InitRandomizationGameObject(go_randomization, particleAttribute.ToString_AttributeName());
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
        /// Clears the object panel. Removes all attributes and texts
        /// after the delimiter, also clears all internally stored attributes.
        /// </summary>
        public void ClearPanel()
        {
            obj = null;
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
        /// Notifies the object panel that the system is running/not running.
        /// Locks or unlocks the attributes, so that changes during runtime are not possible.
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
        /// Notifies the object panel that the round has been changed, so it can refresh.
        /// </summary>
        public void SimState_RoundChanged()
        {
            // Refresh Attributes
            RefreshObjectPanel();
        }



        // Callbacks

        private void SettingChanged_Color(string name, string value)
        {
            if (!AttributeChangeValid()) return;

            if (sim.system.InInitializationState || sim.system.IsInLatestRound())
                obj.Color = settingColor.Color;
            else
                settingColor.UpdateValue(obj.Color);
        }

        /// <summary>
        /// Called when one attribute has been changed by the user.
        /// Forwards this to the particle system and refreshes the relevant UI.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="text">The new value of the attribute.</param>
        private void SettingChanged_Text(string name, string text)
        {
            if (AttributeChangeValid() == false) return;

            if (name.Equals(attributeName_Identifier))
            {
                if (sim.system.InInitializationState)
                {
                    if (int.TryParse(text, out int i))
                        obj.Identifier = i;
                }
                else
                {
                    settingID.UpdateValue(obj.Identifier.ToString());
                }
            }
        }

        /// <summary>
        /// Returns true if the attribute is allowed to be changed. The particle panel must be
        /// active and the system has to be in the latest round while being paused to do so.
        /// </summary>
        /// <returns></returns>
        private bool AttributeChangeValid()
        {
            return go_objectPanel.activeSelf && sim.system.IsInLatestRound() && sim.running == false;
        }

        /// <summary>
        /// Called when the anchor button has been pressed.
        /// Sets the current object as anchor.
        /// </summary>
        public void ButtonPressed_Anchor()
        {
            if (IsOpen() && sim.system.IsInLatestRound())
            {
                obj.MakeAnchor();
                RefreshObjectPanel();
            }
            else if (sim.system.IsInLatestRound() == false)
            {
                Log.Warning("You cannot set the anchor when you are not in the latest round!");
            }
        }
    }

} // namespace AS2.UI
