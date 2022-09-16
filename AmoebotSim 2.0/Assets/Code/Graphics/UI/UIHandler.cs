using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{

    // References
    [HideInInspector]
    public AmoebotSimulator sim;
    public ParticleUIHandler particleUI;
    public SettingsUIHandler settingsUI;
    public InitializationUIHandler initializationUI;

    // UI Objects =====
    // Play/Pause
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
    // AntiAliasing
    public Image image_AAButton;
    public Sprite sprite_aa0;
    public Sprite sprite_aa2;
    public Sprite sprite_aa4;
    public Sprite sprite_aa8;
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
        // Init Visuals
        UpdateAAButton();
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
    }

    public void Update()
    {
        if (sim == null) return;

        UpdateUI(sim.running);
        particleUI.UpdateUI();
    }

    private void UpdateUI(bool running)
    {
        UpdateUI(running, false);
    }

    private void UpdateUI(bool running, bool forceRoundSliderUpdate)
    {
        // Get Round Counter
        int curRound = sim.system.CurrentRound;
        int minRound = sim.system.EarliestRound;
        int maxRound = sim.system.LatestRound;
        int uiRound = (int)slider_round.value;

        // Play/Pause/Step
        button_stepBack.interactable = uiRound > minRound && running == false;
        button_stepForward.interactable = uiRound < maxRound && running == false;
        // Round Slider
        if (slider_round != null)
        {
            if(running || forceRoundSliderUpdate)
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
        if(text_round != null)
        {
            text_round.text = "Round: " + slider_round.value + " (of " + sim.system.LatestRound + ")";
        }
        // JumpCut Button
        button_jumpCut.interactable = uiRound < maxRound && running == false;
    }

    public void NotifyPlayPause(bool running)
    {
        image_playPauseButton.sprite = running ? sprite_pause : sprite_play;
        UpdateUI(running, true);
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

    private void UpdateAAButton()
    {
        int currentAA = sim.renderSystem.GetAntiAliasing();
        switch (currentAA)
        {
            case 0:
                image_AAButton.sprite = sprite_aa0;
                break;
            case 2:
                image_AAButton.sprite = sprite_aa2;
                break;
            case 4:
                image_AAButton.sprite = sprite_aa4;
                break;
            case 8:
                image_AAButton.sprite = sprite_aa8;
                break;
            default:
                break;
        }
    }

    public void ToggleAA()
    {
        sim.renderSystem.ToggleAntiAliasing();
        UpdateAAButton();
    }

    public void SetAA(int value)
    {
        sim.renderSystem.SetAntiAliasing(value);
        UpdateAAButton();
    }

    public void Botton_ExitPressed()
    {
        Application.Quit();
    }

    public void HideTopRightButtons()
    {
        button_settings.gameObject.SetActive(false);
        button_exit.gameObject.SetActive(false);
    }

    public void ShowTopRightButtons()
    {
        button_settings.gameObject.SetActive(true);
        button_exit.gameObject.SetActive(true);
    }

    public void TemporaryButton_ResetAlgorithm(int algoID)
    {
        if (sim.running) sim.TogglePlayPause();

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
            default:
                break;
        }
        UpdateUI(sim.running, true);
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
