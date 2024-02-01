using UnityEngine;
using static AS2.Constants;
using AS2.Sim;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.LongestLines;
using AS2.Subroutines.PASC;

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
    //          - Others go to round 5 and setup global circuit
    //  - Else:
    //      - Marker writes current bits and MSBs to shape parameter counters
    //      - Marker moves one position ahead
    //      - Increment counter

    // Round 3:
    //  - If counter > number of parameters to compute (*2 because of MSBs):
    //      - Set rotation to 0
    //      - Setup global circuit and beep
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
    //          - Jump to round 19
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
    //          - Go to round 20
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
    //  - If counter > number of parameters:
    //      - Set rotation to 0
    //      - Go to round 18
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

    // Round 22:
    //  - If shape construction is finished:
    //      - Terminate with success

    public class SCConvexShapesParticle : ParticleAlgorithm
    {
        public enum ShapeType
        {
            TRIANGLE = 0,
            PARALLELOGRAM = 1,
            TRAPEZOID = 2,
            PENTAGON = 3,
            HEXAGON = 4
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
        ParticleAttribute<bool> marker;
        
        // Stores 26 bits
        // 3 bits for binary counter L, R, M
        // 3 MSBs for binary counter
        // 5 shape parameter bits
        // 5 shape parameter MSBs
        // 5 scaled shape parameter bits
        // 5 scaled shape parameter MSBs
        //            25  24  23  22  21    20  19  18  17  16    15  14  13       12  11  10  9   8     7   6   5   4   3     2   1   0
        // xxxx xx    x   x   x   x   x     x   x   x   x   x     x   x   x        x   x   x   x   x     x   x   x   x   x     x   x   x
        //            Scaled shape MSBs     Shape MSBs            Counter MSBs     Scaled shape bits     Shape bits            Counter bits
        ParticleAttribute<int> bitStorage;

        // Bit index constants
        private const int bit_counter = 0;
        private const int bit_shape = 3;
        private const int bit_shapeS = 8;
        private const int msb_counter = 13;
        private const int msb_shape = 16;
        private const int msb_shapeS = 21;

        SubBinOps binops;
        SubLongestLines ll;
        SubPASC2 pasc1;

        // Static data set by the generation method
        public static Shape shape;
        public static ShapeType shapeType;
        /// <summary>
        /// Shape parameters as binary strings in the order a, d, c, a' = a + c, b.
        /// <para>
        /// The shape type determines which parameters will be used:
        /// <list type="bullet">
        /// <item>
        ///     <b>Triangle:</b> Only a is used
        /// </item>
        /// <item>
        ///     <b>Parallelogram and trapezoid:</b> Only a (width) and d (height) are used
        /// </item>
        /// <item>
        ///     <b>Pentagon:</b> a, d, c and a' are used
        /// </item>
        /// <item>
        ///     <b>Hexagon:</b> All parameters are used. a and d describe the first trapezoid
        ///     while a and b are used for the other trapezoid or a, b, d and a' for the pentagon
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public static string[] shapeParams = new string[5];

        public SCConvexShapesParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            rotation = CreateAttributeInt("Rotation", 0);
            marker = CreateAttributeBool("Marker", false);
            bitStorage = CreateAttributeInt("Bits", 0);

            binops = new SubBinOps(p);
            pasc1 = new SubPASC2(p);
            ll = new SubLongestLines(p, pasc1);

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
        //public override bool IsFinished()
        //{
        //    // Return true when this particle has terminated
        //    return false;
        //}

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
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
                                pc.SetToGlobal(0);
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
                    {
                        if (rotation > NumShapeParams() * 2)
                        {
                            rotation.SetValue(0);
                            PinConfiguration pc = GetContractedPinConfiguration();
                            pc.SetToGlobal(0);
                            SetPlannedPinConfiguration(pc);
                            pc.SendBeepOnPartitionSet(0);
                            round.SetValue(5);
                        }
                        else
                        {
                            // Initialize and start binop to compute R * parameter or MSB
                            InitBinOpsShapeParam(false);
                            round.SetValue(round + 1);
                        }
                    }
                    break;
                case 4:
                    {
                        // Run binops and write result bits/MSBs
                        if (RunBinOpsShapeParam())
                        {
                            // Increment counter and repeat
                            rotation.SetValue(rotation + 1);
                            round.SetValue(3);
                        }
                    }
                    break;
                case 5: // WAIT round
                    {
                        // Wait for beep on global circuit
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            round.SetValue(6);
                        }
                    }
                    break;

                // CHECK LARGEST SCALE

                case 6:
                    {
                        if (rotation > 5)
                        {
                            // Continue with next step
                            rotation.SetValue(0);
                            round.SetValue(8);
                        }
                        else
                        {
                            // Check next rotation
                            // Init containment check
                            // TODO.....
                        }
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
            if (shapeType < ShapeType.TRAPEZOID)
                return (int)shapeType + 1;
            else if (shapeType == ShapeType.TRAPEZOID)
                return (int)shapeType;
            else
                return (int)shapeType + 2;
        }

        /// <summary>
        /// Helper to compute the maximum number of bits of any used
        /// shape parameter.
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
            return m;
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
            }
        }

        /// <summary>
        /// Helper starting binary operations on shape parameters.
        /// Sets up the binops utils on the counters and starts either
        /// a MULT or an MSB operation. The current rotation counter
        /// indicates the index of the shape parameter and whether
        /// a multiplication or MSB detection is required.
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
            int idx = rotation.GetCurrentValue();
            int n = NumShapeParams();
            int i = idx % n;
            if (idx < n)
            {
                // Multiplication
                binops.Init(SubBinOps.Mode.MULT, ShapeBit(i), pred ? opp : Direction.NONE, succ ? d : Direction.NONE, useM ? Bit_M : Bit_R, ShapeMSB(i));
            }
            else
            {
                // MSB
                binops.Init(SubBinOps.Mode.MSB, ScaledShapeBit(i), pred ? opp : Direction.NONE, succ ? d : Direction.NONE);
            }
            PinConfiguration pc = GetContractedPinConfiguration();
            binops.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);
            binops.ActivateSend();
        }

        /// <summary>
        /// Helper running the binary operations on shape parameters.
        /// Activates the binops routine and writes the multiplication
        /// or MSB result when it is finished. Sets up the pin configuration
        /// and sends beep if it is not finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the binops routine is finished.</returns>
        private bool RunBinOpsShapeParam()
        {
            if (!ll.IsOnMaxLine())
                return false;

            binops.ActivateReceive();
            if (binops.IsFinished())
            {
                int idx = rotation.GetCurrentValue();
                int n = NumShapeParams();
                int i = idx % n;
                if (idx < n)
                {
                    // Multiplication
                    SetScaledShapeBit(i, binops.ResultBit());
                }
                else
                {
                    // MSB
                    SetScaledShapeMSB(i, binops.IsMSB());
                }

                return true;
            }

            PinConfiguration pc = GetContractedPinConfiguration();
            binops.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);
            binops.ActivateSend();

            return false;
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
        public void Generate(SCConvexShapesParticle.ShapeType shapeType = SCConvexShapesParticle.ShapeType.PARALLELOGRAM, int a = 3, int d = 2, int numAmoebots = 150, float holeProb = 0.25f, bool fillHoles = false, bool prioritizeInner = false, float lambda = 0.1f)
        {
            SCConvexShapesParticle.shapeType = shapeType;
            string str_a = IntToBinary(a);
            string str_d = IntToBinary(d);
            SCConvexShapesParticle.shapeParams[0] = str_a;
            SCConvexShapesParticle.shapeParams[1] = str_d;

            foreach (Vector2Int pos in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles, null, true, prioritizeInner, lambda))
            {
                AddParticle(pos);
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

} // namespace AS2.Algos.SCConvexShapes
