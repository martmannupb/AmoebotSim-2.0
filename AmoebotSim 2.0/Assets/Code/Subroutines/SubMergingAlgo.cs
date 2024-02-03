using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;

namespace AS2.Subroutines.ConvexShapeContainment
{
    /// <summary>
    /// Shape containment check for triangles, trapezoids and pentagons.
    /// Finds all valid placements for the given shape extending in the
    /// given directions for two side lengths given in a binary counter,
    /// rotated by the specified amount.
    /// <para>
    /// It is assumed that the counter storing the side lengths consists of at
    /// least two amoebots (the start must not be equal to the end point).
    /// </para>
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge. This subroutine sometimes uses all pins on one side.
    /// </para>

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
    //          - If the received bit is 0, we are between a pair (but only if we have two neighbors in Q, Q')
    //          - If we only have a right neighbor, we are still a participant
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
        public enum ShapeType
        {
            TRIANGLE = 0,   // Match the IDs of shape types in convex shape algorithm
            TRAPEZOID = 2,
            PENTAGON = 3
        }

        enum ComparisonResult
        {
            NONE = 0,
            EQUAL = 1,
            LESS = 2,
            GREATER = 3
        }

        // State represented in 2 ints
        //               2120      1918       1715    1412    119     876     54      3210
        // xxxx xxxx xx   xx        xx        xxx     xxx     xxx     xxx     xx      xxxx
        //                Comp. 2   Comp. 1   Succ.   Pred.   Dir h   Dir w   Shape   Round
        ParticleAttribute<int> state1;
        //            24      23        22         21        20        19    18       17          16         15     14      13   12   11          10            9  8  7  6  5        4  3  2  1  0
        // xxxx xxx   x       x         x          x         x         x     x        x           x          x      x       x    x    x           x             x  x  x  x  x        x  x  x  x  x
        //            Color   Success   Finished   Carry 2   Carry 1   Bit   Pair L   Nbr Right   Nbr Left   PASC   Merge   Q'   Q    Candidate   Marker   MSBs a3 a2 c  d  a   Bits a3 a2 c  d  a
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
        BinAttributeBool inPasc;                            // Whether this amoebot participates in PASC between a pair of amoebots in Q, Q'
        BinAttributeBool haveNbrL;                          // Whether we have a neighbor in Q, Q' somewhere to our "left"
        BinAttributeBool haveNbrR;                          // Whether we have a neighbor in Q, Q' somewhere to our "right"
        BinAttributeBool pairLeftSide;                      // Whether we are the left side of a pair
        BinAttributeBool storedBit;                         // Storage for one bit of a number
        BinAttributeBool carry1;                            // Whether the bit stream subtraction 1 has a "carry" bit
        BinAttributeBool carry2;                            // Whether the bit stream subtraction 2 has a "carry" bit
        BinAttributeBool finished;                          // Whether the procedure has finished
        BinAttributeBool success;                           // Whether the procedure has finished successfully
        BinAttributeBool color;                             // Whether we should control the amoebot's color
        

        public SubMergingAlgo(Particle p) : base(p)
        {
            state1 = algo.CreateAttributeInt(FindValidAttributeName("[Merge] State 1"), 0);
            state2 = algo.CreateAttributeInt(FindValidAttributeName("[Merge] State 2"), 0);

            round = new BinAttributeInt(state1, 0, 4);
            shapeType = new BinAttributeEnum<ShapeType>(state1, 4, 2);
            directionW = new BinAttributeDirection(state1, 6);
            directionH = new BinAttributeDirection(state1, 9);
            directionPred = new BinAttributeDirection(state1, 12);
            directionSucc = new BinAttributeDirection(state1, 15);
            comp1 = new BinAttributeEnum<ComparisonResult>(state1, 18, 2);
            comp2 = new BinAttributeEnum<ComparisonResult>(state1, 20, 2);

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
            inPasc = new BinAttributeBool(state2, 15);
            haveNbrL = new BinAttributeBool(state2, 16);
            haveNbrR = new BinAttributeBool(state2, 17);
            pairLeftSide = new BinAttributeBool(state2, 18);
            storedBit = new BinAttributeBool(state2, 19);
            carry1 = new BinAttributeBool(state2, 20);
            carry2 = new BinAttributeBool(state2, 21);
            finished = new BinAttributeBool(state2, 22);
            success = new BinAttributeBool(state2, 23);
            color = new BinAttributeBool(state2, 24);
        }
    }

} // namespace AS2.Subroutines.ConvexShapeContainment
