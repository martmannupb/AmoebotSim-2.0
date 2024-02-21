using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.ShapeContainment;

namespace AS2.Subroutines.SnowflakeContainment
{

    /// <summary>
    /// Container class storing all information required by the containment check
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
    /// Containment check procedure for snowflake shapes.
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
    //      - (Skip the shift part)
    //      - Go to round 20

    // Round 19:
    //  Receive ( * ):
    //  - Receive shift subroutine beep
    //  - If finished:
    //      - Update elimination segments
    //      - Go to round 20
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



    public class SubSnowflakeContainment : Subroutine
    {

        // Round: int (0-22)                                    + 5
        // (Scaled) arm length bits:
        //  Bit field, +32 for every 32 arm lengths             + 32 * (A / 32)
        // Arm length MSB: bool                                 + 1
        // Counter index: int (0-31)                            + 5
        // Counter flag: bool                                   + 1
        // 6 * num arm lengths comparison results
        //  2 bits per result                                   + 6 * 2 * A
        // 6 * num nodes placement flags
        //  1 bit per flag                                      + 6 * N
        // Generic counter: int (0-255)                         + 8
        // Dependency tree idx: int (0-255)                     + 8
        // Direction/Rotation index: int (0-5)                  + 3
        // Candidate set 6 times: bool                          + 6
        // Have candidate on segment flag 6 times: bool         + 6
        // PASC participant flag 6 times: bool                  + 6
        // Elimination segment flag 6 times: bool               + 6
        // Comparison results for the 6 segments                + 6 * 2
        // Counter data
        //  Predecessor: Direction                              + 3
        //  Successor: Direction                                + 3
        //  Marker: bool                                        + 1
        //  Scale bit: bool                                     + 1
        //  Scale MSB: bool                                     + 1
        // Control color flag: bool                             + 1
        // Final rotation flags: bool                           + 6
        // Final valid placement flags: bool                    + 6
        // Finished flag                                        + 1

        SnowflakeInfo snowflakeInfo;

        public SubSnowflakeContainment(Particle p, SnowflakeInfo snowflakeInfo) : base(p)
        {
            this.snowflakeInfo = snowflakeInfo;
        }
    }

} // namespace AS2.Subroutines.SnowflakeContainment
