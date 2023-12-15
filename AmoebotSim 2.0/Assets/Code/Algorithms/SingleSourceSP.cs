using System.Collections.Generic;
using AS2.Sim;
using AS2.Subroutines.ETT;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.SingleSourceSP
{

    /// <summary>
    /// Implements the (1, l)-SPF algorithm.
    /// <para>
    /// In the first phase, we run the Euler Tour Technique (ETT) on the
    /// implicit portal graphs for the 3 axes, using the source amoebot as
    /// root and the destination amoebots to define the weight function.
    /// After each iteration, each amoebot knows which of its adjacent portals
    /// is a parent portal. For each neighbor, we count in how many portal graphs
    /// this neighbor is on a parent portal.<br/>
    /// At the end of the first phase, each amoebot elects one neighbor that
    /// occurred as parent portal twice to be its parent in the shortest path tree.
    /// </para>
    /// <para>
    /// In the second phase, we run ETT again on the resulting forest to prune all
    /// subtrees without destinations and to identify and remove components that
    /// are not connected to the root.
    /// </para>
    /// </summary>
    // PORTAL PHASE
    // Round 0:
    //  - Setup portal circuits and let the source amoebot beep to identify the source portal
    //  - Find the amoebots with edges to the neighbor portals
    // Round 1:
    //  - Identify source portal and representative based on the received beep
    //  - Let the destination amoebots beep to identify the destination portals
    // Round 2:
    //  - Identify the destination portals and their representatives
    // Round 3:
    //  - Setup ETT circuit:
    //      - Find the incoming and outgoing edges
    //      - Root representative chooses a place to split the cycle
    //      - Dest representatives choose exactly one outgoing edge to mark
    //      - Send first beep
    // Round 4:
    //  - Keep running the ETT subroutine:
    //      - Receive last beep, setup new circuit and send new beep
    //  - As soon as the ETT subroutine is finished: Go to round 5
    // Round 5:
    //  - Setup simple portal circuits again
    //  - Amoebots with edges to other portals read the comparison results
    //  - For all portals except the root: Send beep if one of the edge differences is != 0
    //  - Root portal: Send beep if |Q| = 0
    // Round 6:
    //  - Receive portal beeps:
    //      - If we are not the root portal and we received no beep: We were pruned! (Does not mean anything yet)
    //      - If we are not the root portal and we received a beep: We were not pruned
    //      - If we are the root portal and we received a beep: |Q| = 0, so we can terminate (does not happen in this implementation)
    //  - Setup portal neighbor circuits (one circuit for each neighbor portal
    //      - Amoebots with neighbor edges send beep on that circuit if the comparison of OUT - IN was GREATER 0
    // Round 7:
    //  - Receive parent beeps
    //  - Increment neighbor counters for all parent portal neighbors
    //  - Reset all flags specific to this iteration and move on to the next iteration
    //      - After the third portal axis, move on to the next phase
    //
    // FINAL PRUNE PHASE
    // Round 0:
    //  - Choose one of the neighbors with counter 2 as parent
    //      - If there is no such neighbor, we have no parent (either we are the source or we will be pruned)
    //      - After this round, we can find our children by inspecting our neighbors' parent directions
    // Round 1:
    //  - Establish ETT circuit again, but using the actual tree this time
    //      - Amoebots that have neither a parent nor any children immediately terminate by pruning themselves
    //  - The root sends a beep on the circuit, this will be received by the component that contains the source
    // Round 2:
    //  - Amoebots that have not received any beep prune themselves already
    //      - They terminate and thereafter do not participate in the rest of the procedure
    //  - Root sends first actual ETT beep
    // Round 3:
    //  - Receive ETT beep and send
    //  - Keep running ETT until finished
    //  - Once finished, go to round 4
    // Round 4:
    //  - Read the comparison result of the parent edge (unless we are the source)
    //      - If OUT - IN is GREATER than 0, our subtree contains a destination, so we do nothing
    //      - Otherwise, we must prune ourselves
    //  - Finally, reset the pin configuration to singleton and terminate
    public class SingleSourceSPParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "1-SPF";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SingleSourceSPInitializer).FullName;

        // Colors
        static readonly Color sourceColor = ColorData.Particle_Red;
        static readonly Color destColor = ColorData.Particle_BlueDark;
        static readonly Color sourcePortalColor = ColorData.Particle_Orange;
        static readonly Color destPortalColor = ColorData.Particle_Blue;
        static readonly Color mixedPortalColor = ColorData.Particle_Purple;

        enum Phase
        {
            PORTAL_1, PORTAL_2, PORTAL_3,
            FINAL_PRUNE,
            FINISHED
        }

        [StatusInfo("Show Portal Graph")]
        public static void ShowPortalGraph(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            AS2.UI.LineDrawer ld = AS2.UI.LineDrawer.Instance;
            ld.Clear();

            // Find portal direction
            Direction pDir = Direction.NONE;
            SingleSourceSPParticle algo = (SingleSourceSPParticle)system.particles[0].algorithm;
            if (algo.phase == Phase.FINISHED || algo.phase == Phase.FINAL_PRUNE)
                return;
            pDir = algo.portalAxisDir;

            // Find bounding dimensions of the system
            int minX = 0, minY = 0, minXY = 0, maxX = 0, maxY = 0, maxXY = 0;
            foreach (Particle p in system.particles)
            {
                Vector2Int pos = p.Head();
                int x = pos.x;
                int y = pos.y;
                int xy = x + y;
                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
                if (y < minY)
                    minY = y;
                if (y > maxY)
                    maxY = y;
                if (xy < minXY)
                    minXY = xy;
                if (xy > maxXY)
                    maxXY = xy;
            }

            Color portalColor = new Color(0.6f, 0.6f, 0.6f);
            Color rootPortalColor = Color.red;
            Color destPortalColor = Color.blue;
            Color mixedPortalColor = Color.magenta;
            Color edgeColor = Color.cyan;

            // Determine iteration direction
            // Default is X axis
            int dim1Lower = minX, dim1Upper = maxX + 2;
            int dim2Lower = minY, dim2Upper = maxY + 1;
            Vector2Int increment = Vector2Int.right;
            // Y axis
            if (pDir == Direction.NNE || pDir == Direction.SSW)
            {
                dim1Lower = minY;
                dim1Upper = maxY + 2;
                dim2Lower = minX;
                dim2Upper = maxX + 1;
                increment = Vector2Int.up;
            }
            // Z axis
            else if (pDir == Direction.NNW || pDir == Direction.SSE)
            {
                dim1Lower = minY;
                dim1Upper = maxY + 2;
                dim2Lower = minXY;
                dim2Upper = maxXY + 1;
                increment = new Vector2Int(-1, 1);
            }

            // Scan the system portal by portal and draw the lines
            for (int dim2 = dim2Lower; dim2 < dim2Upper; dim2++)
            {
                Vector2 startPos = Vector2.zero;
                Vector2 endPos = Vector2.zero;
                bool startedPortal = false;
                bool isSourcePortal = false;
                bool isDestPortal = false;
                Vector2Int currentPos = new Vector2Int(dim1Lower, dim2);
                if (pDir == Direction.NNE || pDir == Direction.SSW)
                {
                    currentPos = new Vector2Int(dim2, dim1Lower);
                }
                else if (pDir == Direction.NNW || pDir == Direction.SSE)
                {
                    currentPos = new Vector2Int(dim2 - dim1Lower, dim1Lower);
                }
                for (int dim1 = dim1Lower; dim1 < dim1Upper; dim1++)
                {
                    if (system.TryGetParticleAt(currentPos, out AS2.Visuals.IParticleState ips))
                    {
                        // Draw line to next portal if this amoebot has a portal edge
                        algo = (SingleSourceSPParticle)((Particle)ips).algorithm;
                        if (algo.nbrPortalDir1 != Direction.NONE)
                        {
                            Vector2Int nbrPos = currentPos + ParticleSystem_Utils.DirectionToVector(algo.nbrPortalDir1);
                            ld.AddLine(currentPos, nbrPos, edgeColor, false, 1.5f);
                        }
                        if (algo.isSource)
                            isSourcePortal = true;
                        if (algo.isDest)
                            isDestPortal = true;

                        // If we are not drawing a portal yet, remember this as the start position
                        if (!startedPortal)
                        {
                            startPos = currentPos;
                            startedPortal = true;
                        }
                        // Also remember it as the last end position
                        endPos = currentPos;
                    }
                    else
                    {
                        // If a portal has just ended, draw it
                        if (startedPortal)
                        {
                            Color c = isSourcePortal && isDestPortal ? mixedPortalColor : (isSourcePortal ? rootPortalColor : (isDestPortal ? destPortalColor : portalColor));
                            ld.AddLine(startPos, endPos, c, false, 4, 1, -0.5f);
                            startedPortal = false;
                            isSourcePortal = false;
                            isDestPortal = false;
                        }
                    }
                    currentPos += increment;
                }
            }

            ld.SetTimer(20f);
        }

        [StatusInfo("Show SP Tree")]
        public static void ShowTree(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            AS2.UI.LineDrawer ld = AS2.UI.LineDrawer.Instance;
            ld.Clear();

            foreach (Particle p in system.particles)
            {
                SingleSourceSPParticle algo = (SingleSourceSPParticle)p.algorithm;
                Vector2Int pos = p.Head();
                Direction d = algo.parent;
                if (d == Direction.NONE)
                    continue;

                Vector2 parent = pos + (Vector2)ParticleSystem_Utils.DirectionToVector(d) * 0.9f;
                ld.AddLine(pos, parent, Color.cyan, true, 1.5f, 1.5f);
            }

            ld.SetTimer(20f);
        }

        // Declare attributes here
        ParticleAttribute<Phase> phase;         // Current phase
        ParticleAttribute<int> round;           // Round counter

        ParticleAttribute<bool> isSource;   // Source flag
        ParticleAttribute<bool> isDest;     // Destination flag

        ParticleAttribute<Direction> portalAxisDir;     // The current portal axis (should be E, NNE or NNW)
        ParticleAttribute<bool> onRootPortal;           // Flag for root/source portal
        ParticleAttribute<bool> onDestPortal;           // Flag for destination portal
        ParticleAttribute<bool> isRootRepr;             // Representative flag for root/source portal
        ParticleAttribute<bool> isDestRepr;             // Representative flag for destination portal
        ParticleAttribute<Direction> nbrPortalDir1;     // Direction of the first neighbor portal edge (portalDir + 60 or portalDir + 120 or NONE)
        ParticleAttribute<Direction> nbrPortalDir2;     // Direction of the second neighbor portal edge (portalDir - 60 or portalDir - 120 or NONE)

        ParticleAttribute<int>[] parentCounters = new ParticleAttribute<int>[6];    // Count the number of times a neighbor has occurred as a parent portal

        ParticleAttribute<Direction> parent;            // The direction of our chosen parent
        ParticleAttribute<bool> pruned;                 // Flag for pruned amoebots

        // ETT subroutine
        SubETT ett;

        public SingleSourceSPParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            phase = CreateAttributeEnum<Phase>("Phase", Phase.PORTAL_1);
            round = CreateAttributeInt("Round", 0);

            isSource = CreateAttributeBool("Is Source", false);
            isDest = CreateAttributeBool("Is Destination", false);

            portalAxisDir = CreateAttributeDirection("Portal Axis Dir", Direction.E);
            onRootPortal = CreateAttributeBool("Root Portal", false);
            onDestPortal = CreateAttributeBool("Dest Portal", false);
            isRootRepr = CreateAttributeBool("Root Representative", false);
            isDestRepr = CreateAttributeBool("Destination Representative", false);
            nbrPortalDir1 = CreateAttributeDirection("Nbr Portal Dir 1", Direction.NONE);
            nbrPortalDir2 = CreateAttributeDirection("Nbr Portal Dir 2", Direction.NONE);

            for (int i = 0; i < 6; i++)
            {
                parentCounters[i] = CreateAttributeInt("Parent Counter " + i, 0);
            }

            parent = CreateAttributeDirection("Parent", Direction.NONE);
            pruned = CreateAttributeBool("Pruned", false);

            ett = new SubETT(p);

            // Also, set the default initial color
            SetMainColor(Color.gray);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool isSource = false, bool isDest = false)
        {
            // This code is executed directly after the constructor
            this.isSource.SetValue(isSource);
            this.isDest.SetValue(isDest);

            if (this.isSource.GetCurrentValue())
                SetMainColor(sourceColor);
            else if (this.isDest.GetCurrentValue())
                SetMainColor(destColor);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return phase == Phase.FINISHED;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            if (phase == Phase.FINISHED)
                return;
            else if (phase == Phase.FINAL_PRUNE)
            {
                // Hide bonds to indicate parents during last phase

            }
            else
            {
                // Hide bonds to indicate portals during portal phase
                foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                {
                    if (d == portalAxisDir || d.Opposite() == portalAxisDir)
                        continue;
                    if (d != nbrPortalDir1 && d != nbrPortalDir2)
                        HideBond(d);
                }
            }
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            switch (phase.GetValue())
            {
                case Phase.PORTAL_1:
                case Phase.PORTAL_2:
                case Phase.PORTAL_3:
                    ActivatePortalPhase();
                    break;
                case Phase.FINAL_PRUNE:
                    ActivateFinalPrunePhase();
                    break;
            }
        }

        private void ActivatePortalPhase()
        {
            if (round == 0)
            {
                DeterminePortalNeighbors(portalAxisDir);
                SetupSimplePortalCircuit(portalAxisDir);
                if (isSource)
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    pc.SendBeepOnPartitionSet(0);
                }
            }
            else if (round == 1)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    // Received source portal beep
                    onRootPortal.SetValue(true);
                    // Become root/source representative if we have no predecessor
                    if (!HasNeighborAt(portalAxisDir.GetValue().Opposite()))
                        isRootRepr.SetValue(true);
                }
                SetPlannedPinConfiguration(pc);
                if (isDest)
                    pc.SendBeepOnPartitionSet(0);

                SetColor();
            }
            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    // Received dest portal beep
                    onDestPortal.SetValue(true);
                    // Become dest representative if we have no predecessor
                    if (!HasNeighborAt(portalAxisDir.GetValue().Opposite()))
                        isDestRepr.SetValue(true);
                }

                SetColor();
            }
            else if (round == 3)
            {
                // Setup ETT
                // First collect the neighbor directions (order is important)
                List<Direction> nbrDirs = new List<Direction>();
                if (HasNeighborAt(portalAxisDir))
                    nbrDirs.Add(portalAxisDir);
                if (nbrPortalDir1 != Direction.NONE)
                    nbrDirs.Add(nbrPortalDir1);
                if (HasNeighborAt(portalAxisDir.GetValue().Opposite()))
                    nbrDirs.Add(portalAxisDir.GetValue().Opposite());
                if (nbrPortalDir2 != Direction.NONE)
                    nbrDirs.Add(nbrPortalDir2);
                // Mark the first edge if we are a dest portal representative
                int markedIdx = isDestRepr.GetCurrentValue() ? 0 : -1;
                // Split if we are the source portal representative
                bool split = isRootRepr;
                ett.Init(nbrDirs.ToArray(), markedIdx, split);

                PinConfiguration pc = GetContractedPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);

                // Root representative sends a beep
                if (isRootRepr)
                    ett.ActivateSend();
            }
            else if (round == 4)
            {
                ett.ActivateReceive();

                if (ett.IsFinished())
                {
                    round.SetValue(5);
                    return;
                }

                PinConfiguration pc = GetCurrentPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                if (ett.IsTerminationRound())
                    ett.ActivateSend();
                else
                {
                    if (isRootRepr)
                        ett.ActivateSend();
                }
                return;
            }
            else if (round == 5)
            {
                SetupSimplePortalCircuit(portalAxisDir);
                bool send = false;
                // Root portal: Representative sends beep if |Q| = 0
                if (onRootPortal)
                {
                    if (isRootRepr && ett.GetSumComparisonResult() == Comparison.EQUAL)
                    {
                        send = true;
                    }
                }
                // Other portal: Send beep if a neighbor edge was not equal to 0,
                // i.e., our subtree contains a portal in Q
                else
                {
                    if (nbrPortalDir1 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir1) != Comparison.EQUAL)
                        send = true;
                    else if (nbrPortalDir2 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir2) != Comparison.EQUAL)
                        send = true;
                }
                if (send)
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    pc.SendBeepOnPartitionSet(0);
                }
            }
            else if (round == 6)
            {
                // No need to receive the beeps since the results are not used
                // Continue with finding parent portals
                SetupPortalNeighborCircuits(portalAxisDir);
                // Amoebots with portal neighbor difference > 0 send beep
                if (nbrPortalDir1 != Direction.NONE || nbrPortalDir2 != Direction.NONE)
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    if (nbrPortalDir1 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir1) == Comparison.GREATER)
                        pc.SendBeepOnPartitionSet(0);
                    if (nbrPortalDir2 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir2) == Comparison.GREATER)
                        pc.SendBeepOnPartitionSet(1);
                }
            }
            else if (round == 7)
            {
                // Receive parent beeps and update parent counters
                // There are only four parents that can be updated this round
                PinConfiguration pc = GetCurrentPinConfiguration();
                UpdateParentCounter(portalAxisDir.GetValue().Rotate60(1), pc, 0);
                UpdateParentCounter(portalAxisDir.GetValue().Rotate60(2), pc, 0);
                UpdateParentCounter(portalAxisDir.GetValue().Rotate60(-1), pc, 1);
                UpdateParentCounter(portalAxisDir.GetValue().Rotate60(-2), pc, 1);

                // Reset
                round.SetValue(0);
                // (Just reset these values to be sure)
                onRootPortal.SetValue(false);
                onDestPortal.SetValue(false);
                isRootRepr.SetValue(false);
                isDestRepr.SetValue(false);
                nbrPortalDir1.SetValue(Direction.NONE);
                nbrPortalDir2.SetValue(Direction.NONE);
                SetColor();

                // Determine next phase
                switch (phase.GetValue())
                {
                    case Phase.PORTAL_1:
                        phase.SetValue(Phase.PORTAL_2);
                        break;
                    case Phase.PORTAL_2:
                        phase.SetValue(Phase.PORTAL_3);
                        break;
                    case Phase.PORTAL_3:
                        phase.SetValue(Phase.FINAL_PRUNE);
                        break;
                }
                portalAxisDir.SetValue(portalAxisDir.GetCurrentValue().Rotate60(1));

                return;
            }

            round.SetValue(round + 1);
        }

        private void ActivateFinalPrunePhase()
        {
            if (round == 0)
            {
                // Choose one of the neighbors with parent counter 2 as parent
                for (int i = 0; i < 6; i++)
                {
                    if (parentCounters[i] == 2)
                    {
                        parent.SetValue(DirectionHelpers.Cardinal(i));
                        break;
                    }
                }
            }
            else if (round == 1)
            {
                // Initialize ETT
                List<Direction> edges = new List<Direction>();
                foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                {
                    if (parent == d || IsChild(d))
                        edges.Add(d);
                }

                PinConfiguration pc = GetContractedPinConfiguration();

                if (edges.Count == 0)
                {
                    // We don't have a single edge => Must be pruned
                    Prune();
                    SetPlannedPinConfiguration(pc);
                    return;
                }

                ett.Init(edges.ToArray(), isDest ? 0 : -1, isSource);
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                // Root sends beep on one of the circuits
                if (isSource)
                {
                    Direction d = ett.GetNeighborDirections()[0];
                    pc.SendBeepOnPartitionSet(d.ToInt() * 2);
                }
            }
            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                Direction d = ett.GetNeighborDirections()[0];
                if (!pc.ReceivedBeepOnPartitionSet(d.ToInt() * 2) && !pc.ReceivedBeepOnPartitionSet(d.ToInt() * 2 + 1))
                {
                    // Received no beep on either circuit: Prune
                    Prune();
                    pc.SetToSingleton();
                    SetPlannedPinConfiguration(pc);
                    return;
                }

                // Root sends first proper ETT beep
                if (isSource)
                {
                    SetPlannedPinConfiguration(pc);
                    ett.ActivateSend();
                }
            }
            else if (round == 3)
            {
                ett.ActivateReceive();

                if (ett.IsFinished())
                {
                    round.SetValue(4);
                    return;
                }

                PinConfiguration pc = GetCurrentPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                if (ett.IsTerminationRound())
                    ett.ActivateSend();
                else if (isSource)
                    ett.ActivateSend();
                return;
            }
            else if (round == 4)
            {
                // Read parent edge comparison result unless we are the source
                if (!isSource)
                {
                    if (ett.GetComparisonResult(parent) != Comparison.GREATER)
                    {
                        // We have no destination in our subtree, so we have to terminate
                        Prune();
                    }
                }
                SetColor();
                SetPlannedPinConfiguration(GetContractedPinConfiguration());
                phase.SetValue(Phase.FINISHED);
            }
            round.SetValue(round + 1);
        }

        /// <summary>
        /// Sets up a simple circuit connecting all pins along the given axis
        /// into partition set 0.
        /// </summary>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void SetupSimplePortalCircuit(Direction portalDir)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            int[] pinIds = new int[PinsPerEdge * 2];
            for (int i = 0; i < PinsPerEdge; i++)
            {
                pinIds[2 * i] = pc.GetPinAt(portalDir, i).Id;
                pinIds[2 * i + 1] = pc.GetPinAt(portalDir.Opposite(), i).Id;
            }
            pc.MakePartitionSet(pinIds, 0);
            SetPlannedPinConfiguration(pc);
        }

        private void SetupPortalNeighborCircuits(Direction portalDir)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            List<int> pinsTop = new List<int>();
            List<int> pinsBot = new List<int>();
            // Top right
            if (HasNeighborAt(portalDir.Rotate60(1)))
            {
                pinsTop.Add(pc.GetPinAt(portalDir, PinsPerEdge - 1).Id);
            }
            // Top left
            if (HasNeighborAt(portalDir.Rotate60(2)))
            {
                pinsTop.Add(pc.GetPinAt(portalDir.Opposite(), 0).Id);
            }

            // Bottom right
            if (HasNeighborAt(portalDir.Rotate60(-1)))
            {
                pinsBot.Add(pc.GetPinAt(portalDir, 0).Id);
            }
            // Bottom left
            if (HasNeighborAt(portalDir.Rotate60(-2)))
            {
                pinsBot.Add(pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id);
            }

            if (pinsTop.Count > 0)
            {
                pc.MakePartitionSet(pinsTop.ToArray(), 0);
                pc.SetPartitionSetPosition(0, new Vector2((portalDir.ToInt() + 1.5f) * 60f, 0.5f));
                pc.SetPartitionSetDrawHandle(0, true);
            }
            if (pinsBot.Count > 0)
            {
                pc.MakePartitionSet(pinsBot.ToArray(), 1);
                pc.SetPartitionSetPosition(1, new Vector2((portalDir.Opposite().ToInt() + 1.5f) * 60f, 0.5f));
                pc.SetPartitionSetDrawHandle(1, true);
            }
            SetPlannedPinConfiguration(pc);
        }

        private void DeterminePortalNeighbors(Direction portalDir)
        {
            // Reset neighbor directions
            nbrPortalDir1.SetValue(Direction.NONE);
            nbrPortalDir2.SetValue(Direction.NONE);

            // Check which neighbors exist
            Direction d60p = portalDir.Rotate60(1);
            Direction d120p = portalDir.Rotate60(2);
            Direction d60n = portalDir.Rotate60(-1);
            Direction d120n = portalDir.Rotate60(-2);
            bool nbr60p = HasNeighborAt(d60p);
            bool nbr120p = HasNeighborAt(d120p);
            bool nbr60n = HasNeighborAt(d60n);
            bool nbr120n = HasNeighborAt(d120n);
            bool nbrPortal = HasNeighborAt(portalDir.Opposite());

            if (nbrPortal)
            {
                // We have a predecessor in the portal, neighbor portal must be at 60 position
                if (!nbr120p && nbr60p)
                {
                    nbrPortalDir1.SetValue(d60p);
                }

                if (!nbr120n && nbr60n)
                {
                    nbrPortalDir2.SetValue(d60n);
                }
            }
            else
            {
                // We have no predecessor in the portal, neighbor portal may be at 120 position
                if (nbr120p)
                {
                    nbrPortalDir1.SetValue(d120p);
                }
                else if (nbr60p)
                {
                    nbrPortalDir1.SetValue(d60p);
                }

                if (nbr120n)
                {
                    nbrPortalDir2.SetValue(d120n);
                }
                else if (nbr60n)
                {
                    nbrPortalDir2.SetValue(d60n);
                }
            }
        }

        private void UpdateParentCounter(Direction d, PinConfiguration pc, int pSet)
        {
            if (HasNeighborAt(d) && pc.ReceivedBeepOnPartitionSet(pSet))
                parentCounters[d.ToInt()].SetValue(parentCounters[d.ToInt()].GetCurrentValue() + 1);
        }

        private bool IsChild(Direction d)
        {
            return HasNeighborAt(d) && ((SingleSourceSPParticle)GetNeighborAt(d)).parent == d.Opposite();
        }

        private void Prune()
        {
            pruned.SetValue(true);
            phase.SetValue(Phase.FINISHED);
            parent.SetValue(Direction.NONE);
            SetColor();
        }

        private void SetColor()
        {
            if (isSource.GetCurrentValue())
                SetMainColor(sourceColor);
            else if (isDest.GetCurrentValue())
                SetMainColor(destColor);
            else if (onRootPortal.GetCurrentValue() && onDestPortal.GetCurrentValue())
                SetMainColor(mixedPortalColor);
            else if (onRootPortal.GetCurrentValue())
                SetMainColor(sourcePortalColor);
            else if (onDestPortal.GetCurrentValue())
                SetMainColor(destPortalColor);
            else if (pruned.GetCurrentValue())
                SetMainColor(ColorData.Particle_Black);
            else
                SetMainColor(Color.gray);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SingleSourceSPInitializer : InitializationMethod
    {
        public SingleSourceSPInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 100, int numDestinations = 10, float holeProb = 0.05f)
        {
            if (numParticles < 1)
            {
                Log.Error("Number of particles must be at least 1");
                throw new SimulatorStateException("Invalid number of particles");
            }

            // Generate positions without holes
            List<Vector2Int> positions = GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb, true);
            foreach (Vector2Int v in positions)
                AddParticle(v);

            InitializationParticle ip;

            // Choose random source
            int sourceIdx = Random.Range(0, positions.Count);
            if (TryGetParticleAt(positions[sourceIdx], out ip))
                ip.SetAttribute("isSource", true);
            positions.RemoveAt(sourceIdx);

            // Choose random destinations
            numDestinations = Mathf.Min(numDestinations, positions.Count);
            for (int i = 0; i < numDestinations; i++)
            {
                int destIdx = Random.Range(0, positions.Count);
                if (TryGetParticleAt(positions[destIdx], out ip))
                    ip.SetAttribute("isDest", true);
                positions.RemoveAt(destIdx);
            }
        }
    }

} // namespace AS2.Algos.SingleSourceSP
