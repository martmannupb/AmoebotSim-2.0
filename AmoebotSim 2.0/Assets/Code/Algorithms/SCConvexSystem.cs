using System.Collections.Generic;
using AS2.Sim;
using AS2.Subroutines.BinaryOps;
using AS2.Subroutines.PASC;
using AS2.Subroutines.ShapeConstruction;
using AS2.ShapeContainment;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.SCConvexSystem
{

    /// <summary>
    /// Shape containment solution for the case of convex amoebot systems.
    /// Supports convex amoebot systems with at least 3 corners (the only
    /// other convex shapes being lines and points).
    /// <para>
    /// This algorithm can handle every valid shape unless it is so large
    /// that the resulting left sides of the inequalities cannot be stored
    /// on the outer boundary of the amoebot system, in which case the shape
    /// would never fit into the system anyway.
    /// </para>
    /// <para>
    /// <b>Disclaimer: The save/load feature does not work for
    /// this algorithm because it stores the target shape in a
    /// static member. Always generate this algorithm from
    /// Init Mode.</b>
    /// </para>
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

    // Procedure:
    //  - Only the outer boundary amoebots participate, the inner amoebots establish a global circuit and wait for a synchronization beep
    //  - Compute R1,...,R5 by adding side lengths (rounds 7-8)
    //  - Find MSBs of R1,...,R5 (required for division; rounds 9-10)
    //  - For each rotation counter1 = 0,...,5: (start round 11)
    //      - Set all Li := 0
    //      - Place a marker at the counter start
    //      - Move the marker along the counter, placing the bits of the Li (round 12)
    //      - For each i = counter2 = 0,...,4: (start round 13)
    //          - If counter2 > 4: Compare K to K2 and break (round 17)
    //          - If Li = 0 (all amoebots can check this): Increment counter2 and continue
    //          - Compare Li to Ri (round 14)
    //              - If Li > Ri: Set K3 := 0
    //              - Otherwise: Compute K3 = Ri / Li (round 14)
    //          - Compare K3 to K2 (round 15)
    //          - If K3 < K2: Set K2 := K3 (round 16)
    //          - Compare K to K2
    //              - Handle the result (round 17):
    //              - If counter2 has reached the end:
    //                  - Set K := Max(K, K2)
    //                      - And update rotation if necessary
    //                  - Increment counter1 and go back to round 11
    //                      - Or finish loop and go to round 18
    //              - If counter2 has not reached the end yet:
    //                  - If K >= K2: Increment counter 1 and go back to round 11 (or finish and go to round 18)
    //                  - Otherwise, increment counter2 and go back to round 13

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
    //  - Inner amoebots setup global circuit and go to round 19 to wait

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
    //          - Reset counter 1
    //          - Go to round 11

    // Round 11:
    //  - Set K2 := 1111... on the boundary
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
    //      - Setup binary operation to kompare K to K2
    //      - Start binary ops
    //      - Go to round 17
    //  - Setup binary operation to compare Ri to Li
    //  - Start binary ops
    //  - Go to round 14

    // Round 14:
    //  - Continue binary ops
    //  - If finished:
    //      - If Ri < Li:
    //          - Set K3 := 0
    //          - Setup binary operation to compare K3 to K2
    //          - Start binary ops
    //          - Go to round 16
    //      - Otherwise:
    //          - Setup binary operation to compute Ri / Li
    //          - Start binary ops
    //          - Go to round 15

    // Round 15:
    //  - Continue binary ops
    //  - If finished:
    //      - Set K3 := Ri / Li
    //      - Setup binary operation to compare K3 to K2
    //      - Start binary ops
    //      - Go to round 16

    // Round 16:
    //  - Continue binary ops
    //  - If finished:
    //      - If K3 < K2: Set K2 := K3
    //      - Setup binary operation to kompare K to K2
    //      - Start binary ops
    //      - Go to round 17

    // Round 17:
    //  - Continue binary ops
    //  - If finished:
    //      - Increment counter2
    //      - If counter2 has reached the end:
    //          - Set K := Max(K, K2) and update rotation if necesary
    //          - Increment counter1 and continue loop (round 11) or finish (round 18)
    //      - If counter2 has not reached the end:
    //          - If K >= K2: Increment counter1 and continue (11) or finish (18)
    //          - Otherwise: Increment counter 2 and go to round 13

    // Round 18:
    //  - Inequality computation is finished
    //  - Establish global circuit and beep for synchronization
    //  - Go to round 19

    // Round 19:
    //  - All amoebots run this code again
    //  - If beep is received on global circuit:
    //      - Amoebots with K-bit = 1 beep
    //      - Go to round 20

    // Round 20:
    //  - If no beep on global circuit: Terminate with failure
    //  - Otherwise:
    //      - Setup 3 global circuits on partition sets 0, 1, 2
    //      - Border amoebots send beeps to transmit rotation value in binary

    // Round 21:
    //  - Receive rotation value in binary
    //  - Set counter1 := 0
    //  - Reset all helper bits and MSBs


    // PHASE 4: HALF-PLANE INTERSECTION

    // We compute the half-plane distances and determine each of the six half-planes
    // At the end, we find the convex representation set and choose a unique representative

    // Round 22:
    //  - Place marker at chain start
    //  - Marker writes first bits and MSBs of origin distances
    //  - Increment counter1
    //  - Inner amoebots setup global circuit and go to round 27 to wait for synchronization
    //  - Go to round 23

    // Round 23:
    //  - If counter1 has reached end of origin distance strings:
    //      - Remove marker, reset counter and go to round 24
    //  - Move marker ahead and write next bits and MSBs
    //  - Increment counter1

    // Round 24:
    //  - If counter1 >= 6:
    //      - Setup global circuit and beep
    //      - Jump to round 27
    //  - Otherwise:
    //      - Setup binop for multiplying d_i and k (k should be second argument)
    //      - Start binop
    //      - Go to round 25

    // Round 25:
    //  - Continue binop
    //  - If finished:
    //      - Write result into helper bits
    //      - Setup binop for MSB detection on d_i
    //      - Start binop
    //      - Go to round 26

    // Round 26:
    //  - Continue binop
    //  - If finished:
    //      - Set MSB for current d_i
    //      - Increment counter1
    //      - Go to round 24

    // Round 27:
    //  - (Inner and outer amoebots meet here again)
    //  - If synchronization beep on global circuit is received:
    //      - Reset counter1 to 0
    //      - Go to round 28

    // Round 28:
    //  - If counter1 >= 6:
    //      - Have finished this part, jump to round 31
    //  - Setup PASC instance for current direction (based on counter1)
    //  - Reset comparison result to >= (only bool needed here)
    //  - Setup PASC circuit and 2 global circuits
    //  - Place marker on counter start
    //  - Start PASC
    //  - Marker sends distance bit on first global circuit
    //      - And MSB bit on second global circuit
    //  - Go to round 29

    // Round 29:
    //  - Receive PASC bit
    //  - Receive both global circuit bits
    //  - Update comparison result
    //  - If MSB beep on second global circuit was received:
    //      - Setup PASC cutoff circuit and send cutoff beep
    //      - Go to round 30
    //  - Move marker one position ahead
    //  - Setup new PASC and global circuits
    //  - Send PASC, distance and MSB beeps

    // Round 30:
    //  - Receive PASC cutoff beep
    //  - Update comparison result (all amoebots receiving beep set result to >=)
    //  - Update half-plane intersection result
    //  - Increment counter1
    //  - Go back to round 28

    // Round 31:
    //  - Choose unique representative of the representation set
    //      - Top-left amoebot (?)
    //  - Setup empty pin configuration
    //  - Reset marker
    //  - Go to round 32


    // PHASE 5: PLACING THE FINAL SHAPE

    // Round 32:
    //  - Setup binop to find MSB of K
    //  - Start binop
    //  - Inner amoebots go to round 34 and setup global circuit
    //  - Go to round 33

    // Round 33:
    //  - Wait for binop to finish
    //  - If finished:
    //      - Set MSB of K
    //      - Setup global circuit and beep
    //      - Go to round 34

    // Round 34:
    //  - Wait for beep on global circuit
    //  - If received:
    //      - Setup shape construction subroutine
    //      - Place marker at counter start
    //      - Start shape construction
    //      - Go to round 35

    // Round 35:
    //  - Receive shape construction beeps
    //  - If finished:
    //      - Terminate successfully
    //  - Otherwise:
    //      - If scale bit must be reset:
    //          - Set marker to counter start
    //          - Go to round 36
    //      - Otherwise:
    //          - Activate shape construction with possible scale bit
    //          - If scale bit was needed: Forward marker

    // Round 36:
    //  - (Scale bit was reset)
    //  - Activate shape construction as usual
    //      - Including marker forwarding etc.
    //  - Go back to round 35


    public class SCConvexSystemParticle : ParticleAlgorithm
    {
        [StatusInfo("Draw Target Shape", "Draws the target shape at the origin or the selected amoebot.")]
        public static void DrawTargetShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            Vector2Int pos = Vector2Int.zero;
            if (selectedParticle != null)
                pos = selectedParticle.Head();

            AS2.UI.LineDrawer.Instance.Clear();
            shape.Draw(pos);
            AS2.UI.LineDrawer.Instance.SetTimer(20);
        }

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

        // Could encode everything in 2 integers (not worth the effort (?))
        // round: 0-36              +6
        // predDir                  +3
        // succDir                  +3
        // shapeType                +3
        // 11 bools                 +11
        // 23 bits                  +23
        // counter1: 0-6            +3
        // counter2: 0-6            +3
        // rotation: -1-5           +3
        //                          58

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

        ParticleAttribute<bool>[] helperBits = new ParticleAttribute<bool>[6];          // Bits used for inequalities and side distances
        ParticleAttribute<bool>[] helperMSBs = new ParticleAttribute<bool>[6];          // MSB flags used for inequalities and side distances
        ParticleAttribute<bool>[] inequalityL = new ParticleAttribute<bool>[5];

        ParticleAttribute<bool> bitK;       // Final (max) scale
        ParticleAttribute<bool> bitK2;      // Intermediate (min) scale
        ParticleAttribute<bool> bitK3;      // Temporary variable
        ParticleAttribute<int> rotation;    // The rotation of the maximum scale

        ParticleAttribute<bool> inCurrentHalfPlane;
        ParticleAttribute<bool> inHalfPlaneIntersection;
        ParticleAttribute<bool> representative;

        public static Shape shape;

        SubBinOps binops;
        SubPASC2 pasc;
        SubShapeConstruction shapeConstr;

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
            for (int i = 0; i < 6; i++)
            {
                helperBits[i] = CreateAttributeBool("Helper Bit [" + i + "]", false);
            }
            for (int i = 0; i < 6; i++)
            {
                helperMSBs[i] = CreateAttributeBool("Helper MSB [" + i + "]", false);
            }
            for (int i = 0; i < 5; i++)
            {
                inequalityL[i] = CreateAttributeBool("Inequality L [" + i + "]", false);
            }
            bitK = CreateAttributeBool("Bit K", false);
            bitK2 = CreateAttributeBool("Bit K2", false);
            bitK3 = CreateAttributeBool("Bit K3", false);
            rotation = CreateAttributeInt("Rotation", -1);

            inCurrentHalfPlane = CreateAttributeBool("Current Half-Plane", true);
            inHalfPlaneIntersection = CreateAttributeBool("Half-Plane Intersection", true);
            representative = CreateAttributeBool("Representative", false);

            pasc = new SubPASC2(p);
            binops = new SubBinOps(p);
            shapeConstr = new SubShapeConstruction(p, shape, pasc);

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
            int r = round;
            switch (r)
            {
                // PHASE 1: ESTABLISH SHAPE
                case 0:
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
                    break;
                case 1:
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
                    break;
                case 2:
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
                    break;
                case 3:
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
                    break;

                // PHASE 2: COMPUTE SIDE LENGTHS
                case 4:
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
                            List<Direction> predecessors = new List<Direction>();
                            List<Direction> successors = new List<Direction>();
                            if (!leader)
                                predecessors.Add(predDir);
                            if (succDir == sideDir)
                                successors.Add(succDir);
                            pasc.Init(predecessors, successors, 0, 1, 0, 1, leader);

                            // Setup PASC circuit
                            pasc.SetupPC(pc);
                        }

                        // Setup two global circuits on the unused pins
                        SetupGlobalCircuitsPASC(pc, sideDir);
                        SetPlannedPinConfiguration(pc);

                        // Send first PASC beep
                        if (IsOnSide(sideDir) && succDir == sideDir)
                        {
                            pasc.ActivateSend();
                        }

                        round.SetValue(5);
                    }
                    break;
                case 5:
                case 6:
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
                    break;

                // PHASE 3: INEQUALITIES
                case 7:
                    {
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
                            bool bitA = counter1 < 2 ? sideLengthBits[3] : (counter1 == 2 ? sideLengthBits[2] : helperBits[0]);
                            bool bitB = counter1 == 0 ? sideLengthBits[2] : (counter1 == 1 || counter1 == 3 ? sideLengthBits[4] : sideLengthBits[1]);

                            StartBinOp(SubBinOps.Mode.ADD, bitA, bitB);
                            round.SetValue(8);
                        }
                        // Other amoebots setup global circuit and wait
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            pc.SetToGlobal(0);
                            pc.SetPartitionSetColor(0, ColorData.Circuit_Colors[4]);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(19);
                        }
                    }
                    break;
                case 8:
                    {
                        bool binOpFinished = ActivateBinOp();

                        if (binOpFinished)
                        {
                            // Record the inequality bit
                            helperBits[counter1].SetValue(binops.ResultBit());
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
                    break;
                case 9:
                    {
                        // Setup binary operation for MSB detection on outer boundary
                        StartBinOp(SubBinOps.Mode.MSB, helperBits[counter1]);
                        round.SetValue(10);
                    }
                    break;
                case 10:
                    {
                        bool binOpFinished = ActivateBinOp();

                        if (binOpFinished)
                        {
                            // Record MSB
                            helperMSBs[counter1].SetValue(binops.IsMSB());
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
                    break;
                case 11:
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
                    break;
                case 12:
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
                            // Start solving inequalities
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
                    break;
                case 13:
                    {
                        // If we have finished all operations: Continue in round 16
                        if (counter2 >= 5)
                        {
                            // Setup binary operation to compare K to K2
                            StartBinOp(SubBinOps.Mode.COMP, bitK, bitK2);
                            round.SetValue(17);
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
                                // Setup binary operation to compare Ri to Li
                                StartBinOp(SubBinOps.Mode.COMP, helperBits[counter2], inequalityL[counter2]);
                                round.SetValue(14);
                            }
                        }
                    }
                    break;
                case 14:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            if (binops.CompResult() == SubComparison.ComparisonResult.LESS)
                            {
                                // Ri < Li: Set result to 0 and skip division
                                bitK3.SetValue(false);
                                // Start binary operation to compare K3 to K2
                                StartBinOp(SubBinOps.Mode.COMP, bitK3.GetCurrentValue(), bitK2);
                                round.SetValue(16);
                            }
                            else
                            {
                                // Ri >= Li: Start binary operation to compute Ri / Li
                                StartBinOp(SubBinOps.Mode.DIV, helperBits[counter2], inequalityL[counter2], helperMSBs[counter2]);
                                round.SetValue(15);
                            }
                        }
                    }
                    break;
                case 15:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            // Set division result
                            bitK3.SetValue(binops.ResultBit());

                            // Setup binary op to compare K3 and K2
                            StartBinOp(SubBinOps.Mode.COMP, bitK3.GetCurrentValue(), bitK2);
                            round.SetValue(16);
                        }
                    }
                    break;
                case 16:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            // Update K2 if K3 < K2
                            if (binops.CompResult() == SubComparison.ComparisonResult.LESS)
                            {
                                bitK2.SetValue(bitK3);
                            }

                            // Setup binary op to compare K to K2
                            StartBinOp(SubBinOps.Mode.COMP, bitK, bitK2.GetCurrentValue());
                            round.SetValue(17);
                        }
                    }
                    break;
                case 17:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            counter2.SetValue(counter2 + 1);
                            bool continueOuterLoop = false;
                            if (counter2.GetCurrentValue() >= 5)
                            {
                                // Counter 2 has reached the end
                                // Update K if K2 > K
                                if (binops.CompResult() == SubComparison.ComparisonResult.LESS)
                                {
                                    bitK.SetValue(bitK2);
                                    rotation.SetValue(counter1);
                                }
                                continueOuterLoop = true;
                            }
                            else
                            {
                                // Counter 2 has not reached the end
                                // Break this iteration if K >= K2
                                if (binops.CompResult() == SubComparison.ComparisonResult.GREATER || binops.CompResult() == SubComparison.ComparisonResult.EQUAL)
                                {
                                    continueOuterLoop = true;
                                }
                                else
                                {
                                    // Continue with next iteration
                                    round.SetValue(13);
                                }
                            }
                            if (continueOuterLoop)
                            {
                                counter1.SetValue(counter1 + 1);
                                if (counter1.GetCurrentValue() >= 6)
                                {
                                    // Iteration is complete, finish
                                    round.SetValue(18);
                                }
                                else
                                {
                                    // Continue with next iteration
                                    round.SetValue(11);
                                }
                            }
                        }
                    }
                    break;
                case 18:
                    {
                        // Inequality computation is finished
                        // Setup global circuit and send synchronization beep
                        PinConfiguration pc = GetContractedPinConfiguration();
                        pc.SetToGlobal(0);
                        SetPlannedPinConfiguration(pc);
                        pc.SendBeepOnPartitionSet(0);
                        round.SetValue(19);
                    }
                    break;
                case 19:
                    {
                        // Check for synchronization beep
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Amoebots with k-bit = 1 send beep again
                            if (bitK)
                            {
                                SetPlannedPinConfiguration(pc);
                                pc.SendBeepOnPartitionSet(0);
                            }

                            round.SetValue(20);
                        }
                    }
                    break;
                case 20:
                    {
                        // Terminate with failure if no beep was received
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            finished.SetValue(true);
                            success.SetValue(false);
                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                        }
                        // Otherwise setup 3 global circuits to transmit rotation
                        else
                        {
                            SetupGlobalCircuitsBinary(pc);
                            SetPlannedPinConfiguration(pc);
                            int rot = rotation;
                            if (rot != -1)
                            {
                                if ((rot & 1) > 0)
                                    pc.SendBeepOnPartitionSet(0);
                                if ((rot & 2) > 0)
                                    pc.SendBeepOnPartitionSet(1);
                                if ((rot & 4) > 0)
                                    pc.SendBeepOnPartitionSet(2);
                            }
                        }
                        round.SetValue(21);
                    }
                    break;
                case 21:
                    {
                        // Receive rotation
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (rotation == -1)
                        {
                            int rot = 0;
                            if (pc.ReceivedBeepOnPartitionSet(0))
                                rot += 1;
                            if (pc.ReceivedBeepOnPartitionSet(1))
                                rot += 2;
                            if (pc.ReceivedBeepOnPartitionSet(2))
                                rot += 4;
                            rotation.SetValue(rot);
                        }
                        SetPlannedPinConfiguration(GetContractedPinConfiguration());

                        // Reset some values
                        counter1.SetValue(0);
                        counter2.SetValue(0);
                        for (int i = 0; i < 6; i++)
                        {
                            helperBits[i].SetValue(false);
                            helperMSBs[i].SetValue(false);
                        }
                        round.SetValue(22);
                    }
                    break;

                // PHASE 4: HALF-PLANE INTERSECTION
                case 22:
                    {
                        if (MyCornerType() != CornerType.INNER)
                        {
                            // Place marker at the chain start and write first bits and MSBs of origin distances
                            marker.SetValue(IsCounterStart());
                            WriteDistanceBits();
                            counter1.SetValue(1);
                            round.SetValue(23);
                        }
                        // Inner amoebots setup global circuit again and wait for sync beep
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            pc.SetToGlobal(0);
                            pc.SetPartitionSetColor(0, ColorData.Circuit_Colors[4]);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(27);
                        }
                    }
                    break;
                case 23:
                    {
                        // Remove marker and go to next round if marker has reached the end
                        int i = counter1;
                        bool reachedEnd = true;
                        for (int j = 0; j < 6; j++)
                        {
                            string d = shape.GetConvexHullDistanceString(rotation, j);
                            if (i < d.Length)
                            {
                                reachedEnd = false;
                                break;
                            }
                        }
                        if (reachedEnd)
                        {
                            marker.SetValue(false);
                            counter1.SetValue(0);
                            round.SetValue(24);
                        }
                        else
                        {
                            if (MyCornerType() != CornerType.INNER)
                            {
                                marker.SetValue(((SCConvexSystemParticle)GetNeighborAt(predDir)).marker);
                                WriteDistanceBits();
                                counter1.SetValue(counter1 + 1);
                            }
                        }
                    }
                    break;
                case 24:
                    {
                        // Finish if counter has reached 6
                        if (counter1 >= 6)
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            pc.SetToGlobal(0);
                            SetPlannedPinConfiguration(pc);
                            pc.SendBeepOnPartitionSet(0);
                            round.SetValue(27);
                        }
                        else
                        {
                            // Setup binary operation for multiplying d_i and k
                            StartBinOp(SubBinOps.Mode.MULT, helperBits[counter1], bitK, helperMSBs[counter1]);
                            round.SetValue(25);
                        }
                    }
                    break;
                case 25:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            // Write result into helper bits
                            helperBits[counter1].SetValue(binops.ResultBit());
                            // Setup binop to find MSB
                            StartBinOp(SubBinOps.Mode.MSB, helperBits[counter1].GetCurrentValue());
                            round.SetValue(26);
                        }
                    }
                    break;
                case 26:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            // Write result into helper MSBs
                            helperMSBs[counter1].SetValue(binops.IsMSB());
                            // Continue with next iteration
                            counter1.SetValue(counter1 + 1);
                            round.SetValue(24);
                        }
                    }
                    break;
                case 27:
                    {
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Received synchronization beep
                            counter1.SetValue(0);
                            round.SetValue(28);
                        }
                    }
                    break;
                case 28:
                    {
                        // Termination: counter1 >= 6
                        if (counter1 >= 6)
                        {
                            round.SetValue(31);
                            break;
                        }

                        // Setup PASC instance for the current direction
                        Direction d = DirectionHelpers.Cardinal(counter1);
                        Direction pascDir1 = d.Rotate60(-1);
                        Direction pascDir2 = d.Rotate60(-2);

                        bool leader = IsOnSide(d);
                        Direction nbrDir = Direction.NONE;
                        bool nbrConn1 = false;
                        bool nbrConn2 = false;
                        List<Direction> predecessors = new List<Direction>();
                        List<Direction> successors = new List<Direction>();
                        if (HasNeighborAt(pascDir1))
                            successors.Add(pascDir1);
                        if (HasNeighborAt(pascDir2))
                            successors.Add(pascDir2);
                        if (HasNeighborAt(pascDir1.Opposite()))
                            predecessors.Add(pascDir1.Opposite());
                        if (HasNeighborAt(pascDir2.Opposite()))
                            predecessors.Add(pascDir2.Opposite());
                        if (HasNeighborAt(d) || HasNeighborAt(d.Opposite()))
                        {
                            nbrDir = d;
                            if (HasNeighborAt(d))
                                nbrConn1 = true;
                            if (HasNeighborAt(d.Opposite()))
                                nbrConn2 = true;
                        }

                        pasc.Init(predecessors, successors, 0, 1, 0, 1, leader, true, nbrDir, nbrConn1, nbrConn2);

                        // Reset comparison result
                        inCurrentHalfPlane.SetValue(true);

                        // Setup PASC circuit and 2 global circuits
                        PinConfiguration pc = GetContractedPinConfiguration();
                        pasc.SetupPC(pc);
                        SetupGlobalCircuitsPASC(pc, pascDir1);
                        SetPlannedPinConfiguration(pc);

                        // Place marker on counter start
                        marker.SetValue(IsCounterStart());

                        // Start PASC
                        pasc.ActivateSend();

                        // Marker sends distance bit on first and MSB bit on second global circuit
                        if (marker.GetCurrentValue())
                        {
                            if (helperBits[counter1])
                                pc.SendBeepOnPartitionSet(2);
                            if (helperMSBs[counter1])
                                pc.SendBeepOnPartitionSet(3);
                        }
                        round.SetValue(29);
                    }
                    break;
                case 29:
                    {
                        // Receive PASC and two global beeps
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        pasc.ActivateReceive();
                        bool pascBit = pasc.GetReceivedBit() != 0;
                        bool beepBit = pc.ReceivedBeepOnPartitionSet(2);
                        bool beepMSB = pc.ReceivedBeepOnPartitionSet(3);

                        // Update comparison result
                        if (beepBit && !pascBit)
                            inCurrentHalfPlane.SetValue(false);
                        else if (!beepBit && pascBit)
                            inCurrentHalfPlane.SetValue(true);

                        // Setup PASC cutoff if MSB beep was received on second global circuit
                        if (beepMSB)
                        {
                            pasc.SetupCutoffCircuit(pc);
                            SetPlannedPinConfiguration(pc);
                            pasc.SendCutoffBeep();
                            round.SetValue(30);
                        }
                        // Otherwise: Continue
                        else
                        {
                            // Move marker ahead
                            if (MyCornerType() != CornerType.INNER)
                                marker.SetValue(((SCConvexSystemParticle)GetNeighborAt(predDir)).marker);

                            // Setup new circuits
                            pasc.SetupPC(pc);
                            SetupGlobalCircuitsPASC(pc, DirectionHelpers.Cardinal(counter1).Rotate60(-1));
                            SetPlannedPinConfiguration(pc);

                            // Send beeps
                            pasc.ActivateSend();
                            if (marker.GetCurrentValue())
                            {
                                if (helperBits[counter1])
                                    pc.SendBeepOnPartitionSet(2);
                                if (helperMSBs[counter1])
                                    pc.SendBeepOnPartitionSet(3);
                            }
                        }
                    }
                    break;
                case 30:
                    {
                        // Receive PASC cutoff and update comparison result
                        pasc.ReceiveCutoffBeep();
                        if (pasc.GetReceivedBit() != 0)
                            inCurrentHalfPlane.SetValue(true);

                        // Update half-plane intersection result
                        if (!inCurrentHalfPlane.GetCurrentValue())
                            inHalfPlaneIntersection.SetValue(false);

                        // Increment counter and repeat
                        counter1.SetValue(counter1 + 1);
                        round.SetValue(28);
                    }
                    break;
                case 31:
                    {
                        // Half-plane intersection is done
                        // Find out which neighbors are part of the solution and choose bottom-left as representative
                        if (inHalfPlaneIntersection)
                        {
                            bool isRepr = true;
                            foreach (Direction d in DirectionHelpers.Iterate60(Direction.W, 3))
                            {
                                if (HasNeighborAt(d) && ((SCConvexSystemParticle)GetNeighborAt(d)).inHalfPlaneIntersection)
                                {
                                    isRepr = false;
                                    break;
                                }
                            }
                            if (isRepr)
                                representative.SetValue(true);
                        }
                        marker.SetValue(false);
                        SetPlannedPinConfiguration(GetContractedPinConfiguration());
                        round.SetValue(32);
                    }
                    break;

                // PHASE 5: PLACING THE FINAL SHAPE
                case 32:
                    {
                        // Setup binop to find MSB of K (use K2 to store MSB)
                        bitK2.SetValue(false);
                        if (MyCornerType() != CornerType.INNER)
                        {
                            StartBinOp(SubBinOps.Mode.MSB, bitK);
                            round.SetValue(33);
                        }
                        // Inner amoebots wait in round 34
                        else
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            pc.SetToGlobal(0);
                            pc.SetPartitionSetColor(0, ColorData.Circuit_Colors[4]);
                            SetPlannedPinConfiguration(pc);
                            round.SetValue(34);
                        }
                    }
                    break;
                case 33:
                    {
                        bool isFinished = ActivateBinOp();
                        if (isFinished)
                        {
                            bitK2.SetValue(binops.IsMSB());
                            PinConfiguration pc = GetContractedPinConfiguration();
                            pc.SetToGlobal(0);
                            SetPlannedPinConfiguration(pc);
                            pc.SendBeepOnPartitionSet(0);
                            round.SetValue(34);
                        }
                    }
                    break;
                case 34:
                    {
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Setup shape construction subroutine
                            shapeConstr.Init(representative, rotation);
                            // Place marker at counter start
                            marker.SetValue(IsCounterStart());
                            // Start shape construction
                            pc = GetContractedPinConfiguration();
                            shapeConstr.SetupPinConfig(pc);
                            SetPlannedPinConfiguration(pc);
                            shapeConstr.ActivateSend();

                            round.SetValue(35);
                        }
                    }
                    break;
                case 35:
                    {
                        shapeConstr.ActivateReceive();
                        if (shapeConstr.IsFinished())
                        {
                            if (shapeConstr.IsSuccessful())
                            {
                                success.SetValue(true);
                            }
                            else
                            {
                                success.SetValue(false);
                            }
                            marker.SetValue(false);
                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                            finished.SetValue(true);
                        }
                        else
                        {
                            if (shapeConstr.ResetScaleCounter())
                            {
                                // Reset marker to counter start and wait
                                marker.SetValue(IsCounterStart());
                                round.SetValue(36);
                            }
                            else
                            {
                                PinConfiguration pc = GetContractedPinConfiguration();
                                shapeConstr.SetupPinConfig(pc);
                                SetPlannedPinConfiguration(pc);
                                if (shapeConstr.NeedScaleBit())
                                {
                                    shapeConstr.ActivateSend(marker.GetCurrentValue() && bitK, marker.GetCurrentValue() && bitK2);
                                    marker.SetValue(predDir != Direction.NONE && ((SCConvexSystemParticle)GetNeighborAt(predDir)).marker);
                                }
                                else
                                {
                                    shapeConstr.ActivateSend();
                                }
                            }
                        }
                    }
                    break;
                case 36:
                    {
                        PinConfiguration pc = GetContractedPinConfiguration();
                        shapeConstr.SetupPinConfig(pc);
                        SetPlannedPinConfiguration(pc);
                        if (shapeConstr.NeedScaleBit())
                        {
                            shapeConstr.ActivateSend(marker.GetCurrentValue() && bitK, marker.GetCurrentValue() && bitK2);
                            marker.SetValue(predDir != Direction.NONE && ((SCConvexSystemParticle)GetNeighborAt(predDir)).marker);
                        }
                        else
                        {
                            shapeConstr.ActivateSend();
                        }
                        round.SetValue(35);
                    }
                    break;
            }

            SetColor();
        }

        /// <summary>
        /// Helper setting the proper color.
        /// </summary>
        private void SetColor()
        {
            if (finished.GetCurrentValue() && !success.GetCurrentValue())
                SetMainColor(ColorData.Particle_Red);
            else if (marker.GetCurrentValue())
                SetMainColor(ColorData.Particle_Orange);
            else
            {
                int r = round.GetCurrentValue();
                // Inequality phase
                if (r >= 7 && r < 22)
                {
                    // Outer boundary amoebots display scale bits (K and K2)
                    if (MyCornerType() != CornerType.INNER)
                    {
                        bool k = bitK.GetCurrentValue();
                        bool k2 = bitK2.GetCurrentValue();
                        if (!k && !k2)
                            SetMainColor(ColorData.Particle_BlueDark);
                        else if (k && !k2)
                            SetMainColor(ColorData.Particle_Green);
                        else if (k2 && !k)
                            SetMainColor(ColorData.Particle_Blue);
                        else
                            SetMainColor(ColorData.Particle_Aqua);
                    }
                }
                // Half-plane intersection phase
                else if (r >= 28 && r < 32)
                {
                    if (representative.GetCurrentValue())
                        SetMainColor(Color.yellow);
                    else if (inHalfPlaneIntersection.GetCurrentValue())
                        SetMainColor(ColorData.Particle_Green);
                    else
                        SetMainColor(ColorData.Particle_Black);
                }
                // Shape construction phase
                else if (r >= 32)
                {
                    SubShapeConstruction.ShapeElement element = shapeConstr.ElementType();
                    if (element == SubShapeConstruction.ShapeElement.NODE)
                        SetMainColor(ColorData.Particle_Green);
                    else if (element == SubShapeConstruction.ShapeElement.EDGE)
                        SetMainColor(ColorData.Particle_Blue);
                    else if (element == SubShapeConstruction.ShapeElement.FACE)
                        SetMainColor(ColorData.Particle_Aqua);
                    else
                        SetMainColor(ColorData.Particle_Black);
                }
                // Any other phase
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
        /// Helper for writing the bits and MSBs of the
        /// origin distances into the marker position.
        /// </summary>
        private void WriteDistanceBits()
        {
            if (marker.GetCurrentValue())
            {
                int i = counter1.GetCurrentValue();
                for (int j = 0; j < 6; j++)
                {
                    string dist = shape.GetConvexHullDistanceString(rotation.GetCurrentValue(), j);
                    if (i < dist.Length)
                    {
                        helperBits[j].SetValue(dist[i] == '1');
                        if (i == dist.Length - 1)
                        {
                            helperMSBs[j].SetValue(true);
                        }
                    }
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
        /// <param name="d">The direction in which PASC is currently running.
        /// It is assumed that PASC uses the "right lane" in this and the two
        /// adjacent directions.</param>
        private void SetupGlobalCircuitsPASC(PinConfiguration pc, Direction d)
        {
            bool[] invertedDirs = new bool[6];
            invertedDirs[d.ToInt()] = true;
            invertedDirs[d.Rotate60(1).ToInt()] = true;
            invertedDirs[d.Rotate60(-1).ToInt()] = true;

            pc.SetStarConfig(0, invertedDirs, 2);
            pc.SetStarConfig(1, invertedDirs, 3);
            pc.SetPartitionSetPosition(2, new Vector2((d.ToInt() + 1.5f) * 60, 0.65f));
            pc.SetPartitionSetPosition(3, new Vector2((d.ToInt() + 4.5f) * 60, 0.65f));
        }

        /// <summary>
        /// Helper setting up three global circuits on partition sets
        /// 0, 1 and 2 that can be used to transmit binary numbers between
        /// 0 and 7.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        private void SetupGlobalCircuitsBinary(PinConfiguration pc)
        {
            bool[] invertedDirs = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(0, invertedDirs, 0);
            pc.SetStarConfig(1, invertedDirs, 1);
            pc.SetStarConfig(2, invertedDirs, 2);
        }

        /// <summary>
        /// Helper procedure for initializing and starting a binary
        /// operation subroutine.
        /// </summary>
        /// <param name="mode">The operation to be initialized.</param>
        /// <param name="bitA">The bit of the <c>a</c> operand.</param>
        /// <param name="bitB">The bit of the optional <c>b</c> operand.</param>
        /// <param name="msbA">The optional MSB flag for <c>a</c>
        /// (only required for division and multiplication).</param>
        private void StartBinOp(SubBinOps.Mode mode, bool bitA, bool bitB = false, bool msbA = false)
        {
            binops.Init(mode, bitA, IsCounterStart() ? Direction.NONE : predDir, IsCounterEnd() ? Direction.NONE : succDir, bitB, msbA);
            PinConfiguration pc = GetContractedPinConfiguration();
            binops.SetupPinConfig(pc);
            SetPlannedPinConfiguration(pc);
            binops.ActivateSend();
        }

        /// <summary>
        /// Helper for code used to run the binary operation
        /// subroutine on the outer boundary. Runs the receiving
        /// part of the subroutine and returns <c>true</c> if it
        /// is finished. Otherwise, sets up the pin configuration
        /// and sends the beeps. Does not check whether the
        /// amoebot is actually on the outer boundary.
        /// </summary>
        /// <param name="pc">The pin configuration to be used.</param>
        private bool ActivateBinOp()
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
            if (!sc.shape.IsConsistent())
            {
                Log.Warning("Shape is inconsistent!");
            }
            else
            {
                sc.shape.GenerateTraversal();
                sc.shape.GenerateConvexHull();
                AS2.UI.LineDrawer.Instance.Clear();
                sc.shape.Draw(Vector2Int.zero);
                sc.shape.DrawConvexHull(Vector2Int.zero);
                AS2.UI.LineDrawer.Instance.SetTimer(30);
                SCConvexSystemParticle.shape = sc.shape;
            }

            // Construct the system
            int nPlaced = 0;
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
                        nPlaced++;
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
                        nPlaced++;
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
                        nPlaced++;
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
                        nPlaced++;
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
                        nPlaced++;
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
            Log.Debug("Generated shape has " + nPlaced + " amoebots");
        }
    }

} // namespace AS2.Algos.SCConvexSystem
