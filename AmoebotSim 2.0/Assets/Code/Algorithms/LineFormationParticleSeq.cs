using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Basic line formation algorithm that assumes a connected system
/// as well as common chirality and compass alignment. Designed to
/// work in the asynchronous execution model with one activation per round.
/// </summary>
public class LineFormationParticleSeq : ParticleAlgorithm
{
    public enum LFState { IDLE, FLWR, ROOT, DONE, LEADER }

    private static Color leaderColor = ColorData.Green;
    private static Color idleColor = ColorData.Black;
    private static Color rootColor = ColorData.Red;
    private static Color flwrColor = ColorData.Blue;
    private static Color doneColor = ColorData.Yellow;

    // Used to create one leader particle
    private static bool leaderCreated = false;

    public ParticleAttribute<LFState> state;
    public ParticleAttribute<int> constructionDir;
    public ParticleAttribute<int> moveDir;
    public ParticleAttribute<int> followDir;

    // Helpers to make sure that followDir is updated only if a push handover was successful
    // TODO: Switch to only doing pull handovers (pulling particle can choose, but need message to update followDir)
    public ParticleAttribute<bool> havePushed;
    public ParticleAttribute<int> newFollowDir;

    public LineFormationParticleSeq(Particle p) : base(p)
    {
        constructionDir = CreateAttributeDirection("constructionDir", -1);
        moveDir = CreateAttributeDirection("moveDir", -1);
        followDir = CreateAttributeDirection("followDir", -1);
        state = CreateAttributeEnum<LFState>("State", LFState.IDLE);

        SetMainColor(idleColor);

        // Make one particle the leader
        if (!leaderCreated)
        {
            state.SetValue(LFState.LEADER);
            constructionDir.SetValue(Random.Range(0, 6));
            SetMainColor(leaderColor);
            Debug.Log("Line construction dir: " + (int)constructionDir);
            leaderCreated = true;
        }

        havePushed = CreateAttributeBool("havePushed", false);
        newFollowDir = CreateAttributeDirection("newFollowDir", -1);
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
        // Check if neighbor is LEADER or DONE, if yes become DONE or ROOT
        if (FindFirstNeighborWithProperty((LineFormationParticleSeq p) => p.state == LFState.LEADER || p.state == LFState.DONE, out Neighbor<LineFormationParticleSeq> nbrDone))
        {
            // Try to become DONE
            if (TryToBecomeDone(nbrDone))
                return;

            // Otherwise become ROOT
            state.SetValue(LFState.ROOT);
            SetMainColor(rootColor);
            ComputeRootMoveDir(nbrDone);
            constructionDir.SetValue(nbrDone.neighbor.constructionDir);
            return;
        }

        // Check if neighbor is ROOT, if yes become FLWR
        if (FindFirstNeighborWithProperty((LineFormationParticleSeq p) => p.state == LFState.ROOT, out Neighbor<LineFormationParticleSeq> nbrRoot))
        {
            state.SetValue(LFState.FLWR);
            SetMainColor(flwrColor);
            constructionDir.SetValue(nbrRoot.neighbor.constructionDir);
            followDir.SetValue(nbrRoot.localDir);
            return;
        }

        // Check if neighbor is FLWR, if yes become FLWR
        if (FindFirstNeighborWithProperty((LineFormationParticleSeq p) => p.state == LFState.FLWR, out Neighbor<LineFormationParticleSeq> nbrFlwr))
        {
            state.SetValue(LFState.FLWR);
            SetMainColor(flwrColor);
            constructionDir.SetValue(nbrFlwr.neighbor.constructionDir);
            followDir.SetValue(nbrFlwr.localDir);
            return;
        }
    }

    private void RootActivate()
    {
        // If we are expanded: Contract or perform pull handover if possible (handover not yet possible because followDir has to be changed)
        if (IsExpanded())
        {
            // Search for a blocking neighbor
            if (HaveBlockingTailNeighbor())
                return;
            // Did not find a blocking neighbor => Can contract
            ContractHead();
        }
        // If we are contracted: Try to become DONE, otherwise compute new move direction and try expanding
        else
        {
            // We know that a neighbor that is LEADER or DONE must exist
            if (FindFirstNeighborWithProperty((LineFormationParticleSeq p) => p.state == LFState.LEADER || p.state == LFState.DONE, out Neighbor<LineFormationParticleSeq> nbrDone))
            {
                if (TryToBecomeDone(nbrDone))
                    return;

                ComputeRootMoveDir(nbrDone);
                // Expand if there is a free node in movement direction or try to do a push handover with another ROOT
                ParticleAlgorithm nbr = GetNeighborAt(moveDir);
                if (nbr == null)
                {
                    Expand(moveDir);
                    return;
                }
                LineFormationParticleSeq lfp = (LineFormationParticleSeq)nbr;
                if (lfp.state == LFState.ROOT && lfp.IsExpanded() && !IsHeadAt(moveDir))
                {
                    PushHandover(moveDir);
                    return;
                }
            }
        }
    }

    private void FlwrActivate()
    {
        // If we are expanded: Try to contract
        if (IsExpanded())
        {
            // First check if we tried to push before
            if (havePushed)
            {
                // It worked!
                havePushed.SetValue(false);
                followDir.SetValue(newFollowDir);
            }

            // Search for a blocking neighbor
            // TODO: This is the same code as for the ROOT state
            if (HaveBlockingTailNeighbor())
                return;

            // Did not find a blocking neighbor => Can contract
            ContractHead();
        }
        // If we are contracted: Try to become DONE or ROOT, otherwise try push handover into followed particle
        else
        {
            // First check if we tried to push before
            if (havePushed)
            {
                // It didn't work
                havePushed.SetValue(false);
            }

            // Try to become DONE or ROOT
            // TODO: This is copied from the IDLE state
            if (FindFirstNeighborWithProperty((LineFormationParticleSeq p) => p.state == LFState.LEADER || p.state == LFState.DONE, out Neighbor<LineFormationParticleSeq> nbrDone))
            {
                // Try to become DONE
                if (TryToBecomeDone(nbrDone))
                    return;

                // Otherwise become ROOT
                state.SetValue(LFState.ROOT);
                SetMainColor(rootColor);
                ComputeRootMoveDir(nbrDone);
                constructionDir.SetValue(nbrDone.neighbor.constructionDir);
                return;
            }
            // Did not become DONE or ROOT, try pushing into followed particle
            ParticleAlgorithm nbr = GetNeighborAt(followDir, true);
            if (nbr != null && nbr.IsExpanded() && !IsHeadAt(followDir, true) /* UGLY HACK */ && !((LineFormationParticleSeq)nbr).havePushed)
            {
                PushHandover(followDir);
                havePushed.SetValue(true);
                newFollowDir.SetValue(nbr.HeadDirection());
            }
        }
    }

    private bool HaveBlockingTailNeighbor()
    {
        // A neighbor is blocking if it is IDLE and adjacent to our tail or FLWR that is following our tail
        // TODO: Can simplify this using better neighbor discovery methods
        for (int d = 0; d < 6; d++)
        {
            if (d == HeadDirection())
                continue;
            ParticleAlgorithm nbr = GetNeighborAt(d, false);
            if (nbr != null && IsHeadAt(d, false))
            {
                LineFormationParticleSeq lfp = (LineFormationParticleSeq)nbr;
                if (lfp.state == LFState.IDLE || lfp.state == LFState.FLWR && (lfp.followDir == ((d + 3) % 6) ||
                    // Ugly hack: Look at followDir update mechanism to prevent disconnection
                    lfp.havePushed && lfp.newFollowDir == ((d + 3) % 6)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryToBecomeDone(Neighbor<LineFormationParticleSeq> nbr)
    {
        int cd = nbr.neighbor.constructionDir;
        if (cd == -1)
            return false;

        // Safe to always set constructionDir because we have common chirality and compass orientation
        constructionDir.SetValue(cd);
        if (constructionDir == (nbr.localDir + 3) % 6)
        {
            state.SetValue(LFState.DONE);
            SetMainColor(doneColor);
            return true;
        }
        return false;
    }

    private void ComputeRootMoveDir(Neighbor<LineFormationParticleSeq> nbr)
    {
        // We already know cnostructionDir, set moveDir relative to neighbor position
        // On the other end of the line => Move around the left side
        if (constructionDir == nbr.localDir)
        {
            moveDir.SetValue((constructionDir + 1) % 6);
            return;
        }

        // Left or right side of the line => Move up the line
        if (nbr.localDir == (constructionDir + 5) % 6 || nbr.localDir == (constructionDir + 4) % 6)
        {
            // On left side
            // First check if we can move to the end position of the line
            ParticleAlgorithm nbr2 = GetNeighborAt((constructionDir + 5) % 6);
            if (nbr2 == null || (((LineFormationParticleSeq)nbr2).state != LFState.LEADER && ((LineFormationParticleSeq)nbr2).state != LFState.DONE))
            {
                // Position is empty or occupied by non-LEADER, non-DONE particle => try to move there
                moveDir.SetValue((constructionDir + 5) % 6);
            }
            else
            {
                // Position is already part of the line, move forward
                moveDir.SetValue(constructionDir);
            }
        }
        else if (nbr.localDir == (constructionDir + 1) % 6 || nbr.localDir == (constructionDir + 2) % 6)
        {
            // On right side
            // First check if we can move to the end position of the line
            ParticleAlgorithm nbr2 = GetNeighborAt((constructionDir + 1) % 6);
            if (nbr2 == null || (((LineFormationParticleSeq)nbr2).state != LFState.LEADER && ((LineFormationParticleSeq)nbr2).state != LFState.DONE))
            {
                // Position is empty or occupied by non-LEADER, non-DONE particle => try to move there
                moveDir.SetValue((constructionDir + 1) % 6);
            }
            else
            {
                // Position is already part of the line, move forward
                moveDir.SetValue(constructionDir);
            }
        }
    }
}
