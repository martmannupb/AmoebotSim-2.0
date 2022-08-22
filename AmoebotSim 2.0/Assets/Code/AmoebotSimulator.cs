using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AmoebotSimulator : MonoBehaviour
{
    // <<<TEMPORARY>>> Prefab and instantiation method for simple visualization
    public GameObject particlePrefab;

    public TextMeshProUGUI roundsText;
    public TextMeshProUGUI maxRoundText;
    public TMP_InputField roundInput;

    public void AddParticle(float x, float y)
    {
        Instantiate(particlePrefab, new Vector2(x, y), particlePrefab.transform.rotation);
    }


    // TODO: Make this public and assign in editor?
    // (dont know, maybe it is better if we do like this via code, so we might be able to initialize the system with parameters for something like the graphics)
    private ParticleSystem system;
    private RenderSystem renderSystem;

    // Start is called before the first frame update
    void Start()
    {
        renderSystem = new RenderSystem();
        system = new ParticleSystem(this, renderSystem);

        // Activate one particle every 1000ms (only for testing)
        //InvokeRepeating(nameof(ActivateParticle), 0.0f, 1.0f);
        Time.fixedDeltaTime = 0.005f;
        Time.fixedDeltaTime = 0.5f;

        //system.InitializeExample(1, 1, 1f, -9, -5);
        //system.InitializeExample(50, 50, 0.3f, -9, -5);
        //system.InitializeLineFormation(50, 0.4f);
        //system.InitializeLineFormation(25, 0.4f);
        //system.InitializeLeaderElection(50, 0.35f);
        //system.InitializeChiralityCompass(50, 0.2f);
        //system.InitializeBoundaryTest(100, 0.05f);
        system.InitializeExpandedTest(10);



        // Test Area -----
        Debug.Log("V1: " + AmoebotFunctions.GetGridPositionFromWorldPosition(new Vector2(0, 0)));
        Debug.Log("V2: " + AmoebotFunctions.GetGridPositionFromWorldPosition(new Vector2(0.5f, 0.1f)));
        Debug.Log("V3: " + AmoebotFunctions.GetGridPositionFromWorldPosition(new Vector2(50f, 42.2f)));
        Debug.Log("V3 Inverted: " + AmoebotFunctions.CalculateAmoebotCenterPositionVector2(AmoebotFunctions.GetGridPositionFromWorldPosition(new Vector2(50f, 42.2f)).x, AmoebotFunctions.GetGridPositionFromWorldPosition(new Vector2(50f, 42.2f)).y));
        // -----
    }

    public void ActivateParticle()
    {
        //Debug.Log("Activate");
        //float tStart = Time.realtimeSinceStartup;

        //system.ActivateRandomParticle();
        system.SimulateRound();
        UpdateRoundCounter();
        //activationsText.text = "Round: " + system.CurrentRound;
        //Debug.Log("Simulated round in " + (Time.realtimeSinceStartup - tStart) + " s");
    }

    private void UpdateRoundCounter()
    {
        roundsText.text = "Round: " + system.CurrentRound;
        maxRoundText.text = "Max: " + system.LatestRound;
    }

    // Update is called once per frame
    void Update()
    {
        renderSystem.Render();
    }

    // FixedUpdate is called once per Time.fixedDeltaTime interval
    void FixedUpdate()
    {
        if(play) ActivateParticle();
    }





    private bool play = true;

    /// <summary>
    /// Toggles the Play/Pause functionality.
    /// </summary>
    public void TogglePlayPause()
    {
        if (!play)
        {
            system.ContinueTracking();
        }
        else
        {
            //system.Print();
        }
        play = !play;
    }

    public void ToggleView()
    {
        renderSystem.ToggleViewType();
    }

    public void ToggleCircuits()
    {
        renderSystem.ToggleCircuits();
    }

    public void StepBack()
    {
        if (!play)
        {
            system.StepBack();
            UpdateRoundCounter();
        }
    }

    public void StepForward()
    {
        if (!play)
        {
            system.StepForward();
            UpdateRoundCounter();
        }
    }

    public void Jump()
    {
        if (!play)
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
        if (!play)
        {
            system.CutOffAtMarker();
            UpdateRoundCounter();
        }
    }

    public void ResetSystem()
    {
        if (!play)
        {
            system.Reset();
            system.InitializeLineFormation(50, 0.4f);
            UpdateRoundCounter();
        }
    }
}
