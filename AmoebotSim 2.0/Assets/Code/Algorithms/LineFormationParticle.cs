using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LFState { IDLE, FLWR, ROOT, DONE, LEADER }

/// <summary>
/// Basic line formation algorithm that assumes a connected system
/// as well as common chirality and compass alignment. Designed to
/// work in the asynchronous and synchronous execution model.
/// </summary>
public class LineFormationParticle : ParticleAlgorithm
{
    // Used to create one leader particle
    private static bool leaderCreated = false;

    public ParticleAttribute_Enum<LFState> state;
    public ParticleAttribute_Direction constructionDir;
    public ParticleAttribute_Direction moveDir;
    public ParticleAttribute_Direction followDir;

    public LineFormationParticle(Particle p) : base(p)
    {
        constructionDir = new ParticleAttribute_Direction(this, "constructionDir", -1);
        moveDir = new ParticleAttribute_Direction(this, "moveDir", -1);
        followDir = new ParticleAttribute_Direction(this, "followDir", -1);
        state = new ParticleAttribute_Enum<LFState>(this, "State", LFState.IDLE);

        // Make one particle the leader
        if (!leaderCreated)
        {
            state.SetValue(LFState.LEADER);
            constructionDir.SetValue(Random.Range(0, 6));
            Debug.Log("Construction dir: " + (int)constructionDir);
            leaderCreated = true;
        }
    }

    public override void Activate()
    {
        switch ((LFState)state)
        {
            case LFState.DONE:
            case LFState.LEADER:
                return;
            case LFState.IDLE: IdleActivate();
                break;
            case LFState.ROOT: RootActivate();
                break;
            case LFState.FLWR: FlwrActivate();
                break;
            default: throw new System.InvalidOperationException("Undefined state " + state);
        }
    }

    private void IdleActivate()
    {
        // Check if neighbor is LEADER or DONE, if yes become ROOT
        if (FindFirstNeighborWithProperty<LineFormationParticle>((LineFormationParticle p) => p.state == LFState.LEADER || p.state == LFState.DONE, out Neighbor<LineFormationParticle> nbr))
        {
            // TODO: Do something else here
            Debug.Log("Found neighbor that is LEADER or DONE!");
            state.SetValue(LFState.DONE);
        }

        // Check if neighbor is ROOT, if yes become FLWR

        // Check if neighbor is FLWR, if yes become FLWR
    }

    private void RootActivate()
    {

    }

    private void FlwrActivate()
    {

    }
}
