using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{

    // References
    private AmoebotSimulator sim;

    // UI Objects
    public TextMeshProUGUI text_round;
    public Slider slider_round;
    public Toggle toggle_alwaysUpdateWhenRoundSliderIsChanged;


    private void Start()
    {
        // Init Listeners
        slider_round.onValueChanged.AddListener(delegate { SliderRound_onValueChanged(); });
    }

    public void RegisterSim(AmoebotSimulator sim)
    {
        this.sim = sim;
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
