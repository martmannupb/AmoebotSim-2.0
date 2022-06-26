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
                    Particle p = new ExampleParticle(this, new Vector2Int(left + col - row / 2, bottom + row));
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

    /**
     * Particle functions
     */

    // TODO: Actual implementation
    // TODO: Documentation

    public bool HasNeighborAt(Particle p, int locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
        //Particle nbr = null;
        // Return true iff there is a particle at that position and it is not the
        // same as the querying particle
        return particleMap.TryGetValue(pos, out Particle nbr) && nbr != p;
    }

    // TODO: How to handle case that neighbor does not exist? For now just return null
    public Particle GetNeighborAt(Particle p, int locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
        if (particleMap.TryGetValue(pos, out Particle nbr) && nbr != p)
            return nbr;
        else
            return null;
    }

    public void ExpandParticle(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void ContractParticleHead(Particle p)
    {
        throw new System.NotImplementedException();
    }

    public void ContractParticleTail(Particle p)
    {
        throw new System.NotImplementedException();
    }

    public void PerformPushHandover(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void PerformPullHandoverHead(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void PerformPullHandoverTail(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void SendParticleMessage(Particle p, Message msg, int locDir, bool fromHead = true)
    {
        throw new System.NotImplementedException();
    }

    // ...
}
