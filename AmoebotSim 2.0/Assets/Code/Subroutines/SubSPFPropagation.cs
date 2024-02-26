using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.SPFPropagation
{

    /// <summary>
    /// Implements the propagation primitive that propagates
    /// a shortest path forest from one region into an adjacent
    /// region through the portal that separates them. It is possible
    /// to run this procedure on both sides of a portal (or even 4
    /// sides of a marker) if the source region is the portal itself.
    /// However, only the first phase can run simultaneously. For the
    /// second phase, a coloring of the portal graph that colors adjacent
    /// regions differently is required, such that only regions of the
    /// same color run the procedure at the same time.
    /// </summary>

    // Algorithm plan:
    // 1. Cover the region visible by the portal
    //  1.1. Send activation beeps
    //      - Establish circuits along two axes and let the portal amoebots beep on them
    //      - All amoebots in the visible region will receive at least one beep
    //      - Amoebots that receive only one beep already set their parent direction
    //  1.2. Send reply beeps
    //      - Let the amoebots receiving two axis beeps reply on the same circuits
    //      - Also let them beep on a global/regional circuit to determine whether the next phase is necessary
    //  1.3. Use PASC to finish the visible region
    //      - Only run this if there was a beep on the global circuit
    //      - Let all non-source amoebots in the "upper" region beep in the direction of their parent
    //      - Then establish PASC circuits along the shortest path in the upper region
    //      - Establish axis circuits in the "lower" region
    //      - Run PASC and let the portal amoebots forward their distances to the amoebots in the lower region
    //          - (Limit this to the axes where it is necessary)
    //      - The amoebots that received two beeps in the lower region compare the two PASC results and choose the smaller one for their parent direction
    //      - Use a regional circuit for synchronization and terminate when all PASC trees are finished
    // (Allow the calling algorithm to wait here)
    // 2. Cover the non-visible regions
    //  2.1. Establish the regions and start points
    //      - Let amoebots that are not in the visible region beep on a global circuit
    //          - Terminate already if there is no beep
    //      - Let all amoebots in the visible region send a beep to the neighbors that might be in a non-visible region
    //          - The amoebots in the non-visible region can now determine their start points
    //  2.2. Use 1-SPF to solve the non-visible regions
    //      - Run the single-source subroutine on each non-visible region independently
    //      - Use a regional circuit for synchronization
    //      - Terminate as soon as all regions are finished


    // Round plan:

    // 1.1. Activation beeps

    // Round 0:
    //  Send:
    //  - Establish axis circuits in the lower region and let portal amoebots beep
    //  Receive:
    //  - Amoebots in the lower region listen for axis beeps
    //  - If only one beep: Set parent direction
    //  - If no beep: We are in a non-visible region (store this info)
    //  - If two beeps: We are in the visible region but have two options
    //  - Go to round 1

    // 1.2. Send reply beeps

    // Round 1:
    //  Send:
    //  - Establish axis circuits and a regional circuit
    //  - Amoebots in the visible region that received two beeps send reply on axis and regional circuit
    //      - Even split the axis circuits to minimize the number of amoebots participating in the next phase
    //  Receive:
    //  - If no beep on the regional circuit: Terminate
    //  - Else:
    //      - Find the amoebots that have to participate in the next phase

    // 1.3. Use PASC to finish the visible region

    // Round 2:
    //  Send:
    //  - Establish singleton pin configurations and let all non-source amoebots in the "upper" region and on the portal beep towards their parent
    //  Receive:
    //  - Initialize PASC circuits in the upper region

    // Round 3:
    //  Send:
    //  - Setup PASC circuit and axis circuits and let the portal amoebots connect their secondary partition sets to the axis circuits
    //  - Send PASC beep
    //  Receive:
    //  - Receive PASC and axis beeps
    //  - Update comparison result
    //  - Go to round 4

    // Round 4:
    //  Send:
    //  - Setup regional circuit and beep if we became passive
    //  Receive:
    //  - If no beep was received:
    //      - The PASC phase is finished
    //      - Use the comparison result to set the parent direction
    //      - Go to round 5
    //  - Else:
    //      - Go back to round 3

    // 2. Cover the non-visible regions

    // 2.1. Establish the regions and start points

    // Round 5:
    //  Send:
    //  - Setup a regional circuit
    //  - Let amoebots outside the visible region beep on the global circuit
    //  - Also let amoebots in the visible region beep in the direction of neighbors that might not be visible
    //  Receive:
    //  - If there is no beep on the global circuit: Terminate
    //  - Else:
    //      - Let non-visible amoebots identify their visible neighbors
    //      - Also identify the start points for the 1-SPF subroutine and initialize it

    // 2.2. Use 1-SPF to solve the non-visible regions

    // Round 6:
    //  Send:
    //  - Setup 1-SPF circuit and beep
    //  Receive:
    //  - Receive 1-SPF beep
    //  - Go to round 7

    // Round 7:
    //  Send:
    //  - Setup regional circuit and beep if our 1-SPF is not finished yet
    //  Receive:
    //  - Receive beep on regional circuit
    //  - If no beep:
    //      - Set parent direction and terminate
    //  - Else:
    //      - Go back to round 6



    public class SubSPFPropagation : Subroutine
    {
        // Round counter: int (0-7)                     + 3
        // Ignore directions: bool[6]                   + 6
        // Portal direction: Direction                  + 3
        // Initial parent direction: Direction          + 3
        // New parent direction: Direction              + 3
        // Instance index: int (0-3)                    + 2 (used to find unique partition set IDs)

        // Source region is portal: bool                + 1
        // Portal flag: bool                            + 1
        // Source flag: bool                            + 1
        // Source region flag: bool                     + 1
        // Region points down: bool                     + 1
        // Visible region flag: bool                    + 1
        // Received two beeps flag: bool                + 1
        // On PASC axis flag: bool                      + 1
        // Finished flag: bool                          + 1

        // 28         27                 26         25        24            23              22       21          20                    1918            1715           1412           119          8    3        2 0
        // x          x                  x          x         x             x               x        x           x                      xx             xxx            xxx            xxx          xxxxxx        xxx
        // Finished   PASC participant   Two axes   Visible   Region down   Source region   Source   On portal   Source region portal   Instance idx   Parent dir 2   Parent dir 1   Portal dir   Ignore dirs   Round
        ParticleAttribute<int> state;

        public SubSPFPropagation(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Prop] State"), 0);
        }
    }

} // namespace AS2.Subroutines.SPFPropagation
