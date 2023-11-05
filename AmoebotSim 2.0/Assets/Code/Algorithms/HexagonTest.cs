using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;
using AS2.Subroutines.PASC;

namespace AS2.Algos.HexagonTest
{

    /// <summary>
    /// Testing the empty hexagon approach for a single empty region.
    /// 
    /// START:
    /// Round 0:
    ///     - Setup axis circuits
    ///     - Extreme boundary particles send beeps in both directions
    /// Round 1:
    ///     - Particles receive axis beeps
    ///     - Particles that received two beeps check which direction the beeps came from
    ///       and turn into corner particles if the combination is right
    ///     - Setup axis circuits again (but corners and extreme particles are now different)
    ///       -> Corners send/block beeps to identify hexagon edges
    /// Round 2:
    ///     - Hexagon edge particles identify themselves
    ///     - Setup hexagon's internal circuit
    ///     - Beep on internal circuit
    /// Round 3:
    ///     - Internal particles receive beep and become inactive
    /// 
    /// SECTOR_IDENT (independent of scaling factor):
    /// Round 0:
    ///     - Setup axis circuits (except hexagon particles)
    ///     - Corner particles send beeps in two axis directions
    ///       -> Use two lanes to differentiate between the beeps in the two directions
    /// Round 1:
    ///     - Non-hexagon particles receive beeps and store axis IDs
    ///     - All axis particles disconnect their circuits (back to singleton)
    ///     - All even axis particles (except intersection points) send beep inside the triangle, parallel to the base line
    /// Round 2:
    ///     - Some axis particles receive beep from opposite triangle side
    ///       -> They remember in generic bool 1
    ///     - Construct circuit along the right triangle side, stopping at axis particles that have received no beep
    ///     - Corner points send beep along this circuit
    /// Round 3:
    ///     - Some right triangle axis particles receive beep from corner point
    ///       -> If we receive such a beep and our successor on the axis has not received the beep in the previous round
    ///          (generic bool 1) AND is not the intersection particle:
    ///          Then the triangle is not filled and we are the extreme point, send beep to left triangle side
    ///          And send beep back to corner point
    ///     - If the intersection point receives the beep from the corner point: Send beep back
    ///       -> This beep tells the corner point that there is an extreme particle other than the corner itself
    /// Round 4:
    ///     - Left triangle side particles can receive beep from right side, telling them to be triangle limit
    ///     - Corners should receive beep from extreme point
    ///       -> If not, then the corner itself is the extreme point
    ///     - Setup axis circuits again on all particles
    ///     - Triangle limit particles send beeps on each limited axis, again using two-lane principle to distinguish
    ///       between the two possible axes of the same direction
    /// Round 5:
    ///     - Limited triangle beeps are received by some particles
    ///       -> These particles now have to act as if they did not have a neighbor in a certain direction
    ///          (or even two directions in this case)
    ///     - Setup multi-axis circuits again, this time using virtual holes as delimiters
    ///     - Axis particles send beeps parallel to triangle bases towards the inside
    ///       -> Idea: Angle of incoming beeps determines the sector uniquely
    /// Round 6:
    ///     - Particles receive beeps sent by axis circuits
    ///     - If we received two beeps, we can determine the angle between the directions and find out in which sector we are
    ///     - If we received less than two beeps, we are not part of any sector / hidden by a hole
    ///       -> This means we become a hole and are inactive from now on
    ///     - Establish global circuit excluding holes
    ///     - Corner particles send beep on global circuit
    /// Round 7:
    ///     - Particles that did not receive the global beep become holes
    ///     - Move to next phase
    /// 
    /// CANDIDATE_LIMIT (dependent on scaling factor):
    /// - Repeat this for the 3 axes and their corresponding sectors
    /// Round 0:
    ///     - Establish simple axis circuits
    ///     - Particles in the sectors that have a hole on one side corresponding to the hexagon side send beep
    ///       in the opposite direction
    ///       -> Particles that have holes on both sides immediately become limited
    /// Round 1:
    ///     - Particles that received the beep setup PASC circuits
    ///     - Send first PASC beeps
    /// Round 2:
    ///     - Receive PASC beeps
    ///     - Establish 4-way global circuit, send beeps according to counters and PASC status
    /// Round 3:
    ///     - Update comparison result
    ///     - If we are completely finished: Use comparison result to find out which segments do not work (end points determine this)
    ///       -> Then establish axis circuit again and send beep where the segments are too small
    ///     - If comparisons are finished but PASC is not: Start PASC cutoff and go to round 4
    ///     - Otherwise setup PASC circuits again and continue
    /// Round 4:
    ///     - Receive PASC cutoff
    ///     - Use final comparison to determine result (same as when we finish in round 3)
    /// Round 5:
    ///     - Receive elimination beep and become limited hole
    ///     - Setup repetition for next direction, repeat this whole process for all three axes
    ///     - After the last iteration: Go to next phase
    /// 
    /// CANDIDATE_SHIFT:
    /// Round 0:
    ///     - All particles in sectors 6, 12 and 13, on axes 1 and 8, and corner 0 become candidates (for both sides)
    ///     
    /// Round 1-5: Distance check in SSW and E direction at the same time
    /// 
    /// TODO
    /// 
    /// 
    /// 
    /// 
    /// Round 2:
    ///     - Particles receive beeps from interval start points
    ///     - All particles receiving the beep setup PASC
    ///       - Use PASC 1 for left candidates and PASC 2 for right candidates
    ///     - Interval start points start with first PASC step
    ///     - Remaining particles chill
    ///     - Additionally: Corner particles initialize two binary counter iterators
    ///       -> Simply set two counters to 0, corresponding to the two binary strings representing
    ///          edge lengths 0 and 1
    /// Round 3:
    ///     - Receive PASC beeps (stores bits as well)
    ///     - Setup 3-way global circuit
    ///     - Beep on first circuit if we became passive in this round OR one of the side lengths has not been scanned completely yet
    ///     - Send first counter bit on second circuit
    ///     - Send second counter bit on third circuit
    /// Round 4:
    ///     - Receive beep on global circuit 0 if we have to continue
    ///       -> Otherwise: Move on to next step
    ///       - PASC procedure is finished, comparison results are ready
    ///       - Participants with EQUAL result become interval starts, previous interval starts remove that state
    ///       - Establish simple axis circuits again (like in Round 1) using interval ends
    ///       - Send beep in shifting direction and go to round 5
    ///     - Receive the two edge length bits and update comparison results
    ///     - Setup PASC circuits again, send beeps and go back to round 3
    /// Round 5:
    ///     - Particles receive beeps from interval end points (similar to round 2)
    ///     - All particles receiving the beep setup PASC
    ///         - Again PASC 1 for left candidates and PASC 2 for right candidates
    ///     - Interval end points start with first PASC step
    ///     - Remaining particles chill
    ///     - Corner particles initialize the counters again
    /// Round 6:
    ///     - Same as round 3
    /// Round 7:
    ///     - Same as round 4, but for interval end points
    ///     - And we proceed to round 8 if no continuation beep was received
    ///         - Setup simple axis circuits, using interval starts and ends as breaks
    ///         - Interval ends send beeps in the last shifting direction
    /// Round 8:
    ///     - All particles receiving the beeps (including interval starts and ends) become candidates
    ///       -> All others are non-candidates
    ///     - Setup simple axis circuit
    ///     - Axis 10 sends beep in NNE direction and axis 11 sends beep in W direction
    /// Round 9:
    ///     - All candidates receiving the beep sent by the axes become non-candidates (except the axes themselves)
    /// </summary>
    public class HexagonTestParticle : ParticleAlgorithm
    {
        // Target hexagon size (in binary)
        public static string hexLine0;     // Top
        public static string hexLine1;     // Top Left
        public static string hexLine2;     // Bot Left
        public static string hexLine3;     // Bottom
        public static string hexLine4;     // Bot Right
        public static string hexLine5;     // Top Right

        enum Phase
        {
            START,              // Find corners, edges and internal particles
            SECTOR_IDENT,       // Identify particles on the various axes and in the relevant sectors
            CANDIDATE_LIMIT,    // Rule out areas where the segments are too short to enable candidate shift
            CANDIDATE_SHIFT     // Find candidates and shift them around
        }

        enum Comparison
        {
            EQUAL,
            LESS,
            GREATER
        }

        private static readonly float pSetDistance = 0.7f;

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Hexagon Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(HexagonTestInitializer).FullName;

        ParticleAttribute<string> hexBoundIndex;

        ParticleAttribute<Phase> phase;
        ParticleAttribute<int> round;

        ParticleAttribute<int> hexCornerIndex;      // -1 = No corner; 0 = Top left, 1 = Left, etc.
        ParticleAttribute<int> hexEdgeIndex;        // -1 = No edge; 0 = Top, 1 = Top left, etc.
        ParticleAttribute<bool> isInternal;         // true for particles inside the initial hexagon

        ParticleAttribute<int> axisIndex1;          // Corner axes (extended hexagon sides) are numbered 0,...,11 (numbering indicates direction of origin and pin)
        ParticleAttribute<int> axisIndex2;          // A particle can be on two axes

        ParticleAttribute<bool> isTriangleLimit;    // Whether we are on an axis and we are limiting the triangle
        ParticleAttribute<bool>[] virtualHoles;     // Directions in which we have no neighbor (either due to actual hole or because we have to behave as if there was a hole)
        ParticleAttribute<bool> isHole;             // Whether we are a virtual hole (basically excluded from the rest of the procedure)
        ParticleAttribute<int> sectorIndex;         // Which sector we are in (-1: no sector; 0-5: triangles, 6-11: single candidate areas, 12-17: intersections)

        ParticleAttribute<bool> limited;            // Whether we have been excluded from becoming a candidate by segment length limits

        ParticleAttribute<bool> isCandidateL;       // Whether or not we are a candidate for the left (right) side
        ParticleAttribute<bool> isCandidateR;
        ParticleAttribute<bool> isIntervalStartL;   // Whether or not we are a candidate interval start or end point
        ParticleAttribute<bool> isIntervalEndL;
        ParticleAttribute<bool> isIntervalStartR;
        ParticleAttribute<bool> isIntervalEndR;
        ParticleAttribute<bool> pasc1Participant;   // Whether or not we participate in PASC1/2
        ParticleAttribute<bool> pasc2Participant;
        ParticleAttribute<int> counter1Idx;         // Index in the binary counter specifying edge length 1
        ParticleAttribute<int> counter2Idx;
        ParticleAttribute<Comparison> comparison1;  // Counter comparison results 1 and 2
        ParticleAttribute<Comparison> comparison2;

        // Generic attributes that can be used in multiple phases
        ParticleAttribute<bool> genericBool1;

        // Subroutines
        SubPASC pasc1;
        SubPASC pasc2;

        public HexagonTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            hexBoundIndex = CreateAttributeString("Hexagon boundary idx", string.Empty);
            phase = CreateAttributeEnum<Phase>("Phase", Phase.START);
            round = CreateAttributeInt("Round", 0);

            hexCornerIndex = CreateAttributeInt("Hex Corner Idx", -1);
            hexEdgeIndex = CreateAttributeInt("Hex Edge Idx", -1);
            isInternal = CreateAttributeBool("Internal", false);

            axisIndex1 = CreateAttributeInt("Axis Idx 1", -1);
            axisIndex2 = CreateAttributeInt("Axis Idx 2", -1);

            isTriangleLimit = CreateAttributeBool("Triangle Limit", false);
            virtualHoles = new ParticleAttribute<bool>[6];
            for (int i = 0; i < 6; i++)
                virtualHoles[i] = CreateAttributeBool("Virtual Hole " + i, false);
            isHole = CreateAttributeBool("Is Hole", false);
            sectorIndex = CreateAttributeInt("Sector Idx", -1);

            limited = CreateAttributeBool("Limited", false);

            isCandidateL = CreateAttributeBool("Candidate L", false);
            isCandidateR = CreateAttributeBool("Candidate R", false);
            isIntervalStartL = CreateAttributeBool("Interval Start L", false);
            isIntervalEndL = CreateAttributeBool("Interval End L", false);
            isIntervalStartR = CreateAttributeBool("Interval Start R", false);
            isIntervalEndR = CreateAttributeBool("Interval End R", false);
            pasc1Participant = CreateAttributeBool("PASC 1 Part.", false);
            pasc2Participant = CreateAttributeBool("PASC 2 Part.", false);
            counter1Idx = CreateAttributeInt("Counter 1 Idx", 0);
            counter2Idx = CreateAttributeInt("Counter 2 Idx", 0);
            comparison1 = CreateAttributeEnum<Comparison>("Comparison 1", Comparison.EQUAL);
            comparison2 = CreateAttributeEnum<Comparison>("Comparison 2", Comparison.EQUAL);

            genericBool1 = CreateAttributeBool("Generic Bool 1", false);

            pasc1 = new SubPASC(p);
            pasc2 = new SubPASC(p);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(string hexBoundIndex = "000")
        {
            this.hexBoundIndex.SetValue(hexBoundIndex);
            if (!hexBoundIndex.Equals("000"))
                SetMainColor(ColorData.Particle_Blue);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return isInternal;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (isInternal)
                return;

            switch (phase.GetValue())
            {
                case Phase.START:
                    ActivateStart();
                    break;
                case Phase.SECTOR_IDENT:
                    ActivateSector();
                    break;
                case Phase.CANDIDATE_LIMIT:
                    ActivateCandidateLimit();
                    break;
                case Phase.CANDIDATE_SHIFT:
                    ActivateCandidateShift();
                    break;
                default:
                    break;
            }
        }

        void ActivateStart()
        {
            if (round == 0)
            {
                PinConfiguration pc = SetupAxisCircuit(0);

                for (int d = 0; d < 3; d++)
                {
                    Direction dir = DirectionHelpers.Cardinal(d);
                    bool isExtreme = false;
                    if (d == 0 && !hexBoundIndex.GetValue()[1].Equals('0') ||
                        d == 1 && !hexBoundIndex.GetValue()[0].Equals('0') ||
                        d == 2 && !hexBoundIndex.GetValue()[2].Equals('0'))
                        isExtreme = true;

                    if (isExtreme)
                    {
                        pc.SendBeepOnPartitionSet(d);
                        pc.SendBeepOnPartitionSet(d + 3);
                    }
                }

                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                // Check where we have received beeps
                PinConfiguration pc = GetCurrentPinConfiguration();
                bool[] receivedBeeps = new bool[6];     // i = true <=> received beep from direction i
                for (int d = 0; d < 6; d++)
                {
                    Direction dir = DirectionHelpers.Cardinal(d);
                    receivedBeeps[d] = pc.GetPinAt(dir, 3).PartitionSet.ReceivedBeep();
                }

                // Check which corner we are
                for (int i = 0; i < 6; i++)
                {
                    if (receivedBeeps[i] && receivedBeeps[(i + 4) % 6])
                    {
                        hexCornerIndex.SetValue(i);
                        break;
                    }
                }

                if (hexCornerIndex.GetCurrentValue() != -1)
                    SetMainColor(ColorData.Particle_BlueDark);

                // Setup new pin configuration
                pc = SetupAxisCircuit(1);

                // Corners send beeps in their respective directions
                int hexIdx = hexCornerIndex.GetCurrentValue();
                if (hexIdx != -1)
                {
                    pc.GetPinAt(DirectionHelpers.Cardinal(hexIdx), 0).PartitionSet.SendBeep();
                }

                round.SetValue(round + 1);
            }
            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (hexCornerIndex == -1)
                {
                    // Check where we have received beeps
                    for (int d = 0; d < 6; d++)
                    {
                        if (pc.GetPinAt(DirectionHelpers.Cardinal(d).Opposite(), 3).PartitionSet.ReceivedBeep())
                        {
                            hexEdgeIndex.SetValue(d);
                            SetMainColor(ColorData.Particle_Blue);
                            break;
                        }
                    }
                }

                // Setup hexagon's internal circuit and beep
                pc = SetupInternalCircuit();
                if (hexCornerIndex != -1)
                    pc.SendBeepOnPartitionSet(0);

                round.SetValue(round + 1);
            }
            else if (round == 3)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (hexCornerIndex == -1 && hexEdgeIndex == -1)
                {
                    if (pc.ReceivedBeepOnPartitionSet(0))
                    {
                        // We are an internal particle
                        SetMainColor(Color.gray);
                        isInternal.SetValue(true);
                    }
                    else
                    {
                        // We are an external particle
                    }
                }

                pc.SetToSingleton();
                SetPlannedPinConfiguration(pc);

                phase.SetValue(Phase.SECTOR_IDENT);
                round.SetValue(0);
            }
        }

        void ActivateSector()
        {

            if (round == 0)
            {
                // Setup axis circuit again
                PinConfiguration pc = SetupMultiAxisCircuit();

                // Corners send beeps
                if (hexCornerIndex != -1)
                {
                    Direction dirPrimary = DirectionHelpers.Cardinal((hexCornerIndex + 1) % 6);
                    Direction dirSecondary = DirectionHelpers.Cardinal((hexCornerIndex + 3) % 6);

                    // On pin 0 in primary direction and pin 1 in secondary direction
                    // Also color these circuits differently to make them stand out more
                    int pSetP = pc.GetPinAt(dirPrimary, 0).PartitionSet.Id;
                    int pSetS = pc.GetPinAt(dirSecondary, 1).PartitionSet.Id;
                    pc.SetPartitionSetColor(pSetP, Color.yellow);
                    pc.SetPartitionSetColor(pSetS, Color.yellow);
                    pc.SendBeepOnPartitionSet(pSetP);
                    pc.SendBeepOnPartitionSet(pSetS);
                }

                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Find out if we are on an axis and which one(s)
                if (hexCornerIndex == -1 && hexEdgeIndex == -1)     // Only non-hexagon particles need to do this
                {
                    for (int d = 0; d < 6; d++)
                    {
                        Direction dir = DirectionHelpers.Cardinal(d);
                        // It is only possible to receive one beep per direction
                        int beepIdx = -1;
                        if (pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                            beepIdx = 2 * d;
                        if (pc.GetPinAt(dir, PinsPerEdge - 2).PartitionSet.ReceivedBeep())
                            beepIdx = 2 * d + 1;

                        if (beepIdx != -1)
                        {
                            if (axisIndex1.GetCurrentValue() == -1)
                            {
                                axisIndex1.SetValue(beepIdx);
                            }
                            else
                            {
                                axisIndex2.SetValue(beepIdx);
                                break;
                            }
                        }
                    }

                    if (axisIndex1.GetCurrentValue() != -1)
                    {
                        // Disconnect circuits
                        pc.SetToSingleton();
                        SetPlannedPinConfiguration(pc);

                        if (axisIndex2.GetCurrentValue() != -1)
                        {
                            // We are on two axes!
                        }
                        else
                        {
                            // We are on one axis
                            // If we are on an even axis: Send beep in parallel to triangle base (towards the other side of the triangle)
                            if (axisIndex1.GetCurrentValue() % 2 == 0)
                            {
                                int dir = (axisIndex1.GetCurrentValue() / 2 + 2) % 6;
                                pc.SendBeepOnPartitionSet(pc.GetPinAt(DirectionHelpers.Cardinal(dir), 0).PartitionSet.Id);
                            }
                        }
                        UpdateColor();
                    }
                }

                round.SetValue(round + 1);
            }
            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Only particles on the right axis of a triangle have to change their pin configuration
                if (axisIndex1 != -1 && axisIndex1 % 2 == 1 && axisIndex2 == -1)
                {
                    // Check if beep was received
                    int d = (axisIndex1 / 2 + 4) % 6;
                    if (pc.GetPinAt(DirectionHelpers.Cardinal(d), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                    {
                        genericBool1.SetValue(true);    // Remember that we received the beep
                        // Connect the axis circuit
                        Direction dir = DirectionHelpers.Cardinal(axisIndex1 / 2);
                        pc.MakePartitionSet(new int[] {
                            pc.GetPinAt(dir, PinsPerEdge - 1).Id,
                            pc.GetPinAt(dir.Opposite(), 0).Id
                        }, 0);
                        pc.ResetPartitionSetPlacement();
                        SetPlannedPinConfiguration(pc);
                    }
                }

                // Corner particles send beep on this circuit
                if (hexCornerIndex != -1)
                {
                    int d = (hexCornerIndex + 3) % 6;
                    SetPlannedPinConfiguration(pc);
                    pc.GetPinAt(DirectionHelpers.Cardinal(d), 0).PartitionSet.SendBeep();
                }

                round.SetValue(round + 1);
            }
            else if (round == 3)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Particle on right axis on triangle checks for received beep from corner
                if (axisIndex1 != -1 && axisIndex1 % 2 == 1 && axisIndex2 == -1)
                {
                    // Check if beep was received
                    Direction dirToCorner = DirectionHelpers.Cardinal(axisIndex1 / 2);
                    if (pc.GetPinAt(dirToCorner, PinsPerEdge - 1).PartitionSet.ReceivedBeep() && genericBool1)
                    {
                        // Check whether our successor on the line has received the previous beep or is the triangle corner
                        bool successorIsNotValid = false;
                        if (!HasNeighborAt(dirToCorner.Opposite()))
                            successorIsNotValid = true;
                        else
                        {
                            HexagonTestParticle nbr = (HexagonTestParticle)GetNeighborAt(dirToCorner.Opposite());
                            if (nbr.axisIndex2 == -1 && !nbr.genericBool1)
                                successorIsNotValid = true;
                        }

                        if (successorIsNotValid)
                        {
                            isTriangleLimit.SetValue(true);
                            // Our successor has not received the beep and is not the triangle corner
                            // => Send a beep to our counterpart on the left side of the triangle
                            // Also send beep back to corner
                            pc.SetToSingleton();
                            SetPlannedPinConfiguration(pc);
                            pc.GetPinAt(dirToCorner.Rotate60(4), 0).PartitionSet.SendBeep();
                            pc.GetPinAt(dirToCorner, PinsPerEdge - 1).PartitionSet.SendBeep();
                        }
                    }
                }

                // Axis intersection particle checks for received beep and sends reply if it was received
                if (axisIndex2 != -1)
                {
                    int oddAxis = axisIndex1 % 2 == 1 ? axisIndex1 : axisIndex2;
                    int partitionSet = pc.GetPinAt(DirectionHelpers.Cardinal(oddAxis / 2), PinsPerEdge - 1).PartitionSet.Id;
                    if (pc.ReceivedBeepOnPartitionSet(partitionSet))
                    {
                        // We have received the beep, send reply
                        SetPlannedPinConfiguration(pc);
                        pc.SendBeepOnPartitionSet(partitionSet);
                    }
                }

                round.SetValue(round + 1);
            }
            else if (round == 4)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Left triangle axis particles check for beep from right side
                if (axisIndex1 != -1 && axisIndex1 % 2 == 0 && axisIndex2 == -1)
                {
                    int d = (axisIndex1 / 2 + 2) % 6;
                    if (pc.GetPinAt(DirectionHelpers.Cardinal(d), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                    {
                        // We received a beep! => We are the triangle's limit
                        isTriangleLimit.SetValue(true);
                    }
                }

                // Corner particles check for beep from axis
                if (hexCornerIndex != -1)
                {
                    int d = (hexCornerIndex + 3) % 6;
                    if (!pc.GetPinAt(DirectionHelpers.Cardinal(d), 0).PartitionSet.ReceivedBeep())
                    {
                        // We received no beep...
                        // That means we are the extreme point of the triangle
                        isTriangleLimit.SetValue(true);
                    }
                }

                // Setup axis pin configuration again
                pc = SetupMultiAxisCircuit(1);

                // Triangle limit particles send beep according to the triangle they belong to
                if (isTriangleLimit.GetCurrentValue())
                {
                    if (hexCornerIndex != -1)
                    {
                        // Corner particle
                        int d = (hexCornerIndex + 1) % 6;
                        Direction dir = DirectionHelpers.Cardinal(d);
                        // Corners 0, 1 and 5 send on outer pins, 2, 3 and 4 send on inner pins
                        if (hexCornerIndex < 2 || hexCornerIndex == 5)
                        {
                            pc.GetPinAt(dir, 0).PartitionSet.SendBeep();
                            pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.SendBeep();
                        }
                        else
                        {
                            pc.GetPinAt(dir, 1).PartitionSet.SendBeep();
                            pc.GetPinAt(dir, PinsPerEdge - 2).PartitionSet.SendBeep();
                        }
                    }
                    else if (axisIndex1 % 2 == 0)
                    {
                        // No corner particle
                        // Only even axis particles have to send beep
                        Direction dir = DirectionHelpers.Cardinal((axisIndex1 / 2 + 2) % 6);
                        // Axes 0, 8 and 10 send on outer pins, 2, 4 and 6 on inner pins
                        if (axisIndex1 == 0 || axisIndex1 > 6)
                        {
                            pc.GetPinAt(dir, 0).PartitionSet.SendBeep();
                            pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.SendBeep();
                        }
                        else
                        {
                            pc.GetPinAt(dir, 1).PartitionSet.SendBeep();
                            pc.GetPinAt(dir, PinsPerEdge - 2).PartitionSet.SendBeep();
                        }
                    }
                }

                round.SetValue(round + 1);
            }
            else if (round == 5)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // All particles can receive a beep and must behave as if there was no
                // neighbor in a given direction, depending on where the beep came from
                for (int d = 0; d < 3; d++)
                {
                    if (pc.GetPinAt(DirectionHelpers.Cardinal(d), 0).PartitionSet.ReceivedBeep())
                    {
                        // Received beep on outer circuit
                        virtualHoles[(d + 1) % 6].SetValue(true);
                        virtualHoles[(d + 2) % 6].SetValue(true);
                    }
                    else if (pc.GetPinAt(DirectionHelpers.Cardinal(d), 1).PartitionSet.ReceivedBeep())
                    {
                        // Received beep on inner circuit
                        virtualHoles[(d + 4) % 6].SetValue(true);
                        virtualHoles[(d + 5) % 6].SetValue(true);
                    }
                }

                // Establish circuits along all axes again (only outside), excluding hexagon and using virtual holes as delimiters
                pc = SetupMultiAxisDirectionCircuit(0);

                // Axis particles send beeps parallel to base line towards the inside
                foreach (int idx in new int[] { axisIndex1, axisIndex2 })
                {
                    if (idx == -1)
                        continue;

                    Direction dirToCorner = DirectionHelpers.Cardinal(idx / 2);
                    Direction dir;
                    int pinIdx = 0;
                    // Axes belonging to top and left triangles use outer circuits, others use inner
                    if (idx % 2 == 0)
                    {
                        // Left axis
                        dir = dirToCorner.Rotate60(2);
                        if (idx == 2 || idx == 4 || idx == 6)
                            pinIdx = 1;
                    }
                    else
                    {
                        // Right axis
                        dir = dirToCorner.Rotate60(4);
                        if (idx == 5 || idx == 7 || idx == 9)
                            pinIdx = 1;
                    }

                    pc.GetPinAt(dir, pinIdx).PartitionSet.SendBeep();
                }

                round.SetValue(round + 1);
            }
            else if (round == 6)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // All non-hexagon particles determine whether they are in a valid sector
                if (hexCornerIndex == -1 && hexEdgeIndex == -1)
                {
                    int receiveDir1 = -1;
                    int receiveDir2 = -1;
                    for (int d = 0; d < 6; d++)
                    {
                        Direction dir = DirectionHelpers.Cardinal(d);
                        bool received = false;
                        // We have to check both pins of a direction due to the multi-axis circuit
                        if (pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.ReceivedBeep() ||
                            pc.GetPinAt(dir, PinsPerEdge - 2).PartitionSet.ReceivedBeep())
                        {
                            received = true;
                        }
                        else if (axisIndex1 != -1)
                        {
                            // If we are an axis particle: Check if we sent a beep in the opposite direction
                            if (pc.GetPinAt(dir.Opposite(), 0).PartitionSet.ReceivedBeep() ||
                                pc.GetPinAt(dir.Opposite(), 1).PartitionSet.ReceivedBeep())
                                received = true;
                        }

                        if (received)
                        {
                            if (receiveDir1 == -1)
                                receiveDir1 = d;
                            else
                            {
                                receiveDir2 = d;
                                break;
                            }
                        }
                    }
                    if (receiveDir2 == -1)
                    {
                        // Received less than two beeps => We are hidden by a hole, so we are just as good as a hole
                        isHole.SetValue(true);
                        // We also lose our status as axis particle in this case
                        axisIndex1.SetValue(-1);
                        axisIndex2.SetValue(-1);
                    }
                    else
                    {
                        // Determine which sector we are in
                        // If the two directions are opposite of each other, we are inside one of the adjacent triangles
                        // (no candidates can be there)
                        if ((receiveDir1 + 3) % 6 == receiveDir2)
                        {
                            bool rcvOnOuterPin = pc.GetPinAt(DirectionHelpers.Cardinal(receiveDir1), PinsPerEdge - 1).PartitionSet.ReceivedBeep() ||
                                pc.GetPinAt(DirectionHelpers.Cardinal(receiveDir2), 0).PartitionSet.ReceivedBeep();
                            if (rcvOnOuterPin)
                                sectorIndex.SetValue(receiveDir1);
                            else
                                sectorIndex.SetValue(receiveDir2);
                        }
                        // If the angle between the directions is 120 degrees, we are in a single-candidate sector
                        else if ((receiveDir1 + 2) % 6 == receiveDir2 || (receiveDir2 + 2) % 6 == receiveDir1)
                        {
                            // 0,4 -> 6
                            // 1,5 -> 7
                            // 0,2 -> 8
                            // 1,3 -> 9
                            // 2,4 -> 10
                            // 3,5 -> 11
                            if (receiveDir1 > 1 || receiveDir2 == 3)
                                sectorIndex.SetValue(receiveDir1 + 8);
                            else if (receiveDir2 == 2)
                                sectorIndex.SetValue(8);
                            else
                                sectorIndex.SetValue(receiveDir1 + 6);
                        }
                        // If the angle between the directions is 60 degrees, we are in the intersection of two candidate sectors
                        else if ((receiveDir1 + 1) % 6 == receiveDir2 || (receiveDir2 + 1) % 6 == receiveDir1)
                        {
                            // 4,5 -> 12
                            // 0,5 -> 13
                            // 0,1 -> 14
                            // 1,2 -> 15
                            // 2,3 -> 16
                            // 3,4 -> 17
                            if (receiveDir2 < 5)
                                sectorIndex.SetValue(receiveDir1 + 14);
                            else if (receiveDir1 == 4)
                                sectorIndex.SetValue(12);
                            else
                                sectorIndex.SetValue(13);
                        }
                    }
                    UpdateColor();
                }

                // Setup global circuit excluding holes
                if (isHole.GetCurrentValue())
                    pc.SetToSingleton();
                else
                    pc.SetToGlobal(0);

                pc.ResetPartitionSetPlacement();
                SetPlannedPinConfiguration(pc);

                // Corners send beep
                if (hexCornerIndex != -1)
                    pc.SendBeepOnPartitionSet(0);

                round.SetValue(round + 1);
            }
            else if (round == 7)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // All non-hexagon and non-hole particles check whether they received the global beep
                if (hexCornerIndex == -1 && hexEdgeIndex == -1 && !isHole)
                {
                    if (!pc.ReceivedBeepOnPartitionSet(0))
                    {
                        // We are not part of a valid sector: Become hole and lose axis status
                        isHole.SetValue(true);
                        axisIndex1.SetValue(-1);
                        axisIndex2.SetValue(-1);
                        UpdateColor();
                    }
                }

                pc.SetToSingleton();
                SetPlannedPinConfiguration(pc);

                // Move to next phase
                phase.SetValue(Phase.CANDIDATE_LIMIT);
                round.SetValue(0);
            }
        }

        void ActivateCandidateLimit()
        {
            if (isHole)
                return;

            // Rounds 0-5: First iteration with axis E
            // Rounds 6-11: Second iteration with axis NNE
            // Rounds 12-17: Third iteration with axis NNW
            // Round 18: Finish and move on to next phase
            if (round < 18)
            {
                Direction dir = round < 6 ? Direction.E : (round < 12 ? Direction.NNE : Direction.NNW);
                string line1 = round < 6 ? hexLine0 : (round < 12 ? hexLine1 : hexLine2);
                string line2 = round < 6 ? hexLine3 : (round < 12 ? hexLine4 : hexLine5);
                if (round % 6 == 0)
                {
                    CandidateLimitRound0(dir);
                    UpdateColor();
                    round.SetValue(round + 1);
                }
                else if (round % 6 == 1)
                {
                    CandidateLimitRound1(dir);
                    round.SetValue(round + 1);
                }
                else if (round % 6 == 2)
                {
                    CandidateLimitRound2(line1, line2);
                    round.SetValue(round + 1);
                }
                else if (round % 6 == 3)
                {
                    int result = CandidateLimitRound3(dir, line1, line2);
                    if (result == 0)
                    {
                        // Finished
                        round.SetValue(round + 2);
                    }
                    else if (result == 1)
                    {
                        // PASC cutoff
                        round.SetValue(round + 1);
                    }
                    else
                    {
                        // Next iteration
                        round.SetValue(round - 1);
                    }
                }
                else if (round % 6 == 4)
                {
                    CandidateLimitRound4(dir);
                    round.SetValue(round + 1);
                }
                else if (round % 6 == 5)
                {
                    CandidateLimitRound5(dir);
                    UpdateColor();
                    round.SetValue(round + 1);
                }
            }
            else
            {
                // TODO: Finish
            }
        }

        void ActivateCandidateShift()
        {
            if (isHole)
                return;

            if (round == 0)
            {
                // Particles in sectors 6, 12 and 13, on axes 1 and 8, and corner 0 become candidates for both sides
                if (sectorIndex == 6 || sectorIndex == 12 || sectorIndex == 13 || axisIndex1 == 1 || axisIndex1 == 8 || axisIndex2 == 1 || axisIndex2 == 8 || hexCornerIndex == 0)
                {
                    isCandidateL.SetValue(true);
                    isCandidateR.SetValue(true);
                    SetCandidateColor();
                }

                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                DistanceCheckRound0(Direction.SSW, Direction.E);
                round.SetValue(round + 1);
            }
            else if (round == 2)
            {
                DistanceCheckRound1(Direction.SSW, Direction.E);
                round.SetValue(round + 1);
            }
            else if (round == 3)
            {
                DistanceCheckRound2(hexLine1, hexLine0);
                round.SetValue(round + 1);
            }
            else if (round == 4)
            {
                int result = DistanceCheckRound3(hexLine1, hexLine0, true);
                if (result == 0)
                {
                    // Finished
                    round.SetValue(100);
                }
                else if (result == 1)
                {
                    // Do PASC Cutoff
                    round.SetValue(5);
                }
                else
                {
                    // Continue
                    round.SetValue(3);
                }
            }
            else if (round == 5)
            {
                DistanceCheckRound4(true);

                PinConfiguration pc = GetContractedPinConfiguration();
                SetPlannedPinConfiguration(pc);

                round.SetValue(100);
            }












            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                // Check whether we have received the PASC activation beep
                if (isIntervalStartL || pc.GetPinAt(Direction.NNE, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                {
                    pasc1.Init(isIntervalStartL, isIntervalStartL ? Direction.NONE : Direction.NNE, Direction.SSW, 3, 0, 0, 3, 0, 1);
                    pasc1Participant.SetValue(true);
                }
                if (isIntervalStartR || pc.GetPinAt(Direction.W, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                {
                    pasc2.Init(isIntervalStartR, isIntervalStartR ? Direction.NONE : Direction.W, Direction.E, 0, 3, 3, 0, 2, 3);
                    pasc2Participant.SetValue(true);
                }

                // Setup PASC circuit and start the procedure
                pc.SetToSingleton();

                if (pasc1Participant.GetCurrentValue())
                    pasc1.SetupPC(pc);
                if (pasc2Participant.GetCurrentValue())
                    pasc2.SetupPC(pc);

                SetPlannedPinConfiguration(pc);

                if (isIntervalStartL)
                    pasc1.ActivateSend();
                if (isIntervalStartR)
                    pasc2.ActivateSend();

                // Corner particles initialize counters
                if (hexCornerIndex != -1)
                {
                    counter1Idx.SetValue(0);
                    counter2Idx.SetValue(0);
                }

                // Participants initialize comparison results
                if (pasc1Participant.GetCurrentValue())
                    comparison1.SetValue(Comparison.EQUAL);
                if (pasc2Participant.GetCurrentValue())
                    comparison2.SetValue(Comparison.EQUAL);

                round.SetValue(round + 1);
            }
            else if (round == 3)
            {
                Activate_Rounds3And6();
            }
            else if (round == 4)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Check for beep on global circuit
                if (!pc.ReceivedBeepOnPartitionSet(0))
                {
                    // No beep on global circuit: First PASC run is finished
                    // Interval starts remove this status
                    if (isIntervalStartL)
                        isIntervalStartL.SetValue(false);
                    if (isIntervalStartR)
                        isIntervalStartR.SetValue(false);

                    // If we are a participant and we have received an EQUAL result, become interval start
                    if (pasc1Participant && comparison1 == Comparison.EQUAL)
                        isIntervalStartL.SetValue(true);
                    if (pasc2Participant && comparison2 == Comparison.EQUAL)
                        isIntervalStartR.SetValue(true);

                    // Reset participant states
                    pasc1Participant.SetValue(false);
                    pasc2Participant.SetValue(false);

                    // Setup simple axis circuit using interval ends as breaks
                    pc = SetupAxisCircuit(3);
                    // IntervalL ends send beep on NNE axis, IntervalR ends send beep on E axis
                    if (isIntervalEndL.GetCurrentValue())
                        pc.GetPinAt(Direction.SSW, 0).PartitionSet.SendBeep();
                    if (isIntervalEndR.GetCurrentValue())
                        pc.GetPinAt(Direction.E, 0).PartitionSet.SendBeep();

                    round.SetValue(round + 1);
                }
                else
                {
                    // Receive bits from the two counters and update comparison results
                    if (pasc1Participant || pasc2Participant)
                    {
                        int counter1Bit = pc.ReceivedBeepOnPartitionSet(1) ? 1 : 0;
                        int counter2Bit = pc.ReceivedBeepOnPartitionSet(2) ? 1 : 0;

                        // Update comparison result
                        if (pasc1Participant)
                        {
                            int pascBit = pasc1.GetReceivedBit();
                            if (pascBit > counter1Bit)
                                comparison1.SetValue(Comparison.GREATER);
                            else if (pascBit < counter1Bit)
                                comparison1.SetValue(Comparison.LESS);
                        }
                        if (pasc2Participant)
                        {
                            int pascBit = pasc2.GetReceivedBit();
                            if (pascBit > counter2Bit)
                                comparison2.SetValue(Comparison.GREATER);
                            else if (pascBit < counter2Bit)
                                comparison2.SetValue(Comparison.LESS);
                        }
                    }

                    // Corners increment indices
                    if (hexCornerIndex != -1)
                    {
                        if (counter1Idx < hexLine1.Length)
                            counter1Idx.SetValue(counter1Idx + 1);
                        if (counter2Idx < hexLine0.Length)
                            counter2Idx.SetValue(counter2Idx + 1);
                    }

                    // Setup PASC circuits again, send next beeps and go back to round 3
                    pc.SetToSingleton();
                    if (pasc1Participant)
                        pasc1.SetupPC(pc);
                    if (pasc2Participant)
                        pasc2.SetupPC(pc);
                    SetPlannedPinConfiguration(pc);
                    if (isIntervalStartL)
                        pasc1.ActivateSend();
                    if (isIntervalStartR)
                        pasc2.ActivateSend();

                    round.SetValue(3);
                }
            }
            else if (round == 5)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                // Check whether we have received the PASC activation beep
                if (isIntervalEndL || pc.GetPinAt(Direction.NNE, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                {
                    pasc1.Init(isIntervalEndL, isIntervalEndL ? Direction.NONE : Direction.NNE, Direction.SSW, 3, 0, 0, 3, 0, 1);
                    pasc1Participant.SetValue(true);
                }
                if (isIntervalEndR || pc.GetPinAt(Direction.W, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                {
                    pasc2.Init(isIntervalEndR, isIntervalEndR ? Direction.NONE : Direction.W, Direction.E, 0, 3, 3, 0, 2, 3);
                    pasc2Participant.SetValue(true);
                }

                // Setup PASC circuit and start the procedure
                pc.SetToSingleton();

                if (pasc1Participant.GetCurrentValue())
                    pasc1.SetupPC(pc);
                if (pasc2Participant.GetCurrentValue())
                    pasc2.SetupPC(pc);

                SetPlannedPinConfiguration(pc);

                if (isIntervalEndL)
                    pasc1.ActivateSend();
                if (isIntervalEndR)
                    pasc2.ActivateSend();

                // Corner particles initialize counters (again)
                if (hexCornerIndex != -1)
                {
                    counter1Idx.SetValue(0);
                    counter2Idx.SetValue(0);
                }

                // Participants initialize comparison results
                if (pasc1Participant.GetCurrentValue())
                    comparison1.SetValue(Comparison.EQUAL);
                if (pasc2Participant.GetCurrentValue())
                    comparison2.SetValue(Comparison.EQUAL);

                round.SetValue(round + 1);
            }
            else if (round == 6)
            {
                Activate_Rounds3And6();
            }
            else if (round == 7)
            {
                // Almost the same as round 4
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Check for beep on global circuit
                if (!pc.ReceivedBeepOnPartitionSet(0))
                {
                    // No beep on global circuit: Second PASC run is finished
                    // Interval ends remove this status
                    if (isIntervalEndL)
                        isIntervalEndL.SetValue(false);
                    if (isIntervalEndR)
                        isIntervalEndR.SetValue(false);

                    // If we are a participant and we have received an EQUAL result, become interval end
                    if (pasc1Participant && comparison1 == Comparison.EQUAL)
                        isIntervalEndL.SetValue(true);
                    if (pasc2Participant && comparison2 == Comparison.EQUAL)
                        isIntervalEndR.SetValue(true);

                    // Reset participant states
                    pasc1Participant.SetValue(false);
                    pasc2Participant.SetValue(false);

                    // Finalize the first shift

                    // Setup simple axis circuit using interval starts and ends as breaks
                    pc = SetupAxisCircuit(4);
                    // IntervalL ends send beep on NNE axis, IntervalR ends send beep on E axis
                    if (isIntervalEndL.GetCurrentValue())
                        pc.GetPinAt(Direction.SSW, 0).PartitionSet.SendBeep();
                    if (isIntervalEndR.GetCurrentValue())
                        pc.GetPinAt(Direction.E, 0).PartitionSet.SendBeep();

                    round.SetValue(round + 1);
                }
                else
                {
                    // Receive bits from the two counters and update comparison results
                    if (pasc1Participant || pasc2Participant)
                    {
                        int counter1Bit = pc.ReceivedBeepOnPartitionSet(1) ? 1 : 0;
                        int counter2Bit = pc.ReceivedBeepOnPartitionSet(2) ? 1 : 0;

                        // Update comparison result
                        if (pasc1Participant)
                        {
                            int pascBit = pasc1.GetReceivedBit();
                            if (pascBit > counter1Bit)
                                comparison1.SetValue(Comparison.GREATER);
                            else if (pascBit < counter1Bit)
                                comparison1.SetValue(Comparison.LESS);
                        }
                        if (pasc2Participant)
                        {
                            int pascBit = pasc2.GetReceivedBit();
                            if (pascBit > counter2Bit)
                                comparison2.SetValue(Comparison.GREATER);
                            else if (pascBit < counter2Bit)
                                comparison2.SetValue(Comparison.LESS);
                        }
                    }

                    // Corners increment indices
                    if (hexCornerIndex != -1)
                    {
                        if (counter1Idx < hexLine1.Length)
                            counter1Idx.SetValue(counter1Idx + 1);
                        if (counter2Idx < hexLine0.Length)
                            counter2Idx.SetValue(counter2Idx + 1);
                    }

                    // Setup PASC circuits again, send next beeps and go back to round 3
                    pc.SetToSingleton();
                    if (pasc1Participant)
                        pasc1.SetupPC(pc);
                    if (pasc2Participant)
                        pasc2.SetupPC(pc);
                    SetPlannedPinConfiguration(pc);
                    if (isIntervalEndL)
                        pasc1.ActivateSend();
                    if (isIntervalEndR)
                        pasc2.ActivateSend();

                    round.SetValue(6);
                }
            }
            else if (round == 8)
            {
                // Receive candidate beeps
                PinConfiguration pc = GetCurrentPinConfiguration();

                isCandidateL.SetValue(false);
                isCandidateR.SetValue(false);

                // Become candidate if beep was received (or we are interval end)
                if (pc.GetPinAt(Direction.NNE, PinsPerEdge - 1).PartitionSet.ReceivedBeep() || isIntervalEndL)
                    isCandidateL.SetValue(true);
                if (pc.GetPinAt(Direction.W, PinsPerEdge - 1).PartitionSet.ReceivedBeep() || isIntervalEndR)
                    isCandidateR.SetValue(true);

                // Update color
                SetCandidateColor();

                // Setup simple axis circuit (axes do not connect)
                pc = SetupAxisCircuit(5);
                // Axis 10 sends beep in NNE direction, axis 11 sends in W direction
                // (to eliminate candidates that have not moved far enough)
                if (axisIndex1 == 10 || axisIndex2 == 10 || hexCornerIndex == 1)
                    pc.GetPinAt(Direction.NNE, 0).PartitionSet.SendBeep();
                if (axisIndex1 == 11 || axisIndex2 == 11 || hexCornerIndex == 5)
                    pc.GetPinAt(Direction.W, 0).PartitionSet.SendBeep();

                round.SetValue(round + 1);
            }
            else if (round == 9)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                // Receive direction beeps and withdraw candidacy
                if (isCandidateL && pc.GetPinAt(Direction.SSW, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                    isCandidateL.SetValue(false);
                if (isCandidateR && pc.GetPinAt(Direction.E, PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                    isCandidateR.SetValue(false);

                // Update color
                SetCandidateColor();

                round.SetValue(round + 1);
            }
        }


        /// <summary>
        /// Determines in which region of the candidate limit procedure we are.
        /// </summary>
        /// <param name="dir">The axis of the limit procedure.</param>
        /// <param name="extended">Whether the extended sectors should be used
        /// (for finding out which side we are on rather than checking if we
        /// have to become a PASC leader).</param>
        /// <returns><c>1</c> if we are in the first area, <c>2</c> if
        /// we are in the second area, <c>0</c> otherwise.</returns>
        private int CandidateLimitSector(Direction dir, bool extended = false)
        {
            if (dir == Direction.E)
            {
                // Sectors 0, 6, 11, 12, corners 0 and 5, sector 1 x axis 1, sector 5 x axis 6 for top side
                // Extended: Sectors 13 and 17, entire axes 1 and 6
                if (SectorCheck(0, 6, 11, 12, 0, 5, 1, 1, 5, 6, 13, 17, extended))
                    return 1;
                // Sectors 3, 8, 9, 15, corners 2 and 3, sector 2 x axis 0, sector 4 x axis 7 for bottom side
                // Extended: Sectors 14 and 16, entire axes 0 and 7
                else if (SectorCheck(3, 8, 9, 15, 2, 3, 2, 0, 4, 7, 14, 16, extended))
                    return 2;
            }
            else if (dir == Direction.NNE)
            {
                // Sectors 1, 6, 7, 13, corners 0 and 1, sector 2 x axis 3, sector 0 x axis 8 for top side
                // Extended: Sectors 12 and 14, entire axes 3 and 8
                if (SectorCheck(1, 6, 7, 13, 0, 1, 2, 3, 0, 8, 12, 14, extended))
                    return 1;
                // Sectors 4, 9, 10, 16, corners 3 and 4, sector 3 x axis 2, sector 5 x axis 9 for bottom side
                // Extended: Sectors 15 and 17, entire axes 2 and 9
                else if (SectorCheck(4, 9, 10, 16, 3, 4, 3, 2, 5, 9, 15, 17, extended))
                    return 2;
            }
            else if (dir == Direction.NNW)
            {
                // Sectors 2, 7, 8, 14, corners 1 and 2, sector 1 x axis 10, sector 3 x axis 5 for top side
                // Extended: Sectors 13 and 15, entire axes 5 and 10
                if (SectorCheck(2, 7, 8, 14, 1, 2, 1, 10, 3, 5, 13, 15, extended))
                    return 1;
                // Sectors 5, 10, 11, 17, corners 4 and 5, sector 0 x axis 11, sector 4 x axis 4 for bottom side
                // Extended: Sectors 12 and 16, entire axes 4 and 11
                else if (SectorCheck(5, 10, 11, 17, 4, 5, 0, 11, 4, 4, 12, 16, extended))
                    return 2;
            }
            return 0;
        }

        /// <summary>
        /// Helper to check whether the particle is in a specific region.
        /// Returns true if any of the conditions holds.
        /// <para>
        /// <paramref name="s1"/> to <paramref name="s4"/> are sector IDs.<br/>
        /// <paramref name="c1"/> and <paramref name="c2"/> are corner indices.<br/>
        /// <paramref name="sas1"/> and <paramref name="saa1"/> are IDs of a sector
        /// and an axis that both have to match (intersection; same for index 2).<br/>
        /// <paramref name="se1"/> and <paramref name="se2"/> are sectors for the
        /// extended check.<br/>
        /// Extended axes are taken from the intersections (<paramref name="saa1"/>
        /// and <paramref name="saa2"/>);
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if the particle lies in the specified region.</returns>
        private bool SectorCheck(int s1, int s2, int s3, int s4, int c1, int c2, int sas1, int saa1, int sas2, int saa2, int se1, int se2, bool extended)
        {
            return sectorIndex == s1 || sectorIndex == s2 || sectorIndex == s3 || sectorIndex == s4 || hexCornerIndex == c1 || hexCornerIndex == c2
                    || sectorIndex == sas1 && (axisIndex1 == saa1 || axisIndex2 == saa1)
                    || sectorIndex == sas2 && (axisIndex1 == saa2 || axisIndex2 == saa2)
                    || extended && (sectorIndex == se1 || sectorIndex == se2 || axisIndex1 == saa1 || axisIndex2 == saa1 || axisIndex1 == saa2 || axisIndex2 == saa2);
        }

        /// <summary>
        /// Establish simple axis circuits, particles with hole neighbors
        /// in the corresponding directions send beep or become limited
        /// immediately.
        /// which they want to check.
        /// </summary>
        /// <param name="dir">The direction of the axis we are checking
        /// (directions 0, 1 and 2 identify the axes).</param>
        private void CandidateLimitRound0(Direction dir)
        {
            // Check whether we are in the right sector to check for holes
            bool checkHoles = false;
            if (CandidateLimitSector(dir) != 0)
                checkHoles = true;

            // Check whether we are a leader in one direction or we even have to become limited already
            bool hasNbr1 = true;
            bool hasNbr2 = true;
            if (checkHoles)
            {
                hasNbr1 = !IsNbrHole(dir);
                hasNbr2 = !IsNbrHole(dir.Opposite());
                if (!hasNbr1 && !hasNbr2)
                {
                    // No neighbors: Become limited and don't do anything
                    limited.SetValue(true);
                    return;
                }
            }

            // Setup simple axis circuit, only connect axis direction if we have both neighbors
            PinConfiguration pc = SetupAxisCircuit(dir.Rotate60(1), dir.Rotate60(2), !hasNbr1 || !hasNbr2 ? dir : Direction.NONE);
            // Particles with hole neighbors send beep
            if (!hasNbr1)
                pc.GetPinAt(dir.Opposite(), 0).PartitionSet.SendBeep();
            if (!hasNbr2)
                pc.GetPinAt(dir, 0).PartitionSet.SendBeep();
        }

        /// <summary>
        /// Establish PASC circuits where needed, initialize counters,
        /// send first PASC beeps.
        /// </summary>
        /// <param name="dir">The direction of the axis we are checking
        /// (directions 0, 1 and 2 identify the axes).</param>
        private void CandidateLimitRound1(Direction dir)
        {
            // We only use one direction if we have received beeps from both sides
            // -> Prefer the one that goes in direction dir
            // The PASC instance we use later tells us in sector we are (top or bottom etc.)

            // Reset data first
            pasc1Participant.SetValue(false);
            pasc2Participant.SetValue(false);

            // Check whether we have received the PASC activation beep
            PinConfiguration pc = GetCurrentPinConfiguration();
            bool beep1 = pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep();
            bool beep2 = pc.GetPinAt(dir, PinsPerEdge - 1).PartitionSet.ReceivedBeep();

            bool sentBeep1 = false;
            bool sentBeep2 = false;
            if (CandidateLimitSector(dir) != 0)
            {
                bool hasNbr1 = !IsNbrHole(dir);
                bool hasNbr2 = !IsNbrHole(dir.Opposite());
                if (hasNbr1 ^ hasNbr2)
                {
                    sentBeep1 = !hasNbr2;
                    sentBeep2 = !hasNbr1;
                }
            }

            pc.SetToSingleton();

            if (beep1 || beep2 || sentBeep1 || sentBeep2)
            {
                // We are involved in the PASC procedure
                // Find out in which sector we are
                int sector = CandidateLimitSector(dir, true);
                SubPASC pascInstance;
                if (sector == 1)
                {
                    // Setup PASC 1 instance
                    pascInstance = pasc1;
                    pasc1Participant.SetValue(true);
                }
                else
                {
                    // Setup PASC 2 instance
                    pascInstance = pasc2;
                    pasc2Participant.SetValue(true);
                }

                bool isLeader = sentBeep1 || (sentBeep2 && !beep1);
                bool withDir = sentBeep1 || beep1;

                pascInstance.Init(isLeader, isLeader ? Direction.NONE : (withDir ? dir.Opposite() : dir), withDir ? dir : dir.Opposite(), 0, PinsPerEdge - 1, PinsPerEdge - 1, 0, 0, 1);
                pascInstance.SetupPC(pc);
            }

            SetPlannedPinConfiguration(pc);

            if (pasc1Participant.GetCurrentValue() && pasc1.IsLeader())
                pasc1.ActivateSend();
            if (pasc2Participant.GetCurrentValue() && pasc2.IsLeader())
                pasc2.ActivateSend();

            // Corner particles initialize counters
            if (hexCornerIndex != -1)
            {
                counter1Idx.SetValue(0);
                counter2Idx.SetValue(0);
            }

            // Participants initialize comparison results
            if (pasc1Participant.GetCurrentValue())
                comparison1.SetValue(Comparison.EQUAL);
            if (pasc2Participant.GetCurrentValue())
                comparison2.SetValue(Comparison.EQUAL);
        }

        /// <summary>
        /// Receive PASC beep, then establish 4-way global circuit and
        /// send PASC and comparison status info.
        /// </summary>
        /// <param name="length1">The bit string representing the first sector
        /// length to be checked.</param>
        /// <param name="length2">The bit string representing the second sector
        /// length to be checked.</param>
        private void CandidateLimitRound2(string length1, string length2)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            // Receive PASC beep
            if (pasc1Participant)
                pasc1.ActivateReceive();
            if (pasc2Participant)
                pasc2.ActivateReceive();

            // Setup 4-way global circuit (have 4 separate global circuits 0, 1, 2, 3)
            pc = SetupNWayGlobalCircuit(4);

            // Beep on circuit 0 if participant became inactive
            if (pasc1Participant && pasc1.BecamePassive() ||
                pasc2Participant && pasc2.BecamePassive())
                pc.SendBeepOnPartitionSet(0);

            // Beep on circuit 1 if one counter is still active
            if (hexCornerIndex != -1 && (counter1Idx < length1.Length || counter2Idx < length2.Length))
                pc.SendBeepOnPartitionSet(1);

            // Corner particles send current bit of counter
            if (hexCornerIndex != -1)
            {
                if (counter1Idx < length1.Length && length1[counter1Idx] == '1')
                    pc.SendBeepOnPartitionSet(2);
                if (counter2Idx < length2.Length && length2[counter2Idx] == '1')
                    pc.SendBeepOnPartitionSet(3);
            }
        }

        /// <summary>
        /// Updates the comparison result and checks the
        /// status beeps on the global circuit. If we are
        /// finished: Line ends with comparison result LESS
        /// send beep on axis circuit. If the comparison is finished:
        /// Start PASC cutoff. Otherwise: Start next iteration.
        /// </summary>
        /// <param name="dir">The direction of the limit check.</param>
        /// <param name="length1">The bit string representing the first sector
        /// length to be checked.</param>
        /// <param name="length2">The bit string representing the second sector
        /// length to be checked.</param>
        /// <returns><c>0</c> if we are finished completely,
        /// <c>1</c> if we are performing PASC cutoff,
        /// <c>2</c> if we have to continue.</returns>
        private int CandidateLimitRound3(Direction dir, string length1, string length2)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            // Check for beeps on global circuits
            bool[] beeps = new bool[4];
            for (int i = 0; i < 4; i++)
                beeps[i] = pc.ReceivedBeepOnPartitionSet(i);

            int counter1Bit = beeps[2] ? 1 : 0;
            int counter2Bit = beeps[3] ? 1 : 0;

            // Update comparison result
            if (pasc1Participant)
                UpdateComparisonResult(pasc1.GetReceivedBit(), counter1Bit, comparison1);
            if (pasc2Participant)
                UpdateComparisonResult(pasc2.GetReceivedBit(), counter2Bit, comparison2);

            // Check status beeps on global circuit
            if (!beeps[0] && !beeps[1])
            {
                // Both PASC and comparison are finished:
                // Setup axis circuit again and let line ends with comparison result
                // of LESS send beep
                CandidateLimitEliminationBeep(dir);

                return 0;
            }
            else if (!beeps[1])
            {
                // Comparison is finished but PASC may not be: Start PASC cutoff
                pc.SetToSingleton();
                if (pasc1Participant)
                    pasc1.SetupCutoffCircuit(pc);
                if (pasc2Participant)
                    pasc2.SetupCutoffCircuit(pc);
                SetPlannedPinConfiguration(pc);
                if (pasc1Participant)
                    pasc1.SendCutoffBeep();
                if (pasc2Participant)
                    pasc2.SendCutoffBeep();

                return 1;
            }
            else
            {
                // Not finished yet
                // Corners increment indices
                if (hexCornerIndex != -1)
                {
                    if (counter1Idx < length1.Length)
                        counter1Idx.SetValue(counter1Idx + 1);
                    if (counter2Idx < length2.Length)
                        counter2Idx.SetValue(counter2Idx + 1);
                }

                // Setup PASC circuits again, send next beeps and go back to round 3
                pc.SetToSingleton();
                if (pasc1Participant)
                    pasc1.SetupPC(pc);
                if (pasc2Participant)
                    pasc2.SetupPC(pc);
                SetPlannedPinConfiguration(pc);
                if (pasc1Participant && pasc1.IsLeader())
                    pasc1.ActivateSend();
                if (pasc2Participant && pasc2.IsLeader())
                    pasc2.ActivateSend();

                return 2;
            }
        }

        /// <summary>
        /// Receive PASC cutoff beep and determine final comparison result.
        /// Then establish axis circuit and send elimination beep.
        /// </summary>
        /// <param name="dir">The direction of the limit check.</param>
        private void CandidateLimitRound4(Direction dir)
        {
            // Receive PASC cutoff beep
            if (pasc1Participant)
            {
                pasc1.ReceiveCutoffBeep();
                if (pasc1.GetReceivedBit() > 0)
                    comparison1.SetValue(Comparison.GREATER);
            }
            if (pasc2Participant)
            {
                pasc2.ReceiveCutoffBeep();
                if (pasc2.GetReceivedBit() > 0)
                    comparison2.SetValue(Comparison.GREATER);
            }

            CandidateLimitEliminationBeep(dir);
        }

        private void CandidateLimitEliminationBeep(Direction dir)
        {
            // Setup axis circuit and let line ends with comparison
            // result of LESS send beep
            PinConfiguration pc;
            pc = SetupAxisCircuit(dir.Rotate60(1), dir.Rotate60(2));
            bool hasNbr1 = !IsNbrHole(dir);
            bool hasNbr2 = !IsNbrHole(dir.Opposite());
            if (pasc1Participant && comparison1.GetCurrentValue() == Comparison.LESS && !pasc1.IsLeader() && (hasNbr1 ^ hasNbr2) ||
                pasc2Participant && comparison2.GetCurrentValue() == Comparison.LESS && !pasc2.IsLeader() && (hasNbr1 ^ hasNbr2))
            {
                pc.GetPinAt(dir, 0).PartitionSet.SendBeep();
                pc.GetPinAt(dir.Opposite(), 0).PartitionSet.SendBeep();
            }
        }

        /// <summary>
        /// Receive elimination beep on axis circuit, become limited hole
        /// if received.
        /// </summary>
        /// <param name="dir">The direction of the limit check.</param>
        private void CandidateLimitRound5(Direction dir)
        {
            // Check for elimination beep
            PinConfiguration pc = GetCurrentPinConfiguration();
            if (pc.GetPinAt(dir, 0).PartitionSet.ReceivedBeep() && !isHole)
            {
                limited.SetValue(true);
            }
        }



        private void Activate_Rounds3And6()
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            // Receive PASC beep
            if (pasc1Participant)
                pasc1.ActivateReceive();
            if (pasc2Participant)
                pasc2.ActivateReceive();

            // Setup 3-way global circuit (have 3 separate global circuits 0, 1, 2)
            pc = SetupNWayGlobalCircuit(3);

            // Beep if participant became inactive or one counter is still not finished
            if (pasc1Participant && pasc1.BecamePassive() ||
                pasc2Participant && pasc2.BecamePassive() ||
                hexCornerIndex != -1 && (counter1Idx < hexLine1.Length || counter2Idx < hexLine0.Length))
                pc.SendBeepOnPartitionSet(0);

            // Corner particles send current bit of counter
            if (hexCornerIndex != -1)
            {
                if (counter1Idx < hexLine1.Length && hexLine1[counter1Idx] == '1')
                    pc.SendBeepOnPartitionSet(1);
                if (counter2Idx < hexLine0.Length && hexLine0[counter2Idx] == '1')
                    pc.SendBeepOnPartitionSet(2);
            }

            round.SetValue(round + 1);
        }

        /// <summary>
        /// Establish axis circuits, let candidates beep in the direction in
        /// which they want to check.
        /// </summary>
        /// <param name="dirL">Left candidate check direction.</param>
        /// <param name="dirR">Right candidate check direction.</param>
        private void DistanceCheckRound0(Direction dirL, Direction dirR)
        {
            bool candL = isCandidateL.GetCurrentValue();
            bool candR = isCandidateR.GetCurrentValue();
            // Setup simple axis circuit, candidates do not connect
            PinConfiguration pc = SetupAxisCircuit(candL ? dirL : Direction.NONE, candR ? dirR : Direction.NONE);
            // Left candidates send beep on first axis, Right candidates send beep on second axis
            if (candL)
                pc.GetPinAt(dirL, 0).PartitionSet.SendBeep();
            if (candR)
                pc.GetPinAt(dirR, 0).PartitionSet.SendBeep();
        }

        /// <summary>
        /// Establish PASC circuits where needed, initialize counters,
        /// send first PASC beeps.
        /// </summary>
        /// <param name="dirL">Left candidate check direction.</param>
        /// <param name="dirR">Right candidate check direction.</param>
        private void DistanceCheckRound1(Direction dirL, Direction dirR)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();
            // Check whether we have received the PASC activation beep
            if (isCandidateL || pc.GetPinAt(dirL.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
            {
                // Leader has no neighbor in check direction
                bool leader = IsNbrHole(dirL);
                pasc1.Init(leader, leader ? Direction.NONE : dirL, dirL.Opposite(), 0, PinsPerEdge - 1, PinsPerEdge - 1, 0, 0, 1);
                pasc1Participant.SetValue(true);
            }
            if (isCandidateR || pc.GetPinAt(dirR.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
            {
                // Leader has no neighbor in check direction
                bool leader = IsNbrHole(dirR);
                pasc2.Init(leader, leader ? Direction.NONE : dirR, dirR.Opposite(), PinsPerEdge - 2, 1, 1, PinsPerEdge - 2, 2, 3);
                pasc2Participant.SetValue(true);
            }

            // Setup PASC circuit and start the procedure
            pc.SetToSingleton();

            if (pasc1Participant.GetCurrentValue())
                pasc1.SetupPC(pc);
            if (pasc2Participant.GetCurrentValue())
                pasc2.SetupPC(pc);

            SetPlannedPinConfiguration(pc);

            if (pasc1Participant.GetCurrentValue() && pasc1.IsLeader())
                pasc1.ActivateSend();
            if (pasc2Participant.GetCurrentValue() && pasc2.IsLeader())
                pasc2.ActivateSend();

            // Corner particles initialize counters
            if (hexCornerIndex != -1)
            {
                counter1Idx.SetValue(0);
                counter2Idx.SetValue(0);
            }

            // Participants initialize comparison results
            if (pasc1Participant.GetCurrentValue())
                comparison1.SetValue(Comparison.EQUAL);
            if (pasc2Participant.GetCurrentValue())
                comparison2.SetValue(Comparison.EQUAL);
        }

        /// <summary>
        /// Receive PASC beep, then establish 4-way global circuit and
        /// send PASC and comparison status info.
        /// </summary>
        /// <param name="lengthL">The bit string representing the left
        /// length to be checked.</param>
        /// <param name="lengthR">The bit string representing the right
        /// length to be checked.</param>
        private void DistanceCheckRound2(string lengthL, string lengthR)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            // Receive PASC beep
            if (pasc1Participant)
                pasc1.ActivateReceive();
            if (pasc2Participant)
                pasc2.ActivateReceive();

            // Setup 4-way global circuit (have 4 separate global circuits 0, 1, 2, 3)
            pc = SetupNWayGlobalCircuit(4);

            // Beep on circuit 0if participant became inactive
            if (pasc1Participant && pasc1.BecamePassive() ||
                pasc2Participant && pasc2.BecamePassive())
                pc.SendBeepOnPartitionSet(0);

            // Beep on circuit 1 if one counter is still active
            if (hexCornerIndex != -1 && (counter1Idx < hexLine1.Length || counter2Idx < hexLine0.Length))
                pc.SendBeepOnPartitionSet(1);

            // Corner particles send current bit of counter
            if (hexCornerIndex != -1)
            {
                if (counter1Idx < lengthL.Length && lengthL[counter1Idx] == '1')
                    pc.SendBeepOnPartitionSet(2);
                if (counter2Idx < lengthR.Length && lengthR[counter2Idx] == '1')
                    pc.SendBeepOnPartitionSet(3);
            }
        }

        /// <summary>
        /// Updates the comparison result and checks the
        /// status beeps on the global circuit. If we are
        /// finished: Candidates with comparison result LESS
        /// are eliminated. If the comparison is finished:
        /// Start PASC cutoff. Otherwise: Start next iteration.
        /// </summary>
        /// <param name="lengthL">The bit string representing the left
        /// length to be checked.</param>
        /// <param name="lengthR">The bit string representing the right
        /// length to be checked.</param>
        /// <param name="eliminateBoth">Whether both candidate types
        /// should be eliminated if one comparison result is LESS.</param>
        /// <returns><c>0</c> if we are finished completely,
        /// <c>1</c> if we are performing PASC cutoff,
        /// <c>2</c> if we have to continue.</returns>
        private int DistanceCheckRound3(string lengthL, string lengthR, bool eliminateBoth = false)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            // Check for beeps on global circuits
            bool[] beeps = new bool[4];
            for (int i = 0; i < 4; i++)
                beeps[i] = pc.ReceivedBeepOnPartitionSet(i);

            int counter1Bit = beeps[2] ? 1 : 0;
            int counter2Bit = beeps[3] ? 1 : 0;

            // Update comparison result
            if (pasc1Participant)
                UpdateComparisonResult(pasc1.GetReceivedBit(), counter1Bit, comparison1);
            if (pasc2Participant)
                UpdateComparisonResult(pasc2.GetReceivedBit(), counter2Bit, comparison2);

            // Check status beeps on global circuit
            if (!beeps[0] && !beeps[1])
            {
                // Both PASC and comparison are finished:
                // All candidates with a comparison result of LESS withdraw their candidacies
                if (isCandidateL && comparison1.GetCurrentValue() == Comparison.LESS)
                {
                    isCandidateL.SetValue(false);
                    if (eliminateBoth)
                        isCandidateR.SetValue(false);
                }
                if (isCandidateR && comparison2.GetCurrentValue() == Comparison.LESS)
                {
                    isCandidateR.SetValue(false);
                    if (eliminateBoth)
                        isCandidateL.SetValue(false);
                }
                SetCandidateColor();

                return 0;
            }
            else if (!beeps[1])
            {
                // Comparison is finished but PASC may not be: Start PASC cutoff
                if (pasc1Participant)
                    pasc1.SetupCutoffCircuit(pc);
                if (pasc2Participant)
                    pasc2.SetupCutoffCircuit(pc);
                SetPlannedPinConfiguration(pc);
                if (pasc1Participant)
                    pasc1.SendCutoffBeep();
                if (pasc2Participant)
                    pasc2.SendCutoffBeep();

                return 1;
            }
            else
            {
                // Not finished yet
                // Corners increment indices
                if (hexCornerIndex != -1)
                {
                    if (counter1Idx < lengthL.Length)
                        counter1Idx.SetValue(counter1Idx + 1);
                    if (counter2Idx < lengthR.Length)
                        counter2Idx.SetValue(counter2Idx + 1);
                }

                // Setup PASC circuits again, send next beeps and go back to round 3
                pc.SetToSingleton();
                if (pasc1Participant)
                    pasc1.SetupPC(pc);
                if (pasc2Participant)
                    pasc2.SetupPC(pc);
                SetPlannedPinConfiguration(pc);
                if (pasc1Participant && pasc1.IsLeader())
                    pasc1.ActivateSend();
                if (pasc2Participant && pasc2.IsLeader())
                    pasc2.ActivateSend();

                return 2;
            }
        }

        /// <summary>
        /// Receive PASC cutoff beep and determine final comparison result.
        /// Then eliminate candidates.
        /// <param name="eliminateBoth">Whether both candidate types
        /// should be eliminated if one comparison result is LESS.</param>
        /// </summary>
        private void DistanceCheckRound4(bool eliminateBoth = false)
        {
            // Receive PASC cutoff beep
            if (pasc1Participant)
            {
                pasc1.ReceiveCutoffBeep();
                if (pasc1.GetReceivedBit() > 0)
                    comparison1.SetValue(Comparison.GREATER);
            }
            if (pasc2Participant)
            {
                pasc2.ReceiveCutoffBeep();
                if (pasc2.GetReceivedBit() > 0)
                    comparison2.SetValue(Comparison.GREATER);
            }

            if (isCandidateL && comparison1.GetCurrentValue() == Comparison.LESS)
            {
                isCandidateL.SetValue(false);
                if (eliminateBoth)
                    isCandidateR.SetValue(false);
            }
            if (isCandidateR && comparison2.GetCurrentValue() == Comparison.LESS)
            {
                isCandidateR.SetValue(false);
                if (eliminateBoth)
                    isCandidateL.SetValue(false);
            }
            SetCandidateColor();
        }

        private PinConfiguration SetupAxisCircuit(int mode)
        {
            PinConfiguration pc = GetContractedPinConfiguration();

            // Mode 0: Extreme boundary particles do not connect
            //          their directions
            // Mode 1: Corners connect no directions
            // Mode 2: CandidateL interval starts do not connect NNE axis and
            //         CandidateR interval starts do not connect E axis
            // Mode 3: CandidateL interval ends do not connect NNE axis and
            //         CandidateR interval ends do not connect E axis
            // Mode 4: CandidateL interval starts and ends do not connect NNE axis and
            //         CandidateR interval starts and ends do not connect E axis
            // Mode 5: Axis 10 does not connect NNE axis and axis 11 does not connect E axis
            //         (includes corners)

            for (int d = 0; d < 3; d++)
            {
                Direction dir = DirectionHelpers.Cardinal(d);
                bool connectAxis = true;
                if (mode == 0)
                {
                    if (d == 0 && !hexBoundIndex.GetValue()[1].Equals('0') ||
                        d == 1 && !hexBoundIndex.GetValue()[0].Equals('0') ||
                        d == 2 && !hexBoundIndex.GetValue()[2].Equals('0'))
                        connectAxis = false;
                }
                else if (mode == 1)
                {
                    if (hexCornerIndex.GetCurrentValue() != -1)
                        connectAxis = false;
                }
                if (mode == 2 || mode == 4)
                {
                    if (isIntervalStartL.GetCurrentValue() && d == 1 ||
                        isIntervalStartR.GetCurrentValue() && d == 0)
                        connectAxis = false;
                }
                if (mode == 3 || mode == 4)
                {
                    if (isIntervalEndL.GetCurrentValue() && d == 1 ||
                        isIntervalEndR.GetCurrentValue() && d == 0)
                        connectAxis = false;
                }
                if (mode == 5)
                {
                    if (d == 0 && (axisIndex1 == 11 || axisIndex2 == 11 || hexCornerIndex == 5) ||
                        d == 1 && (axisIndex1 == 10 || axisIndex2 == 10 || hexCornerIndex == 1))
                        connectAxis = false;
                }

                if (connectAxis)
                {
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, 0).Id, pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).Id },
                        d
                    );
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, PinsPerEdge - 1).Id, pc.GetPinAt(dir.Opposite(), 0).Id },
                        d + 3
                    );
                    pc.SetPartitionSetPosition(d, new Vector2(d * 60.0f - 30.0f, pSetDistance));
                    pc.SetPartitionSetPosition(d + 3, new Vector2(((d + 3) % 6) * 60.0f - 30.0f, pSetDistance * 0.9f));
                }
                else
                {
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, 0).Id },
                        d
                    );
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir.Opposite(), 0).Id },
                        d + 3
                    );
                }
            }

            SetPlannedPinConfiguration(pc);
            return pc;
        }

        private PinConfiguration SetupAxisCircuit(Direction excludeDir1 = Direction.NONE, Direction excludeDir2 = Direction.NONE, Direction excludeDir3 = Direction.NONE)
        {
            PinConfiguration pc = GetContractedPinConfiguration();

            for (int d = 0; d < 3; d++)
            {
                Direction dir = DirectionHelpers.Cardinal(d);
                bool connectAxis = dir != excludeDir1 && dir != excludeDir1.Opposite() &&
                    dir != excludeDir2 && dir != excludeDir2.Opposite() &&
                    dir != excludeDir3 && dir != excludeDir3.Opposite();

                if (connectAxis)
                {
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, 0).Id, pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).Id },
                        d
                    );
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, PinsPerEdge - 1).Id, pc.GetPinAt(dir.Opposite(), 0).Id },
                        d + 3
                    );
                    pc.SetPartitionSetPosition(d, new Vector2(d * 60.0f - 30.0f, pSetDistance));
                    pc.SetPartitionSetPosition(d + 3, new Vector2(((d + 3) % 6) * 60.0f - 30.0f, pSetDistance * 0.9f));
                }
                else
                {
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, 0).Id },
                        d
                    );
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir.Opposite(), 0).Id },
                        d + 3
                    );
                }
            }

            SetPlannedPinConfiguration(pc);
            return pc;
        }

        private PinConfiguration SetupMultiAxisCircuit(int mode = 0)
        {
            PinConfiguration pc = GetContractedPinConfiguration();

            // Mode 0: Hexagon corner and edge particles do not connect their axes
            // Mode 1: Everybody connects their axes

            for (int d = 0; d < 3; d++)
            {
                Direction dir = DirectionHelpers.Cardinal(d);
                bool connectAxis = true;
                if (mode == 0)
                {
                    if (hexCornerIndex != -1 || hexEdgeIndex != -1)
                        connectAxis = false;
                }

                if (connectAxis)
                {
                    for (int i = 0; i < PinsPerEdge; i++)
                    {
                        pc.MakePartitionSet(new int[] {
                            pc.GetPinAt(dir, i).Id,
                            pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1 - i).Id
                        }, d * PinsPerEdge + i);

                        if (i < 2)
                            pc.SetPartitionSetPosition(d * PinsPerEdge + i, new Vector2(d * 60.0f - (i == 0 ? 30.0f : 10.0f), i == 0 ? pSetDistance : pSetDistance * 0.9f));
                        else
                            pc.SetPartitionSetPosition(d * PinsPerEdge + i, new Vector2(((d + 3) % 6) * 60.0f - (i == PinsPerEdge - 1 ? 30.0f : 10.0f), i == PinsPerEdge - 1 ? pSetDistance : pSetDistance * 0.9f));
                    }
                }
            }

            SetPlannedPinConfiguration(pc);
            return pc;
        }

        private PinConfiguration SetupMultiAxisDirectionCircuit(int mode = 0)
        {
            PinConfiguration pc = GetContractedPinConfiguration();

            // Mode 0: Hexagon corners and lines connect no directions,
            //         Axis particles only connect the direction they let through during SECTOR_IDENT
            //         Also, holes do not connect any directions

            for (int d = 0; d < 6; d++)
            {
                // Dir is the direction in which we send
                Direction dir = DirectionHelpers.Cardinal(d);
                bool connectAxis = true;

                if (mode == 0)
                {
                    if (isHole.GetCurrentValue() || hexCornerIndex.GetCurrentValue() != -1 || hexEdgeIndex.GetCurrentValue() != -1 ||
                        virtualHoles[d].GetCurrentValue() || virtualHoles[(d + 3) % 6].GetCurrentValue())
                        connectAxis = false;
                    else if (axisIndex1 != -1)
                    {
                        foreach (int idx in new int[] { axisIndex1, axisIndex2 })
                        {
                            if (idx != -1 && (
                                idx % 2 == 0 && d == (idx / 2 + 2) % 6 ||
                                idx % 2 == 1 && d == (idx / 2 + 4) % 6))
                            {
                                connectAxis = false;
                                break;
                            }
                        }
                    }
                }

                if (connectAxis)
                {
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, 0).Id, pc.GetPinAt(dir.Opposite(), PinsPerEdge - 1).Id },
                        2 * d
                    );
                    pc.MakePartitionSet(
                        new int[] { pc.GetPinAt(dir, 1).Id, pc.GetPinAt(dir.Opposite(), PinsPerEdge - 2).Id },
                        2 * d + 1
                    );

                    pc.SetPartitionSetPosition(2 * d, new Vector2(d * 60.0f - 30.0f, pSetDistance));
                    pc.SetPartitionSetPosition(2 * d + 1, new Vector2(d * 60.0f - 10.0f, pSetDistance * 0.9f));
                }
            }

            SetPlannedPinConfiguration(pc);
            return pc;
        }

        private PinConfiguration SetupInternalCircuit()
        {
            PinConfiguration pc = GetContractedPinConfiguration();

            if (hexCornerIndex != -1 || hexEdgeIndex.GetCurrentValue() != -1)
            {
                // Collect all pins on the "inside" of the hexagon in partition set 0
                int numDirs;
                int startDir;
                if (hexCornerIndex != -1)
                {
                    numDirs = 3;
                    startDir = (hexCornerIndex + 4) % 6;
                }
                else
                {
                    numDirs = 4;
                    startDir = (hexEdgeIndex.GetCurrentValue() + 3) % 6;
                }

                List<int> pins = new List<int>();
                for (int i = 0; i < numDirs; i++)
                {
                    int d = (startDir + i) % 6;
                    for (int j = 0; j < PinsPerEdge; j++)
                        pins.Add(pc.GetPinAt(DirectionHelpers.Cardinal(d), j).Id);
                }
                pc.MakePartitionSet(pins.ToArray(), 0);
            }
            else
            {
                pc.SetToGlobal(0);
            }

            SetPlannedPinConfiguration(pc);
            return pc;
        }

        private PinConfiguration SetupNWayGlobalCircuit(int n)
        {
            // n must be between 1 and PinsPerEdge

            PinConfiguration pc = GetContractedPinConfiguration();

            // Connect pins to create multiple global circuits
            for (int i = 0; i < n; i++)
            {
                int j = PinsPerEdge - 1 - i;
                pc.MakePartitionSet(new int[] {
                    pc.GetPinAt(Direction.E, i).Id,
                    pc.GetPinAt(Direction.NNE, i).Id,
                    pc.GetPinAt(Direction.NNW, i).Id,
                    pc.GetPinAt(Direction.W, j).Id,
                    pc.GetPinAt(Direction.SSW, j).Id,
                    pc.GetPinAt(Direction.SSE, j).Id
                }, i);
            }

            SetPlannedPinConfiguration(pc);

            return pc;
        }

        private bool IsNbrCandidate(Direction dir, bool left)
        {
            if (!HasNeighborAt(dir))
                return false;
            HexagonTestParticle nbr = (HexagonTestParticle)GetNeighborAt(dir);
            return left ? nbr.isCandidateL : nbr.isCandidateR;
        }

        private void UpdateComparisonResult(int bit1, int bit2, ParticleAttribute<Comparison> comp)
        {
            if (bit1 > bit2)
                comp.SetValue(Comparison.GREATER);
            else if (bit1 < bit2)
                comp.SetValue(Comparison.LESS);
        }

        private bool IsNbrHole(Direction dir)
        {
            if (!HasNeighborAt(dir))
                return true;
            HexagonTestParticle nbr = (HexagonTestParticle)GetNeighborAt(dir);
            return nbr.isHole;
        }

        private void SetCandidateColor()
        {
            if (hexCornerIndex.GetCurrentValue() == -1 && hexEdgeIndex.GetCurrentValue() == -1 && axisIndex1.GetCurrentValue() == -1 && !isHole.GetCurrentValue() && !limited.GetCurrentValue() && !isInternal.GetCurrentValue())
            {
                if (isCandidateL.GetCurrentValue() || isCandidateR.GetCurrentValue())
                    SetMainColor(ColorData.Particle_Aqua);
                else
                    SetMainColor(ColorData.Particle_Black);
            }
        }

        private void SetLimitedColor()
        {
            if (limited.GetCurrentValue() && hexCornerIndex.GetCurrentValue() == -1 && hexEdgeIndex.GetCurrentValue() == -1 && !isHole.GetCurrentValue() && !isInternal.GetCurrentValue())
            {
                SetMainColor(ColorData.Particle_Orange);
            }
        }

        // TODO: Proper color management
        private void UpdateColor()
        {
            if (isInternal.GetCurrentValue())
                SetMainColor(Color.gray);
            else if (isHole.GetCurrentValue())
                SetMainColor(ColorData.Particle_Red);
            else if (hexCornerIndex.GetCurrentValue() != -1)
                SetMainColor(ColorData.Particle_BlueDark);
            else if (hexEdgeIndex.GetCurrentValue() != -1)
                SetMainColor(ColorData.Particle_Blue);
            else if (limited.GetCurrentValue())
                SetMainColor(ColorData.Particle_Orange);
            else if (axisIndex1.GetCurrentValue() != -1)
                SetMainColor(ColorData.Particle_Yellow);
            else if (isCandidateL.GetCurrentValue() || isCandidateR.GetCurrentValue())
                SetMainColor(ColorData.Particle_Green);
            else
                SetMainColor(ColorData.Particle_Black);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class HexagonTestInitializer : InitializationMethod
    {
        public HexagonTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int holeSize = 10, int numSurroundingParticles = 250, float holeProb = 0.025f, bool fillHoles = false,
            bool prioritizeInner = true, float lambda = 0.1f,
            int hexLengthTop = 6, int hexLengthTopLeft = 6, int hexLengthBotLeft = 6, int hexLengthBot = 6)
        {
            // Generate hole
            List<Vector2Int> holePositions = GenerateRandomConnectedPositions(Vector2Int.zero, holeSize, 0f, true);
            // Find extreme coordinates
            int yMin = 0;
            int yMax = 0;
            int xMin = 0;
            int xMax = 0;
            int xyMin = 0;
            int xyMax = 0;
            foreach (Vector2Int v in holePositions)
            {
                if (v.y <= yMin)
                    yMin = v.y - 1;
                if (v.y >= yMax)
                    yMax = v.y + 1;

                if (v.x <= xMin)
                    xMin = v.x - 1;
                if (v.x >= xMax)
                    xMax = v.x + 1;

                int xy = v.x + v.y;
                if (xy <= xyMin)
                    xyMin = xy - 1;
                if (xy >= xyMax)
                    xyMax = xy + 1;
            }

            // Find corner points
            Vector2Int topLeft = new Vector2Int(xMin, yMax);
            Vector2Int topRight = new Vector2Int(xyMax - yMax, yMax);
            Vector2Int left = new Vector2Int(xMin, xyMin - xMin);
            Vector2Int right = new Vector2Int(xMax, xyMax - xMax);
            Vector2Int botLeft = new Vector2Int(xyMin - yMin, yMin);
            Vector2Int botRight = new Vector2Int(xMax, yMin);

            // Also draw lines
            AS2.UI.CollisionLineDrawer ld = AS2.UI.CollisionLineDrawer.Instance;
            ld.Clear();
            // Hexagon
            ld.AddLine(topLeft, topRight, Color.blue);
            ld.AddLine(topRight, right, Color.blue);
            ld.AddLine(right, botRight, Color.blue);
            ld.AddLine(botRight, botLeft, Color.blue);
            ld.AddLine(botLeft, left, Color.blue);
            ld.AddLine(left, topLeft, Color.blue);
            // Extension lines
            ld.AddLine(topLeft, ParticleSystem_Utils.GetNbrInDir(topLeft, Direction.W, 10), Color.green, true);
            ld.AddLine(topLeft, ParticleSystem_Utils.GetNbrInDir(topLeft, Direction.NNE, 10), Color.green, true);
            ld.AddLine(topRight, ParticleSystem_Utils.GetNbrInDir(topRight, Direction.NNW, 10), Color.green, true);
            ld.AddLine(topRight, ParticleSystem_Utils.GetNbrInDir(topRight, Direction.E, 10), Color.green, true);
            ld.AddLine(right, ParticleSystem_Utils.GetNbrInDir(right, Direction.NNE, 10), Color.green, true);
            ld.AddLine(right, ParticleSystem_Utils.GetNbrInDir(right, Direction.SSE, 10), Color.green, true);
            ld.AddLine(botRight, ParticleSystem_Utils.GetNbrInDir(botRight, Direction.E, 10), Color.green, true);
            ld.AddLine(botRight, ParticleSystem_Utils.GetNbrInDir(botRight, Direction.SSW, 10), Color.green, true);
            ld.AddLine(botLeft, ParticleSystem_Utils.GetNbrInDir(botLeft, Direction.W, 10), Color.green, true);
            ld.AddLine(botLeft, ParticleSystem_Utils.GetNbrInDir(botLeft, Direction.SSE, 10), Color.green, true);
            ld.AddLine(left, ParticleSystem_Utils.GetNbrInDir(left, Direction.NNW, 10), Color.green, true);
            ld.AddLine(left, ParticleSystem_Utils.GetNbrInDir(left, Direction.SSW, 10), Color.green, true);
            ld.SetTimer(10f);

            // Place particles and let boundary particles know that they are the boundary
            int y = yMax;
            int xLeft = topLeft.x;
            int xRight = topRight.x;
            InitializationParticle ip;
            int hexagonArea = 0;
            while (y >= yMin)
            {
                // Place row of particles between xLeft and xRight
                for (int x = xLeft; x <= xRight; x++)
                {
                    Vector2Int p = new Vector2Int(x, y);

                    hexagonArea++;

                    if (holePositions.Contains(p))  // Leave hole free
                        continue;

                    ip = AddParticle(p);

                    // Check if the position has a neighboring hole (i.e. is boundary)
                    bool onBoundary = false;
                    foreach (Vector2Int v in holePositions)
                    {
                        if (AmoebotFunctions.AreNodesNeighbors(p, v))
                        {
                            onBoundary = true;
                            break;
                        }
                    }
                    if (!onBoundary)
                        continue;

                    // On boundary: Check in which directions we are an extreme point
                    int xy = x + y;
                    int extremeX = x == xMax ? 2 : (x == xMin ? 1 : 0);
                    int extremeY = y == yMax ? 2 : (y == yMin ? 1 : 0);
                    int extremeXY = xy == xyMax ? 2 : (xy == xyMin ? 1 : 0);
                    // e.g., "201" means "xMax and xyMin"
                    string boundaryIndex = (100 * extremeX + 10 * extremeY + extremeXY).ToString();
                    boundaryIndex = boundaryIndex.PadLeft(3, '0');
                    ip.SetAttribute("hexBoundIndex", boundaryIndex);
                }

                // Update left and right borders
                if (y <= left.y)
                    xLeft++;
                if (y > right.y)
                    xRight++;

                y--;
            }

            // Now place more particles around the hexagon
            // Generate particles centered
            List<Vector2Int> positions = GenerateRandomConnectedPositions(Vector2Int.zero, numSurroundingParticles + hexagonArea, holeProb, fillHoles, null, true, prioritizeInner, lambda);

            //List<Vector2Int> positions = GenerateRandomConnectedPositions(topLeft + Vector2Int.up, numSurroundingParticles, holeProb, fillHoles,
            //    (Vector2Int v) => { return !(v.x < xMin || v.x > xMax || v.y < yMin || v.y > yMax || v.x + v.y < xyMin || v.x + v.y > xyMax); },
            //    true, prioritizeInner, lambda);

            // Only place the particles that are not in the hexagon
            foreach (Vector2Int v in positions)
            {
                if (v.x < xMin || v.x > xMax || v.y < yMin || v.y > yMax || v.x + v.y < xyMin || v.x + v.y > xyMax)
                    AddParticle(v);
            }

            // Compute hexagon size

            // First test for invalid input data
            if (hexLengthTop < 1 || hexLengthTopLeft < 1 || hexLengthBotLeft < 1 || hexLengthBot < 1)
                throw new SimulatorStateException("Invalid hexagon size, all lengths must be at least 1");
            if (hexLengthBot + hexLengthBotLeft <= hexLengthTop)
                throw new SimulatorStateException("Invalid hexagon size, bottom line is too short");
            if (hexLengthBot >= hexLengthTop + hexLengthTopLeft)
                throw new SimulatorStateException("Invalid hexagon size, bottom line is too long");

            // Now compute the remaining side lengths
            // Side4 + Side5 = Side1 + Side2  <=>  Side5 = Side1 + Side2 - Side4
            // Side4 = Side1 + Side2 - (Side3 + Side2 - Side0) = Side0 + Side1 - Side3
            int hexLengthBotRight = hexLengthTop + hexLengthTopLeft - hexLengthBot;
            int hexLengthTopRight = hexLengthTopLeft + hexLengthBotLeft - hexLengthBotRight;

            // DEBUG: Draw lines
            Vector2Int u = Vector2Int.zero;
            Vector2Int w = new Vector2Int(hexLengthTop, 0);
            ld.AddLine(u, w, Color.red, false);
            w = new Vector2Int(0, -hexLengthTopLeft);
            ld.AddLine(u, w, Color.red, false);
            u = w + new Vector2Int(hexLengthBotLeft, -hexLengthBotLeft);
            ld.AddLine(u, w, Color.red, false);
            w = u + new Vector2Int(hexLengthBot, 0);
            ld.AddLine(u, w, Color.red, false);
            u = w + new Vector2Int(0, hexLengthBotRight);
            ld.AddLine(u, w, Color.red, false);
            w = u + new Vector2Int(-hexLengthTopRight, hexLengthTopRight);
            ld.AddLine(u, w, Color.red, false);

            HexagonTestParticle.hexLine0 = intToBinary(hexLengthTop);
            HexagonTestParticle.hexLine1 = intToBinary(hexLengthTopLeft);
            HexagonTestParticle.hexLine2 = intToBinary(hexLengthBotLeft);
            HexagonTestParticle.hexLine3 = intToBinary(hexLengthBot);
            HexagonTestParticle.hexLine4 = intToBinary(hexLengthBotRight);
            HexagonTestParticle.hexLine5 = intToBinary(hexLengthTopRight);
        }

        private string intToBinary(int num)
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

} // namespace AS2.Algos.HexagonTest
