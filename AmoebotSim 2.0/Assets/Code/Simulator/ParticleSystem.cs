using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystem
{
    // References
    private AmoebotSimulator sim;

    public List<Particle> particles = new List<Particle>();
    public Dictionary<Vector2Int, Particle> particleMap = new Dictionary<Vector2Int, Particle>();

    public ParticleSystem(AmoebotSimulator sim)
    {
        this.sim = sim;
    }

    /// <summary>
    /// Initializes the system with ExampleParticles for testing purposes.
    /// This should be removed after a proper initialization method has been created.
    /// </summary>
    public void InitializeExample(int width, int height, float spawnProb, int left = 0, int bottom = 0)
    {
        // Fill a "rectangle" randomly with particles (it should be an actual rectangle)
        int num = 0;
        
        for (int col = 0; col < width; ++col)
        {
            for (int row = 0; row < height; ++row)
            {
                if (Random.Range(0f, 1f) <= spawnProb)
                {
                    // TODO: Create functions for adding and removing particles
                    // Don't use column as x coordinate but shift it to stay in a rectangular shape
                    Particle p = new ExampleParticle(this, left + col - row / 2, bottom + row);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                    ++num;

                    // <<<TEMPORARY>>> Add GameObject to the scene for visualization
                    sim.AddParticle(p.Head().x + 0.5f * p.Head().y, p.Head().y * Mathf.Sqrt(0.75f));
                }
            }
        }
        Debug.Log("Created system with " + num + " particles");
    }



    public void ActivateParticles()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].Activate();
        }
    }

    public Particle GetNeighborAt(Particle p, int locDir, bool isHead)
    {
        if(p.IsExpanded())
        {
            // Expanded

        }
        else
        {
            // Contracted
            
        }
        throw new System.NotImplementedException();
    }
    









    /// <summary>
    /// Queue an expansion movement of a particle. This is executed at the end of the round.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="locDir"></param>
    public void ExpandParticle(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void ContractParticleHead(Particle p)
    {
        throw new System.NotImplementedException();
    }

    // ...
}
