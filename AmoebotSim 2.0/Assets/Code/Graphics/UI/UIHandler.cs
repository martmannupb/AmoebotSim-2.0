using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using SFB;

public class UIHandler : MonoBehaviour
{

    // References
    [HideInInspector]
    public AmoebotSimulator sim;
    public ParticleUIHandler particleUI;
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
    public Button button_toolRemove;
    public Button button_toolMove;
    private Color toolColor_active;
    private Color toolColor_inactive;
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
    private Color overlayColor_active;
    private Color overlayColor_inactive;
    // Settings/Exit
    public Button button_settings;
    public Button button_exit;

    // State
    public UITool activeTool = UITool.Standard;

    public enum UITool
    {
        Standard, Add, Remove, Move
    }


    private void Start()
    {
        InitUI();
        NotifyPlayPause(sim.running);
    }

    public void RegisterSim(AmoebotSimulator sim)
    {
        this.sim = sim;
    }

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
    }
        

    public void Update()
    {
        if (sim == null) return;

        UpdateUI(sim.running);
        particleUI.UpdateUI();

        ProcessInputs();
    }

    private float timestamp_hidden;

    private void ProcessInputs()
    {
        // Process Inputs
        if(Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                // Hide UI
                HideUI();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                // Center Camera
                Button_CameraCenterPressed();
            }
            if(Input.GetKeyDown(KeyCode.V))
            {
                // Screenshot
                Button_ScreenshotPressed();
            }
            if(Input.GetKeyDown(KeyCode.S))
            {
                // Save
                Button_SavePressed();
            }
            if(Input.GetKeyDown(KeyCode.O))
            {
                // Open
                Button_OpenPressed();
            }
            if(Input.GetKeyDown(KeyCode.Q))
            {
                // Quit
                Botton_ExitPressed();
            }
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            // Show UI
            ShowUI();
        }
    }

    private void UpdateUI(bool running)
    {
        UpdateUI(running, false);
    }

    private void UpdateUI(bool running, bool forceRoundSliderUpdate)
    {
        // UI State
        if(initializationUI.IsOpen())
        {
            // Init Mode

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
            // Circuit View Type
            if (sim.renderSystem.IsCircuitViewActive()) button_circuitViewType.gameObject.GetComponent<Image>().color = overlayColor_active;
            else button_circuitViewType.gameObject.GetComponent<Image>().color = overlayColor_inactive;
        }
    }

    public void NotifyPlayPause(bool running)
    {
        image_playPauseButton.sprite = running ? sprite_pause : sprite_play;
        UpdateUI(running, true);
    }

    public void ShowUI()
    {
        ui.SetActive(true);
    }
    
    public void HideUI()
    {
        ui.SetActive(false);
    }






    // Callback Methods =========================

    public void Button_StepBackPressed()
    {
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

    public void Button_StepForwardPressed()
    {
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

    public void SliderSpeed_onValueChanged()
    {
        // Update Sim Speed
        int val = (int)slider_speed.value;
        float speed = slider_speed_values[val];
        sim.SetSimSpeed(speed);

        // Update UI
        text_speed.text = "Round Speed: " + (speed == 0f ? "max" : (speed + "s"));
    }

    public void SliderRound_onValueChanged()
    {
        if(toggle_alwaysUpdateWhenRoundSliderIsChanged.isOn && sim.running == false)
        {
            JumpToRound((int)slider_round.value);
        }
        UpdateUI(sim.running);
    }

    public void SliderRound_onDragEnd()
    {
        if (toggle_alwaysUpdateWhenRoundSliderIsChanged.isOn == false && sim.running == false)
        {
            JumpToRound((int)slider_round.value);
        }
        UpdateUI(sim.running);
    }
    
    public void Botton_CutPressed()
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
        if(sim.running)
        {
            // Error
            Log.Error("Cannot cut round: System is still running.");
            return;
        }

        sim.system.CutOffAtMarker();
        UpdateUI(sim.running, true);
    }

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

    public void Button_OpenPressed()
    {
        if(initializationUI.IsOpen() == false)
        {
            bool isSimRunning = sim.running;
            sim.PauseSim();
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Load Algorithm State", "", "amalgo", false);
            if (paths.Length != 0)
            {
                if (!sim.running)
                {
                    SimulationStateSaveData data = SaveStateUtility.Load(paths[0]);
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
    }

    public void Button_SavePressed()
    {
        if(initializationUI.IsOpen() == false)
        {
            // Initialization Handler closed
            // Save File
            string path = StandaloneFileBrowser.SaveFilePanel("Save Algorithm State", "", "algorithm", "amalgo");
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
    }

    public void Button_ScreenshotPressed()
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Save Screenshot", "", "AmoebotScreenshot", "png");
        if(path.Equals("") == false)
        {
            ScreenCapture.CaptureScreenshot(path);
        }
    }

    public void Button_ToolStandardPressed()
    {
        activeTool = UITool.Standard;
        UpdateTools();
    }

    public void Button_ToolAddPressed()
    {
        activeTool = UITool.Add;
        UpdateTools();
    }

    public void Button_ToolRemovePressed()
    {
        activeTool = UITool.Remove;
        UpdateTools();
    }

    public void Button_ToolMovePressed()
    {
        activeTool = UITool.Move;
        UpdateTools();
    }

    public void Button_ToggleViewPressed()
    {
        sim.renderSystem.ToggleViewType();
    }

    public void Button_ToggleCircuitViewPressed()
    {
        sim.renderSystem.ToggleCircuits();
    }

    private void UpdateTools()
    {
        // Reset Colors
        SetButtonColor(button_toolStandard, toolColor_inactive);
        SetButtonColor(button_toolAdd, toolColor_inactive);
        SetButtonColor(button_toolRemove, toolColor_inactive);
        SetButtonColor(button_toolMove, toolColor_inactive);
        // Set Active Tool Color
        switch (activeTool)
        {
            case UITool.Standard:
                SetButtonColor(button_toolStandard, toolColor_active);
                break;
            case UITool.Add:
                SetButtonColor(button_toolAdd, toolColor_active);
                break;
            case UITool.Remove:
                SetButtonColor(button_toolRemove, toolColor_active);
                break;
            case UITool.Move:
                SetButtonColor(button_toolMove, toolColor_active);
                break;
            default:
                break;
        }
    }

    private void SetButtonColor(Button button, Color color)
    {
        button.gameObject.GetComponent<Image>().color = color;
    }

    public void Button_CameraCenterPressed()
    {
        Vector2 pos;
        if (sim.system.particles.Count <= 1000) pos = sim.system.BBoxCenterPosition();
        else pos = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(sim.system.SeedPosition());
        Camera.main.transform.position = new Vector3(pos.x, pos.y, Camera.main.transform.position.z);
    }

    public void Botton_ExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void HideTopRightButtons()
    {
        button_settings.interactable = false;
        button_exit.interactable = false;
    }

    public void ShowTopRightButtons()
    {
        button_settings.interactable = true;
        button_exit.interactable = true;
    }

    public TMP_InputField temporaryBox;
    public void TemporaryButton_ResetAlgorithm(int algoID)
    {
        if (sim.running) sim.TogglePlayPause();

        particleUI.Close();
        if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.HideAll();
        sim.system.Reset();
        switch (algoID)
        {
            case 0:
                sim.system.InitializeBoundaryTest(100, 0.05f);
                break;
            case 1:
                sim.system.InitializeLineFormation(50, 0.4f);
                break;
            case 2:
                sim.system.InitializeLeaderElection(50, 0.35f);
                break;
            case 3:
                sim.system.InitializeChiralityCompass(50, 0.2f);
                break;
            case 4:
                sim.system.InitializeExpandedTest(10);
                break;
            case 5:
                sim.system.InitializeJMTest(int.Parse(temporaryBox.text));
                break;
            default:
                break;
        }
        UpdateUI(sim.running, true);
        sim.RoundChanged();
    }


    public void TemporarySaveBtn()
    {
        if (!sim.running)
        {
            SimulationStateSaveData data = sim.system.GenerateSaveData();
            SaveStateUtility.Save(data);
        }
    }

    public void TemporaryLoadBtn()
    {
        if (!sim.running)
        {
            SimulationStateSaveData data = SaveStateUtility.Load();
            if (data != null)
            {
                sim.system.Reset();
                sim.system.InitializeFromSaveState(data);
                UpdateUI(sim.running, true);
            }
        }
    }
}
