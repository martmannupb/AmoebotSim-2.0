using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{

    // References
    private AmoebotSimulator sim;

    // UI Objects =====
    // Speed
    public TextMeshProUGUI text_speed;
    public Slider slider_speed;
    private float[] slider_speed_values = new float[] { 4f, 2f, 1f, 0.5f, 0.2f, 0.1f, 0f };
    // Round
    public TextMeshProUGUI text_round;
    public Slider slider_round;
    public Toggle toggle_alwaysUpdateWhenRoundSliderIsChanged;


    private void Start()
    {
        InitUI();
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
        slider_speed.value = 3f;
        SliderSpeed_onValueChanged();
        // Init Listeners
        slider_round.onValueChanged.AddListener(delegate { SliderRound_onValueChanged(); });
        slider_speed.onValueChanged.AddListener(delegate { SliderSpeed_onValueChanged(); });
    }

    public void Update()
    {
        if (sim == null) return;

        UpdateUI(sim.running);
    }

    private void UpdateUI(bool running)
    {
        // 
        int curRound = sim.system.CurrentRound;
        int maxRound = sim.system.LatestRound;
        

        // Round Slider
        if(slider_round != null)
        {
            if(running)
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
    }

    public void NotifyPlayPause(bool running)
    {
        if(running == false)
        {
            // Update the UI one more time to get the current data
            UpdateUI(true);
        }
    }





    // Callback Methods =========================

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
    }

    public void SliderRound_onDragEnd()
    {
        if (toggle_alwaysUpdateWhenRoundSliderIsChanged.isOn == false && sim.running == false)
        {
            JumpToRound((int)slider_round.value);
        }
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
        }
    }

}
