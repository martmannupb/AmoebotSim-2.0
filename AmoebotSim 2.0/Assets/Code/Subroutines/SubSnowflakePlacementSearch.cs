using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.PASC;
using AS2.Subroutines.SegmentShift;

namespace AS2.Subroutines.SnowflakePlacementSearch
{

    /// <summary>
    /// Container class storing all information required by the placement search
    /// for snowflake shapes.
    /// </summary>
    public class SnowflakeInfo
    {
        /// <summary>
        /// The list of the snowflake's dependency tree's nodes,
        /// in topological order. The arm lengths must be indices
        /// in the array of occurring lengths instead of actual lengths.
        /// </summary>
        public ShapeContainer.DTreeNode[] nodes;

        /// <summary>
        /// The occurring arm lengths in ascending order.
        /// </summary>
        public int[] armLengths;

        /// <summary>
        /// Binary strings of the occurring arm lengths.
        /// </summary>
        public string[] armLengthsStr;

        /// <summary>
        /// The number of bits in the longest arm length string.
        /// </summary>
        public int longestParameter;
    }

    /// <summary>
    /// Valid placement search procedure for snowflake shapes.
    /// <para>
    /// The snowflake must be described by a <see cref="SnowflakeInfo"/> instance.
    /// The instance contains a dependency graph as well as a list of arm lengths
    /// that occur in the shape. This allows us to drastically reduce the runtime
    /// of the placement search because we can check all 6 rotations at the same
    /// time and minimize the number of distance checks.
    /// </para>
    /// <para>
    /// It is assumed that each counter storing the scale factor and arm lengths consists
    /// of at least two amoebots (the start must not be equal to the end point). The
    /// arm lengths are not stored directly on the counters. Instead, the part of each
    /// counter that stores the base arm lengths (and only this part) has counter
    /// indices already stored in the amoebots. These indices are then used to look up
    /// the bits of the base arm lengths in the snowflake info.
    /// </para>
    /// <para>
    /// There are some requirements that must be met for the algorithm to work:
    /// <list type="bullet">
    ///     <item>The shape must not be a single node.</item>
    ///     <item>The number of nodes, number of different arm lengths and
    ///     maximum arm length must be at most 255.</item>
    ///     <item>The counters must have sufficient space to store the scaled
    ///     arm lengths. This can be achieved by using the longest line length of
    ///     the shape to limit the scale factor first.</item>
    /// </list>
    /// </para>
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge. This subroutine sometimes uses all pins simultaneously.
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
    ///     whether the procedure is finished. If it is, you can check whether it was successful by calling
    ///     <see cref="Success"/>, find the lowest valid rotation with <see cref="LowestValidRotation"/>, find
    ///     all valid rotations with <see cref="ValidRotations"/>, and check whether an amoebot is a representative
    ///     for a given rotation with <see cref="IsRepresentative(int)"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>

    // Algorithm plan:
    //  1. Compute the scaled arm lengths
    //      - Counter amoebots know the scale factor and all arm length bits
    //      - Also need to find the maximum length's MSB
    //      - Do this in a loop while the other amoebots wait
    //      - When finished, beep on global circuit to continue
    //  2. Measure distances in all 6 directions
    //      - Run 6 PASC instances at the same time
    //      - After each PASC iteration, send the bits of the scaled arm lengths on global circuits
    //          - Use as many rounds for this as necessary
    //          - All amoebots update comparison results while doing this
    //      - In the final round of the iteration, send MSB and PASC continuation beeps and forward the marker
    //      - Finish when MSB of the arm lengths has been reached (terminate if PASC is finished before that)
    //          - Potential PASC cutoff
    //      - At the end, each amoebot knows for all 6 directions which arm lengths fit and which do not
    //  3. For each node v of the dependency tree in the topological order:
    //      3.1. Setup 6 candidate sets C(v) for the 6 rotations, choosing only the amoebots with matching arm lengths
    //          - Setup a global circuit and let amoebots in these sets beep
    //          - If no beep is sent on the global circuit, terminate with failure!
    //      3.2. For each direction in which the node v has children:
    //          3.2.1. Find out where we have to run the next step
    //              - Setup 6 axis circuits and let the candidates beep on the axes where they have the children
    //              - Use the axis circuits to find the segments on which we have to run this step
    //                  - Establish empty elimination segments for these directions
    //              - The other amoebots participate in the synchronization but nothing else
    //          3.2.2. For k = largest child distance in the current direction, ..., 0:
    //              - If v has at least one child at this distance:
    //                  - Identify invalid placements for the children at distance k, let this set be Q for each rotation
    //                  - Establish chain circuits and let Q beep towards the origin to identify the PASC participants
    //                  - Init PASC on these segments
    //                  - Let Q eliminate segments of length = scale factor using PASC
    //                      - Early termination if PASC is finished everywhere or MSB is reached + cutoff
    //                  - Merge the eliminated segments with the current elimination segments
    //              - If k > 0:
    //                  - Shift the elimination segments by the scale factor
    //                  - Decrement k and continue
    //              - If k = 0:
    //                  - Break and go to next step
    //          3.2.3. Remove candidates that are in an elimination segment
    //              - Setup global circuit and let the candidates beep
    //              - If no beep is received: Terminate
    //              - Increment direction
    //      3.3. Store the remaining candidates in the corresponding valid placement matrix entries
    //  4. If we have not terminated yet:
    //      - Use two rounds and let valid placements beep on global circuits to determine the smallest rotation for which the shape matches
    //          - Can even store all rotations
    //      - Terminate with success and report the smallest rotation as well as its valid placement
    //          - Same here: Can even report all valid placements

    // Round plan:

    // Init:
    //  - Store counter index (can be computed outside of the subroutine!)
    //  - Set generic counter to 0


    // 1. Compute scaled arm lengths

    // Round 0:
    //  Send:
    //  - If counter is > number of arm lengths:
    //      - Setup global circuit and beep
    //      - Go to round 2
    //  - Else:
    //      - Amoebots on the binary counter start the next binary operation and go to round 1
    //          - First operations are just multiplying arm length by scale factor
    //          - Final operation is finding MSB of largest arm length
    //      - Others go to round 2
    //          - Establish global circuit and wait
    //          - (This will only happen the first time we enter this round)

    // Round 1:
    //  Receive:
    //  - Receive binop beeps
    //  - If binop is finished:
    //      - Store result
    //      - Increment generic counter
    //      - Go back to round 0
    //  Send:
    //  - Continue running binary operation

    // Round 2:
    //  Receive:
    //  - Wait for beep on global circuit
    //  - If received:
    //      - Reset generic counter
    //      - Set marker to binary counter start
    //      - Init 6 PASC instances
    //      - Set all comparison results to EQUAL (Should already be the case after Init())
    //      - Go to round 3


    // 2. Measure distances in all 6 directions

    // Round 3:
    //  Receive ( * ):
    //  - Receive PASC continuation, MSB and marker beep
    //  - Update marker flag
    //  - If no PASC beep:
    //      - If MSB beep:
    //          - (All amoebots have the correct comparison results)
    //          - Set node index to 0 (should already be the case)
    //          - Go to round 7
    //      - If no MSB beep:
    //          - Terminate with failure (No valid placements)
    //  - If PASC beep:
    //      - If MSB beep:
    //          - (Start PASC cutoff)
    //          - Go to round 6
    //  Send:
    //  - Send PASC beep
    //  - Go to round 4

    // Round 4:
    //  Receive:
    //  - Receive PASC beep
    //  - Set counter to 0
    //  - Go to round 5

    // Round 5:
    //  Send:
    //  - If counter < number of arm lengths:
    //      - Setup 4 global circuits
    //      - Let marker send bits of the (up to) 4 next arm lengths
    //      - Increment counter by 4
    //  - Else:
    //      - Setup 2 (3) global circuits
    //      - Send PASC continuation beep on first circuit (have become passive)
    //      - Marker sends its MSB flag on second circuit
    //      - Send marker forwarding beep (unless we have reached the MSB)
    //      - Go to round 3
    //  Receive:
    //  - Receive bits on 4 global circuits
    //  - Update comparison results

    // Round 6:
    //  Send:
    //  - Send PASC cutoff beep
    //  Receive:
    //  - Receive PASC cutoff beep
    //  - Update final comparison results (set all comparisons to GREATER if we received a cutoff beep)
    //  - Set node index to 0 (should already be the case)
    //  - Go to round 7


    // 3. Iterate through topological ordering and find valid placements of each node

    // 3.1. Determine initial candidate sets

    // Round 7:
    //  Send:
    //  - If node index >= num nodes:
    //      - (We are finished!)
    //      - Setup 4 global circuits
    //      - Let valid placements of the first 4 rotations beep on them
    //      - Go to round 21
    //  - Else:
    //      - Initialize the 6 candidate sets using the stored arm lengths
    //      - Setup a global circuit and let the candidates beep
    //      - Go to round 8

    // Round 8:
    //  Receive:
    //  - Receive candidate beeps on global circuit
    //  - If no beep received:
    //      - Terminate with failure (have no candidates left)
    //  - Else:
    //      - Set direction counter to 0
    //      - Go to round 9

    // 3.2. Check stretched children in each direction
    // 3.2.1. Find out where we have to run the next step
    // Also 3.3. Store the remaining candidates in the corresponding valid placement matrix entries (after going through all directions)

    // Round 9:
    //  Send:
    //  - If direction counter >= 6:
    //      - (Have checked children of all directions)
    //      - Store remaining valid placements
    //      - Increment node index
    //      - Go back to round 7
    //  - Else:
    //      - If the current node has no children in the current direction:
    //          - Increment direction counter and stay in this round
    //      - Else:
    //          - Set distance (generic) counter to max. child distance
    //          - Establish 6 axis circuits and let candidates with children in the current direction beep
    //          - Go to round 10

    // Round 10:
    //  Receive:
    //  - Receive beep on axis circuits
    //  - Identify the segments on which we have to run the next procedure
    //      - Set "candidate on segment" flag
    //      - Also reset elimination segments
    //  - Go to round 11

    // 3.2.2. Check children at each distance

    // Round 11:
    //  Send:
    //  - If the current node has at least one child at the current distance:
    //      - Determine all invalid placements of any of these children
    //          - Only on the active segments and for the right directions
    //      - Setup chain circuits, split by the invalid placements
    //      - Let invalid placements beep towards the shape's origin
    //      - Go to round 12
    //  - Else:
    //      - (Skip the elimination distance part)
    //      - Go to round 18

    // Round 12:
    //  Receive:
    //  - Receive axis beeps (only on active segments)
    //  - Initialize PASC where the beeps were received
    //      - Reset temporary comparison results
    //  - Place marker at counter start
    //  - Go to round 13

    // Round 13:
    //  Send:
    //  - PASC participants send beep
    //  - Go to round 14

    // Round 14:
    //  Receive:
    //  - Receive PASC beep
    //  Send:
    //  - Setup 3 global circuits
    //  - Marker sends scale bit on first circuit, scale MSB on second and marker beep to successor (unless it is the MSB)
    //  - Send PASC continuation beep on third circuit
    //  - Go to round 15

    // Round 15 (similar to 3):
    //  Receive:
    //  - Receive PASC continuation, MSB and marker beep
    //  - Update marker flag
    //  - Update comparison result
    //  - If no PASC beep:
    //      - If MSB beep:
    //          - (All amoebots have the correct comparison results)
    //          - Go to round 17
    //      - If no MSB beep:
    //          - Set all comparison results to LESS
    //          - Go to round 17
    //  - If PASC beep:
    //      - If MSB beep:
    //          - (Start PASC cutoff)
    //          - Go to round 16
    //  Send:
    //  - Send PASC beep
    //  - Go to round 14

    // Round 16:
    //  Send:
    //  - Send PASC cutoff beep
    //  Receive:
    //  - Receive PASC cutoff beep
    //  - Update comparison result
    //  - Go to round 17

    // Round 17:
    //  Send:
    //  - (Have final comparison results)
    //  - Update segment status based on comparison result
    //  - Go to round 18

    // Start segment shift

    // Round 18:
    //  Receive:
    //  - If distance counter is > 0:
    //      - (Start segment shift)
    //      - Init shifting subroutine
    //      - *All amoebots have to do this, also the ones waiting* (for synchronization)
    //      - Go to round 19
    //  - Else:
    //      - (Break and go to next step)
    //      - Go to round 20

    // Round 19:
    //  Receive ( * ):
    //  - Receive shift subroutine beep
    //  - If finished:
    //      - Update elimination segments
    //      - If current distance is 0:
    //          - Go to round 20
    //      - Else:
    //          - Decrement distance
    //          - Go to round 11
    //  Send:
    //  - Send shift subroutine beep


    // 3.2.3. Remove candidates that are in an elimination segment

    // Round 20:
    //  Send:
    //  - Candidates in elimination segments retire
    //  - Setup global circuit
    //  - Candidates beep on the global circuit
    //  Receive:
    //  - Receive beep on global circuit
    //  - If no beep:
    //      - There are no candidates left for this node
    //      - Terminate with failure
    //  - Otherwise:
    //      - Increment direction counter
    //      - Go to round 9


    // 4. If we have not terminated yet: Find the valid rotations and placements and terminate

    // Round 21:
    //  Receive:
    //  - Receive beeps on 4 global circuits
    //  - Update valid rotation flags
    //  Send:
    //  - Setup global circuits again
    //  - Let valid placements of the last two rotations beep on the first two circuits
    //  - Go to round 22

    // Round 22:
    //  Receive:
    //  - Receive last two rotation beeps and update final result
    //  - Terminate with success or failure (based on whether there are any valid rotations left)

    public class SubSnowflakePlacementSearch : Subroutine
    {
        enum ComparisonResult
        {
            EQUAL = 0,
            LESS = 1,
            GREATER = 2
        }

        //       28         27  22            21              20 16         15        14          13         12 10    9 7     6               5        4   0
        // xxx   x          xxxxxx            x               xxxxx         x         x           x           xxx     xxx     x               x        xxxxx
        //       Finished   Final rotations   Control color   Counter Idx   Arm MSB   Scale MSB   Scale Bit   Succ.   Pred.   Counter valid   Marker   Round
        ParticleAttribute<int> state1;

        //     30  25          24  19     18 16       15     8   7      0
        // x   xxxxxx          xxxxxx      xxx        xxxxxxxx   xxxxxxxx
        //     Cand. Segment   Candidate   Rot. Idx   Node Idx   Gen. Counter
        ParticleAttribute<int> state2;

        //             23             12   11   6          5    0
        // xxxx xxxx   xx xx xx xx xx xx   xxxxxx          xxxxxx
        //             Comp.               Elim. Segment   PASC Part.
        ParticleAttribute<int> state3;

        // One bit for each scaled arm length, add one extra int each 32 lengths (stored in bit fields of size 32)
        ParticleAttribute<int>[] statesArmLengthBits;
        // 6 bits for the 6 rotations of each node, stored tightly in size 32 bit fields
        ParticleAttribute<int>[] statesNodePlacements;
        // 6 comparison results for the rotations of each arm length
        ParticleAttribute<int>[] statesArmLengthComparisons;

        BinAttributeInt round;                              // Round counter

        BinAttributeBool controlColor;                      // Control amoebot color flag
        BinAttributeBool counterIndexValid;                 // Whether we are on the part of a binary counter that stores the arm lengths
        BinAttributeDirection counterPred;                  // Counter predecessor and successor directions
        BinAttributeDirection counterSucc;
        BinAttributeBool scaleBit;                          // Our bit of the scale factor (on the counter)
        BinAttributeBool scaleMSB;                          // Whether we hold the scale's MSB (on the counter)
        BinAttributeBool armLengthMSB;                      // Whether we hold the scaled arm length MSB (on the counter)
        BinAttributeInt counterIndex;                       // Our position on the counter (only up to arm length MSB) up to 31

        BinAttributeBool marker;                            // Marker flag for the counter

        BinAttributeInt genericCounter;                     // A generic counter (distance, number of arm lengths etc.) up to 255
        BinAttributeInt nodeIndex;                          // A counter for the node indices in the topological ordering up to 255
        BinAttributeInt rotationIndex;                      // A counter for the rotation up to 5

        BinAttributeBitField candidate;                     // Candidate flags for the 6 rotations, used to compute the valid placements for each node
        BinAttributeBitField onCandidateSegment;            // Indicators for the 6 rotations whether there is a candidate on this segment
        BinAttributeBitField pascParticipant;               // PASC participation flags for the 6 rotations
        BinAttributeBitField eliminationSegment;            // Elimination segment flags for the 6 rotations
        BinAttributeEnum<ComparisonResult>[] comparisons = new BinAttributeEnum<ComparisonResult>[6];   // Temporary comparison results for the 6 rotations

        BinAttributeBitField[] armLengthBits;               // Bit fields storing the scaled arm length bits (32 bits, get extra entries every 32 arm lengths)
        BinAttributeBitField[] nodePlacements;              // Bit fields storing the valid placement flags for all nodes and rotations (same principle as above)
        BinAttributeEnum<ComparisonResult>[] armLengthComparisons;  // Comparison results for all scaled arm lengths in all 6 directions

        BinAttributeBool finished;                          // Whether the procedure is finished
        BinAttributeBitField finalRotations;                // The 6 rotations for which we found valid placements


        SubPASC2[] pasc = new SubPASC2[6];
        SubBinOps binop;
        SubSegmentShift[] segmentShift = new SubSegmentShift[6];

        SnowflakeInfo snowflakeInfo;

        public SubSnowflakePlacementSearch(Particle p, SnowflakeInfo snowflakeInfo,
            SubBinOps binop = null, SubPASC2[] pasc = null) : base(p)
        {
            this.snowflakeInfo = snowflakeInfo;

            state1 = algo.CreateAttributeInt(FindValidAttributeName("[SFC] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[SFC] State 2"), 0);
            state3 = algo.CreateAttributeInt(FindValidAttributeName("[SFC] State 3"), 0);

            // Variable attributes
            int numArmLengths = snowflakeInfo.armLengths.Length;
            int numNodes = snowflakeInfo.nodes.Length;

            int numArmLengthBitFields = (numArmLengths / 32) + (numArmLengths % 32 > 0 ? 1 : 0);
            statesArmLengthBits = new ParticleAttribute<int>[numArmLengthBitFields];
            armLengthBits = new BinAttributeBitField[numArmLengthBitFields];
            for (int i = 0; i < numArmLengthBitFields; i++)
            {
                statesArmLengthBits[i] = algo.CreateAttributeInt(FindValidAttributeName("[SFC] Arm Bits_" + i + "_"), 0);
                armLengthBits[i] = new BinAttributeBitField(statesArmLengthBits[i], 0, 32);
            }

            int numNodePlacementBitFields = (6 * numNodes / 32) + ((6 * numNodes) % 32 > 0 ? 1 : 0);
            statesNodePlacements = new ParticleAttribute<int>[numNodePlacementBitFields];
            nodePlacements = new BinAttributeBitField[numNodePlacementBitFields];
            for (int i = 0; i < numNodePlacementBitFields; i++)
            {
                statesNodePlacements[i] = algo.CreateAttributeInt(FindValidAttributeName("[SFC] Node Bits_" + i + "_"), 0);
                nodePlacements[i] = new BinAttributeBitField(statesNodePlacements[i], 0, 32);
            }

            int numComparisonBits = 2 * 6 * numArmLengths;
            int numComparisonStates = (numComparisonBits / 32) + (numComparisonBits % 32 > 0 ? 1 : 0);
            statesArmLengthComparisons = new ParticleAttribute<int>[numComparisonStates];
            for (int i = 0; i < numComparisonStates; i++)
            {
                statesArmLengthComparisons[i] = algo.CreateAttributeInt(FindValidAttributeName("[SFC] Arm Comps_" + i + "_"), 0);
            }
            armLengthComparisons = new BinAttributeEnum<ComparisonResult>[6 * numArmLengths];
            for (int i = 0; i < 6 * numArmLengths; i++)
            {
                int stateIdx = i / 16;
                int stateOffset = 2 * (i % 16);
                armLengthComparisons[i] = new BinAttributeEnum<ComparisonResult>(statesArmLengthComparisons[stateIdx], stateOffset, 2);
            }

            // State 1 binary attributes
            round = new BinAttributeInt(state1, 0, 5);
            marker = new BinAttributeBool(state1, 5);
            counterIndexValid = new BinAttributeBool(state1, 6);
            counterPred = new BinAttributeDirection(state1, 7);
            counterSucc = new BinAttributeDirection(state1, 10);
            scaleBit = new BinAttributeBool(state1, 13);
            scaleMSB = new BinAttributeBool(state1, 14);
            armLengthMSB = new BinAttributeBool(state1, 15);
            counterIndex = new BinAttributeInt(state1, 16, 5);
            controlColor = new BinAttributeBool(state1, 21);
            finalRotations = new BinAttributeBitField(state1, 22, 6);
            finished = new BinAttributeBool(state1, 28);

            // State 2 binary attributes
            genericCounter = new BinAttributeInt(state2, 0, 8);
            nodeIndex = new BinAttributeInt(state2, 8, 8);
            rotationIndex = new BinAttributeInt(state2, 16, 3);
            candidate = new BinAttributeBitField(state2, 19, 6);
            onCandidateSegment = new BinAttributeBitField(state2, 25, 6);

            // State 3 binary attributes
            pascParticipant = new BinAttributeBitField(state3, 0, 6);
            eliminationSegment = new BinAttributeBitField(state3, 6, 6);
            for (int i = 0; i < 6; i++)
            {
                comparisons[i] = new BinAttributeEnum<ComparisonResult>(state3, 12 + 2 * i, 2);
            }

            // Subroutines
            if (pasc is null)
                pasc = new SubPASC2[6];
            for (int i = 0; i < 6; i++)
            {
                if (i < pasc.Length && !(pasc[i] is null))
                    this.pasc[i] = pasc[i];
                else
                    this.pasc[i] = new SubPASC2(p);
            }
            if (binop is null)
                this.binop = new SubBinOps(p);
            else
                this.binop = binop;
            for (int i = 0; i < 6; i++)
                segmentShift[i] = new SubSegmentShift(p, this.pasc[i]);
        }

        /// <summary>
        /// Initializes the procedure. Assumes that a binary counter stores the
        /// scale factor bits and its MSB and a part of this counter has enough
        /// space to store the base arm lengths. Each amoebot in this part of
        /// the counter must know its position in the counter for easy access to
        /// these bits without storing the bits themselves (since they are
        /// available in the static shape description).
        /// </summary>
        /// <param name="controlColor">Whether the subroutine should control the amoebot color.</param>
        /// <param name="onBaseArmCounter">Whether the amoebot is part of the counter that
        /// stores the base arm lengths.</param>
        /// <param name="counterIndex">The index of this amoebot in the counter part that stores the
        /// base arm lengths. Must be between <c>0</c> and <c>31</c>.</param>
        /// <param name="counterPred">The predecessor direction of the counter.
        /// Should be <see cref="Direction.NONE"/> for the counter start and amoebots
        /// that are not on the counter.</param>
        /// <param name="counterSucc">The successor direction of the counter.
        /// Should be <see cref="Direction.NONE"/> for the counter end and amoebots
        /// that are not on the counter.</param>
        /// <param name="scaleBit">If the amoebot is on the counter, stores the bit
        /// of the scale <c>k</c>.</param>
        /// <param name="scaleMSB">Whether this is the MSB of <c>k</c> if the amoebot
        /// is on the counter.</param>
        public void Init(bool controlColor = false,
            // Counter setup
            bool onBaseArmCounter = false, int counterIndex = 0, Direction counterPred = Direction.NONE, Direction counterSucc = Direction.NONE,
            bool scaleBit = false, bool scaleMSB = false)
        {
            // Reset entire state
            state1.SetValue(0);
            state2.SetValue(0);
            state3.SetValue(0);
            foreach (ParticleAttribute<int> a in statesArmLengthBits)
                a.SetValue(0);
            foreach (ParticleAttribute<int> a in statesNodePlacements)
                a.SetValue(0);
            foreach (ParticleAttribute<int> a in statesArmLengthComparisons)
                a.SetValue(0);

            this.controlColor.SetValue(controlColor);
            if (counterPred != Direction.NONE || counterSucc != Direction.NONE)
            {
                this.counterIndexValid.SetValue(onBaseArmCounter);
                this.counterIndex.SetValue(counterIndex);
                this.counterPred.SetValue(counterPred);
                this.counterSucc.SetValue(counterSucc);
                this.scaleBit.SetValue(scaleBit);
                this.scaleMSB.SetValue(scaleMSB);
            }
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
                        // Counter amoebots receive binop beep (others do not reach this round)
                        binop.ActivateReceive();
                        if (binop.IsFinished())
                        {
                            // Process result
                            int ctr = genericCounter.GetCurrentValue();
                            if (ctr < snowflakeInfo.armLengths.Length)
                            {
                                // Multiplication result
                                SetScaledArmLengthBit(ctr, binop.ResultBit());
                            }
                            else
                            {
                                // MSB detection
                                armLengthMSB.SetValue(binop.IsMSB());
                            }
                            genericCounter.SetValue(ctr + 1);
                            round.SetValue(r - 1);
                        }
                    }
                    break;
                case 2:
                    {
                        // Wait for beep on global circuit
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Received beep: Continue with next phase
                            genericCounter.SetValue(0);
                            SetMarkerToCounterStart();
                            // Initialize all 6 PASC instances for the 6 rotations
                            for (int d = 0; d < 6; d++)
                            {
                                Direction dirOpp = DirectionHelpers.Cardinal(d);
                                Direction dir = dirOpp.Opposite();
                                bool leader = !algo.HasNeighborAt(dirOpp);
                                pasc[d].Init(leader ? null : new List<Direction>() { dirOpp }, new List<Direction>() { dir }, 0, 1, 2 * d, 2 * d + 1, leader);
                            }
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 3:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Receive PASC continuation on global circuit 0
                        // Receive arm length MSB on global circuit 1
                        bool pascContinue = pc.ReceivedBeepOnPartitionSet(0);
                        bool armMSB = pc.ReceivedBeepOnPartitionSet(1);
                        
                        ForwardMarker(pc);

                        if (!pascContinue)
                        {
                            if (armMSB)
                            {
                                // All amoebots already have the correct comparison results
                                // Go to next phase
                                marker.SetValue(false);
                                round.SetValue(7);
                            }
                            else
                            {
                                // Terminate with failure because we have no valid placements
                                finished.SetValue(true);
                                marker.SetValue(false);
                            }
                        }
                        else
                        {
                            if (armMSB)
                            {
                                // Start PASC cutoff
                                round.SetValue(6);
                            }
                        }
                    }
                    break;
                case 4:
                    {
                        // Receive PASC beep
                        for (int i = 0; i < 6; i++)
                            pasc[i].ActivateReceive();

                        // Start iterating through arm length bits
                        genericCounter.SetValue(0);
                        round.SetValue(r + 1);
                    }
                    break;
                case 5:
                    {
                        // Receive up to 4 beeps on global circuits
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        bool[] pascBits = new bool[6];
                        for (int i = 0; i < 6; i++)
                            pascBits[i] = pasc[i].GetReceivedBit() > 0;

                        int ctr = genericCounter.GetCurrentValue();
                        for (int i = ctr - 4; i < ctr && i < snowflakeInfo.armLengths.Length; i++)
                        {
                            bool distBit = pc.ReceivedBeepOnPartitionSet(i - ctr + 4);
                            for (int j = 0; j < 6; j++)
                            {
                                if (pascBits[j] && !distBit)
                                    SetArmLengthComp(i, j, ComparisonResult.GREATER);
                                else if (!pascBits[j] && distBit)
                                    SetArmLengthComp(i, j, ComparisonResult.LESS);
                            }
                        }
                    }
                    break;
                case 6:
                    {
                        // Receive PASC cutoff beep
                        // and set all comparison results to GREATER if we received a 1-bit
                        for (int i = 0; i < 6; i++)
                        {
                            pasc[i].ReceiveCutoffBeep();
                            if (pasc[i].GetReceivedBit() > 0)
                            {
                                for (int j = 0; j < snowflakeInfo.armLengths.Length; j++)
                                    SetArmLengthComp(j, i, ComparisonResult.GREATER);
                            }
                        }
                        // Continue with next phase
                        round.SetValue(r + 1);
                    }
                    break;
                case 8:
                    {
                        // Receive beep on global circuit to verify existence of candidates
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            finished.SetValue(true);
                        }
                        else
                        {
                            // Reset rotation counter to 0 and start check procedure
                            rotationIndex.SetValue(0);
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 10:
                    {
                        // Receive beeps on axis circuits
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Identify the segments on which we have to run the next procedure
                        // Also reset elimination segments
                        for (int d = 0; d < 6; d++)
                        {
                            onCandidateSegment.SetValue(d, pc.ReceivedBeepOnPartitionSet(d));
                            eliminationSegment.SetValue(d, false);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 12:
                    {
                        // Receive axis beeps on active segments and initialize PASC
                        // where the beeps were received (or sent)
                        if (onCandidateSegment.GetCurrentOr())
                        {
                            PinConfiguration pc = algo.GetCurrentPinConfiguration();
                            bool[] invalidRotations = GetInvalidPlacementRotations();
                            int rot = rotationIndex.GetCurrentValue();
                            for (int d = 0; d < 6; d++)
                            {
                                if (!onCandidateSegment.GetCurrentValue(d))
                                {
                                    pascParticipant.SetValue(d, false);
                                    continue;
                                }
                                bool leader = invalidRotations[(d + 6 - rot) % 6];
                                Direction dir = DirectionHelpers.Cardinal(d);
                                if (leader)
                                {
                                    pascParticipant.SetValue(d, true);
                                    pasc[d].Init(null, new List<Direction>() { dir.Opposite() }, 0, 1, 2 * d, 2 * d + 1, leader);
                                }
                                else if (pc.GetPinAt(dir, 0).PartitionSet.ReceivedBeep())
                                {
                                    pascParticipant.SetValue(d, true);
                                    pasc[d].Init(new List<Direction>() { dir }, new List<Direction>() { dir.Opposite() }, 0, 1, 2 * d, 2 * d + 1, false);
                                }
                                else
                                {
                                    pascParticipant.SetValue(d, false);
                                }
                            }
                        }
                        // Reset temporary comparison results
                        for (int d = 0; d < 6; d++)
                            comparisons[d].SetValue(ComparisonResult.EQUAL);

                        SetMarkerToCounterStart();
                        round.SetValue(r + 1);
                    }
                    break;
                case 14:
                    {
                        // Receive PASC beeps
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                                pasc[d].ActivateReceive();
                        }
                    }
                    break;
                case 15:
                    {
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        // Receive scale bit, MSB and PASC continuation beeps on global circuit
                        bool beepScaleBit = pc.ReceivedBeepOnPartitionSet(0);
                        bool beepScaleMSB = pc.ReceivedBeepOnPartitionSet(1);
                        bool pascContinue = pc.ReceivedBeepOnPartitionSet(2);

                        ForwardMarker(pc);

                        // Update comparison results
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                            {
                                bool pascBit = pasc[d].GetReceivedBit() > 0;
                                if (pascBit && !beepScaleBit)
                                    comparisons[d].SetValue(ComparisonResult.GREATER);
                                else if (beepScaleBit && !pascBit)
                                    comparisons[d].SetValue(ComparisonResult.LESS);
                            }
                        }

                        if (!pascContinue)
                        {
                            // All amoebots have the correct comparison results in this round
                            // Go to next phase
                            if (!beepScaleMSB)
                            {
                                // Set all comparison results to LESS
                                for (int d = 0; d < 6; d++)
                                {
                                    if (pascParticipant.GetCurrentValue(d))
                                        comparisons[d].SetValue(ComparisonResult.LESS);
                                }
                            }
                            marker.SetValue(false);
                            round.SetValue(17);
                        }
                        else
                        {
                            if (beepScaleMSB)
                            {
                                // Start PASC cutoff
                                round.SetValue(16);
                            }
                        }
                    }
                    break;
                case 16:
                    {
                        // Receive PASC cutoff beep and update comparison result
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                            {
                                pasc[d].ReceiveCutoffBeep();
                                if (pasc[d].GetReceivedBit() > 0)
                                    comparisons[d].SetValue(ComparisonResult.GREATER);
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 18:
                    {
                        if (genericCounter.GetCurrentValue() > 0)
                        {
                            // Start segment shift
                            for (int d = 0; d < 6; d++)
                            {
                                Direction dir = DirectionHelpers.Cardinal((d + 3) % 6);
                                segmentShift[d].Init(eliminationSegment.GetCurrentValue(d), dir, 2 * d, 2 * d + 1, 12, counterPred.GetValue(), counterSucc.GetValue(), scaleBit.GetValue(), scaleMSB.GetValue());
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Skip the segment shift
                            round.SetValue(r + 2);
                        }
                    }
                    break;
                case 19:
                    {
                        // Run segment shifting
                        for (int d = 0; d < 6; d++)
                            segmentShift[d].ActivateReceive();
                        // All segment shifting subroutines will finish at the same time
                        if (segmentShift[0].IsFinished())
                        {
                            // Update elimination segments
                            for (int d = 0; d < 6; d++)
                            {
                                eliminationSegment.SetValue(d, segmentShift[d].IsOnNewSegment());
                            }

                            int dist = genericCounter.GetCurrentValue();
                            if (dist == 0)
                            {
                                // Finished with segment shifting, go to next phase
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                // Decrement distance counter and continue with next distance
                                genericCounter.SetValue(dist - 1);
                                round.SetValue(11);
                            }
                        }
                    }
                    break;
                case 20:
                    {
                        // Receive candidate beep on global circuit
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Terminate with failure (no candidates left for the current node)
                            finished.SetValue(true);
                        }
                        else
                        {
                            // Increment direction and go back to round 9
                            rotationIndex.SetValue(rotationIndex.GetCurrentValue() + 1);
                            round.SetValue(9);
                        }
                    }
                    break;
                case 21:
                    {
                        // Receive beeps on 4 global circuits
                        // Store the valid final rotation flags
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        for (int i = 0; i < 4; i++)
                        {
                            if (pc.ReceivedBeepOnPartitionSet(i))
                                finalRotations.SetValue(i, true);
                        }
                    }
                    break;
                case 22:
                    {
                        // Receive the remaining two rotation beeps
                        PinConfiguration pc = algo.GetCurrentPinConfiguration();
                        for (int i = 0; i < 2; i++)
                        {
                            if (pc.ReceivedBeepOnPartitionSet(i))
                                finalRotations.SetValue(i + 4, true);
                        }
                        finished.SetValue(true);
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
                        int ctr = genericCounter.GetCurrentValue();
                        if (ctr > snowflakeInfo.armLengths.Length)
                        {
                            // Have finished initial arm length computations: Setup global circuit
                            pc.SetToGlobal(0);
                            pc.SetPartitionSetColor(0, ColorData.Circuit_Colors[0]);
                        }
                        else
                        {
                            // Amoebots on the binary counter start the next binary operation
                            if (IsOnCounter())
                            {
                                // First operations are arm length multiplications
                                if (ctr < snowflakeInfo.armLengths.Length)
                                {
                                    binop.Init(SubBinOps.Mode.MULT, scaleBit.GetCurrentValue(), counterPred.GetCurrentValue(), counterSucc.GetCurrentValue(), GetArmLengthBit(ctr), scaleMSB.GetCurrentValue());
                                }
                                // Last operation is MSB detection of final arm length
                                else
                                {
                                    binop.Init(SubBinOps.Mode.MSB, GetScaledArmLengthBit(snowflakeInfo.armLengths.Length - 1), counterPred.GetCurrentValue(), counterSucc.GetCurrentValue());
                                }
                                binop.SetupPinConfig(pc);
                            }
                            // Others go to waiting round 2
                            else
                            {
                                pc.SetToGlobal(0);
                                round.SetValue(2);
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        // Just setup binary operation circuit
                        binop.SetupPinConfig(pc);
                    }
                    break;
                case 2:
                    {
                        // Waiting round: Setup global pin config
                        pc.SetToGlobal(0);
                        pc.SetPartitionSetColor(0, ColorData.Circuit_Colors[0]);
                    }
                    break;
                case 3:
                    {
                        // Setup all PASC circuits
                        for (int i = 0; i < 6; i++)
                            pasc[i].SetupPC(pc);
                    }
                    break;
                case 5:
                    {
                        if (genericCounter.GetCurrentValue() < snowflakeInfo.armLengths.Length)
                        {
                            // Setup 4 global circuits for sending arm length bits
                            SetupGlobalCircuits(pc, 4);
                        }
                        else
                        {
                            // Setup 3 global circuits for PASC iteration end
                            SetupGlobalCircuits(pc, 3);
                        }
                    }
                    break;
                case 6:
                    {
                        // Setup PASC cutoff circuit
                        for (int i = 0; i < 6; i++)
                            pasc[i].SetupCutoffCircuit(pc);
                    }
                    break;
                case 7:
                    {
                        if (nodeIndex.GetCurrentValue() >= snowflakeInfo.nodes.Length)
                        {
                            // Finished: Setup 4 global circuits
                            SetupGlobalCircuits(pc, 4);
                        }
                        else
                        {
                            // Just setup one global circuit
                            pc.SetToGlobal(0);
                        }
                    }
                    break;
                case 9:
                    {
                        // Setup 6 axis circuits if the current node has children in the current direction
                        int rot = rotationIndex.GetCurrentValue();
                        if (rot < 6 && HasChildInDirection(nodeIndex.GetCurrentValue(), rot))
                        {
                            SetupAxisCircuits(pc);
                        }
                    }
                    break;
                case 11:
                    {
                        // Setup axis circuits split at invalid placements of the child shapes if
                        // the current shape has any children at the current distance
                        int rot = rotationIndex.GetCurrentValue();
                        if (HasChildAtDistance(nodeIndex.GetCurrentValue(), rot, genericCounter.GetCurrentValue()))
                        {
                            bool[] invalidRotations = GetInvalidPlacementRotations();
                            // Translate the rotations into directions where we have to split
                            // Also split on intervals where no beeps are necessary
                            bool[] split = new bool[6];
                            for (int d = 0; d < 6; d++)
                            {
                                if (invalidRotations[(d + 6 - rot) % 6] || !onCandidateSegment.GetCurrentValue(d))
                                    split[d] = true;
                            }
                            SetupAxisCircuits(pc, split);
                        }
                    }
                    break;
                case 13:
                    {
                        // PASC participants setup circuits
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                                pasc[d].SetupPC(pc);
                        }
                    }
                    break;
                case 14:
                    {
                        // Setup 3 global circuits for PASC coordination
                        SetupGlobalCircuits(pc, 3);
                    }
                    break;
                case 15:
                    {
                        // Setup PASC config
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                                pasc[d].SetupPC(pc);
                        }
                    }
                    break;
                case 16:
                    {
                        // Setup PASC cutoff
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                                pasc[d].SetupCutoffCircuit(pc);
                        }
                    }
                    break;
                case 19:
                    {
                        // Setup segment shifting
                        for (int d = 0; d < 6; d++)
                            segmentShift[d].SetupPC(pc);
                    }
                    break;
                case 20:
                    {
                        // Setup global circuit
                        pc.SetToGlobal(0);
                    }
                    break;
                case 21:
                    {
                        // 2 global circuits to transmit remaining valid rotations
                        SetupGlobalCircuits(pc, 2);
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
                        int ctr = genericCounter.GetCurrentValue();
                        if (ctr <= snowflakeInfo.armLengths.Length)
                        {
                            // Send binop beep if we are not finished yet
                            binop.ActivateSend();
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Send beep on global circuit for waiting amoebots when we are finished
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(0);
                            round.SetValue(2);
                        }
                    }
                    break;
                case 1:
                    {
                        // Send binop beep
                        binop.ActivateSend();
                    }
                    break;
                case 3:
                    {
                        // Send PASC beeps
                        for (int i = 0; i < 6; i++)
                            pasc[i].ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 5:
                    {
                        int ctr = genericCounter.GetCurrentValue();
                        if (ctr < snowflakeInfo.armLengths.Length)
                        {
                            // Marker sends bits of the next up to 4 arm lengths
                            if (marker.GetCurrentValue())
                            {
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                for (int i = ctr; i < ctr + 4 && i < snowflakeInfo.armLengths.Length; i++)
                                {
                                    if (GetScaledArmLengthBit(i))
                                        pc.SendBeepOnPartitionSet(i - ctr);
                                }
                            }
                            // Increment counter
                            genericCounter.SetValue(ctr + 4);
                        }
                        else
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            // Send PASC continuation beep on the first circuit
                            for (int i = 0; i < 6; i++)
                            {
                                if (pasc[i].BecamePassive())
                                {
                                    pc.SendBeepOnPartitionSet(0);
                                    break;
                                }
                            }

                            // Marker sends MSB flag on second circuit
                            // Marker also sends forwarding beep unless it is the MSB
                            if (marker.GetCurrentValue())
                            {
                                if (armLengthMSB.GetCurrentValue())
                                    pc.SendBeepOnPartitionSet(1);
                                else
                                {
                                    Direction d = counterSucc.GetValue();
                                    pc.GetPinAt(d, GetMarkerPin(true, d)).PartitionSet.SendBeep();
                                }
                            }
                            round.SetValue(r - 2);
                        }
                    }
                    break;
                case 6:
                    {
                        // Send PASC cutoff beep
                        for (int i = 0; i < 6; i++)
                            pasc[i].SendCutoffBeep();
                    }
                    break;
                case 7:
                    {
                        int nodeIdx = nodeIndex.GetCurrentValue();
                        if (nodeIdx >= snowflakeInfo.nodes.Length)
                        {
                            // Finished: Let valid placements of the first 4 rotations beep
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            int idx = snowflakeInfo.nodes.Length - 1;
                            for (int i = 0; i < 4; i++)
                            {
                                if (GetNodePlacement(idx, i))
                                    pc.SendBeepOnPartitionSet(i);
                            }
                            round.SetValue(21);
                        }
                        else
                        {
                            // Initialize the 6 candidate sets using the stored arm lengths
                            int[] arms = snowflakeInfo.nodes[nodeIdx].arms;
                            bool isCandidate = false;
                            for (int d = 0; d < 6; d++)
                            {
                                bool cand = true;
                                for (int i = 0; i < 6; i++)
                                {
                                    // Compare the node's arm length to our arm in the rotated direction 
                                    int arm = arms[i];
                                    int dir = (d + i) % 6;
                                    if (arm != -1 && GetArmLengthComp(arm, dir) == ComparisonResult.LESS)
                                    {
                                        cand = false;
                                        break;
                                    }
                                }
                                candidate.SetValue(d, cand);
                                isCandidate = isCandidate || cand;
                            }

                            // Let the candidates beep on the 
                            if (isCandidate)
                            {
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                pc.SendBeepOnPartitionSet(0);
                            }
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 9:
                    {
                        int rot = rotationIndex.GetCurrentValue();
                        int nodeIdx = nodeIndex.GetCurrentValue();
                        if (rot >= 6)
                        {
                            // Have checked children in all directions
                            // Store the remaining valid placements and continue with the next node
                            for (int d = 0; d < 6; d++)
                            {
                                SetNodePlacement(nodeIdx, d, candidate.GetCurrentValue(d));
                            }
                            nodeIndex.SetValue(nodeIdx + 1);
                            round.SetValue(r - 2);
                        }
                        else
                        {
                            // If we have no children in this direction: Find the next direction where we have children and stay in this round
                            if (!HasChildInDirection(nodeIdx, rot))
                            {
                                int d = rot + 1;
                                for (; d < 6; d++)
                                {
                                    if (HasChildInDirection(nodeIdx, d))
                                        break;
                                }
                                rotationIndex.SetValue(d);
                            }
                            // If we have children: Initialize max child distance and let candidates beep on appropriate axis circuits
                            else
                            {
                                genericCounter.SetValue(GetMaxChildDistance(nodeIdx, rot));
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                for (int d = 0; d < 6; d++)
                                {
                                    if (candidate.GetCurrentValue(d))
                                        pc.SendBeepOnPartitionSet((d + rot) % 6);
                                }
                                round.SetValue(r + 1);
                            }
                        }
                    }
                    break;
                case 11:
                    {
                        // Send PASC activation beeps if the current shape has any children at the current distance
                        int rot = rotationIndex.GetCurrentValue();
                        if (HasChildAtDistance(nodeIndex.GetCurrentValue(), rot, genericCounter.GetCurrentValue()))
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            bool[] invalidRotations = GetInvalidPlacementRotations();
                            for (int d = 0; d < 6; d++)
                            {
                                if (!onCandidateSegment.GetCurrentValue(d))
                                    continue;
                                if (invalidRotations[(d + 6 - rot) % 6])
                                {
                                    pc.GetPinAt(DirectionHelpers.Cardinal((d + 3) % 6), algo.PinsPerEdge - 1).PartitionSet.SendBeep();
                                    // Also add the PASC start points to the elimination segments already
                                    eliminationSegment.SetValue(d, true);
                                }
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Skip whole elimination distance part
                            round.SetValue(18);
                        }
                    }
                    break;
                case 13:
                    {
                        // Send PASC beeps
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                                pasc[d].ActivateSend();
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 14:
                    {
                        // Marker sends scale bit and MSB on the first two global circuits and forwards marker
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        if (marker.GetCurrentValue())
                        {
                            if (scaleBit.GetValue())
                                pc.SendBeepOnPartitionSet(0);
                            if (scaleMSB.GetValue())
                                pc.SendBeepOnPartitionSet(1);
                            else
                            {
                                Direction succ = counterSucc.GetValue();
                                pc.GetPinAt(succ, GetMarkerPin(true, succ)).PartitionSet.SendBeep();
                            }
                        }
                        // Send PASC continuation beep on third global circuit
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d) && pasc[d].BecamePassive())
                            {
                                pc.SendBeepOnPartitionSet(2);
                                break;
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 15:
                    {
                        // Send PASC beep
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                            {
                                pasc[d].ActivateSend();
                            }
                        }
                        round.SetValue(r - 1);
                    }
                    break;
                case 16:
                    {
                        // Send PASC cutoff
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                            {
                                pasc[d].SendCutoffBeep();
                            }
                        }
                    }
                    break;
                case 17:
                    {
                        // We have the final comparison results now
                        // Update elimination segment status based on the results
                        for (int d = 0; d < 6; d++)
                        {
                            if (pascParticipant.GetCurrentValue(d))
                            {
                                pascParticipant.SetValue(d, false);
                                eliminationSegment.SetValue(d, eliminationSegment.GetCurrentValue(d) || comparisons[d].GetCurrentValue() != ComparisonResult.GREATER);
                                comparisons[d].SetValue(ComparisonResult.EQUAL);
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 19:
                    {
                        // Send segment shifting beep
                        for (int d = 0; d < 6; d++)
                            segmentShift[d].ActivateSend();
                    }
                    break;
                case 20:
                    {
                        // Candidates in elimination segments retire
                        for (int d = 0; d < 6; d++)
                        {
                            if (candidate.GetCurrentValue(d) && eliminationSegment.GetCurrentValue((d + rotationIndex.GetCurrentValue()) % 6))
                            {
                                candidate.SetValue(d, false);
                            }
                        }
                        // Remaining candidates beep on global circuit
                        if (candidate.GetCurrentOr())
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(0);
                        }
                    }
                    break;
                case 21:
                    {
                        // Transmit the remaining 2 valid rotations
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        for (int i = 0; i < 2; i++)
                        {
                            if (GetNodePlacement(snowflakeInfo.nodes.Length - 1, i + 4))
                                pc.SendBeepOnPartitionSet(i);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
            }

            SetColor();
        }

        private void SetColor()
        {
            if (!controlColor.GetCurrentValue())
                return;

            int r = round.GetCurrentValue();

            if (marker.GetCurrentValue())
                algo.SetMainColor(ColorData.Particle_Orange);
            else if (r >= 7)
            {
                if (IsFinished())
                {
                    int lowest = LowestValidRotation();
                    if (lowest != -1 && IsRepresentative(lowest))
                        algo.SetMainColor(ColorData.Particle_Green);
                    else if (lowest == -1)
                        algo.SetMainColor(ColorData.Particle_Red);
                    else
                        algo.SetMainColor(ColorData.Particle_Black);
                }
                else
                {
                    bool elim = eliminationSegment.GetCurrentOr();
                    bool cand = candidate.GetCurrentOr();
                    if (elim && cand)
                        algo.SetMainColor(ColorData.Particle_Aqua);
                    else if (elim)
                        algo.SetMainColor(ColorData.Particle_Blue);
                    else if (cand)
                        algo.SetMainColor(ColorData.Particle_Green);
                    else
                        algo.SetMainColor(ColorData.Particle_Black);
                }
            }
            else
                algo.SetMainColor(ColorData.Particle_Black);
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if all valid
        /// placements have been found.</returns>
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
            return IsFinished() && finalRotations.GetCurrentOr();
        }

        /// <summary>
        /// Finds the lowest rotation for which there exist a valid
        /// placement in the system if the procedure is finished.
        /// </summary>
        /// <returns>The lowest rotation for which a valid placement
        /// has been found, if it exists, otherwise <c>-1</c>.</returns>
        public int LowestValidRotation()
        {
            if (!Success())
                return -1;
            for (int r = 0; r < 6; r++)
            {
                if (finalRotations.GetCurrentValue(r))
                    return r;
            }
            return -1;
        }

        /// <summary>
        /// Finds all rotations for which there exists a valid
        /// placement in the system if the procedure is finished.
        /// </summary>
        /// <returns>An array containing all rotations for
        /// which a valid placement has been found. May be empty.</returns>
        public int[] ValidRotations()
        {
            if (!Success())
                return new int[0];
            List<int> rotations = new List<int>();
            for (int r = 0; r < 6; r++)
            {
                if (finalRotations.GetCurrentValue(r))
                    rotations.Add(r);
            }
            return rotations.ToArray();
        }

        /// <summary>
        /// Checks whether this amoebot is a valid placement at
        /// the given rotation.
        /// </summary>
        /// <param name="rotation">The rotation of the shape to check.</param>
        /// <returns><c>true</c> if and only if this amoebot has been
        /// determined as a valid placement of the input shape at rotation
        /// <paramref name="rotation"/>.</returns>
        public bool IsRepresentative(int rotation)
        {
            return GetNodePlacement(snowflakeInfo.nodes.Length - 1, rotation);
        }

        /// <summary>
        /// Helper checking whether we are part of a counter.
        /// </summary>
        /// <returns><c>true</c> if and only if we are on a binary counter,
        /// i.e., we have a counter successor or predecessor.</returns>
        private bool IsOnCounter()
        {
            return counterPred.GetCurrentValue() != Direction.NONE || counterSucc.GetCurrentValue() != Direction.NONE;
        }

        /// <summary>
        /// Helper that places the marker at the counter start.
        /// </summary>
        private void SetMarkerToCounterStart()
        {
            marker.SetValue(counterPred.GetValue() == Direction.NONE && counterSucc.GetValue() != Direction.NONE);
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

        /// <summary>
        /// Helper to forward the marker by one location based on the received beep.
        /// </summary>
        /// <param name="pc">The pin configuration on which to listen for the forwarding beep.</param>
        private void ForwardMarker(PinConfiguration pc)
        {
            if (IsOnCounter())
            {
                Direction pred = counterPred.GetValue();
                marker.SetValue(pred != Direction.NONE && pc.GetPinAt(pred, GetMarkerPin(false, pred.Opposite())).PartitionSet.ReceivedBeep());
            }
        }

        /// <summary>
        /// Helper finding out whether the given node has a child node in
        /// the given direction.
        /// </summary>
        /// <param name="nodeIdx">The index of the node to check.</param>
        /// <param name="direction">The direction to check.</param>
        /// <returns><c>true</c> if and only if the node with index
        /// <paramref name="nodeIdx"/> has a child node in direction
        /// <paramref name="direction"/>.</returns>
        private bool HasChildInDirection(int nodeIdx, int direction)
        {
            return GetMaxChildDistance(nodeIdx, direction) >= 0;
        }

        /// <summary>
        /// Helper finding the maximum distance of a child of the given
        /// node in the given direction.
        /// </summary>
        /// <param name="nodeIdx">The index of the node to check.</param>
        /// <param name="direction">The direction to check.</param>
        /// <returns>The maximum distance of any child of the node with
        /// index <paramref name="nodeIdx"/> that lies in direction
        /// <paramref name="direction"/>, or <c>-1</c> if such a
        /// child does not exist.</returns>
        private int GetMaxChildDistance(int nodeIdx, int direction)
        {
            int distance = -1;
            foreach (ShapeContainer.DTreeChild c in snowflakeInfo.nodes[nodeIdx].children)
            {
                if (c.direction == direction)
                    distance = Mathf.Max(distance, c.distance);
            }
            return distance;
        }

        /// <summary>
        /// Helper finding out whether the given node has a child at
        /// the given distance in the given direction.
        /// </summary>
        /// <param name="nodeIdx">The index of the node to check.</param>
        /// <param name="direction">The direction to check.</param>
        /// <param name="distance">The distance to check</param>
        /// <returns><c>true</c> if and only if the node with index
        /// <paramref name="nodeIdx"/> has a child in direction
        /// <paramref name="direction"/> at distance <paramref name="distance"/>.</returns>
        private bool HasChildAtDistance(int nodeIdx, int direction, int distance)
        {
            foreach (ShapeContainer.DTreeChild c in snowflakeInfo.nodes[nodeIdx].children)
            {
                if (c.direction == direction && c.distance == distance)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Finds the rotations for which we are not a valid placement
        /// of a child of the current node for the current direction and distance.
        /// </summary>
        /// <returns>An array with <c>true</c> entries for the rotations at
        /// which we are not a valid placement.</returns>
        private bool[] GetInvalidPlacementRotations()
        {
            bool[] dirs = new bool[6];

            // First, find all children of the current node at the current distance and rotation
            int nodeIdx = nodeIndex.GetCurrentValue();
            int rot = rotationIndex.GetCurrentValue();
            int dist = genericCounter.GetCurrentValue();
            List<Vector2Int> children = new List<Vector2Int>();
            for (int i = 0; i < snowflakeInfo.nodes[nodeIdx].children.Length; i++)
            {
                ShapeContainer.DTreeChild c = snowflakeInfo.nodes[nodeIdx].children[i];
                if (c.direction == rot && c.distance == dist)
                    children.Add(new Vector2Int(c.childIdx, c.rotation));
            }

            // Now determine all rotations for which we are not a valid placement of at least one child
            for (int d = 0; d < 6; d++)
            {
                foreach (Vector2Int c in children)
                {
                    if (!GetNodePlacement(c.x, (d + c.y) % 6))
                    {
                        dirs[d] = true;
                        break;
                    }
                }
            }

            return dirs;
        }

        /// <summary>
        /// Sets up global circuits on partition sets 0, 1....
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="numCircuits">The number of global circuits
        /// to establish.</param>
        private void SetupGlobalCircuits(PinConfiguration pc, int numCircuits)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            for (int i = 0; i < numCircuits; i++)
                pc.SetStarConfig(i, inverted, i);
        }

        /// <summary>
        /// Sets up 6 axis circuits using the outer two pins. The
        /// partition set IDs are 0,...,5.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="split">The directions in which the axis circuit
        /// should be split.</param>
        private void SetupAxisCircuits(PinConfiguration pc, bool[] split = null)
        {
            if (split is null)
                split = new bool[6];

            for (int i = 0; i < 6; i++)
            {
                if (!split[i])
                {
                    Direction d = DirectionHelpers.Cardinal(i);
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 0).Id, pc.GetPinAt(d.Opposite(), algo.PinsPerEdge - 1).Id }, i);
                    pc.SetPartitionSetPosition(i, new Vector2((d.ToInt() - 1.5f) * 60, 0.4f));
                }
            }
        }


        // Helpers for complex binary data access

        /// <summary>
        /// Helper for reading the stored scaled arm length bit from the
        /// array of bit fields.
        /// </summary>
        /// <param name="armIdx">The index of the arm length.</param>
        /// <returns>The stored bit value of the arm length with
        /// index <paramref name="armIdx"/>.</returns>
        private bool GetScaledArmLengthBit(int armIdx)
        {
            int fieldIdx = armIdx / 32;
            int offset = armIdx % 32;
            return armLengthBits[fieldIdx].GetCurrentValue(offset);
        }

        /// <summary>
        /// Helper for reading the base arm length bit at this position
        /// in the counter.
        /// </summary>
        /// <param name="armIdx">The index of the arm.</param>
        /// <returns>The bit of the arm length with index <paramref name="armIdx"/>
        /// at the position matching our counter position.</returns>
        private bool GetArmLengthBit(int armIdx)
        {
            if (!counterIndexValid.GetCurrentValue())
                return false;
            string armString = snowflakeInfo.armLengthsStr[armIdx];
            int ctrIdx = counterIndex.GetCurrentValue();
            return ctrIdx < armString.Length ? armString[ctrIdx] == '1' : false;
        }

        /// <summary>
        /// Helper for reading a valid placement flag from the
        /// array of bit fields.
        /// </summary>
        /// <param name="nodeIdx">The index of the node.</param>
        /// <param name="rotation">The rotation of the node's shape to check.</param>
        /// <returns><c>true</c> if and only if we are a valid placement of
        /// the shape with node index <paramref name="nodeIdx"/> at
        /// rotation <paramref name="rotation"/>.</returns>
        private bool GetNodePlacement(int nodeIdx, int rotation)
        {
            int idx = nodeIdx * 6 + rotation;
            int fieldIdx = idx / 32;
            int offset = idx % 32;
            return nodePlacements[fieldIdx].GetCurrentValue(offset);
        }

        /// <summary>
        /// Helper for reading an arm length comparison result from
        /// the array.
        /// </summary>
        /// <param name="armIdx">The index of the arm length.</param>
        /// <param name="rotation">The rotation of the arm to check.</param>
        /// <returns>The result of comparing our maximal segment in the
        /// direction indicated by <paramref name="rotation"/> to the
        /// scaled length of the arm with index <paramref name="armIdx"/>.</returns>
        private ComparisonResult GetArmLengthComp(int armIdx, int rotation)
        {
            int idx = armIdx * 6 + rotation;
            return armLengthComparisons[idx].GetCurrentValue();
        }

        /// <summary>
        /// Helper for writing the stored arm length bit in the
        /// array of bit fields.
        /// </summary>
        /// <param name="armIdx">The index of the arm length.</param>
        /// <param name="value">The new value of the stored bit.</param>
        private void SetScaledArmLengthBit(int armIdx, bool value)
        {
            int fieldIdx = armIdx / 32;
            int offset = armIdx % 32;
            armLengthBits[fieldIdx].SetValue(offset, value);
        }

        /// <summary>
        /// Helper for writing a valid placement flag in the
        /// array of bit fields.
        /// </summary>
        /// <param name="nodeIdx">The index of the node.</param>
        /// <param name="rotation">The rotation of the node's shape to set.</param>
        /// <param name="value">The new value of the placement flag.</param>
        private void SetNodePlacement(int nodeIdx, int rotation, bool value)
        {
            int idx = nodeIdx * 6 + rotation;
            int fieldIdx = idx / 32;
            int offset = idx % 32;
            nodePlacements[fieldIdx].SetValue(offset, value);
        }

        /// <summary>
        /// Helper for writing an arm length comparison result to
        /// the array.
        /// </summary>
        /// <param name="armIdx">The index of the arm length.</param>
        /// <param name="rotation">The rotation of the arm to set.</param>
        /// <param name="value">The new comparison result to write.</param>
        private void SetArmLengthComp(int armIdx, int rotation, ComparisonResult value)
        {
            int idx = armIdx * 6 + rotation;
            armLengthComparisons[idx].SetValue(value);
        }
    }

} // namespace AS2.Subroutines.SnowflakePlacementSearch
