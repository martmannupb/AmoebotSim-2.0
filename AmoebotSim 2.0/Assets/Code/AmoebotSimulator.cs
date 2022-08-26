using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AmoebotSimulator : MonoBehaviour
{

    // System Data
    public ParticleSystem system;
    public RenderSystem renderSystem;
    // System State
    public bool running = true;
    
    // UI
    public UIHandler uiHandler;

    // Old UI (will be deleted)
    public TextMeshProUGUI roundsText;
    public TextMeshProUGUI maxRoundText;
    public TMP_InputField roundInput;

    // Start is called before the first frame update
    void Start()
    {
        // Init Renderer + Particle System
        renderSystem = new RenderSystem();
        system = new ParticleSystem(this, renderSystem);
        // Set Sim Speed
        SetSimSpeed(0.005f);
        SetSimSpeed(0.5f);

        // Register UI
        if(uiHandler != null) uiHandler.RegisterSim(this);

        // Init Algorithm
        //system.InitializeExample(1, 1, 1f, -9, -5);
        //system.InitializeExample(50, 50, 0.3f, -9, -5);
        system.InitializeLineFormation(50, 0.4f);
        //system.InitializeLineFormation(25, 0.4f);
        //system.InitializeLeaderElection(50, 0.35f);
        //system.InitializeChiralityCompass(50, 0.2f);
        //system.InitializeBoundaryTest(100, 0.05f);
        //system.InitializeExpandedTest(10);
    }


    // Update is called once per frame
    void Update()
    {
        renderSystem.Render();
    }

    // FixedUpdate is called once per Time.fixedDeltaTime interval
    void FixedUpdate()
    {
        if(running) ActivateParticle();
    }

    public void ActivateParticle()
    {
        //system.ActivateRandomParticle();
        system.SimulateRound();
        UpdateRoundCounter();
    }

    public void SetSimSpeed(float roundTime)
    {
        if (roundTime == 0) roundTime = 0.02f; // dummy
        Time.fixedDeltaTime = roundTime;
        renderSystem.SetRoundTime(roundTime);
    }

    private void UpdateRoundCounter()
    {
        roundsText.text = "Round: " + system.CurrentRound;
        maxRoundText.text = "Max: " + system.LatestRound;
    }




    

    /// <summary>
    /// Toggles the Play/Pause functionality.
    /// </summary>
    public void TogglePlayPause()
    {
        if (!running)
        {
            system.ContinueTracking();
        }
        else
        {
            //system.Print();
        }
        running = !running;
        if (uiHandler != null) uiHandler.NotifyPlayPause(running);
    }

    public void ToggleView()
    {
        renderSystem.ToggleViewType();
    }

    public void ToggleCircuits()
    {
        renderSystem.ToggleCircuits();
    }

    public void ToggleAA()
    {
        renderSystem.ToggleAntiAliasing();
    }

    public void StepBack()
    {
        if (!running)
        {
            system.StepBack();
            UpdateRoundCounter();
        }
    }

    public void StepForward()
    {
        if (!running)
        {
            system.StepForward();
            UpdateRoundCounter();
        }
    }

    public void Jump()
    {
        if (!running)
        {
            string text = roundInput.text;
            if (int.TryParse(text, out int round))
            {
                if (round < system.EarliestRound || round > system.LatestRound)
                {
                    Debug.LogWarning("Round must be between earliest round (" + system.EarliestRound + ") and latest round (" + system.LatestRound + ")");
                }
                else
                {
                    system.SetMarkerToRound(round);
                    UpdateRoundCounter();
                }
            }
            else
            {
                Debug.LogWarning("Must enter a number");
            }
        }
    }

    public void CutOff()
    {
        if (!running)
        {
            system.CutOffAtMarker();
            UpdateRoundCounter();
        }
    }

    public void ResetSystem()
    {
        if (!running)
        {
            system.Reset();
            system.InitializeLineFormation(50, 0.4f);
            UpdateRoundCounter();
        }
    }
}
