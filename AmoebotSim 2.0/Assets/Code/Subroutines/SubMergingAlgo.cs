using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.PASC;
using AS2.ShapeContainment;

namespace AS2.Subroutines.ConvexShapePlacementSearch
{

    /// <summary>
    /// Valid placement search for triangles, trapezoids and pentagons.
    /// Finds all valid placements for the given shape extending in the
    /// given directions for the side lengths given in a binary counter,
    /// rotated by the specified amount.
    /// <para>
    /// It is assumed that the counter storing the side lengths consists of at
    /// least two amoebots (the start must not be equal to the end point) and
    /// each amoebot only stores one bit (only single occurrences).
    /// </para>
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge. This subroutine sometimes uses all pins on one side.
    /// </para>
    /// <para>
    /// <b>Usage:</b> Same as for <see cref="SubParallelogram"/>.
    /// </para>
    /// <para>
    /// The "merging algorithm" applied by this subroutine is modified in
    /// such a way that it has a much shorter runtime in practice (but not
    /// asymptotically). The two phases of a merge step (elimination distance
    /// check and then comparison) are done at the same time since all amoebots
    /// in Q and Q' have access to all required bits to do the comparison
    /// during the elimination step. Additionally, only amoebots in Q or Q'
    /// that have at least one candidate as a predecessor participate because
    /// all others do not contribute anything. Finally, the "leftmost" amoebot
    /// in Q or Q' always runs its elimination distance check and becomes passive.
    /// Its successor is the left end point of the first pair. This ensures that
    /// "lingering" candidates at the start of a segment are eliminated quickly.
    /// </para>
    /// </summary>

    // Round plan:

    // Init:
    //  - Rotate the given directions dw, dh
    //  - Store counter information based on the shape type
    //      - Triangle only needs one parameter a
    //      - Trapezoid needs 2 parameters a, d
    //      - Pentagon needs 5 parameters a, d, c, a' = a+c =: a2, a + 1 =: a3
    //  - Setup PASC on maximal segments in direction dw^-1
    //  - Place marker at counter start
    //  - Initialize comparison result

    // DISTANCE CHECK 1
    // Find all amoebots with distance at least a in the first given direction dw

    // Round 0:
    //  Receive (only reached after first send (*)):
    //  - Receive bit of a
    //  - Update comparison result
    //  - Receive beeps on two global circuits
    //  - If no beep on circuit 3:
    //      - If no beep on circuit 4:
    //          - PASC is finished
    //          - Set candidate flag based on comparison result
    //          - PREPARE SECOND CHECK
    //              - Set marker to counter start
    //              - Init PASC for direction dh^-1
    //              - Reset comparison results
    //              - Go to round 3
    //      - Else:
    //          - Terminate with failure
    //  - Else:
    //      - If no beep on 4:
    //          - (Perform PASC cutoff)
    //          - Go to round 2
    //  Send:
    //  - Setup PASC circuit
    //  - Send PASC beep
    //  - Marker sends beep to its successor if it is not the MSB of a
    //  - Marker stores bit of a and MSB of a in temp memory (to send them in the next round, after the marker has moved)
    //  - Go to round 1

    // Round 1:
    //  Receive:
    //  - Receive PASC beep
    //  - Move marker
    //  Send:
    //  - Setup four global circuits
    //  - Send bit of a on circuit 1
    //  - Beep on circuit 3 if we became passive
    //  - Beep on circuit 4 if we are the marker
    //  - Go back to round 0

    // Round 2:
    //  Send:
    //  - Setup PASC cutoff circuit
    //  - Send cutoff beep
    //  Receive *:
    //  - Receive PASC cutoff beep
    //  - Update comparison result based on received beep
    //  - Set candidate flag based on comparison result
    //  - PREPARE SECOND CHECK
    //      - Set marker to counter start
    //      - Init PASC for direction dh^-1
    //      - Reset comparison results
    //      - Go to round 3


    // DISTANCE CHECK 2
    // Find amoebots with distance less than d in direction dh
    // Also find amoebots with distance less than c (only pentagons)
    // This allows us to find the sets Q and Q'

    // Round 3 (similar to round 0):
    //  Receive *:
    //  - Receive bit(s) of d (and c)
    //  - Update comparison result(s)
    //  - Receive beeps on two global circuits
    //  - If no beep on circuit 3:
    //      - If no beep on circuit 4:
    //          - PASC is finished
    //          - Set Q, Q' status based on comparison result
    //          - Remove candidate status based on this
    //          - GO TO NEXT PHASE
    //              - Reset marker
    //              - Go to round 6
    //      - Else:
    //          - Terminate with failure
    //  - Else:
    //      - If no beep on circuit 4:
    //          - (Perform PASC cutoff)
    //          - Go to round 5
    //  Send:
    //  - Setup PASC circuit
    //  - Send PASC beep
    //  - Marker sends beep to its successor if it is not the MSB of d
    //      - (d >= c holds for every pentagon!!!)
    //  - Marker stores bit of d and MSB of d in temp memory (to send them in the next round, after the marker has moved)
    //      - Additionally store bit of c for pentagons
    //  - Go to round 4

    // Round 4 (similar to round 1):
    //  Receive:
    //  - Receive PASC beep
    //  - Move marker
    //  Send:
    //  - Setup four global circuits
    //  - Send bit of d on circuit 1
    //  - For pentagons: Send bit of c on circuit 2
    //  - Beep on circuit 3 if we became passive
    //  - Beep on circuit 4 if we are the marker
    //  - Go back to round 3

    // Round 5 (similar to round 2):
    //  Send:
    //  - Setup PASC cutoff circuit
    //  - Send cutoff beep
    //  Receive *:
    //  - Receive PASC cutoff beep
    //  - Update comparison result based on received beep
    //  - Set Q, Q' status based on comparison result
    //  - Remove candidate status based on this
    //  - GO TO NEXT PHASE
    //      - Reset marker
    //      - Go to round 6


    // MERGING ALGORITHM
    // In each iteration, we have a set of candidates and a set of "obstacles" in Q, Q'
    // We perform some status checks to determine whether we are finished and on what segments we have to perform merges and eliminations
    // Then, we run the elimination/merge procedure wherever it is required

    // Round 6:
    //  Send:
    //  - Establish global circuit
    //      - Candidates send beep on global circuit
    //  - Establish circuits along dw segments, split at candidates (can do both directions)
    //      - Candidates send in direction dw
    //  Receive *:
    //  - If no beep on global circuit:
    //      - Terminate with failure (no candidates left)
    //  - If we are in Q or Q' and receive no beep from the left:
    //      - Remove from Q and Q'
    //  - Go to round 7

    // Round 7:
    //  Send:
    //  - Setup global circuit
    //      - Amoebots in Q or Q' send beep
    //  - Setup segment circuits, split at amoebots in Q or Q', in both directions (use same pin config as in round 6)
    //      - Amoebots in Q or Q' send beep in both directions
    //  - Setup axis circuit in direction dh, split at amoebots in Q
    //      - Amoebots in Q send beep in direction dh
    //  Receive *:
    //  - If no beep on global circuit:
    //      - Terminate with success (no more obstacles)
    //  - If beep on segment circuit (anywhere):
    //      - Set flag indicating that this segment participates in the next phase
    //  - If we have not received a beep from a direction:
    //      - Set flag indicating we have no left/right obstacle neighbor
    //      - (Both for amoebots in Q, Q' and others)
    //  - If we have received a beep on the axis circuit:
    //      - Initialize PASC 2 in direction dh^-1
    //      - Boundary amoebots are leaders
    //  - Participating segments setup PASC 1 in direction dw, using only amoebots in Q or Q' as active ones
    //      - Amoebot without neighbor in direction dw^-1 is leader
    //  - Place marker at counter start
    //  - Go to round 8

    // Round 8:
    //  Send:
    //  - Send PASC beep on participating segments
    //  Receive *:
    //  - Receive PASC beep
    //      - Set flag for left/right pair partner if we are in Q or Q'
    //      - Amoebots not in Q or Q':
    //          - If the received bit is 1, we are between a pair (but only if we have two neighbors in Q, Q')
    //          - If we only have a right neighbor, we are still a participant (using the beep on the extra circuit)
    //  - Initialize PASC 1 instance left of each right side of a Q, Q' amoebot
    //      - Reset comparison results and carry flags
    //  - Go to round 9

    // Round 9:
    //  Send:
    //  - Setup PASC circuits for directions dw^-1 and dh^-1
    //  - Setup two global circuits
    //  - Send PASC beeps
    //  - Marker sends bit of a (or a') on first global circuit
    //  - Marker sends bit of a + 1 on second global circuit (for pentagon)
    //  Receive *:
    //  - Amoebots between pair receive PASC 1 bit
    //  - Amoebots in Q receive PASC 2 bit
    //  - All amoebots receive bit(s) of a (or a' and a+1)
    //  - Amoebots in Q:
    //      - Compute next bit of e(q) = a - d(q) (or a' - d(q))
    //  - Amoebots in Q':
    //      - Store bit of a+1
    //  - Go to round 10

    // Round 10:
    //  Send:
    //  - Setup circuit between pair (or between end point and lonely obstacle)
    //      - Right end point sends bit of e(q)
    //  - Setup two global circuits
    //      - Marker sends beep on first circuit if MSB of a (or a') has been reached
    //      - PASC 1 participant sends beep on second circuit if it became passive
    //  - Marker sends beep to successor (unless it is on MSB)
    //  Receive *:
    //  - Receive bit of e(q)
    //      - Amoebots between pair update comparison result (also left end point)
    //      - Left end point computes next bit of e(q2) - b (knows bit of b from PASC 1)
    //          - Then update comparison result between e(q) and e(q2)
    //          - (Only have to store overflow, not the bit itself)
    //  - Update marker
    //  - Receive MSB beep
    //  - If MSB has been reached:
    //      - If PASC has not finished yet:
    //          - (Start PASC cutoff)
    //          - Go to round 11
    //      - Else:
    //          - Candidates with comparison result d < e(q) retire
    //          - Go to round 12
    //  - Else:
    //      - Go back to round 9

    // Round 11:
    //  Send:
    //  - Setup PASC 1 cutoff circuit
    //  - Send cutoff beep
    //  Receive *:
    //  - Receive PASC 1 cutoff beep
    //  - Update comparison result
    //  - Candidates with comparison result d < e(q) retire
    //  - Go to round 12

    // Round 12:
    //  Send:
    //  - Setup circuit between pair end points
    //  - Left end point beeps if right end point has to become passive
    //  Receive:
    //  - Receive beep from left end point
    //  - Become passive if received
    //  - Otherwise, left end point becomes passive (it knows this already)
    //  - Last remaining end point also becomes passive
    //  - Go to round 6

    public class SubMergingAlgo : Subroutine
    {
        enum ComparisonResult
        {
            NONE = 0,
            EQUAL = 1,
            LESS = 2,
            GREATER = 3
        }

        // State represented in 2 ints
        //              2221      2019       1816    1513    1210    987     654     3210
        // xxxx xxxx x   xx        xx        xxx     xxx     xxx     xxx     xxx     xxxx
        //               Comp. 2   Comp. 1   Succ.   Pred.   Dir h   Dir w   Shape   Round
        ParticleAttribute<int> state1;
        //       28         27      26        25         24        23                 22 21 20  19       18          17         16       15       14      13   12   11          10            9  8  7  6  5        4  3  2  1  0
        // xxx   x          x       x         x          x         x                  x  x  x   x        x           x          x        x        x       x    x    x           x             x  x  x  x  x        x  x  x  x  x
        //       Excluded   Color   Success   Finished   Carry 2   Carry 1   Tmp bits 3  2  1   Pair L   Nbr Right   Nbr Left   PASC 2   PASC 1   Merge   Q'   Q    Candidate   Marker   MSBs a3 a2 c  d  a   Bits a3 a2 c  d  a
        ParticleAttribute<int> state2;

        // Binary state wrappers
        BinAttributeInt round;                              // The round counter (0-12)
        BinAttributeEnum<ShapeType> shapeType;              // The type of shape (matches convex shape algorithm's enum)
        BinAttributeDirection directionW;                   // Main direction ("width")
        BinAttributeDirection directionH;                   // Secondary direction ("height")
        BinAttributeDirection directionPred;                // Counter predecessor direction
        BinAttributeDirection directionSucc;                // Counter successor direction
        BinAttributeEnum<ComparisonResult> comp1;           // Comparison result 1
        BinAttributeEnum<ComparisonResult> comp2;           // Comparison result 2

        BinAttributeBool[] bits = new BinAttributeBool[5];  // Counter bits of numbers a, d, c, a2 = a' = a + c and a3 = a + 1
        BinAttributeBool[] msbs = new BinAttributeBool[5];  // Counter MSBs of numbers a, d, c, a2, a3
        BinAttributeBool marker;                            // Counter marker flag
        BinAttributeBool candidate;                         // Placement candidate flag
        BinAttributeBool inQ;                               // Flag for amoebots in Q
        BinAttributeBool inQ2;                              // Flag for amoebots in Q'
        BinAttributeBool inMerge;                           // Whether this segment participates in the merge procedure
        BinAttributeBool inPasc1;                           // Whether this amoebot participates in PASC between a pair of amoebots in Q, Q'
        BinAttributeBool inPasc2;                           // Whether this amoebot participates in PASC on the secondary axis
        BinAttributeBool haveNbrL;                          // Whether we have a neighbor in Q, Q' somewhere to our "left"
        BinAttributeBool haveNbrR;                          // Whether we have a neighbor in Q, Q' somewhere to our "right"
        BinAttributeBool pairLeftSide;                      // Whether we are the left side of a pair
        BinAttributeBool storedBit1;                        // Temp storages for one bit each
        BinAttributeBool storedBit2;
        BinAttributeBool storedBit3;
        BinAttributeBool carry1;                            // Whether the bit stream subtraction 1 has a "carry" bit
        BinAttributeBool carry2;                            // Whether the bit stream subtraction 2 has a "carry" bit
        BinAttributeBool finished;                          // Whether the procedure has finished
        BinAttributeBool success;                           // Whether the procedure has finished successfully
        BinAttributeBool color;                             // Whether we should control the amoebot's color
        BinAttributeBool excluded;                          // Whether we are excluded from being a placement candidate

        SubPASC2 pasc1;
        SubPASC2 pasc2;

        public SubMergingAlgo(Particle p, SubPASC2 pascInstance1 = null, SubPASC2 pascInstance2 = null) : base(p)
        {
            state1 = algo.CreateAttributeInt(FindValidAttributeName("[Merge] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[Merge] State 2"), 0);

            round = new BinAttributeInt(state1, 0, 4);
            shapeType = new BinAttributeEnum<ShapeType>(state1, 4, 3);
            directionW = new BinAttributeDirection(state1, 7);
            directionH = new BinAttributeDirection(state1, 10);
            directionPred = new BinAttributeDirection(state1, 13);
            directionSucc = new BinAttributeDirection(state1, 16);
            comp1 = new BinAttributeEnum<ComparisonResult>(state1, 19, 2);
            comp2 = new BinAttributeEnum<ComparisonResult>(state1, 21, 2);

            for (int i = 0; i < 5; i++)
            {
                bits[i] = new BinAttributeBool(state2, i);
                msbs[i] = new BinAttributeBool(state2, i + 5);
            }
            marker = new BinAttributeBool(state2, 10);
            candidate = new BinAttributeBool(state2, 11);
            inQ = new BinAttributeBool(state2, 12);
            inQ2 = new BinAttributeBool(state2, 13);
            inMerge = new BinAttributeBool(state2, 14);
            inPasc1 = new BinAttributeBool(state2, 15);
            inPasc2 = new BinAttributeBool(state2, 16);
            haveNbrL = new BinAttributeBool(state2, 17);
            haveNbrR = new BinAttributeBool(state2, 18);
            pairLeftSide = new BinAttributeBool(state2, 19);
            storedBit1 = new BinAttributeBool(state2, 20);
            storedBit2 = new BinAttributeBool(state2, 21);
            storedBit3 = new BinAttributeBool(state2, 22);
            carry1 = new BinAttributeBool(state2, 23);
            carry2 = new BinAttributeBool(state2, 24);
            finished = new BinAttributeBool(state2, 25);
            success = new BinAttributeBool(state2, 26);
            color = new BinAttributeBool(state2, 27);
            excluded = new BinAttributeBool(state2, 28);

            if (pascInstance1 is null)
                pasc1 = new SubPASC2(p);
            else
                pasc1 = pascInstance1;
            if (pascInstance2 is null)
                pasc2 = new SubPASC2(p);
            else
                pasc2 = pascInstance2;
        }

        /// <summary>
        /// Initializes the subroutine. Assumes that a binary counter stores
        /// the shape parameters as well as their MSBs. Each amoebot on the
        /// counter can only store one bit.
        /// </summary>
        /// <param name="shapeType">The kind of shape to be tested. Parallelograms
        /// and hexagons are not supported.</param>
        /// <param name="dirW">The main direction of the shape (line a).</param>
        /// <param name="dirH">The secondary direction of the shape (line d).</param>
        /// <param name="rotation">The number of 60 degree counter-clockwise rotations
        /// to be applied before testing the shape.</param>
        /// <param name="controlColor">Whether the subroutine should control the
        /// amoebot's color to display status information.</param>
        /// <param name="excluded">Whether this amoebot should be excluded from
        /// being a valid placement.</param>
        /// <param name="counterPred">The direction of the counter predecessor if
        /// this amoebot is on a counter.</param>
        /// <param name="counterSucc">The direction of the counter successor if
        /// this amoebot is on a counter.</param>
        /// <param name="bitA">This amoebot's bit of the shape parameter a.</param>
        /// <param name="msbA">Whether this amoebot holds the MSB of shape parameter a.</param>
        /// <param name="bitD">This amoebot's bit of the shape parameter d.</param>
        /// <param name="msbD">Whether this amoebot holds the MSB of shape parameter d.</param>
        /// <param name="bitC">This amoebot's bit of the shape parameter c.</param>
        /// <param name="msbC">Whether this amoebot holds the MSB of shape parameter c.</param>
        /// <param name="bitA2">This amoebot's bit of the shape parameter a' = a + c.</param>
        /// <param name="msbA2">Whether this amoebot holds the MSB of shape parameter a' = a + c.</param>
        /// <param name="bitA3">This amoebot's bit of the shape parameter a + 1.</param>
        /// <param name="msbA3">Whether this amoebot holds the MSB of shape parameter a + 1.</param>
        public void Init(ShapeType shapeType, Direction dirW, Direction dirH, int rotation, bool controlColor = false, bool excluded = false,
            Direction counterPred = Direction.NONE, Direction counterSucc = Direction.NONE,
            bool bitA = false, bool msbA = false, bool bitD = false, bool msbD = false,
            bool bitC = false, bool msbC = false, bool bitA2 = false, bool msbA2 = false, bool bitA3 = false, bool msbA3 = false)
        {
            if (shapeType == ShapeType.PARALLELOGRAM || shapeType == ShapeType.HEXAGON)
            {
                throw new InvalidActionException("Merging algorithm does not handle parallelograms and hexagons");
            }
            // Reset state
            state1.SetValue(0);
            state2.SetValue(0);

            // Read in all of the parameters
            this.shapeType.SetValue(shapeType);
            if (rotation != 0)
            {
                dirW = dirW.Rotate60(rotation);
                dirH = dirH.Rotate60(rotation);
            }
            directionW.SetValue(dirW);
            directionH.SetValue(dirH);

            color.SetValue(controlColor);
            this.excluded.SetValue(excluded);
            directionPred.SetValue(counterPred);
            directionSucc.SetValue(counterSucc);

            bits[0].SetValue(bitA);
            msbs[0].SetValue(msbA);
            if (shapeType > ShapeType.TRIANGLE)
            {
                bits[1].SetValue(bitD);
                msbs[1].SetValue(msbD);
                if (shapeType == ShapeType.PENTAGON)
                {
                    bits[2].SetValue(bitC);
                    msbs[2].SetValue(msbC);
                    bits[3].SetValue(bitA2);
                    msbs[3].SetValue(msbA2);
                    bits[4].SetValue(bitA3);
                    msbs[4].SetValue(msbA3);
                }
            }
            else
            {
                // Set d = a for triangles (makes the code simpler)
                bits[1].SetValue(bitA);
                msbs[1].SetValue(msbA);
            }

            // Setup procedure
            Direction dw = this.directionW.GetCurrentValue();
            Direction dOpp = dw.Opposite();
            bool leader = !algo.HasNeighborAt(dw);
            pasc1.Init(leader ? null : new List<Direction>() { dw }, new List<Direction>() { dOpp }, 0, 1, 0, 1, leader);
            PlaceMarkerAtCounterStart();
            comp1.SetValue(ComparisonResult.EQUAL);
            comp2.SetValue(ComparisonResult.EQUAL);

            SetColor();
        }

        /// <summary>
        /// The first half of the subroutine activation. Must be called
        /// in the round immediately after <see cref="ActivateSend"/>
        /// was called.
        /// </summary>
        public void ActivateReceive()
        {
            if (finished.GetCurrentValue())
                return;
            int r = round.GetCurrentValue();
            switch (r)
            {
                // DISTANCE CHECKS 1 AND 2

                case 0:
                case 3:
                    {
                        // Receive on 4 global circuits
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // a or d
                        bool receivedBit1 = pc.ReceivedBeepOnPartitionSet(0);
                        // c (pentagons only)
                        bool receivedBit2 = pc.ReceivedBeepOnPartitionSet(1);
                        // Update comparison result(s)
                        bool bitPasc = pasc1.GetReceivedBit() > 0;
                        UpdateComparisonPhase1(receivedBit1, bitPasc, receivedBit2);
                        bool pascContinue = pc.ReceivedBeepOnPartitionSet(2);
                        bool markerExists = pc.ReceivedBeepOnPartitionSet(3);
                        if (!pascContinue)
                        {
                            if (!markerExists)
                            {
                                // PASC is finished
                                if (r == 0)
                                {
                                    // Set candidate flag
                                    candidate.SetValue(comp1.GetCurrentValue() != ComparisonResult.LESS && !excluded.GetValue());
                                    // Prepare for second check
                                    SetupSecondDistanceCheck();
                                }
                                else
                                {
                                    // Set Q / Q' flag
                                    SetupQFlag();
                                    // Remove marker
                                    marker.SetValue(false);
                                }
                                // Reset results and go to next round
                                comp1.SetValue(ComparisonResult.EQUAL);
                                comp2.SetValue(ComparisonResult.EQUAL);
                                round.SetValue(r + 3);
                            }
                            else
                            {
                                // Terminate with failure (no amoebot has the required distance)
                                finished.SetValue(true);
                                success.SetValue(false);
                            }
                        }
                        else
                        {
                            if (!markerExists)
                            {
                                // Start PASC cutoff
                                round.SetValue(r + 2);
                            }
                        }
                    }
                    break;
                case 1:
                case 4:
                    {
                        // Receive PASC beep
                        pasc1.ActivateReceive();
                        // Move marker
                        MoveMarker(r == 1 ? directionW.GetCurrentValue() : directionH.GetCurrentValue());
                    }
                    break;
                case 2:
                case 5:
                    {
                        pasc1.ReceiveCutoffBeep();
                        // Update comparison (both distance bits will be 0 because the marker has reached the MSB)
                        UpdateComparisonPhase1(false, pasc1.GetReceivedBit() > 0, false);
                        if (r == 2)
                        {
                            // Set candidate flag and setup next distance check
                            candidate.SetValue(comp1.GetCurrentValue() != ComparisonResult.LESS && !excluded.GetValue());
                            SetupSecondDistanceCheck();
                        }
                        else
                        {
                            // Set Q / Q' flag
                            SetupQFlag();
                            // Remove marker
                            marker.SetValue(false);
                        }
                        // Reset results and go to next phase (second check or merging algorithm)
                        comp1.SetValue(ComparisonResult.EQUAL);
                        comp2.SetValue(ComparisonResult.EQUAL);
                        round.SetValue(r + 1);
                    }
                    break;

                // MERGING ALGORITHM

                case 6:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Terminate with failure if no candidate beeped on the global circuit
                        if (!pc.ReceivedBeepOnPartitionSet(2))
                        {
                            finished.SetValue(true);
                            success.SetValue(false);
                            break;
                        }
                        // Amoebots in Q or Q' that do not receive a beep from the left: Remove from the set
                        if ((inQ.GetCurrentValue() || inQ2.GetCurrentValue()) && !pc.GetPinAt(directionW.GetValue().Opposite(), 0).PartitionSet.ReceivedBeep())
                        {
                            inQ.SetValue(false);
                            inQ2.SetValue(false);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 7:
                    {
                        // inMerge and inPasc2 will be set here
                        inMerge.SetValue(false);
                        inPasc2.SetValue(false);

                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Terminate with success if no amoebot in Q or Q' beeped on the global circuit
                        if (!pc.ReceivedBeepOnPartitionSet(2))
                        {
                            finished.SetValue(true);
                            success.SetValue(true);
                            break;
                        }
                        // Receive directional beeps from amoebots in Q and Q'
                        Direction d = directionW.GetValue();
                        Direction dOpp = d.Opposite();
                        bool beepL = pc.GetPinAt(dOpp, 1).PartitionSet.ReceivedBeep();
                        bool beepR = pc.GetPinAt(d, 3).PartitionSet.ReceivedBeep();
                        haveNbrL.SetValue(beepL);
                        haveNbrR.SetValue(beepR);
                        // If any beep was received, we will participate in the next phase
                        // Amoebots in Q and Q' always participate
                        if (beepL || beepR || inQ.GetCurrentValue() || inQ2.GetCurrentValue())
                        {
                            inMerge.SetValue(true);
                            // Setup PASC 1 along the segment
                            bool leader = !algo.HasNeighborAt(dOpp);
                            pasc1.Init(leader ? null : new List<Direction>() { dOpp }, new List<Direction>() { d }, 2, 3, 0, 1, leader, inQ.GetCurrentValue() || inQ2.GetCurrentValue());
                        }
                        // Establish PASC 2 along axis h where a beep was received
                        d = directionH.GetValue();
                        dOpp = d.Opposite();
                        if (inQ.GetCurrentValue() || pc.GetPinAt(dOpp, 3).PartitionSet.ReceivedBeep())
                        {
                            bool leader = !algo.HasNeighborAt(d);
                            pasc2.Init(leader ? null : new List<Direction>() { d }, new List<Direction>() { dOpp }, 0, 1, 2, 3, leader);
                            inPasc2.SetValue(true);
                        }
                        PlaceMarkerAtCounterStart();
                        round.SetValue(r + 1);
                    }
                    break;
                case 8:
                    {
                        // inPasc1 and inPairLeftSide will be set here
                        inPasc1.SetValue(false);
                        pairLeftSide.SetValue(false);

                        // Participating segments receive PASC beep and setup new PASC instance
                        if (inMerge.GetCurrentValue())
                        {
                            pasc1.ActivateReceive();
                            // Find out whether we participate in PASC 1 and setup the instance
                            // Bit 1 means we are right nbr, bit 0 means we are left nbr, if the partner exists
                            bool q = inQ.GetCurrentValue() || inQ2.GetCurrentValue();
                            bool pascBit = pasc1.GetReceivedBit() > 0;
                            bool nbrL = haveNbrL.GetCurrentValue();
                            bool nbrR = haveNbrR.GetCurrentValue();
                            bool inPair = q && (!pascBit && nbrR || pascBit && nbrL);
                            Direction d = directionW.GetValue();
                            if (q)
                            {
                                // Amoebots in Q or Q' determine whether they are the left or right side of a pair
                                // (Special case: Leftmost amoebot runs PASC as well)
                                inPasc1.SetValue(inPair || !nbrL);
                                if (inPair)
                                    pairLeftSide.SetValue(!pascBit);
                            }
                            else
                            {
                                // Other amoebots participate if they have a right neighbor in Q or Q'
                                // and have received a 0-bit
                                inPasc1.SetValue(nbrR && !pascBit);
                            }
                            if (inPasc1.GetCurrentValue())
                            {
                                // Setup the PASC instance
                                bool leader = q && (inPair && !pairLeftSide.GetCurrentValue() || !nbrL);
                                pasc1.Init(leader ? null : new List<Direction>() { d }, new List<Direction>() { d.Opposite() }, 0, 1, 0, 1, leader);
                                comp1.SetValue(ComparisonResult.EQUAL);
                                comp2.SetValue(ComparisonResult.EQUAL);
                                carry1.SetValue(false);
                                carry2.SetValue(false);
                                storedBit1.SetValue(false);
                                storedBit2.SetValue(false);
                                storedBit3.SetValue(false);
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 9:
                    {
                        // Receive PASC bits
                        if (inPasc1.GetCurrentValue())
                            pasc1.ActivateReceive();
                        if (inPasc2.GetCurrentValue())
                            pasc2.ActivateReceive();
                        // Receive bits
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        bool bit1 = pc.ReceivedBeepOnPartitionSet(4);
                        bool bit2 = pc.ReceivedBeepOnPartitionSet(5);
                        // Amoebots in Q compute next bit of e(q) = a - d(q) (or a' - d(q))
                        if (inQ.GetCurrentValue() && inPasc1.GetCurrentValue())
                        {
                            bool pasc2Bit = pasc2.GetReceivedBit() > 0;
                            BinSubtraction(bit1, pasc2Bit, carry1.GetCurrentValue(), out bool bit, out bool carryOut);
                            storedBit1.SetValue(bit);
                            carry1.SetValue(carryOut);
                        }
                        // Amoebots in Q' store next bit of a+1
                        else if (inQ2.GetCurrentValue() && inPasc1.GetCurrentValue())
                        {
                            storedBit1.SetValue(bit2);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 10:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Receive bit of e(q) from right neighbor in Q, Q'
                        if (inPasc1.GetCurrentValue() && !pasc1.IsLeader())
                        {
                            bool bit = pc.GetPinAt(directionW.GetValue(), 3).PartitionSet.ReceivedBeep();
                            bool bitPasc = pasc1.GetReceivedBit() > 0;
                            // Update comparison result
                            if (bitPasc && !bit)
                                comp1.SetValue(ComparisonResult.GREATER);
                            else if (!bitPasc && bit)
                                comp1.SetValue(ComparisonResult.LESS);

                            // Left end point computes next bit of e(q2) - b
                            if (pairLeftSide.GetCurrentValue())
                            {
                                BinSubtraction(bit, bitPasc, carry2.GetCurrentValue(), out bool bitOut, out bool carryOut);
                                carry2.SetValue(carryOut);
                                // Compare e(q) to e(q2) - b
                                bool myBit = storedBit1.GetCurrentValue();
                                if (myBit && !bitOut)
                                    comp2.SetValue(ComparisonResult.GREATER);
                                else if (!myBit && bitOut)
                                    comp2.SetValue(ComparisonResult.LESS);
                            }
                        }
                        // Update the marker
                        MoveMarker(directionW.GetValue());
                        // Receive MSB and PASC continuation beeps
                        bool msbReached = pc.ReceivedBeepOnPartitionSet(1);
                        bool pascContinue = pc.ReceivedBeepOnPartitionSet(2);
                        if (msbReached)
                        {
                            if (pascContinue)
                            {
                                // Start PASC cutoff
                                round.SetValue(11);
                            }
                            else
                            {
                                // Candidates with comparison result d < e(q) retire
                                if (candidate.GetCurrentValue() && inPasc1.GetCurrentValue() && comp1.GetCurrentValue() == ComparisonResult.LESS)
                                    candidate.SetValue(false);
                                round.SetValue(12);
                            }
                        }
                        else
                        {
                            // Continue with next iteration (have to go until end of counter)
                            round.SetValue(9);
                        }
                    }
                    break;
                case 11:
                    {
                        // PASC 1 cutoff
                        if (inPasc1.GetCurrentValue())
                        {
                            pasc1.ReceiveCutoffBeep();
                            // Update comparison result
                            if (pasc1.GetReceivedBit() > 0)
                            {
                                comp1.SetValue(ComparisonResult.GREATER);
                                // Left end points listening and comparing e(q) to e(q2) - b now know that b > e(q2), so e(q) is greater
                                if (pairLeftSide.GetCurrentValue())
                                    comp2.SetValue(ComparisonResult.GREATER);
                            }
                            // Candidates with comparison result d < e(q) retire
                            if (candidate.GetCurrentValue() && comp1.GetCurrentValue() == ComparisonResult.LESS)
                                candidate.SetValue(false);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 12:
                    {
                        // Receive beep from left end point
                        // Retire if we received the beep or did not send the beep or we are the leftmost amoebot in Q, Q'
                        if (inPasc1.GetCurrentValue() && (inQ.GetCurrentValue() || inQ2.GetCurrentValue()))
                        {
                            bool retire = false;
                            if (!haveNbrL.GetCurrentValue())
                                retire = true;
                            else if (pairLeftSide.GetCurrentValue())
                            {
                                ComparisonResult c1 = comp1.GetCurrentValue();
                                ComparisonResult c2 = comp2.GetCurrentValue();
                                if (c1 == ComparisonResult.LESS && c2 == ComparisonResult.LESS)
                                    retire = true;
                            }
                            else
                            {
                                PinConfiguration pc = algo.GetCurrentPinConfiguration();
                                if (pc.GetPinAt(directionW.GetValue().Opposite(), 0).PartitionSet.ReceivedBeep())
                                    retire = true;
                            }
                            if (retire)
                            {
                                inQ.SetValue(false);
                                inQ2.SetValue(false);
                            }
                        }
                        // Repeat
                        round.SetValue(6);
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
            if (finished.GetCurrentValue())
                return;
            int r = round.GetCurrentValue();
            switch (r)
            {
                // DISTANCE CHECKS 1 AND 2

                case 0:
                case 3:
                    {
                        pasc1.SetupPC(pc);
                    }
                    break;
                case 1:
                case 4:
                    {
                        // Setup 4 global circuits
                        Setup4GlobalCircuits(pc);
                    }
                    break;
                case 2:
                case 5:
                    {
                        pasc1.SetupCutoffCircuit(pc);
                    }
                    break;

                // MERGING ALGORITHM

                case 6:
                    {
                        SetupGlobalAndLineCircuits(pc, directionW.GetValue(), candidate.GetCurrentValue());
                    }
                    break;
                case 7:
                    {
                        bool q = inQ.GetCurrentValue();
                        bool q2 = inQ2.GetCurrentValue();
                        SetupGlobalAndLineCircuits(pc, directionW.GetValue(), q || q2, directionH.GetValue(), q);
                    }
                    break;
                case 8:
                    {
                        // Participating segments setup PASC circuit
                        if (inMerge.GetCurrentValue())
                        {
                            pasc1.SetupPC(pc);
                        }
                    }
                    break;
                case 9:
                    {
                        // Setup both PASC instance circuits
                        if (inPasc1.GetCurrentValue())
                            pasc1.SetupPC(pc);
                        if (inPasc2.GetCurrentValue())
                            pasc2.SetupPC(pc);
                        // Also setup 2 global circuits
                        Setup2GlobalCircuits(pc, directionW.GetValue(), directionH.GetValue());
                    }
                    break;
                case 10:
                    {
                        SetupPairAndGlobalCircuits(pc, inPasc1.GetCurrentValue() ? directionW.GetValue() : Direction.NONE, inQ.GetCurrentValue() || inQ2.GetCurrentValue());
                    }
                    break;
                case 11:
                    {
                        // PASC 1 cutoff
                        if (inPasc1.GetCurrentValue())
                            pasc1.SetupCutoffCircuit(pc);
                    }
                    break;
                case 12:
                    {
                        // Setup simple circuit between pair end points
                        if (inPasc1.GetCurrentValue() && !inQ.GetCurrentValue() && !inQ2.GetCurrentValue() && haveNbrL.GetCurrentValue() && haveNbrR.GetCurrentValue())
                        {
                            Direction d = directionW.GetValue();
                            pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 3).Id, pc.GetPinAt(d.Opposite(), 0).Id }, 0);
                        }
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
            if (finished.GetCurrentValue())
                return;
            int r = round.GetCurrentValue();
            switch (r)
            {
                // DISTANCE CHECKS 1 AND 2

                case 0:
                case 3:
                    {
                        // Send PASC beep
                        pasc1.ActivateSend();
                        // Marker sends beep to successor if it is not at the MSB of a / d
                        if (marker.GetCurrentValue())
                        {
                            Direction succ = directionSucc.GetCurrentValue();
                            if (succ != Direction.NONE && !(r == 0 && msbs[0].GetCurrentValue()) && !(r == 3 && msbs[1].GetCurrentValue()))
                            {
                                int pin = GetMarkerPin(true, succ, r == 0 ? directionW.GetCurrentValue() : directionH.GetCurrentValue());
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                pc.GetPinAt(succ, pin).PartitionSet.SendBeep();
                            }
                            // Marker also stores bit(s) and MSB value
                            if (r == 0)
                            {
                                // a in first check
                                storedBit1.SetValue(bits[0].GetCurrentValue());
                                storedBit2.SetValue(msbs[0].GetCurrentValue());
                            }
                            else
                            {
                                // d in second check
                                storedBit1.SetValue(bits[1].GetCurrentValue());
                                storedBit2.SetValue(msbs[1].GetCurrentValue());
                                // c for pentagons
                                if (shapeType.GetCurrentValue() == ShapeType.PENTAGON)
                                    storedBit3.SetValue(bits[2].GetCurrentValue());
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                case 4:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Previous marker sends bit of a / d on first circuit
                        if (storedBit1.GetCurrentValue())
                        {
                            pc.SendBeepOnPartitionSet(0);
                            storedBit1.SetValue(false);
                        }
                        // Pentagon: Send bit of c as well
                        if (storedBit3.GetCurrentValue())
                        {
                            pc.SendBeepOnPartitionSet(1);
                            storedBit3.SetValue(false);
                        }
                        // Beep on circuit 3 if we became passive
                        if (pasc1.BecamePassive())
                            pc.SendBeepOnPartitionSet(2);
                        // Beep on circuit 4 if we are the current marker
                        if (marker.GetCurrentValue())
                            pc.SendBeepOnPartitionSet(3);
                        // Go back to previous round
                        round.SetValue(r - 1);
                    }
                    break;
                case 2:
                case 5:
                    {
                        pasc1.SendCutoffBeep();
                    }
                    break;

                // MERGING ALGORITHM

                case 6:
                    {
                        // Candidates beep on global circuit and send beeps in direction w
                        if (candidate.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(2);
                            Direction d = directionW.GetValue();
                            pc.GetPinAt(d, 3).PartitionSet.SendBeep();
                            pc.GetPinAt(d, 2).PartitionSet.SendBeep();
                        }
                    }
                    break;
                case 7:
                    {
                        bool q = inQ.GetCurrentValue();
                        bool q2 = inQ2.GetCurrentValue();
                        // Amoebots in Q or Q' beep on global circuit and send beeps in both directions on segment
                        // Amoebots in Q send beep upward on axis to inform about PASC 2
                        if (q || q2)
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(2);
                            Direction d = directionW.GetValue();
                            pc.GetPinAt(d, 2).PartitionSet.SendBeep();
                            pc.GetPinAt(d.Opposite(), 0).PartitionSet.SendBeep();
                            if (q)
                                pc.GetPinAt(directionH.GetValue(), 0).PartitionSet.SendBeep();
                        }
                    }
                    break;
                case 8:
                    {
                        // Participating segments send PASC beep
                        if (inMerge.GetCurrentValue())
                        {
                            pasc1.ActivateSend();
                        }
                    }
                    break;
                case 9:
                    {
                        // Send PASC beeps
                        if (inPasc1.GetCurrentValue())
                            pasc1.ActivateSend();
                        if (inPasc2.GetCurrentValue())
                            pasc2.ActivateSend();
                        // Marker sends bit of a (or a') on first global circuit
                        if (marker.GetCurrentValue())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            if (shapeType.GetValue() == ShapeType.PENTAGON)
                            {
                                if (bits[3].GetValue())
                                    pc.SendBeepOnPartitionSet(4);
                                // Also send bit of a+1 on second global circuit
                                if (bits[4].GetValue())
                                    pc.SendBeepOnPartitionSet(5);
                            }
                            else
                            {
                                if (bits[0].GetValue())
                                    pc.SendBeepOnPartitionSet(4);
                            }
                        }
                    }
                    break;
                case 10:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Right end point of pair (or leftmost amoebot in Q, Q') sends bit of e(q) on the line circuit
                        if (inPasc1.GetCurrentValue() && pasc1.IsLeader() && storedBit1.GetCurrentValue())
                        {
                            pc.GetPinAt(directionW.GetValue().Opposite(), 0).PartitionSet.SendBeep();
                        }
                        // Marker sends beep on first global circuit if the MSB has been reached
                        // and sends beep to successor otherwise
                        if (marker.GetCurrentValue())
                        {
                            bool reachedMSB = shapeType.GetValue() == ShapeType.PENTAGON && msbs[3].GetValue() || shapeType.GetValue() != ShapeType.PENTAGON && msbs[0].GetValue();
                            if (reachedMSB)
                                pc.SendBeepOnPartitionSet(1);
                            else
                            {
                                Direction succ = directionSucc.GetValue();
                                if (succ != Direction.NONE)
                                {
                                    int pin = GetMarkerPin(true, succ, directionW.GetValue());
                                    pc.GetPinAt(succ, pin).PartitionSet.SendBeep();
                                }
                            }
                        }
                        // PASC 1 participants becoming passive send beep on second global circuit
                        if (inPasc1.GetCurrentValue() && pasc1.BecamePassive())
                            pc.SendBeepOnPartitionSet(2);
                    }
                    break;
                case 11:
                    {
                        // PASC 1 cutoff
                        if (inPasc1.GetCurrentValue())
                            pasc1.SendCutoffBeep();
                    }
                    break;
                case 12:
                    {
                        // Left end point beeps if the right end point has to become passive
                        ComparisonResult c1 = comp1.GetCurrentValue();
                        ComparisonResult c2 = comp2.GetCurrentValue();
                        if (pairLeftSide.GetCurrentValue() && (c1 != ComparisonResult.LESS || c1 == ComparisonResult.LESS && c2 != ComparisonResult.LESS))
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.GetPinAt(directionW.GetValue(), 3).PartitionSet.SendBeep();
                        }
                    }
                    break;
            }
            SetColor();
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if all valid placements
        /// were found or ruled out.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether the procedure finished successfully, i.e.,
        /// there is a valid placement in the system.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// is finished and there is at least one valid placement.</returns>
        public bool Success()
        {
            return IsFinished() && success.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this amoebot is a representative of a
        /// valid placement after the procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure
        /// is finished and this amoebot was determined as a
        /// valid placement.</returns>
        public bool IsRepresentative()
        {
            return IsFinished() && candidate.GetCurrentValue();
        }

        /// <summary>
        /// Helper setting the amoebot color if this option
        /// is active. The base color is black, the marker is
        /// highlighted in orange, candidates are green,
        /// checking segments are blue (light blue for amoebots
        /// in Q, aqua for amoebots in Q', dark blue for passive
        /// ones) and the whole system is red if no placement was
        /// found.
        /// </summary>
        private void SetColor()
        {
            if (!color.GetCurrentValue())
                return;
            bool isFinished = finished.GetCurrentValue();
            if (marker.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Orange);
            else if (candidate.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Green);
            else if (isFinished && !success.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Red);
            else if (!isFinished && inMerge.GetCurrentValue())
            {
                if (inQ.GetCurrentValue())
                    algo.SetMainColor(ColorData.Particle_Blue);
                else if (inQ2.GetCurrentValue())
                    algo.SetMainColor(ColorData.Particle_Aqua);
                else
                    algo.SetMainColor(ColorData.Particle_BlueDark);
            }
            else
                algo.SetMainColor(ColorData.Particle_Black);
        }

        /// <summary>
        /// Helper that places a marker at each counter start point and
        /// removes all other markers.
        /// </summary>
        private void PlaceMarkerAtCounterStart()
        {
            marker.SetValue(directionPred.GetCurrentValue() == Direction.NONE && directionSucc.GetCurrentValue() != Direction.NONE);
        }

        /// <summary>
        /// Helper to set the new marker after a marker beep has been sent.
        /// Removes all other markers.
        /// </summary>
        /// <param name="lineDir">The direction of the line on which PASC is
        /// being executed (shape line direction).</param>
        private void MoveMarker(Direction lineDir)
        {
            PinConfiguration pc = algo.GetCurrentPinConfiguration();
            Direction pred = directionPred.GetCurrentValue();
            if (pred != Direction.NONE)
            {
                int pin = GetMarkerPin(false, pred.Opposite(), lineDir);
                marker.SetValue(pc.GetPinAt(pred, pin).PartitionSet.ReceivedBeep());
            }
            else
                marker.SetValue(false);
        }

        /// <summary>
        /// Helper updating the comparison result during the first phase,
        /// where the distance checks are carried out. Updates the second
        /// comparison result too if the shape is a pentagon.
        /// </summary>
        /// <param name="bitDist">The distance bit for a or d.</param>
        /// <param name="bitPasc">The received PASC bit.</param>
        /// <param name="bitDist2">The distance bit for c if the shape
        /// is a pentagon.</param>
        private void UpdateComparisonPhase1(bool bitDist, bool bitPasc, bool bitDist2 = false)
        {
            if (bitPasc && !bitDist)
                comp1.SetValue(ComparisonResult.GREATER);
            else if (!bitPasc && bitDist)
                comp1.SetValue(ComparisonResult.LESS);
            if (shapeType.GetValue() == ShapeType.PENTAGON)
            {
                if (bitPasc && !bitDist2)
                    comp2.SetValue(ComparisonResult.GREATER);
                else if (!bitPasc && bitDist2)
                    comp2.SetValue(ComparisonResult.LESS);
            }
        }

        /// <summary>
        /// Helper containing code to setup the second distance check
        /// in the first phase. Only used to avoid duplicating code.
        /// </summary>
        private void SetupSecondDistanceCheck()
        {
            PlaceMarkerAtCounterStart();
            Direction dh = directionH.GetValue();
            Direction dOpp = dh.Opposite();
            bool leader = !algo.HasNeighborAt(dh);
            pasc1.Init(leader ? null : new List<Direction>() { dh }, new List<Direction>() { dOpp }, 0, 1, 0, 1, leader);
        }

        /// <summary>
        /// Helper setting up the Q and Q' flags and removing the candidate state
        /// after the second distance check in the first phase. Only used to
        /// avoid duplicate code.
        /// </summary>
        private void SetupQFlag()
        {
            if (comp1.GetCurrentValue() == ComparisonResult.LESS)
            {
                inQ.SetValue(true);
                candidate.SetValue(false);  // Also retire candidacy
                // For pentagon: Also check Q'
                // (only has to be done if first distance check failed because c <= d)
                if (shapeType.GetValue() == ShapeType.PENTAGON && comp2.GetCurrentValue() == ComparisonResult.LESS)
                {
                    inQ.SetValue(false);
                    inQ2.SetValue(true);
                }
            }
        }

        /// <summary>
        /// Helper to determine the free pin on which to send/receive
        /// the marker beep.
        /// </summary>
        /// <param name="outgoing">Whether the outgoing pin should be
        /// returned rather than the incoming pin.</param>
        /// <param name="succDir">The direction in which the marker should move.</param>
        /// <param name="lineDir">The direction of the line on which PASC is
        /// being executed (shape line direction).</param>
        /// <returns>The offset of the free pin.</returns>
        private int GetMarkerPin(bool outgoing, Direction succDir, Direction lineDir)
        {
            if (succDir == lineDir)
                return outgoing ? 0 : 3;
            else
                return outgoing ? 3 : 0;
        }

        /// <summary>
        /// Helper for binary subtraction. Computes the new bit
        /// and carry of A - B with the given carry flag.
        /// </summary>
        /// <param name="bitA">The bit of the first number.</param>
        /// <param name="bitB">The bit of the second number.</param>
        /// <param name="carryIn">Whether a carry is necessary.</param>
        /// <param name="bitOut">The resulting bit.</param>
        /// <param name="carryOut">The resulting carry.</param>
        private void BinSubtraction(bool bitA, bool bitB, bool carryIn, out bool bitOut, out bool carryOut)
        {
            // 0 - 0 = 0
            // 1 - 0 = 1
            // 0 - 1 = 1 + carry
            // 1 - 1 = 0
            bool bit = bitA ^ bitB;
            // Apply carry
            if (carryIn)
                bit = !bit;
            bitOut = bit;
            // Compute next carry
            carryOut = !bitA && bitB || carryIn && bitA == bitB;
        }

        /// <summary>
        /// Sets up 4 global circuits on partition sets 0, 1, 2, 3.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void Setup4GlobalCircuits(PinConfiguration pc)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            for (int i = 0; i < 4; i++)
            {
                pc.SetStarConfig(i, inverted, i);
            }
        }

        /// <summary>
        /// Sets up 2 global circuits on partition sets 4 and 5, avoiding
        /// the two given PASC line directions (PASC is actually running in
        /// the opposite direction). The two directions must not be on the
        /// same axis
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="dir1">The first PASC line direction to avoid.</param>
        /// <param name="dir2">The second PASC line direction to avoid.</param>
        private void Setup2GlobalCircuits(PinConfiguration pc, Direction dir1, Direction dir2)
        {
            bool[] inverted = new bool[] { false, false, false, false, false, false };
            int d1 = dir1.Opposite().ToInt();
            int d2 = dir2.Opposite().ToInt();
            inverted[d1] = true;
            inverted[d2] = true;
            // Find the next free direction
            int d = (d1 + 1) % 6;
            if (d == d2 || (d == ((d2 + 3) % 6)))
                d = (d1 + 2) % 6;
            inverted[d] = true;
            pc.SetStarConfig(0, inverted, 4);
            pc.SetStarConfig(1, inverted, 5);
            pc.SetPartitionSetPosition(4, new Vector2((dir1.ToInt() - 0.6f) * 60, 0.7f));
            pc.SetPartitionSetPosition(5, new Vector2((dir1.ToInt() - 1.4f) * 60, 0.7f));
        }

        /// <summary>
        /// Helper setting up directional circuits along a line, a global
        /// circuit on partition set 2 and an optional axis circuit. The
        /// directional circuits and the axis circuit can be split at
        /// given positions. The directional circuit uses pins 0 and 1 in
        /// the opposite direction of the line and the axis circuit uses
        /// pin 0 in direction of the axis.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="lineDir">The shape line direction of the directional circuits.</param>
        /// <param name="split">Whether the directional circuits should be split here.</param>
        /// <param name="axisDir">The direction of the axis circuit. If <see cref="Direction.NONE"/>,
        /// no axis circuit will be established.</param>
        /// <param name="splitAxis">Whether the axis circuit should be split here.</param>
        private void SetupGlobalAndLineCircuits(PinConfiguration pc, Direction lineDir, bool split, Direction axisDir = Direction.NONE, bool splitAxis = false)
        {
            // Setup segment circuits on first two pins
            if (!split)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(lineDir, 3).Id, pc.GetPinAt(lineDir.Opposite(), 0).Id }, 0);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(lineDir, 2).Id, pc.GetPinAt(lineDir.Opposite(), 1).Id }, 1);
                pc.SetPartitionSetPosition(0, new Vector2((lineDir.ToInt() + 1.5f) * 60, 0.5f));
                pc.SetPartitionSetPosition(1, new Vector2((lineDir.ToInt() + 1.5f) * 60, 0.2f));
            }
            
            // Setup global circuit
            bool[] inverted = new bool[] { false, false, false, false, false, false };
            int d = lineDir.ToInt();
            for (int i = 0; i < 3; i++)
                inverted[(d + i) % 6] = true;
            pc.SetStarConfig(2, inverted, 2);
            pc.SetPartitionSetPosition(2, new Vector2((lineDir.ToInt() - 2f) * 60, 0.6f));

            // Setup axis circuit
            if (axisDir != Direction.NONE && !splitAxis)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(axisDir, 0).Id, pc.GetPinAt(axisDir.Opposite(), 3).Id }, 3);
                pc.SetPartitionSetPosition(3, new Vector2((axisDir.ToInt() - 1.5f) * 60, 0.3f));
            }
        }

        /// <summary>
        /// Helper setting up a line circuit to connect pairs and two
        /// global circuits on partition sets 1 and 2. If the line direction
        /// is <see cref="Direction.NONE"/>, no line circuit will be created.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="lineDir">The direction of the shape line.</param>
        /// <param name="split">Whether the line circuit should be split here.</param>
        private void SetupPairAndGlobalCircuits(PinConfiguration pc, Direction lineDir, bool split)
        {
            if (lineDir != Direction.NONE && !split)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(lineDir, 3).Id, pc.GetPinAt(lineDir.Opposite(), 0).Id }, 0);
                pc.SetPartitionSetPosition(0, new Vector2((lineDir.ToInt() + 1.5f) * 60, 0.7f));
            }
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(1, inverted, 1);
            pc.SetStarConfig(2, inverted, 2);
            pc.SetPartitionSetPosition(1, new Vector2((lineDir.ToInt() - 1f) * 60, 0.6f));
            pc.SetPartitionSetPosition(2, new Vector2((lineDir.ToInt() - 2f) * 60, 0.6f));
        }
    }

} // namespace AS2.Subroutines.ConvexShapePlacementSearch
