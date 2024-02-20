using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.SegmentShift
{

    /// <summary>
    /// Procedure for shifting segments of highlighted amoebots
    /// along maximal amoebot segments.
    /// The shifting distance <c>k</c> must be given on a simple binary
    /// counter (no multiple occurrences) and each highlighted segment
    /// except the first and the last must have length at least <c>k - 1</c>.
    /// <para>
    /// This algorithm uses only two pins on each side, occupying only two
    /// "pin axes". This allows the procedure to be run on all six directions
    /// simultaneously, as long as the instances use the same binary counters
    /// with the same distance and run synchronously.
    /// If multiple instances are used, they are designed to terminate at
    /// the same time, i.e., they synchronize and wait for each other automatically.
    /// </para>
    /// <para>
    /// It is assumed that the counter storing the side lengths consists of at
    /// least two amoebots (the start must not be equal to the end point).
    /// </para>
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge. This subroutine sometimes uses all pins if multiple
    /// instances run simultaneously.
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <list type="bullet">
    /// <item>
    ///     Initialize by calling <see cref="Init(bool, Direction, int, int, int, Direction, Direction, bool, bool)"/>.
    ///     This assumes you have set up a binary counter storing the shift distance and its MSB as well as the highlighted segments.
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
    ///     whether the procedure is finished. If it is, you can determine the new segments by calling
    ///     <see cref="IsOnNewSegment"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>

    // Round plan:

    // Init:
    //  - Setup highlighted flags
    //  - Setup counter

    // Round 0:
    //  Send:
    //  - Establish singleton pin config
    //  - Non-highlighted amoebots send beep to both neighbors

    // Round 1:
    //  Receive:
    //  - Receive beep from non-highlighted neighbors
    //  - Establish start and end points based on received beeps
    //  Send:
    //  - Setup two chain circuits, split by start and end points
    //  - Start points send beep to predecessor, end points send beep to successor

    // Round 2:
    //  Receive:
    //  - Receive beeps from other segments
    //  - Start and end points set first/middle/last segment flags
    //  Send:
    //  - Setup same two chain circuits, split by start and end points
    //  - First segment beeps on first circuit (from end points towards inside)
    //  - Last segment beeps on second circuit

    // Round 3:
    //  Receive:
    //  - Highlighted amoebots receive the classification beep
    //  - Go to round 4

    // Round 4:
    //  Send:
    //  - Setup "outside" fully connected chain circuit
    //  - Setup "inside" global circuit
    //  - Let first segment start points beep on both circuits
    //  Receive:
    //  - Receive chain and global circuit beeps
    //  - If no global beep:
    //      - Terminate (have no segments)
    //  - Else:
    //      - If no beep on chain circuit:
    //          - Go to waiting mode (do not set participation flag)
    //      - Else:
    //          - We have to participate! (Set flag)
    //      - Go to round 5

    // Round 5:
    //  Send:
    //  - Setup chain circuit split only by next PASC leaders (e.g., start of first segment)
    //  - Let these leaders beep towards their predecessor
    //  Receive:
    //  - Receive PASC activation beep
    //  - If no beep: Go to waiting mode (reset participation flag
    //  - Else:
    //      - Initialize PASC for current leader (e.g., start of first segment)
    //      - Reset comparison result
    //  - Set the marker to the counter start
    //  - Go to round 6

    // Round 6:
    //  Receive ( * ):
    //  - Marker receives beep and moves forward
    //  - Receive distance bit, MSB and PASC continuation beep on the 3 global circuits
    //      - Update comparison result
    //      - If no PASC continuation beep:
    //          - If no MSB beep: Set comparison result to LESS
    //          - If the result is EQUAL: Become new start point
    //          - If we are the segment start and the result is LESS: Store the result
    //          - Reset the marker to the counter start
    //          - If we participate in PASC: Setup PASC for first segment end point
    //          - Go to round 9
    //      - If MSB and PASC continuation beep:
    //          - Start PASC cutoff (go to round 8)
    //  Send:
    //  - If we participate in the current shift:
    //      - Send PASC beep
    //  - Go to round 7

    // Round 7:
    //  Receive:
    //  - Receive PASC beep
    //  Send:
    //  - Setup three global circuits (not interfering with marker forwarding)
    //  - Marker sends distance bit on first circuit
    //  - Marker sends MSB on second circuit
    //  - Amoebots that became passive send on third circuit
    //  - Marker sends beep to successor unless it is the MSB
    //  - Go back to round 6

    // Round 8:
    //  Send:
    //  - Setup PASC cutoff and beep
    //  Receive:
    //  - Receive PASC cutoff and update comparison result
    //  - Set segment start or store comparison result
    //  - Do rest of initialization for segment end (see round 6)
    //  - Go to round 9

    // Rounds 9-12:
    //  - Receive and send like rounds 5-8
    //  - If we are finished:
    //      - We set the new segment end instead of the start flag
    //      - The segment start has slightly different updating rules
    //      - Go to round 13

    // Rounds 13-21:
    //  - Similar to rounds 4-12
    //  - But we handle the middle segments
    //  - If there are no middle segments, just skip
    //  - The segment start point is still involved but does not change its status once it already has one
    //  - When finished, we go to round 22

    // Rounds 22-30:
    //  - Similar to rounds 4-12 and 13-21
    //  - This time we handle the last segment
    //  - There is always a last segment
    //  - When finished, we go to round 31

    // Round 31:
    //  Send:
    //  - Setup simple chain circuit split at new segment start and end points
    //  - Let new start and end points beep inward (make sure that start AND end point does not beep)
    //  Receive:
    //  - Receive the beep and set new segment flag
    //  - Terminate


    public class SubSegmentShift : Subroutine
    {
        enum ComparisonResult
        {
            NONE = 0,
            EQUAL = 1,
            LESS = 2,
            GREATER = 3
        }

        //          26    25    24           23 21       20 18       17 15       14   10   9   5    4   0
        // xxxx x   x     x     x             xxx         xxx         xxx         xxxxx    xxxxx    xxxxx
        //          MSB   Bit   Highlight 1   Succ. dir   Pred. dir   Shift dir   PSet 3   PSet 2   PSet 1
        ParticleAttribute<int> state1;
        //                  19         18           17            16      15        14      13        12            11     10      9        87        65        4   0
        // xxxx xxxx xxxx   x          x            x             x       x         x       x         x             x      x       x        xx        xx        xxxxx
        //                  Finished   PASC part.   Participant   End 2   Start 2   End 1   Start 1   Highlight 2   Last   First   Marker   Comp. 2   Comp. 1   Round
        ParticleAttribute<int> state2;

        BinAttributeInt pSet1;                                      // 3 partition set IDs
        BinAttributeInt pSet2;                                      // First 2 must be different for each instance if one amoebot uses multiple instances
        BinAttributeInt pSet3;                                      // Last one must be the same for all instances
        BinAttributeDirection shiftDir;                             // Shift direction
        BinAttributeDirection predDir;                              // Counter pred. and succ. directions
        BinAttributeDirection succDir;
        BinAttributeBool highlight;                                 // Highlighted flag for input segment
        BinAttributeBool distanceBit;                               // Counter bit for the distance
        BinAttributeBool distanceMSB;                               // Counter MSB flag

        BinAttributeInt round;                                      // Round counter (0-31, 5 bits)
        BinAttributeEnum<ComparisonResult> comp1;                   // Two comparison results
        BinAttributeEnum<ComparisonResult> comp2;
        BinAttributeBool marker;                                    // Counter marker flag
        BinAttributeBool firstSegment;                              // Flags for first and last segment
        BinAttributeBool lastSegment;
        BinAttributeBool highlightNew;                              // Highlighted flag for output segment
        BinAttributeBool isStart;                                   // Start flag of input segment
        BinAttributeBool isEnd;                                     // End flag for input segment
        BinAttributeBool isStartNew;                                // Start and end flags for output segment
        BinAttributeBool isEndNew;
        BinAttributeBool participant;                               // Participant flag (we may only have one or two segments)
        BinAttributeBool pascParticipant;                           // PASC participant flag (only part of the max. segment participates)
        BinAttributeBool finished;                                  // Finished flag

        SubPASC2 pasc;

        public SubSegmentShift(Particle p, SubPASC2 pasc = null) : base(p)
        {
            state1 = algo.CreateAttributeInt(FindValidAttributeName("[Shift] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[Shift] State 2"), 0);

            pSet1 = new BinAttributeInt(state1, 0, 5);
            pSet2 = new BinAttributeInt(state1, 5, 5);
            pSet3 = new BinAttributeInt(state1, 10, 5);
            shiftDir = new BinAttributeDirection(state1, 15);
            predDir = new BinAttributeDirection(state1, 18);
            succDir = new BinAttributeDirection(state1, 21);
            highlight = new BinAttributeBool(state1, 24);
            distanceBit = new BinAttributeBool(state1, 25);
            distanceMSB = new BinAttributeBool(state1, 26);

            round = new BinAttributeInt(state2, 0, 5);
            comp1 = new BinAttributeEnum<ComparisonResult>(state2, 5, 2);
            comp2 = new BinAttributeEnum<ComparisonResult>(state2, 7, 2);
            marker = new BinAttributeBool(state2, 9);
            firstSegment = new BinAttributeBool(state2, 10);
            lastSegment = new BinAttributeBool(state2, 11);
            highlightNew = new BinAttributeBool(state2, 12);
            isStart = new BinAttributeBool(state2, 13);
            isEnd = new BinAttributeBool(state2, 14);
            isStartNew = new BinAttributeBool(state2, 15);
            isEndNew = new BinAttributeBool(state2, 16);
            participant = new BinAttributeBool(state2, 17);
            pascParticipant = new BinAttributeBool(state2, 18);
            finished = new BinAttributeBool(state2, 19);

            if (pasc is null)
                this.pasc = new SubPASC2(p);
            else
                this.pasc = pasc;
        }

        /// <summary>
        /// Initializes the subroutine. Assumes that a binary counter stores
        /// the shifting distance <c>k</c> with its MSB. Each amoebot on the
        /// counter can only store one bit. On each maximal amoebot segment along
        /// the shifting direction, the middle highlighted segments must have length
        /// at least <c>k - 1</c>.
        /// </summary>
        /// <param name="highlighted">Whether this amoebot is part of a highlighted
        /// segment that should be shifted.</param>
        /// <param name="shiftDir">The direction in which the highlighted segments
        /// should be shifted.</param>
        /// <param name="pSet1">The first partition set ID to use. Should be different
        /// for each parallel instance of the subroutine.</param>
        /// <param name="pSet2">The second partition set ID to use. Should be different
        /// for each parallel instance of the subroutine.</param>
        /// <param name="pSet3">The third partition set ID to use. Should be <b>the same</b>
        /// for each parallel instance of the subroutine.</param>
        /// <param name="counterPred">The predecessor direction of the counter.
        /// Should be <see cref="Direction.NONE"/> for the counter start and amoebots
        /// that are not on the counter.</param>
        /// <param name="counterSucc">The successor direction of the counter.
        /// Should be <see cref="Direction.NONE"/> for the counter end and amoebots
        /// that are not on the counter.</param>
        /// <param name="distanceBit">If the amoebot is on the counter, stores the bit
        /// of the distance <c>k</c>.</param>
        /// <param name="distanceMSB">Whether this is the MSB of <c>k</c> if the amoebot
        /// is on the counter.</param>
        public void Init(bool highlighted, Direction shiftDir, int pSet1, int pSet2, int pSet3,
            Direction counterPred = Direction.NONE, Direction counterSucc = Direction.NONE, bool distanceBit = false, bool distanceMSB = false)
        {
            state1.SetValue(0);
            state2.SetValue(0);

            this.highlight.SetValue(highlighted);
            this.shiftDir.SetValue(shiftDir);
            this.pSet1.SetValue(pSet1);
            this.pSet2.SetValue(pSet2);
            this.pSet3.SetValue(pSet3);
            this.predDir.SetValue(counterPred);
            this.succDir.SetValue(counterSucc);
            this.distanceBit.SetValue(distanceBit);
            this.distanceMSB.SetValue(distanceMSB);
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
                case 1:
                    {
                        // Receive beeps from non-highlighted neighbors
                        // Establish start and end points
                        if (highlight.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction pred = shiftDir.GetCurrentValue();
                            Direction succ = pred.Opposite();
                            if (!algo.HasNeighborAt(pred) || pc.GetPinAt(pred, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                // Received beep from segment predecessor or have no predecessor
                                isStart.SetValue(true);
                            if (!algo.HasNeighborAt(succ) || pc.GetPinAt(succ, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                // Received beep from segment successor or have no successor
                                isEnd.SetValue(true);
                        }
                    }
                    break;
                case 2:
                    {
                        // Receive beeps from other segments to identify first/middle/last segments
                        bool start = isStart.GetCurrentValue();
                        bool end = isEnd.GetCurrentValue();
                        if (start || end)
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction pred = shiftDir.GetCurrentValue();
                            if (start && !pc.GetPinAt(pred, 1).PartitionSet.ReceivedBeep())
                                firstSegment.SetValue(true);
                            if (end && !pc.GetPinAt(pred.Opposite(), algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                lastSegment.SetValue(true);
                        }
                    }
                    break;
                case 3:
                    {
                        // Highlighted amoebots receive the classification beep (actually only required by start and end points)
                        bool start = isStart.GetCurrentValue();
                        bool end = isEnd.GetCurrentValue();
                        if (start || end)
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            Direction pred = shiftDir.GetCurrentValue();
                            if (end && pc.GetPinAt(pred, 1).PartitionSet.ReceivedBeep())
                                firstSegment.SetValue(true);
                            if (start && pc.GetPinAt(pred.Opposite(), algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                lastSegment.SetValue(true);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 4:     // First segment
                case 13:    // Middle segments
                case 22:    // Last segment
                    {
                        // Receive chain and global circuit beeps
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(pSet3.GetCurrentValue()))
                        {
                            // No beep on the global circuit: Have no segments of this type!
                            if (r == 4)
                            {
                                // No first segment means we have no segments at all => Terminate
                                finished.SetValue(true);
                                break;
                            }
                            else if (r == 13)
                            {
                                // No middle segments: Go to last segment phase
                                round.SetValue(22);
                                break;
                            }
                        }
                        else
                        {
                            // This segment has to participate if it has received a beep on the chain circuit
                            bool participate = pc.ReceivedBeepOnPartitionSet(pSet1.GetCurrentValue());
                            participant.SetValue(participate);
                            if (!participate)
                                pascParticipant.SetValue(false);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 5:     // Start point of first segment
                case 9:     // End point of first segment
                case 14:    // Start of middle segments
                case 18:    // End of middle segments
                case 23:    // Start of last segment
                case 27:    // End of last segment
                    {
                        // Receive PASC activation beep
                        if (participant.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            bool leader = IsNextPASCLeader();
                            Direction pred = shiftDir.GetCurrentValue();
                            if (leader || pc.GetPinAt(pred.Opposite(), algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                            {
                                // Initialize PASC
                                pascParticipant.SetValue(true);
                                pasc.Init(leader ? null : new List<Direction>() { pred.Opposite() }, new List<Direction>() { pred }, 0, 1, pSet1.GetCurrentValue(), pSet2.GetCurrentValue(), leader);
                                if (r == 5 || r == 14 || r == 23)
                                    comp1.SetValue(ComparisonResult.EQUAL);
                                else
                                    comp2.SetValue(ComparisonResult.EQUAL);
                            }
                            else
                            {
                                // Do not participate
                                pascParticipant.SetValue(false);
                            }
                        }
                        // Set the marker to the counter start
                        marker.SetValue(succDir.GetCurrentValue() != Direction.NONE && predDir.GetCurrentValue() == Direction.NONE);
                        round.SetValue(r + 1);
                    }
                    break;
                case 6:     // Start point of first segment
                case 10:    // End point of first segment
                case 15:    // Start of middle segments
                case 19:    // End of middle segments
                case 24:    // Start of last segment
                case 28:    // End of last segment
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Marker receives beep and moves forward
                        marker.SetValue(false);
                        Direction pred = predDir.GetValue();
                        if (pred != Direction.NONE)
                        {
                            if (pc.GetPinAt(pred, GetMarkerPin(false, pred.Opposite())).PartitionSet.ReceivedBeep())
                                marker.SetValue(true);
                        }

                        // Receive distance bit, MSB and PASC continuation beep on 3 global circuits
                        bool distBit = pc.ReceivedBeepOnPartitionSet(0);
                        bool distMSB = pc.ReceivedBeepOnPartitionSet(1);
                        bool pascContinue = pc.ReceivedBeepOnPartitionSet(2);
                        // Have to update a different result based on current segment part
                        BinAttributeEnum<ComparisonResult> comp = r == 6 || r == 15 || r == 24 ? comp1 : comp2;
                        if (pascParticipant.GetCurrentValue())
                        {
                            // Update comparison result
                            bool pascBit = pasc.GetReceivedBit() > 0;
                            if (pascBit && !distBit)
                                comp.SetValue(ComparisonResult.GREATER);
                            else if (distBit && !pascBit)
                                comp.SetValue(ComparisonResult.LESS);
                        }
                        if (!pascContinue)
                        {
                            // PASC is finished everywhere
                            // Participants update comparison result
                            if (pascParticipant.GetCurrentValue())
                            {
                                if (!distMSB)
                                {
                                    // Received no MSB although PASC is finished: Comparison result is always LESS
                                    comp.SetValue(ComparisonResult.LESS);
                                }
                                // Become new segment start or end point
                                EvaluateCompResult();
                            }
                            // Reset the marker to the counter start
                            marker.SetValue(succDir.GetValue() != Direction.NONE && pred == Direction.NONE);
                            // Continue with next iteration
                            round.SetValue(r + 3);
                        }
                        else if (distMSB)
                        {
                            // We received only the MSB beep => Start PASC cutoff
                            round.SetValue(r + 2);
                        }
                    }
                    break;
                case 7:     // Start point of first segment
                case 11:    // End point of first segment
                case 16:    // Start of middle segments
                case 20:    // End of middle segments
                case 25:    // Start of last segment
                case 29:    // End of last segment
                    {
                        // Participants receive PASC beep
                        if (pascParticipant.GetCurrentValue())
                            pasc.ActivateReceive();
                    }
                    break;
                case 8:     // Start point of first segment
                case 12:    // End point of first segment
                case 17:    // Start of middle segment
                case 21:    // End of middle segments
                case 26:    // Start of last segment
                case 30:    // End of last segment
                    {
                        // Participants receive PASC cutoff beep
                        if (pascParticipant.GetCurrentValue())
                        {
                            pasc.ReceiveCutoffBeep();
                            // Update comparison result
                            if (pasc.GetReceivedBit() > 0)
                            {
                                // Have to update a different result based on current segment part
                                BinAttributeEnum<ComparisonResult> comp = r == 8 || r == 17 || r == 26 ? comp1 : comp2;
                                comp.SetValue(ComparisonResult.GREATER);
                            }
                            // Become new segment start or end point
                            EvaluateCompResult();
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 31:
                    {
                        // Determine final new segments by beeps and start/end points, then terminate
                        if (isStartNew.GetCurrentValue() || isEndNew.GetCurrentValue())
                            highlightNew.SetValue(true);
                        else
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            highlightNew.SetValue(pc.ReceivedBeepOnPartitionSet(pSet1.GetCurrentValue()));
                        }
                        finished.SetValue(true);
                    }
                    break;
            }
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
                        pc.SetToSingleton();
                    }
                    break;
                case 1:
                case 2:
                    {
                        // Setup two chain circuits, split at start and end points
                        SetupChainCircuits(pc, isStart.GetCurrentValue() || isEnd.GetCurrentValue());
                    }
                    break;
                case 4:     // First segment
                case 13:    // Middle segments
                case 22:    // Last segment
                    {
                        // Setup one fully connected chain circuit and a global circuit
                        SetupChainAndGlobalCircuits(pc);
                    }
                    break;
                case 5:     // Start point of first segment
                case 9:     // End point of first segment
                case 14:    // Start of middle segments
                case 18:    // End of middle segments
                case 23:    // Start of last segment
                case 27:    // End of last segment
                    {
                        // Setup chain circuits split at the next PASC leader
                        SetupChainCircuits(pc, IsNextPASCLeader());
                    }
                    break;
                case 6:     // Start point of first segment
                case 10:    // End point of first segment
                case 15:    // Start of middle segments
                case 19:    // End of middle segments
                case 24:    // Start of last segment
                case 28:    // End of last segment
                    {
                        // Participants setup PASC circuit
                        if (pascParticipant.GetCurrentValue())
                            pasc.SetupPC(pc);
                    }
                    break;
                case 7:     // Start point of first segment
                case 11:    // End point of first segment
                case 16:    // Start of middle segments
                case 20:    // End of middle segments
                case 25:    // Start of last segment
                case 29:    // End of last segment
                    {
                        // Setup 3 global circuits
                        SetupGlobalCircuits(pc);
                    }
                    break;
                case 8:     // Start point of first segment
                case 12:    // End point of first segment
                case 17:    // Start of middle segment
                case 21:    // End of middle segments
                case 26:    // Start of last segment
                case 30:    // End of last segment
                    {
                        // Participants setup PASC cutoff circuit
                        if (pascParticipant.GetCurrentValue())
                            pasc.SetupCutoffCircuit(pc);
                    }
                    break;
                case 31:
                    {
                        // Setup chain circuit split at the new segment start and end points
                        SetupChainCircuits(pc, isStartNew.GetCurrentValue() || isEndNew.GetCurrentValue());
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
                        // Non-highlighted amoebots send beep to both neighbors
                        if (!highlight.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.GetPinAt(shiftDir.GetCurrentValue(), 0).PartitionSet.SendBeep();
                            pc.GetPinAt(shiftDir.GetCurrentValue().Opposite(), 0).PartitionSet.SendBeep();
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                    {
                        bool start = isStart.GetCurrentValue();
                        bool end = isEnd.GetCurrentValue();
                        if (start || end)
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            // Start points send beep to predecessor, end points send beep to successor
                            Direction pred = shiftDir.GetCurrentValue();
                            if (start)
                                pc.GetPinAt(pred, 0).PartitionSet.SendBeep();
                            if (end)
                                pc.GetPinAt(pred.Opposite(), algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 2:
                    {
                        bool start = isStart.GetCurrentValue();
                        bool end = isEnd.GetCurrentValue();
                        if (start || end)
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            // First start point sends beep to successor, last end point sends beep to predecessor
                            // Only do this if we are not simultaneously the other segment boundary!
                            Direction pred = shiftDir.GetCurrentValue();
                            if (start && !end && firstSegment.GetCurrentValue())
                                pc.GetPinAt(pred.Opposite(), algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                            if (end && !start && lastSegment.GetCurrentValue())
                                pc.GetPinAt(pred, 0).PartitionSet.SendBeep();
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 4:     // First segment
                case 13:    // Middle segments
                case 22:    // Last segment
                    {
                        // First segment start point sends beep on both circuits
                        if (IsNextPASCLeader())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(pSet1.GetCurrentValue());
                            pc.SendBeepOnPartitionSet(pSet3.GetCurrentValue());
                        }
                    }
                    break;
                case 5:     // Start point of first segment
                case 9:     // End point of first segment
                case 14:    // Start of middle segments
                case 18:    // End of middle segments
                case 23:    // Start of last segment
                case 27:    // End of last segment
                    {
                        // Next PASC leaders send beep to predecessor
                        if (IsNextPASCLeader())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.GetPinAt(shiftDir.GetCurrentValue(), 0).PartitionSet.SendBeep();
                        }
                    }
                    break;
                case 6:     // Start point of first segment
                case 10:    // End point of first segment
                case 15:    // Start of middle segments
                case 19:    // End of middle segments
                case 24:    // Start of last segment
                case 28:    // End of last segment
                    {
                        // PASC participants send beep
                        if (pascParticipant.GetCurrentValue())
                            pasc.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 7:     // Start point of first segment
                case 11:    // End point of first segment
                case 16:    // Start of middle segments
                case 20:    // End of middle segments
                case 25:    // Start of last segment
                case 29:    // End of last segment
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Marker sends beeps on different circuits
                        if (marker.GetCurrentValue())
                        {
                            // Distance bit on first circuit
                            if (distanceBit.GetValue())
                                pc.SendBeepOnPartitionSet(0);

                            // Distance MSB on second circuit
                            bool msb = distanceMSB.GetValue();
                            if (msb)
                                pc.SendBeepOnPartitionSet(1);

                            // Marker forwarding beep to successor if this is not the MSB
                            if (!msb)
                            {
                                Direction succ = succDir.GetValue();
                                pc.GetPinAt(succ, GetMarkerPin(true, succ)).PartitionSet.SendBeep();
                            }
                        }

                        // PASC participants that became passive send beep on third global circuit
                        if (pascParticipant.GetCurrentValue() && pasc.BecamePassive())
                            pc.SendBeepOnPartitionSet(2);

                        round.SetValue(r - 1);
                    }
                    break;
                case 8:     // Start point of first segment
                case 12:    // End point of first segment
                case 17:    // Start of middle segment
                case 21:    // End of middle segments
                case 26:    // Start of last segment
                case 30:    // End of last segment
                    {
                        // Participants send PASC cutoff beep
                        if (pascParticipant.GetCurrentValue())
                            pasc.SendCutoffBeep();
                    }
                    break;
                case 31:
                    {
                        // Let new start and end points beep inward
                        bool start = isStartNew.GetCurrentValue();
                        bool end = isEndNew.GetCurrentValue();
                        if (start ^ end)
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            Direction pred = shiftDir.GetCurrentValue();
                            if (start)
                                pc.GetPinAt(pred.Opposite(), algo.PinsPerEdge - 1).PartitionSet.SendBeep();
                            if (end)
                                pc.GetPinAt(pred, 0).PartitionSet.SendBeep();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the shifted
        /// segments have been identified.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this amoebot is on a shifted segment.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure has
        /// finished and this amoebot is on one of the shifted segments.</returns>
        public bool IsOnNewSegment()
        {
            return IsFinished() && highlightNew.GetCurrentValue();
        }

        /// <summary>
        /// Helper to evaluate the comparison result of the current
        /// PASC procedure and update our status as a new segment
        /// start/end point.
        /// </summary>
        private void EvaluateCompResult()
        {
            int r = round.GetCurrentValue();
            // Must await result from end point first: Only update in end point rounds
            if (r == 10 || r == 12 || r == 19 || r == 21 || r == 28 || r == 30)
            {
                ComparisonResult startRes = comp1.GetCurrentValue();
                ComparisonResult endRes = comp2.GetCurrentValue();
                // If the result for the start point is EQUAL: We are the new start point!
                if (startRes == ComparisonResult.EQUAL)
                    isStartNew.SetValue(true);
                // Same for the end point
                if (endRes == ComparisonResult.EQUAL)
                    isEndNew.SetValue(true);

                // Special logic for max. segment start
                if (!algo.HasNeighborAt(shiftDir.GetCurrentValue()))
                {
                    // Become start point if start has moved past but end has not
                    if (startRes == ComparisonResult.LESS && endRes != ComparisonResult.LESS)
                        isStartNew.SetValue(true);

                    // Special case for later segments:
                    // If the start point reached us but the end point has not, we only become the start point
                    // (We might have been the end point due to a previous shifting operation)
                    if (startRes == ComparisonResult.EQUAL && endRes == ComparisonResult.GREATER)
                        isEndNew.SetValue(false);
                }
            }
        }

        /// <summary>
        /// Helper determining whether this amoebot is a PASC leader
        /// in the next/current PASC phase.
        /// </summary>
        /// <returns><c>true</c> for the start/end points of the
        /// first/middle/last segment(s) in the appropriate rounds.</returns>
        private bool IsNextPASCLeader()
        {
            int r = round.GetCurrentValue();
            if (r < 9)
                return firstSegment.GetCurrentValue() && isStart.GetCurrentValue();
            else if (r < 13)
                return firstSegment.GetCurrentValue() && isEnd.GetCurrentValue();
            else if (r < 18)
                return isStart.GetCurrentValue() && !firstSegment.GetCurrentValue() && !lastSegment.GetCurrentValue();
            else if (r < 22)
                return isEnd.GetCurrentValue() && !firstSegment.GetCurrentValue() && !lastSegment.GetCurrentValue();
            else if (r < 27)
                return lastSegment.GetCurrentValue() && isStart.GetCurrentValue();
            else
                return lastSegment.GetCurrentValue() && isEnd.GetCurrentValue();
        }

        /// <summary>
        /// Sets up two chain circuits that can be split at arbitrary positions.
        /// If it is not split here, the two partition sets will have IDs
        /// ID1 and ID2.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="split">Whether the chain circuits should be split here.</param>
        private void SetupChainCircuits(PinConfiguration pc, bool split)
        {
            if (!split)
            {
                Direction pred = shiftDir.GetCurrentValue();
                Direction succ = pred.Opposite();
                pc.MakePartitionSet(new int[] { pc.GetPinAt(pred, 0).Id, pc.GetPinAt(succ, algo.PinsPerEdge - 1).Id }, pSet1.GetCurrentValue());
                pc.MakePartitionSet(new int[] { pc.GetPinAt(pred, 1).Id, pc.GetPinAt(succ, algo.PinsPerEdge - 2).Id }, pSet2.GetCurrentValue());
                pc.SetPartitionSetPosition(pSet1.GetCurrentValue(), new Vector2((succ.ToInt() + 1.5f) * 60, 0.6f));
                pc.SetPartitionSetPosition(pSet2.GetCurrentValue(), new Vector2((succ.ToInt() + 1.5f) * 60, 0.3f));
            }
        }

        /// <summary>
        /// Sets up a connected chain circuit using only the outside pin and a
        /// global circuit on the two inside pins. The chain circuit uses partition set
        /// ID1 and the global circuit uses ID3.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupChainAndGlobalCircuits(PinConfiguration pc)
        {
            // Connect the chain circuit on the "outside"
            Direction pred = shiftDir.GetCurrentValue();
            Direction succ = pred.Opposite();
            pc.MakePartitionSet(new int[] { pc.GetPinAt(pred, 0).Id, pc.GetPinAt(succ, algo.PinsPerEdge - 1).Id }, pSet1.GetCurrentValue());
            pc.SetPartitionSetPosition(pSet1.GetCurrentValue(), new Vector2((succ.ToInt() + 1.5f) * 60, 0.8f));

            // Setup global circuit using both "inside" pins
            int[] pins = new int[12];
            for (int i = 0; i < 6; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                pins[2 * i] = pc.GetPinAt(d, 1).Id;
                pins[2 * i + 1] = pc.GetPinAt(d, 2).Id;
            }
            pc.MakePartitionSet(pins, pSet3.GetCurrentValue());
        }

        /// <summary>
        /// Sets up three global circuits on partition sets 0, 1 and 2.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupGlobalCircuits(PinConfiguration pc)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(0, inverted, 0);
            pc.SetStarConfig(1, inverted, 1);
            pc.SetStarConfig(2, inverted, 2);
        }

        /// <summary>
        /// Helper to determine the free pin on which to send/receive
        /// the marker beep.
        /// </summary>
        /// <param name="outgoing">Whether the outgoing pin should be
        /// returned rather than the incoming pin.</param>
        /// <param name="succDir">The direction in which the marker should move.</param>
        /// <returns>The offset of the free pin.</returns>
        private int GetMarkerPin(bool outgoing, Direction succDir)
        {
            int d = succDir.ToInt();
            if (d > 2)
                return outgoing ? 0 : 3;
            else
                return outgoing ? 3 : 0;
        }
    }

} // namespace AS2.Subroutines.SegmentShift
