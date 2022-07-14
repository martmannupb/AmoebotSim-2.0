using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionType { EXPAND, CONTRACT_HEAD, CONTRACT_TAIL, PUSH, PULL_HEAD, PULL_TAIL, NULL }

/// <summary>
/// Represents an action a particle can schedule when it is activated.
/// <para>
/// Some particle actions need to be scheduled because applying them
/// immediately would violate the FSYNC execution model where all particles
/// operate on the same snapshot of the system. Thus, these actions are
/// scheduled and only applied after all particles have been activated.
/// </para>
/// </summary>
public class ParticleAction
{
    public Particle particle;
    public ActionType type;
    public int localDir;

    public ParticleAction(Particle particle = null, ActionType type = ActionType.NULL, int localDir = -1)
    {
        this.particle = particle;
        this.type = type;
        this.localDir = localDir;
    }
}
