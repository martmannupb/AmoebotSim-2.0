using System.Collections.Generic;
using AS2.Sim;
using AS2.UI;
using UnityEngine;
using static AS2.Constants;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.BoundaryTest;
using AS2.Subroutines.LongestLines;
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
    //  (In case (b), we will have N = Floor(sqrt(n)) on the outer boundary now)
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
    //              - Go to binary search (round ????????????????????)


    //      - Beep on second:
    //          - (Binary search finished)
    //          - (a) Go to final shape construction (round 36)
    //          - (b) Go to linear search (round 30)
    //      - Beep on third:
    //          - (Binary search continues)
    //          - (a) Go to round 24
    //          - (b) Go to round 26
    //      - Beep on fourth:
    //          - (Linear search decrement and comparison are finished)
    //          - Go to round 30

    
    // 3. Setup the outer boundary

    // Round 6:
    //  - Run boundary test subroutine
    //  - If finished:
    //      - If the shape has a hole and there are no inner boundaries:
    //          - Set K <= sqrt(n) flag and go to step 4 (round ?????????????????????????????)
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
    //          - Go to the binary search phase (round ?????????????????????????)
    //      - Current marker writes received PASC bit (second circuit)
    //      - If the bit was 1: Marker becomes new MSB, old MSB removes itself
    //  - Marker moves forward
    //  - Send PASC beeps

    public class SCGeneralParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "SC General Solution";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SCGeneralInitializer).FullName;

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
        ParticleAttribute<bool>[] boundaryBits = new ParticleAttribute<bool>[9];    // Array storing 3 bits for each outer boundary occurrence
        ParticleAttribute<bool>[] boundaryMSBs = new ParticleAttribute<bool>[9];    // Array storing 3 MSBs for each outer boundary occurrence
        ParticleAttribute<bool> belowSqrt;                      // Whether our scale is at most sqrt(n)
        ParticleAttribute<Direction> parentDir;                 // Our parent direction in the spanning tree
        ParticleAttribute<int> numPascETT;                      // How many PASC instances we have to run during the ETT


        SubBinOps binops;
        SubBoundaryTest boundaryTest;
        SubLongestLines ll;
        SubPASC[] pasc = new SubPASC[6];

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

            binops = new SubBinOps(p);
            boundaryTest = new SubBoundaryTest(p);
            ll = new SubLongestLines(p);
            for (int i = 0; i < 6; i++)
                pasc[i] = new SubPASC(p);

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
                                binops.Init(SubBinOps.Mode.COMP, bits[M].GetValue(), pred, succ, bits[R].GetValue());
                            }
                            else if (ctr == 1)
                            {
                                // Compute R / M
                                binops.Init(SubBinOps.Mode.DIV, bits[R].GetValue(), pred, succ, bits[M].GetValue(), msbs[R].GetValue());
                            }
                            else if (ctr == 2)
                            {
                                // Find MSB of R
                                binops.Init(SubBinOps.Mode.MSB, bits[R].GetValue(), pred, succ);
                            }
                            PinConfiguration pc = GetContractedPinConfiguration();
                            binops.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binops.ActivateSend();
                            round.SetValue(r + 1);
                        }
                    }
                    break;
                case 4:
                    {
                        binops.ActivateReceive();
                        if (binops.IsFinished())
                        {
                            // Store result based on counter
                            int ctr = counter.GetValue();
                            if (ctr == 0)
                            {
                                // If s > R: Terminate
                                if (binops.CompResult() == SubComparison.ComparisonResult.GREATER)
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
                                bits[R].SetValue(binops.ResultBit());
                            }
                            else if (ctr == 2)
                            {
                                // Store MSB of R
                                msbs[R].SetValue(binops.IsMSB());
                            }
                            counter.SetValue(ctr + 1);
                            round.SetValue(r - 1);
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            binops.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binops.ActivateSend();
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
                                // Go directly to binary search
                                // TODO
                            }
                        }
                        //else if (beep1)
                        //{
                        //    // Binary search finished
                        //    if (shapeIsStarConvex)
                        //        // Go to final shape construction
                        //        round.SetValue(36);
                        //    else
                        //        // Go to linear search
                        //        round.SetValue(30);
                        //}
                        //else if (beep2)
                        //{
                        //    // Binary search continues
                        //    if (shapeIsStarConvex)
                        //        // Test the shape directly
                        //        round.SetValue(24);
                        //    else
                        //        // Test the triangle
                        //        round.SetValue(26);
                        //}
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
                                // TODO
                                round.SetValue(42);
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
                                    boundaryMSBs[3 * i + R].SetValue(true);
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
                                        boundaryBits[3 * i + L].SetValue(true);
                                        boundaryMSBs[3 * i + L].SetValue(true);
                                        break;
                                    }
                                }
                            }

                            // Go to binary search phase
                            // TODO
                            round.SetValue(42);
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
                                boundaryBits[3 * (markerIdx - 1) + R].SetValue(true);
                                boundaryMSBs[3 * (markerIdx - 1) + R].SetValue(true);
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
        /// Sets up a global circuit using the given pin offset.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="offset">The pin offset.</param>
        /// <param name="pSet">The partition set ID.</param>
        private void SetupGlobalCircuit(PinConfiguration pc, int offset, int pSet)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(offset, inverted, pSet);
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
