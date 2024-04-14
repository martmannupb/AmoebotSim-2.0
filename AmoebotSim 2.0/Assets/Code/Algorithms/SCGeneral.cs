using System.Collections.Generic;
using AS2.Sim;
using AS2.UI;
using UnityEngine;
using static AS2.Constants;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.BoundaryTest;
using AS2.Subroutines.LeaderElectionSC;
using AS2.Subroutines.LongestLines;
using AS2.Subroutines.ConvexShapePlacementSearch;
using AS2.Subroutines.PASC;
using AS2.Subroutines.ShapeConstruction;

namespace AS2.Algos.SCGeneral
{

    /// <summary>
    /// Implementation of the improved general shape containment solution.
    /// </summary>

    // Algorithm plan:
    //  ((a) means our shape has a face or it has a hole and the system has no holes, (b) is the other case)
    //  1. Find all longest lines in the system
    //      - Use the length as upper bound R for the scale later
    //  2. Limit R using the longest line of the shape
    //      - Let s be the longest line in the target shape (binary string is available in static data)
    //      - Move a marker along the (line!) counters, storing the bits and MSB of s
    //      - Compare R to s
    //          - If s > R, terminate immediately
    //      - Compute R' := R / s
    //      - Determine MSB of R' and let K := R'
    //      - Set the binary search limits to L := 1 and R := K
    //  3. Setup the outer boundary (IF the shape has no triangle)
    //      3.1. Run the boundary test subroutine
    //          - If the shape has a hole and the system has no holes:
    //              - Set the K <= sqrt(n) flag and jump to step 4
    //      3.2. Prepare computation of N := Floor(sqrt(n))
    //          - Identify the start and end point of the boundary
    //              - Marker must have a boundary index!
    //          3.2.1. Construct a spanning tree
    //              - Beep along axes instead of only to neighbors
    //          3.2.2. Compute n on the outer boundary
    //              - Use ETT (need up to 6 PASC instances...)
    //              - Then write binary search limits L := 1 and R := n to the outer boundary counter
    //  4. Binary search
    //      4.1. Check right bound R
    //          - (a) Run placement search for triangle at scale R (two rotations!)
    //              - Success: Set K := R and go to step 5
    //          - (b) Skip
    //      4.2. Check left bound L = 1
    //          - (a) Run placement search for triangle at scale L (two rotations!)
    //              - Failure: Terminate with failure
    //          - (b) Skip
    //      4.3. Binary search between L and R
    //          4.3.1. Compute next middle value and check termination condition (on lines (a) or outer boundary (b))
    //              - Compute M := (L + R) / 2
    //              - If M = L:
    //                  - (a) Set K := M
    //                  - (b) Set N := M
    //                  - Go to step 5
    //          4.3.2. Check for value M
    //              - (a) Run placement search for triangle at scale M (two rotations!) (keep track of which rotations matched)
    //                  - If successful: If one rotation matched and the other did not, only try the matching rotation next time
    //                      - Set L := M
    //                  - If not successful: Set R := M
    //              - (b) Compute M^2 and compare the result to n
    //                  - If M^2 = n: Set N := M and break out of the binary search (go to step 5)
    //                  - Else if M^2 < n: Set L := M
    //                  - Else: Set R := M
    //  (In case (b), we will have N = Floor(sqrt(n)) on the outer boundary now, stored in M)
    //  5. Linear search
    //      5.1. Triangle placement search: If the target shape has at least one face
    //          - Run the triangle placement search for scale K and two rotations
    //          - Store the valid placements
    //      5.2. If K <= sqrt(n) flag is not set:
    //          - Compare K to N
    //          - If K <= N: Set the flag
    //      5.3. Initialize candidate set C
    //          - If K <= sqrt(n) flag is set: Set C := A
    //          - Else: Run distance check in all 6 directions with distance K and initialize the correct candidate sets
    //          - In both cases: Limit candidates by using the faces if possible
    //      5.4. If K <= sqrt(n):
    //          - On the longest lines in the system, place a marker spawn at distance K to the marker start (should already be placed before the search starts)
    //          - For each edge in the traversal, move all candidates one step at a time, using the marker spawn to count the correct number of steps
    //          - Eliminate amoebots according to faces after each shift
    //          - Beep on a global circuit after each edge to check whether there are still candidates left
    //      5.5. Else:
    //          - For each edge, use chain circuits on the segments to identify the first candidates
    //          - Run PASC from the boundaries in all directions first to eliminate candidates that are too close to the boundary
    //          - Beep on the global circuit now to check whether there are candidates left (beep anyway for candidates that have to be shifted!)
    //          - Then use PASC to shift the candidates
    //          - Eliminate amoebots using faces again, then beep on global circuit to check if any candidates are left
    //      5.6. If there are any candidates left:
    //          - Go to step 6
    //      5.7. Decrement K
    //          - Let non-zero bits of K beep on global circuit
    //          - If there is no beep: Terminate with failure
    //          - Otherwise repeat step 5
    //  6. Shape construction
    //      - Find the lowest rotation for which the shape fits
    //      - Run leader election among candidates with this rotation
    //      - Run shape construction subroutine for this candidate at current scale K


    // Round plan:

    // 1. Find longest lines

    // Round 0:
    //  - Setup longest lines subroutine
    //  - Start sending beeps

    // Round 1:
    //  - Activate longest lines subroutine
    //  - If subroutine is finished:
    //      - Setup and start boundary test subroutine
    //      - Go to round 2
    //  - Else:
    //      - Continue sending

    // 2. Limit R using the longest line of the shape

    // Round 2:
    //  - If counter >= length of longest binary parameter (longest line):
    //      - Set counter to 0
    //      - Reset marker
    //      - SPLIT:
    //          - Counters go to round 3
    //          - Others go to round 5 and setup 4 global circuits
    //  - Else:
    //      - Marker writes the current bit/MSB of s
    //      - Marker moves one position ahead (unless it is the MSB)
    //      - Increment counter

    // Rounds 3 and 4 are for running binary operations

    // Round 3:
    //  - If counter >= 3:
    //      - Set K := R
    //      - Setup 4 global circuits and beep on first
    //      - Go to round 5
    //  - Else:
    //      - Init binop to compute:
    //          - Comparison between longest line length s and R for counter 0
    //          - R / s for counter 1
    //          - MSB of previous result (R) for counter 2
    //      - Start binop
    //      - Go to round 4

    // Round 4:
    //  - Continue binop
    //  - If binop is finished:
    //      - Store result based on counter
    //          - If s > R: Go to round 5 and beep on all 4 global circuits
    //      - Increment counter
    //      - Go to round 3

    // Round 5 (WAIT):
    //  - Wait for beep on global circuits
    //      - Beep on all four:
    //          - Terminate with failure
    //      - Beep on (only) first: Setup next phase
    //          - If the target shape does not have a triangle:
    //              - Setup and start boundary test subroutine
    //              - Go to round 6
    //          - Else:
    //              - Go to binary search (round 12)
    //      - Beep on second:
    //          - (Binary search finished)
    //          - (a) Set K := M
    //          - (b) M now holds the value N = Floor(sqrt(n)) (remove marker)
    //          - Go to linear search (round 34)
    //      - Beep on third:
    //          - (Binary search continues, only case (a))
    //          - (a) Go to round 26
    //      - Beep on fourth:
    //          - (Linear search decrement is finished)
    //          - If there is no beep on the first circuit:
    //              - K = 0: Terminate with failure
    //          - Else:
    //              - Continue with the next scale (round 39)


    // 3. Setup the outer boundary

    // Round 6:
    //  - Run boundary test subroutine
    //  - If finished:
    //      - If the shape has a hole and there are no inner boundaries:
    //          - Set K <= sqrt(n) flag and go to step 4 (round 12)
    //      - Else:
    //          - Store outer boundary parameters
    //          - Start point sends beep to end point
    //          - Go to round 7

    // 3.2. Prepare computation of N = Floor(sqrt(n))

    // 3.2.1. Construct spanning tree

    // Round 7:
    //  - Outer boundary end point receives beep and sets successor direction to NONE
    //  - Establish global and axis circuits
    //  - Boundary leader sends beep in all directions
    //  - Other amoebots send beep on global circuit

    // Round 8:
    //  - Amoebots without parent (except leader) listen for beeps on axis circuits
    //  - If no beep on global circuit:
    //      - Setup singleton configuration and send beep to parent
    //      - Go to round 9
    //  - If received:
    //      - Set parent to that direction
    //      - Remove own axis circuits (split) and send beep in all directions
    //  - Else (and no parent yet): Send beep on global circuit

    // 3.2.2. Compute n on the outer boundary

    // Round 9:
    //  - Receive beeps from children
    //  - Initialize PASC (up to 6 instances)
    //  - Send first PASC beep
    //  - Place marker and MSB of n at outer boundary start

    // Round 10:
    //  - Receive PASC beeps
    //  - Setup 2 global circuits
    //  - Beep on first circuit if we became passive
    //  - Leader beeps on second circuit if the received bit was 1
    //  - Marker also sends forwarding beep to successor

    // Round 11:
    //  - Receive beeps on two global circuits
    //      - If there was no beep on the first circuit:
    //          - (Computation of n is finished)
    //          - Write L := 1 and R := n to the outer boundary counter
    //              - Also write K := n
    //          - Go to the binary search phase (round 20)
    //      - Current marker writes received PASC bit (second circuit)
    //      - If the bit was 1: Marker becomes new MSB, old MSB removes itself
    //  - Marker moves forward
    //  - Send PASC beeps


    // 4. Binary search
    // 4.1. Check right bound R

    // Rounds 12-15 run the entire triangle placement search

    // Round 12 (Start of triangle placement search):
    //  - Init triangle placement search subroutine for rotation 0
    //      - Scale R, 1 or M
    //  - Start running
    //  - Go to round 13

    // Round 13:
    //  - Run triangle placement search routine
    //  - If finished:
    //      - Success:
    //          - If we checked R: Set K := R and go to step 5 (round 34)
    //          - If we checked 1: Move on to actual binary search (round 20)
    //          - If we checked M: Set L := M and proceed with next iteration
    //          - Other cases apply in other phases
    //      - Go to round 14

    // Rounds 14/15:
    //  - Same as rounds 12/13
    //  - But use rotation 1 instead
    //  - When finished:
    //      - Success is same as above
    //      - Failure:
    //          - If we checked R: Just go to test for L (round round 16)
    //          - If we checked 1: Terminate with failure
    //          - If we checked M: Set R := M and proceed with next iteration

    // 4.2 Check left bound L = 1

    // Rounds 16-19:
    //  - Same as rounds 12-15
    //  - But use scale 1 instead
    //  - If the check is not successful:
    //      - Terminate with failure in both cases

    // 4.3 Binary search procedure

    // Round 20:
    //  - Init and start binop to compute M := L + R
    //      - (a) On all longest lines
    //      - (b) On outer boundary
    //  - Split:
    //      - Non-counter amoebots go to round 5 with 4 global circuits

    // Round 21:
    //  - Run binop
    //  - If finished:
    //      - Go to round 22
    //      - (b): Let outer boundary amoebots send bits of M to predecessor

    // Round 22:
    //  - Shift each bit of M one position to the left
    //  - Init and start binop for finding MSB of M
    //      - (a) On all longest lines
    //      - (b) On outer boundary

    // Round 23:
    //  - Run binop
    //  - If finished:
    //      - Go to round 24

    // Round 24:
    //  - Init and start binop for comparing L to M

    // Round 25:
    //  - Run binop
    //  - If finished:
    //      - If L == M:
    //          - Setup 4 global circuits, beep on second, go to round 5
    //      - Else:
    //          - (a) Setup 4 global circuits, beep on third, go to round 5
    //          - (b) Go to round 30 (start square root check)

    // Rounds 26-29:
    //  - Same as rounds 12-15 / 16-19
    //  - Use scale M
    //  - When finished:
    //      - Success:
    //          - Set L := M
    //      - Failure:
    //          - Set R := M
    //      - Go back to round 20

    // Round 30 (b):
    //  - Start binop for computing M^2

    // Round 31:
    //  - Run binop
    //  - If finished:
    //      - Go to round 32

    // Round 32:
    //  - Start binop for comparing M^2 to n (transfer bits directly from the previous subroutine)

    // Round 33:
    //  - Run binop
    //  - If finished:
    //      - If M^2 = n: Set N := M, setup 4 global circuits, beep on second and go to round 5
    //      - Else:
    //          - If M^2 < n: Set L := M
    //          - If M^2 > n: Set R := M
    //          - Go back to round 20


    // 5. Linear Search

    // Before the loop, we place a marker spawn on the longest lines at distance K
    //  The marker spawn creates markers for the shift procedure in case K <= sqrt(n)
    // The following 5 rounds implement the PASC procedure with cutoff and can be reused later

    // Round 34 (PASC setup):
    //  - Init PASC on all longest lines
    //  - Reset comparison results
    //  - Place marker at line counter start
    //  - Send first PASC beep
    //  - Go to next round

    // Round 35:
    //  - Receive PASC beep
    //  - Setup 3 global circuits
    //  - Marker sends bit and MSB of K on first 2 circuits
    //  - PASC participants that became passive beep on third circuit

    // Round 36:
    //  - Receive on 3 global circuits
    //  - PASC participants update comparison results
    //  - Marker moves forward (unless MSB beep has been sent)
    //  - If PASC is finished:
    //      - If MSB beep:
    //          - Already have correct result
    //      - Else:
    //          - Set all comparison results to LESS
    //          - Also remember this fact (can be useful for early cutoff)!
    //      - Go to round 38
    //  - Else:
    //      - If MSB beep:
    //          - Start PASC cutoff
    //          - Go to round 37
    //      - Else:
    //          - Send PASC beeps again
    //          - Go back to round 35

    // Round 37:
    //  - Receive PASC cutoff
    //  - Update comparison results
    //  - Go to evaluation (round 38)

    // Round 38 (PASC evaluation):
    //  - Remove marker
    //  - Amoebots with result EQUAL set marker spawn flags
    //  - Go to round 39


    // 5.1. Triangle placement search (and loop start)

    // Round 39:
    //  - If the target shape has faces:
    //      - Reset valid face flags
    //      - Start triangle placement search (go to round 40)
    //  - Else:
    //      - Go to step 5.2. (round 44)

    // Rounds 40-43 (similar to 12-15):
    //  - Run triangle check for scale K
    //  - Store valid placements for both rotations
    //  - When finished, go to round 44

    // 5.2. K <= sqrt(n) update

    // Round 44:
    //  - If K <= sqrt(n) flag is set: Skip this step (go to step 5.3., round 46)
    //  - Init comparison result to EQUAL
    //  - Place markers at line starts and outer boundary start
    //  - Setup 4 global circuits
    //  - Line markers send bit and MSB of K on first 2 circuits
    //  - Boundary marker sends bit and MSB of M (= N) on second 2 circuits
    //  - Go to round 45

    // Round 45:
    //  - Receive beeps on all 4 global circuits
    //  - Update comparison result based on bits
    //  - If one of the MSBs is reached:
    //      - Update final comparison result (if only one MSB is reached: The other number is GREATER)
    //      - If K <= M: Set the flag
    //      - Reset markers
    //      - Go to round 46
    //  - Else:
    //      - Move markers forward
    //      - Keep the 4 global circuits and send the same beeps again

    // 5.3. Initialize candidate sets

    // Round 46:
    //  - Every amoebot becomes a candidate for every rotation
    //  - Remove invalid faces for the first node of the traversal
    //  - If K <= sqrt(n):
    //      - Reset counter to 0
    //      - Go to step 5.4. (round 53)
    //  - Else:
    //      - Setup axis circuits, split at candidates for the corresponding limiting direction
    //      - Let candidates beep as PASC activation
    //      - Go to round 47

    // Rounds 47-51 (PASC, similar to 34-38):
    //  - Else:
    //      - Setup PASC on all 6 axes, where the beep was received
    //      - When finished:
    //          - PASC finished and no MSB: No amoebot has enough space, skip this scale
    //              - Go to step 5.7 (round 70)
    //          - Eliminate candidates based on the result
    //          - Setup global circuit and let candidates beep
    //          - Go to round 52

    // Round 52:
    //  - Listen for beep on global circuit
    //  - If no beep: Skip this scale
    //      - Go to step 5.7. (round 70)
    //  - Else:
    //      - Reset counter to 0
    //      - Go to next step (round 53)


    // 5.4./5.5. Shape traversal

    // Round 53:
    //  - If counter >= length of the shape traversal:
    //      - Go to step 6. (candidates must exist at this point!)
    //      - Round 73
    //  - Else:
    //      - If K <= sqrt(n):
    //          - Place marker at marker spawn
    //          - Setup global circuit and let the marker beep
    //          - Go to round 54
    //      - Else:
    //          - Setup directional axis circuits
    //          - Let each candidate beep on the axis circuits that belong to its next movement direction
    //          - Go to round 55

    // Rounds 54 implements the simple edge traversal

    // Round 54:
    //  - Receive beep on global circuit
    //  - If received:
    //      - Move candidates one step in the current direction
    //      - Move marker one step down the counter
    //      - Let the marker beep on the global circuit if it has not yet reached the counter's start
    //  - Else:
    //      - (Traversal of this edge is over)
    //      - Go to round 68

    // Rounds 55-67 implement the K > sqrt(n) edge traversal

    // Rounds 55-59 (PASC, similar to 34-38):
    //  - Receive beeps on axis circuits
    //  - Setup PASC on the segments receiving a beep, using boundary amoebots as leaders
    //  - When finished:
    //      - Eliminate candidates with result LESS
    //      - If no MSB (all results are LESS):
    //          - (No candidates left, skip this scale)
    //          - Go to step 5.7. (round 70)
    //      - Establish axis circuits, split at candidates with comparison result EQUAL
    //          - Let these candidates send beep and retire
    //          - Go to round 60

    // Round 60 (already move the first candidate in this lucky case):
    //  - Receive beeps on axis circuits
    //  - If a boundary amoebot receives such a beep: Become new candidate (rotation depends on axis direction and distance check direction)
    //  - Go to round 61

    // Round 61:
    //  - Establish global and directional axis circuits, split at candidates that still need to be moved
    //  - Let these candidates beep on the global and all relevant axis circuits (towards boundary)

    // Round 62:
    //  - Receive global and axis beeps
    //  - If no global beep:
    //      - (All remaining candidates have been shifted, traversal of this edge is over)
    //      - Go to round 68
    //  - Else:
    //      - Establish axis circuits again, split at candidates, and let boundary amoebots that received a beep on their axis beep

    // Rounds 63-67 (PASC, similar to 34-38):
    //  - Setup PASC where a beep was received on the axis circuits
    //      - Candidates that received the beeps become leaders
    //  - When finished:
    //      - Amoebots with result EQUAL become new candidates, PASC leaders retire
    //      - Go to round 61

    // The two parts merge here again

    // Round 68
    //  - New candidates become candidates (only case K > sqrt(n))
    //  - Eliminate candidates using faces
    //  - Setup global circuit and let remaining candidates beep

    // Round 69:
    //  - Receive on global circuit
    //  - If no beep:
    //      - (No candidates left, skip this scale)
    //      - Go to step 5.7. (round 70)
    //  - Else:
    //      - Increment the traversal counter
    //      - Go back to round 53

    // (5.6. does not have any code, directly jump to step 6.)

    // 5.7. Decrement K

    // Round 70:
    //  - Move the marker spawn one position down on the longest lines
    //  - Start binop for decrementing K
    //  - SPLIT
    //      - Longest line amoebots start the binop and go to round 71
    //      - Other amoebots go to round 5 with 4 global circuits

    // Round 71:
    //  - Run binop
    //  - When finished:
    //      - Store new K
    //      - Start binop to find MSB of K

    // Round 72:
    //  - Run binop
    //  - When finished:
    //      - Store MSB of new K
    //      - Setup 4 global circuits and beep on fourth
    //      - Let non-zero bits of K beep on the first global circuit
    //      - Go to round 5


    // 6. Shape construction

    // Round 73:
    //  - Setup leader election on remaining candidates
    //      - Use entire system

    // Round 74:
    //  - Run leader election
    //  - If leader election is finished:
    //      - Let the leader select a random valid rotation
    //      - Setup 3 global circuits and let the leader send the bits of the chosen rotation

    // Round 75:
    //  - Receive bits of the chosen rotation
    //  - Setup shape construction subroutine for leader and scale K
    //      - Place markers at counter starts

    // Round 76:
    //  - Continue running shape construction
    //  - If shape construction is finished:
    //      - Terminate with success
    //  - If scale reset:
    //      - Set marker to counter start
    //      - Go to round 77
    //  - Else:
    //      - Continue running (maybe with scale bit)
    //      - Forward marker

    // Round 77:
    //  - Send next bit of shape construction
    //  - Forward marker
    //  - Go back to round 76

    public class SCGeneralParticle : ParticleAlgorithm
    {
        enum ComparisonResult
        {
            EQUAL = 0,
            LESS = 1,
            GREATER = 2
        }

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "SC General Solution";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SCGeneralInitializer).FullName;

        [StatusInfo("Display Shape", "Displays the target shape at the selecetd location.")]
        public static void ShowShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            LineDrawer.Instance.Clear();
            shape.Draw(selectedParticle is null ? Vector2Int.zero : selectedParticle.Head(), 0, 1);
            LineDrawer.Instance.SetTimer(20);
        }

        [StatusInfo("Draw Spanning Tree", "Displays the parent directions making up the spanning tree.")]
        public static void DrawSpanningTree(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            LineDrawer.Instance.Clear();
            
            foreach (Particle p in system.particles)
            {
                SCGeneralParticle algo = (SCGeneralParticle)p.algorithm;
                Direction dir = algo.parentDir.GetValue();
                if (dir != Direction.NONE)
                {
                    Vector2Int pos = p.Head();
                    Vector2 to = ParticleSystem_Utils.DirectionToVector(dir);
                    to *= 0.85f;
                    LineDrawer.Instance.AddLine(pos, pos + to, Color.blue, true, 2.5f, 2f);
                }
            }

            LineDrawer.Instance.SetTimer(20);
        }

        // Declare attributes here
        ParticleAttribute<int> round;                           // Round counter
        ParticleAttribute<bool> finished;                       // Whether we are finished
        ParticleAttribute<int> numOuterBoundaryOccurrences;     // How often we appear on the outer boundary
        ParticleAttribute<Direction>[] outerBoundaryDirs = new ParticleAttribute<Direction>[6]; // Predecessor/successor directions for the up to 3 outer boundary occurrences
        ParticleAttribute<int> markerIdx;                       // Which boundary occurrence currently holds the marker. 0 means no marker
        ParticleAttribute<bool> lineMarker;                     // Whether we are holding a marker on a longest line
        ParticleAttribute<bool> markerSpawn;                    // Whether we are a marker spawn on a longest line (always has distance K to the counter start)
        ParticleAttribute<int> counter;                         // Generic counter
        ParticleAttribute<bool>[] bits = new ParticleAttribute<bool>[4];    // Array storing bits of L, M, R and K
        ParticleAttribute<bool>[] msbs = new ParticleAttribute<bool>[4];    // Array storing MSBs of L, M, R and K
        private const int L = 0;
        private const int M = 1;
        private const int R = 2;
        private const int K = 3;
        ParticleAttribute<bool>[] boundaryBits = new ParticleAttribute<bool>[12];   // Array storing 4 bits for each outer boundary occurrence
        ParticleAttribute<bool>[] boundaryMSBs = new ParticleAttribute<bool>[12];   // Array storing 4 MSBs for each outer boundary occurrence
        ParticleAttribute<bool> belowSqrt;                      // Whether our scale is at most sqrt(n)
        ParticleAttribute<Direction> parentDir;                 // Our parent direction in the spanning tree
        ParticleAttribute<int> numPascETT;                      // How many PASC instances we have to run during the ETT
        ParticleAttribute<bool>[] pascParticipant = new ParticleAttribute<bool>[6]; // Flags for PASC participants in the 6 directions
        ParticleAttribute<ComparisonResult>[] comparison = new ParticleAttribute<ComparisonResult>[6];  // Comparison results for all 6 directions
        ParticleAttribute<bool> pascFinishedBeforeCounter;      // Flag that gets set during the PASC procedure if the counter MSB has not been reached when PASC is already finished
        ParticleAttribute<bool> trianglePlacement0;             // Flag for valid triangle placements with rotation 0
        ParticleAttribute<bool> trianglePlacement1;             // Flag for valid triangle placements with rotation 1
        ParticleAttribute<bool>[] candidate = new ParticleAttribute<bool>[6];       // Candidate flags for the 6 rotations
        ParticleAttribute<bool>[] newCandidate = new ParticleAttribute<bool>[6];    // Moved candidate flags for the 6 rotations
        ParticleAttribute<bool> visited;                        // Flag for amoebots visited during the traversal for K <= sqrt(n) (just for visualization)

        SubBinOps[] binops = new SubBinOps[3];
        SubBinOps binop;
        SubBoundaryTest boundaryTest;
        SubLeaderElectionSC leaderElection;
        SubLongestLines ll;
        SubPASC[] pasc = new SubPASC[6];
        SubPASC2 sharedPasc;
        SubMergingAlgo triangleCheck;
        SubShapeConstruction shapeConstruction;

        public static Shape shape;                      // The target shape
        public static string longestLineStr;            // String representation of the length of the longest line in the target shape
        public static bool shapeHasFaces;               // Whether the target shape has a face
        public static bool shapeHasHoles;               // Whether the target shape has a hole
        public static Direction distanceCheckDir;       // The direction used for the distance check before the traversal (at rotation 0)
        public static bool[,] incidentFaceMatrix;       // A matrix of dimensions N x 6, where N is the number of nodes in the shape. Stores the incident face directions of all nodes
        public static int[] traversalNodes;             // Indices of the nodes in order of the generated traversal
        public static Direction[] traversalDirs;        // Edge movement directions during the traversal

        public SCGeneralParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            finished = CreateAttributeBool("Finished", false);
            numOuterBoundaryOccurrences = CreateAttributeInt("Outer boundary occ.", 0);
            for (int i = 0; i < 6; i++)
                outerBoundaryDirs[i] = CreateAttributeDirection("Boundary " + (i % 2 == 0 ? "Pred. " : "Succ. ") + i / 2, Direction.NONE);
            markerIdx = CreateAttributeInt("Marker idx", 0);
            lineMarker = CreateAttributeBool("Line Marker", false);
            markerSpawn = CreateAttributeBool("Marker Spawn", false);
            counter = CreateAttributeInt("Counter", 0);
            for (int i = 0; i < bits.Length; i++)
                bits[i] = CreateAttributeBool("Bit " + i, false);
            for (int i = 0; i < msbs.Length; i++)
                msbs[i] = CreateAttributeBool("MSB " + i, false);
            for (int i = 0; i < boundaryBits.Length; i++)
                boundaryBits[i] = CreateAttributeBool("Boundary Bit " + i, false);
            for (int i = 0; i < boundaryMSBs.Length; i++)
                boundaryMSBs[i] = CreateAttributeBool("Boundary MSB " + i, false);
            belowSqrt = CreateAttributeBool("Below sqrt(n)", false);
            parentDir = CreateAttributeDirection("Parent", Direction.NONE);
            numPascETT = CreateAttributeInt("Num PASC ETT", 0);
            for (int i = 0; i < 6; i++)
                pascParticipant[i] = CreateAttributeBool("PASC Part. " + i, false);
            for (int i = 0; i < 6; i++)
                comparison[i] = CreateAttributeEnum<ComparisonResult>("Comparison " + i, ComparisonResult.EQUAL);
            pascFinishedBeforeCounter = CreateAttributeBool("PASC Finish", false);
            trianglePlacement0 = CreateAttributeBool("Triangle 0", false);
            trianglePlacement1 = CreateAttributeBool("Triangle 1", false);
            for (int i = 0; i < 6; i++)
                candidate[i] = CreateAttributeBool("Candidate " + i, false);
            for (int i = 0; i < 6; i++)
                newCandidate[i] = CreateAttributeBool("New Candidate " + i, false);
            visited = CreateAttributeBool("Visited", false);

            for (int i = 0; i < binops.Length; i++)
                binops[i] = new SubBinOps(p);
            binop = binops[0];
            sharedPasc = new SubPASC2(p);
            leaderElection = new SubLeaderElectionSC(p);
            ll = new SubLongestLines(p, sharedPasc);
            for (int i = 0; i < 6; i++)
                pasc[i] = new SubPASC(p);
            boundaryTest = new SubBoundaryTest(p, pasc);
            triangleCheck = new SubMergingAlgo(p, sharedPasc);
            shapeConstruction = new SubShapeConstruction(p, shape, sharedPasc);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        //public void Init(/* Custom parameters with default values */)
        //{
        //    // This code is executed directly after the constructor
        //}

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return finished;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (finished)
                return;

            int r = round.GetValue();
            switch (r)
            {
                // 1. Find the longest lines

                case 0:
                    {
                        // Start running longest lines subroutine
                        ll.Init();
                        PinConfiguration pc = GetContractedPinConfiguration();
                        ll.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        ll.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                    {
                        // Run longest lines subroutine
                        // Start boundary test when finished
                        ll.ActivateReceive();
                        if (ll.IsFinished())
                        {
                            // Store line length bit and MSB, place marker at line start
                            if (ll.IsOnMaxLine())
                            {
                                if (ll.IsMSB())
                                    msbs[R].SetValue(true);
                                if (ll.GetBit())
                                    bits[R].SetValue(true);
                                if (!HasNeighborAt(ll.GetMaxDir().Opposite()))
                                {
                                    bits[L].SetValue(true);
                                    msbs[L].SetValue(true);
                                    lineMarker.SetValue(true);
                                }
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            ll.SetupPC(pc);
                            SetPlannedPinConfiguration(pc);
                            ll.ActivateSend();
                        }
                    }
                    break;


                // 2. Limit R using the longest line of the shape

                case 2:
                    {
                        int ctr = counter.GetValue();
                        if (ctr >= longestLineStr.Length)
                        {
                            // Finished setting up the marker
                            counter.SetValue(0);
                            lineMarker.SetValue(false);
                            // Counters go to round 3, others go to round 5 with 4 global circuits
                            if (ll.IsOnMaxLine())
                            {
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                for (int i = 0; i < 4; i++)
                                    SetupGlobalCircuit(pc, i, i);
                                SetPlannedPinConfiguration(pc);
                                round.SetValue(5);
                            }
                        }
                        else
                        {
                            // Marker writes bit/MSB of longest line
                            if (lineMarker.GetValue())
                            {
                                // Store bit/MSB of s in M
                                bits[M].SetValue(longestLineStr[ctr] == '1');
                                msbs[M].SetValue(ctr == longestLineStr.Length - 1);
                            }
                            // Marker moves one position ahead
                            if (ll.IsOnMaxLine())
                            {
                                Direction pred = ll.GetMaxDir().Opposite();
                                lineMarker.SetValue(HasNeighborAt(pred) && ((SCGeneralParticle)GetNeighborAt(pred)).lineMarker.GetValue());
                            }
                            counter.SetValue(ctr + 1);
                        }
                    }
                    break;
                case 3:     // Start of binary operations
                    {
                        int ctr = counter.GetValue();
                        if (ctr >= 3)
                        {
                            // Finished initial counter setup
                            // Store K := R
                            bits[K].SetValue(bits[R]);
                            msbs[K].SetValue(msbs[R]);
                            PinConfiguration pc = GetContractedPinConfiguration();
                            for (int i = 0; i < 4; i++)
                                SetupGlobalCircuit(pc, i, i);
                            SetPlannedPinConfiguration(pc);
                            pc.SendBeepOnPartitionSet(0);
                            round.SetValue(5);
                        }
                        else
                        {
                            // Initialize binary operation
                            Direction succ = ll.GetMaxDir();
                            Direction pred = succ.Opposite();
                            succ = HasNeighborAt(succ) ? succ : Direction.NONE;
                            pred = HasNeighborAt(pred) ? pred : Direction.NONE;
                            if (ctr == 0)
                            {
                                // Compare longest line length (stored in M) to R
                                binop.Init(SubBinOps.Mode.COMP, bits[M].GetValue(), pred, succ, bits[R].GetValue());
                            }
                            else if (ctr == 1)
                            {
                                // Compute R / M
                                binop.Init(SubBinOps.Mode.DIV, bits[R].GetValue(), pred, succ, bits[M].GetValue(), msbs[R].GetValue());
                            }
                            else if (ctr == 2)
                            {
                                // Find MSB of R
                                binop.Init(SubBinOps.Mode.MSB, bits[R].GetValue(), pred, succ);
                            }
                            PinConfiguration pc = GetContractedPinConfiguration();
                            binop.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binop.ActivateSend();
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 4:
                    {
                        binop.ActivateReceive();
                        if (binop.IsFinished())
                        {
                            // Store result based on counter
                            int ctr = counter.GetValue();
                            if (ctr == 0)
                            {
                                // If s > R: Terminate
                                if (binop.CompResult() == SubComparison.ComparisonResult.GREATER)
                                {
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    for (int i = 0; i < 4; i++)
                                        SetupGlobalCircuit(pc, i, i);
                                    SetPlannedPinConfiguration(pc);
                                    for (int i = 0; i < 4; i++)
                                        pc.SendBeepOnPartitionSet(i);
                                    round.SetValue(5);
                                }
                            }
                            else if (ctr == 1)
                            {
                                // Store result of R / s in R
                                bits[R].SetValue(binop.ResultBit());
                            }
                            else if (ctr == 2)
                            {
                                // Store MSB of R
                                msbs[R].SetValue(binop.IsMSB());
                            }
                            counter.SetValue(ctr + 1);
                            round.SetValue(r - 1);
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            binop.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binop.ActivateSend();
                        }
                    }
                    break;
                case 5:     // Waiting round
                    {
                        // Wait for beeps on 4 global circuits
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        bool beep0 = pc.ReceivedBeepOnPartitionSet(0);
                        bool beep1 = pc.ReceivedBeepOnPartitionSet(1);
                        bool beep2 = pc.ReceivedBeepOnPartitionSet(2);
                        bool beep3 = pc.ReceivedBeepOnPartitionSet(3);
                        if (beep0 && beep1 && beep2 && beep3)
                        {
                            // Beep on all 4 circuits means we terminate with failure
                            SetMainColor(ColorData.Particle_Red);
                            finished.SetValue(true);
                        }
                        else if (beep0 && !beep3)
                        {
                            // Setup next phase
                            if (!shapeHasFaces)
                            {
                                // Setup boundary test subroutine and go to step 3
                                boundaryTest.Init(true);
                                pc = GetContractedPinConfiguration();
                                boundaryTest.SetupPC(pc);
                                SetPlannedPinConfiguration(pc);
                                boundaryTest.ActivateSend();
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                // Go directly to binary search (triangle case)
                                belowSqrt.SetValue(true);
                                round.SetValue(12);
                            }
                        }
                        else if (beep1)
                        {
                            // Binary search finished
                            // (a) Set K := M
                            if (belowSqrt)
                            {
                                if (ll.IsOnMaxLine())
                                {
                                    bits[K].SetValue(bits[M]);
                                    msbs[K].SetValue(msbs[M]);
                                }
                            }
                            // (b) M now holds the value N = Floor(sqrt(n))
                            else
                            {
                                markerIdx.SetValue(0);
                            }
                            // Go to linear search
                            round.SetValue(34);
                        }
                        else if (beep2)
                        {
                            // Binary search continues, only case (a)
                            // Test the triangle
                            round.SetValue(26);
                        }
                        else if (beep3)
                        {
                            // Linear search decrement is finished
                            if (!beep0)
                            {
                                // No beep on first circuits means K = 0
                                SetMainColor(ColorData.Particle_Red);
                                finished.SetValue(true);
                                return;
                            }
                            else
                            {
                                // Continue with the next scale
                                round.SetValue(39);
                            }
                        }
                    }
                    break;


                // 3. Setup the outer boundary

                case 6:
                    {
                        // Run boundary test subroutine
                        boundaryTest.ActivateReceive();
                        if (boundaryTest.IsFinished())
                        {
                            if (shapeHasHoles && !boundaryTest.InnerBoundaryExists())
                            {
                                // No need for outer boundary setup, go to step 4
                                belowSqrt.SetValue(true);
                                // Start binary search (triangle case)
                                round.SetValue(12);
                            }
                            else
                            {
                                // Store number of outer boundary occurrences and directions
                                // Also let leader beep towards predecessor
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetPlannedPinConfiguration(pc);
                                int num = 0;
                                for (int i = 0; i < boundaryTest.NumBoundaries(); i++)
                                {
                                    if (boundaryTest.IsOuterBoundary(i))
                                    {
                                        Direction pred = boundaryTest.GetBoundaryPredecessor(i);
                                        if (!boundaryTest.IsBoundaryLeader(i))
                                            outerBoundaryDirs[2 * num].SetValue(pred);
                                        else
                                            pc.GetPinAt(pred, 0).PartitionSet.SendBeep();
                                        outerBoundaryDirs[2 * num + 1].SetValue(boundaryTest.GetBoundarySuccessor(i));
                                        num++;
                                    }
                                }
                                numOuterBoundaryOccurrences.SetValue(num);
                                round.SetValue(round + 1);
                            }
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            boundaryTest.SetupPC(pc);
                            SetPlannedPinConfiguration(pc);
                            boundaryTest.ActivateSend();
                        }
                    }
                    break;

                // 3.2. Prepare computation of N = Floor(sqrt(n))
                // 3.2.1. Construct spanning tree

                case 7:
                    {
                        // Boundary end point receives beep and removes successor
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                        {
                            if (pc.GetPinAt(outerBoundaryDirs[2 * i + 1], PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                            {
                                outerBoundaryDirs[2 * i + 1].SetValue(Direction.NONE);
                                break;
                            }
                        }

                        // Establish one global and 6 axis circuits (only leader does not setup axis circuits)
                        pc = GetContractedPinConfiguration();
                        SetupGlobalCircuit(pc, 1, 6);
                        bool leader = IsOuterBoundaryLeader();
                        if (!leader)
                            SetupAxisCircuits(pc);
                        SetPlannedPinConfiguration(pc);

                        // Leader beeps in all directions
                        if (leader)
                        {
                            for (int i = 0; i < 6; i++)
                                pc.GetPinAt(DirectionHelpers.Cardinal(i), 0).PartitionSet.SendBeep();
                            SetMainColor(ColorData.Particle_Green);
                        }
                        // Everyone else beeps on the global circuit
                        else
                        {
                            pc.SendBeepOnPartitionSet(6);
                            SetMainColor(ColorData.Particle_Black);
                        }
                        round.SetValue(round + 1);
                    }
                    break;
                case 8:
                    {
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        // Listen for beep on global circuit
                        if (!pc.ReceivedBeepOnPartitionSet(6))
                        {
                            // Spanning tree construction has finished
                            // Send beep to parent
                            pc.SetToSingleton();
                            SetPlannedPinConfiguration(pc);
                            if (parentDir.GetValue() != Direction.NONE)
                                pc.GetPinAt(parentDir, 0).PartitionSet.SendBeep();
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Amoebots without parent (and not leader) listen for beeps on axis circuits
                            if (parentDir.GetValue() == Direction.NONE && !IsOuterBoundaryLeader())
                            {
                                bool foundParent = false;
                                for (int i = 0; i < 6; i++)
                                {
                                    if (pc.ReceivedBeepOnPartitionSet(i))
                                    {
                                        // Received parent beep!
                                        parentDir.SetValue(DirectionHelpers.Cardinal((i + 3) % 6));
                                        foundParent = true;
                                        break;
                                    }
                                }

                                // If we found our parent in this round: Split axis circuits and beep in all 6 directions
                                if (foundParent)
                                {
                                    pc.SetToSingleton();
                                    SetupGlobalCircuit(pc, 1, 6);
                                    SetPlannedPinConfiguration(pc);
                                    for (int i = 0; i < 6; i++)
                                        pc.GetPinAt(DirectionHelpers.Cardinal(i), 0).PartitionSet.SendBeep();
                                    SetMainColor(ColorData.Particle_Blue);
                                }
                                // Otherwise, beep on the global circuit
                                else
                                {
                                    SetPlannedPinConfiguration(pc);
                                    pc.SendBeepOnPartitionSet(6);
                                }
                            }
                        }
                    }
                    break;
                case 9:
                    {
                        // Receive beeps from children
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        Direction pDir = parentDir;
                        bool leader = pDir == Direction.NONE;
                        int dStart = leader ? 0 : pDir.ToInt();
                        List<Direction> children = new List<Direction>();
                        for (int d = dStart; d < dStart + 6; d++)
                        {
                            Direction dir = DirectionHelpers.Cardinal(d % 6);
                            if (pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                children.Add(dir);
                        }

                        // Initialize PASC for ETT
                        children.Insert(0, pDir);
                        children.Add(pDir);
                        int numPasc = children.Count - 1;
                        for (int i = 0; i < numPasc; i++)
                        {
                            bool active = i == numPasc - 1;
                            Direction pred = children[i];
                            Direction succ = children[i + 1];
                            pasc[i].Init(leader && i == 0, pred, succ, PinsPerEdge - 1, PinsPerEdge - 2, 0, 1, 2 * i, 2 * i + 1, active);
                        }
                        numPascETT.SetValue(numPasc);

                        // Start running PASC
                        pc.SetToSingleton();
                        for (int i = 0; i < numPasc; i++)
                            pasc[i].SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        for (int i = 0; i < numPasc; i++)
                            pasc[i].ActivateSend();

                        // Place marker and MSB of n at start of outer boundary
                        if (leader)
                        {
                            for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                            {
                                if (outerBoundaryDirs[2 * i].GetValue() == Direction.NONE)
                                {
                                    markerIdx.SetValue(i + 1);
                                    boundaryMSBs[4 * i + R].SetValue(true);
                                    break;
                                }
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 10:
                    {
                        // Receive PASC beeps
                        bool becamePassive = false;
                        for (int i = 0; i < numPascETT; i++)
                        {
                            pasc[i].ActivateReceive();
                            if (pasc[i].BecamePassive())
                                becamePassive = true;
                        }

                        // Setup 2 global circuits
                        PinConfiguration pc = GetContractedPinConfiguration();
                        SetupGlobalCircuit(pc, 1, 0);
                        SetupGlobalCircuit(pc, 2, 1);
                        SetPlannedPinConfiguration(pc);

                        // Beep on first if we became passive
                        if (becamePassive)
                            pc.SendBeepOnPartitionSet(0);
                        // Leader beeps on second circuit if the received bit was 1
                        if (IsOuterBoundaryLeader() && pasc[numPascETT - 1].GetReceivedBit() > 0)
                            pc.SendBeepOnPartitionSet(1);

                        // Marker sends forwarding beep to successor
                        if (markerIdx > 0)
                        {
                            Direction dir = outerBoundaryDirs[2 * (markerIdx - 1) + 1];
                            if (dir != Direction.NONE)
                                pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.SendBeep();
                        }

                        round.SetValue(r + 1);
                    }
                    break;
                case 11:
                    {
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        // Receive beeps on two global circuits
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // No beep on first partition set means we are finished
                            // Set L := 1
                            if (IsOuterBoundaryLeader())
                            {
                                for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                {
                                    if (outerBoundaryDirs[2 * i].GetValue() == Direction.NONE)
                                    {
                                        boundaryBits[4 * i + L].SetValue(true);
                                        boundaryMSBs[4 * i + L].SetValue(true);
                                        break;
                                    }
                                }
                            }

                            // Set K := R (= n)
                            for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                            {
                                boundaryBits[4 * i + K].SetValue(boundaryBits[4 * i + R].GetCurrentValue());
                                boundaryMSBs[4 * i + K].SetValue(boundaryMSBs[4 * i + R].GetCurrentValue());
                            }

                            // Go to binary search phase (non-triangle case)
                            round.SetValue(20);
                            break;
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(1))
                        {
                            // Clear MSB of R
                            for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                boundaryMSBs[3 * i + R].SetValue(false);

                            // Current marker writes the received PASC bit and MSB
                            if (markerIdx > 0)
                            {
                                boundaryBits[4 * (markerIdx - 1) + R].SetValue(true);
                                boundaryMSBs[4 * (markerIdx - 1) + R].SetValue(true);
                            }
                        }

                        // Marker moves forward
                        markerIdx.SetValue(0);
                        for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                        {
                            Direction dir = outerBoundaryDirs[2 * i];
                            if (dir != Direction.NONE && pc.GetPinAt(dir, 0).PartitionSet.ReceivedBeep())
                            {
                                markerIdx.SetValue(i + 1);
                                break;
                            }
                        }

                        // Send PASC beeps
                        pc.SetToSingleton();
                        for (int i = 0; i < numPascETT; i++)
                            pasc[i].SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        for (int i = 0; i < numPascETT; i++)
                            pasc[i].ActivateSend();

                        round.SetValue(r - 1);
                    }
                    break;


                // 4. Binary Search
                // 4.1. Check right bound R
                // 4.2. Check left bound L = 1

                // Rounds 12-15 run the entire triangle placement search

                case 12:    // 12 and 14: Scale R check
                case 14:
                case 16:    // 16 and 18: Scale L check
                case 18:
                case 26:    // 26 and 28: Scale M check
                case 28:
                case 40:    // 40 and 42: Scale K check
                case 42:
                    {
                        // Init triangle placement search for rotation 0/1
                        int rot = r == 12 || r == 16 || r == 26 || r == 40 ? 0 : 1;
                        if (ll.IsOnMaxLine())
                        {
                            int scaleIdx = r < 16 ? R : (r < 26 ? L : (r < 40 ? M : K));
                            bool scaleBit = bits[scaleIdx].GetCurrentValue();
                            bool scaleMSB = msbs[scaleIdx].GetCurrentValue();
                            Direction succ = ll.GetMaxDir();
                            Direction pred = succ.Opposite();
                            succ = HasNeighborAt(succ) ? succ : Direction.NONE;
                            pred = HasNeighborAt(pred) ? pred : Direction.NONE;
                            triangleCheck.Init(ShapeType.TRIANGLE, Direction.E, Direction.NNE, rot, true, false, pred, succ, scaleBit, scaleMSB);
                        }
                        else
                        {
                            triangleCheck.Init(ShapeType.TRIANGLE, Direction.E, Direction.NNE, rot, true);
                        }
                        PinConfiguration pc = GetContractedPinConfiguration();
                        triangleCheck.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        triangleCheck.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 13:    // 13 and 15: Scale R check
                case 15:
                case 17:    // 17 and 19: Scale L check
                case 19:
                case 27:    // 27 and 29: Scale M check
                case 29:
                case 41:    // 41 and 43: Scale K check
                case 43:
                    {
                        triangleCheck.ActivateReceive();
                        if (triangleCheck.IsFinished())
                        {
                            if (triangleCheck.Success())
                            {
                                if (r <= 15)
                                {
                                    // Checked R successfully: Set K := R and go to linear search
                                    if (ll.IsOnMaxLine())
                                    {
                                        bits[K].SetValue(bits[R]);
                                        msbs[K].SetValue(msbs[R]);
                                    }
                                    round.SetValue(34);
                                }
                                else if (r <= 19)
                                {
                                    // Checked L successfully: Move on to rest of the binary search
                                    round.SetValue(20);
                                }
                                else if (r < 41)
                                {
                                    // Checked M successfully: Set L := M and go back to binary search start
                                    bits[L].SetValue(bits[M]);
                                    msbs[L].SetValue(msbs[M]);
                                    round.SetValue(20);
                                }
                                else
                                {
                                    if (r == 41)
                                        trianglePlacement0.SetValue(triangleCheck.IsRepresentative());
                                    else
                                        trianglePlacement1.SetValue(triangleCheck.IsRepresentative());
                                    round.SetValue(r + 1);
                                }
                            }
                            else
                            {
                                if (r <= 17 || r == 27 || r >= 41)
                                {
                                    // Check for rotation 0 failed, go to next one
                                    // OR finished check for R with failure, just move on to check for L
                                    // OR finished triangle check for K in linear search, just move on
                                    round.SetValue(r + 1);
                                }
                                else if (r == 19)
                                {
                                    // Finished check for L with failure: Terminate
                                    SetMainColor(ColorData.Particle_Red);
                                    finished.SetValue(true);
                                    break;
                                }
                                else if (r == 29)
                                {
                                    // Finished check for M with failure: Set R := M and go back to binary search start
                                    bits[R].SetValue(bits[M]);
                                    msbs[R].SetValue(msbs[M]);
                                    round.SetValue(20);
                                }
                            }
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            triangleCheck.SetupPC(pc);
                            SetPlannedPinConfiguration(pc);
                            triangleCheck.ActivateSend();
                        }
                    }
                    break;

                // 4.3. Binary search procedure

                case 20:    // M := L + R
                case 22:    // Get shifted M and find MSB
                case 24:    // Compare L to M
                case 30:    // Compute M^2
                case 32:    // Compare M^2 to n
                    {
                        if (belowSqrt && ll.IsOnMaxLine() || !belowSqrt && boundaryTest.OnOuterBoundary())
                        {
                            bool onOuterBoundary = !belowSqrt;
                            if (r == 20)
                            {
                                // Compute M := L + R
                                StartBinOp(onOuterBoundary, SubBinOps.Mode.ADD, L, R);
                            }
                            else if (r == 22)
                            {
                                // Get shifted bits
                                bool bit1 = false;
                                if (onOuterBoundary)
                                {
                                    // Receive from beep
                                    PinConfiguration pc = GetCurrentPinConfiguration();
                                    for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                    {
                                        Direction succ = outerBoundaryDirs[2 * i + 1];
                                        bool bit = succ != Direction.NONE && pc.GetPinAt(succ, PinsPerEdge - 1).PartitionSet.ReceivedBeep();
                                        boundaryBits[4 * i + M].SetValue(bit);
                                        bit1 = bit1 || bit;
                                    }
                                }
                                else
                                {
                                    // Get from successor
                                    Direction succ = ll.GetMaxDir();
                                    bit1 = succ != Direction.NONE && HasNeighborAt(succ) && ((SCGeneralParticle)GetNeighborAt(succ)).bits[M].GetValue();
                                    bits[M].SetValue(bit1);
                                }

                                // Update color to show current M
                                bool leader = onOuterBoundary && boundaryTest.IsOuterBoundaryLeader();
                                if (bit1 && leader)
                                    SetMainColor(ColorData.Particle_Aqua);
                                else if (bit1 && !leader)
                                    SetMainColor(new Color(1, 1, 0));
                                else if (!bit1 && leader)
                                    SetMainColor(ColorData.Particle_Green);
                                else
                                    SetMainColor(ColorData.Particle_Blue);

                                // Start binary operation for finding MSB of M
                                StartBinOp(onOuterBoundary, SubBinOps.Mode.MSB, M);
                            }
                            else if (r == 24)
                            {
                                // Compare L to R
                                StartBinOp(onOuterBoundary, SubBinOps.Mode.COMP, L, M);
                            }
                            else if (r == 30)
                            {
                                // Compute M^2
                                StartBinOp(true, SubBinOps.Mode.MULT, M, M, M);
                            }
                            else if (r == 32)
                            {
                                // Compare M^2 to n (using bits of previous binop)
                                StartBinOp(true, SubBinOps.Mode.COMP, -1, K);
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Non-counter amoebots go to round 5 and wait
                            PinConfiguration pc = GetContractedPinConfiguration();
                            for (int i = 0; i < 4; i++)
                                SetupGlobalCircuit(pc, i, i);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(5);
                        }
                    }
                    break;
                case 21:    // M := L + R
                case 23:    // MSB of M
                case 25:    // Compare L to M
                case 31:    // M^2
                case 33:    // Compare M^2 to n
                    {
                        // This code is only run by the counter amoebots
                        binops[0].ActivateReceive();
                        bool onOuterBoundary = !belowSqrt;
                        if (onOuterBoundary)
                        {
                            for (int i = 1; i < numOuterBoundaryOccurrences; i++)
                                binops[i].ActivateReceive();
                        }
                        // All binary ops instances finish at the same time
                        if (binops[0].IsFinished())
                        {
                            if (r == 21)
                            {
                                if (onOuterBoundary)
                                {
                                    // Let amoebots send bits of M to predecessor
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    SetPlannedPinConfiguration(pc);
                                    for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                    {
                                        Direction pred = outerBoundaryDirs[2 * i];
                                        if (binops[i].ResultBit() && pred != Direction.NONE)
                                        {
                                            pc.GetPinAt(pred, 0).PartitionSet.SendBeep();
                                        }
                                    }
                                }
                                else
                                {
                                    // Store bit
                                    bits[M].SetValue(binop.ResultBit());
                                }
                            }
                            else if (r == 23)
                            {
                                // Set MSB
                                if (onOuterBoundary)
                                {
                                    for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                    {
                                        boundaryMSBs[4 * i + M].SetValue(binops[i].IsMSB());
                                    }
                                }
                                else
                                {
                                    msbs[M].SetValue(binop.IsMSB());
                                }
                            }
                            else if (r == 25)
                            {
                                // All binary op instances have the same result
                                SubComparison.ComparisonResult comp = binop.CompResult();
                                if (comp == SubComparison.ComparisonResult.EQUAL || !onOuterBoundary)
                                {
                                    // L = M: Binary search finished
                                    // OR not on outer boundary: Send beep on third circuit
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    for (int i = 0; i < 4; i++)
                                        SetupGlobalCircuit(pc, i, i);
                                    SetPlannedPinConfiguration(pc);
                                    pc.SendBeepOnPartitionSet(comp == SubComparison.ComparisonResult.EQUAL ? 1 : 2);
                                    round.SetValue(5);
                                    break;
                                }
                                else
                                {
                                    // Start square root check
                                    round.SetValue(30);
                                    break;
                                }
                            }
                            else if (r == 31)
                            {
                                // Just computed M^2, result is stored in binops, continue
                            }
                            else if (r == 33)
                            {
                                // Compared M^2 to n
                                SubComparison.ComparisonResult comp = binop.CompResult();
                                if (comp == SubComparison.ComparisonResult.EQUAL)
                                {
                                    // M^2 = n: Found exactly the square root, finish binary search
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    for (int i = 0; i < 4; i++)
                                        SetupGlobalCircuit(pc, i, i);
                                    SetPlannedPinConfiguration(pc);
                                    pc.SendBeepOnPartitionSet(1);
                                    round.SetValue(5);
                                    break;
                                }
                                else
                                {
                                    // M^2 < n or M^2 > n, set L := M or R := M
                                    int idx = comp == SubComparison.ComparisonResult.LESS ? L : R;
                                    for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                    {
                                        boundaryBits[4 * i + idx].SetValue(boundaryBits[4 * i + M]);
                                        boundaryMSBs[4 * i + idx].SetValue(boundaryMSBs[4 * i + M]);
                                    }
                                    // Continue with next iteration
                                    round.SetValue(20);
                                    break;
                                }
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            ActivateBinopsSend(onOuterBoundary);
                        }
                    }
                    break;

                // (b) Rounds 30-33 implement the square root check

                // 5. Linear Search
                // 5.0. Place marker spawn on the longest lines at distance K

                // Rounds 34-38 implement the PASC procedure and distance check with distance K

                case 34:    // PASC setup
                case 47:    // Angle distance check before first edge
                case 55:    // Distance check before edge traversal
                case 63:    // Edge traversal
                    {
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (r == 34)
                        {
                            // Setup PASC on all longest lines in the system
                            if (ll.IsOnMaxLine())
                            {
                                Direction dir = ll.GetMaxDir();
                                Direction pred = HasNeighborAt(dir.Opposite()) ? dir.Opposite() : Direction.NONE;
                                Direction succ = HasNeighborAt(dir) ? dir : Direction.NONE;
                                bool leader = pred == Direction.NONE;
                                pasc[0].Init(leader, pred, succ, PinsPerEdge - 1, PinsPerEdge - 2, 0, 1, 0, 1);
                                comparison[0].SetValue(ComparisonResult.EQUAL);
                                pascParticipant[0].SetValue(true);
                            }
                            else
                                pascParticipant[0].SetValue(false);
                        }
                        else if (r == 47 || r == 55)
                        {
                            // Setup PASC on all axes where the activation beep was received
                            int d = r == 47 ? distanceCheckDir.ToInt() : traversalDirs[counter].ToInt();
                            for (int i = 0; i < 6; i++)
                            {
                                Direction dir = DirectionHelpers.Cardinal((i + d) % 6);
                                Direction pred = HasNeighborAt(dir) && pc.GetPinAt(dir, 0).PartitionSet.ReceivedBeep() ? dir : Direction.NONE;
                                Direction succ = candidate[i] || pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep() ? dir.Opposite() : Direction.NONE;
                                if (pred != Direction.NONE || succ != Direction.NONE)
                                {
                                    bool leader = pred == Direction.NONE;
                                    pasc[i].Init(leader, pred, succ, PinsPerEdge - 1, PinsPerEdge - 2, 0, 1, 2 * i, 2 * i + 1);
                                    comparison[i].SetValue(ComparisonResult.EQUAL);
                                    pascParticipant[i].SetValue(true);
                                }
                                else
                                    pascParticipant[i].SetValue(false);
                            }
                        }
                        else if (r == 63)
                        {
                            // Setup PASC on the axes where the activation beep was received, using the candidate as leader
                            int d = traversalDirs[counter].ToInt();
                            for (int i = 0; i < 6; i++)
                            {
                                Direction dir = DirectionHelpers.Cardinal((i + d) % 6);
                                Direction pred = pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep() ? dir.Opposite() : Direction.NONE;
                                Direction succ = pc.GetPinAt(dir, 0).PartitionSet.ReceivedBeep() ? dir : Direction.NONE;
                                if (pred != Direction.NONE || succ != Direction.NONE)
                                {
                                    bool leader = pred == Direction.NONE;
                                    pasc[i].Init(leader, pred, succ, PinsPerEdge - 1, PinsPerEdge - 2, 0, 1, 2 * i, 2 * i + 1);
                                    comparison[i].SetValue(ComparisonResult.EQUAL);
                                    pascParticipant[i].SetValue(true);
                                }
                                else
                                    pascParticipant[i].SetValue(false);
                            }
                        }

                        // Place marker at the counter start
                        lineMarker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));

                        // Start the new PASC subroutines
                        pc = GetContractedPinConfiguration();
                        for (int i = 0; i < 6; i++)
                        {
                            if (pascParticipant[i].GetCurrentValue())
                                pasc[i].SetupPC(pc);
                        }
                        SetPlannedPinConfiguration(pc);
                        for (int i = 0; i < 6; i++)
                        {
                            if (pascParticipant[i].GetCurrentValue())
                                pasc[i].ActivateSend();
                        }
                        pascFinishedBeforeCounter.SetValue(false);
                        round.SetValue(r + 1);
                    }
                    break;
                case 35:
                case 48:
                case 56:
                case 64:
                    {
                        // Receive all PASC beeps
                        bool becamePassive = false;
                        for (int i = 0; i < 6; i++)
                        {
                            if (pascParticipant[i])
                            {
                                pasc[i].ActivateReceive();
                                becamePassive = becamePassive || pasc[i].BecamePassive();
                            }
                        }

                        // Setup 3 global circuits, let the marker send bits and MSB of K, let amoebots that became passive beep
                        PinConfiguration pc = GetContractedPinConfiguration();
                        for (int i = 0; i < 3; i++)
                            SetupGlobalCircuit(pc, i, i);
                        SetPlannedPinConfiguration(pc);
                        if (lineMarker)
                        {
                            if (bits[K])
                                pc.SendBeepOnPartitionSet(0);
                            if (msbs[K])
                                pc.SendBeepOnPartitionSet(1);
                        }
                        if (becamePassive)
                            pc.SendBeepOnPartitionSet(2);

                        round.SetValue(r + 1);
                    }
                    break;
                case 36:
                case 49:
                case 57:
                case 65:
                    {
                        // Receive the 3 global circuit beeps
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        bool beep0 = pc.ReceivedBeepOnPartitionSet(0);  // Bit of K
                        bool beep1 = pc.ReceivedBeepOnPartitionSet(1);  // MSB of K
                        bool beep2 = pc.ReceivedBeepOnPartitionSet(2);  // PASC continuation
                        bool pascFinishedFirst = !beep2 && !beep1;  // No PASC continuation beep and no MSB beep

                        // Update comparison results
                        for (int i = 0; i < 6; i++)
                        {
                            if (pascParticipant[i])
                            {
                                if (pascFinishedFirst)
                                    comparison[i].SetValue(ComparisonResult.LESS);
                                else
                                {
                                    bool pascBit = pasc[i].GetReceivedBit() > 0;
                                    if (pascBit && !beep0)
                                        comparison[i].SetValue(ComparisonResult.GREATER);
                                    else if (!pascBit && beep0)
                                        comparison[i].SetValue(ComparisonResult.LESS);
                                }
                            }
                        }

                        // Marker moves forward
                        if (ll.IsOnMaxLine())
                        {
                            Direction pred = ll.GetMaxDir().Opposite();
                            lineMarker.SetValue(!beep1 && HasNeighborAt(pred) && ((SCGeneralParticle)GetNeighborAt(pred)).lineMarker);
                        }

                        if (!beep2)
                        {
                            // PASC is finished
                            pascFinishedBeforeCounter.SetValue(pascFinishedFirst);
                            round.SetValue(r + 2);
                        }
                        else
                        {
                            pc = GetContractedPinConfiguration();
                            if (beep1)
                            {
                                // PASC and counter have finished at the same time
                                // Start PASC cutoff
                                for (int i = 0; i < 6; i++)
                                {
                                    if (pascParticipant[i])
                                        pasc[i].SetupCutoffCircuit(pc);
                                }
                                SetPlannedPinConfiguration(pc);
                                for (int i = 0; i < 6; i++)
                                {
                                    if (pascParticipant[i])
                                        pasc[i].SendCutoffBeep();
                                }
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                // Continue running PASC
                                for (int i = 0; i < 6; i++)
                                {
                                    if (pascParticipant[i])
                                        pasc[i].SetupPC(pc);
                                }
                                SetPlannedPinConfiguration(pc);
                                for (int i = 0; i < 6; i++)
                                {
                                    if (pascParticipant[i])
                                        pasc[i].ActivateSend();
                                }
                                round.SetValue(r - 1);
                            }
                        }
                    }
                    break;
                case 37:
                case 50:
                case 58:
                case 66:
                    {
                        // Receive PASC cutoff and update comparison results
                        for (int i = 0; i < 6; i++)
                        {
                            if (pascParticipant[i])
                            {
                                pasc[i].ReceiveCutoffBeep();
                                if (pasc[i].GetReceivedBit() > 0)
                                    comparison[i].SetValue(ComparisonResult.GREATER);
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 38:    // PASC evaluation
                case 51:    // Angle distance check before first edge
                case 59:    // Distance check before edge traversal
                case 67:    // Edge traversal
                    {
                        lineMarker.SetValue(false);
                        if (r == 38)
                        {
                            // Amoebots with result EQUAL set marker spawn flags
                            if (pascParticipant[0] && comparison[0].GetValue() == ComparisonResult.EQUAL)
                                markerSpawn.SetValue(true);
                            round.SetValue(r + 1);
                        }
                        else if (r == 51 || r == 59)
                        {
                            // Eliminate candidates based on comparison result
                            for (int i = 0; i < 6; i++)
                            {
                                if (pascParticipant[i] && candidate[i] && comparison[i].GetValue() == ComparisonResult.LESS)
                                    candidate[i].SetValue(false);
                            }
                            if (pascFinishedBeforeCounter)
                            {
                                // No candidate has enough space, skip this scale (go to step 5.7.)
                                round.SetValue(70);
                            }
                            else
                            {
                                if (r == 51)    // Angle distance check
                                {
                                    // Setup global circuit and let remaining candidates beep
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    pc.SetToGlobal(0);
                                    SetPlannedPinConfiguration(pc);
                                    for (int i = 0; i < 6; i++)
                                    {
                                        if (candidate[i].GetCurrentValue())
                                        {
                                            pc.SendBeepOnPartitionSet(0);
                                            break;
                                        }
                                    }
                                }
                                else if (r == 59)   // Edge traversal distance check
                                {
                                    // Establish axis circuits, split at candidates with comparison result EQUAL
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    bool[] split = new bool[6];
                                    int d = traversalDirs[counter].ToInt();
                                    for (int i = 0; i < 6; i++)
                                    {
                                        if (candidate[i].GetCurrentValue() && comparison[i].GetValue() == ComparisonResult.EQUAL)
                                        {
                                            split[(i + d) % 6] = true;
                                        }
                                    }
                                    SetupAxisCircuits(pc, split);
                                    SetPlannedPinConfiguration(pc);
                                    // Let these candidates beep and retire
                                    for (int i = 0; i < 6; i++)
                                    {
                                        int dir = (i + d) % 6;
                                        if (split[dir])
                                        {
                                            pc.GetPinAt(DirectionHelpers.Cardinal(dir), 0).PartitionSet.SendBeep();
                                            candidate[i].SetValue(false);
                                        }
                                    }
                                }
                                round.SetValue(r + 1);
                            }
                        }
                        else if (r == 67)
                        {
                            // Amoebots with result EQUAL become new candidates, PASC leaders retire
                            for (int i = 0; i < 6; i++)
                            {
                                if (pascParticipant[i])
                                {
                                    if (pasc[i].IsLeader())
                                        candidate[i].SetValue(false);
                                    else if (comparison[i].GetValue() == ComparisonResult.EQUAL)
                                        newCandidate[i].SetValue(true);
                                }
                            }
                            round.SetValue(61);
                        }
                    }
                    break;

                // 5.1. Triangle placement search (and loop start)

                case 39:
                    {
                        // Start triangle placement search if the shape has faces
                        if (shapeHasFaces)
                        {
                            trianglePlacement0.SetValue(false);
                            trianglePlacement1.SetValue(false);
                            round.SetValue(40);     // Check is like rounds 12-15
                        }
                        // Otherwise, skip the check and go to 5.2.
                        else
                        {
                            round.SetValue(44);
                        }
                    }
                    break;

                // 5.2. K <= sqrt(n) update

                case 44:
                    {
                        if (belowSqrt)
                            round.SetValue(46);
                        else
                        {
                            // Start comparison by placing markers and letting them send the first bits and MSBs on global circuits
                            comparison[0].SetValue(ComparisonResult.EQUAL);
                            PinConfiguration pc = GetContractedPinConfiguration();
                            for (int i = 0; i < 4; i++)
                                SetupGlobalCircuit(pc, i, i);
                            SetPlannedPinConfiguration(pc);
                            if (ll.IsOnMaxLine())
                            {
                                if (!HasNeighborAt(ll.GetMaxDir().Opposite()))
                                {
                                    lineMarker.SetValue(true);
                                    if (bits[K])
                                        pc.SendBeepOnPartitionSet(0);
                                    if (msbs[K])
                                        pc.SendBeepOnPartitionSet(1);
                                }
                            }
                            if (boundaryTest.IsOuterBoundaryLeader())
                            {
                                for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                {
                                    if (outerBoundaryDirs[2 * i].GetValue() == Direction.NONE)
                                    {
                                        markerIdx.SetValue(i + 1);
                                        break;
                                    }
                                }
                                if (boundaryBits[4 * (markerIdx.GetCurrentValue() - 1) + M])
                                    pc.SendBeepOnPartitionSet(2);
                                if (boundaryMSBs[4 * (markerIdx.GetCurrentValue() - 1) + M])
                                    pc.SendBeepOnPartitionSet(3);
                            }
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 45:
                    {
                        // Receive beeps on 4 global circuits
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        bool bitK = pc.ReceivedBeepOnPartitionSet(0);
                        bool msbK = pc.ReceivedBeepOnPartitionSet(1);
                        bool bitN = pc.ReceivedBeepOnPartitionSet(2);
                        bool msbN = pc.ReceivedBeepOnPartitionSet(3);

                        // Update comparison result
                        if (bitK && !bitN)
                            comparison[0].SetValue(ComparisonResult.GREATER);
                        else if (!bitK && bitN)
                            comparison[0].SetValue(ComparisonResult.LESS);

                        // Finish if one of the MSBs is reached
                        if (msbK || msbN)
                        {
                            // Have to update the result again
                            if (!msbK && msbN)
                                comparison[0].SetValue(ComparisonResult.GREATER);
                            else if (msbK && !msbN)
                                comparison[0].SetValue(ComparisonResult.LESS);
                            // Set flag based on result
                            belowSqrt.SetValue(comparison[0].GetCurrentValue() != ComparisonResult.GREATER);
                            // Reset markers
                            markerIdx.SetValue(0);
                            lineMarker.SetValue(false);
                            round.SetValue(r + 1);
                        }
                        // Otherwise continue sending the beeps
                        else
                        {
                            SetPlannedPinConfiguration(pc);

                            // Move markers and send the same beeps
                            if (ll.IsOnMaxLine())
                            {
                                Direction pred = ll.GetMaxDir().Opposite();
                                lineMarker.SetValue(HasNeighborAt(pred) && ((SCGeneralParticle)GetNeighborAt(pred)).lineMarker);
                                if (lineMarker.GetCurrentValue())
                                {
                                    if (bits[K])
                                        pc.SendBeepOnPartitionSet(0);
                                    if (msbs[K])
                                        pc.SendBeepOnPartitionSet(1);
                                }
                            }
                            if (boundaryTest.OnOuterBoundary())
                            {
                                markerIdx.SetValue(0);
                                for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                                {
                                    Direction pred = outerBoundaryDirs[2 * i].GetValue();
                                    if (pred != Direction.NONE)
                                    {
                                        // Have to look up predecessor's marker index and successor direction to find the right marker position
                                        SCGeneralParticle nbr = (SCGeneralParticle)GetNeighborAt(pred);
                                        if (nbr.markerIdx > 0 && nbr.outerBoundaryDirs[2 * (nbr.markerIdx - 1) + 1] == pred.Opposite())
                                        {
                                            markerIdx.SetValue(i + 1);
                                            break;
                                        }
                                    }
                                }
                                if (markerIdx.GetCurrentValue() > 0)
                                {
                                    if (boundaryBits[4 * (markerIdx.GetCurrentValue() - 1) + M])
                                        pc.SendBeepOnPartitionSet(2);
                                    if (boundaryMSBs[4 * (markerIdx.GetCurrentValue() - 1) + M])
                                        pc.SendBeepOnPartitionSet(3);
                                }
                            }
                        }
                    }
                    break;

                // 5.3. Initialize candidate sets

                case 46:
                    {
                        // Reset candidate flags to true
                        for (int i = 0; i < 6; i++)
                            candidate[i].SetValue(true);
                        // Remove invalid faces
                        RemoveInvalidFaces(traversalNodes[0]);

                        if (belowSqrt)
                        {
                            // Start simple traversal
                            counter.SetValue(0);
                            round.SetValue(53);
                        }
                        else
                        {
                            // Need to run the distance limitation for all candidates that still exist
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupCandidateAxisCircuits(pc, distanceCheckDir.ToInt());
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 52:
                    {
                        // Listen for beep on global circuit sent by remaining candidates
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Received no beep: No candidates left, skip to step 5.7.
                            round.SetValue(70);
                        }
                        else
                        {
                            // Go to traversal
                            counter.SetValue(0);
                            round.SetValue(r + 1);
                        }
                    }
                    break;

                // 5.4./5.5. Shape traversal

                case 53:
                    {
                        if (counter >= traversalDirs.Length)
                        {
                            // Finished traversal with surviving candidates
                            // Go to shape construction
                            round.SetValue(73);
                        }
                        else
                        {
                            // Continue with next edge traversal
                            if (belowSqrt)
                            {
                                // Place marker at the marker spawn
                                lineMarker.SetValue(markerSpawn);
                                // Setup global circuit and let the marker beep
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuit(pc, 0, 0);
                                SetPlannedPinConfiguration(pc);
                                if (markerSpawn)
                                    pc.SendBeepOnPartitionSet(0);
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                // Setup directional circuits and let candidates beep on the axis circuits for the new movement direction
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupCandidateAxisCircuits(pc, traversalDirs[counter].ToInt());
                                round.SetValue(r + 2);
                            }
                        }
                    }
                    break;
                case 54:    // Simple edge traversal
                    {
                        // Receive beep on global circuit
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        // Set visited flag
                        for (int i = 0; i < 6; i++)
                        {
                            if (candidate[i])
                            {
                                visited.SetValue(true);
                                break;
                            }
                        }
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Move candidates by one step
                            int edgeDir = traversalDirs[counter].ToInt();
                            for (int i = 0; i < 6; i++)
                            {
                                int d = (edgeDir + i + 3) % 6;
                                Direction dir = DirectionHelpers.Cardinal(d);
                                candidate[i].SetValue(HasNeighborAt(dir) && ((SCGeneralParticle)GetNeighborAt(dir)).candidate[i]);
                            }
                            // Move marker one step down the counter
                            if (ll.IsOnMaxLine())
                            {
                                Direction dir = ll.GetMaxDir();
                                bool hasMarker = HasNeighborAt(dir) && ((SCGeneralParticle)GetNeighborAt(dir)).lineMarker;
                                lineMarker.SetValue(hasMarker);
                                // Marker beeps on global circuit if it has not reached the counter's end yet
                                if (hasMarker && HasNeighborAt(dir.Opposite()))
                                {
                                    SetPlannedPinConfiguration(pc);
                                    pc.SendBeepOnPartitionSet(0);
                                }
                            }
                        }
                        else
                        {
                            // Edge traversal is over
                            lineMarker.SetValue(false);
                            round.SetValue(68);
                        }
                    }
                    break;
                case 60:    // Edge traversal for K > sqrt(n)
                    {
                        // Receive beep on axis circuits, indicating first candidate's traversal ending exactly at boundaries
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        int d = traversalDirs[counter].ToInt();
                        for (int i = 0; i < 6; i++)
                        {
                            Direction dir = DirectionHelpers.Cardinal((i + d) % 6);
                            if (!HasNeighborAt(dir) && pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                newCandidate[i].SetValue(true);
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 61:
                    {
                        // Establish global and directional circuits, split at candidates that still need to be moved
                        PinConfiguration pc = GetContractedPinConfiguration();
                        SetupGlobalCircuit(pc, 1, 6);
                        SetupCandidateAxisCircuits(pc, traversalDirs[counter].ToInt());
                        // Also beep on global circuit if we are a candidate
                        for (int i = 0; i < 6; i++)
                        {
                            if (candidate[i])
                            {
                                pc.SendBeepOnPartitionSet(6);
                                break;
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 62:
                    {
                        // Receive global and axis beeps
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(6))
                        {
                            // No beep on the global circuit means all candidates have been shifted
                            round.SetValue(r + 6);
                        }
                        else
                        {
                            // Establish axis circuits again, split at candidates, but let boundary amoebots that received a beep send a reply
                            bool[] sendBeep = new bool[6];
                            for (int i = 0; i < 6; i++)
                            {
                                Direction dir = DirectionHelpers.Cardinal(i);
                                if (!HasNeighborAt(dir) && pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                    sendBeep[(i + 3) % 6] = true;
                            }
                            pc.SetToSingleton();
                            int d = traversalDirs[counter].ToInt();
                            SetupCandidateAxisCircuits(pc, d, false);
                            SetPlannedPinConfiguration(pc);
                            for (int i = 0; i < 6; i++)
                            {
                                if (sendBeep[i])
                                    pc.GetPinAt(DirectionHelpers.Cardinal(i), PinsPerEdge - 1).PartitionSet.SendBeep();
                            }
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 68:    // Traversals merge here again
                    {
                        // Turn new candidates into normal candidates
                        if (!belowSqrt)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                candidate[i].SetValue(newCandidate[i]);
                                newCandidate[i].SetValue(false);
                            }
                        }

                        RemoveInvalidFaces(traversalNodes[counter + 1]);

                        // Let remaining candidates beep on global circuit
                        PinConfiguration pc = GetContractedPinConfiguration();
                        SetupGlobalCircuit(pc, 0, 0);
                        SetPlannedPinConfiguration(pc);
                        for (int i = 0; i < 6; i++)
                        {
                            if (candidate[i].GetCurrentValue())
                            {
                                pc.SendBeepOnPartitionSet(0);
                                break;
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 69:
                    {
                        // Beep on global circuit means there are still candidates left
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // No candidates, skip this scale (step 5.7.)
                            round.SetValue(70);
                        }
                        else
                        {
                            // Have candidates left, repeat for the next edge
                            counter.SetValue(counter + 1);
                            round.SetValue(53);
                        }
                        // Reset visited state
                        visited.SetValue(false);
                    }
                    break;

                // 5.7. Decrement K

                case 70:
                    {
                        PinConfiguration pc = GetContractedPinConfiguration();
                        // Split by line counters and others
                        if (ll.IsOnMaxLine())
                        {
                            // Move the marker spawn by one position
                            Direction succ = ll.GetMaxDir();
                            Direction pred = succ.Opposite();
                            bool hasSuccessor = HasNeighborAt(succ);
                            bool hasPredecessor = HasNeighborAt(pred);
                            markerSpawn.SetValue(hasSuccessor && hasPredecessor && ((SCGeneralParticle)GetNeighborAt(succ)).markerSpawn);

                            // Start binary operation for decrementing K
                            succ = hasSuccessor ? succ : Direction.NONE;
                            pred = hasPredecessor ? pred : Direction.NONE;
                            binop.Init(SubBinOps.Mode.SUB, bits[K], pred, succ, !hasPredecessor);
                            binop.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binop.ActivateSend();
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            for (int i = 0; i < 4; i++)
                                SetupGlobalCircuit(pc, i, i);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(5);
                        }
                    }
                    break;
                case 71:
                case 72:
                    {
                        binop.ActivateReceive();
                        PinConfiguration pc = GetContractedPinConfiguration();
                        if (binop.IsFinished())
                        {
                            if (r == 71)
                            {
                                // Store new K
                                bits[K].SetValue(binop.ResultBit());
                                // Start binop to find MSB of K
                                Direction succ = ll.GetMaxDir();
                                Direction pred = succ.Opposite();
                                succ = HasNeighborAt(succ) ? succ : Direction.NONE;
                                pred = HasNeighborAt(pred) ? pred : Direction.NONE;
                                binop.Init(SubBinOps.Mode.MSB, bits[K].GetCurrentValue(), pred, succ);
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                // Store MSB of K
                                msbs[K].SetValue(binop.IsMSB());
                                // Setup 4 global circuits and beep on fourth
                                // Let non-zero bits of K beep on first
                                for (int i = 0; i < 4; i++)
                                    SetupGlobalCircuit(pc, i, i);
                                SetPlannedPinConfiguration(pc);
                                pc.SendBeepOnPartitionSet(3);
                                if (bits[K])
                                    pc.SendBeepOnPartitionSet(0);
                                round.SetValue(5);
                                break;
                            }
                        }
                        binop.SetupPinConfig(pc);
                        SetPlannedPinConfiguration(pc);
                        binop.ActivateSend();
                    }
                    break;

                // 6. Shape Construction

                case 73:
                    {
                        // Setup leader election on remaining candidates
                        bool cand = false;
                        for (int i = 0; i < 6; i++)
                        {
                            if (candidate[i])
                            {
                                cand = true;
                                break;
                            }
                        }
                        leaderElection.Init(cand, true);
                        PinConfiguration pc = GetContractedPinConfiguration();
                        leaderElection.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        leaderElection.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 74:
                    {
                        leaderElection.ActivateReceive();
                        PinConfiguration pc = GetContractedPinConfiguration();
                        if (leaderElection.IsFinished())
                        {
                            // Setup 3 global circuits
                            for (int i = 0; i < 3; i++)
                                SetupGlobalCircuit(pc, i, i);
                            SetPlannedPinConfiguration(pc);

                            // Let the leader select a random valid rotation and broadcast it
                            if (leaderElection.IsLeader())
                            {
                                List<int> rotations = new List<int>();
                                for (int i = 0; i < 6; i++)
                                {
                                    if (candidate[i])
                                        rotations.Add(i);
                                }
                                int rotation = rotations[Random.Range(0, rotations.Count)];
                                if ((rotation & 1) > 0)
                                    pc.SendBeepOnPartitionSet(0);
                                if ((rotation & 2) > 0)
                                    pc.SendBeepOnPartitionSet(1);
                                if ((rotation & 4) > 0)
                                    pc.SendBeepOnPartitionSet(2);
                            }
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            leaderElection.SetupPC(pc);
                            SetPlannedPinConfiguration(pc);
                            leaderElection.ActivateSend();
                        }
                    }
                    break;
                case 75:
                    {
                        // Receive the selected rotation
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        int rotation = 0;
                        if (pc.ReceivedBeepOnPartitionSet(0))
                            rotation += 1;
                        if (pc.ReceivedBeepOnPartitionSet(1))
                            rotation += 2;
                        if (pc.ReceivedBeepOnPartitionSet(2))
                            rotation += 4;

                        // Setup shape construction
                        lineMarker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));
                        shapeConstruction.Init(leaderElection.IsLeader(), rotation);
                        ActivateShapeConstrSend(false);
                        round.SetValue(r + 1);
                    }
                    break;
                case 76:
                    {
                        shapeConstruction.ActivateReceive();

                        SubShapeConstruction.ShapeElement t = shapeConstruction.ElementType();
                        if (t == SubShapeConstruction.ShapeElement.NODE)
                            SetMainColor(ColorData.Particle_Green);
                        else if (t == SubShapeConstruction.ShapeElement.EDGE)
                            SetMainColor(ColorData.Particle_Blue);
                        else if (t == SubShapeConstruction.ShapeElement.FACE)
                            SetMainColor(ColorData.Particle_Aqua);
                        else
                            SetMainColor(ColorData.Particle_Black);

                        if (shapeConstruction.IsFinished())
                        {
                            finished.SetValue(true);
                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                        }
                        else
                        {
                            if (shapeConstruction.ResetScaleCounter())
                            {
                                // Set marker to start
                                lineMarker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                ActivateShapeConstrSend();
                            }
                        }
                    }
                    break;
                case 77:
                    {
                        ActivateShapeConstrSend();
                        round.SetValue(r - 1);
                    }
                    break;
            }
            if (r > 33 && r < 73)
                SetLinearSearchColor();
        }

        private void SetLinearSearchColor()
        {
            int r = round;
            // Keep color during triangle check
            if (r >= 40 && r <= 43)
                return;

            if (markerSpawn.GetCurrentValue())
                SetMainColor(ColorData.Particle_Orange);
            else if (lineMarker.GetCurrentValue())
                SetMainColor(new Color(1, 1, 0));
            else
            {
                bool cand = false;
                for (int i = 0; i < 6; i++)
                {
                    if (candidate[i].GetCurrentValue())
                    {
                        cand = true;
                        break;
                    }
                }
                bool newCand = false;
                for (int i = 0; i < 6; i++)
                {
                    if (newCandidate[i].GetCurrentValue())
                    {
                        newCand = true;
                        break;
                    }
                }
                if (cand)
                    SetMainColor(ColorData.Particle_Green);
                else if (newCand)
                    SetMainColor(ColorData.Particle_Blue);
                else if (visited.GetCurrentValue())
                    SetMainColor(ColorData.Particle_Blue);
                else
                    SetMainColor(ColorData.Particle_Black);
            }
        }

        /// <summary>
        /// Helper to remove the candidate status where faces
        /// cannot be placed.
        /// </summary>
        /// <param name="nodeIdx">The node index whose faces we use
        /// to eliminate the candidates.</param>
        private void RemoveInvalidFaces(int nodeIdx)
        {
            if (!shapeHasFaces)
                return;
            if (!trianglePlacement0.GetValue())
            {
                // Can eliminate faces with tip pointing up where we are the lower left corner
                // Face at 0 => Eliminate rotation 0
                // Face at 5 => Eliminate rotation 1
                // 4 => 2, 3 => 3, 2 => 4, 1 => 5
                for (int d = 0; d < 6; d++)
                {
                    if (incidentFaceMatrix[nodeIdx, (6 - d) % 6])
                        candidate[d].SetValue(false);
                }
            }
            if (!trianglePlacement1.GetValue())
            {
                // Can eliminate faces with tip pointing down where we are the tip
                // Face at 1 => Eliminate rotation 0
                // Face at 0 => Eliminate rotation 1
                // 5 => 2, 4 => 3, 3 => 4, 2 => 5
                for (int d = 0; d < 6; d++)
                {
                    if (incidentFaceMatrix[nodeIdx, (7 - d) % 6])
                        candidate[d].SetValue(false);
                }
            }
        }

        /// <summary>
        /// Helper checking whether we are the leader of the outer boundary.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot is on the
        /// outer boundary and has no predecessor.</returns>
        private bool IsOuterBoundaryLeader()
        {
            for (int i = 0; i < numOuterBoundaryOccurrences; i++)
            {
                if (outerBoundaryDirs[2 * i].GetValue() == Direction.NONE)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Helper for initializing and starting the binary operation
        /// subroutine(s). Already sets up the pin configuration and
        /// activates all active binary ops instances.
        /// </summary>
        /// <param name="onOuterBoundary">Whether the binary operation should
        /// be run on the outer boundary.</param>
        /// <param name="mode">The binary operation mode.</param>
        /// <param name="bitIndex1">The index of the first input bit. If <c>-1</c>, the
        /// result bit of the previous binary operation will be used.</param>
        /// <param name="bitIndex2">The index of the second input bit. If <c>-1</c>, the
        /// result bit of the previous binary operation will be used.</param>
        /// <param name="msbIndex">The index of the input MSB.</param>
        private void StartBinOp(bool onOuterBoundary, SubBinOps.Mode mode, int bitIndex1, int bitIndex2 = 0, int msbIndex = 0)
        {
            if (!onOuterBoundary)
            {
                // Setup only one subroutine instance
                Direction succ = ll.GetMaxDir();
                Direction pred = succ.Opposite();
                succ = HasNeighborAt(succ) ? succ : Direction.NONE;
                pred = HasNeighborAt(pred) ? pred : Direction.NONE;
                binop.Init(mode, bitIndex1 == -1 ? binop.ResultBit() : bits[bitIndex1].GetCurrentValue(), pred, succ,
                    bitIndex2 == -1 ? binop.ResultBit() : bits[bitIndex2].GetCurrentValue(), msbs[msbIndex].GetCurrentValue());
            }
            else
            {
                // Setup one instance per boundary occurrence
                for (int i = 0; i < numOuterBoundaryOccurrences; i++)
                {
                    Direction pred = outerBoundaryDirs[2 * i];
                    Direction succ = outerBoundaryDirs[2 * i + 1];
                    binops[i].Init(mode, bitIndex1 == -1 ? binops[i].ResultBit() : boundaryBits[4 * i + bitIndex1].GetCurrentValue(), pred, succ,
                        bitIndex2 == -1 ? binops[i].ResultBit() : boundaryBits[4 * i + bitIndex2].GetCurrentValue(), boundaryMSBs[4 * i + msbIndex].GetCurrentValue());
                }
            }
            ActivateBinopsSend(onOuterBoundary);
        }

        /// <summary>
        /// Helper for activating the binary operation subroutine(s).
        /// Sets up the pin configuration and activates all active
        /// binary ops instances.
        /// </summary>
        /// <param name="onOuterBoundary">Whether the binary operation should
        /// be run on the outer boundary.</param>
        private void ActivateBinopsSend(bool onOuterBoundary)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            binops[0].SetupPinConfig(pc);
            if (onOuterBoundary)
            {
                for (int i = 1; i < numOuterBoundaryOccurrences; i++)
                    binops[i].SetupPinConfig(pc);
            }
            SetPlannedPinConfiguration(pc);
            binops[0].ActivateSend();
            if (onOuterBoundary)
            {
                for (int i = 1; i < numOuterBoundaryOccurrences; i++)
                    binops[i].ActivateSend();
            }
        }

        /// <summary>
        /// Helper for activating the send part of the shape construction
        /// subroutine. Sets up the pin configuration and sends the beep,
        /// using the scale bit and MSB if required. Also moves the marker
        /// ahead by one position if a scale bit is needed.
        /// </summary>
        /// <param name="checkNeedScaleBit">Whether the scale bit
        /// should be checked.</param>
        private void ActivateShapeConstrSend(bool checkNeedScaleBit = true)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            shapeConstruction.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);

            if (checkNeedScaleBit && shapeConstruction.NeedScaleBit())
            {
                shapeConstruction.ActivateSend(lineMarker.GetCurrentValue() && bits[K], lineMarker.GetCurrentValue() && msbs[K]);
                if (ll.IsOnMaxLine())
                {
                    Direction d = ll.GetMaxDir().Opposite();
                    lineMarker.SetValue(HasNeighborAt(d) && ((SCGeneralParticle)GetNeighborAt(d)).lineMarker.GetValue());
                }
            }
            else
            {
                shapeConstruction.ActivateSend();
            }
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
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 0).Id, pc.GetPinAt(d.Opposite(), PinsPerEdge - 1).Id }, i);
                    pc.SetPartitionSetPosition(i, new Vector2((d.ToInt() - 1.5f) * 60, 0.4f));
                }
            }
        }

        /// <summary>
        /// Helper that sets up axis circuits, splits them for candidates
        /// in the given direction and lets these candidates send beeps.
        /// Sets the planned pin configuration to send the beeps.
        /// </summary>
        /// <param name="pc">The pin configuration to modify and plan.</param>
        /// <param name="direction">The direction in which to send the
        /// beep for rotation 0.</param>
        /// <param name="sendBeep">Whether to plan the given pin configuration
        /// and send beeps.</param>
        private void SetupCandidateAxisCircuits(PinConfiguration pc, int direction, bool sendBeep = true)
        {
            bool[] split = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                if (candidate[i].GetCurrentValue())
                    split[(i + direction) % 6] = true;
            }
            SetupAxisCircuits(pc, split);
            if (sendBeep)
            {
                SetPlannedPinConfiguration(pc);
                for (int i = 0; i < 6; i++)
                {
                    if (candidate[i].GetCurrentValue())
                        pc.GetPinAt(DirectionHelpers.Cardinal((i + direction) % 6), 0).PartitionSet.SendBeep();
                }
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SCGeneralInitializer : InitializationMethod
    {
        private int nPlaced;

        public SCGeneralInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(string shape = "shape_test_general.json", bool fromFile = true, bool drawTraversal = true, int numAmoebots = 250, bool fillShape = true, int scale = 1, int rotation = 0,
            float holeProb = 0.3f, bool fillHoles = false)
        {
            nPlaced = 0;

            // Read the shape
            Shape s;
            ShapeContainer sc;
            if (fromFile)
            {
                sc = ShapeContainer.ReadFromJson(FilePaths.path_shapes + shape);
            }
            else
            {
                sc = JsonUtility.FromJson<ShapeContainer>(shape);
            }

            if (sc is null || sc.shape is null)
            {
                Log.Error("Failed to read shape");
                return;
            }
            s = sc.shape;
            if (!s.IsConsistent())
            {
                Log.Warning("Shape is inconsistent!");
            }
            else
            {
                s.GenerateTraversal();
                SCGeneralParticle.shape = s;
            }

            SCGeneralParticle.longestLineStr = IntToBinary(s.GetLongestLineLength());
            SCGeneralParticle.shapeHasFaces = s.faces.Count > 0;
            SCGeneralParticle.shapeHasHoles = s.HasLoop();
            SCGeneralParticle.incidentFaceMatrix = s.GetIncidentFaceMatrix();
            // Generate traversal nodes and edge directions
            // We need a start point with incident edges at an angle if the shape does not have any faces
            s.GeneratePostmanTraversal(s.faces.Count == 0, out List<int> traversalNodes, out List<Direction> traversalDirections, out Direction distanceCheckDir);
            SCGeneralParticle.traversalNodes = traversalNodes.ToArray();
            SCGeneralParticle.traversalDirs = traversalDirections.ToArray();
            SCGeneralParticle.distanceCheckDir = distanceCheckDir;

            // Place amoebot system
            foreach (Vector2Int v in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles))
            {
                AddParticle(v);
                nPlaced++;
            }

            // Fill up positions of the shape
            if (scale < 1)
            {
                Log.Warning("Scale must be >= 1");
                scale = 1;
            }
            rotation = rotation % 6;
            if (rotation < 0)
            {
                rotation += 6;
            }
            if (fillShape)
            {
                if (scale == 1)
                {
                    foreach (Shape.Node node in s.nodes)
                    {
                        TryPlaceParticle(AmoebotFunctions.RotateVector(node, rotation));
                    }
                }
                else
                {
                    // Fill edges
                    foreach (Shape.Edge edge in s.edges)
                    {
                        Vector2Int n1 = s.nodes[edge.u];
                        Vector2Int n2 = s.nodes[edge.v];
                        n1 = AmoebotFunctions.RotateVector(n1, rotation);
                        n2 = AmoebotFunctions.RotateVector(n2, rotation);
                        Vector2Int to = n2 - n1;
                        n1 *= scale;
                        for (int i = 0; i < scale + 1; i++)
                        {
                            TryPlaceParticle(n1 + i * to);
                        }
                    }

                    // Fill faces
                    foreach (Shape.Face face in s.faces)
                    {
                        Vector2Int n1 = s.nodes[face.u];
                        Vector2Int n2 = s.nodes[face.v];
                        Vector2Int n3 = s.nodes[face.w];
                        n1 = AmoebotFunctions.RotateVector(n1, rotation);
                        n2 = AmoebotFunctions.RotateVector(n2, rotation);
                        n3 = AmoebotFunctions.RotateVector(n3, rotation);
                        Vector2Int to1 = n2 - n1;
                        Vector2Int to2 = n3 - n1;
                        n1 *= scale;
                        for (int i = 1; i < scale - 1; i++)
                        {
                            Vector2Int start = n1 + i * to1;
                            for (int j = 1; j < scale - i; j++)
                            {
                                TryPlaceParticle(start + j * to2);
                            }
                        }
                    }
                }
            }

            Log.Debug("Generated system has " + nPlaced + " amoebots");

            // Draw shape preview and traversal
            LineDrawer.Instance.Clear();
            s.Draw(Vector2Int.zero, rotation, scale);
            if (drawTraversal)
            {
                int n = traversalNodes.Count - 1;
                for (int i = 0; i < n; i++)
                {
                    Vector2Int start = s.nodes[traversalNodes[i]];
                    Vector2Int end = s.nodes[traversalNodes[i + 1]];
                    start = AmoebotFunctions.RotateVector(start, rotation);
                    end = AmoebotFunctions.RotateVector(end, rotation);
                    start *= scale;
                    end *= scale;
                    float frac = (float)i / Mathf.Max(1, n - 1);
                    Color color = new Color(frac, frac, frac);
                    float width = 3f - 2.5f * frac;
                    LineDrawer.Instance.AddLine(start, end, color, true, width, width, -0.1f * frac);
                }
            }
            LineDrawer.Instance.SetTimer(20);
        }

        private void TryPlaceParticle(Vector2Int pos)
        {
            if (!TryGetParticleAt(pos, out _))
            {
                InitializationParticle p = AddParticle(pos);
                nPlaced++;
            }
        }

        private string IntToBinary(int num)
        {
            string s = "";

            while (num > 0)
            {
                s += (num & 1) > 0 ? '1' : '0';
                num >>= 1;
            }

            return s;
        }
    }

} // namespace AS2.Algos.SCGeneral
