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

    public bool useFCFS = true;     // <<<TEMPORARY>>> If true, do not crash on expansion conflicts but simply abort the expansion

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
    /// Simulates a round in which a single, randomly chosen particle
    /// is activated and all other particles remain inactive.
    /// </summary>
    public void ActivateRandomParticle()
    {
        if (particles.Count > 0)
        {
            particles[Random.Range(0, particles.Count)].Activate();
            ApplyAllActionsInQueue();
            CleanupAfterRound();
        }
    }

    public void SimulateRound()
    {
        ActivateParticles();
        ApplyAllActionsInQueue();
        CleanupAfterRound();
    }

    public void ActivateParticles()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].Activate();
        }
    }

    public void ApplyAllActionsInQueue()
    {
        while (actionQueue.Count > 0)
        {
            ApplyParticleAction(actionQueue.Dequeue());
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

    public void CleanupAfterRound()
    {
        // Remove hasMoved flags from all particles
        // TODO: Maybe remove scheduled actions too? Should actually be removed after processing
        // (No scheduled action should remain after processing the queue)
        foreach (Particle p in particles)
        {
            p.hasMoved = false;
        }
    }

    /**
     * Particle functions (called by particles to get information or trigger actions)
     */

    // TODO: Documentation

    public bool HasNeighborAt(Particle p, int locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
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

        // Reject if there is a contracted particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);
        if (particleMap.TryGetValue(targetLoc, out Particle p2) && p2.IsContracted())
        {
            throw new System.InvalidOperationException("Particle cannot expand onto node occupied by contracted particle.");
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
        // Reject if the particle is already expanded
        if (p.IsExpanded())
        {
            throw new System.InvalidOperationException("Expanded particle cannot perform a push handover.");
        }

        // Reject if there is no expanded particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);
        if (!particleMap.TryGetValue(targetLoc, out Particle p2) || p2.IsContracted())
        {
            throw new System.InvalidOperationException("Particle cannot perform push handover onto node occupied by no or contracted particle.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.scheduledAction != null)
        {
            Debug.LogWarning("Particle scheduling push handover already has a scheduled movement.");
        }

        // Store push handover action in particle and queue
        ParticleAction a = new ParticleAction(p, ActionType.PUSH, locDir);
        p.scheduledAction = a;
        actionQueue.Enqueue(a);
    }

    public void PerformPullHandoverHead(Particle p, int locDir)
    {
        PerformPullHandover(p, locDir, true);
    }

    public void PerformPullHandoverTail(Particle p, int locDir)
    {
        PerformPullHandover(p, locDir, false);
    }

    private void PerformPullHandover(Particle p, int locDir, bool head)
    {
        // Reject if the particle is already contracted
        if (p.IsContracted())
        {
            throw new System.InvalidOperationException("Contracted particle cannot perform pull handover.");
        }

        // Reject if there is no contracted particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, !head);
        if (!particleMap.TryGetValue(targetLoc, out Particle p2) || p2.IsExpanded())
        {
            throw new System.InvalidOperationException("Particle cannot perform pull handover onto node occupied by no or expanded particle.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.scheduledAction != null)
        {
            Debug.LogWarning("Particle scheduling pull handover already has a scheduled movement.");
        }

        // Store pull handover action in particle and queue
        ParticleAction a = new ParticleAction(p, head ? ActionType.PULL_HEAD : ActionType.PULL_TAIL);
        p.scheduledAction = a;
        actionQueue.Enqueue(a);
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
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);

        // Error if the target location is already occupied and the occupying particle
        // does not intend to move away
        if (particleMap.TryGetValue(targetLoc, out Particle p2))
        {
            // Target node is occupied, check if the occupying particle intends to move away
            if (p2.hasMoved || !MovementMatchesExpansion(p, p2, targetLoc))
            {
                if (!useFCFS)
                {
                    throw new System.InvalidOperationException("Particle tries to expand onto occupied node and occupying particle does not intend to move away.");
                }
                else
                {
                    // This movement would lead to a conflict, abort
                    p.hasMoved = true;
                    return;
                }
            }
        }

        // Action is allowed
        // First let the particle update its internal state
        p.Apply_Expand(locDir);
        // Then update the particle map (need a second entry that points to this particle)
        particleMap[p.Head()] = p;
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    public void Apply_ContractParticleHead(Particle p)
    {
        // Action is always allowed
        // First let the particle update its internal state
        Vector2Int tailPos = p.Tail();
        p.Apply_ContractHead();
        // Then update the particle map (need to remove the particle's tail entry if not removed yet)
        if (particleMap.TryGetValue(tailPos, out Particle p2) && p2 == p)
        {
            particleMap.Remove(tailPos);
        }
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    public void Apply_ContractParticleTail(Particle p)
    {
        // Action is always allowed
        // First let the particle update its internal state
        Vector2Int headPos = p.Head();
        p.Apply_ContractTail();
        // Then update the particle map (need to remove the particle's head entry if not removed yet)
        if (particleMap.TryGetValue(headPos, out Particle p2) && p2 == p)
        {
            particleMap.Remove(headPos);
        }
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    public void Apply_PerformPushHandover(Particle p, int locDir)
    {
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);

        // If the target location is already occupied: Make sure that the occupying particle
        // has not moved already (i.e., is the same one as before), and maybe move it if it
        // has a null action
        if (particleMap.TryGetValue(targetLoc, out Particle p2))
        {
            // Error if the other particle has already moved or intends to perform a non-matching movement
            if (p2.hasMoved || p2.scheduledAction != null && !MovementMatchesExpansion(p, p2, targetLoc))
            {
                if (!useFCFS)
                {
                    throw new System.InvalidOperationException("Particle tries to perform push handover but pushed particle has already moved or intends to perform a different movement.");
                }
                else
                {
                    // This movement would lead to a conflict, abort
                    p.hasMoved = true;
                    return;
                }
            }
            
            // If the other particle does not intend to do anything: Contract it manually
            if (p2.scheduledAction == null)
            {
                // p2 must be expanded at this point because it has not moved yet and cannot have been contracted when the action was scheduled
                if (targetLoc == p2.Tail())
                {
                    Apply_ContractParticleHead(p2);
                }
                else
                {
                    Apply_ContractParticleTail(p2);
                }
            }
        }

        // Action is allowed
        // First let the particle update its internal state
        p.Apply_PushHandover(locDir);
        // Then update the particle map (need a second entry that points to this particle)
        particleMap[p.Head()] = p;
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    public void Apply_PerformPullHandoverHead(Particle p, int locDir)
    {
        Apply_PerformPullHandover(p, locDir, true);
    }

    public void Apply_PerformPullHandoverTail(Particle p, int locDir)
    {
        Apply_PerformPullHandover(p, locDir, false);
    }

    private void Apply_PerformPullHandover(Particle p, int locDir, bool head)
    {
        Vector2Int targetLoc = head ? p.Tail() : p.Head();
        Vector2Int nbrLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, !head);

        // Error if the position from which we wanted to pull the particle is empty
        if (!particleMap.TryGetValue(nbrLoc, out Particle p2))
        {
            // FCFS: This should not happen at all, so throwing an exception is fine
            throw new System.InvalidOperationException("Particle tries to perform pull handover but there is no particle to pull.");
        }

        // Also throw error if the neighbor has already moved to a different position or intends to perform a different non-matching movement
        if (p2.hasMoved && p2.Head() != targetLoc && p2.Tail() != targetLoc || p2.scheduledAction != null && !MovementMatchesContraction(p, p2, targetLoc))
        {
            if (!useFCFS)
            {
                throw new System.InvalidOperationException("Particle tries to perform pull handover but pulled particle has already expanded somewhere else or intends to perform a different non-matching movement.");
            }
            else
            {
                // This movement would lead to a conflict, abort
                p.hasMoved = true;
                return;
            }
        }

        // If the other particle does not intend to do anything and has not moved yet: Expand it manually
        if (!p2.hasMoved && p2.scheduledAction == null)
        {
            // p2 must be contracted at this point because it has not moved yet and cannot have been expanded when the action was scheduled
            // Expansion direction is local((global(locDir) + 3) % 6)
            Apply_ExpandParticle(p2, ParticleSystem_Utils.GlobalToLocalDir((ParticleSystem_Utils.LocalToGlobalDir(locDir, p.comDir, p.chirality) + 3) % 6, p2.comDir, p2.chirality));
        }

        // Action is allowed
        // First let the particle update its internal state
        Vector2Int rmPos = head ? p.Tail() : p.Head();
        if (head)
        {
            p.Apply_PullHandoverHead(locDir);
        }
        else
        {
            p.Apply_PullHandoverTail(locDir);
        }
        // Then update the particle map (need to remove the particle's second entry if not removed yet)
        if (particleMap.TryGetValue(targetLoc, out p2) && p2 == p)
        {
            particleMap.Remove(rmPos);
        }
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    public void Apply_SendParticleMessage(Particle p, Message msg, int locDir, bool fromHead = true)
    {
        throw new System.NotImplementedException();
    }


    /**
     * Helpers
     */

    /// <summary>
    /// Checks if the scheduled <see cref="ParticleAction"/> of an expanded Particle
    /// matches the expansion action of a contracted neighbor.
    /// </summary>
    /// <param name="expandingPart">The Particle that wants to expand.</param>
    /// <param name="otherPart">The Particle whose action should be checked.</param>
    /// <param name="targetLoc">The grid node onto which <paramref name="expandingPart"/>
    /// wants to expand and which is occupied by <paramref name="otherPart"/>.</param>
    /// <returns><c>true</c> if and only if the <see cref="Particle.scheduledAction"/> of
    /// <paramref name="otherPart"/> is not <c>null</c> and allows <paramref name="expandingPart"/>
    /// to expand, i.e., if <paramref name="otherPart"/> intends to contract away from
    /// <paramref name="targetLoc"/> either through a regular contraction or through a
    /// pull handover directed at <paramref name="expandingPart"/>.</returns>
    private bool MovementMatchesExpansion(Particle expandingPart, Particle otherPart, Vector2Int targetLoc)
    {
        return otherPart.scheduledAction != null && (
            (otherPart.scheduledAction.type == ActionType.CONTRACT_HEAD && otherPart.Tail() == targetLoc) ||
            (otherPart.scheduledAction.type == ActionType.CONTRACT_TAIL && otherPart.Head() == targetLoc) ||
            (otherPart.scheduledAction.type == ActionType.PULL_HEAD && otherPart.Tail() == targetLoc && ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.scheduledAction.localDir, false) == expandingPart.Head()) ||
            (otherPart.scheduledAction.type == ActionType.PULL_TAIL && otherPart.Head() == targetLoc && ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.scheduledAction.localDir, true) == expandingPart.Head()));
    }

    /// <summary>
    /// Checks if the scheduled <see cref="ParticleAction"/> of a contracted Particle
    /// matches the contraction action of an expanded neighbor.
    /// </summary>
    /// <param name="contractingPart">The Particle that wants to contract with a pull handover.</param>
    /// <param name="otherPart">The Particle that is supposed to follow using an expansion.</param>
    /// <param name="targetLoc">The grid node from which <paramref name="contractingPart"/> wants to
    /// contract and onto which <paramref name="otherPart"/> is supposed to expand.</param>
    /// <returns><c>true</c> if and only if the <see cref="Particle.scheduledAction"/> of
    /// <paramref name="otherPart"/> is not <c>null</c> and follows the pull handover of
    /// <paramref name="contractingPart"/>, i.e., it is a regular expansion or a push handover
    /// directed at <paramref name="contractingPart"/>.</returns>
    private bool MovementMatchesContraction(Particle contractingPart, Particle otherPart, Vector2Int targetLoc)
    {
        return otherPart.scheduledAction != null && (
            (otherPart.scheduledAction.type == ActionType.EXPAND && targetLoc == ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.scheduledAction.localDir, true)) ||
            (otherPart.scheduledAction.type == ActionType.PUSH && targetLoc == ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.scheduledAction.localDir, true)));
    }
}
