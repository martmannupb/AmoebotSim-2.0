using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystem
{
    public List<Particle> particles;
    public Dictionary<Vector2Int, Particle> particleMap;


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
            Vector2 neighborPos = ;
        }
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
