using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystem
{
    // References
    public AmoebotSimulator sim;
    public RenderSystem renderSystem;

    public List<Particle> particles = new List<Particle>();
    public Dictionary<Vector2Int, Particle> particleMap = new Dictionary<Vector2Int, Particle>();

    public Queue<ParticleAction> actionQueue = new Queue<ParticleAction>();

    public ParticleSystem(AmoebotSimulator sim, RenderSystem renderSystem)
    {
        this.sim = sim;
        this.renderSystem = renderSystem;
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
                    //sim.AddParticle(p.Head().x + 0.5f * p.Head().y, p.Head().y * Mathf.Sqrt(0.75f));      // Note: Replaced by RenderSystem
                }
            }
        }
        Debug.Log("Created system with " + num + " particles");
    }

    /**
     * Simulation functions
     */

    /// <summary>
    /// Activates a single, randomly chosen particle in the system.
    /// </summary>
    public void ActivateRandomParticle()
    {
        if (particles.Count > 0)
        {
            particles[Random.Range(0, particles.Count)].Activate();
            ApplyNextActionInQueue();
        }
    }

    public void ActivateParticles()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].Activate();
        }
    }

    public void ApplyNextActionInQueue()
    {
        if (actionQueue.Count > 0)
        {
            ApplyParticleAction(actionQueue.Dequeue());
        }
    }

    public void ApplyParticleAction(ParticleAction a)
    {
        switch (a.type)
        {
            case ActionType.EXPAND: Apply_ExpandParticle(a.particle, a.localDir);
                break;
            case ActionType.CONTRACT_HEAD: Apply_ContractParticleHead(a.particle);
                break;
            case ActionType.CONTRACT_TAIL: Apply_ContractParticleTail(a.particle);
                break;
            case ActionType.PUSH: Apply_PerformPushHandover(a.particle, a.localDir);
                break;
            case ActionType.PULL_HEAD: Apply_PerformPullHandoverHead(a.particle, a.localDir);
                break;
            case ActionType.PULL_TAIL: Apply_PerformPullHandoverTail(a.particle, a.localDir);
                break;
            default:
                throw new System.ArgumentException("Unknown ParticleAction type " + a.type);
        }
        // Finally remove the particle's scheduled action if it is this one
        if (a == a.particle.scheduledAction)
        {
            a.particle.scheduledAction = null;
        }
    }

    /**
     * Particle functions (called by particles to trigger actions)
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
        // Reject if the particle is already expanded
        if (p.IsExpanded())
        {
            throw new System.InvalidOperationException("Expanded particle cannot expand again.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.scheduledAction != null)
        {
            Debug.LogWarning("Expanding particle already has a scheduled movement.");
        }

        // Store expansion action in particle and queue
        ParticleAction a = new ParticleAction(p, ActionType.EXPAND, locDir);
        p.scheduledAction = a;
        actionQueue.Enqueue(a);
    }

    public void ContractParticleHead(Particle p)
    {
        ContractParticle(p, true);
    }

    public void ContractParticleTail(Particle p)
    {
        ContractParticle(p, false);
    }

    private void ContractParticle(Particle p, bool head)
    {
        // Reject if the particle is already contracted
        if (p.IsContracted())
        {
            throw new System.InvalidOperationException("Contracted particle cannot contract again.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.scheduledAction != null)
        {
            Debug.LogWarning("Contracting particle already has a scheduled movement.");
        }

        // Store contraction action in particle and queue
        ParticleAction a = new ParticleAction(p, head ? ActionType.CONTRACT_HEAD : ActionType.CONTRACT_TAIL);
        p.scheduledAction = a;
        actionQueue.Enqueue(a);
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


    /**
     * State change functions (called to update the simulation state)
     * These functions should NOT be called by particles!
     */

    public void Apply_ExpandParticle(Particle p, int locDir)
    {
        // Error if the target location is already occupied and the occupying particle
        // does not intend to move away
        // TODO: This is only an error in the FSYNC model!
        Vector2Int targetLoc = ParticleSystem_Utils.GetNbrInDir(p.Head(), ParticleSystem_Utils.LocalToGlobalDir(locDir, p.comDir, p.chirality));
        Particle p2 = null;
        if (particleMap.TryGetValue(targetLoc, out p2))
        {
            // Target node is occupied, check if the occupying particle intends to move away
            if (p2.scheduledAction == null ||
                !(p2.scheduledAction.type == ActionType.CONTRACT_HEAD && p2.Tail() == p.Head()) &&
                !(p2.scheduledAction.type == ActionType.CONTRACT_TAIL && p2.Head() == p.Head()) &&
                !(p2.scheduledAction.type == ActionType.PULL_HEAD && ParticleSystem_Utils.GetNeighborPosition(p2, p2.scheduledAction.localDir, false) == p.Head()) &&
                !(p2.scheduledAction.type == ActionType.PULL_TAIL && ParticleSystem_Utils.GetNeighborPosition(p2, p2.scheduledAction.localDir, true) == p.Head()))
            {
                throw new System.InvalidOperationException("Particle tries to expand onto occupied node and occupying particle does not intend to move away.");
            }
        }

        // Action is allowed
        // First let the particle update its internal state
        p.Apply_Expand(locDir);
        // Then update the particle map (need a second entry that points to this particle)
        particleMap.Add(p.Head(), p);
    }

    public void Apply_ContractParticleHead(Particle p)
    {
        // Action is always allowed
        // First let the particle update its internal state
        Vector2Int tailPos = p.Tail();
        p.Apply_ContractHead();
        // Then update the particle map (need to remove the particle's tail entry)
        particleMap.Remove(tailPos);
    }

    public void Apply_ContractParticleTail(Particle p)
    {
        // Action is always allowed
        // First let the particle update its internal state
        Vector2Int headPos = p.Head();
        p.Apply_ContractTail();
        // Then update the particle map (need to remove the particle's head entry)
        particleMap.Remove(headPos);
    }

    public void Apply_PerformPushHandover(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void Apply_PerformPullHandoverHead(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void Apply_PerformPullHandoverTail(Particle p, int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void Apply_SendParticleMessage(Particle p, Message msg, int locDir, bool fromHead = true)
    {
        throw new System.NotImplementedException();
    }
}
