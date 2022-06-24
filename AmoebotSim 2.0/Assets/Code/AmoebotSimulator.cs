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
    private ParticleSystem system;

    // Start is called before the first frame update
    void Start()
    {
        system = new ParticleSystem(this);
        system.InitializeExample(25, 15, 0.3f, -9, -5);
        system.ActivateParticles();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
