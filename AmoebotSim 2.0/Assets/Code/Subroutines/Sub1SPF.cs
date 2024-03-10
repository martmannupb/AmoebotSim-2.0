using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.ETT;

namespace AS2.Subroutines.SingleSourceSP
{

    /// <summary>
    /// Subroutine implementation of the single-source shortest path algorithm
    /// (<see cref="AS2.Algos.SingleSourceSP.SingleSourceSPParticle"/>).
    /// <para>
    /// <b>Usage:</b>
    /// <list type="bullet">
    /// <item>
    ///     Initialize by calling <see cref="Init(bool, bool, bool, List{Direction})"/>.
    ///     There must be exactly one source in each connected set of amoebots running this subroutine.
    /// </item>
    /// <item>
    ///     Run <see cref="SetupPC(PinConfiguration)"/>, then <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/>
    ///     and <see cref="ActivateSend"/> to start the procedure.
    /// </item>
    /// <item>
    ///     In the round immediately following a <see cref="ActivateSend"/> call, <see cref="ActivateReceive"/>
    ///     must be called. There can be an arbitrary break until the next pin configuration setup and
    ///     <see cref="ActivateSend"/> call. Continue this until the procedure is finished.
    /// </item>
    /// <item>
    ///     You can call <see cref="IsFinished"/> immediately after <see cref="ActivateReceive"/> to check
    ///     whether the procedure is finished. If it is, you can find each amoebot's parent direction in the
    ///     resulting shortest path tree using <see cref="Parent"/>. Some amoebots may not be part of a
    ///     shortest path to a destination, so they will have no parent.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>

    // Notes:
    //  - Send both source and destination beeps in the first round
    //  - Use counter for the 3 portal directions instead of phases (don't use phases at all)

    // Init:
    //  - Set portal direction counter to 0
    //  - Add non-existent neighbors to ignored neighbors (to avoid checking later)
    //  - If we have no neighbors: Terminate immediately

    // Round 0:
    //  Send:
    //  - Setup 2 portal circuits for the current portal direction and let the source/destination amoebots beep on the first/second circuit
    //  - Also identify amoebots with edges to neighbor portals
    //  - Additionally: Setup a global circuit and let destinations beep
    //  Receive:
    //  - If there was no beep on the global circuit: Terminate
    //  - Identify the portal types based on the received beeps and find their representatives
    //  - Setup ETT subroutine:
    //      - Determine which edges are incoming and outgoing
    //      - Root portal representative chooses a place to split the cycle
    //      - Dest portal representatives choose one outgoing edge to mark

    // Round 1:
    //  Send:
    //  - Send ETT beep
    //  Receive:
    //  - Receive ETT beep
    //  - If finished:
    //      - Go to round 2

    // Round 2:
    //  Send:
    //  - Setup portal neighbor circuits (one for each portal neighbor)
    //  - On non-source portals: Representatives beep on the neighbor circuit where OUT - IN is GREATER 0
    //      - No beep if no such side exists
    //  Receive:
    //  - On non-source portals:
    //      - If a beep was received for a neighbor portal: Increment the parent counters for the neighboring amoebots
    //  - If the direction counter is already 2:
    //      - Go to round 3
    //  - Else:
    //      - Increment direction counter
    //      - Reset all portal information
    //      - Go back to round 0

    // Round 3:
    //  Send:
    //  - Select one of the neighbors with parent counter 2 as parent
    //  - Send beep to that neighbor in singleton pin configuration
    //  Receive:
    //  - Receive beeps from children
    //  - Amoebots without parent or children prune themselves and go to round 7
    //  - Initialize ETT again, but using the parent and children instead of portals

    // Round 4:
    //  Send:
    //  - Setup ETT circuit
    //  - Source amoebot sends beep
    //  Receive:
    //  - Amoebots that have not received any beep prune themselves and go to round 7
    //  - Others go to round 5

    // Round 5:
    //  Send:
    //  - Setup ETT circuit and send beep
    //  Receive:
    //  - Receive ETT beep
    //  - If finished:
    //      - If we are not the root: Check comparison result of OUT - IN for the parent edge
    //          - If the result is not GREATER 0: Prune and go to round 7
    //      - Go to round 6

    // Round 6:
    //  Send:
    //  - Setup global circuit and beep, go to round 7

    // Round 7:
    //  Send:
    //  - Setup global circuit
    //  Receive:
    //  - If beep on global circuit:
    //      - Terminate

    public class Sub1SPF : Subroutine
    {
        // Colors
        static readonly Color sourceColor = ColorData.Particle_Red;
        static readonly Color destColor = ColorData.Particle_BlueDark;
        static readonly Color sourcePortalColor = ColorData.Particle_Orange;
        static readonly Color destPortalColor = ColorData.Particle_Blue;
        static readonly Color mixedPortalColor = ColorData.Particle_Purple;

        // 31            30      29 27          26 24          2322 2120 1918 1716 1514 1312  11   6               54            3  0
        // x             x        xxx            xxx            xx   xx   xx   xx   xx   xx   xxxxxx               xx            xxxx
        // Destination   Source   Nbr Portal 2   Nbr Portal 1   Parent counters               Ignored directions   Dir counter   Round
        ParticleAttribute<int> state1;

        //                                 7               6          5        4             3               2 0
        // xxxx xxxx xxxx xxxx xxxx xxxx   x               x          x        x             x               xxx
        //                                 Control color   Finished   Pruned   Dest portal   Source portal   Parent dir
        ParticleAttribute<int> state2;

        BinAttributeInt round;              // Round counter
        BinAttributeInt dirCounter;         // Portal direction counter
        BinAttributeBitField ignoreDirs;    // Which directions to ignore (6 flags)
        BinAttributeInt[] parentCounters;   // One parent counter for each direction
        BinAttributeDirection nbrPortal1;   // Direction of the "upper" portal neighbor
        BinAttributeDirection nbrPortal2;   // Direction of the "lower" portal neighbor
        BinAttributeBool isSource;          // Source flag
        BinAttributeBool isDest;            // Destination flag

        BinAttributeDirection parentDir;    // Parent direction resulting from this procedure
        BinAttributeBool onSourcePortal;    // Source portal flag
        BinAttributeBool onDestPortal;      // Destination portal flag
        BinAttributeBool pruned;            // Whether we were pruned
        BinAttributeBool finished;          // Whether the procedure has finished
        BinAttributeBool controlColor;      // Whether we should control the amoebot color

        SubETT ett;

        public Sub1SPF(Particle p) : base(p)
        {
            state1 = algo.CreateAttributeInt(FindValidAttributeName("[SPF] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[SPF] State 2"), 0);

            round = new BinAttributeInt(state1, 0, 4);
            dirCounter = new BinAttributeInt(state1, 4, 2);
            ignoreDirs = new BinAttributeBitField(state1, 6, 6);
            parentCounters = new BinAttributeInt[6];
            for (int i = 0; i < 6; i++)
                parentCounters[i] = new BinAttributeInt(state1, 12 + 2 * i, 2);
            nbrPortal1 = new BinAttributeDirection(state1, 24);
            nbrPortal2 = new BinAttributeDirection(state1, 27);
            isSource = new BinAttributeBool(state1, 30);
            isDest = new BinAttributeBool(state1, 31);

            parentDir = new BinAttributeDirection(state2, 0);
            onSourcePortal = new BinAttributeBool(state2, 3);
            onDestPortal = new BinAttributeBool(state2, 4);
            pruned = new BinAttributeBool(state2, 5);
            finished = new BinAttributeBool(state2, 6);
            controlColor = new BinAttributeBool(state2, 7);

            ett = new SubETT(p);
        }

        public void Init(bool isSource, bool isDest, bool controlColor = false, List<Direction> ignoreDirections = null)
        {
            state1.SetValue(0);
            state2.SetValue(0);
            this.isSource.SetValue(isSource);
            this.isDest.SetValue(isDest);
            this.onSourcePortal.SetValue(isSource);
            this.onDestPortal.SetValue(isDest);
            this.controlColor.SetValue(controlColor);
            if (!(ignoreDirections is null))
            {
                foreach (Direction d in ignoreDirections)
                    this.ignoreDirs.SetValue(d.ToInt(), true);
            }
            // Also ignore non-existent neighbors
            foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
            {
                if (!algo.HasNeighborAt(d))
                    this.ignoreDirs.SetValue(d.ToInt(), true);
            }
            // Terminate immediately if we have no neighbors at all
            bool haveNbr = false;
            for (int i = 0; i < 6; i++)
            {
                if (!this.ignoreDirs.GetCurrentValue(i))
                {
                    haveNbr = true;
                    break;
                }
            }
            if (!haveNbr)
                finished.SetValue(true);
        }

        /// <summary>
        /// The first half of the subroutine activation. Must be called
        /// in the round immediately after <see cref="ActivateSend"/>
        /// was called.
        /// </summary>

        public void ActivateReceive()
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(2))
                        {
                            // No beep on global circuit means there are no destinations
                            finished.SetValue(true);
                            pruned.SetValue(!isSource.GetCurrentValue());
                            break;
                        }

                        // Identify portal types based on received beeps
                        // (only have to check if we have portal neighbors)
                        Direction dir = DirectionHelpers.Cardinal(dirCounter.GetCurrentValue());
                        if (!ignoreDirs.GetCurrentValue(dir.ToInt()) || !ignoreDirs.GetCurrentValue(dir.Opposite().ToInt()))
                        {
                            if (pc.ReceivedBeepOnPartitionSet(0))
                                onSourcePortal.SetValue(true);
                            if (pc.ReceivedBeepOnPartitionSet(1))
                                onDestPortal.SetValue(true);
                        }

                        // Setup ETT
                        // First collect the neighbor directions (order is important)
                        List<Direction> nbrDirs = new List<Direction>();
                        if (!ignoreDirs.GetCurrentValue(dir.ToInt()))
                            nbrDirs.Add(dir);
                        if (nbrPortal1.GetCurrentValue() != Direction.NONE)
                            nbrDirs.Add(nbrPortal1.GetCurrentValue());
                        if (!ignoreDirs.GetCurrentValue(dir.Opposite().ToInt()))
                            nbrDirs.Add(dir.Opposite());
                        if (nbrPortal2.GetCurrentValue() != Direction.NONE)
                            nbrDirs.Add(nbrPortal2.GetCurrentValue());
                        // Mark the first edge if we are a dest portal representative
                        int markedIdx = onDestPortal.GetCurrentValue() && ignoreDirs.GetCurrentValue(dir.Opposite().ToInt()) ? 0 : -1;
                        // Split if we are the source portal representative
                        bool split = onSourcePortal.GetCurrentValue() && ignoreDirs.GetCurrentValue(dir.Opposite().ToInt());
                        ett.Init(nbrDirs.ToArray(), markedIdx, split);

                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                    {
                        ett.ActivateReceive();
                        if (ett.IsFinished())
                        {
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 2:
                    {
                        // If a beep was received on a neighbor portal circuit: Increment parent counters
                        int ctr = dirCounter.GetCurrentValue();
                        if (!onSourcePortal.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction dir = DirectionHelpers.Cardinal(ctr);
                            UpdateParentCounter(dir.Rotate60(1), pc, 0);
                            UpdateParentCounter(dir.Rotate60(2), pc, 0);
                            UpdateParentCounter(dir.Rotate60(-1), pc, 1);
                            UpdateParentCounter(dir.Rotate60(-2), pc, 1);
                        }

                        // Reset values specific to this direction
                        onSourcePortal.SetValue(isSource.GetCurrentValue());
                        onDestPortal.SetValue(isDest.GetCurrentValue());
                        nbrPortal1.SetValue(Direction.NONE);
                        nbrPortal2.SetValue(Direction.NONE);

                        if (ctr == 2)
                        {
                            // Finished all 3 directions, go to next phase
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Increment counter and repeat
                            dirCounter.SetValue(ctr + 1);
                            round.SetValue(0);
                        }
                    }
                    break;
                case 3:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Receive child beeps
                        bool[] children = new bool[6];
                        bool hasChild = false;
                        for (int i = 0; i < 6; i++)
                        {
                            Direction d = DirectionHelpers.Cardinal(i);
                            if (!ignoreDirs.GetCurrentValue(i) && pc.GetPinAt(d, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                            {
                                children[i] = true;
                                hasChild = true;
                            }
                        }
                        
                        // Amoebots without parent or children prune themselves and go to waiting round
                        if (parentDir.GetCurrentValue() == Direction.NONE && !hasChild)
                        {
                            pruned.SetValue(true);
                            round.SetValue(7);
                            break;
                        }

                        // Initialize ETT again, using parent and child directions instead of portals
                        List<Direction> edges = new List<Direction>();
                        foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                        {
                            if (parentDir.GetCurrentValue() == d || children[d.ToInt()])
                                edges.Add(d);
                        }

                        ett.Init(edges.ToArray(), isDest.GetCurrentValue() ? 0 : -1, isSource.GetCurrentValue());
                        round.SetValue(r + 1);
                    }
                    break;
                case 4:
                    {
                        if (pruned.GetCurrentValue())
                            break;

                        // Amoebots that have not received any beep prune themselves and go to the waiting round
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        Direction d = ett.GetNeighborDirections()[0];
                        if (!pc.ReceivedBeepOnPartitionSet(d.ToInt() * 2) && !pc.ReceivedBeepOnPartitionSet(d.ToInt() * 2 + 1))
                        {
                            // Received no beep on either circuit: Prune
                            pruned.SetValue(true);
                            round.SetValue(7);
                        }
                        else
                        {
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 5:
                    {
                        if (pruned.GetCurrentValue())
                            break;

                        ett.ActivateReceive();
                        if (ett.IsFinished())
                        {
                            // Non-source amoebots check comparison result of OUT - IN in parent direction
                            // If result is not GREATER 0: We are not part of a tree with destinations, so we are pruned
                            if (!isSource.GetCurrentValue())
                            {
                                if (ett.GetComparisonResult(parentDir.GetCurrentValue()) != Comparison.GREATER)
                                {
                                    pruned.SetValue(true);
                                    round.SetValue(7);
                                    break;
                                }
                            }
                            round.SetValue(6);
                        }
                    }
                    break;
                case 7:
                    {
                        // Terminate as soon as a beep is received on the global circuit
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            finished.SetValue(true);
                        }
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Sets up the pin configuration required for the
        /// <see cref="ActivateSend"/> call. The pin configuration
        /// is not planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>

        public void SetupPC(PinConfiguration pc)
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        Direction dir = DirectionHelpers.Cardinal(dirCounter.GetCurrentValue());
                        SetupSimplePortalCircuit(pc, dir, 0, 0);
                        SetupSimplePortalCircuit(pc, dir, algo.PinsPerEdge - 1, 1);
                        SetupGlobalCircuit(pc, 1, 2);
                    }
                    break;
                case 1:
                case 4:
                case 5:
                    {
                        if (!pruned.GetCurrentValue())
                            ett.SetupPinConfig(pc);
                    }
                    break;
                case 2:
                    {
                        SetupPortalNeighborCircuits(pc, DirectionHelpers.Cardinal(dirCounter.GetCurrentValue()));
                    }
                    break;
                case 3:
                    {
                        pc.SetToSingleton();
                    }
                    break;
                case 6:
                case 7:
                    {
                        SetupGlobalCircuit(pc, 0, 0);
                    }
                    break;
            }
        }

        /// <summary>
        /// The second half of the subroutine activation. Before this
        /// can be called, the pin configuration set up by
        /// <see cref="SetupPC(PinConfiguration)"/> must be planned.
        /// </summary>

        public void ActivateSend()
        {
            int r = round.GetCurrentValue();
            switch (r)
            {
                case 0:
                    {
                        // Set the portal neighbor flags
                        Direction dir = DirectionHelpers.Cardinal(dirCounter.GetCurrentValue());
                        DeterminePortalNeighbors(dir);

                        // Send classification beeps
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        bool havePortalCircuit = !ignoreDirs.GetCurrentValue(dir.ToInt()) || !ignoreDirs.GetCurrentValue(dir.Opposite().ToInt());
                        // Source beeps on first portal circuit
                        if (isSource.GetCurrentValue() && havePortalCircuit)
                            pc.SendBeepOnPartitionSet(0);
                        // Destinations beep on second portal and global circuit
                        if (isDest.GetCurrentValue())
                        {
                            if (havePortalCircuit)
                                pc.SendBeepOnPartitionSet(1);
                            pc.SendBeepOnPartitionSet(2);
                        }
                    }
                    break;
                case 1:
                case 5:
                    {
                        if (!pruned.GetCurrentValue())
                            ett.ActivateSend();
                    }
                    break;
                case 2:
                    {
                        // Representatives of non-source portals beep on the neighbor circuits where OUT - IN is GREATER 0
                        if (!onSourcePortal.GetCurrentValue())
                        {
                            Direction nbr1 = nbrPortal1.GetCurrentValue();
                            Direction nbr2 = nbrPortal2.GetCurrentValue();
                            if (nbr1 != Direction.NONE || nbr2 != Direction.NONE)
                            {
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                if (nbr1 != Direction.NONE && ett.GetComparisonResult(nbr1) == Comparison.GREATER)
                                    pc.SendBeepOnPartitionSet(0);
                                if (nbr2 != Direction.NONE && ett.GetComparisonResult(nbr2) == Comparison.GREATER)
                                    pc.SendBeepOnPartitionSet(1);
                            }
                        }
                    }
                    break;
                case 3:
                    {
                        // Select a neighbor with parent counter 3 as parent and send beep
                        if (!isSource.GetCurrentValue())
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                if (parentCounters[i].GetCurrentValue() >= 2)
                                {
                                    parentDir.SetValue(DirectionHelpers.Cardinal(i));
                                    break;
                                }
                            }
                            if (parentDir.GetCurrentValue() != Direction.NONE)
                            {
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                pc.GetPinAt(parentDir.GetCurrentValue(), 0).PartitionSet.SendBeep();
                            }
                        }
                    }
                    break;
                case 4:
                    {
                        // Source amoebot sends beep on ETT circuit
                        if (isSource.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            Direction d = ett.GetNeighborDirections()[0];
                            pc.SendBeepOnPartitionSet(d.ToInt() * 2);
                        }
                    }
                    break;
                case 6:
                    {
                        // Just send a beep on the global circuit
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        pc.SendBeepOnPartitionSet(0);
                        round.SetValue(r + 1);
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Checks whether the SPF procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the shortest paths
        /// to the source have been found.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Gets the parent direction of this amoebot after the procedure
        /// is finished.
        /// </summary>
        /// <returns>The direction of this amoebot in the shortest path
        /// forest. <see cref="Direction.NONE"/> if the amoebot has no parent
        /// (this is the case if it is the source/root or has been pruned).</returns>
        public Direction Parent()
        {
            return IsFinished() && !pruned.GetCurrentValue() ? parentDir.GetCurrentValue() : Direction.NONE;
        }

        private void SetColor()
        {
            if (!controlColor.GetCurrentValue())
                return;

            if (isSource.GetCurrentValue())
                algo.SetMainColor(sourceColor);
            else if (isDest.GetCurrentValue())
                algo.SetMainColor(destColor);
            else if (onSourcePortal.GetCurrentValue() && onDestPortal.GetCurrentValue())
                algo.SetMainColor(mixedPortalColor);
            else if (onSourcePortal.GetCurrentValue())
                algo.SetMainColor(sourcePortalColor);
            else if (onDestPortal.GetCurrentValue())
                algo.SetMainColor(destPortalColor);
            else if (pruned.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Black);
            else
                algo.SetMainColor(Color.gray);
        }

        /// <summary>
        /// Sets the neighbor portal flags of this amoebot. Direction 1
        /// indicates the "top" neighbor edge, direction 2 the "bottom"
        /// neighbor edge. The direction will be unequal to
        /// <see cref="Direction.NONE"/> if and only if this amoebot
        /// is the unique amoebot with the edge to that neighboring portal.
        /// </summary>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void DeterminePortalNeighbors(Direction portalDir)
        {
            // Reset neighbor directions
            nbrPortal1.SetValue(Direction.NONE);
            nbrPortal2.SetValue(Direction.NONE);

            // Check which neighbors exist
            Direction d60p = portalDir.Rotate60(1);
            Direction d120p = portalDir.Rotate60(2);
            Direction d60n = portalDir.Rotate60(-1);
            Direction d120n = portalDir.Rotate60(-2);
            bool nbr60p = !ignoreDirs.GetCurrentValue(d60p.ToInt());
            bool nbr120p = !ignoreDirs.GetCurrentValue(d120p.ToInt());
            bool nbr60n = !ignoreDirs.GetCurrentValue(d60n.ToInt());
            bool nbr120n = !ignoreDirs.GetCurrentValue(d120n.ToInt());
            bool nbrPortal = !ignoreDirs.GetCurrentValue(portalDir.Opposite().ToInt());

            if (nbrPortal)
            {
                // We have a predecessor in the portal, neighbor portal must be at 60 position
                if (!nbr120p && nbr60p)
                {
                    nbrPortal1.SetValue(d60p);
                }

                if (!nbr120n && nbr60n)
                {
                    nbrPortal2.SetValue(d60n);
                }
            }
            else
            {
                // We have no predecessor in the portal, neighbor portal may be at 120 position
                if (nbr120p)
                {
                    nbrPortal1.SetValue(d120p);
                }
                else if (nbr60p)
                {
                    nbrPortal1.SetValue(d60p);
                }

                if (nbr120n)
                {
                    nbrPortal2.SetValue(d120n);
                }
                else if (nbr60n)
                {
                    nbrPortal2.SetValue(d60n);
                }
            }
        }

        /// <summary>
        /// Increments the parent counter of the parent in
        /// direction <paramref name="d"/> in case a beep
        /// was received on the partition set with ID
        /// <paramref name="pSet"/> and there is a neighbor
        /// in that direction.
        /// </summary>
        /// <param name="dir">The direction of the neighbor to update.</param>
        /// <param name="pc">The pin configuration that may have received the beep.</param>
        /// <param name="pSet">The partition set to check.</param>
        private void UpdateParentCounter(Direction dir, PinConfiguration pc, int pSet)
        {
            int d = dir.ToInt();
            if (!ignoreDirs.GetCurrentValue(d) && pc.ReceivedBeepOnPartitionSet(pSet))
                parentCounters[d].SetValue(parentCounters[d].GetCurrentValue() + 1);
        }

        /// <summary>
        /// Sets up a simple circuit connecting all amoebots along the given
        /// portal direction, using the given pin offset in that direction.
        /// No partition set will be created if there are no neighbors on the
        /// portal axis.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="portalDir">The direction of the portal axis.</param>
        /// <param name="pinOffset">The pin offset in portal direction.</param>
        /// <param name="partitionSet">The partition set ID to use.</param>
        private void SetupSimplePortalCircuit(PinConfiguration pc, Direction portalDir, int pinOffset, int partitionSet = 0)
        {
            List<int> pins = new List<int>();
            if (!ignoreDirs.GetCurrentValue(portalDir.ToInt()))
                pins.Add(pc.GetPinAt(portalDir, pinOffset).Id);
            if (!ignoreDirs.GetCurrentValue(portalDir.Opposite().ToInt()))
                pins.Add(pc.GetPinAt(portalDir.Opposite(), algo.PinsPerEdge - 1 - pinOffset).Id);
            if (pins.Count > 0)
                pc.MakePartitionSet(pins.ToArray(), partitionSet);
        }

        /// <summary>
        /// Sets up a global circuit using the given pin offset.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="offset">The pin offset.</param>
        /// <param name="pSet">The partition set ID.</param>
        private void SetupGlobalCircuit(PinConfiguration pc, int offset, int pSet)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(offset, inverted, pSet);
            pc.SetPartitionSetColor(pSet, ColorData.Circuit_Colors[offset]);
        }


        /// <summary>
        /// Sets up one circuit for each neighbor of a portal along
        /// the given axis. The "top" partition set will have ID 0
        /// and the "bottom" partition set will have ID 1. If there
        /// is no neighbor in one of the directions, no partition set
        /// will be created for that direction.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void SetupPortalNeighborCircuits(PinConfiguration pc, Direction portalDir)
        {
            List<int> pinsTop = new List<int>();
            List<int> pinsBot = new List<int>();
            // Top right
            if (!ignoreDirs.GetCurrentValue((portalDir.ToInt() + 1) % 6))
            {
                pinsTop.Add(pc.GetPinAt(portalDir, algo.PinsPerEdge - 1).Id);
            }
            // Top left
            if (!ignoreDirs.GetCurrentValue((portalDir.ToInt() + 2) % 6))
            {
                pinsTop.Add(pc.GetPinAt(portalDir.Opposite(), 0).Id);
            }

            // Bottom right
            if (!ignoreDirs.GetCurrentValue((portalDir.ToInt() + 5) % 6))
            {
                pinsBot.Add(pc.GetPinAt(portalDir, 0).Id);
            }
            // Bottom left
            if (!ignoreDirs.GetCurrentValue((portalDir.ToInt() + 4) % 6))
            {
                pinsBot.Add(pc.GetPinAt(portalDir.Opposite(), algo.PinsPerEdge - 1).Id);
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
        }
    }

} // namespace AS2.Subroutines.SingleSourceSP
