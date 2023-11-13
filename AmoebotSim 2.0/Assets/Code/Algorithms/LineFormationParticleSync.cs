using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.LineFormation
{

    public class MyMessage : Message
    {
        public enum Direction { LEFT, RIGHT }

        public Direction dir;

        public MyMessage()
        {
            dir = Direction.LEFT;
        }

        public MyMessage(Direction dir)
        {
            this.dir = dir;
        }

        public override Message Copy()
        {
            return new MyMessage(dir);
        }

        public override bool Equals(Message other)
        {
            if (this == other)
            {
                return true;
            }
            MyMessage otherMessage = other as MyMessage;
            return otherMessage != null && otherMessage.dir == dir;
        }

        public override bool GreaterThan(Message other)
        {
            if (other == null)
            {
                return true;
            }
            else if (Equals(other))
            {
                return false;
            }
            else
            {
                return dir == Direction.LEFT;
            }
        }
    }

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
        public enum LFState { IDLE, FLWR, ROOT, INLINE, LEADER, FINISHED }

        private static Color leaderColor = ColorData.Particle_Aqua;
        private static Color idleColor = ColorData.Particle_Black;
        private static Color rootColor = ColorData.Particle_Red;
        private static Color flwrColor = ColorData.Particle_Blue;
        private static Color inlineColor = ColorData.Particle_Yellow;
        private static Color finishedColor = ColorData.Particle_BlueDark;

        public ParticleAttribute<LFState> state;
        public ParticleAttribute<Direction> constructionDir;
        public ParticleAttribute<Direction> moveDir;
        public ParticleAttribute<Direction> followDir;

        // Helper to ensure that ROOTs only push when no handover is planned already
        public ParticleAttribute<bool> rootHandoverAvailable;

        // The direction into which we should send a beep in the next beep phase. Is set in a move phase
        // and read and reset in the following beep phase
        private ParticleAttribute<Direction> handoverBeepDirection;

        // Flag to indicate when the LEADER or an INLINE particle has decided that a ROOT may move
        private ParticleAttribute<bool> hasChosenRoot;

        // Flag to indicate when the LEADER or an INLINE particle has successfully determined its local part of the line to be complete
        // For INLINE particles, this only becomes true once they have received a beep from the LEADER
        private ParticleAttribute<bool> localLineComplete;

        // Flag to make LEADER send a beep every 2 rounds and recognize when the line is complete
        private ParticleAttribute<bool> beepInLastRound;

        [StatusInfo("Draw Spanning Tree", "Draws the entire spanning tree, i.e., the parent edges for all FLWR particles.")]
        public static void DrawSpanningTree(AS2.Sim.ParticleSystem system, Particle selected)
        {
            AS2.UI.CollisionLineDrawer ld = AS2.UI.CollisionLineDrawer.Instance;
            ld.Clear();

            // Draw parent edge for each follower particle
            foreach (Particle p in system.particles)
            {
                LineFormationParticleSync lfp = (LineFormationParticleSync)p.algorithm;
                if (lfp.state == LFState.FLWR)
                {
                    Vector2Int pos = p.Head();
                    Vector2 parent = pos + (Vector2)ParticleSystem_Utils.DirectionToVector(lfp.followDir) * 0.8f;
                    ld.AddLine(pos, parent, Color.blue, true, 1.5f, 1.5f);
                }
            }

            ld.SetTimer(20f);
        }

        [StatusInfo("Draw FLWR Path", "Draws the follower path from the currently selected FLWR particle to its ROOT parent.")]
        public static void DrawPath(AS2.Sim.ParticleSystem system, Particle selected)
        {
            AS2.UI.CollisionLineDrawer ld = AS2.UI.CollisionLineDrawer.Instance;
            ld.Clear();

            Particle p = selected;
            LineFormationParticleSync lfp = (LineFormationParticleSync)selected.algorithm;
            while (p is not null && lfp.state == LFState.FLWR)
            {
                // Get follow vector
                Vector2Int followVec = ParticleSystem_Utils.DirectionToVector(lfp.followDir);
                // Draw line to parent
                Vector2Int pos = p.Head();
                Vector2 parent = pos + (Vector2)followVec * 0.8f;
                ld.AddLine(pos, parent, Color.blue, true, 1.5f, 1.5f);

                // Get parent
                Vector2Int parentPos = pos + followVec;
                if (system.TryGetParticleAt(parentPos, out Visuals.IParticleState q))
                {
                    p = (Particle)q;
                    lfp = (LineFormationParticleSync)p.algorithm;
                }
                else
                    p = null;
            }

            ld.SetTimer(20f);
        }

        public LineFormationParticleSync(Particle p) : base(p)
        {
            constructionDir = CreateAttributeDirection("constructionDir", Direction.NONE);
            moveDir = CreateAttributeDirection("moveDir", Direction.NONE);
            followDir = CreateAttributeDirection("followDir", Direction.NONE);
            state = CreateAttributeEnum<LFState>("State", LFState.IDLE);

            rootHandoverAvailable = CreateAttributeBool("ROOT handover OK", true);
            handoverBeepDirection = CreateAttributeDirection("Handover dir", Direction.NONE);
            hasChosenRoot = CreateAttributeBool("Has chosen ROOT", false);
            localLineComplete = CreateAttributeBool("Local line complete", false);
            beepInLastRound = CreateAttributeBool("Beep in last round", false);

            SetMainColor(idleColor);
        }

        public void Init(bool leader = false)
        {
            // Exactly one particle will become the leader and choose a random construction direction
            if (leader)
            {
                state.SetValue(LFState.LEADER);
                constructionDir.SetValue(DirectionHelpers.Cardinal(Random.Range(0, 6)));
                SetMainColor(leaderColor);
            }
        }

        // Only need one pin per edge in this algorithm because communication
        // is very simple
        public override int PinsPerEdge => 1;

        public static new string Name => "Line Formation";

        public static new string GenerationMethod => typeof(LineFormationInitializer).FullName;

        public override bool IsFinished()
        {
            return state == LFState.FINISHED;
        }

        public override void ActivateMove()
        {
            // Use old movement system - we don't have to worry about bonds
            UseAutomaticBonds();

            switch ((LFState)state)
            {
                case LFState.FINISHED:
                case LFState.LEADER:
                case LFState.IDLE:
                case LFState.INLINE:
                    // LEADER, IDLEs, INLINEs and FINISHED do not move, don't do anything here
                    return;
                case LFState.ROOT:
                    RootMove();
                    break;
                case LFState.FLWR:
                    FlwrMove();
                    break;
                default: throw new System.InvalidOperationException("Undefined state " + state);
            }
        }

        public override void ActivateBeep()
        {
            switch ((LFState)state)
            {
                case LFState.FINISHED:
                    return;
                case LFState.LEADER:
                    LeaderActivate();
                    break;
                case LFState.IDLE:
                    IdleActivate();
                    break;
                case LFState.ROOT:
                    RootBeep();
                    break;
                case LFState.FLWR:
                    FlwrBeep();
                    break;
                case LFState.INLINE:
                    InlineActivate();
                    break;
                default: throw new System.InvalidOperationException("Undefined state " + state);
            }
        }

        private void LeaderActivate()
        {
            if (!localLineComplete)
            {
                if (!hasChosenRoot && SendBeepToWaitingRoot())
                {
                    hasChosenRoot.SetValue(true);
                    return;
                }
                if (CheckLocalCompleteness())
                {
                    localLineComplete.SetValue(true);
                }
            }
            else
            {
                // Local part of the line is complete
                // Send a beep every 2 rounds and finish if a beep is sent in between
                PinConfiguration pc = GetCurrentPinConfiguration();
                PartitionSet ps = pc.GetPinAt(constructionDir, 0).PartitionSet;
                if (!beepInLastRound)
                {
                    if (ps.ReceivedBeep())
                    {
                        // Received beep although we did not send it: Line is complete
                        state.SetValue(LFState.FINISHED);
                        SetMainColor(finishedColor);
                    }
                    else
                    {
                        SetPlannedPinConfiguration(pc);
                        ps.SendBeep();
                        beepInLastRound.SetValue(true);
                    }
                }
                else
                {
                    beepInLastRound.SetValue(false);
                }
            }
        }

        private void IdleActivate()
        {
            // Check if neighbor is LEADER or INLINE, if yes become INLINE or ROOT
            if (TryToBecomeRootOrInline() > 0)
            {
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

        private void RootMove()
        {
            Direction cd = constructionDir.GetCurrentValue();

            // ROOT handovers take precedence: Try performing handover with ROOT first
            if (IsContracted())
            {
                // Always compute the move direction when contracted
                // The result indicates whether we are about to enter the end position of the line
                int moveDirResult = ComputeRootMoveDir();
                Direction md = moveDir.GetCurrentValue();

                // If we are contracted and we can expand freely or push into an expanded ROOT: Do it
                // Contracted ROOTs can almost always expand
                LineFormationParticleSync nbr = GetNeighborAt(md) as LineFormationParticleSync;
                if (nbr == null)
                {
                    // Special case: We are almost at the end of the line
                    PinConfiguration pc = GetCurrentPinConfiguration();
                    if (moveDirResult == 1)
                    {
                        // We are on the left side, wait for beep from INLINE or LEADER particle
                        if (!pc.GetPinAt(md.Rotate60(-1), 0).PartitionSet.ReceivedBeep())
                        {
                            return;
                        }
                    }
                    else if (moveDirResult == 2)
                    {
                        // We are on the right side, wait for beep from INLINE or LEADER particle
                        if (!pc.GetPinAt(md.Rotate60(1), 0).PartitionSet.ReceivedBeep())
                        {
                            return;
                        }
                    }
                    if (moveDirResult == 1 || moveDirResult == 2)
                    {
                        MyMessage msg = (MyMessage)pc.GetPinAt(moveDirResult == 1 ? md.Rotate60(-1) : md.Rotate60(1), 0).PartitionSet.GetReceivedMessage();
                        Debug.Log("ALLOWED TO MOVE FROM " + msg.dir);
                    }

                    // No reason not to expand
                    Expand(md);
                }
                else if (nbr.state == LFState.ROOT && nbr.IsExpanded() && nbr.rootHandoverAvailable && IsTailAt(md))
                {
                    PushHandover(md);
                }
            }
            else
            {
                // If we have sent a beep to a FLWR neighbor in the last round: Perform pull handover
                if (PullIfSentBeep())
                {
                    // Also reset handover flag
                    rootHandoverAvailable.SetValue(true);
                    return;
                }

                // If there is a ROOT neighbor that we can pull: Do it
                // ROOT neighbors to pull can only be at our tail in direction
                // constructionDir + 3 or constructionDir + 4
                LineFormationParticleSync nbr = GetNeighborAt(cd.Opposite(), false) as LineFormationParticleSync;
                if (nbr != null && nbr.state == LFState.ROOT && nbr.IsContracted())
                {
                    PullHandoverHead(cd.Opposite());
                    return;
                }
                else
                {
                    nbr = GetNeighborAt(cd.Rotate60(4), false) as LineFormationParticleSync;
                    if (nbr != null && nbr.state == LFState.ROOT && nbr.IsContracted())
                    {
                        PullHandoverHead(cd.Rotate60(4));
                        return;
                    }
                }

                // ROOT handover did not work: Try pulling a FLWR instead (this can prevent a ROOT handover in the next round)
                if (SendBeepForPull())
                {
                    rootHandoverAvailable.SetValue(false);
                    return;
                }

                // No handover possible: Contract on our own if there is no blocking tail neighbor
                if (!HaveBlockingTailNeighbor())
                {
                    ContractHead();
                }
            }
        }

        private void RootBeep()
        {
            if (IsContracted())
            {
                // State change happens in beep phase
                if (TryToBecomeInline())
                {
                    return;
                }
            }
            else
            {
                // If we have scheduled a beep for a pull: Send the beep now
                if (handoverBeepDirection != Direction.NONE)
                {
                    PinConfiguration pc = GetCurrentPinConfiguration();
                    // Current pin config is still singleton
                    SetPlannedPinConfiguration(pc);
                    pc.GetPinAt(handoverBeepDirection, 0, false).PartitionSet.SendBeep();
                    // Reset the handover direction
                    handoverBeepDirection.SetValue(Direction.NONE);
                }
            }
        }

        private void RootActivate()
        {
            Direction cd = constructionDir.GetCurrentValue();

            // ROOT handovers take precedence: Try performing handover with ROOT first
            if (IsContracted())
            {
                // First thing to try: Become INLINE
                if (TryToBecomeInline())
                {
                    return;
                }

                // Always compute the move direction when contracted
                // The result indicates whether we are about to enter the end position of the line
                int moveDirResult = ComputeRootMoveDir();
                Direction md = moveDir.GetCurrentValue();

                // If we are contracted and we can expand freely or push into an expanded ROOT: Do it
                // Contracted ROOTs can almost always expand
                LineFormationParticleSync nbr = GetNeighborAt(md) as LineFormationParticleSync;
                if (nbr == null)
                {
                    // Special case: We are almost at the end of the line
                    PinConfiguration pc = GetCurrentPinConfiguration();
                    if (moveDirResult == 1)
                    {
                        // We are on the left side, wait for beep from INLINE or LEADER particle
                        if (!pc.GetPinAt(md.Rotate60(-1), 0).PartitionSet.ReceivedBeep())
                        {
                            return;
                        }
                    }
                    else if (moveDirResult == 2)
                    {
                        // We are on the right side, wait for beep from INLINE or LEADER particle
                        if (!pc.GetPinAt(md.Rotate60(1), 0).PartitionSet.ReceivedBeep())
                        {
                            return;
                        }
                    }
                    if (moveDirResult == 1 || moveDirResult == 2)
                    {
                        MyMessage msg = (MyMessage)pc.GetPinAt(moveDirResult == 1 ? md.Rotate60(-1) : md.Rotate60(1), 0).PartitionSet.GetReceivedMessage();
                        Debug.Log("ALLOWED TO MOVE FROM " + msg.dir);
                    }

                    // No reason not to expand
                    Expand(md);
                }
                else if (nbr.state == LFState.ROOT && nbr.IsExpanded() && nbr.rootHandoverAvailable && IsTailAt(md))
                {
                    PushHandover(md);
                }
            }
            else
            {
                // If we have sent a beep to a FLWR neighbor in the last round: Perform pull handover
                if (PullIfSentBeep())
                {
                    // Also reset handover flag
                    rootHandoverAvailable.SetValue(true);
                    return;
                }

                // If there is a ROOT neighbor that we can pull: Do it
                // ROOT neighbors to pull can only be at our tail in direction
                // constructionDir + 3 or constructionDir + 4
                LineFormationParticleSync nbr = GetNeighborAt(cd.Opposite(), false) as LineFormationParticleSync;
                if (nbr != null && nbr.state == LFState.ROOT && nbr.IsContracted())
                {
                    PullHandoverHead(cd.Opposite());
                    return;
                }
                else
                {
                    nbr = GetNeighborAt(cd.Rotate60(4), false) as LineFormationParticleSync;
                    if (nbr != null && nbr.state == LFState.ROOT && nbr.IsContracted())
                    {
                        PullHandoverHead(cd.Rotate60(4));
                        return;
                    }
                }

                // ROOT handover did not work: Try pulling a FLWR instead (this can prevent a ROOT handover in the next round)
                if (SendBeepForPull())
                {
                    rootHandoverAvailable.SetValue(false);
                    return;
                }

                // No handover possible: Contract on our own if there is no blocking tail neighbor
                if (!HaveBlockingTailNeighbor())
                {
                    ContractHead();
                }
            }
        }

        private void FlwrBeep()
        {
            if (IsContracted())
            {
                // State change happens in beep phase
                if (TryToBecomeRootOrInline() > 0)
                {
                    return;
                }
            }
            else
            {
                // If we have scheduled a beep for a pull: Send the beep now
                if (handoverBeepDirection != Direction.NONE)
                {
                    PinConfiguration pc = GetCurrentPinConfiguration();
                    // Current pin config is still singleton
                    SetPlannedPinConfiguration(pc);
                    pc.GetPinAt(handoverBeepDirection, 0, false).PartitionSet.SendBeep();
                    // Reset the handover direction
                    handoverBeepDirection.SetValue(Direction.NONE);
                }
            }
        }

        private void FlwrMove()
        {
            if (IsContracted())
            {
                // Contracted FLWR must wait for followed particle to send beep
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.GetPinAt(followDir, 0).PartitionSet.ReceivedBeep())
                {
                    PushHandover(followDir);
                    // Also update the follow direction
                    LineFormationParticleSync nbr = GetNeighborAt(followDir) as LineFormationParticleSync;
                    followDir.SetValue(nbr.HeadDirection());
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

        private void InlineActivate()
        {
            if (!localLineComplete && !hasChosenRoot && SendBeepToWaitingRoot())
            {
                hasChosenRoot.SetValue(true);
                return;
            }

            PinConfiguration pc = GetCurrentPinConfiguration();
            PartitionSet ps = pc.GetPinAt(constructionDir.GetValue().Opposite(), 0).PartitionSet;
            if (!localLineComplete)
            {
                if (CheckLocalCompleteness() && ps.ReceivedBeep())
                {
                    // Our local view of the line is complete and the completeness beep from
                    // the leader has reached us: We are locally done as well
                    localLineComplete.SetValue(true);
                    SetMainColor(ColorData.Particle_Purple);     // FOR DEBUGGING (should be visible by circuits alone later)
                                                                 // Connect the two pins in direction of the line
                    ps.AddPin(pc.GetPinAt(constructionDir, 0));
                    SetPlannedPinConfiguration(pc);
                    // LEADER has beeped in the last round, so the flag must be reset to false
                    beepInLastRound.SetValue(false);
                }
            }
            else
            {
                if (beepInLastRound)
                {
                    // LEADER has beeped in last round, we received it this round
                    beepInLastRound.SetValue(false);
                    // If we are at the end of the line: Send reply beep
                    if (!HasNeighborAt(constructionDir))
                    {
                        SetPlannedPinConfiguration(pc);
                        ps.SendBeep();
                    }
                }
                else
                {
                    // LEADER has not beeped in last round, will beep this round
                    beepInLastRound.SetValue(true);
                    // If we have received a beep: Came from the end of the line, so we can finish
                    if (ps.ReceivedBeep())
                    {
                        state.SetValue(LFState.FINISHED);
                        SetMainColor(finishedColor);
                    }
                }
            }
        }

        private bool HaveBlockingTailNeighbor()
        {
            // A neighbor is blocking if it is IDLE and adjacent to our tail or FLWR that is following our tail
            // TODO: Can simplify this using better neighbor discovery methods
            for (int d = 0; d < 6; d++)
            {
                Direction dir = DirectionHelpers.Cardinal(d);
                if (dir == HeadDirection())
                    continue;
                ParticleAlgorithm nbr = GetNeighborAt(dir, false);
                if (nbr != null && IsHeadAt(dir, false))
                {
                    LineFormationParticleSync lfp = (LineFormationParticleSync)nbr;
                    if (lfp.state == LFState.IDLE || lfp.state == LFState.FLWR && (lfp.followDir == dir.Opposite()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryToBecomeInline(Neighbor<LineFormationParticleSync> nbr)
        {
            Direction cd = nbr.neighbor.constructionDir;
            if (cd == Direction.NONE)
                return false;

            // Safe to always set constructionDir because we have common chirality and compass orientation
            constructionDir.SetValue(cd);
            if (constructionDir.GetCurrentValue() == nbr.localDir.Opposite())
            {
                state.SetValue(LFState.INLINE);
                SetMainColor(inlineColor);
                return true;
            }
            return false;
        }

        private bool TryToBecomeInline()
        {
            if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.INLINE || p.state == LFState.LEADER, out Neighbor<LineFormationParticleSync> nbr))
            {
                return TryToBecomeInline(nbr);
            }
            return false;
        }

        /// <summary>
        /// Searches for a neighbor in state INLINE or LEADER and goes to
        /// state INLINE if we are at the end of the line or state ROOT if
        /// we are not. If no such neighbor is found, the state does not
        /// change.
        /// </summary>
        /// <returns><c>2</c> if we are now INLINE, <c>1</c> if we are now
        /// a ROOT, <c>0</c> otherwise.</returns>
        private int TryToBecomeRootOrInline()
        {
            if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.INLINE || p.state == LFState.LEADER, out Neighbor<LineFormationParticleSync> nbrInline))
            {
                // Become INLINE if we are at the end of the line
                if (TryToBecomeInline(nbrInline))
                    return 2;

                // Otherwise become ROOT
                state.SetValue(LFState.ROOT);
                SetMainColor(rootColor);
                ComputeRootMoveDir(nbrInline);
                constructionDir.SetValue(nbrInline.neighbor.constructionDir);
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Computes the next movement direction of the contracted ROOT
        /// particle and determines whether it is about to enter the end
        /// position of the line.
        /// </summary>
        /// <param name="nbr">The INLINE or LEADER neighbor from which we
        /// can determine the movement direction.</param>
        /// <returns><c>1</c> if we are about to enter the end position of
        /// the line from the left side, <c>2</c> if we are about to enter
        /// from the right side, and <c>0</c> otherwise.</returns>
        private int ComputeRootMoveDir(Neighbor<LineFormationParticleSync> nbr)
        {
            // We already know constructionDir, set moveDir relative to neighbor position
            // On the other end of the line => Move around the left side
            Direction cd = constructionDir.GetCurrentValue();
            if (cd == nbr.localDir)
            {
                moveDir.SetValue(cd.Rotate60(1));
                return 0;
            }

            // Left or right side of the line => Move up the line
            if (nbr.localDir == cd.Rotate60(-1) || nbr.localDir == cd.Rotate60(-2))
            {
                // On left side
                // First check if we can move to the end position of the line
                ParticleAlgorithm nbr2 = GetNeighborAt(cd.Rotate60(-1));
                if (nbr2 == null || (((LineFormationParticleSync)nbr2).state != LFState.LEADER && ((LineFormationParticleSync)nbr2).state != LFState.INLINE))
                {
                    // Position is empty or occupied by non-LEADER, non-INLINE particle => try to move there
                    moveDir.SetValue(cd.Rotate60(-1));
                    return 1;
                }
                else
                {
                    // Position is already part of the line, move forward
                    moveDir.SetValue(cd);
                }
            }
            else if (nbr.localDir == cd.Rotate60(1) || nbr.localDir == cd.Rotate60(2))
            {
                // On right side
                // First check if we can move to the end position of the line
                ParticleAlgorithm nbr2 = GetNeighborAt(cd.Rotate60(1));
                if (nbr2 == null || (((LineFormationParticleSync)nbr2).state != LFState.LEADER && ((LineFormationParticleSync)nbr2).state != LFState.INLINE))
                {
                    // Position is empty or occupied by non-LEADER, non-INLINE particle => try to move there
                    moveDir.SetValue(cd.Rotate60(1));
                    return 2;
                }
                else
                {
                    // Position is already part of the line, move forward
                    moveDir.SetValue(cd);
                }
            }
            return 0;
        }

        private int ComputeRootMoveDir()
        {
            if (FindFirstNeighborWithProperty((LineFormationParticleSync p) => p.state == LFState.INLINE || p.state == LFState.LEADER, out Neighbor<LineFormationParticleSync> nbr))
            {
                return ComputeRootMoveDir(nbr);
            }
            else
            {
                // This should never occur
                Debug.LogError("ROOT particle does not have an INLINE or LEADER neighbor!");
                moveDir.SetValue(Direction.NONE);
                return -1;
            }
        }

        /// <summary>
        /// Assuming that we are expanded, check if we have received a
        /// beep on one of our tail edges incident to a contracted FLWR.
        /// If we have, that means that we have sent that beep and we can
        /// now pull the particle at that edge.
        /// <para>
        /// Will perform the pull handover immediately.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if we can perform the pull
        /// handover due to a beep on the corresponding edge.</returns>
        private bool PullIfSentBeep()
        {
            PinConfiguration pc = GetCurrentPinConfiguration();
            for (int d = 0; d < 6; d++)
            {
                Direction direction = DirectionHelpers.Cardinal(d);
                if (direction == HeadDirection())
                {
                    continue;
                }
                if (pc.GetPinAt(direction, 0, false).PartitionSet.ReceivedBeep())
                {
                    LineFormationParticleSync nbr = GetNeighborAt(direction, false) as LineFormationParticleSync;
                    // Should never be null
                    if (nbr == null)
                    {
                        Debug.LogError("Neighbor to which beep was sent does not exist anymore!");
                        continue;
                    }
                    // Only pull if the neighbor is still a FLWR
                    if (nbr.state == LFState.FLWR)
                    {
                        PullHandoverHead(direction);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Assuming that we are expanded, search for a contracted FLWR particle
        /// that is following our tail and send a beep in its direction if we
        /// find one. Schedules the beep using <see cref="handoverBeepDirection"/>
        /// so it can be sent in the next beep phase.
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
            // TODO: There should be a helper method for something like this (maybe change FindFirstNbrWithProperty such that Neighbor<>s can be tested)
            for (int d = 0; d < 6; d++)
            {
                Direction direction = DirectionHelpers.Cardinal(d);
                if (direction == HeadDirection())
                {
                    continue;
                }
                LineFormationParticleSync nbr = GetNeighborAt(direction, false) as LineFormationParticleSync;
                if (nbr != null && nbr.state == LFState.FLWR && nbr.IsContracted() && nbr.followDir == direction.Opposite())
                {
                    // Send a beep to that neighbor
                    handoverBeepDirection.SetValue(direction);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Procedure of LEADER and INLINE particles to decide which of
        /// the up to 2 waiting ROOT particles may move to the end
        /// position of the line. If this situation is detected and a
        /// decision can be made, a beep is sent to the chosen ROOT.
        /// </summary>
        /// <returns><c>true</c> if and only if a ROOT particle was
        /// chosen or no ROOT particle will ever be chosen because the
        /// next position in the line is already occupied by an INLINE
        /// particle.</returns>
        private bool SendBeepToWaitingRoot()
        {
            Direction cd = constructionDir.GetCurrentValue();

            // First ensure that position in construction direction is free
            LineFormationParticleSync nbr = GetNeighborAt(cd) as LineFormationParticleSync;
            if (nbr != null)
            {
                return nbr.state == LFState.INLINE;
            }
            // Now check the two candidate positions for waiting ROOTs
            PinConfiguration pc = GetCurrentPinConfiguration();
            foreach (Direction candidateDir in new Direction[] { cd.Rotate60(1), cd.Rotate60(-1) })
            {
                nbr = GetNeighborAt(candidateDir) as LineFormationParticleSync;
                if (nbr != null && nbr.state == LFState.ROOT && nbr.IsContracted())
                {
                    // Found a waiting ROOT: Send beep
                    SetPlannedPinConfiguration(pc);
                    pc.GetPinAt(candidateDir, 0).PartitionSet.SendBeep();
                    MyMessage msg = new MyMessage(candidateDir == cd.Rotate60(1) ? MyMessage.Direction.LEFT : MyMessage.Direction.RIGHT);
                    pc.GetPinAt(candidateDir, 0).PartitionSet.SendMessage(msg);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the line is locally complete.
        /// </summary>
        /// <returns><c>true</c> if and only if the only neighbors
        /// are LEADER or INLINE particles directly in or directly opposite
        /// of the construction direction.</returns>
        private bool CheckLocalCompleteness()
        {
            Direction cd = constructionDir.GetCurrentValue();
            for (int d = 0; d < 6; d++)
            {
                Direction direction = DirectionHelpers.Cardinal(d);
                LineFormationParticleSync nbr = GetNeighborAt(direction) as LineFormationParticleSync;
                if (direction == cd || direction == cd.Opposite())
                {
                    if (nbr != null && nbr.state != LFState.LEADER && nbr.state != LFState.INLINE)
                    {
                        return false;
                    }
                }
                else if (nbr != null)
                {
                    return false;
                }
            }
            return true;
        }
    }


    // Initialization method
    public class LineFormationInitializer : InitializationMethod
    {
        public LineFormationInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        public void Generate(int numParticles = 50, float holeProb = 0.3f)
        {
            GenerateRandomWithHoles(numParticles, holeProb, Initialization.Chirality.CounterClockwise, Initialization.Compass.E);

            // Select a leader
            InitializationParticle[] particles = GetParticles();
            if (particles.Length > 0)
            {
                particles[Random.Range(0, particles.Length)].SetAttribute("leader", true);
            }
        }
    }

} // namespace AS2.Algos.LineFormation
