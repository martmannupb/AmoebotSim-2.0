using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using System;
using TMPro;

public class AmoebotSimulator : MonoBehaviour
{

    // Singleton
    public static AmoebotSimulator instance;

    // System Data
    public ParticleSystem system;
    public RenderSystem renderSystem;
    // System State
    public bool running = true;
    
    // UI
    public UIHandler uiHandler;

    public AmoebotSimulator()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        //// MinMax Test
        //MinMax minMax = new MinMax(2.45f, 3, false);
        //Debug.Log("MinMax Test: " + MinMax.Parse(minMax.ToString()));
        //minMax = new MinMax(2.45f, 3, true);
        //Debug.Log("MinMax Test: " + MinMax.Parse(minMax.ToString()));

        // Init Renderer + Particle System
        renderSystem = new RenderSystem(this, FindObjectOfType<InputController>());
        system = new ParticleSystem(this, renderSystem);
        // Set Sim Speed (not necessary with the new UI)
        //SetSimSpeed(0.005f);
        //SetSimSpeed(0.5f);

        // Register UI
        if(uiHandler != null) uiHandler.RegisterSim(this);

        // Init Algorithm
        //system.InitializeExample(1, 1, 1f, -9, -5);
        //system.InitializeExample(50, 50, 0.3f, -9, -5);
        //system.InitializeLineFormation(50, 0.4f);
        //system.InitializeLineFormation(25, 0.4f);
        //system.InitializeLeaderElection(50, 0.35f);
        //system.InitializeChiralityCompass(50, 0.2f);
        //system.InitializeBoundaryTest(100, 0.05f);
        //system.InitializeExpandedTest(10);
        //system.InitializeJMTest(17);

        // Open Init Mode (when initialized)
        StartCoroutine(OpenInitModeCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        renderSystem.Render();
    }

    // FixedUpdate is called once per Time.fixedDeltaTime interval
    void FixedUpdate()
    {
        if(running)
        {
            ActivateParticle();
        }
    }

    public void ActivateParticle()
    {
        //system.ActivateRandomParticle();
        system.SimulateRound();
        RoundChanged();
    }

    public void RoundChanged()
    {
        uiHandler.particleUI.SimState_RoundChanged();
        if (WorldSpaceUIHandler.instance != null) WorldSpaceUIHandler.instance.Refresh();
    }

    public void SetSimSpeed(float roundTime)
    {
        if (roundTime == 0)
        {
            // Max Speed
            roundTime = 0.01f;
        }
        Time.fixedDeltaTime = roundTime;
        renderSystem.SetRoundTime(roundTime);
    }

    

    /// <summary>
    /// Toggles the Play/Pause functionality.
    /// </summary>
    public void TogglePlayPause()
    {
        if(uiHandler.initializationUI.IsOpen() == false)
        {
            if (running == false)
            {
                system.ContinueTracking();
                if (EventDatabase.event_sim_startedStopped != null) EventDatabase.event_sim_startedStopped(true);
            }
            else
            {
                //system.Print();
                if (EventDatabase.event_sim_startedStopped != null) EventDatabase.event_sim_startedStopped(false);
            }
            running = !running;
            if (uiHandler != null) uiHandler.NotifyPlayPause(running);
        }
    }

    public void PlaySim()
    {
        if(running == false)
        {
            TogglePlayPause();
        }
    }

    public void PauseSim()
    {
        if(running)
        {
            TogglePlayPause();
        }
    }

    private IEnumerator OpenInitModeCoroutine()
    {
        yield return new WaitForEndOfFrame();
        if (uiHandler != null && uiHandler.initializationUI != null && uiHandler.initializationUI.IsInitialized())
        {
            uiHandler.initializationUI.Open();
        }
        else
        {
            StartCoroutine(OpenInitModeCoroutine());
        }
    }

}
