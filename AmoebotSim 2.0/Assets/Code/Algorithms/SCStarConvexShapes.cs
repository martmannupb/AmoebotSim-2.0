using UnityEngine;
using static AS2.Constants;
using AS2.Sim;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.LeaderElectionSC;
using AS2.Subroutines.LongestLines;
using AS2.Subroutines.PASC;
using AS2.Subroutines.ConvexShapeContainment;
using AS2.Subroutines.ShapeConstruction;

namespace AS2.Algos.SCStarConvexShapes
{

    /// <summary>
    /// Shape containment solution for star convex shapes.
    /// <para>
    /// <b>Disclaimer: The save/load feature does not work for
    /// this algorithm because it stores the target shape in a
    /// static member. Always generate this algorithm from
    /// Init Mode.</b>
    /// </para>
    /// <para>
    /// If the amoebot system is so small that the longest lines cannot
    /// store the scaled shape parameters, the algorithm will not work
    /// correctly. A rough estimate of sufficient size is that the longest
    /// line should have length at least log(its own length) +
    /// log(diameter of the shape).
    /// </para>
    /// <para>
    /// This algorithm uses a binary search combined with a parallelogram
    /// or merging subroutine as a containment check. A slight modification
    /// allows the algorithm to divide its maximum line length by the longest
    /// line in the target shape first to get a smaller upper bound on the
    /// scale factor, reducing the number of containment checks in practice.
    /// </para>
    /// <para>
    /// The implementation is almost the same as that of the convex shapes
    /// algorithm (see <see cref="SCConvexShapes.SCConvexShapesParticle"/>).
    /// </para>
    /// </summary>

    // Algorithm plan:
    //  - Run longest lines subroutine, getting longest length k
    //  - Initialize L := 1 and R := k / m, where m is the length of a longest line in the target shape
    //      - Terminate with failure if k < m
    //  - Move a marker along each counter and let each amoebot store its position for the shape parameter indices
    //      - Instead of storing the numbers themselves, just store the index to avoid writing the numbers every time the constituent shape changes
    //  - Run the containment check for R
    //      - If successful: Construct the final shape and finish
    //  - Run the containment check for L = 1
    //      - If not successful: Terminate with failure
    //  - Binary search:
    //      - Compute M := (L + R) / 2
    //      - If M = L:
    //          - Construct the final shape and finish
    //      - Containment check:
    //          - For i = 0,...,numShapes - 1:
    //              - Compute the shape parameters scaled by M
    //              - For each rotation r = 0,...,5 for which candidates still exist:
    //                  - Run the containment check for r, M and intersect the solution set with the current one
    //              - Establish global circuits to transmit existence of valid placements for each rotation
    //              - If no rotations are valid anymore:
    //                  - Cancel the check for this scale with failure (R := M)
    //          - If there are still valid placements for some rotation:
    //              - End the check for this scale with success (L := M)

    // Round overview:

    // FIND LONGEST LINES

    // Round 0:
    //  - Setup longest lines subroutine
    //  - Start sending beeps

    // Round 1:
    //  - Activate longest lines subroutine
    //  - If subroutine is finished:
    //      - Setup left and right side of the binary search
    //          - Left := 1, Right := length of longest line
    //      - Set rotation/counter to 0
    //      - Place marker at counter start(s)
    //      - Go to round 2
    //  - Else:
    //      - Continue sending

    // SETUP SHAPE PARAMETERS AND COMPARE LINE LENGTH

    // Round 2:
    //  - If counter >= length of longest binary parameter:
    //      - Set counter to 0
    //      - Reset marker
    //      - SPLIT:
    //          - Counters go to round 3
    //          - Others go to round 5 and setup 4 global circuits
    //  - Else:
    //      - Marker writes current counter value to shape parameter index
    //      - Marker moves one position ahead
    //      - Increment counter

    // Round 3:
    //  - If counter > 3:
    //      - Setup 4 global circuits and beep on first
    //      - Go to round 5
    //  - Else:
    //      - Init binop to compute:
    //          - Comparison between longest line length and R for counter 0
    //          - R / longest line length for counter 1
    //          - MSB of previous result (R) for counter 2
    //      - Start binop
    //      - Go to round 4

    // Round 4:
    //  - Continue binop
    //  - If binop is finished:
    //      - Store result based on counter
    //      - Increment counter
    //      - Go to round 3

    // Round 5 (WAIT):
    //  - Wait for beep on global circuits
    //      - First circuit beep: Setup next phase
    //          - Set shape index to 0
    //          - Set all valid placement flags to true
    //          - Then go to round 6
    //      - Second circuit beep:
    //          - Set all valid placement flags to true
    //          - Set rotation to 0
    //          - Go to round 9
    //  - If beep on fourth global circuit: Terminate with failure


    // CHECK LARGEST SCALE

    // Round 6:
    //  - If shape idx >= num shapes:
    //      - We found a valid placement: Go to final shape construction
    //      - Also: Store rotation and valid placements for later (in any case)
    //      - Go to round 33
    //  - Else:
    //      - Set counter to 0
    //      - SPLIT:
    //          - Counters go to round 7
    //          - Others go to round 5 and setup 4 global circuits

    // Round 7 (similar to 3):
    //  - If counter >= 2 * (number of shape params):
    //      - Setup 4 global circuits and beep on second
    //      - Go to round 5
    //  - Else:
    //      - Init binop to compute current scaled shape parameter, using scale M
    //      - Start binop
    //      - Go to round 8

    // Round 8 (similar to 4):
    //  - Continue binop
    //  - If binop is finished:
    //      - Store result based on counter
    //      - Increment counter
    //      - Go to round 7

    // Round 9:
    //  - If rotation > 5:
    //      - Setup 4 global circuits
    //      - Valid placements of rotations 0-3 beep
    //      - Go to round 11
    //  - Else:
    //      - Init containment check for current rotation and scaled parameters
    //      - Start containment check
    //      - Go to round 10

    // Round 10:
    //  - If containment check is finished:
    //      - Store valid placements in their rotation
    //      - Increment rotation
    //          - Find the next rotation for which we still have valid placements, else set it to 6
    //      - Go to round 9
    //  - Else:
    //      - Continue running

    // Round 11:
    //  - Receive beeps on global circuits 0-3
    //      - Update valid placement existence flags
    //  - Setup global circuits again, let valid placements for rotations 4, 5 beep
    //  - Go to round 12

    // Round 12:
    //  - Receive beeps on global circuits 0-1
    //      - Update valid placement existence flags
    //  - (We now know the result of the containment check for this shape)
    //  - Success:
    //      - Increment shape counter
    //      - Go to round 6
    //  - Failure (no valid placements left):
    //      - Just go to next phase (largest scale check failed)
    //      - Reset all the valid placement flags
    //      - Reset shape index to 0
    //      - Assign M := L
    //      - Go to round 13


    // CHECK SCALE 1

    // Rounds 13-19 are almost the same as 6-12
    // These are the differences:

    // Round 13 (6):
    //  - When finished, we just continue with the next phase instead of going to the shape construction
    //      - ???????????

    // Round 14 (7):
    //  - When finished, beep on third circuit instead of second

    // Round 19 (12):
    //  - Failure means that we terminate with a negative result immediately


    // BINARY SEARCH

    // Round 20:
    //  - Initialize and start binop for computing M := L + R
    //  - SPLIT:
    //      - Counter amoebots go to round 21
    //      - Other amoebots setup 4 global circuits and go to round 25

    // Round 21:
    //  - If binop is finished:
    //      - Store result in M
    //      - Go to round 22
    //  - Else:
    //      - Continue running

    // Round 22:
    //  - Shift each bit of M one position backwards
    //  - Setup binop for finding MSB of M

    // Round 23:
    //  - If binop is finished:
    //      - Store MSB of M
    //      - Setup and start binop for comparing L to M
    //  - Else:
    //      - Continue running

    // Round 24:
    //  - If binop is finished:
    //      - If L = M:
    //          - Setup 4 global circuits and beep on first one
    //          - Go to round 25
    //      - Else:
    //          - Set counter to 0
    //          - Setup 4 global circuits and beep on second one
    //          - Go to round 25
    //  - Else:
    //      - Continue running

    // Round 25 (WAIT):
    //  - Listen on 4 global circuits
    //  - If beep on first circuit:
    //      - Found solution, go to shape construction
    //      - Go to round ?????????
    //  - If beep on second circuit:
    //      - Start containment check procedure for next scale factor
    //      - Go to round 26
    //  - If beep on third circuit:
    //      - Start containment check procedure for next shape
    //      - Go to round 29

    // CONTAINMENT CHECK

    // Rounds 26-32 are almost the same as 6-12
    // These are the differences:

    // Round 26 (6):
    //  - We split by going to round 25 instead of 5
    //  - When finished, we assign L := M and go back to round 20

    // Round 27 (7):
    //  - When finished, beep on third circuit instead of second

    // Round 32 (12):
    //  - Failure means we assign R := M and go back to round 20


    // SHAPE CONSTRUCTION

    // Round 33:
    //  - Setup leader election on valid placements
    //      - But use entire system

    // Round 34:
    //  - If leader election is finished:
    //      - Setup shape construction subroutine for leader and scale L
    //      - Markers at counter starts

    // Round 35:
    //  - Continue running shape construction
    //  - If shape construction is finished:
    //      - Terminate with success
    //  - If scale reset:
    //      - Set marker to counter start
    //      - Go to round 36
    //  - Else:
    //      - Continue running (maybe with scale bit)
    //      - Forward marker

    // Round 36:
    //  - Send next bit of shape construction
    //  - Forward marker
    //  - Go back to round 35


    public class SCStarConvexShapesParticle : ParticleAlgorithm
    {

        /// <summary>
        /// Container for information required to run the containment check
        /// of a constituent shape. The shape parameters are given in the
        /// order a, d, c, a', a+1
        /// </summary>
        public class ShapeInfo
        {
            public ShapeType shapeType;
            public Direction directionW;
            public Direction directionH;
            public string[] shapeParams = new string[5];

            public ShapeInfo(ShapeType shapeType, Direction directionW, Direction directionH, string a, string d, string c, string a2, string a3)
            {
                this.shapeType = shapeType;
                this.directionW = directionW;
                this.directionH = directionH;
                shapeParams[0] = a;
                shapeParams[1] = d;
                shapeParams[2] = c;
                shapeParams[3] = a2;
                shapeParams[4] = a3;
            }
        }

        [StatusInfo("Draw Shape", "Draws the target shape at the selected amoebot.")]
        public static void DrawShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            AS2.UI.LineDrawer.Instance.Clear();
            if (shape is null)
                return;

            Vector2Int pos = Vector2Int.zero;
            if (selectedParticle != null)
                pos = selectedParticle.Head();

            shape.Draw(pos);

            AS2.UI.LineDrawer.Instance.SetTimer(20);
        }

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "SC Star Convex Shapes";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SCStarConvexShapesInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<int> round;

        // Rotation / generic counter
        ParticleAttribute<int> rotation;
        ParticleAttribute<int> finalRotation;
        ParticleAttribute<int> shapeIdx;        // The index of the constituent shape we are testing
        ParticleAttribute<int> paramIdx;        // The index of the shape parameter bit for our counter position
        ParticleAttribute<bool> marker;
        ParticleAttribute<bool> validPlacement;
        ParticleAttribute<bool> finished;
        
        // Stores 24 bits
        // 3 bits for binary counter L, R, M
        // 3 MSBs for binary counter
        // 6 scaled shape parameter bits
        // 6 scaled shape parameter MSBs
        // 6 valid placement flags
        // 6 valid placement existence flags
        //       29  28  27  26  25  24   23  22  21  20  19  18    17  16  15  14  13  12    11  10  9      8   7   6   5   4   3    2   1   0
        // xxx   x   x   x   x   x   x    x   x   x   x   x   x     x   x   x   x   x   x     x   x   x      x   x   x   x   x   x    x   x   x
        //       Placement exists         Valid placement           Scaled shape MSBs         Counter MSBs   Scaled shape bits        Counter bits
        ParticleAttribute<int> bitStorage;

        // Bit index constants
        private const int bit_counter = 0;
        private const int bit_shape = 3;
        private const int msb_counter = 9;
        private const int msb_shape = 12;
        private const int bit_valid = 18;
        private const int bit_placement_exists = 24;

        SubBinOps binops;
        SubLeaderElectionSC leaderElection;
        SubLongestLines ll;
        SubPASC2 pasc1;
        SubParallelogram parallelogram;
        SubMergingAlgo mergeAlgo;
        SubConvexShapeContainment containment;
        SubShapeConstruction shapeConstr;

        // Static data set by the generation method
        public static Shape shape;                  // The whole target shape, used for the final construction
        public static ShapeInfo[] constituents;     // The constituent shapes
        public static string longestLine;           // The length of the target shape's longest line in binary
        public static int longestParameter;         // The number of bits in the longest shape parameter

        public SCStarConvexShapesParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            rotation = CreateAttributeInt("Rotation", 0);
            finalRotation = CreateAttributeInt("Final Rotation", -1);
            shapeIdx = CreateAttributeInt("Shape Index", 0);
            paramIdx = CreateAttributeInt("Param Index", -1);
            marker = CreateAttributeBool("Marker", false);
            bitStorage = CreateAttributeInt("Bits", 0);

            validPlacement = CreateAttributeBool("Valid placement", false);
            finished = CreateAttributeBool("Finished", false);

            binops = new SubBinOps(p);
            pasc1 = new SubPASC2(p);
            ll = new SubLongestLines(p, pasc1);
            parallelogram = new SubParallelogram(p, pasc1);
            mergeAlgo = new SubMergingAlgo(p, pasc1);
            containment = new SubConvexShapeContainment(p, parallelogram, mergeAlgo);
            leaderElection = new SubLeaderElectionSC(p);
            shapeConstr = new SubShapeConstruction(p, shape, pasc1);

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
            switch (round)
            {
                // FIND LONGEST LINES

                case 0:
                    {
                        // Start longest lines subroutine
                        ll.Init();
                        PinConfiguration pc = GetContractedPinConfiguration();
                        ll.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        ll.ActivateSend();
                        round.SetValue(round + 1);
                    }
                    break;
                case 1:
                    {
                        ll.ActivateReceive();
                        if (ll.IsFinished())
                        {
                            // Set Left and Right bits according to longest line
                            // Also set marker to counter start(s)
                            if (ll.IsOnMaxLine())
                            {
                                if (ll.IsMSB())
                                    MSB_R = true;
                                if (ll.GetBit())
                                    Bit_R = true;
                                if (!HasNeighborAt(ll.GetMaxDir().Opposite()))
                                {
                                    MSB_L = true;
                                    Bit_L = true;
                                    marker.SetValue(true);
                                }
                            }
                            // Set counter/rotation to 0 and go to next phase
                            rotation.SetValue(0);
                            round.SetValue(round + 1);
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

                // SETUP SHAPE PARAMETERS

                case 2:
                    {
                        if (rotation >= longestParameter)
                        {
                            // Done setting up shape parameter indices
                            rotation.SetValue(0);
                            // Set all valid placement flags to true in preparation for the first containment check
                            for (int i = 0; i < 6; i++)
                            {
                                SetValidPlacement(i, true);
                                SetPlacementExists(i, true);
                            }
                            marker.SetValue(false);
                            // Split here
                            if (ll.IsOnMaxLine())
                                round.SetValue(3);
                            else
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc);
                                SetPlannedPinConfiguration(pc);
                                round.SetValue(5);
                            }
                        }
                        else
                        {
                            // Marker writes shape parameter index
                            if (marker)
                                paramIdx.SetValue(rotation);
                            // Marker moves one position ahead
                            if (ll.IsOnMaxLine())
                            {
                                Direction d = ll.GetMaxDir().Opposite();
                                marker.SetValue(HasNeighborAt(d) && ((SCStarConvexShapesParticle)GetNeighborAt(d)).marker);
                            }
                            // Increment counter
                            rotation.SetValue(rotation + 1);
                        }
                    }
                    break;
                case 3:
                case 7:     // From check for scale R
                case 14:    // From check for scale 1
                case 27:    // From check for scale M
                    {
                        // 3 binary operations to setup longest line comparison in setup phase
                        int n = (round == 3) ? 3 : (2 * NumShapeParams());
                        if (rotation >= n)
                        {
                            // Done with binary operations, send signal to continue
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupGlobalCircuits(pc);
                            SetPlannedPinConfiguration(pc);
                            pc.SendBeepOnPartitionSet(round == 3 ? 0 : (round == 7 ? 1 : 2));
                            rotation.SetValue(0);
                            round.SetValue(round <= 14 ? 5 : 25);
                        }
                        else
                        {
                            // Initialize and start binop for longest line comparison or scaling shape parameters
                            InitBinOpsShapeParam(round == 3);
                            round.SetValue(round + 1);
                        }
                    }
                    break;
                case 4:
                case 8:     // From check for scale R
                case 15:    // From check for scale 1
                case 28:    // From check for scale M
                    {
                        // Run binops and write result bits/MSBs
                        if (RunBinOpsShapeParam(out bool terminate))
                        {
                            // We may have to terminate during the first phase
                            if (terminate)
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc);
                                SetPlannedPinConfiguration(pc);
                                pc.SendBeepOnPartitionSet(3);
                                round.SetValue(5);
                            }
                            else
                            {
                                // Increment counter and repeat
                                rotation.SetValue(rotation + 1);
                                round.SetValue(round - 1);
                            }
                        }
                    }
                    break;
                case 5: // WAIT round
                    {
                        // Wait for beep on global circuit
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(3))
                        {
                            // Terminate with failure
                            finished.SetValue(true);
                            SetMainColor(ColorData.Particle_Red);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            round.SetValue(6);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(1))
                        {
                            // Initialize containment check for scale R
                            // Use the first rotation for which there are still valid placements
                            int r = 0;
                            for (; r < 6; r++)
                            {
                                if (PlacementExists(r))
                                    break;
                            }
                            rotation.SetValue(r);
                            round.SetValue(9);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(2))
                        {
                            // Initialize containment check for scale 1
                            // Use the first rotation for which there are still valid placements
                            int r = 0;
                            for (; r < 6; r++)
                            {
                                if (PlacementExists(r))
                                    break;
                            }
                            rotation.SetValue(r);
                            round.SetValue(16);
                        }
                    }
                    break;

                // CHECK LARGEST SCALE

                case 6:
                case 13:    // Start of containment check, same for checking scale 1
                case 26:    // Same for check during binary search
                    {
                        if (shapeIdx >= constituents.Length)
                        {
                            // We found a valid placement: Store the result
                            // Find the lowest valid rotation and remember the valid placements
                            int r = 0;
                            for (; r < 6; r++)
                            {
                                if (PlacementExists(r))
                                    break;
                            }
                            finalRotation.SetValue(r);
                            validPlacement.SetValue(IsValidPlacement(r));
                            if (round == 6)
                            {
                                // Found a valid placement for scale R!
                                // Go to final shape construction
                                Bit_L = Bit_R;
                                MSB_L = MSB_R;
                                round.SetValue(33);
                            }
                            else if (round == 13)
                            {
                                // Found a valid placement for scale 1
                                // Just continue with the next phase
                                round.SetValue(20);
                            }
                            else if (round == 26)
                            {
                                // Found a valid placement for scale M
                                Bit_L = Bit_M;
                                MSB_L = MSB_M;
                                round.SetValue(20);
                            }
                        }
                        else
                        {
                            // Start containment check for the current shape
                            rotation.SetValue(0);
                            // Split
                            if (ll.IsOnMaxLine())
                                round.SetValue(round + 1);
                            else
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc);
                                SetPlannedPinConfiguration(pc);
                                round.SetValue(round <= 13 ? 5 : 25);
                            }
                        }
                    }
                    break;
                case 9:
                case 16:    // From check for scale 1
                case 29:    // From check for scale M
                    {
                        if (rotation > 5)
                        {
                            // We have finished checking the rotations, now transmit which placements still exist
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupGlobalCircuits(pc);
                            SetPlannedPinConfiguration(pc);
                            for (int i = 0; i < 4; i++)
                            {
                                if (IsValidPlacement(i))
                                    pc.SendBeepOnPartitionSet(i);
                            }
                            round.SetValue(round + 2);
                        }
                        else
                        {
                            // Init containment check for current rotation and scaled parameters
                            StartShapeContainmentCheck();

                            round.SetValue(round + 1);
                        }
                    }
                    break;
                case 10:
                case 17:    // From check for scale 1
                case 30:    // From check for scale M
                    {
                        containment.ActivateReceive();
                        if (containment.IsFinished())
                        {
                            // Store the valid placements for the current rotation
                            bool valid = containment.IsRepresentative();
                            SetValidPlacement(rotation, valid && IsValidPlacement(rotation));
                            if (IsValidPlacement(rotation))
                                SetMainColor(ColorData.Particle_Green);
                            else
                                SetMainColor(ColorData.Particle_Black);

                            // Increment rotation
                            // Find the next rotation for which we still have valid placements
                            int r = rotation + 1;
                            for (; r < 6; r++)
                            {
                                if (PlacementExists(r))
                                    break;
                            }
                            rotation.SetValue(r);
                            round.SetValue(round - 1);
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            containment.SetupPC(pc);
                            SetPlannedPinConfiguration(pc);
                            containment.ActivateSend();
                        }
                    }
                    break;
                case 11:
                case 18:    // From check for scale 1
                case 31:    // From check for scale M
                    {
                        // Receive beeps on global circuits 0-3
                        // and update valid placement existence flags
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        for (int i = 0; i < 4; i++)
                        {
                            if (!pc.ReceivedBeepOnPartitionSet(i))
                                SetPlacementExists(i, false);
                        }
                        // Setup the same circuits and beep for rotations 4 and 5
                        SetPlannedPinConfiguration(pc);
                        if (IsValidPlacement(4))
                            pc.SendBeepOnPartitionSet(0);
                        if (IsValidPlacement(5))
                            pc.SendBeepOnPartitionSet(1);
                        round.SetValue(round + 1);
                    }
                    break;
                case 12:
                case 19:    // From check for scale 1
                case 32:    // From check for scale M
                    {
                        // Receive beeps on global circuits 0, 1
                        // and update valid placement existence flags
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                            SetPlacementExists(4, false);
                        if (!pc.ReceivedBeepOnPartitionSet(1))
                            SetPlacementExists(5, false);
                        // Check whether there are still any valid placements
                        bool success = false;
                        for (int r = 0; r < 6; r++)
                        {
                            if (PlacementExists(r))
                            {
                                success = true;
                                break;
                            }
                        }
                        if (success)
                        {
                            // Increment shape counter and continue with next check
                            shapeIdx.SetValue(shapeIdx + 1);
                            round.SetValue(round - 6);
                        }
                        else
                        {
                            // No valid placements left!
                            if (round == 12)
                            {
                                // Check for R failed: Just go to next phase
                                // Ret all valid placement flags to true
                                for (int i = 0; i < 6; i++)
                                {
                                    SetValidPlacement(i, true);
                                    SetPlacementExists(i, true);
                                }
                                shapeIdx.SetValue(0);
                                // Set M := L for next check
                                if (ll.IsOnMaxLine())
                                {
                                    Bit_M = Bit_L;
                                    MSB_M = MSB_L;
                                }
                                round.SetValue(round + 1);
                            }
                            else if (round == 19)
                            {
                                // Check for scale 1 failed: Terminate
                                SetMainColor(ColorData.Particle_Red);
                                finished.SetValue(true);
                            }
                            else if (round == 32)
                            {
                                // Check for scale M failed: Update R
                                Bit_R = Bit_M;
                                MSB_R = MSB_M;
                                round.SetValue(20);
                            }
                        }
                    }
                    break;

                // CHECK FOR SCALE 1 (rounds 13-19)

                // BINARY SEARCH

                case 20:
                    {
                        // Start adding L + R
                        InitBinOpsSearch();
                        // Split: Counters go to round 21, others to round 25 with 4 global circuits
                        if (ll.IsOnMaxLine())
                        {
                            round.SetValue(round + 1);
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupGlobalCircuits(pc);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(25);
                        }
                    }
                    break;
                case 21:
                case 23:
                case 24:
                    {
                        // All 3 rounds wait for binary operations to finish
                        // Just the reaction when it is finished differs
                        bool isFinished = RunBinOps();
                        if (isFinished)
                        {
                            if (round == 21)
                            {
                                // Store result in M
                                Bit_M = binops.ResultBit();
                                round.SetValue(round + 1);
                            }
                            else if (round == 23)
                            {
                                // Store MSB of M
                                MSB_M = binops.IsMSB();

                                // Setup binary operation for comparing L to M
                                InitBinOpsSearch(false, true);
                                round.SetValue(round + 1);
                            }
                            else if (round == 24)
                            {
                                // Go to waiting round and let amoebots know whether we are finished or
                                // have to start the next containment check
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc);
                                SetPlannedPinConfiguration(pc);
                                if (binops.CompResult() == SubComparison.ComparisonResult.EQUAL)
                                    pc.SendBeepOnPartitionSet(0);
                                else
                                    pc.SendBeepOnPartitionSet(1);
                                round.SetValue(round + 1);
                            }
                        }
                    }
                    break;
                case 22:
                    {
                        // Shift each bit of M one position backwards
                        Direction d = ll.GetMaxDir();
                        Bit_M = HasNeighborAt(d) && ((SCStarConvexShapesParticle)GetNeighborAt(d)).GetBitM();

                        // Setup binary operation for finding MSB of M
                        InitBinOpsSearch(true);
                        round.SetValue(round + 1);
                    }
                    break;
                case 25: // WAIT round
                    {
                        // Wait for beep on global circuits
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Finished: Go to shape construction
                            round.SetValue(33);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(1))
                        {
                            // Start next containment check
                            shapeIdx.SetValue(0);
                            rotation.SetValue(0);
                            for (int r = 0; r < 6; r++)
                            {
                                SetValidPlacement(r, true);
                                SetPlacementExists(r, true);
                            }
                            round.SetValue(26);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(2))
                        {
                            // Start next check for current rotation
                            round.SetValue(29);
                        }
                    }
                    break;

                // CONTAINMENT CHECK IN BINARY SEARCH
                // Uses almost the same code as before

                // SHAPE CONSTRUCTION

                case 33:
                    {
                        // Setup leader election on valid placements
                        leaderElection.Init(validPlacement.GetCurrentValue(), true);
                        PinConfiguration pc = GetContractedPinConfiguration();
                        leaderElection.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        leaderElection.ActivateSend();
                        round.SetValue(round + 1);
                    }
                    break;
                case 34:
                    {
                        leaderElection.ActivateReceive();
                        if (leaderElection.IsFinished())
                        {
                            // Setup shape construction
                            marker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));
                            shapeConstr.Init(leaderElection.IsLeader(), finalRotation);
                            ActivateShapeConstrSend(false);
                            round.SetValue(round + 1);
                        }
                        else
                        {
                            SetPlannedPinConfiguration(GetCurrentPinConfiguration());
                            leaderElection.ActivateSend();
                        }
                    }
                    break;
                case 35:
                    {
                        shapeConstr.ActivateReceive();

                        SubShapeConstruction.ShapeElement t = shapeConstr.ElementType();
                        if (t == SubShapeConstruction.ShapeElement.NODE)
                            SetMainColor(ColorData.Particle_Green);
                        else if (t == SubShapeConstruction.ShapeElement.EDGE)
                            SetMainColor(ColorData.Particle_Blue);
                        else if (t == SubShapeConstruction.ShapeElement.FACE)
                            SetMainColor(ColorData.Particle_Aqua);
                        else
                            SetMainColor(ColorData.Particle_Black);

                        if (shapeConstr.IsFinished())
                        {
                            finished.SetValue(true);
                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                        }
                        else
                        {
                            if (shapeConstr.ResetScaleCounter())
                            {
                                // Set marker to start
                                marker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));
                                round.SetValue(round + 1);
                            }
                            else
                            {
                                ActivateShapeConstrSend();
                            }
                        }
                    }
                    break;
                case 36:
                    {
                        ActivateShapeConstrSend();
                        round.SetValue(round - 1);
                    }
                    break;
            }
        }

        /// <summary>
        /// Helper to find the number of shape parameters used
        /// by the current shape.
        /// </summary>
        /// <returns>The number of shape parameters we use to
        /// detect a shape of the current constituent's type.</returns>
        private int NumShapeParams()
        {
            ShapeType shapeType = constituents[shapeIdx].shapeType;
            // Triangle needs 1, parallelogram 2
            if (shapeType < ShapeType.TRAPEZOID)
                return (int)shapeType + 1;
            // Trapezoid needs 2
            else if (shapeType == ShapeType.TRAPEZOID)
                return 2;
            // Pentagon needs 5
            else if (shapeType == ShapeType.PENTAGON)
                return 5;

            return -1;
        }

        /// <summary>
        /// Helper starting binary operations on shape parameters.
        /// Sets up the binops utils on the counters and starts either
        /// a MULT, an ADD or an MSB operation. The current rotation counter
        /// indicates the index of the shape parameter and whether
        /// a multiplication, addition or MSB detection is required.
        /// </summary>
        /// <param name="comparison">Whether we are in the setup phase
        /// and want to perform the comparison and division by the
        /// longest line length.</param>
        private void InitBinOpsShapeParam(bool comparison = true)
        {
            if (!ll.IsOnMaxLine())
                return;

            Direction d = ll.GetMaxDir();
            Direction opp = d.Opposite();
            bool pred = HasNeighborAt(opp);
            bool succ = HasNeighborAt(d);
            Direction dirPred = pred ? opp : Direction.NONE;
            Direction dirSucc = succ ? d : Direction.NONE;
            int idx = rotation.GetCurrentValue();

            // In the first phase, we use the first 3 operations to:
            //  - Compare R to the longest line length S
            //  - Compute R' := R / S
            //  - Find the MSB of R' and set R := R'
            if (comparison)
            {
                if (idx == 0)
                {
                    // Compare S to R
                    binops.Init(SubBinOps.Mode.COMP, ShapeParamBit(longestLine), dirPred, dirSucc, Bit_R);
                }
                else if (idx == 1)
                {
                    // Compute R / S
                    binops.Init(SubBinOps.Mode.DIV, Bit_R, dirPred, dirSucc, ShapeParamBit(longestLine), MSB_R);
                }
                else
                {
                    // Find MSB of R
                    binops.Init(SubBinOps.Mode.MSB, Bit_R, dirPred, dirSucc);
                }
            }
            else
            {
                int n = NumShapeParams();
                int i = idx % n;
                if (idx < n)
                {
                    if (i < 4)
                    {
                        // Multiplication
                        string shapeParam = constituents[shapeIdx].shapeParams[i];
                        binops.Init(SubBinOps.Mode.MULT, ShapeParamBit(shapeParam), dirPred, dirSucc, Bit_M, ShapeParamMSB(shapeParam));
                    }
                    else
                    {
                        // This only occurs for pentagons: Compute k*a + 1
                        // Bit of the second operand is 1 for the counter start and 0 for everything else
                        binops.Init(SubBinOps.Mode.ADD, ScaledShapeBit(0), dirPred, dirSucc, !pred);
                    }
                }
                else
                {
                    // MSB
                    binops.Init(SubBinOps.Mode.MSB, ScaledShapeBit(i), dirPred, dirSucc);
                }
            }
            PinConfiguration pc = GetContractedPinConfiguration();
            binops.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);
            binops.ActivateSend();
        }

        /// <summary>
        /// Helper starting binary addition of L and R,
        /// MSB detection of M or comparison of L to M.
        /// </summary>
        /// <param name="findMSB">Whether the MSB detection
        /// should be initialized rather than addition.</param>
        /// <param name="compare">Whether the comparison should be
        /// initialized rather than addition.</param>
        private void InitBinOpsSearch(bool findMSB = false, bool compare = false)
        {
            if (!ll.IsOnMaxLine())
                return;

            Direction d = ll.GetMaxDir();
            Direction opp = d.Opposite();
            bool pred = HasNeighborAt(opp);
            bool succ = HasNeighborAt(d);
            if (findMSB)
                binops.Init(SubBinOps.Mode.MSB, Bit_M, pred ? opp : Direction.NONE, succ ? d : Direction.NONE);
            else if (compare)
                binops.Init(SubBinOps.Mode.COMP, Bit_L, pred ? opp : Direction.NONE, succ ? d : Direction.NONE, Bit_M);
            else
                binops.Init(SubBinOps.Mode.ADD, Bit_L, pred ? opp : Direction.NONE, succ ? d : Direction.NONE, Bit_R);
            
            PinConfiguration pc = GetContractedPinConfiguration();
            binops.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);
            binops.ActivateSend();
        }

        /// <summary>
        /// Helper for running any binary operation subroutine.
        /// Calls the receive and send activations.
        /// </summary>
        /// <returns><c>true</c> as soon as the subroutine
        /// is finished.</returns>
        private bool RunBinOps()
        {
            if (!ll.IsOnMaxLine())
                return false;

            binops.ActivateReceive();
            if (binops.IsFinished())
                return true;

            PinConfiguration pc = GetContractedPinConfiguration();
            binops.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);
            binops.ActivateSend();

            return false;
        }

        /// <summary>
        /// Helper running the binary operations on shape parameters.
        /// Activates the binops routine and writes the multiplication
        /// or MSB result when it is finished. Sets up the pin configuration
        /// and sends beep if it is not finished.
        /// </summary>
        /// <param name="terminate">Whether we have to terminate prematurely
        /// during the first phase because the longest shape line is too long.</param>
        /// <returns><c>true</c> if and only if the binops routine is finished.</returns>
        private bool RunBinOpsShapeParam(out bool terminate)
        {
            terminate = false;
            if (!ll.IsOnMaxLine())
                return false;

            if (RunBinOps())
            {
                int idx = rotation.GetCurrentValue();

                // Special case in first 3 rounds of first phase
                // In the first phase, we use the first 3 operations to:
                //  - Compare R to the longest line length S
                //  - Compute R' := R / S
                //  - Find the MSB of R' and set R := R'
                if (round.GetCurrentValue() < 5)
                {
                    if (idx == 0)
                    {
                        // Check comparison result
                        if (binops.CompResult() == SubComparison.ComparisonResult.GREATER)
                        {
                            // Terminate due to longest shape line being longer than longest line in the system
                            terminate = true;
                        }
                    }
                    else if (idx == 1)
                    {
                        // Store bit of R / S
                        Bit_R = binops.ResultBit();
                        MSB_R = false;
                    }
                    else
                    {
                        // Store MSB of R (*and copy to M*)
                        MSB_R = binops.IsMSB();
                        MSB_M = MSB_R;
                        Bit_M = Bit_R;
                    }
                }
                else
                {
                    int n = NumShapeParams();
                    int i = idx % n;
                    if (idx < n)
                    {
                        // Multiplication result
                        SetScaledShapeBit(i, binops.ResultBit());
                    }
                    else
                    {
                        // MSB
                        SetScaledShapeMSB(i, binops.IsMSB());
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper to initialize and start the shape containment subroutine
        /// for the correct constituent shape.
        /// </summary>
        /// <param name="useScaledShapeBits">Whether the scaled shape bits or
        /// the scale 1 shape bits should be used.</param>
        private void StartShapeContainmentCheck(bool useScaledShapeBits = true)
        {
            Direction counterPred = Direction.NONE;
            Direction counterSucc = Direction.NONE;
            ShapeInfo shapeInfo = constituents[shapeIdx];
            ShapeType shapeType = shapeInfo.shapeType;
            if (ll.IsOnMaxLine())
            {
                Direction maxDir = ll.GetMaxDir();
                if (HasNeighborAt(maxDir.Opposite()))
                    counterPred = maxDir.Opposite();
                if (HasNeighborAt(maxDir))
                    counterSucc = maxDir;

                bool[] _bits = new bool[5];
                bool[] _msbs = new bool[5];
                for (int i = 0; i < 5; i++)
                {
                    string shapeParam = shapeInfo.shapeParams[i];
                    _bits[i] = useScaledShapeBits ? ScaledShapeBit(i) : ShapeParamBit(shapeParam);
                    _msbs[i] = useScaledShapeBits ? ScaledShapeMSB(i) : ShapeParamMSB(shapeParam);
                }

                containment.Init(shapeType, shapeInfo.directionW, shapeInfo.directionH, rotation, true, !IsValidPlacement(rotation), false, Direction.NONE,
                    counterPred, counterSucc, _bits[0], _msbs[0], _bits[1], _msbs[1], _bits[2], _msbs[2], _bits[3], _msbs[3], _bits[4], _msbs[4]);
            }
            else
            {
                containment.Init(shapeType, shapeInfo.directionW, shapeInfo.directionH, rotation, true, !IsValidPlacement(rotation));
            }
            PinConfiguration pc = GetContractedPinConfiguration();
            containment.SetupPC(pc);
            SetPlannedPinConfiguration(pc);
            containment.ActivateSend();
        }

        /// <summary>
        /// Helper setting up four global circuits on
        /// partition sets 0-3.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupGlobalCircuits(PinConfiguration pc)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            for (int i = 0; i < 4; i++)
                pc.SetStarConfig(i, inverted, i);
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
            shapeConstr.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);

            if (checkNeedScaleBit && shapeConstr.NeedScaleBit())
            {
                shapeConstr.ActivateSend(marker.GetCurrentValue() && Bit_L, marker.GetCurrentValue() && MSB_L);
                if (ll.IsOnMaxLine())
                {
                    Direction d = ll.GetMaxDir().Opposite();
                    marker.SetValue(HasNeighborAt(d) && ((SCStarConvexShapesParticle)GetNeighborAt(d)).marker);
                }
            }
            else
            {
                shapeConstr.ActivateSend();
            }
        }

        // Bit helpers

        #region Bit Helpers

        // Binary counter bits
        private bool Bit_L
        {
            get { return GetStateBit(bit_counter); }
            set { SetStateBit(bit_counter, value); }
        }

        private bool Bit_R
        {
            get { return GetStateBit(bit_counter + 1); }
            set { SetStateBit(bit_counter + 1, value); }
        }

        private bool Bit_M
        {
            get { return GetStateBit(bit_counter + 2); }
            set { SetStateBit(bit_counter + 2, value); }
        }

        /// <summary>
        /// Helper to get the bit of M from a neighboring amoebot.
        /// </summary>
        /// <returns>The currently stored bit of M.</returns>
        public bool GetBitM()
        {
            return ((bitStorage.GetValue() >> (bit_counter + 2)) & 1) > 0;
        }

        // Binary counter MSBs
        private bool MSB_L
        {
            get { return GetStateBit(msb_counter); }
            set { SetStateBit(msb_counter, value); }
        }

        private bool MSB_R
        {
            get { return GetStateBit(msb_counter + 1); }
            set { SetStateBit(msb_counter + 1, value); }
        }

        private bool MSB_M
        {
            get { return GetStateBit(msb_counter + 2); }
            set { SetStateBit(msb_counter + 2, value); }
        }

        // Shape bit, MSB and valid flag getters
        private bool ScaledShapeBit(int idx)
        {
            return GetStateBit(bit_shape + idx);
        }

        private bool ScaledShapeMSB(int idx)
        {
            return GetStateBit(msb_shape + idx);
        }

        private bool IsValidPlacement(int rot)
        {
            return GetStateBit(bit_valid + rot);
        }

        private bool PlacementExists(int rot)
        {
            return GetStateBit(bit_placement_exists + rot);
        }

        // Shape bit, MSB and valid flag setters
        private void SetScaledShapeBit(int idx, bool value)
        {
            SetStateBit(bit_shape + idx, value);
        }

        private void SetScaledShapeMSB(int idx, bool value)
        {
            SetStateBit(msb_shape + idx, value);
        }

        private void SetValidPlacement(int rot, bool value)
        {
            SetStateBit(bit_valid + rot, value);
        }

        private void SetPlacementExists(int rot, bool value)
        {
            SetStateBit(bit_placement_exists + rot, value);
        }

        /// <summary>
        /// Helper for reading a binary shape parameter bit from its
        /// string representation, depending on our shape parameter index.
        /// </summary>
        /// <param name="shapeParam">The binary number from which to read.</param>
        /// <returns><c>true</c> if and only if we are on a counter and our
        /// bit of the shape parameter is 1.</returns>
        private bool ShapeParamBit(string shapeParam)
        {
            int idx = paramIdx;
            return idx != -1 && idx < shapeParam.Length && shapeParam[idx] == '1';
        }

        /// <summary>
        /// Helper for finding a binary shape parameter MSB from its string
        /// representation, depending on our shape parameter index.
        /// </summary>
        /// <param name="shapeParam">The binary number from which to read.</param>
        /// <returns><c>true</c> if and only if we are on a counter and at
        /// the MSB of the shape parameter.</param>
        private bool ShapeParamMSB(string shapeParam)
        {
            int idx = paramIdx;
            return idx != -1 && idx == shapeParam.Length - 1;
        }

        /// <summary>
        /// Helper for reading bits from the bit storage int.
        /// </summary>
        /// <param name="bit">The index of the bit to read.</param>
        /// <returns>The bit stored at the given position.</returns>
        private bool GetStateBit(int bit)
        {
            return ((bitStorage.GetCurrentValue() >> bit) & 1) > 0;
        }

        /// <summary>
        /// Helper for writing bits in the bit storage int.
        /// </summary>
        /// <param name="bit">The index of the bit to be written.</param>
        /// <param name="value">The new value of the bit.</param>
        private void SetStateBit(int bit, bool value)
        {
            bitStorage.SetValue((bitStorage.GetCurrentValue() & ~(1 << bit)) | ((value ? 1 : 0) << bit));
        }
        #endregion
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SCStarConvexShapesInitializer : InitializationMethod
    {
        /// <summary>
        /// Container for reading in star convex shape descriptions.
        /// </summary>
        [System.Serializable]
        public class StarConvexShapeContainer
        {
            [System.Serializable]
            public class Constituent
            {
                public ShapeType shapeType = ShapeType.TRIANGLE;
                public Direction directionW = Direction.NONE;
                public Direction directionH = Direction.NONE;
                public int a = 0;
                public int d = 0;
                public int c = 0;
                public int a2 = 0;
                public int a3 = 0;
            }

            public Shape shape;
            public Constituent[] constituents;
        }

        public SCStarConvexShapesInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(string shapeFile = "starConvexShape.json",
            int numAmoebots = 150, float holeProb = 0.1f, bool fillHoles = false, bool prioritizeInner = true, float lambda = 0.25f)
        {
            // Read the given shape from its file
            string json = System.IO.File.ReadAllText(FilePaths.path_shapes + shapeFile);
            StarConvexShapeContainer container = JsonUtility.FromJson<StarConvexShapeContainer>(json);
            // Check whether it is consistent
            if (container.shape.IsConsistent())
                container.shape.GenerateTraversal();
            else
                Log.Error("Shape is not consistent!");

            // Process the information and give it to the algorithm class
            SCStarConvexShapesParticle.shape = container.shape;
            SCStarConvexShapesParticle.constituents = new SCStarConvexShapesParticle.ShapeInfo[container.constituents.Length];
            int longestParam = 0;
            for (int i = 0; i < container.constituents.Length; i++)
            {
                StarConvexShapeContainer.Constituent cont = container.constituents[i];
                string a = IntToBinary(cont.a);
                string d = IntToBinary(cont.d);
                string c = IntToBinary(cont.c);
                string a2 = IntToBinary(cont.a2);
                string a3 = IntToBinary(cont.a3);
                longestParam = Mathf.Max(longestParam, a.Length, d.Length, c.Length, a2.Length, a3.Length);
                SCStarConvexShapesParticle.constituents[i] = new SCStarConvexShapesParticle.ShapeInfo(cont.shapeType, cont.directionW, cont.directionH, a, d, c, a2, a3);
            }
            string ll_str = IntToBinary(container.shape.GetLongestLineLength());
            SCStarConvexShapesParticle.longestLine = ll_str;
            SCStarConvexShapesParticle.longestParameter = Mathf.Max(longestParam, ll_str.Length);

            // Draw the shape
            AS2.UI.LineDrawer.Instance.Clear();
            container.shape.Draw(Vector2Int.zero);
            AS2.UI.LineDrawer.Instance.SetTimer(60);

            // Place the amoebots
            foreach (Vector2Int pos in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles, null, true, prioritizeInner, lambda))
            {
                AddParticle(pos);
            }
        }

        private string IntToBinary(int num)
        {
            if (num == 0)
                return "0";

            string s = "";
            while (num > 0)
            {
                s += (num & 1) > 0 ? '1' : '0';
                num >>= 1;
            }

            return s;
        }
    }

} // namespace AS2.Algos.SCStarConvexShapes
