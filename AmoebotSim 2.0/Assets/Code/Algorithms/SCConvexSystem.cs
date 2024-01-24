using AS2.Sim;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.PASC;
using AS2.ShapeContainment;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.SCConvexSystem
{

    /// <summary>
    /// Shape containment solution for the case of convex amoebot systems.
    /// Supports convex amoebot systems with at least 3 corners (the only
    /// other convex shapes being lines and points).
    /// </summary>
    
    // PHASE 1: ESTABLISH SHAPE

    // Round 0:
    //  Send:
    //  - Establish global circuit
    //  - Amoebots categorize themselves as inner, side or corner (acute or obtuse)
    //  - Amoebots failing to categorize themselves send beep

    // Round 1:
    //  Receive:
    //  - If beep is received on global circuit, terminate with failure
    //  Send:
    //  - Establish side circuits
    //  - Corners send beep to both neighbors if they are acute
    
    // Round 2:
    //  Receive:
    //  - Corners receive first beeps from neighbors, now know whether they are acute or obtuse
    //  Send:
    //  - Keep side circuits
    //  - Corners forward the beeps they received from their neighbors

    // Round 3:
    //  Receive:
    //  - Corners receive additional information on the other corners
    //  - Now know about 4 other corners, enough to deduce what type of shape we have
    //  - Initialize counter on outer boundary
    //      - Leader is top left amoebot (side f, left end)
    //      - Direction is clockwise
    //      - Nothing to do actually


    // PHASE 2: COMPUTE SIDE LENGTHS

    // Round 4:
    //  - Initialize side length computation procedure
    //      - Marker on first bit of counter
    //  Send:
    //  - Setup PASC circuit on current side
    //  - Setup two more global circuits
    //  - Send first PASC beep
    //  - Go to round 5

    // Round 5:
    //  Receive:
    //  - End of current side receives PASC beep
    //  - Some amoebot on the PASC chain may have become passive
    //  Send:
    //  - Send PASC bit on first global circuit
    //  - Send continuation beep on second global circuit if we have become passive
    //  - Send next PASC beep already
    //  - Go to round 6

    // Round 6:
    //  Receive:
    //  - End of current side receives new PASC beep
    //  - Some amoebot on PASC chain may have become passive
    //  - Marked counter amoebot receives previous PASC beep
    //  - Whole system also receives continuation beep
    //      - If received: Marker moves to successor
    //      - If not received:
    //          - If this was not the last side: Increment side counter and go back to round 4
    //          - If this was the last side: Continue with round 7
    //  Send:
    //  - (Only get here if continuation beep was received)
    //  - Send new PASC bit on first global circuit
    //  - Send continuation beep on global circuit if we have become passive
    //  - Send next PASC beep
    //  - Stay in round 6


    // PHASE 3: INEQUALITIES

    // Round 7:
    //  - Start with some counter being 0
    //  - Boundary amoebots setup binary operation for addition
    //      - Order of computations:
    //          1. R1 = a + b
    //          2. R2 = a + c
    //          3. R3 = b + d
    //          4. R4 = a + b + c = R1 + c
    //          5. R5 = a + b + d = R1 + d
    //  Send:
    //  - Setup binary operation circuit
    //  - Send binOp beep
    //  - Counter amoebots go to round 8
    //  - Inner amoebots setup global circuit and go to round 42 to wait

    // Round 8:
    //  - Continue running binary operation
    //  - If finished:
    //      - Increase counter if limit is not reached
    //          - Then return to round 7
    //      - If limit is reached:
    //          - Reset counter
    //          - Go to round 9

    // Round 9:
    //  - Setup binary operation for MSB detection
    //      - Find MSBs of R1, ..., R5
    //  Send:
    //  - Setup binary operation circuit and send beep
    //  - Go to round 10

    // Round 10:
    //  - Continue running binary operation
    //  - If finished:
    //      - Record MSB
    //      - Increase counter and go back to round 9 if counter is < 4
    //      - Otherwise:
    //          - Reset counter
    //          - Go to round 11

    // Round 11:
    //  - Set K2 := 1 on the boundary
    //  - Reset counter 2
    //  - Reset all Li := 0
    //  - Set marker on counter start
    //  - Marker writes down first bit of all Li
    //  - Increment counter 2
    //  - Go to round 12

    // Round 12:
    //  - If counter 2 has reached end of Lis:
    //      - Reset counter 2
    //      - Go to round 13
    //  - Otherwise:
    //      - Move marker ahead
    //      - Write bits of Li
    //      - Increment counter 2
    //      - If the marker reaches the counter start again: Log error because of too large input shape (could terminate with failure etc.)

    // Round 13:
    //  - Counter 2 indicates which Li we are looking at
    //  - If Li = 0:
    //      - Increment counter until it is not 0 or we have reached the end
    //  - If counter has reached end:
    //      - Go to round 16
    //  - Setup binary operation to compute Ri / Li
    //  - Start binary ops
    //  - Go to round 14

    // Round 14:
    //  - Continue binary ops
    //  - If finished:
    //      - Set K3 := Ri / Li
    //      - Setup binary operation to compare K3 to K2
    //      - Start binary ops
    //      - Go to round 15

    // Round 15:
    //  - Continue binary ops
    //  - If finished:
    //      - If K3 < K2: Set K2 := K3
    //      - Increment counter 2
    //      - Go to round 13

    // Round 16:
    //  - Newest K2 is ready
    //  - Setup binary operation to compare K to K2
    //  - Start binary ops
    //  - Go to round 17

    // Round 17:
    //  - Continue binary ops
    //  - If finished:
    //      - If K2 > K:
    //          - Set K := K2
    //          - Set rotation to counter 1
    //      - Increment counter 1
    //      - If counter 1 has not reached the end: Go to round 11
    //      - Otherwise:
    //          - Go to round 18

    // TODO





    public class SCConvexSystemParticle : ParticleAlgorithm
    {
        public enum ShapeType
        {
            TRIANGLE,
            PARALLELOGRAM,
            TRAPEZOID,
            PENTAGON,
            HEXAGON
        }

        private enum CornerType
        {
            INNER,
            SIDE,
            OBTUSE,
            ACUTE
        }


        // This is the display name of the algorithm (must be unique)
        public static new string Name => "SC Convex System";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SCConvexSystemInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<int> round;
        ParticleAttribute<Direction> predDir;
        ParticleAttribute<Direction> succDir;
        ParticleAttribute<ShapeType> shapeType;

        ParticleAttribute<bool> finished;
        ParticleAttribute<bool> success;

        ParticleAttribute<bool> succCornerAcute;
        ParticleAttribute<bool> predCornerAcute;

        ParticleAttribute<int> counter1;
        ParticleAttribute<int> counter2;
        ParticleAttribute<bool> marker;
        // Index corresponds to successor direction of the side
        ParticleAttribute<bool>[] sideLengthBits = new ParticleAttribute<bool>[6];

        ParticleAttribute<bool>[] inequalityR = new ParticleAttribute<bool>[5];
        ParticleAttribute<bool>[] inequalityMSBsR = new ParticleAttribute<bool>[5];
        ParticleAttribute<bool>[] inequalityL = new ParticleAttribute<bool>[5];

        ParticleAttribute<bool> bitK;   // Final (max) scale
        ParticleAttribute<bool> bitK2;  // Intermediate (min) scale
        ParticleAttribute<bool> bitK3;  // Temporary variable
        ParticleAttribute<int> rotation;    // The rotation of the maximum scale

        public static Shape shape;

        SubBinOps binops;
        SubPASC pasc;

        public SCConvexSystemParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            predDir = CreateAttributeDirection("Predecessor", Direction.NONE);
            succDir = CreateAttributeDirection("Successor", Direction.NONE);
            shapeType = CreateAttributeEnum<ShapeType>("Shape Type", ShapeType.HEXAGON);

            finished = CreateAttributeBool("Finished", false);
            success = CreateAttributeBool("Success", false);

            succCornerAcute = CreateAttributeBool("Succ. Corner Acute", false);
            predCornerAcute = CreateAttributeBool("Pred. Corner Acute", false);

            counter1 = CreateAttributeInt("Counter 1", 0);
            counter2 = CreateAttributeInt("Counter 2", 0);
            marker = CreateAttributeBool("Marker", false);
            for (int i = 0; i < 6; i++)
            {
                sideLengthBits[i] = CreateAttributeBool("Side Length [" + i + "]", false);
            }
            for (int i = 0; i < 5; i++)
            {
                inequalityR[i] = CreateAttributeBool("Inequality R [" + i + "]", false);
            }
            for (int i = 0; i < 5; i++)
            {
                inequalityMSBsR[i] = CreateAttributeBool("Inequality R MSB [" + i + "]", false);
            }
            for (int i = 0; i < 5; i++)
            {
                inequalityL[i] = CreateAttributeBool("Inequality L [" + i + "]", false);
            }
            bitK = CreateAttributeBool("Bit K", false);
            bitK2 = CreateAttributeBool("Bit K2", false);
            bitK3 = CreateAttributeBool("Bit K3", false);
            rotation = CreateAttributeInt("Rotation", -1);

            pasc = new SubPASC(p);
            binops = new SubBinOps(p);

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
            if (round == 0)
            {
                // Identify corner type and predecessor/successor direction
                bool validConvex = IdentifyCornerType();

                // Establish global circuit
                PinConfiguration pc = GetContractedPinConfiguration();
                pc.SetToGlobal(0);
                SetPlannedPinConfiguration(pc);

                // Send beep if we are not a valid convex shape
                if (!validConvex)
                    pc.SendBeepOnPartitionSet(0);

                round.SetValue(1);
            }
            else if (round == 1)
            {
                // Listen for beep on global circuit telling us that we are not a valid convex shape
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    finished.SetValue(true);
                    success.SetValue(false);
                    SetMainColor(ColorData.Particle_Red);
                    SetPlannedPinConfiguration(GetContractedPinConfiguration());
                    return;
                }
                CornerType t = MyCornerType();
                if (t == CornerType.ACUTE || t == CornerType.OBTUSE)
                {
                    SetMainColor(ColorData.Particle_Green);
                }
                else if (t == CornerType.SIDE)
                {
                    SetMainColor(ColorData.Particle_Blue);
                }

                // Setup the side circuits
                pc = GetContractedPinConfiguration();
                SetupSideCircuit(pc);
                SetPlannedPinConfiguration(pc);

                // Acute corners send beep to neighboring corners along the sides
                if (t == CornerType.ACUTE)
                {
                    pc.SendBeepOnPartitionSet(0);
                    pc.SendBeepOnPartitionSet(2);
                }

                round.SetValue(2);
            }
            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Corners receive first acute neighbor beeps
                CornerType t = MyCornerType();
                if (t == CornerType.ACUTE || t == CornerType.OBTUSE)
                {
                    succCornerAcute.SetValue(pc.ReceivedBeepOnPartitionSet(1));
                    predCornerAcute.SetValue(pc.ReceivedBeepOnPartitionSet(3));

                    // Forward this information
                    SetPlannedPinConfiguration(pc);
                    if (predCornerAcute.GetCurrentValue())
                        pc.SendBeepOnPartitionSet(0);
                    if (succCornerAcute.GetCurrentValue())
                        pc.SendBeepOnPartitionSet(2);
                }

                round.SetValue(3);
            }
            else if (round == 3)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Corners receive additional information on the other corners
                CornerType t = MyCornerType();
                if (t == CornerType.ACUTE || t == CornerType.OBTUSE)
                {
                    bool succCornerBeep = pc.ReceivedBeepOnPartitionSet(1);
                    bool predCornerBeep = pc.ReceivedBeepOnPartitionSet(3);

                    // We now know about (up to) 4 other corners, this suffices to determine the shape type and rotation
                    // Count number of acute corners
                    int numAcute = (t == CornerType.ACUTE ? 1 : 0)
                        + (succCornerAcute ? 1 : 0)
                        + (predCornerAcute ? 1 : 0)
                        + (succCornerBeep ? 1 : 0)
                        + (predCornerBeep ? 1 : 0);

                    // 0 => Hexagon
                    // 1 => Pentagon
                    // 2 => Obtuse: Parallelogram, Acute: Trapezoid
                    // 3 => Obtuse: Trapezoid, Acute: Parallelogram
                    // 5 => Triangle
                    if (numAcute == 0)
                    {
                        shapeType.SetValue(ShapeType.HEXAGON);
                    }
                    else if (numAcute == 1)
                    {
                        shapeType.SetValue(ShapeType.PENTAGON);
                    }
                    else if (numAcute == 2)
                    {
                        shapeType.SetValue(t == CornerType.OBTUSE ? ShapeType.PARALLELOGRAM : ShapeType.TRAPEZOID);
                    }
                    else if (numAcute == 3)
                    {
                        shapeType.SetValue(t == CornerType.OBTUSE ? ShapeType.TRAPEZOID : ShapeType.PARALLELOGRAM);
                    }
                    else if (numAcute == 5)
                    {
                        shapeType.SetValue(ShapeType.TRIANGLE);
                    }
                    else
                    {
                        Log.Error("Invalid number of acute beeps, cannot determine system's shape type");
                    }
                }

                round.SetValue(4);
            }
            else if (round == 4)
            {
                // Place the marker on the start amoebot of the global counter
                marker.SetValue(IsCounterStart());

                PinConfiguration pc = GetContractedPinConfiguration();

                // Setup PASC on the current side
                Direction sideDir = DirectionHelpers.Cardinal(counter1);
                if (succDir == sideDir || predDir == sideDir.Opposite())
                {
                    // We must participate in PASC
                    bool leader = !HasNeighborAt(sideDir.Opposite());
                    pasc.Init(leader, leader ? Direction.NONE : predDir, succDir == sideDir ? sideDir : Direction.NONE, PinsPerEdge - 1, PinsPerEdge - 2, 0, 1, 0, 1);

                    // Setup PASC circuit
                    pasc.SetupPC(pc);
                }

                // Setup two global circuits on the unused pins
                SetupGlobalCircuits(pc, sideDir);
                SetPlannedPinConfiguration(pc);

                // Send first PASC beep
                if (IsOnSide(sideDir) && succDir == sideDir)
                {
                    pasc.ActivateSend();
                }

                round.SetValue(5);
            }
            else if (round == 5 || round == 6)
            {
                Direction sideDir = DirectionHelpers.Cardinal(counter1);
                PinConfiguration pc = GetCurrentPinConfiguration();

                // PASC participants receive beep
                bool pascParticipant = succDir == sideDir || predDir == sideDir.Opposite();
                if (pascParticipant)
                {
                    pasc.ActivateReceive();
                }

                // Receive beeps on two global circuits if in round 6
                if (round == 6)
                {
                    // Marker records side length bit on first global circuit
                    if (marker)
                    {
                        sideLengthBits[counter1].SetValue(pc.ReceivedBeepOnPartitionSet(2));
                    }

                    bool continuationBeep = pc.ReceivedBeepOnPartitionSet(3);
                    if (continuationBeep)
                    {
                        // Continuation beep: Marker moves forward
                        if (MyCornerType() != CornerType.INNER)
                        {
                            marker.SetValue(((SCConvexSystemParticle)GetNeighborAt(predDir)).marker);
                        }
                    }
                    else
                    {
                        // No continuation beep: Finish or continue with next iteration
                        counter1.SetValue(counter1 + 1);
                        if (counter1.GetCurrentValue() > 5)
                        {
                            marker.SetValue(false);
                            counter1.SetValue(0);    // Reset counter so it can be reused
                            round.SetValue(7);
                        }
                        else
                            round.SetValue(4);
                        return;
                    }
                }

                // Sending part
                if (pascParticipant)
                    pasc.SetupPC(pc);

                SetPlannedPinConfiguration(pc);

                // Send PASC bit on first global circuit
                if (pascParticipant && !HasNeighborAt(sideDir) && pasc.GetReceivedBit() != 0)
                {
                    pc.SendBeepOnPartitionSet(2);
                }

                // Send continuation beep on second global circuit if we became passive
                if (pascParticipant && pasc.BecamePassive())
                {
                    pc.SendBeepOnPartitionSet(3);
                }

                // Also send next PASC beep
                if (pascParticipant)
                {
                    pasc.ActivateSend();
                }

                round.SetValue(6);
            }
            else if (round == 7)
            {
                PinConfiguration pc = GetContractedPinConfiguration();
                CornerType t = MyCornerType();
                
                // Inner amoebots initialize binary operation subroutine on outer boundary counter
                if (t != CornerType.INNER)
                {
                    // Order of computations:
                    // 0. R1 = a + b
                    // 1. R2 = a + c
                    // 2. R3 = b + d
                    // 3. R4 = a + b + c = R1 + c
                    // 4. R5 = a + b + d = R1 + d
                    bool bitA = counter1 < 2 ? sideLengthBits[3] : (counter1 == 2 ? sideLengthBits[2] : inequalityR[0]);
                    bool bitB = counter1 == 0 ? sideLengthBits[2] : (counter1 == 1 || counter1 == 3 ? sideLengthBits[4] : sideLengthBits[1]);

                    binops.Init(SubBinOps.Mode.ADD, bitA, IsCounterStart() ? Direction.NONE : predDir, IsCounterEnd() ? Direction.NONE : succDir, bitB);
                    binops.SetupPinConfig(pc);

                    SetPlannedPinConfiguration(pc);
                    binops.ActivateSend();
                    round.SetValue(8);
                }
                // Other amoebots setup global circuit and wait
                else
                {
                    pc.SetToGlobal(0);
                    SetPlannedPinConfiguration(pc);
                    round.SetValue(42);
                }
            }
            else if (round == 8)
            {
                bool binOpFinished = ActivateBinOp();

                if (binOpFinished)
                {
                    // Record the inequality bit
                    inequalityR[counter1].SetValue(binops.ResultBit());
                    counter1.SetValue(counter1 + 1);
                    if (counter1.GetCurrentValue() > 4)
                    {
                        // Finished
                        // Reset counter
                        counter1.SetValue(0);
                        // Setup global circuit and beep
                        round.SetValue(9);
                    }
                    else
                    {
                        round.SetValue(7);
                    }
                }
            }
            else if (round == 9)
            {
                // Setup binary operation for MSB detection on outer boundary
                if (MyCornerType() != CornerType.INNER)
                {
                    binops.Init(SubBinOps.Mode.MSB, inequalityR[counter1], IsCounterStart() ? Direction.NONE : predDir, IsCounterEnd() ? Direction.NONE : succDir);
                    PinConfiguration pc = GetContractedPinConfiguration();
                    binops.SetupPinConfig(pc);
                    SetPlannedPinConfiguration(pc);
                    binops.ActivateSend();
                    round.SetValue(10);
                }
            }
            else if (round == 10)
            {
                bool binOpFinished = ActivateBinOp();

                if (binOpFinished)
                {
                    // Record MSB
                    inequalityMSBsR[counter1].SetValue(binops.IsMSB());
                    counter1.SetValue(counter1 + 1);
                    // Repeat with other MSBs if we are not done yet
                    if (counter1.GetCurrentValue() < 5)
                    {
                        round.SetValue(9);
                    }
                    // Otherwise continue
                    else
                    {
                        counter1.SetValue(0);
                        round.SetValue(11);
                    }
                }
            }
            else if (round == 11)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    // Set K2 := 1111..., counter2 := 0, Li := 0
                    bitK2.SetValue(true);
                    counter2.SetValue(0);
                    for (int i = 0; i < 5; i++)
                    {
                        inequalityL[i].SetValue(false);
                    }
                    // Place marker on counter start
                    marker.SetValue(IsCounterStart());

                    // Marker writes down the first bit of all Lis
                    WriteLiBits();
                    counter2.SetValue(1);

                    round.SetValue(12);
                }
            }
            else if (round == 12)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    // Continue if counter has reached end of LIs
                    bool reachedEnd = true;
                    for (int i = 0; i < 5; i++)
                    {
                        if (counter2 < shape.GetConvHullInequalityString(counter1, i).Length)
                        {
                            reachedEnd = false;
                            break;
                        }
                    }
                    if (reachedEnd)
                    {
                        counter2.SetValue(0);
                        marker.SetValue(false);
                        round.SetValue(13);
                    }
                    else
                    {
                        // Move marker ahead
                        marker.SetValue(((SCConvexSystemParticle)GetNeighborAt(predDir)).marker);

                        // Print error if the marker has reached the counter start again
                        if (marker.GetCurrentValue() && IsCounterStart())
                        {
                            Log.Error("Marker has reached counter start: Input shape is much too large");
                        }

                        WriteLiBits();
                        counter2.SetValue(counter2 + 1);
                    }
                }
            }
            else if (round == 13)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    // If we have finished all operations: Continue in round 16
                    if (counter2 >= 5)
                    {
                        round.SetValue(16);
                    }
                    else
                    {
                        // Skip this counter value if the current Li is 0
                        if (shape.GetConvHullInequality(counter1, counter2) == 0)
                        {
                            counter2.SetValue(counter2 + 1);
                        }
                        else
                        {
                            // Setup binary operation to compute Ri / Li
                            binops.Init(SubBinOps.Mode.DIV, inequalityR[counter2], IsCounterStart() ? Direction.NONE : predDir, IsCounterEnd() ? Direction.NONE : succDir, inequalityL[counter2], inequalityMSBsR[counter2]);
                            PinConfiguration pc = GetContractedPinConfiguration();
                            binops.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            binops.ActivateSend();
                            round.SetValue(14);
                        }
                    }
                }
            }
            else if (round == 14)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    bool isFinished = ActivateBinOp();
                    if (isFinished)
                    {
                        // Set result
                        bitK3.SetValue(binops.ResultBit());

                        // Setup binary op to compare K3 and K2
                        binops.Init(SubBinOps.Mode.COMP, bitK3.GetCurrentValue(), IsCounterStart() ? Direction.NONE : predDir, IsCounterEnd() ? Direction.NONE : succDir, bitK2);
                        PinConfiguration pc = GetContractedPinConfiguration();
                        binops.SetupPinConfig(pc);
                        SetPlannedPinConfiguration(pc);
                        binops.ActivateSend();
                        round.SetValue(15);
                    }
                }
            }
            else if (round == 15)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    bool isFinished = ActivateBinOp();
                    if (isFinished)
                    {
                        // Update K2 if K3 < K2
                        if (binops.CompResult() == SubComparison.ComparisonResult.LESS)
                        {
                            bitK2.SetValue(bitK3);
                        }

                        // Continue with next iteration
                        counter2.SetValue(counter2 + 1);
                        round.SetValue(13);
                    }
                }
            }
            else if (round == 16)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    // Setup binary operation to compare K to K2
                    binops.Init(SubBinOps.Mode.COMP, bitK, IsCounterStart() ? Direction.NONE : predDir, IsCounterEnd() ? Direction.NONE : succDir, bitK2);
                    PinConfiguration pc = GetContractedPinConfiguration();
                    binops.SetupPinConfig(pc);
                    SetPlannedPinConfiguration(pc);
                    binops.ActivateSend();
                    round.SetValue(17);
                }
            }
            else if (round == 17)
            {
                if (MyCornerType() != CornerType.INNER)
                {
                    bool isFinished = ActivateBinOp();
                    if (isFinished)
                    {
                        // If K2 > K: Update K and rotation
                        if (binops.CompResult() == SubComparison.ComparisonResult.LESS)
                        {
                            bitK.SetValue(bitK2);
                            rotation.SetValue(counter1);
                        }

                        counter1.SetValue(counter1 + 1);
                        // Repeat for next rotation if we are not done yet
                        if (counter1.GetCurrentValue() < 6)
                        {
                            round.SetValue(11);
                        }
                        else
                        {
                            round.SetValue(18);
                        }
                    }
                }
            }
            else if (round == 18)
            {
                // TODO
            }



            else if (round == 42)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    // Received synchronization beep, continue
                    // TODO
                }
            }

            SetColor();
        }

        /// <summary>
        /// Helper setting the proper color.
        /// </summary>
        private void SetColor()
        {
            if (marker.GetCurrentValue())
                SetMainColor(ColorData.Particle_Orange);
            else
            {
                CornerType t = MyCornerType();
                if (t == CornerType.INNER)
                    SetMainColor(ColorData.Particle_Black);
                else if (t == CornerType.SIDE)
                    SetMainColor(ColorData.Particle_Blue);
                else
                    SetMainColor(ColorData.Particle_Green);
            }
        }

        /// <summary>
        /// Helper to determine the corner type from the two computed
        /// successor and predecessor directions.
        /// </summary>
        /// <returns>The type of this corner, if the directions
        /// have been computed already.</returns>
        private CornerType MyCornerType()
        {
            Direction pred = predDir.GetCurrentValue();
            Direction succ = succDir.GetCurrentValue();
            if (pred == Direction.NONE)
                return CornerType.INNER;
            else if (pred == succ.Opposite())
                return CornerType.SIDE;
            else if (pred.DistanceTo(succ) == 2)
                return CornerType.ACUTE;
            else
                return CornerType.OBTUSE;
        }

        /// <summary>
        /// Helper to identify the kind of corner this amoebot is.
        /// The corner type includes positions on sides and inner positions.
        /// </summary>
        /// <returns><c>true</c> if and only if the corner type was
        /// identified successfully.</returns>
        private bool IdentifyCornerType()
        {
            bool[] occupied = new bool[6];
            int numOccupied = 0;
            for (int d = 0; d < 6; d++)
            {
                occupied[d] = HasNeighborAt(DirectionHelpers.Cardinal(d));
                if (occupied[d])
                    numOccupied++;
            }

            // Inner
            if (numOccupied == 6)
            {
                return true;
            }

            // Line, point, or concave corner
            if (numOccupied < 2 || numOccupied == 5)
            {
                return false;
            }

            // Find first occupied position such that previous position is not occupied
            // If there is more than one such position, the shape cannot be convex
            int pos = -1;
            for (int d = 0; d < 6; d++)
            {
                if (occupied[d] && !occupied[(d + 5) % 6])
                {
                    if (pos == -1)
                        pos = d;
                    else
                        return false;
                }
            }

            // The number of unoccupied neighbors is enough to tell us what kind of corner we are
            // and the directions
            predDir.SetValue(DirectionHelpers.Cardinal(pos));
            succDir.SetValue(DirectionHelpers.Cardinal((pos + numOccupied - 1) % 6));

            return true;
        }

        /// <summary>
        /// Helper to check whether the amoebot is on the side with
        /// the given successor direction.
        /// </summary>
        /// <param name="dir">The direction of the desired side
        /// when traversing the outer boundary in clockwise direction.</param>
        /// <returns><c>true</c> if and only if this amoebot is part
        /// of the given side.</returns>
        private bool IsOnSide(Direction dir)
        {
            return !HasNeighborAt(dir.Rotate60(1)) && !HasNeighborAt(dir.Rotate60(2));
        }

        /// <summary>
        /// Helper to identify the start amoebot of the counter on
        /// the outer boundary. This is the leftmost amoebot of
        /// side f (the top side). Might return <c>true</c> for
        /// multiple amoebots if the system is not convex.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot is
        /// the counter start.</returns>
        private bool IsCounterStart()
        {
            return IsOnSide(Direction.E) && !HasNeighborAt(Direction.W);
        }

        /// <summary>
        /// Helper to identify the end amoebot of the counter on
        /// the outer boundary.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot is
        /// the counter end.</returns>
        private bool IsCounterEnd()
        {
            return succDir == Direction.NNE && ((SCConvexSystemParticle)GetNeighborAt(Direction.NNE)).succDir != succDir
                || succDir == Direction.NNW && ((SCConvexSystemParticle)GetNeighborAt(Direction.NNW)).succDir == Direction.E;
        }

        /// <summary>
        /// Helper for writing the current bits of the Lis
        /// into the marker position.
        /// </summary>
        private void WriteLiBits()
        {
            if (marker.GetCurrentValue())
            {
                int r = counter1.GetCurrentValue();
                int j = counter2.GetCurrentValue();
                for (int i = 0; i < 5; i++)
                {
                    string ineq = shape.GetConvHullInequalityString(r, i);
                    inequalityL[i].SetValue(j < ineq.Length && ineq[j] == '1');
                }
            }
        }

        /// <summary>
        /// Helper for setting up the side circuits. Creates
        /// two directional circuits along each side of the shape,
        /// allowing each corner to transmit in two directions.
        /// For corners, the successor partition sets have ID
        /// 0 for the outgoing and 1 for the incoming circuit,
        /// and IDs 2 and 3 are used for the predecessor circuits.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        private void SetupSideCircuit(PinConfiguration pc)
        {
            Direction pred = predDir.GetCurrentValue();
            Direction succ = succDir.GetCurrentValue();
            
            // Inner amoebots do nothing
            if (pred == Direction.NONE)
                return;

            int pinSuccOut = pc.GetPinAt(succ, 0).Id;
            int pinSuccIn = pc.GetPinAt(succ, PinsPerEdge - 1).Id;
            int pinPredOut = pc.GetPinAt(pred, 0).Id;
            int pinPredIn = pc.GetPinAt(pred, PinsPerEdge - 1).Id;

            // Side amoebots just connect
            if (pred == succ.Opposite())
            {
                pc.MakePartitionSet(new int[] { pinSuccOut, pinPredIn }, 0);
                pc.MakePartitionSet(new int[] { pinPredOut, pinSuccIn }, 1);
                pc.SetPartitionSetPosition(0, new Vector2((pred.ToInt() + 1.5f) * 60, 0.5f));
                pc.SetPartitionSetPosition(1, new Vector2((succ.ToInt() + 1.5f) * 60, 0.5f));
            }
            // Corners have 4 individual partition sets
            else
            {
                pc.MakePartitionSet(new int[] { pinSuccOut }, 0);
                pc.MakePartitionSet(new int[] { pinSuccIn }, 1);
                pc.MakePartitionSet(new int[] { pinPredOut }, 2);
                pc.MakePartitionSet(new int[] { pinPredIn }, 3);
            }
        }

        /// <summary>
        /// Helper setting up two global circuits such that they do not interfere
        /// with PASC. Their partition set IDs will be 2 and 3.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="d">The direction in which PASC is currently running.</param>
        private void SetupGlobalCircuits(PinConfiguration pc, Direction d)
        {
            bool[] invertedDirs = new bool[] { false, false, false, true, true, true };
            bool invertPins = d == Direction.E || d == Direction.NNE || d == Direction.NNW;
            pc.SetStarConfig(invertPins ? 3 : 0, invertedDirs, 2);
            pc.SetStarConfig(invertPins ? 2 : 1, invertedDirs, 3);
            pc.SetPartitionSetPosition(2, new Vector2((d.ToInt() + 0.5f) * 60, 0.5f));
            pc.SetPartitionSetPosition(3, new Vector2((d.ToInt() + 2.5f) * 60, 0.5f));
        }

        /// <summary>
        /// Helper for code used to run the binary operation
        /// subroutine on the outer boundary. Runs the receiving
        /// part of the subroutine and returns <c>true</c> if it
        /// is finished. Otherwise, sets up the pin configuration
        /// and sends the beeps.
        /// </summary>
        /// <param name="pc">The pin configuration to be used.</param>
        private bool ActivateBinOp()
        {
            CornerType t = MyCornerType();

            if (t != CornerType.INNER)
            {
                binops.ActivateReceive();
                if (binops.IsFinished())
                {
                    return true;
                }

                PinConfiguration pc = GetContractedPinConfiguration();
                binops.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                binops.ActivateSend();
            }
            return false;
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SCConvexSystemInitializer : InitializationMethod
    {

        public SCConvexSystemInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(SCConvexSystemParticle.ShapeType systemShapeType = SCConvexSystemParticle.ShapeType.HEXAGON, int a = 12, int b = 8, int c = 6, int d = 10, int rotation = 0,
            string shape = "shape2.json", bool fromFile = true)
        {
            // Read the shape
            Shape s;
            if (fromFile)
            {
                s = Shape.ReadFromJson(FilePaths.path_shapes + shape);
            }
            else
            {
                s = JsonUtility.FromJson<Shape>(shape);
            }
            if (s is null)
            {
                Log.Error("Failed to read shape");
                return;
            }
            if (!s.IsConsistent())
            {
                Log.Warning("Shape is inconsistent!");
            }
            else
            {
                s.GenerateTraversal();
                s.GenerateConvexHull();
                s.Draw(Vector2Int.zero);
                SCConvexSystemParticle.shape = s;
            }

            // Construct the system
            if (systemShapeType == SCConvexSystemParticle.ShapeType.TRIANGLE)
            {
                if (a < 1)
                {
                    Log.Error("Cannot create triangle with side length < 1");
                    return;
                }

                for (int i = 0; i <= a; i++)
                {
                    for (int j = 0; j <= a - i; j++)
                    {
                        AddParticle(AmoebotFunctions.RotateVector(new Vector2Int(i, j), rotation));
                    }
                }
            }
            else if (systemShapeType == SCConvexSystemParticle.ShapeType.PARALLELOGRAM)
            {
                if (a < 1 || b < 1)
                {
                    Log.Error("Cannot create parallelogram with side length < 1");
                    return;
                }

                for (int i = 0; i <= a; i++)
                {
                    for (int j = 0; j <= b; j++)
                    {
                        AddParticle(AmoebotFunctions.RotateVector(new Vector2Int(i, j), rotation));
                    }
                }
            }
            else if (systemShapeType == SCConvexSystemParticle.ShapeType.TRAPEZOID)
            {
                if (a < 1 || b < 1)
                {
                    Log.Error("Cannot create trapezoid with side length < 1");
                    return;
                }
                else if (b >= a)
                {
                    Log.Error("Cannot create trapezoid with b >= a");
                    return;
                }

                for (int j = 0; j <= b; j++)
                {
                    for (int i = 0; i <= a - j; i++)
                    {
                        AddParticle(AmoebotFunctions.RotateVector(new Vector2Int(i, j), rotation));
                    }
                }
            }
            else if (systemShapeType == SCConvexSystemParticle.ShapeType.PENTAGON)
            {
                if (a < 1 || b < 1 || c < 1)
                {
                    Log.Error("Cannot create pentagon with side length < 1");
                    return;
                }
                else if (c >= b)
                {
                    Log.Error("Cannot create trapezoid with c >= b");
                    return;
                }

                for (int j = 0; j <= b; j++)
                {
                    for (int i = 0; i <= a - Mathf.Max(0, j - c); i++)
                    {
                        AddParticle(AmoebotFunctions.RotateVector(new Vector2Int(i, j), rotation));
                    }
                }
            }
            else if (systemShapeType == SCConvexSystemParticle.ShapeType.HEXAGON)
            {
                if (a < 1 || b < 1 || c < 1 || d < 1)
                {
                    Log.Error("Cannot create hexagon with side length < 1");
                    return;
                }
                else if (c >= b + d || d >= a + c)
                {
                    Log.Error("Cannot create trapezoid with c >= b + d or d >= a + c");
                    return;
                }

                int l = a;
                for (int y = 0; y <= b + d; y++)
                {
                    int xStart = Mathf.Max(-y, -b);
                    for (int x = xStart; x <= xStart + l; x++)
                    {
                        AddParticle(AmoebotFunctions.RotateVector(new Vector2Int(x, y), rotation));
                    }
                    if (y < b)
                    {
                        l++;
                    }
                    if (y >= c)
                    {
                        l--;
                    }
                }
            }
        }
    }

} // namespace AS2.Algos.SCConvexSystem
