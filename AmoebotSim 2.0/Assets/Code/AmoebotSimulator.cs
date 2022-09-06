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
        if(running)
        {
            ActivateParticle();
        }
    }

    public void ActivateParticle()
    {
        //system.ActivateRandomParticle();
        system.SimulateRound();
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
        if (running == false)
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

    public void ToggleView()
    {
        renderSystem.ToggleViewType();
    }

    public void ToggleCircuits()
    {
        renderSystem.ToggleCircuits();
    }
}
