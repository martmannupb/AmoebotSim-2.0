using System.Collections.Generic;
using AS2.Sim;
using AS2.UI;
using UnityEngine;
using static AS2.Constants;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.BoundaryTest;
using AS2.Subroutines.LongestLines;
using AS2.Subroutines.ConvexShapeContainment;
using AS2.Subroutines.PASC;

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
    //          - (a) Run containment check for triangle at scale R (two rotations!)
    //              - Success: Set K := R and go to step 5
    //          - (b) Skip
    //      4.2. Check left bound L = 1
    //          - (a) Run containment check for triangle at scale L (two rotations!)
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
    //              - (a) Run containment check for triangle at scale M (two rotations!) (keep track of which rotations matched)
    //                  - If successful: If one rotation matched and the other did not, only try the matching rotation next time
    //                      - Set L := M
    //                  - If not successful: Set R := M
    //              - (b) Compute M^2 and compare the result to n
    //                  - If M^2 = n: Set N := M and break out of the binary search (go to step 5)
    //                  - Else if M^2 < n: Set L := M
    //                  - Else: Set R := M
    //  (In case (b), we will have N = Floor(sqrt(n)) on the outer boundary now, stored in M)
    //  5. Linear search
    //      5.1. Triangle containment check: If the target shape has at least one face
    //          - Run the triangle containment check for scale K and two rotations
    //          - Store the valid placements
    //      5.2. If K <= sqrt(n) flag is not set:
    //          - Compare K to N
    //          - If K <= N: Set the flag
    //      5.3. Initialize candidate set C
    //          - If K <= sqrt(n) flag is set: Set C := A
    //          - Else: Run distance check in all 6 directions with distance K and initialize the correct candidate sets
    //          - In both cases: Limit candidates by using the faces if possible
    //      5.4. If K <= sqrt(n):
    //          - On the longest lines in the system, place a marker spawn at distance K to the marker start
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
    //      - Beep on first: Setup next phase
    //          - If the target shape does not have a triangle:
    //              - Setup and start boundary test subroutine
    //              - Go to round 6
    //          - Else:
    //              - Go to binary search (round 12)
    //      - Beep on second:
    //          - (Binary search finished)
    //          - (a) Set K := M
    //          - (b) M now holds the value N = Floor(sqrt(n))
    //          - Go to linear search (round ??????????????????)
    //      - Beep on third:
    //          - (Binary search continues, only case (a))
    //          - (a) Go to round 26


    // TODO
    //      - Beep on fourth:
    //          - (Linear search decrement and comparison are finished)
    //          - Go to round 30


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

    // Rounds 12-15 run the entire triangle containment check

    // Round 12 (Start of triangle containment check):
    //  - Init triangle containment check subroutine for rotation 0
    //      - Scale R, 1 or M
    //  - Start running
    //  - Go to round 13

    // Round 13:
    //  - Run triangle containment check routine
    //  - If finished:
    //      - Success:
    //          - If we checked R: Set K := R and go to step 5 (round ?????????????????????????)
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


    // 5.1. Triangle containment check (and loop start)

    // Round 39:
    //  - If the target shape has faces:
    //      - Reset valid face flags
    //      - Start triangle containment check (go to round 40)
    //  - Else:
    //      - Go to step 5.2. (round 44)

    // Rounds 40-43 (similar to 12-15):
    //  - Run triangle check for scale K
    //  - Store valid placements for both rotations
    //      - Also: Store success for first check
    //      - If both rotations were unsuccessful, skip this entire scale and go to step 5.7. (round ????????????????????)

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

    // Rounds 46-50 (PASC, similar to 34-38):
    //  - Every amoebot becomes a candidate for every rotation
    //  - Remove invalid faces for the first node of the traversal
    //  - If K <= sqrt(n):
    //      - Go to step 5.4. (round ????????????????????)
    //  - Else:
    //      - Setup PASC on all 6 axes
    //      - When finished:
    //          - PASC finished and no MSB: No amoebot has enough space, skip this scale
    //              - Go to step 5.7 (round ???????????????????)
    //          - Eliminate candidates based on the result
    //          - Setup global circuit and let candidates beep
    //          - Go to round 51

    // Round 51:
    //  - Listen for beep on global circuit
    //  - If no beep: Skip this scale
    //      - Go to step 5.7. (round ????????????????)
    //  - Else:
    //      - TODO





    public class SCGeneralParticle : ParticleAlgorithm
    {
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


        SubBinOps[] binops = new SubBinOps[3];
        SubBinOps binop;
        SubBoundaryTest boundaryTest;
        SubLongestLines ll;
        SubPASC[] pasc = new SubPASC[6];
        SubMergingAlgo triangleCheck;

        public static Shape shape;                      // The target shape
        public static string longestLineStr;            // String representation of the length of the longest line in the target shape
        public static bool shapeHasFaces;               // Whether the target shape has a face
        public static bool shapeHasHoles;               // Whether the target shape has a hole
        public static Direction distanceCheckDir;       // The direction used for the distance check before the traversal (at rotation 0)

        public SCGeneralParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            finished = CreateAttributeBool("Finished", false);
            numOuterBoundaryOccurrences = CreateAttributeInt("Outer boundary occ.", 0);
            for (int i = 0; i < 6; i++)
                outerBoundaryDirs[i] = CreateAttributeDirection("Boundary " + (i % 2 == 0 ? "Pred. " : "Succ. ") + i / 2, Direction.NONE);
            markerIdx = CreateAttributeInt("Marker idx", 0);
            lineMarker = CreateAttributeBool("LineMarker", false);
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

            for (int i = 0; i < binops.Length; i++)
                binops[i] = new SubBinOps(p);
            binop = binops[0];
            ll = new SubLongestLines(p);
            for (int i = 0; i < 6; i++)
                pasc[i] = new SubPASC(p);
            boundaryTest = new SubBoundaryTest(p, pasc);
            triangleCheck = new SubMergingAlgo(p);

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
                        else if (beep0)
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
                            // - Go to linear search (round ??????????????????)
                            // TODO
                        }
                        else if (beep2)
                        {
                            // Binary search continues, only case (a)
                            // Test the triangle
                            round.SetValue(26);
                        }
                        //else if (beep3)
                        //{
                        //    // Linear search decrement and comparison are finished
                        //    round.SetValue(30);
                        //}
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

                // Rounds 12-15 run the entire triangle containment check

                case 12:    // 12 and 14: Scale R check
                case 14:
                case 16:    // 16 and 18: Scale L check
                case 18:
                case 26:    // 26 and 28: Scale M check
                case 28:
                    {
                        // Init triangle containment check for rotation 0/1
                        int rot = r == 12 || r == 16 || r == 26 ? 0 : 1;
                        if (ll.IsOnMaxLine())
                        {
                            int scaleIdx = r < 16 ? R : (r < 26 ? L : M);
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
                    {
                        triangleCheck.ActivateReceive();
                        if (triangleCheck.IsFinished())
                        {
                            if (triangleCheck.Success())
                            {
                                if (r <= 15)
                                {
                                    // Checked R successfully: Set K := R and go to linear search (round ??????????????????????????)
                                    if (ll.IsOnMaxLine())
                                    {
                                        bits[K].SetValue(bits[R]);
                                        msbs[K].SetValue(msbs[R]);
                                    }
                                    round.SetValue(42);
                                }
                                else if (r <= 19)
                                {
                                    // Checked L successfully: Move on to rest of the binary search
                                    round.SetValue(20);
                                }
                                else
                                {
                                    // Checked M successfully: Set L := M and go back to binary search start
                                    bits[L].SetValue(bits[M]);
                                    msbs[L].SetValue(msbs[M]);
                                    round.SetValue(20);
                                }
                            }
                            else
                            {
                                if (r <= 17 || r == 27)
                                {
                                    // Check for rotation 0 failed, go to next one
                                    // OR finished check for R with failure, just move on to check for L
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
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SCGeneralInitializer : InitializationMethod
    {
        private int nPlaced;

        public SCGeneralInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(string shape = "shape_test_general.json", bool fromFile = true, int numAmoebots = 250, bool fillShape = true, int scale = 1, int rotation = 0,
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
            // TODO
            SCGeneralParticle.shapeHasHoles = false;

            // Place amoebot system
            foreach (Vector2Int v in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles))
            {
                AddParticle(v);
                nPlaced++;
            }

            // Fill up positions of the shape
            if (fillShape)
            {
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

            // Draw shape preview
            LineDrawer.Instance.Clear();
            s.Draw(Vector2Int.zero, rotation, scale);
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
