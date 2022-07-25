using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Basic line formation algorithm that assumes a connected system
/// as well as common chirality and compass alignment. Designed to
/// work in the fully synchronous execution model where all particles
/// are activated in each round.
/// <para>
/// The algorithm uses only 1 pin per edge.
/// </para>
/// </summary>
public class LineFormationParticleSync : ParticleAlgorithm
{
    public enum LFState { IDLE, FLWR, ROOT, DONE, LEADER }

    private static Color leaderColor = ColorData.Aqua;
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

    public LineFormationParticleSync(Particle p) : base(p)
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
            Debug.Log("Line construction dir: " + (int)constructionDir.GetValue_After());
            leaderCreated = true;
        }

        //havePushed = CreateAttributeBool("havePushed", false);
        //newFollowDir = CreateAttributeDirection("newFollowDir", -1);
    }

    // Only need one pin per edge in this algorithm because communication
    // is very simple
    public override int PinsPerEdge => 1;

    public override void Activate()
    {
        switch ((LFState)state)
        {
            case LFState.LEADER:
                LeaderActivate();
                break;
            case LFState.IDLE:
                IdleActivate();
                break;
            case LFState.ROOT:
                RootActivate();
                break;
            case LFState.FLWR:
                FlwrActivate();
                break;
            case LFState.DONE:
                DoneActivate();
                break;
            default: throw new System.InvalidOperationException("Undefined state " + state);
        }
    }

    private void LeaderActivate()
    {
        Debug.Log("Leader!");
        return;
    }

    private void IdleActivate()
    {
        // Check if neighbor is LEADER or DONE, if yes become DONE or ROOT
        if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.LEADER || p.state == LFState.DONE, out Neighbor<LineFormationParticleSync> nbrDone))
        {
            // Become DONE if we are at the end of the line
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
        if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.ROOT, out Neighbor<LineFormationParticleSync> nbrRoot))
        {
            state.SetValue(LFState.FLWR);
            SetMainColor(flwrColor);
            constructionDir.SetValue(nbrRoot.neighbor.constructionDir);
            followDir.SetValue(nbrRoot.localDir);
            return;
        }

        // Check if neighbor is FLWR, if yes become FLWR
        // (This comes after the previous check because we prioritize following ROOTs over other FLWRs)
        if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.FLWR, out Neighbor<LineFormationParticleSync> nbrFlwr))
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
        Debug.Log("Root! " + constructionDir.GetValue_After());

        int cd = constructionDir.GetValue_After();

        // ROOT handovers take precedence: Try performing handover with ROOT first
        if (IsContracted())
        {
            // Always compute the move direction when contracted
            ComputeRootMoveDir();
            int md = moveDir.GetValue_After();

            // If we are contracted and we can expand freely or push into an expanded ROOT: Do it
            // Contracted ROOTs can almost always expand
            LineFormationParticleSync nbr = GetNeighborAt(md) as LineFormationParticleSync;
            if (nbr == null)
            {
                Expand(md);
            }
            else if (nbr.state == LFState.ROOT && nbr.IsExpanded())
            {
                Debug.Log("Push handover in direction " + md);
                PushHandover(md);
            }
        }
        else
        {
            // If we have sent a beep to a FLWR neighbor in the last round: Perform pull handover
            //if (PullIfSentBeep())
            //{
            //    return;
            //}

            // If there is a ROOT neighbor that we can pull: Do it
            // ROOT neighbors to pull can only be at our tail in direction
            // constructionDir + 3 or constructionDir + 4
            LineFormationParticleSync nbr = GetNeighborAt((cd + 3) % 6, false) as LineFormationParticleSync;
            if (nbr != null)
            {
                if (nbr.state == LFState.ROOT && nbr.IsContracted())
                {
                    Debug.Log("Pull handover (1) in direction " + (cd + 3) % 6);
                    PullHandoverHead((cd + 3) % 6);
                    return;
                }
            }
            else
            {
                nbr = GetNeighborAt((cd + 4) % 6, false) as LineFormationParticleSync;
                if (nbr != null && nbr.state == LFState.ROOT && nbr.IsContracted())
                {
                    Debug.Log("Pull handover (2) in direction " + (cd + 4) % 6);
                    PullHandoverHead((cd + 4) % 6);
                    return;
                }
            }
            return;
            // ROOT handover did not work: Try pulling a FLWR instead (this can prevent a ROOT handover in the next round)
            if (SendBeepForPull())
            {
                return;
            }

            // No handover possible: Contract on our own if there is no blocking tail neighbor
            if (!HaveBlockingTailNeighbor())
            {
                ContractHead();
            }
        }
    }

    private void FlwrActivate()
    {
        Debug.Log("Follower!");

        PinConfiguration pc = GetCurrentPinConfiguration();

        if (IsContracted())
        {
            // Contracted FLWR must wait for followed particle to send beep
            if (pc.GetPinAt(followDir, 0).PartitionSet.ReceivedBeep())
            {
                PushHandover(followDir);
                return;
            }
        }
        else
        {
            // Expanded FLWR can pull other FLWR just like ROOTs do when they cannot pull a ROOT
            if (PullIfSentBeep())
            {
                return;
            }

            if (SendBeepForPull())
            {
                return;
            }

            // Could not pull a FLWR: Try contracting if we are not blocked
            if (!HaveBlockingTailNeighbor())
            {
                ContractHead();
            }
        }
    }

    private void DoneActivate()
    {
        Debug.Log("Done!");
        return;
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
                LineFormationParticleSync lfp = (LineFormationParticleSync)nbr;
                if (lfp.state == LFState.IDLE || lfp.state == LFState.FLWR && (lfp.followDir == ((d + 3) % 6)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryToBecomeDone(Neighbor<LineFormationParticleSync> nbr)
    {
        int cd = nbr.neighbor.constructionDir;
        if (cd == -1)
            return false;

        // Safe to always set constructionDir because we have common chirality and compass orientation
        constructionDir.SetValue(cd);
        if (constructionDir.GetValue_After() == (nbr.localDir + 3) % 6)
        {
            state.SetValue(LFState.DONE);
            SetMainColor(doneColor);
            return true;
        }
        return false;
    }

    private void ComputeRootMoveDir(Neighbor<LineFormationParticleSync> nbr)
    {
        // We already know constructionDir, set moveDir relative to neighbor position
        // On the other end of the line => Move around the left side
        int cd = constructionDir.GetValue_After();
        if (cd == nbr.localDir)
        {
            moveDir.SetValue((cd + 1) % 6);
            return;
        }

        // Left or right side of the line => Move up the line
        if (nbr.localDir == (cd + 5) % 6 || nbr.localDir == (cd + 4) % 6)
        {
            // On left side
            // First check if we can move to the end position of the line
            ParticleAlgorithm nbr2 = GetNeighborAt((cd + 5) % 6);
            if (nbr2 == null || (((LineFormationParticleSync)nbr2).state != LFState.LEADER && ((LineFormationParticleSync)nbr2).state != LFState.DONE))
            {
                // Position is empty or occupied by non-LEADER, non-DONE particle => try to move there
                moveDir.SetValue((cd + 5) % 6);
            }
            else
            {
                // Position is already part of the line, move forward
                moveDir.SetValue(cd);
            }
        }
        else if (nbr.localDir == (cd + 1) % 6 || nbr.localDir == (cd + 2) % 6)
        {
            // On right side
            // First check if we can move to the end position of the line
            ParticleAlgorithm nbr2 = GetNeighborAt((cd + 1) % 6);
            if (nbr2 == null || (((LineFormationParticleSync)nbr2).state != LFState.LEADER && ((LineFormationParticleSync)nbr2).state != LFState.DONE))
            {
                // Position is empty or occupied by non-LEADER, non-DONE particle => try to move there
                moveDir.SetValue((cd + 1) % 6);
            }
            else
            {
                // Position is already part of the line, move forward
                moveDir.SetValue(cd);
            }
        }
    }

    private void ComputeRootMoveDir()
    {
        if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.DONE || p.state == LFState.LEADER, out Neighbor<LineFormationParticleSync> nbr))
        {
            ComputeRootMoveDir(nbr);
        }
        else
        {
            // This should never occur
            Debug.LogError("ROOT particle does not have a DONE or LEADER neighbor!");
            moveDir.SetValue(-1);
        }
    }

    /// <summary>
    /// Assuming that we are expanded, check if we have received a
    /// beep on one of our tail edges. If we have, that means that
    /// we have sent that beep and we can now pull the particle at
    /// that edge.
    /// <para>
    /// Will perform the pull handover immediately.
    /// </para>
    /// </summary>
    /// <returns><c>true</c> if and only if we can perform the pull
    /// handover due to a beep on the corresponding edge.</returns>
    private bool PullIfSentBeep()
    {
        PinConfiguration pc = GetCurrentPinConfiguration();
        for (int direction = 0; direction < 6; direction++)
        {
            if (direction == HeadDirection())
            {
                continue;
            }
            if (pc.GetPinAt(direction, 0, false).PartitionSet.ReceivedBeep())
            {
                PullHandoverHead(direction);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Assuming that we are expanded, search for a contracted FLWR particle
    /// that is following our tail and send a beep in its direction if we
    /// find one. Sends the beep immediately if possible.
    /// <para>
    /// In the next round, <see cref="PullIfSentBeep"/> can be used to check
    /// if and where we have sent a beep and perform the corresponding pull
    /// handover.
    /// </para>
    /// </summary>
    /// <returns><c>true</c> if and only if we can send a beep to a contracted
    /// FLWR following our tail.</returns>
    private bool SendBeepForPull()
    {
        PinConfiguration pc = GetCurrentPinConfiguration();
        // TODO: There should be a helper method for something like this (maybe change FindFirstNbrWithProperty such that Neighbor<>s can be tested)
        for (int direction = 0; direction < 6; direction++)
        {
            if (direction == HeadDirection())
            {
                continue;
            }
            LineFormationParticleSync nbr = GetNeighborAt(direction, false) as LineFormationParticleSync;
            if (nbr != null && nbr.state == LFState.FLWR && nbr.IsContracted() && nbr.followDir == ((direction + 3) % 6))
            {
                // Send a beep to that neighbor
                // Current pin config is still singleton
                SetPlannedPinConfiguration(pc);
                pc.GetPinAt(direction, 0, false).PartitionSet.SendBeep();
                return true;
            }
        }
        return false;
    }
}
