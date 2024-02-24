using System.Collections.Generic;
using UnityEngine;
using static AS2.Constants;
using AS2.Sim;
using AS2.UI;
using AS2.ShapeContainment;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.BinStateHelpers;
using AS2.Subroutines.LeaderElectionSC;
using AS2.Subroutines.LongestLines;
using AS2.Subroutines.ConvexShapeContainment;
using AS2.Subroutines.PASC;
using AS2.Subroutines.ShapeConstruction;
using AS2.Subroutines.SnowflakeContainment;


namespace AS2.Algos.SCSnowflakes
{
    /// <summary>
    /// Shape containment solution for snowflake shapes.
    /// <para>
    /// <b>Disclaimer: The save/load feature does not work for
    /// this algorithm because it stores the target shape in a
    /// static member. Always generate this algorithm from
    /// Init Mode.</b>
    /// </para>
    /// <para>
    /// If the amoebot system is so small that the longest lines cannot
    /// store the scaled arm lengths, the algorithm will not work
    /// correctly. A rough estimate of sufficient size is that the longest
    /// line should have length at least log(its own length) +
    /// log(longest arm length).
    /// </para>
    /// <para>
    /// This algorithm uses either a binary or a linear search,
    /// depending on whether the input shape is marked as being star
    /// convex (this is not checked unless the shape has no faces, in
    /// which case it is trivially star convex).
    /// </para>
    /// </summary>

    // Algorithm plan:
    //  ((a) means our input shape is star convex, (b) means it is not)
    //  1. Find all longest lines in the system
    //      - Use the resulting lines as counters
    //      - Use the length as upper bound R for the scale
    //  2. Setup counters and limit R further
    //      - Let s be the longest line in the target shape (binary string is available in static data)
    //      - Move a marker along each counter, storing the counter indices for the arm lengths and writing the bits and MSB of s
    //      - Compare R to s
    //          - If s > R, terminate immediately
    //      - Compute R' := R / s
    //      - Determine MSB of R'
    //  3. Binary search (always do this)
    //      3.1. Check largest scale R'
    //          - (a) Run containment check for the input shape at scale R'
    //              - If successful: Go to final shape construction with this scale
    //          - (b) Run containment check for the triangle with scale R' (two rotations!)
    //              - If successful: Go to next phase with R' as start scale
    //      3.2. Check smallest scale 1
    //          - (a) Run containment check for the input shape at scale 1
    //              - If not successful: Terminate with failure
    //          - (b) Run containment check for the triangle with scale 1 (two rotations!)
    //              - If not successful: Terminate with failure
    //      3.3. Binary search between L and R
    //          3.3.1. Compute next scale and check termination condition
    //              - Compute M := (L + R) / 2
    //              - If M = L:
    //                  - (a) Go to final shape construction with scale M
    //                  - (b) Go to linear search with start scale M
    //          3.3.2. Containment check for scale M
    //              - (a) Run containment check for input shape at scale M
    //              - (b) Run containment check for the triangle with scale M, using at most two rotations
    //                  - Keep track of which rotations last matched
    //              - If successful:
    //                  - (a) Store valid placements and rotations
    //                  - (b) If one rotation matched and the other did not, only try the matching rotation next time
    //                  - Set L := M
    //              - If not successful:
    //                  - Set R := M
    //  4. Linear search (only case b)
    //      - Run containment check for the input shape at scale M
    //      - If successful: Store valid placements and go to shape construction with scale M
    //      - If not successful:
    //          - Compute M <- M - 1 and find MSB
    //          - Compare the current scale M to 0
    //              - If 0: Terminate with failure
    //  5. Shape construction
    //      - Find the lowest rotation for which the shape fits (should already be available)
    //      - Run leader election among candidates with this rotation
    //      - Run shape construction subroutine for this candidate at current scale M


    // Round plan:

    // 1. Find longest lines

    // Round 0:
    //  - Setup longest lines subroutine
    //  - Start sending beeps

    // Round 1:
    //  - Activate longest lines subroutine
    //  - If subroutine is finished:
    //      - Setup left and right side of the binary search
    //          - Left := 1, Right := length of longest line
    //      - Set counter to 0
    //      - Place marker at counter start(s)
    //      - Go to round 2
    //  - Else:
    //      - Continue sending


    // 2. Setup counters and limit scale further

    // Round 2:
    //  - If counter >= length of longest binary parameter (longest line):
    //      - Set counter to 0
    //      - Reset marker
    //      - SPLIT:
    //          - Counters go to round 3
    //          - Others go to round 5 and setup 4 global circuits
    //  - Else:
    //      - Marker writes counter index if the counter is at most the maximum arm (binary) length and writes the bit/MSB of s
    //      - Marker moves one position ahead
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
    //          - (b) Set both triangle rotation flags to true
    //          - (b) Set counter to 0
    //          - Go to round 6
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


    // 3. Binary search
    // 3.1. Check largest scale R

    // Rounds 6-7 and 8-11 run the entire containment check procedure for cases (a) and (b)

    // Round 6 (Start of (a) containment check):
    //  - Init snowflake containment check subroutine
    //      - Scale R, 1 or M
    //  - Start running
    //  - Go to round 7

    // Round 7:
    //  - Run snowflake check procedure
    //  - If finished:
    //      - If successful:
    //          - Store valid rotations and placements
    //          - Go to final shape construction with scale M := R (round 36)
    //          - Or continue with next phase
    //      - Else:
    //          - Continue with next phase (R)
    //          - Terminate with failure (1)
    //  - Else:
    //      - Continue running

    // Round 8 (Start of (b) containment check):
    //  - If rotation 0 is still viable:
    //      - Init triangle containment check subroutine
    //          - Scale R, 1 or M
    //      - Start running
    //      - Go to round 9

    // Round 9:
    //  - Run triangle containment check routine
    //  - If finished:
    //      - Store success/failure
    //      - Go to round 10

    // Rounds 10/11:
    //  - Same as rounds 8/9
    //  - But use rotation 1 instead
    //  - When finished:
    //      - If one rotation is successful and the other is not:
    //          - Mark the unsuccessful rotation as not viable
    //      - Return success if at least one rotation was successful


    // 3.2 Check scale 1

    // Rounds 12-17:
    //  - Same as rounds 6-11
    //  - But use scale 1 instead
    //  - If the check is not successful:
    //      - Terminate with failure in both cases


    // 3.3 Binary search procedure

    // Round 18:
    //  - Init and start binop to compute M := L + R
    //  - Split:
    //      - Non-counter amoebots go to round 5 with 4 global circuits (similar to round 5)

    // Round 19:
    //  - Run binop
    //  - If finished:
    //      - Go to round 20

    // Round 20:
    //  - Shift each bit of M one position to the left
    //  - Init and start binop for finding MSB of M

    // Round 21:
    //  - Run binop
    //  - If finished:
    //      - Go to round 22

    // Round 22:
    //  - Init and start binop for comparing L to M

    // Round 23:
    //  - Run binop
    //  - If finished:
    //      - If L == M:
    //          - Setup 4 global circuits, beep on second, go to round 5
    //      - Else:
    //          - Setup 4 global circuits, beep on third, go to round 5

    // Rounds 24-29:
    //  - Same as rounds 6-11 / 12-17
    //  - Use scale M
    //  - When finished:
    //      - Success:
    //          - Set L := M
    //          - (a) Store valid rotations and placements of the shape
    //          - (b) If one rotation was not valid: Mark it as not viable
    //      - Failure:
    //          - Set R := M
    //      - Go back to round 18


    // 4. Linear search

    // Rounds 30-31:
    //  - Same as rounds 6-7
    //  - Run containment check for shape at scale M
    //  - If successful:
    //      - Store valid rotations and placements
    //      - Go to shape construction phase with scale M (round 36)
    //  - Else:
    //      - Go to round 32

    // Round 32:
    //  - Start binop to compute M -> M - 1
    //  - Split:
    //      - Non-counter amoebots go to round 5
    //  - Go to next round

    // Round 33:
    //  - Run binop
    //  - If finished:
    //      - Go to next round

    // Round 34:
    // - Start binop to compute MSB of M

    // Round 35:
    //  - Run binop
    //  - If finished:
    //      - Setup 4 global circuits and go to round 5
    //      - MSB of M sends beep on fourth global circuit if its stored bit of M is 1
    //      - Otherwise send beep on all four global circuits


    // 5. Shape construction

    // Round 36:
    //  - Find lowest rotation OR just use all candidates
    //  - Setup leader election on valid placements
    //      - But use entire system

    // Round 37:
    //  - Run leader election
    //  - If leader election is finished:
    //      - Setup shape construction subroutine for leader and scale M
    //      - Either use the smallest rotation OR let the leader choose a random rotation
    //      - Place markers at counter starts

    // Round 38:
    //  - Continue running shape construction
    //  - If shape construction is finished:
    //      - Terminate with success
    //  - If scale reset:
    //      - Set marker to counter start
    //      - Go to round 39
    //  - Else:
    //      - Continue running (maybe with scale bit)
    //      - Forward marker

    // Round 39:
    //  - Send next bit of shape construction
    //  - Forward marker
    //  - Go back to round 38

    public class SCSnowflakesParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "SC Snowflakes";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SCSnowflakesInitializer).FullName;

        [StatusInfo("Display Shape", "Displays the target shape at the selecetd location")]
        public static void ShowShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            LineDrawer.Instance.Clear();
            snowflake.Draw(selectedParticle is null ? Vector2Int.zero : selectedParticle.Head(), 0, 1);
            LineDrawer.Instance.SetTimer(20);
        }

        //            24               23 19           18  13             12   7            6          543        210
        // xxxx xxx   x                xxxxx           xxxxxx             xxxxxx            x          xxx        xxx
        //            On arm counter   Counter index   Valid placements   Valid rotations   Finished   Bits LMR   MSBs LMR
        ParticleAttribute<int> state1;

        //                     17                    16 15                14       13     6   5    0
        // xxxx xxxx xxxx xx   x                     x  x                 x        xxxxxxxx   xxxxxx
        //                     Last triangle valid   Rotation valid 1,0   Marker   Counter    Round
        ParticleAttribute<int> state2;

        BinAttributeBitField lmrMSB;                // MSB flags for the values L, M, R
        BinAttributeBitField lmrBit;                // Bit flags for the values L, M, R
        private const int L = 0;
        private const int M = 1;
        private const int R = 2;
        BinAttributeBool finished;                  // Whether we are finished
        BinAttributeBitField validRotations;        // 6 flags for the existence of valid placements at the 6 rotations
        BinAttributeBitField validPlacements;       // 6 valid placement flags for this amoebot
        BinAttributeInt counterIndex;               // The index of this amoebot on the arm counter
        BinAttributeBool onArmCounter;              // Whether we are part of the arm counter or not

        BinAttributeInt round;                      // The round counter
        BinAttributeInt counter;                    // A generic counter
        BinAttributeBool marker;                    // Marker for the binary counters
        BinAttributeBitField rotationValid;         // Two bits storing which rotations of the triangle are still viable
        BinAttributeBool lastTriangleValid;         // Flag indicating whether the last triangle check was successful

        public static Shape snowflake;
        public static SnowflakeInfo snowflakeInfo;
        public static bool shapeIsStarConvex;       // Whether our input shape is star convex (can use binary search!)
        public static string longestLineStr;        // String representation of the length of the longest line in the target shape

        SubBinOps binops;
        SubPASC2[] pasc = new SubPASC2[3];  // Shared PASC instances
        SubSnowflakeContainment snowflakeCheck;
        SubMergingAlgo triangleCheck;
        SubLeaderElectionSC leaderElection;
        SubLongestLines ll;
        SubShapeConstruction shapeConstr;

        public SCSnowflakesParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            state1 = CreateAttributeInt("State 1", 0);
            state2 = CreateAttributeInt("State 2", 0);

            lmrMSB = new BinAttributeBitField(state1, 0, 3);
            lmrBit = new BinAttributeBitField(state1, 3, 3);
            finished = new BinAttributeBool(state1, 6);
            validRotations = new BinAttributeBitField(state1, 7, 6);
            validPlacements = new BinAttributeBitField(state1, 13, 6);
            counterIndex = new BinAttributeInt(state1, 19, 5);
            onArmCounter = new BinAttributeBool(state1, 24);

            round = new BinAttributeInt(state2, 0, 6);
            counter = new BinAttributeInt(state2, 6, 8);
            marker = new BinAttributeBool(state2, 14);
            rotationValid = new BinAttributeBitField(state2, 15, 2);
            lastTriangleValid = new BinAttributeBool(state2, 17);

            // Subroutines
            for (int i = 0; i < 3; i++)
                pasc[i] = new SubPASC2(p);
            ll = new SubLongestLines(p, pasc[0], pasc[1], pasc[2]);
            leaderElection = new SubLeaderElectionSC(p);
            binops = new SubBinOps(p);
            snowflakeCheck = new SubSnowflakeContainment(p, snowflakeInfo, binops, pasc);
            triangleCheck = new SubMergingAlgo(p, pasc[0], pasc[1]);
            shapeConstr = new SubShapeConstruction(p, snowflake, pasc[0]);

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
            return finished.GetValue();
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            int r = round.GetValue();
            switch (r)
            {
                // 1. Find longest lines

                case 0:
                    {
                        // Start longest lines subroutine
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
                        ll.ActivateReceive();
                        if (ll.IsFinished())
                        {
                            // Set Left and Right bits according to longest line
                            // Also set marker to counter start(s)
                            if (ll.IsOnMaxLine())
                            {
                                if (ll.IsMSB())
                                    lmrMSB.SetValue(R, true);
                                if (ll.GetBit())
                                    lmrBit.SetValue(R, true);
                                if (!HasNeighborAt(ll.GetMaxDir().Opposite()))
                                {
                                    lmrMSB.SetValue(L, true);
                                    lmrBit.SetValue(L, true);
                                    marker.SetValue(true);
                                }
                            }
                            // Set counter to 0 and go to next phase
                            counter.SetValue(0);
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


                // 2. Setup counters and limit scale further

                case 2:
                    {
                        int ctr = counter.GetValue();
                        if (ctr >= longestLineStr.Length)
                        {
                            // Finished setting up the marker
                            counter.SetValue(0);
                            marker.SetValue(false);
                            // Counters go to round 3, others go to round 5 with 4 global circuits
                            if (ll.IsOnMaxLine())
                            {
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc, 4);
                                SetPlannedPinConfiguration(pc);
                                round.SetValue(5);
                            }
                        }
                        else
                        {
                            // Marker writes counter index and bit/MSB of longest line
                            if (marker.GetValue())
                            {
                                if (ctr < snowflakeInfo.longestParameter)
                                {
                                    counterIndex.SetValue(ctr);
                                    onArmCounter.SetValue(true);
                                }
                                // Store bit/MSB of s in M
                                lmrBit.SetValue(M, longestLineStr[ctr] == '1');
                                lmrMSB.SetValue(M, ctr == longestLineStr.Length - 1);
                            }
                            // Marker moves one position ahead
                            if (ll.IsOnMaxLine())
                            {
                                Direction pred = ll.GetMaxDir().Opposite();
                                marker.SetValue(HasNeighborAt(pred) && ((SCSnowflakesParticle)GetNeighborAt(pred)).marker.GetValue());
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
                            SetupGlobalCircuits(pc, 4);
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
                                binops.Init(SubBinOps.Mode.COMP, lmrBit.GetValue(M), pred, succ, lmrBit.GetValue(R));
                            }
                            else if (ctr == 1)
                            {
                                // Compute R / M
                                binops.Init(SubBinOps.Mode.DIV, lmrBit.GetValue(R), pred, succ, lmrBit.GetValue(M), lmrMSB.GetValue(R));
                            }
                            else if (ctr == 2)
                            {
                                // Find MSB of R
                                binops.Init(SubBinOps.Mode.MSB, lmrBit.GetValue(R), pred, succ);
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
                                    SetupGlobalCircuits(pc, 4);
                                    SetPlannedPinConfiguration(pc);
                                    for (int i = 0; i < 4; i++)
                                        pc.SendBeepOnPartitionSet(i);
                                    round.SetValue(5);
                                }
                            }
                            else if (ctr == 1)
                            {
                                // Store result of R / s in R
                                lmrBit.SetValue(R, binops.ResultBit());
                            }
                            else if (ctr == 2)
                            {
                                // Store MSB of R
                                lmrMSB.SetValue(R, binops.IsMSB());
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
                            if (!shapeIsStarConvex)
                            {
                                // Have to do triangle test: Set both rotation flags to true initially
                                rotationValid.SetValue(0, true);
                                rotationValid.SetValue(1, true);
                                counter.SetValue(0);
                                round.SetValue(r + 3);
                            }
                            else
                            {
                                round.SetValue(r + 1);
                            }
                        }
                        else if (beep1)
                        {
                            // Binary search finished
                            if (shapeIsStarConvex)
                                // Go to final shape construction
                                round.SetValue(36);
                            else
                                // Go to linear search
                                round.SetValue(30);
                        }
                        else if (beep2)
                        {
                            // Binary search continues
                            if (shapeIsStarConvex)
                                // Test the shape directly
                                round.SetValue(24);
                            else
                                // Test the triangle
                                round.SetValue(26);
                        }
                        else if (beep3)
                        {
                            // Linear search decrement and comparison are finished
                            round.SetValue(30);
                        }
                    }
                    break;


                // 3. Binary search

                case 6:     // Start of snowflake containment check (a)
                case 12:    // Check scale 1 (= L)
                case 24:    // Check scale M
                case 30:    // Check scale M during linear search
                    {
                        if (ll.IsOnMaxLine())
                        {
                            int scaleIdx = r == 6 ? R : (r == 12 ? L : M);
                            bool scaleBit = lmrBit.GetCurrentValue(scaleIdx);
                            bool scaleMSB = lmrMSB.GetCurrentValue(scaleIdx);
                            Direction succ = ll.GetMaxDir();
                            Direction pred = succ.Opposite();
                            succ = HasNeighborAt(succ) ? succ : Direction.NONE;
                            pred = HasNeighborAt(pred) ? pred : Direction.NONE;
                            snowflakeCheck.Init(true, onArmCounter.GetValue(), counterIndex.GetValue(), pred, succ, scaleBit, scaleMSB);
                        }
                        else
                        {
                            snowflakeCheck.Init(true);
                        }
                        PinConfiguration pc = GetContractedPinConfiguration();
                        snowflakeCheck.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        snowflakeCheck.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 7:     // Check scale R
                case 13:    // Check scale 1 (= L)
                case 25:    // Check scale M
                case 31:    // Check scale M during linear search
                    {
                        snowflakeCheck.ActivateReceive();
                        if (snowflakeCheck.IsFinished())
                        {
                            if (snowflakeCheck.Success())
                            {
                                // Store valid rotations and placements
                                for (int i = 0; i < 6; i++)
                                {
                                    validRotations.SetValue(i, false);
                                    validPlacements.SetValue(i, false);
                                }
                                int[] rotations = snowflakeCheck.ValidRotations();
                                foreach (int rot in rotations)
                                {
                                    validRotations.SetValue(rot, true);
                                    if (snowflakeCheck.IsRepresentative(rot))
                                        validPlacements.SetValue(rot, true);
                                }

                                if (r == 7)
                                {
                                    // Go to final shape construction with scale R
                                    lmrBit.SetValue(M, lmrBit.GetValue(R));
                                    lmrMSB.SetValue(M, lmrMSB.GetValue(R));
                                    round.SetValue(36);
                                }
                                else if (r == 13)
                                {
                                    // Continue to main binary search
                                    round.SetValue(18);
                                }
                                else if (r == 25)
                                {
                                    // Success: Set L := M and go to next iteration
                                    lmrBit.SetValue(L, lmrBit.GetValue(M));
                                    lmrMSB.SetValue(L, lmrMSB.GetValue(M));
                                    round.SetValue(18);
                                }
                                else if (r == 31)
                                {
                                    // Success during linear search! Start shape construction
                                    round.SetValue(36);
                                }
                            }
                            else
                            {
                                if (r == 7)
                                {
                                    // Continue with next phase
                                    round.SetValue(12);
                                }
                                else if (r == 13)
                                {
                                    // Terminate with failure
                                    SetMainColor(ColorData.Particle_Red);
                                    finished.SetValue(true);
                                }
                                else if (r == 25)
                                {
                                    // Set R := M and go to next iteration
                                    lmrBit.SetValue(R, lmrBit.GetValue(M));
                                    lmrMSB.SetValue(R, lmrMSB.GetValue(M));
                                    round.SetValue(18);
                                }
                                else if (r == 31)
                                {
                                    round.SetValue(r + 1);
                                }
                            }
                        }
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            snowflakeCheck.SetupPC(pc);
                            SetPlannedPinConfiguration(pc);
                            snowflakeCheck.ActivateSend();
                        }
                    }
                    break;
                case 8:     // Start of triangle containment check
                case 10:    // Scale R (second rotation)
                case 14:    // Scale 1
                case 16:    // Scale 1 (second rotation)
                case 26:    // Scale M
                case 28:    // Scale M (second rotation)
                    {
                        int rot = 0;
                        if (r == 10 || r == 16 || r == 28)
                            // Second case, always check second rotation
                            rot = 1;
                        else if (!rotationValid.GetCurrentValue(0))
                            // First case but first rotation is not valid
                            rot = 1;
                        
                        // Init triangle containment check
                        if (ll.IsOnMaxLine())
                        {
                            int scaleIdx = r < 14 ? R : (r < 26 ? L : M);
                            bool scaleBit = lmrBit.GetCurrentValue(scaleIdx);
                            bool scaleMSB = lmrMSB.GetCurrentValue(scaleIdx);
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
                case 9:     // Scale R
                case 11:    // Scale R (second rotation)
                case 15:    // Scale 1
                case 17:    // Scale 1 (second rotation)
                case 27:    // Scale M
                case 29:    // Scale M (second rotation)
                    {
                        triangleCheck.ActivateReceive();
                        if (triangleCheck.IsFinished())
                        {
                            bool overallSuccess = false;
                            bool haveToRunNextCheck = false;
                            if (r == 11 || r == 17 || r == 29)
                            {
                                // This was the second check
                                bool success1 = lastTriangleValid.GetCurrentValue();
                                bool success2 = triangleCheck.Success();
                                if (success1 && success2)
                                {
                                    // Both were successful
                                    overallSuccess = true;
                                }
                                else if (!success1 && !success2)
                                {
                                    // Both failed: Overall failure
                                    overallSuccess = false;
                                }
                                else
                                {
                                    // One was successful and the other one was not
                                    if (!success1)
                                        rotationValid.SetValue(0, false);
                                    else
                                        rotationValid.SetValue(1, false);
                                    // But overall success
                                    overallSuccess = true;
                                }
                            }
                            else
                            {
                                if (!rotationValid.GetCurrentValue(0) || !rotationValid.GetCurrentValue(1))
                                {
                                    // Only one rotation is valid and this was the check for it
                                    if (triangleCheck.Success())
                                    {
                                        // Single check was successful
                                        overallSuccess = true;
                                    }
                                    else
                                    {
                                        // Last check was not successful
                                        overallSuccess = false;
                                    }
                                }
                                else
                                {
                                    // Both rotations are valid: Continue with the second one
                                    lastTriangleValid.SetValue(triangleCheck.Success());
                                    haveToRunNextCheck = true;
                                    round.SetValue(r + 1);
                                }
                            }

                            if (!haveToRunNextCheck)
                            {
                                // This was the last rotation check, we already know the result
                                if (overallSuccess)
                                {
                                    if (r < 15)
                                    {
                                        // Scale R was successful: Remember scale and go to linear search
                                        lmrBit.SetValue(M, lmrBit.GetValue(R));
                                        lmrMSB.SetValue(M, lmrMSB.GetValue(R));
                                        round.SetValue(30);
                                    }
                                    else if (r < 27)
                                    {
                                        // Scale 1 was successful: Continue with binary search
                                        round.SetValue(18);
                                    }
                                    else
                                    {
                                        // Set L := M and continue with next iteration
                                        lmrBit.SetValue(L, lmrBit.GetValue(M));
                                        lmrMSB.SetValue(L, lmrMSB.GetValue(M));
                                        round.SetValue(18);
                                    }
                                }
                                else
                                {
                                    if (r < 15)
                                    {
                                        // Scale R was not successful: Go to next check
                                        round.SetValue(14);
                                    }
                                    else if (r < 27)
                                    {
                                        // Scale 1 was not successful: Terminate with failure
                                        finished.SetValue(true);
                                        SetMainColor(ColorData.Particle_Red);
                                    }
                                    else
                                    {
                                        // Set R := M and continue with next iteration
                                        lmrBit.SetValue(R, lmrBit.GetValue(M));
                                        lmrMSB.SetValue(R, lmrMSB.GetValue(M));
                                        round.SetValue(18);
                                    }
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


                // 3.3. Binary search between L and R

                case 18:    // Compute L + R
                case 20:    // Shift bits of M and start binop for finding MSB of M
                case 22:    // Compare L to M
                case 32:    // Compute M := M - 1 (linear search)
                case 34:    // Find MSB of M
                    {
                        // Init and start binop
                        // Also, split counter and non-counter amoebots
                        if (ll.IsOnMaxLine())
                        {
                            // Initialize binary operation
                            Direction succ = ll.GetMaxDir();
                            Direction pred = succ.Opposite();
                            succ = HasNeighborAt(succ) ? succ : Direction.NONE;
                            pred = HasNeighborAt(pred) ? pred : Direction.NONE;
                            if (r == 18)
                            {
                                // Compute M := L + R
                                binops.Init(SubBinOps.Mode.ADD, lmrBit.GetValue(L), pred, succ, lmrBit.GetValue(R));
                            }
                            else if (r == 20)
                            {
                                // Shift bits of M
                                lmrBit.SetValue(M, succ != Direction.NONE && ((SCSnowflakesParticle)GetNeighborAt(succ)).lmrBit.GetValue(M));
                                // Find MSB of M
                                binops.Init(SubBinOps.Mode.MSB, lmrBit.GetCurrentValue(M), pred, succ);
                            }
                            else if (r == 22)
                            {
                                // Compare L to M
                                binops.Init(SubBinOps.Mode.COMP, lmrBit.GetValue(L), pred, succ, lmrBit.GetValue(M));
                            }
                            else if (r == 32)
                            {
                                // Compute M - 1
                                binops.Init(SubBinOps.Mode.SUB, lmrBit.GetValue(M), pred, succ, pred == Direction.NONE);
                            }
                            else if (r == 34)
                            {
                                // Find MSB of M
                                binops.Init(SubBinOps.Mode.MSB, lmrBit.GetCurrentValue(M), pred, succ);
                            }
                            PinConfiguration pc = GetContractedPinConfiguration();
                            binops.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binops.ActivateSend();
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            // Non-counter amoebots go to round 5 and wait
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupGlobalCircuits(pc, 4);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(5);
                        }
                    }
                    break;
                case 19:    // Compute M := L + R
                case 21:    // Find MSB of M
                case 23:    // Compare L to M
                case 33:    // Compute M := M - 1
                case 35:    // Find MSB of M
                    {
                        binops.ActivateReceive();
                        if (binops.IsFinished())
                        {
                            if (r == 19)
                            {
                                // Computed L + R, store in M
                                lmrBit.SetValue(M, binops.ResultBit());
                                round.SetValue(r + 1);
                            }
                            else if (r == 21)
                            {
                                // Found MSB of M
                                lmrMSB.SetValue(M, binops.IsMSB());
                                round.SetValue(r + 1);
                            }
                            else if (r == 23)
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc, 4);
                                SetPlannedPinConfiguration(pc);
                                // If L = M: Binary search is finished
                                if (binops.CompResult() != SubComparison.ComparisonResult.LESS)
                                {
                                    pc.SendBeepOnPartitionSet(1);
                                }
                                // Else: Continue binary search
                                else
                                {
                                    pc.SendBeepOnPartitionSet(2);
                                }
                                round.SetValue(5);
                            }
                            else if (r == 33)
                            {
                                // Assign M <- M - 1
                                lmrBit.SetValue(M, binops.ResultBit());
                                round.SetValue(r + 1);
                            }
                            else if (r == 35)
                            {
                                lmrMSB.SetValue(M, binops.IsMSB());
                                // Setup global circuits and beep
                                PinConfiguration pc = GetContractedPinConfiguration();
                                SetupGlobalCircuits(pc, 4);
                                SetPlannedPinConfiguration(pc);
                                // If the MSB's stored bit is 0, we failed and send beeps on all 4 circuits
                                // Else, we send only on the fourth
                                pc.SendBeepOnPartitionSet(3);
                                if (lmrMSB.GetCurrentValue(M) && !lmrBit.GetCurrentValue(M))
                                {
                                    for (int i = 0; i < 3; i++)
                                        pc.SendBeepOnPartitionSet(i);
                                }
                                round.SetValue(5);
                            }
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


                // 4. Linear search
                // Rounds 30-35 are entirely handled by similar rounds


                // 5. Shape construction

                case 36:
                    {
                        // Setup leader election on valid placements
                        // Use the smallest valid rotation
                        bool candidate = false;
                        int rot = -1;
                        for (int i = 0; i < 6; i++)
                        {
                            if (validRotations.GetValue(i))
                            {
                                rot = i;
                                break;
                            }
                        }
                        candidate = validPlacements.GetValue(rot);
                        leaderElection.Init(candidate, true);
                        PinConfiguration pc = GetContractedPinConfiguration();
                        leaderElection.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        leaderElection.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 37:
                    {
                        leaderElection.ActivateReceive();
                        if (leaderElection.IsFinished())
                        {
                            // Setup shape construction
                            marker.SetValue(ll.IsOnMaxLine() && !HasNeighborAt(ll.GetMaxDir().Opposite()));

                            int rot = -1;
                            for (int i = 0; i < 6; i++)
                            {
                                if (validRotations.GetValue(i))
                                {
                                    rot = i;
                                    break;
                                }
                            }

                            shapeConstr.Init(leaderElection.IsLeader(), rot);
                            ActivateShapeConstrSend(false);
                            round.SetValue(r + 1);
                        }
                        else
                        {
                            SetPlannedPinConfiguration(GetCurrentPinConfiguration());
                            leaderElection.ActivateSend();
                        }
                    }
                    break;
                case 38:
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
                                round.SetValue(r + 1);
                            }
                            else
                            {
                                ActivateShapeConstrSend();
                            }
                        }
                    }
                    break;
                case 39:
                    {
                        ActivateShapeConstrSend();
                        round.SetValue(r - 1);
                    }
                    break;
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
            shapeConstr.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);

            if (checkNeedScaleBit && shapeConstr.NeedScaleBit())
            {
                shapeConstr.ActivateSend(marker.GetCurrentValue() && lmrBit.GetValue(M), marker.GetCurrentValue() && lmrMSB.GetValue(M));
                if (ll.IsOnMaxLine())
                {
                    Direction d = ll.GetMaxDir().Opposite();
                    marker.SetValue(HasNeighborAt(d) && ((SCSnowflakesParticle)GetNeighborAt(d)).marker.GetValue());
                }
            }
            else
            {
                shapeConstr.ActivateSend();
            }
        }

        /// <summary>
        /// Sets up global circuits on partition sets 0, 1....
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="numCircuits">The number of global circuits
        /// to establish.</param>
        private void SetupGlobalCircuits(PinConfiguration pc, int numCircuits)
        {
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            for (int i = 0; i < numCircuits; i++)
                pc.SetStarConfig(i, inverted, i);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SCSnowflakesInitializer : InitializationMethod
    {
        public SCSnowflakesInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(string shape = "snowflake.json", bool fromFile = true, bool isStarConvex = false, int numAmoebots = 250, float holeProb = 0.3f, bool fillHoles = false)
        {
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
                SCSnowflakesParticle.snowflake = s;
            }

            // A snowflake without faces is always star convex
            if (s.faces.Count == 0)
                isStarConvex = true;

            // Compute snowflake data
            SnowflakeInfo snowflakeInfo = new SnowflakeInfo();
            // Find all occurring arm lengths and sort them in ascending order
            List<int> armLengths = new List<int>();
            for (int i = 0; i < sc.dependencyTree.Length; i++)
            {
                foreach (int l in sc.dependencyTree[i].arms)
                {
                    if (l > 0 && !armLengths.Contains(l))
                        armLengths.Add(l);
                }
            }
            armLengths.Sort();

            // Replace all actual arm lengths with indices in the list
            for (int i = 0; i < sc.dependencyTree.Length; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    int l = sc.dependencyTree[i].arms[j];
                    if (l == 0)
                        sc.dependencyTree[i].arms[j] = -1;
                    else
                        sc.dependencyTree[i].arms[j] = armLengths.FindIndex(a => a == l);
                }
            }

            // Compute string representations of all line lengths
            string[] armLengthsStr = new string[armLengths.Count];
            for (int i = 0; i < armLengths.Count; i++)
            {
                armLengthsStr[i] = IntToBinary(armLengths[i]);
            }

            // Find the longest parameter string and store all data in the snowflake info container
            snowflakeInfo.longestParameter = armLengths.Count > 0 ? armLengthsStr[armLengths.Count - 1].Length : 0;
            snowflakeInfo.armLengths = armLengths.ToArray();
            snowflakeInfo.armLengthsStr = armLengthsStr;
            snowflakeInfo.nodes = sc.dependencyTree;
            SCSnowflakesParticle.snowflakeInfo = snowflakeInfo;
            SCSnowflakesParticle.shapeIsStarConvex = isStarConvex;
            SCSnowflakesParticle.longestLineStr = IntToBinary(s.GetLongestLineLength());

            // Place amoebot system
            foreach (Vector2Int v in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles))
            {
                AddParticle(v);
            }

            // Draw shape preview
            LineDrawer.Instance.Clear();
            s.Draw(Vector2Int.zero, 0, 1);
            LineDrawer.Instance.SetTimer(20);
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

} // namespace AS2.Algos.SCSnowflakes
