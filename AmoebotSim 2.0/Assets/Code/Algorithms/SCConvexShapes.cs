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

namespace AS2.Algos.SCConvexShapes
{

    /// <summary>
    /// Shape containment solution for convex shapes.
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
    /// Additionally, fewer rotations are tested for triangles and parallelograms
    /// since they are rotationally symmetric.
    /// </para>
    /// </summary>

    // Algorithm plan:
    //  - Run longest lines subroutine, getting longest length k                                Rounds 0-1
    //  - Initialize L := 1 and R := k                                                          Round 1
    //  - Write all required shape parameters to the counter(s)                                 Round 1-2
    //  - Compute all scaled shape parameters using scale R                                     Rounds 3-4 (round 5 is for waiting amoebots)
    //  - For rotation m = 0,...,5:                                                             Round 6
    //      - Run shape containment check with m, R                                             Round 7
    //      - If successful:
    //          - Store rotation and valid placements
    //          - Jump to shape construction
    //  - (Do not compute scaled parameters again since scale is 1)
    //  - For rotation m = 0,...,5:                                                             Round 8
    //      - Run shape containment check with m, L = 1                                         Round 9
    //      - If successful: Continue with next phase
    //  - If not successful: Terminate with failure
    //  - Binary search:                                                                        Round 10
    //      - Compute M := (L + R) / 2                                                          Rounds 11-13
    //      - If M = L:                                                                         Round 14
    //          - Go to shape construction phase
    //      - Compute all scaled shape parameters using scale M                                 Rounds 15-17
    //      - For rotation m = 0,...,5:                                                         Round 18
    //          - Run shape containment check with m, M                                         Rounds 18-19
    //          - If successful:
    //              - Set L := M
    //              - Store rotation and valid placements
    //              - Continue with next iteration
    //      - If not successful for any m:
    //          - Set R := M
    //          - Continue with next iteration
    //  - Shape construction:                                                                   Round 19
    //      - Run leader election on valid placements                                           Rounds 19-20
    //      - Run shape construction subroutine to construct shape, using scale L               Rounds 20-21

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

    // SETUP SHAPE PARAMETERS

    // Round 2:
    //  - If counter >= length of binary shape parameters:
    //      - Set counter to 0
    //      - SPLIT:
    //          - Counters go to round 3
    //          - Others go to round 5 and setup 2 global circuits
    //  - Else:
    //      - Marker writes current bits and MSBs to shape parameter counters
    //      - Marker moves one position ahead
    //      - Increment counter

    // Round 3:
    //  - If counter > number of parameters to compute (*2 + 3 because of MSBs and longest line check):
    //      - Set rotation to 0
    //      - Setup 2 global circuits and beep on first
    //      - Go to round 5
    //  - Else:
    //      - Init binop to compute R * parameter or MSB
    //      - Start binop
    //      - Go to round 4

    // Round 4:
    //  - Continue binop
    //  - If binop is finished:
    //      - Store result in scaled parameter or MSB
    //      - Increment counter
    //      - Go to round 3

    // Round 5 (WAIT):
    //  - Wait for beep on global circuit
    //      - Then go to round 6

    // CHECK LARGEST SCALE

    // Round 6:
    //  - If rotation > 5:
    //      - Reset rotation to 0
    //      - Go to round 8
    //  - Else:
    //      - Init containment check for current rotation and scaled parameters
    //      - Start containment check

    // Round 7:
    //  - If containment check is finished:
    //      - Success:
    //          - Store valid placements and rotation
    //          - Copy scale R into L for the shape construction
    //          - Jump to round 20
    //      - Failure:
    //          - Increment rotation
    //          - Go to round 6
    //  - Else:
    //      - Continue running

    // CHECK SCALE 1

    // Round 8:
    //  - If rotation > 5:
    //      - Terminate with failure
    //  - Else:
    //      - Init containment check for current rotation and non-scaled parameters
    //      - Start containment check

    // Round 9:
    //  - If containment check is finished:
    //      - Success:
    //          - Store valid placements and rotation
    //          - Go to round 10
    //      - Failure:
    //          - Increment rotation and go to round 8
    //  - Else:
    //      - Continue running

    // BINARY SEARCH

    // Round 10:
    //  - Initialize and start binop for computing M := L + R
    //  - SPLIT:
    //      - Counter amoebots go to round 11
    //      - Other amoebots setup 2 global circuits and go to round 15

    // Round 11:
    //  - If binop is finished:
    //      - Store result in M
    //      - Go to round 12
    //  - Else:
    //      - Continue running

    // Round 12:
    //  - Shift each bit of M one position backwards
    //  - Setup binop for finding MSB of M

    // Round 13:
    //  - If binop is finished:
    //      - Store MSB of M
    //      - Setup and start binop for comparing L to M
    //  - Else:
    //      - Continue running

    // Round 14:
    //  - If binop is finished:
    //      - If L = M:
    //          - Setup two global circuits and beep on first one
    //          - Go to round 15
    //      - Else:
    //          - Set counter to 0
    //          - Go to round 16
    //  - Else:
    //      - Continue running

    // Round 15 (WAIT):
    //  - Listen on two global circuits
    //  - If beep on first circuit:
    //      - Go to round 20
    //  - If beep on second circuit:
    //      - Go to round 18

    // Round 16:
    //  - If counter > number of parameters * 2:
    //      - Set rotation to 0
    //      - Setup two global circuits and beep on the second
    //      - Go to round 15
    //  - Else:
    //      - Init binop to compute M * parameter or MSB
    //      - Start binop

    // Round 17:
    //  - Continue binop
    //  - If binop is finished:
    //      - Store result in scaled parameter or MSB
    //      - Increment counter
    //      - Go to round 16

    // CONTAINMENT CHECK

    // Round 18:
    //  - If rotation > 5:
    //      - Set R := M
    //      - Go to round 10
    //  - Else:
    //      - Start containment check for current rotation and M

    // Round 19:
    //  - If containment check is finished:
    //      - Success:
    //          - Set L := M
    //          - Store valid positions and rotation
    //          - Go to round 10
    //      - Failure:
    //          - Increment rotation
    //          - Go to round 18
    //  - Else:
    //      - Continue running

    // SHAPE CONSTRUCTION

    // Round 20:
    //  - Setup leader election on valid placements
    //      - But use entire system

    // Round 21:
    //  - If leader election is finished:
    //      - Setup shape construction subroutine for leader and scale L
    //      - Markers at counter starts

    // Round 22:
    //  - Continue running shape construction
    //  - If shape construction is finished:
    //      - Terminate with success
    //  - If scale reset:
    //      - Set marker to counter start
    //      - Go to round 23
    //  - Else:
    //      - Continue running (maybe with scale bit)
    //      - Forward marker

    // Round 23:
    //  - Send next bit of shape construction
    //  - Forward marker
    //  - Go back to round 22

    public class SCConvexShapesParticle : ParticleAlgorithm
    {
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
        public static new string Name => "SC Convex Shapes";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SCConvexShapesInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<int> round;

        // Rotation / generic counter
        ParticleAttribute<int> rotation;
        ParticleAttribute<int> finalRotation;
        ParticleAttribute<bool> marker;
        ParticleAttribute<bool> validPlacement;
        ParticleAttribute<bool> finished;
        
        // Stores 26 bits
        // 3 bits for binary counter L, R, M
        // 3 MSBs for binary counter
        // 6 shape parameter bits + 1 bit of longest line length
        // 6 shape parameter MSBs
        // 6 scaled shape parameter bits
        // 6 scaled shape parameter MSBs
        //      30  29  28  27  26  25    24  23  22  21  20  19    18  17  16       15  14  13  12  11  10    9   8   7   6   5   4   3     2   1   0
        // x    x   x   x   x   x   x     x   x   x   x   x   x     x   x   x        x   x   x   x   x   x     x   x   x   x   x   x   x     x   x   x
        //      Scaled shape MSBs         Shape MSBs                Counter MSBs     Scaled shape bits         Shape bits                Counter bits
        ParticleAttribute<int> bitStorage;

        // Bit index constants
        private const int bit_counter = 0;
        private const int bit_shape = 3;
        private const int bit_shapeS = 10;
        private const int msb_counter = 16;
        private const int msb_shape = 19;
        private const int msb_shapeS = 25;

        SubBinOps binops;
        SubLeaderElectionSC leaderElection;
        SubLongestLines ll;
        SubPASC2 pasc1;
        SubParallelogram parallelogram;
        SubMergingAlgo mergeAlgo;
        SubConvexShapeContainment containment;
        SubShapeConstruction shapeConstr;

        // Static data set by the generation method
        public static Shape shape;
        public static ShapeType shapeType;
        public static bool hexNeedsPentagon;        // Whether the hexagon requires a pentagon check
        public static Direction shapeDirectionW;    // The 2 (3) directions determining the orientation of the shape
        public static Direction shapeDirectionH1;   // 3 are needed for hexagons
        public static Direction shapeDirectionH2;   // H2 must always be a trapezoid direction for hexagons

        /// <summary>
        /// Shape parameters as binary strings in the order a, d, c, a' = a + c, a + 1, b = d2.
        /// The last entry is the length of the longest line in the shape.
        /// <para>
        /// The shape type determines which parameters will be used:
        /// <list type="bullet">
        /// <item>
        ///     <b>Triangle:</b> Only a is used.
        /// </item>
        /// <item>
        ///     <b>Parallelogram and trapezoid:</b> Only a (width) and d (height) are used.
        /// </item>
        /// <item>
        ///     <b>Pentagon:</b> a, d, c, a' and a + 1 are used (a + 1 only for scale 1).
        /// </item>
        /// <item>
        ///     <b>Hexagon:</b> All parameters are used. If the shape contains a pentagon,
        ///     it uses the pentagon parameters and a, b for the trapezoid. Otherwise, it will
        ///     use a and d for the first trapezoid and a and c for the second.
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public static string[] shapeParams = new string[7];

        public SCConvexShapesParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            rotation = CreateAttributeInt("Rotation", 0);
            finalRotation = CreateAttributeInt("Final Rotation", -1);
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
                        if (rotation >= MaxShapeParamLen())
                        {
                            // Done computing the shape parameters
                            rotation.SetValue(0);
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
                            // Marker writes bits and MSBs of shape parameters
                            WriteShapeParams();
                            // Marker moves one position ahead
                            if (ll.IsOnMaxLine())
                            {
                                Direction d = ll.GetMaxDir().Opposite();
                                marker.SetValue(HasNeighborAt(d) && ((SCConvexShapesParticle)GetNeighborAt(d)).marker);
                            }
                            // Increment counter
                            rotation.SetValue(rotation + 1);
                        }
                    }
                    break;
                case 3:
                case 16:    // From binary search, have to do almost the same computation
                    {
                        int n = NumShapeParams() * 2;
                        // In first phase: Add 3 rounds to perform longest line check
                        if (round == 3)
                            n += 3;
                        if (rotation > n)
                        {
                            rotation.SetValue(0);

                            PinConfiguration pc = GetContractedPinConfiguration();
                            if (round == 3)
                            {
                                SetupGlobalCircuits(pc);
                                SetPlannedPinConfiguration(pc);
                                pc.SendBeepOnPartitionSet(0);
                                round.SetValue(5);
                            }
                            else if (round == 16)
                            {
                                SetupGlobalCircuits(pc);
                                SetPlannedPinConfiguration(pc);
                                pc.SendBeepOnPartitionSet(1);
                                round.SetValue(15);
                            }
                        }
                        else
                        {
                            // Initialize and start binop to compute R * parameter or MSB
                            // In binary search, start binop to compute M * parameter or MSB
                            InitBinOpsShapeParam(round == 16);
                            round.SetValue(round + 1);
                        }
                    }
                    break;
                case 4:
                case 17:    // From binary search
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
                                pc.SendBeepOnPartitionSet(1);
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
                        if (pc.ReceivedBeepOnPartitionSet(1))
                        {
                            // Terminate with failure
                            finished.SetValue(true);
                            SetMainColor(ColorData.Particle_Red);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            round.SetValue(6);
                        }
                    }
                    break;

                // CHECK LARGEST SCALE

                case 6:
                case 18:    // From binary search, need to perform containment check too
                    {
                        if (rotation >= NumRotations())
                        {
                            rotation.SetValue(0);
                            if (round == 6)
                            {
                                // Continue with next step
                                round.SetValue(8);
                            }
                            else if (round == 18)
                            {
                                // Set R := M
                                if (ll.IsOnMaxLine())
                                {
                                    Bit_R = Bit_M;
                                    MSB_R = MSB_M;
                                }
                                round.SetValue(10);
                            }
                        }
                        else
                        {
                            // Check next rotation
                            // Init containment check
                            StartShapeContainmentCheck();
                            
                            round.SetValue(round + 1);
                        }
                    }
                    break;
                case 7:
                case 9:     // (From scale 1 check)
                case 19:    // From binary search
                    {
                        containment.ActivateReceive();
                        if (containment.IsFinished())
                        {
                            if (containment.Success())
                            {
                                // Always store valid positions and rotation, also reset rotation
                                validPlacement.SetValue(containment.IsRepresentative());
                                finalRotation.SetValue(rotation);
                                rotation.SetValue(0);

                                if (round == 7)
                                {
                                    // Copy scale R into L
                                    if (ll.IsOnMaxLine())
                                    {
                                        Bit_L = Bit_R;
                                        MSB_L = MSB_R;
                                    }
                                    round.SetValue(20);
                                }
                                else if (round == 9)
                                    round.SetValue(10);
                                else if (round == 19)
                                {
                                    // Set L := M
                                    if (ll.IsOnMaxLine())
                                    {
                                        Bit_L = Bit_M;
                                        MSB_L = MSB_M;
                                    }
                                    round.SetValue(10);
                                }
                            }
                            else
                            {
                                // Always increment rotation
                                rotation.SetValue(rotation + 1);
                                // Go to previous round (7 -> 6, 9 -> 8, 19 -> 18)
                                round.SetValue(round - 1);
                            }
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

                // CHECK SCALE 1

                case 8:
                    {
                        if (rotation >= NumRotations())
                        {
                            // Terminate with failure
                            finished.SetValue(true);
                            SetMainColor(ColorData.Particle_Red);
                        }
                        else
                        {
                            // Check next rotation
                            // Init containment check
                            StartShapeContainmentCheck(false);

                            round.SetValue(9);
                        }
                    }
                    break;

                // BINARY SEARCH

                case 10:
                    {
                        // Start adding L + R
                        InitBinOpsSearch();
                        // Split: Counters go to round 11, others to round 15 with 2 global circuits
                        if (ll.IsOnMaxLine())
                        {
                            round.SetValue(11);
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupGlobalCircuits(pc);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(15);
                        }

                    }
                    break;
                case 11:
                case 13:
                case 14:
                    {
                        // All 3 rounds wait for binary operations to finish
                        // Just the reaction when it is finished differs
                        bool isFinished = RunBinOps();
                        if (isFinished)
                        {
                            if (round == 11)
                            {
                                // Store result in M
                                Bit_M = binops.ResultBit();
                                round.SetValue(12);
                            }
                            else if (round == 13)
                            {
                                // Store MSB of M
                                MSB_M = binops.IsMSB();

                                // Setup binary operation for comparing L to M
                                InitBinOpsSearch(false, true);
                                round.SetValue(14);
                            }
                            else if (round == 14)
                            {
                                if (binops.CompResult() == SubComparison.ComparisonResult.EQUAL)
                                {
                                    PinConfiguration pc = GetContractedPinConfiguration();
                                    SetupGlobalCircuits(pc);
                                    SetPlannedPinConfiguration(pc);
                                    pc.SendBeepOnPartitionSet(0);
                                    round.SetValue(15);
                                }
                                else
                                {
                                    rotation.SetValue(0);
                                    round.SetValue(16);
                                }
                            }
                        }
                    }
                    break;
                case 12:
                    {
                        // Shift each bit of M one position backwards
                        Direction d = ll.GetMaxDir();
                        Bit_M = HasNeighborAt(d) && ((SCConvexShapesParticle)GetNeighborAt(d)).GetBitM();

                        // Setup binary operation for finding MSB of M
                        InitBinOpsSearch(true);
                        round.SetValue(13);
                    }
                    break;
                case 15: // WAIT round
                    {
                        // Wait for beep on global circuits
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            round.SetValue(20);
                        }
                        else if (pc.ReceivedBeepOnPartitionSet(1))
                        {
                            round.SetValue(18);
                        }
                    }
                    break;

                // CONTAINMENT CHECK
                // (All handled similarly to other rounds)

                // SHAPE CONSTRUCTION

                case 20:
                    {
                        // Setup leader election on valid placements
                        leaderElection.Init(validPlacement, true);
                        PinConfiguration pc = GetContractedPinConfiguration();
                        leaderElection.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        leaderElection.ActivateSend();
                        round.SetValue(21);
                    }
                    break;
                case 21:
                    {
                        leaderElection.ActivateReceive();
                        if (leaderElection.IsFinished())
                        {
                            // Setup shape construction
                            marker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));
                            shapeConstr.Init(leaderElection.IsLeader(), finalRotation);
                            ActivateShapeConstrSend(false);
                            round.SetValue(22);
                        }
                        else
                        {
                            SetPlannedPinConfiguration(GetCurrentPinConfiguration());
                            leaderElection.ActivateSend();
                        }
                    }
                    break;
                case 22:
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
                                round.SetValue(23);
                            }
                            else
                            {
                                ActivateShapeConstrSend();
                            }
                        }
                    }
                    break;
                case 23:
                    {
                        ActivateShapeConstrSend();
                        round.SetValue(22);
                    }
                    break;
            }
        }

        /// <summary>
        /// Helper to find the number of shape parameters used
        /// by the current shape.
        /// </summary>
        /// <returns>The number of shape parameters we use to
        /// detect a shape of our current type.</returns>
        private int NumShapeParams()
        {
            // Triangle needs 1, parallelogram 2
            if (shapeType < ShapeType.TRAPEZOID)
                return (int)shapeType + 1;
            // Trapezoid needs 2
            else if (shapeType == ShapeType.TRAPEZOID)
                return 2;
            // Pentagon needs 5
            else if (shapeType == ShapeType.PENTAGON)
                return 5;
            // Hexagon may need 6 or 3
            else
                return hexNeedsPentagon ? 6 : 3;
        }

        /// <summary>
        /// Helper to compute the maximum number of bits of any used
        /// base shape parameter. Includes the longest line length.
        /// </summary>
        /// <returns>The maximum length of a binary shape parameter
        /// used by our shape type.</returns>
        private int MaxShapeParamLen()
        {
            int m = shapeParams[0].Length;
            int n = NumShapeParams();
            for (int i = 1; i < n; i++)
            {
                m = Mathf.Max(m, shapeParams[i].Length);
            }
            m = Mathf.Max(m, shapeParams[6].Length);
            return m;
        }

        /// <summary>
        /// Determines how many rotations have to be checked for the
        /// current shape type. Triangles need only two rotations,
        /// parallelograms need only 3 rotations, other shapes need
        /// all six rotations to be checked.
        /// </summary>
        /// <returns>The number of rotations to be checked for the
        /// current shape type.</returns>
        private int NumRotations()
        {
            if (shapeType == ShapeType.TRIANGLE)
                return 2;
            else if (shapeType == ShapeType.PARALLELOGRAM)
                return 3;
            else
                return 6;
        }

        /// <summary>
        /// Helper letting a currently marked amoebot write
        /// the bits and MSBs of the required shape parameters
        /// into its memory.
        /// </summary>
        private void WriteShapeParams()
        {
            if (marker.GetCurrentValue())
            {
                int idx = rotation.GetCurrentValue();
                int n = NumShapeParams();
                for (int i = 0; i < n; i++)
                {
                    int l = shapeParams[i].Length;
                    if (idx < l && shapeParams[i][idx] == '1')
                    {
                        SetShapeBit(i, true);
                    }
                    if (idx == l - 1)
                    {
                        SetShapeMSB(i, true);
                    }
                }
                // Also write longest line bit
                string ll = shapeParams[6];
                if (idx < ll.Length)
                    SetShapeBit(6, ll[idx] == '1');
            }
        }

        /// <summary>
        /// Helper starting binary operations on shape parameters.
        /// Sets up the binops utils on the counters and starts either
        /// a MULT, an ADD or an MSB operation. The current rotation counter
        /// indicates the index of the shape parameter and whether
        /// a multiplication, addition or MSB detection is required.
        /// </summary>
        /// <param name="useM">Whether the binary search middle value
        /// M should be used for multiplication. If <c>false</c>, R
        /// will be used.</param>
        private void InitBinOpsShapeParam(bool useM = true)
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
            bool firstPhaseSpecial = false;
            if (!useM)
            {
                if (idx > 2)
                    idx -= 3;
                else
                    firstPhaseSpecial = true;
            }
            if (firstPhaseSpecial)
            {
                if (idx == 0)
                {
                    // Compare S to R
                    binops.Init(SubBinOps.Mode.COMP, ShapeBit(6), dirPred, dirSucc, Bit_R);
                }
                else if (idx == 1)
                {
                    // Compute R / S
                    binops.Init(SubBinOps.Mode.DIV, Bit_R, dirPred, dirSucc, ShapeBit(6), MSB_R);
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
                    if (i < 4 || shapeType == ShapeType.HEXAGON && hexNeedsPentagon && i < 5)
                    {
                        // Multiplication
                        bool bitA, msbA;
                        if (i == 4)
                        {
                            // The 5th multiplication must compute k*d2 and not k*(a + 1) (only hexagons with pentagon part)
                            bitA = ShapeBit(5);
                            msbA = ShapeMSB(5);
                        }
                        else
                        {
                            bitA = ShapeBit(i);
                            msbA = ShapeMSB(i);
                        }
                        binops.Init(SubBinOps.Mode.MULT, bitA, dirPred, dirSucc, useM ? Bit_M : Bit_R, msbA);
                    }
                    else
                    {
                        // This only occurs for pentagons or hexagons that need a pentagon: Compute k*a + 1
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
                bool firstPhaseSpecial = false;
                if (round.GetCurrentValue() < 5)
                {
                    if (idx > 2)
                        idx -= 3;
                    else
                        firstPhaseSpecial = true;
                }
                if (firstPhaseSpecial)
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
                        // Store MSB of R
                        MSB_R = binops.IsMSB();
                    }
                }
                else
                {
                    int n = NumShapeParams();
                    int i = idx % n;
                    if (idx < n)
                    {
                        if (i < 4 || shapeType == ShapeType.HEXAGON && hexNeedsPentagon && i < 5)
                        {
                            // Multiplication
                            // Make sure the result of k*d2 ends up in the right location
                            SetScaledShapeBit(i < 4 ? i : 5, binops.ResultBit());
                        }
                        else
                        {
                            // This only occurs for pentagons and hexagons that need a pentagon
                            // We write the bit of k*a + 1
                            SetScaledShapeBit(4, binops.ResultBit());
                        }
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
        /// for the correct shape type.
        /// </summary>
        private void StartShapeContainmentCheck(bool useScaledShapeBits = true)
        {
            Direction counterPred = Direction.NONE;
            Direction counterSucc = Direction.NONE;
            if (ll.IsOnMaxLine())
            {
                Direction maxDir = ll.GetMaxDir();
                if (HasNeighborAt(maxDir.Opposite()))
                    counterPred = maxDir.Opposite();
                if (HasNeighborAt(maxDir))
                    counterSucc = maxDir;

                bool[] _bits = new bool[6];
                bool[] _msbs = new bool[6];
                for (int i = 0; i < 6; i++)
                {
                    _bits[i] = useScaledShapeBits ? ScaledShapeBit(i) : ShapeBit(i);
                    _msbs[i] = useScaledShapeBits ? ScaledShapeMSB(i) : ShapeMSB(i);
                }

                containment.Init(shapeType, shapeDirectionW, shapeDirectionH1, rotation, true, false, hexNeedsPentagon, shapeDirectionH2,
                    counterPred, counterSucc, _bits[0], _msbs[0], _bits[1], _msbs[1], _bits[2], _msbs[2], _bits[3], _msbs[3], _bits[4], _msbs[4],
                    _bits[5], _msbs[5]);
            }
            else
            {
                containment.Init(shapeType, shapeDirectionW, shapeDirectionH1, rotation, true, false, hexNeedsPentagon, shapeDirectionH2);
            }
            PinConfiguration pc = GetContractedPinConfiguration();
            containment.SetupPC(pc);
            SetPlannedPinConfiguration(pc);
            containment.ActivateSend();
        }

        /// <summary>
        /// Helper setting up two global circuits on
        /// partition sets 0 and 1.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        private void SetupGlobalCircuits(PinConfiguration pc)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(0, inverted, 0);
            pc.SetStarConfig(1, inverted, 1);
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
                    marker.SetValue(HasNeighborAt(d) && ((SCConvexShapesParticle)GetNeighborAt(d)).marker);
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

        // Shape bit and MSB getters
        private bool ShapeBit(int idx)
        {
            return GetStateBit(bit_shape + idx);
        }

        private bool ScaledShapeBit(int idx)
        {
            return GetStateBit(bit_shapeS + idx);
        }

        private bool ShapeMSB(int idx)
        {
            return GetStateBit(msb_shape + idx);
        }

        private bool ScaledShapeMSB(int idx)
        {
            return GetStateBit(msb_shapeS + idx);
        }

        // Shape bit and MSB setters
        private void SetShapeBit(int idx, bool value)
        {
            SetStateBit(bit_shape + idx, value);
        }

        private void SetScaledShapeBit(int idx, bool value)
        {
            SetStateBit(bit_shapeS + idx, value);
        }

        private void SetShapeMSB(int idx, bool value)
        {
            SetStateBit(msb_shape + idx, value);
        }

        private void SetScaledShapeMSB(int idx, bool value)
        {
            SetStateBit(msb_shapeS + idx, value);
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
    public class SCConvexShapesInitializer : InitializationMethod
    {
        public SCConvexShapesInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(ShapeType shapeType = ShapeType.PARALLELOGRAM, int a = 3, int d = 2, int c = 1, int b = 2,
            int numAmoebots = 150, float holeProb = 0.1f, bool fillHoles = false, bool prioritizeInner = true, float lambda = 0.25f)
        {
            SCConvexShapesParticle.shapeType = shapeType;
            SCConvexShapesParticle.shapeDirectionW = Direction.E;
            SCConvexShapesParticle.shapeDirectionH1 = Direction.NNE;
            SCConvexShapesParticle.shapeDirectionH2 = Direction.SSE;
            int longestLine = a;
            if (shapeType == ShapeType.TRIANGLE)
            {
                SCConvexShapesParticle.shape = Shape.GenSimpleConvexShape(a, a, 0);
            }
            else if (shapeType == ShapeType.PARALLELOGRAM)
            {
                SCConvexShapesParticle.shape = Shape.GenSimpleConvexShape(a, d, d);
                longestLine = Mathf.Max(a, d);
            }
            else if (shapeType == ShapeType.TRAPEZOID)
            {
                SCConvexShapesParticle.shape = Shape.GenSimpleConvexShape(a, d, 0);
            }
            else if (shapeType == ShapeType.PENTAGON)
            {
                SCConvexShapesParticle.shape = Shape.GenSimpleConvexShape(a, d, c);
                longestLine = Mathf.Max(a, d);
            }
            else if (shapeType == ShapeType.HEXAGON)
            {
                SCConvexShapesParticle.shape = Shape.GenHexagon(a, d, c, b);
                longestLine = Mathf.Max(a + Mathf.Min(b, c), b + Mathf.Min(a, d), c + Mathf.Min(a, b + d - c));
                // Find out whether the hexagon consists of two trapezoids or a pentagon and a hexagon
                if (c == b)
                {
                    // Two trapezoids
                    SCConvexShapesParticle.hexNeedsPentagon = false;
                    // Write b into c to simplify handling of shape parameters
                    c = b;
                    a = a + c;
                }
                else
                {
                    SCConvexShapesParticle.hexNeedsPentagon = true;
                    int e = b + d - c;
                    if (c < b)
                    {
                        // Pentagon is lower side
                        SCConvexShapesParticle.shapeDirectionH1 = Direction.SSE;
                        SCConvexShapesParticle.shapeDirectionH2 = Direction.NNE;
                        // Compute the shape parameters
                        int tmpB = b;
                        int tmpD = d;

                        a = a + c;
                        d = tmpB;
                        c = e - tmpD;
                        b = tmpD;
                    }
                    else
                    {
                        // Pentagon is upper side
                        a = a + b;
                        c = c - b;
                    }
                }
            }

            AS2.UI.LineDrawer.Instance.Clear();
            SCConvexShapesParticle.shape.Draw(Vector2Int.zero);
            AS2.UI.LineDrawer.Instance.SetTimer(15);

            string str_a = IntToBinary(a);
            string str_d = IntToBinary(d);
            string str_c = IntToBinary(c);
            string str_a2 = IntToBinary(a + c);
            string str_a3 = IntToBinary(a + 1);
            string str_d2 = IntToBinary(b);
            string str_ll = IntToBinary(longestLine);

            SCConvexShapesParticle.shapeParams[0] = str_a;
            SCConvexShapesParticle.shapeParams[1] = str_d;
            SCConvexShapesParticle.shapeParams[2] = str_c;
            SCConvexShapesParticle.shapeParams[3] = str_a2;
            SCConvexShapesParticle.shapeParams[4] = str_a3;
            SCConvexShapesParticle.shapeParams[5] = str_d2;
            SCConvexShapesParticle.shapeParams[6] = str_ll;

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

} // namespace AS2.Algos.SCConvexShapes
