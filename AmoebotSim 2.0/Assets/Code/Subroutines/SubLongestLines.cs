using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.LongestLines
{

    /// <summary>
    /// Procedure for finding all longest lines in the amoebot
    /// system. If longest lines exist in multiple directions,
    /// only lines from one direction will be selected.
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge.
    /// </para>
    /// </summary>
    
    // Round 0:
    //  Send:
    //  - Place markers and MSBs at line starts
    //  - Setup PASC circuits in all 3 directions
    //      - Setup additional connections to forward bits and markers
    //  - Send PASC beep

    // (Lines of length 0: Only do global circuits and wait for the first phase to complete)

    // Round 1:
    //  Receive:
    //  - Amoebot holding the marker stores the received bit and removes its marker
    //      - If the bit is a 1, place the MSB at the marker and remove it from its previous position
    //  - Amoebot receiving the marker beep takes the marker
    //  Send:
    //  - Setup a global circuit
    //  - Send beep if we became passive this round

    // Round 2:
    //  Receive:
    //  - If no beep received on global circuit:
    //      - Place markers at line starts
    //      - Go to round 3
    //  Send:
    //  - Setup PASC, bit and marker circuits again
    //  - Send beeps
    //  - Go to round 1

    // Round 3:
    //  Receive (only entered after the first send):
    //  - If no beep was received on the global circuit:
    //      - Place marker at the MSB if we are still active
    //      - Go to round 4
    //  - If the line did not send a beep but received one on the global circuit: Retire
    //  - Marker gets moved forward
    //  Send:
    //  - Setup global circuit, line circuit and marker circuit
    //  - Marker sends beep on all circuits
    //  - Marker sends forwarding beep unless it is the MSB

    // Round 4:
    //  Receive (only entered after the first send):
    //  - If no beep on second global circuit:
    //      - Go to round 5
    //  - If no beep on line circuit but beep on global circuit: Retire (but continue moving the marker)
    //  - Marker moves to next position and sends beep on second global circuit
    //  Send:
    //  - Setup 2 global circuits, line circuit and marker circuit
    //  - Marker sends beep if bit is 1
    //  - Send marker beep to predecessor
    //  - Marker sends beep on second global circuit

    // Round 5:
    //  Send:
    //  - Setup 3 global circuits
    //  - Longest lines beep on the circuit belonging to their direction
    //  Receive:
    //  - Only lines in the smallest direction remain, other ones retire
    //  - Terminate


    public class SubLongestLines : Subroutine
    {
        //

        public SubLongestLines(Particle p) : base(p)
        {

        }
    }

} // namespace AS2.Subroutines.LongestLines
