using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmoebotSimulator : MonoBehaviour
{
    // <<<TEMPORARY>>> Prefab and instantiation method for simple visualization
    public GameObject particlePrefab;

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
        //system.InitializeExample(25, 15, 0.3f, -9, -5);
        system.InitializeLineFormation(50, 0.4f);
        //system.ActivateParticles();

        // Activate one particle every 1000ms (only for testing)
        InvokeRepeating(nameof(ActivateParticle), 0.0f, 0.01f);




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

        system.ActivateRandomParticle();
        //system.SimulateRound();
        //Debug.Log("Simulated round in " + (Time.realtimeSinceStartup - tStart) + " s");
    }

    // Update is called once per frame
    void Update()
    {
        renderSystem.Render();
    }

    // FixedUpdate is called once per Time.fixedDeltaTime interval
    void FixedUpdate()
    {
        
    }
}
