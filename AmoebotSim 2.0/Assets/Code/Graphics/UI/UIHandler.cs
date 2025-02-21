// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using AS2.Visuals;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{

    /// <summary>
    /// Controls the general UI (like top and bottom bars). Also references the other main panels of the UI.
    /// </summary>
    public class UIHandler : MonoBehaviour
    {

        // References
        [HideInInspector]
        public AmoebotSimulator sim;
        public ParticleUIHandler particleUI;
        public ObjectUIHandler objectUI;
        public SettingsUIHandler settingsUI;
        public InitializationUIHandler initializationUI;

        // UI Objects =====
        public GameObject ui;
        // Play/Pause
        public Button button_playPause;
        public Image image_playPauseButton;
        public Sprite sprite_play;
        public Sprite sprite_pause;
        public Button button_stepBack;
        public Button button_stepForward;
        // Speed
        public TextMeshProUGUI text_speed;
        public Slider slider_speed;
        private float[] slider_speed_values = new float[] { 4f, 2f, 1f, 0.5f, 0.2f, 0.1f, 0.05f, 0f };
        // Round
        public TextMeshProUGUI text_round;
        public Slider slider_round;
        public Toggle toggle_alwaysUpdateWhenRoundSliderIsChanged;
        public Button button_jumpCut;
        // Tool Panel
        public Button button_toolStandard;
        public Button button_toolAdd;
        public Button button_toolAddObject;
        public Button button_toolRemove;
        public Button button_toolMove;
        public Button button_toolPSetMove;
        public TMP_Dropdown dropdown_chirality;
        public TMP_Dropdown dropdown_compass;
        private Color toolColor_active;
        private Color toolColor_inactive;
        private List<GameObject> tools_initMode = new List<GameObject>();
        private List<GameObject> tools_playMode = new List<GameObject>();
        // Overlay Panel
        public Button button_viewType;
        public Image image_viewType;
        public Sprite sprite_viewTypeCircular;
        public Sprite sprite_viewTypeHexagonal;
        public Sprite sprite_viewTypeHexagonalCirc;
        public Button button_circuitViewType;
        public Image image_circuitViewType;
        public Sprite sprite_circuitViewTypeCircuitsEnabled;
        public Sprite sprite_circuitViewTypeCircuitsDisabled;
        public Button button_collisionCheck;
        public Button button_pSetPositioning;
        public Image image_pSetPositioning;
        public Sprite sprite_pSetPositioning_def;
        public Sprite sprite_pSetPositioning_auto;
        public Sprite sprite_pSetPositioning_auto2D;
        public Sprite sprite_pSetPositioning_line;
        public Button button_bondsActive;
        public Button button_backgroundGridActive;
        private Color overlayColor_active;
        private Color overlayColor_inactive;
        // Settings/Exit
        public Button button_settings;
        public Button button_exit;

        /// <summary>
        /// Margin around the particle system when framing the
        /// system in the viewport.
        /// </summary>
        public float frameMargin = 1.5f;
        /// <summary>
        /// Fraction of the viewport height taken up by the top
        /// and bottom bar, used for framing the system in view.
        /// </summary>
        public float topAndBottomBarFraction = 0.15f;


        // State
        public UITool activeTool = UITool.Standard;

        /// <summary>
        /// The set of tools selectable in the top bar.
        /// </summary>
        public enum UITool
        {
            Standard, Add, Remove, Move, PSetMove, AddObject
        }


        private void Start()
        {
            InitUI();
            NotifyPlayPause(sim.running);
        }

        /// <summary>
        /// Registers the simulator at this object.
        /// </summary>
        /// <param name="sim">The simulator instance.</param>
        public void RegisterSim(AmoebotSimulator sim)
        {
            this.sim = sim;
        }

        /// <summary>
        /// Initializes the part of the visual interface that is controlled by this class.
        /// </summary>
        public void InitUI()
        {
            // Init Sim Speed Slider
            slider_speed.wholeNumbers = true;
            slider_speed.minValue = 0f;
            slider_speed.maxValue = slider_speed_values.Length - 1;
            slider_speed.value = 2f;
            SliderSpeed_onValueChanged();
            // Init Listeners
            slider_round.onValueChanged.AddListener(delegate { SliderRound_onValueChanged(); });
            slider_speed.onValueChanged.AddListener(delegate { SliderSpeed_onValueChanged(); });
            // Init Colors
            if (button_toolStandard != null) toolColor_active = button_toolStandard.gameObject.GetComponent<Image>().color;
            if (button_toolAdd != null) toolColor_inactive = button_toolAdd.gameObject.GetComponent<Image>().color;
            if (button_viewType != null) overlayColor_active = button_circuitViewType.gameObject.GetComponent<Image>().color;
            overlayColor_inactive = toolColor_inactive;
            // Init Tools
            tools_initMode.Add(button_toolStandard.gameObject);
            tools_initMode.Add(button_toolAdd.gameObject);
            tools_initMode.Add(button_toolAddObject.gameObject);
            tools_initMode.Add(button_toolRemove.gameObject);
            tools_initMode.Add(button_toolMove.gameObject);
            tools_initMode.Add(dropdown_chirality.gameObject.transform.parent.gameObject);
            tools_playMode.Add(button_toolStandard.gameObject);
            tools_playMode.Add(button_toolPSetMove.gameObject);
            // Init button tooltips
            UpdateTooltip(button_pSetPositioning.gameObject, GetPSetViewModeTooltip());
            UpdateTooltip(button_viewType.gameObject, GetViewModeTooltip());
        }

        /// <summary>
        /// Update loop of the UI Handler. Calls UpdateUI and updates the particle and object panel.
        /// Also processes the inputs for hotkeys.
        /// </summary>
        public void Update()
        {
            if (sim == null) return;

            UpdateUI(sim.running);
            particleUI.UpdateUI();
            objectUI.UpdateUI();

            ProcessInputs();
        }

        /// <summary>
        /// Processes all the hotkeys that are included in the simulator.
        /// Note: You can change the hotkeys if you want.
        /// </summary>
        private void ProcessInputs()
        {
            // Process Inputs ===============
            // Default
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Pause/Play
                sim.TogglePlayPause();
            }
            if(Input.GetKeyDown(KeyCode.PageUp))
            {
                // Forward
                Button_StepForwardPressed();
            }
            if(Input.GetKeyDown(KeyCode.PageDown))
            {
                // Back
                Button_StepBackPressed();
            }
            // Shift + ...
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    // Forward
                    Button_StepForwardPressed();
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    // Backward
                    Button_StepBackPressed();
                }
                if (Input.GetKeyDown(KeyCode.C))
                {
                    // Center Camera
                    Button_CameraCenterPressed();
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    // Frame system
                    Button_FrameSystemPressed();
                }
                if (Input.GetKeyDown(KeyCode.H))
                {
                    // Toggle UI visibility
                    ToggleUI();
                }
                if (Input.GetKeyDown(KeyCode.V))
                {
                    // Screenshot
                    Button_ScreenshotPressed();
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    // Save
                    Button_SavePressed();
                }
                if (Input.GetKeyDown(KeyCode.O))
                {
                    // Open
                    Button_OpenPressed();
                }
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    // Quit
                    Button_ExitPressed();
                }
            }
        }

        /// <summary>
        /// Updates the UI without a forced round slider update.
        /// </summary>
        /// <param name="running">Indicates whether or not the simulation
        /// is currently running.</param>
        private void UpdateUI(bool running)
        {
            UpdateUI(running, false);
        }

        /// <summary>
        /// Updates the UI (things like enable/disable buttons, set sliders, update rounds, set button colors, etc.).
        /// </summary>
        /// <param name="running">Indicates whether or not the simulation
        /// is currently running.</param>
        /// <param name="forceRoundSliderUpdate">Indicates whether the round slider must be
        /// updated for a reason other than the simulation running (like pressing the
        /// step forward or step back button).</param>
        public void UpdateUI(bool running, bool forceRoundSliderUpdate)
        {
            // UI State
            if (initializationUI.IsOpen())
            {
                // Init Mode
                // Update Top Panel Buttons
                foreach (var item in tools_playMode) item.SetActive(false);
                foreach (var item in tools_initMode) item.SetActive(true);
                // Disable Bottom Panel Buttons
                button_stepBack.interactable = false;
                button_stepForward.interactable = false;
                button_playPause.interactable = false;
                slider_round.interactable = false;
                button_jumpCut.interactable = false;
                // Edit Texts
                string textRound = "Init Mode";
                if (text_round.text.Equals(textRound) == false) text_round.text = textRound;
            }
            else
            {
                // Sim Mode
                // Get Round Counter
                int curRound = sim.system.CurrentRound;
                int minRound = sim.system.EarliestRound;
                int maxRound = sim.system.LatestRound;
                int uiRound = (int)slider_round.value;

                // Update Top Panel Buttons
                foreach (var item in tools_initMode) item.SetActive(false);
                foreach (var item in tools_playMode) item.SetActive(true);
                // Play/Pause/Step
                button_stepBack.interactable = uiRound > minRound && running == false;
                button_stepForward.interactable = uiRound < maxRound && running == false;
                button_playPause.interactable = true;
                // Round Slider
                slider_round.interactable = true;
                if (slider_round != null)
                {
                    if (running || forceRoundSliderUpdate)
                    {
                        // Sim running
                        // Update, do not allow editing of the slider
                        slider_round.enabled = false;
                        slider_round.minValue = 0;
                        slider_round.maxValue = maxRound;
                        slider_round.value = curRound;
                    }
                    else
                    {
                        // Sim paused
                        // Allow editing the slider and jump to the given round
                        slider_round.enabled = true;
                    }
                }
                // Round Text
                if (text_round != null)
                {
                    string text = "Round: " + slider_round.value + " (of " + sim.system.LatestRound + ")";
                    if (text_round.text.Equals(text) == false) text_round.text = text;
                }
                // JumpCut Button
                button_jumpCut.interactable = uiRound < maxRound && running == false;
            }

            // View Button Images
            // View Type
            switch (sim.renderSystem.GetCurrentViewType())
            {
                case ViewType.Hexagonal:
                    if (image_viewType.sprite != sprite_viewTypeHexagonal) image_viewType.sprite = sprite_viewTypeHexagonal;
                    break;
                case ViewType.HexagonalCirc:
                    if (image_viewType.sprite != sprite_viewTypeHexagonalCirc) image_viewType.sprite = sprite_viewTypeHexagonalCirc;
                    break;
                case ViewType.Circular:
                    if (image_viewType.sprite != sprite_viewTypeCircular) image_viewType.sprite = sprite_viewTypeCircular;
                    break;
                default:
                    break;
            }
            // PSet Button Images
            // PSet Type
            switch (sim.renderSystem.GetPSetViewType())
            {
                case PartitionSetViewType.Line:
                    if (image_pSetPositioning.sprite != sprite_pSetPositioning_line) image_pSetPositioning.sprite = sprite_pSetPositioning_line;
                    break;
                case PartitionSetViewType.Auto:
                    if (image_pSetPositioning.sprite != sprite_pSetPositioning_auto) image_pSetPositioning.sprite = sprite_pSetPositioning_auto;
                    break;
                case PartitionSetViewType.Auto_2DCircle:
                    if (image_pSetPositioning.sprite != sprite_pSetPositioning_auto2D) image_pSetPositioning.sprite = sprite_pSetPositioning_auto2D;
                    break;
                case PartitionSetViewType.CodeOverride:
                    if (image_pSetPositioning.sprite != sprite_pSetPositioning_def) image_pSetPositioning.sprite = sprite_pSetPositioning_def;
                    break;
                default:
                    break;
            }
            // Circuit View Type
            if (sim.renderSystem.IsCircuitViewActive()) button_circuitViewType.gameObject.GetComponent<Image>().color = overlayColor_active;
            else button_circuitViewType.gameObject.GetComponent<Image>().color = overlayColor_inactive;
            // Collision Check
            if (sim.system.CollisionCheckEnabled) button_collisionCheck.gameObject.GetComponent<Image>().color = overlayColor_active;
            else button_collisionCheck.gameObject.GetComponent<Image>().color = overlayColor_inactive;
            // Bonds Active
            if (sim.renderSystem.AreBondsActive()) button_bondsActive.gameObject.GetComponent<Image>().color = overlayColor_active;
            else button_bondsActive.gameObject.GetComponent<Image>().color = overlayColor_inactive;
            // Background Grid Active
            if (WorldSpaceBackgroundUIHandler.instance != null)
            {
                if (WorldSpaceBackgroundUIHandler.instance.IsActive()) button_backgroundGridActive.gameObject.GetComponent<Image>().color = overlayColor_active;
                else button_backgroundGridActive.gameObject.GetComponent<Image>().color = overlayColor_inactive;
            }
        }

        /// <summary>
        /// Notifies the UI handler that the play/pause has been toggled.
        /// </summary>
        /// <param name="running">The currently active running state. True if
        /// the simulation is running.</param>
        public void NotifyPlayPause(bool running)
        {
            image_playPauseButton.sprite = running ? sprite_pause : sprite_play;
            UpdateUI(running, true);
        }

        /// <summary>
        /// Shows the UI.
        /// </summary>
        public void ShowUI()
        {
            ui.SetActive(true);
        }

        /// <summary>
        /// Hides the UI. Use this for things like screenshots.
        /// </summary>
        public void HideUI()
        {
            ui.SetActive(false);
        }

        /// <summary>
        /// Toggles the UI visibility.
        /// </summary>
        public void ToggleUI()
        {
            if (ui.activeSelf)
                HideUI();
            else
                ShowUI();
        }

        /// <summary>
        /// Returns the color used to display that the setting is active.
        /// </summary>
        /// <returns>The active overlay color.</returns>
        public Color GetButtonColor_Active()
        {
            return overlayColor_active;
        }

        /// <summary>
        /// Returns the color used to display that the setting is inactive.
        /// </summary>
        /// <returns>The inactive overlay color.</returns>
        public Color GetButtonColor_Inactive()
        {
            return overlayColor_inactive;
        }

        /// <summary>
        /// Gets the chirality value from the corresponding dropdown UI element.
        /// </summary>
        /// <returns>The selected chirality value. Can be Random, Clockwise or
        /// Counterclockwise.</returns>
        public Initialization.Chirality GetDropdownValue_Chirality()
        {
            TMP_Dropdown dropdown = dropdown_chirality;
            string value = dropdown.options[dropdown.value].text;
            switch (value)
            {
                case "R":
                    return Initialization.Chirality.Random;
                case "C":
                    return Initialization.Chirality.Clockwise;
                case "CC":
                    return Initialization.Chirality.CounterClockwise;
                default:
                    Log.Error("GetDropdownValue_Chirality: Value not found!");
                    return Initialization.Chirality.Random;
            }
        }

        /// <summary>
        /// Gets the compass dir value from the corresponding dropdown UI element.
        /// </summary>
        /// <returns>The selected compass direction. Can be any cardinal direction
        /// or Random.</returns>
        public Initialization.Compass GetDropdownValue_Compass()
        {
            TMP_Dropdown dropdown = dropdown_compass;
            string value = dropdown.options[dropdown.value].text;
            if (value.Equals("R")) value = "Random";
            object res;
            if (Enum.TryParse(typeof(Initialization.Compass), value, out res)) return (Initialization.Compass)res;
            else Log.Error("GetDropdownValue_Compass: Value not found!");
            return Initialization.Compass.Random;
        }



        // Callback Methods =========================

        /// <summary>
        /// Called when the step back button has been pressed.
        /// Goes back a round if the sim is not running.
        /// </summary>
        public void Button_StepBackPressed()
        {
            if (button_stepBack.interactable == false) return;

            // Check if valid
            if (sim.running)
            {
                Log.Error("Could not step back: Sim is running!");
                return;
            }

            sim.system.StepBack();
            sim.RoundChanged();
            UpdateUI(sim.running, true);
        }

        /// <summary>
        /// Called when the step forward button has been pressed.
        /// Goes forward a round if the sim is not running.
        /// </summary>
        public void Button_StepForwardPressed()
        {
            if (button_stepForward.interactable == false) return;

            // Check if valid
            if (sim.running)
            {
                Log.Error("Could not step forward: Sim is running!");
                return;
            }

            sim.system.StepForward();
            sim.RoundChanged();
            UpdateUI(sim.running, true);
        }

        /// <summary>
        /// Called when the speed slider value has been changed.
        /// Changes the speed of the simulation.
        /// </summary>
        public void SliderSpeed_onValueChanged()
        {
            // Update Sim Speed
            int val = (int)slider_speed.value;
            float speed = slider_speed_values[val];
            sim.SetSimSpeed(speed);

            // Update UI
            text_speed.text = "Round Speed: " + (speed == 0f ? "max" : (speed + "s"));
        }

        /// <summary>
        /// Called when the round slider value has been changed. Alternative to SliderRound_onDragEnd().
        /// Changes the round of the simulation.
        /// </summary>
        public void SliderRound_onValueChanged()
        {
            if (toggle_alwaysUpdateWhenRoundSliderIsChanged.isOn && sim.running == false)
            {
                JumpToRound((int)slider_round.value);
            }
            UpdateUI(sim.running);
        }

        /// <summary>
        /// Called when the round slider drag has been finished. Alternative to SliderRound_onValueChanged().
        /// Changes the round of the simulation.
        /// </summary>
        public void SliderRound_onDragEnd()
        {
            if (toggle_alwaysUpdateWhenRoundSliderIsChanged.isOn == false && sim.running == false)
            {
                JumpToRound((int)slider_round.value);
            }
            UpdateUI(sim.running);
        }

        /// <summary>
        /// Called when the cut rounds button has been pressed.
        /// Deletes the history of later rounds than the currently active round.
        /// </summary>
        public void Button_CutPressed()
        {
            // Check if valid
            int uiRound = (int)slider_round.value;
            int maxRound = sim.system.LatestRound;
            if (uiRound >= maxRound)
            {
                // Error
                Log.Error("Cannot cut round: Current Round is not behind latest round!");
                return;
            }
            if (sim.running)
            {
                // Error
                Log.Error("Cannot cut round: System is still running.");
                return;
            }

            sim.system.CutOffAtMarker();
            UpdateUI(sim.running, true);
        }

        /// <summary>
        /// Jumps to a certain round. The UI is updated afterwards.
        /// </summary>
        /// <param name="newRound">The round to jump to. Must be in the
        /// range of valid rounds.</param>
        private void JumpToRound(int newRound)
        {
            // Null Check
            if (newRound < sim.system.EarliestRound || newRound > sim.system.LatestRound)
            {
                Log.Error("SliderRound_onValueChanged: Round is out of bounds!");
                return;
            }

            // Execute Jump
            int curRound = sim.system.CurrentRound;
            if (curRound != newRound)
            {
                // Round Change
                sim.system.SetMarkerToRound(newRound);
                sim.RoundChanged();
            }
        }

        /// <summary>
        /// Called when the open button has been pressed.
        /// Opens the interface to load a saved simulator configuration.
        /// </summary>
        public void Button_OpenPressed()
        {
            bool isSimRunning = sim.running;
            sim.PauseSim();
            string path = FileBrowser.LoadSimFile();
            if (!path.Equals(""))
            {
                if (!sim.running)
                {
                    // Close Init Mode (if necessary)
                    if (initializationUI.IsOpen()) initializationUI.ButtonPressed_Abort();
                    // Check if closed successfully
                    if (initializationUI.IsOpen())
                    {
                        Log.Error("UIHandler: Button_OpenPressed: Init Mode could not be closed.");
                        return;
                    }

                    SimulationStateSaveData data = SaveStateUtility.Load(path);
                    if (data != null)
                    {
                        sim.system.Reset();
                        sim.system.InitializeFromSaveState(data);
                        UpdateUI(sim.running, true);
                    }
                }
                else
                {
                    Log.Error("Please pause the sim before loading an algorithm state!");
                }
            }
            else
            {
                if (isSimRunning) sim.PlaySim();
            }
        }

        /// <summary>
        /// Called when the save button has been pressed.
        /// Saves the current simulator configuration.
        /// </summary>
        public void Button_SavePressed()
        {
            if (initializationUI.IsOpen() == false)
            {
                // Initialization Handler closed
                // Save File
                string path = FileBrowser.SaveSimFile();
                if (path.Equals("") == false)
                {
                    if (!sim.running)
                    {
                        SimulationStateSaveData data = sim.system.GenerateSaveData();
                        SaveStateUtility.Save(data, path);
                        Log.Entry("Algorithm state has been saved successfully!");
                    }
                    else
                    {
                        Log.Error("Please pause the sim before saving the algorithm state!");
                    }
                }
            }
            else
            {
                // Init Mode open
                Log.Entry("You need to execute an algorithm to save its state.\nGet out of init mode, mate!");
            }
        }

        /// <summary>
        /// Called when the screenshot button has been pressed.
        /// Makes a screenshot of the simulation and saves it to a file
        /// selected via a file browser.
        /// </summary>
        public void Button_ScreenshotPressed()
        {
            string path = FileBrowser.SavePNGFile("Save Screenshot", "AmoebotScreenshot");
            if (path.Equals("") == false)
            {
                ScreenCapture.CaptureScreenshot(path);
            }
        }

        /// <summary>
        /// Called when the save log button has been pressed.
        /// Saves the log to a file.
        /// </summary>
        public void Button_PrintLogToFilePressed()
        {
            string path = FileBrowser.SaveTextFile("Save Log to File", "amsim_log");
            if (path.Equals("") == false)
            {
                Log.SaveLogToFile(path);
                Log.Entry("Log saved to file " + path + ".");
            }
        }

        /// <summary>
        /// Selects the standard selection tool.
        /// </summary>
        public void Button_ToolStandardPressed()
        {
            activeTool = UITool.Standard;
            UpdateTools();
        }

        /// <summary>
        /// Selects the add tool.
        /// </summary>
        public void Button_ToolAddPressed()
        {
            activeTool = UITool.Add;
            UpdateTools();
        }

        /// <summary>
        /// Selects the add object tool.
        /// </summary>
        public void Button_ToolAddObjectPressed()
        {
            activeTool = UITool.AddObject;
            UpdateTools();
        }

        /// <summary>
        /// Selects the remove tool.
        /// </summary>
        public void Button_ToolRemovePressed()
        {
            activeTool = UITool.Remove;
            UpdateTools();
        }

        /// <summary>
        /// Selects the move tool.
        /// </summary>
        public void Button_ToolMovePressed()
        {
            activeTool = UITool.Move;
            UpdateTools();
        }

        /// <summary>
        /// Selects the partition set move tool.
        /// </summary>
        public void Button_ToolPSetMovePressed()
        {
            activeTool = UITool.PSetMove;
            UpdateTools();
        }

        /// <summary>
        /// Toggles the view type (hexagon, circle, graph, ...).
        /// </summary>
        public void Button_ToggleViewPressed()
        {
            sim.renderSystem.ToggleViewType();
            UpdateTooltip(button_viewType.gameObject, GetViewModeTooltip());
        }

        /// <summary>
        /// Toggles the circuit view on/off.
        /// </summary>
        public void Button_ToggleCircuitViewPressed()
        {
            sim.renderSystem.ToggleCircuits();
        }

        /// <summary>
        /// Toggles the collision check on/off.
        /// </summary>
        public void Button_ToggleCollisionCheck()
        {
            sim.system.SetCollisionCheck(!sim.system.CollisionCheckEnabled);
        }

        /// <summary>
        /// Toggles the positioning mode of the partition sets.
        /// </summary>
        public void Button_TogglePSetPositioningPressed()
        {
            sim.renderSystem.TogglePSetPositioning();
            UpdateTooltip(button_pSetPositioning.gameObject, GetPSetViewModeTooltip());
        }

        /// <summary>
        /// Toggles the bonds on/off.
        /// </summary>
        public void Button_ToggleBondsPressed()
        {
            sim.renderSystem.ToggleBonds();
        }

        /// <summary>
        /// Updates the color of the tool buttons. The active tool is highlighted.
        /// </summary>
        private void UpdateTools()
        {
            // Reset Colors
            SetButtonColor(button_toolStandard, toolColor_inactive);
            SetButtonColor(button_toolAdd, toolColor_inactive);
            SetButtonColor(button_toolAddObject, toolColor_inactive);
            SetButtonColor(button_toolRemove, toolColor_inactive);
            SetButtonColor(button_toolMove, toolColor_inactive);
            SetButtonColor(button_toolPSetMove, toolColor_inactive);
            // Set Active Tool Color
            switch (activeTool)
            {
                case UITool.Standard:
                    SetButtonColor(button_toolStandard, toolColor_active);
                    break;
                case UITool.Add:
                    SetButtonColor(button_toolAdd, toolColor_active);
                    break;
                case UITool.AddObject:
                    SetButtonColor(button_toolAddObject, toolColor_active);
                    break;
                case UITool.Remove:
                    SetButtonColor(button_toolRemove, toolColor_active);
                    break;
                case UITool.Move:
                    SetButtonColor(button_toolMove, toolColor_active);
                    break;
                case UITool.PSetMove:
                    SetButtonColor(button_toolPSetMove, toolColor_active);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Sets the background color of an arbitrary standard button.
        /// </summary>
        /// <param name="button">The button to change.</param>
        /// <param name="color">The color the button should have.</param>
        private void SetButtonColor(Button button, Color color)
        {
            button.gameObject.GetComponent<Image>().color = color;
        }

        /// <summary>
        /// Changes the camera position and zoom level such that the whole
        /// particle system is in frame.
        /// </summary>
        public void Button_FrameSystemPressed()
        {
            Vector4 bbox = sim.system.GetBoundingBox();
            Camera cam = Camera.main;

            // Center camera first
            Vector2 center = new Vector2(bbox.x, bbox.y);
            cam.transform.position = new Vector3(center.x, center.y, cam.transform.position.z);

            // Set zoom level to fit whole system into view
            // Orthographic camera size is half the height of the viewport
            float h = bbox.w / 2;                       // Minimum camera size required due to system height
            float w = (bbox.z / 2) / Camera.main.aspect; // Minimum size required due to system width
            // Stretch size to fit top and bottom bar
            h *= (1 + topAndBottomBarFraction);
            w *= (1 + topAndBottomBarFraction);
            MouseController.instance.SetOrthographicSize(Mathf.Max(h, w) + frameMargin);

            settingsUI.UpdateCameraData(cam.transform.position.x, cam.transform.position.y, cam.orthographicSize);
        }

        /// <summary>
        /// Centers the camera to the particles. Sets the (0,0) position if there are no particles.
        /// </summary>
        public void Button_CameraCenterPressed()
        {
            Vector4 bbox = sim.system.GetBoundingBox();
            Camera cam = Camera.main;
            cam.transform.position = new Vector3(bbox.x, bbox.y, Camera.main.transform.position.z);

            settingsUI.UpdateCameraData(cam.transform.position.x, cam.transform.position.y, cam.orthographicSize);
        }

        /// <summary>
        /// Exits the simulator.
        /// </summary>
        public void Button_ExitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Makes the top right buttons that should not be used
        /// during Init Mode not interactable.
        /// (Originally this method has hidden the button.)
        /// </summary>
        public void HideTopRightButtons()
        {
            button_settings.interactable = false;
            //button_exit.interactable = false;
        }

        /// <summary>
        /// Makes the top right buttons that should not be used
        /// during Init Mode interactable.
        /// (Originally this method has shown the buttons.)
        /// </summary>
        public void ShowTopRightButtons()
        {
            button_settings.interactable = true;
            //button_exit.interactable = true;
        }

        // Special button tooltip values

        /// <summary>
        /// Tries to update the tooltip message of the Tooltip
        /// component in the given GameObject.
        /// </summary>
        /// <param name="go">The GameObject that has a Tooltip.</param>
        /// <param name="message">The new tooltip message.</param>
        private void UpdateTooltip(GameObject go, string message)
        {
            Tooltip tt = go.GetComponent<Tooltip>();
            if (tt != null)
            {
                tt.ChangeMessage(message);
            }
        }

        /// <summary>
        /// Generates the tooltip matching the current partition set
        /// view type.
        /// </summary>
        /// <returns>The new tooltip message for the partition set
        /// view type button.</returns>
        private string GetPSetViewModeTooltip()
        {
            PartitionSetViewType vt = sim.renderSystem.GetPSetViewType();
            string s = "Toggle partition set view type: ";

            switch (vt)
            {
                case PartitionSetViewType.Auto:
                    s += "Automatic (circle)";
                    break;
                case PartitionSetViewType.Auto_2DCircle:
                    s += "Automatic (disk)";
                    break;
                case PartitionSetViewType.CodeOverride:
                    s += "Auto with manual override";
                    break;
                case PartitionSetViewType.Line:
                    s += "Line";
                    break;
            }

            return s;
        }

        /// <summary>
        /// Generates the tooltip matching the current view type.
        /// </summary>
        /// <returns>The new tooltip message for the partition set
        /// view type button.</returns>
        private string GetViewModeTooltip()
        {
            ViewType vt = sim.renderSystem.GetCurrentViewType();
            string s = "Toggle view mode: ";

            switch (vt)
            {
                case ViewType.Circular:
                    s += "Circular";
                    break;
                case ViewType.Hexagonal:
                    s += "Hexagonal";
                    break;
                case ViewType.HexagonalCirc:
                    s += "Hexagonal (round)";
                    break;
            }

            return s;
        }
    }
}