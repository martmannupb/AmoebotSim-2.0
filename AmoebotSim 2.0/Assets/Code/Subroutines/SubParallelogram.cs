using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.ParallelogramContainment
{
    /// <summary>
    /// Shape containment check for parallelograms.
    /// </summary>

    // Round plan:

    // Init:
    //  - Place marker at counter start
    //  - Setup PASC in direction d^-1
    //  - Set comparison result to EQUAL

    // Round 0:
    //  Receive (Only reached after first send!):
    //  - Receive beeps on two global circuits
    //  - If no beep on first circuit:
    //      - If no beep on second circuit:
    //          - First PASC is finished
    //          - Set valid line flag based on comparison result
    //          - Go to round 3
    //      - Else:
    //          - Terminate with failure
    //  - Else:
    //      - If no beep on second circuit:
    //          - Perform PASC cutoff
    //          - Go to round 2
    //  Send:
    //  - Setup global circuit
    //  - Send PASC
    //  - Marker sends bit of d on global circuit
    //  - Marker sends beep to successor (unless it is MSB)

    // Round 1:
    //  Receive:
    //  - Receive PASC
    //  - Receive bit d on global circuit
    //  - Receive marker beep
    //  - Update comparison result and marker
    //  Send:
    //  - Setup 2 global circuits
    //  - Send beep on circuit 1 if we became passive
    //  - Current marker sends beep on global circuit 2

    // Round 2:
    //  Receive (Only reached after first send!):
    //  - Receive PASC cutoff beep
    //  - Update comparison result
    //  - Set valid line flag based on result
    //  - Go to round 3
    //  Send:
    //  - Setup PASC cutoff circuit and send beep

    // Round 3:
    //  Receive (Only reached after first send!):
    //  - Receive line circuit beep
    //      - If received and valid line point: Set segment limiter flag
    //      - (This means we have to interpret the PASC result differently later)
    //  - Receive second line circuit beep
    //      - Set have valid placement flag
    //      - (This means we have to run next phase on this segment)
    //  - Receive global circuit beep
    //      - If no beep: Terminate with failure
    //  - Place marker at counter start
    //  - Go to round 4
    //  Send:
    //  - Setup circuits along direction a
    //      - Split at invalid line points
    //      - Amoebots without neighbor in direction a send beep
    //  - Setup second circuits along direction a (complete)
    //      - Valid line points send beep
    //  - Setup a global circuit
    //      - Valid line points send beep

    // Round 4:
    //  Send:
    //  - Setup PASC circuits in direction a^-1
    //      - Only where we have a valid placement
    //      - Leaders are invalid placements and amoebots without neighbor in direction a
    //      - Send first beep
    //  - Setup global circuit
    //      - Marker sends current beep of a
    //  - Marker sends beep to successor (unless it is MSB)

    // Round 5 (similar to round 1):
    //  Receive:
    //  - (Only done by amoebots on line with valid line placement)
    //  - Receive PASC
    //  - Receive bit a on global circuit
    //  - Receive marker beep
    //  - Update comparison result and marker
    //  Send:
    //  - Setup 2 global circuits
    //  - Send beep on circuit 1 if we became passive
    //  - Current marker sends beep on global circuit 2

    // Round 6 (similar to round 0):
    //  Receive:
    //  - Receive beeps on two global circuits
    //  - If no beep on first circuit:
    //      - If no beep on second circuit:
    //          - First PASC is finished
    //          - Set valid placement flag based on comparison result
    //          - Go to round 8
    //      - Else:
    //          - Terminate with failure
    //  - Else:
    //      - If no beep on second circuit:
    //          - Perform PASC cutoff
    //          - Go to round 7
    //  Send:
    //  - Setup global circuit
    //  - Send PASC
    //  - Marker sends bit of a on global circuit
    //  - Marker sends beep to successor (unless it is MSB)

    // Round 7 (similar to round 2):
    //  Receive (Only reached after first send!):
    //  - Receive PASC cutoff beep
    //  - Update comparison result
    //  - Set valid placement flag based on result
    //  - Go to round 8
    //  Send:
    //  - Setup PASC cutoff circuit and send beep

    // Round 8:
    //  Receive (Only reached after first send!):
    //  - Listen for beep on global circuit
    //  - If no beep received:
    //      - Terminate with failure
    //  - Otherwise:
    //      - Terminate with success
    //  Send:
    //  - Setup global circuit
    //  - Valid placements beep

    public class SubParallelogram : Subroutine
    {
        // State:
        // Round: int
        // Bit a: bool                          + 1
        // Bit d: bool                          + 1
        // MSB a: bool                          + 1
        // MSB d: bool                          + 1
        // Direction a: Direction               + 3
        // Direction d: Direction               + 3
        // Counter pred: Direction              + 3
        // Counter succ: Direction              + 3
        // Comparison result                    + 2
        // Segment limiter: bool                + 1
        // Valid line flag: bool                + 1
        // Finished flag: bool                  + 1
        // Success flag: bool                   + 1
        // Valid placement flag: bool           + 1
        // Have valid placement: bool           + 1
        // Marker: bool                         + 1

        ParticleAttribute<int> state;

        public SubParallelogram(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Paral.] State"), 0);
        }
    }

} // namespace AS2.Subroutines.ParallelogramContainment
