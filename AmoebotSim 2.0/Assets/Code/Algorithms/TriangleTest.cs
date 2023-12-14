using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.TriangleTest
{

    /// <summary>
    /// Algorithm for finding empty triangle candidates and
    /// testing scenarios more easily than on paper.
    /// <para>
    /// Phase 1: CORNER_DIST
    /// Find distance to boundary in all 6 directions and determine
    /// corner candidates as particles that have enough distance in
    /// two adjacent directions
    /// Round 0:
    ///     (Listen for beep on global circuit)
    ///     (If no beep on global circuit: Go to phase 2)
    ///     Setup PASC circuit
    ///     Direction leader sends beep on primary partition set
    /// Round 1:
    ///     Receive beep on PASC circuits
    ///     Become passive if beep is received on secondary set
    ///     Store received bit (1 for beep on secondary set, 0 on primary set)
    ///     Compare received bit to counter bit and update comparison result
    ///     Establish global circuit and beep if we became passive
    /// </para>
    /// <para>
    /// Phase 2: PARTNER_TEST
    /// Find distance to closest candidate in all 6 directions and
    /// remove candidates that do not have a partner close enough
    /// in one of the two directions
    /// Round 0:
    ///     (Listen for beep on global circuit)
    ///     (If no beep on global circuit: Terminate)
    ///     Setup PASC circuit
    ///     Send beeps in all directions in which we are a candidate (on primary partition set)
    /// Round 1:
    ///     Receive beep on PASC circuits
    ///     Become passive if beep is received on secondary set
    ///     Store received bit (1 for beep on secondary set, 0 on primary set)
    ///     Compare received bit to counter bit and update comparison result
    ///     Establish global circuit and beep if we became passive
    /// </para>
    /// </summary>
    public class TriangleTestParticle : ParticleAlgorithm
    {
        private enum Phase
        {
            CORNER_DIST,        // Determine corner candidates by checking boundary distance in all directions
            PARTNER_TEST        // Check if we have neighboring candidates not too far away
        }
        
        private enum Comparison
        {
            EQUAL, LESS, GREATER
        }

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Triangle Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(TriangleTestInitializer).FullName;

        private static readonly float pSetDistance = 0.7f;

        // Declare attributes here
        ParticleAttribute<string> counter;

        ParticleAttribute<int> round;
        ParticleAttribute<Phase> phase;
        ParticleAttribute<bool> finished;

        ParticleAttribute<bool>[] cornerCandidate;      // 0 = lower left, 1 = lower right, 2 = top

        ParticleAttribute<int> counterIdx;
        ParticleAttribute<bool>[] pascActive;
        ParticleAttribute<Comparison>[] comparison;


        public TriangleTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            counter = CreateAttributeString("Counter", "");

            round = CreateAttributeInt("Round", -1);
            phase = CreateAttributeEnum<Phase>("Phase", Phase.CORNER_DIST);
            finished = CreateAttributeBool("Finished", false);

            cornerCandidate = new ParticleAttribute<bool>[3];
            for (int i = 0; i < 3; i++)
                cornerCandidate[i] = CreateAttributeBool("Corner cand. [" + i + "]", false);

            counterIdx = CreateAttributeInt("Counter idx", 0);
            pascActive = new ParticleAttribute<bool>[6];
            comparison = new ParticleAttribute<Comparison>[6];
            for (int i = 0; i < 6; i++)
            {
                pascActive[i] = CreateAttributeBool("PASC Active [" + i + "]", true);
                comparison[i] = CreateAttributeEnum<Comparison>("Comparison [" + i + "]", Comparison.EQUAL);
            }

            // Also, set the default initial color
            SetMainColor(Color.gray);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(string counter = "")
        {
            // This code is executed directly after the constructor
            this.counter.SetValue(counter);
        }

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

            // Initialization
            if (round == -1)
            {
                Initialize();
                round.SetValue(1);
                return;
            }

            switch (phase.GetValue())
            {
                case Phase.CORNER_DIST:
                    ActivateCornerDist();
                    break;
                case Phase.PARTNER_TEST:
                    ActivatePartnerTest();
                    break;
                default:
                    break;
            }
        }

        private void ActivateCornerDist()
        {
            if (round == 0)
            {
                // Receive beep on global circuit
                PinConfiguration pc = GetCurrentPinConfiguration();
                // Transition to next phase if no beep was received
                if (!pc.ReceivedBeepOnPartitionSet(0))
                {
                    phase.SetValue(Phase.PARTNER_TEST);

                    // Determine which triangle corners we can be
                    bool[] spaceInDirection = new bool[6];
                    for (int i = 0; i < 6; i++)
                    {
                        if (comparison[i] == Comparison.GREATER || comparison[i] == Comparison.EQUAL)
                            spaceInDirection[i] = true;
                    }

                    bool lowerLeft = spaceInDirection[0] && spaceInDirection[1] &&
                        HasNeighborAt(Direction.W) && HasNeighborAt(Direction.SSW);
                    bool lowerRight = spaceInDirection[2] && spaceInDirection[3] &&
                        HasNeighborAt(Direction.SSE) && HasNeighborAt(Direction.E);
                    bool top = spaceInDirection[4] && spaceInDirection[5] &&
                        HasNeighborAt(Direction.NNW) && HasNeighborAt(Direction.NNE);

                    // Set color based on that
                    Color c = new Color(0.25f, 0.25f, 0.25f);
                    if (lowerLeft)  // green
                    {
                        c.g += 0.5f;
                        cornerCandidate[0].SetValue(true);
                    }
                    if (lowerRight) // blue
                    {
                        c.b += 0.5f;
                        cornerCandidate[1].SetValue(true);
                    }
                    if (top)        // red
                    {
                        c.r += 0.5f;
                        cornerCandidate[2].SetValue(true);
                    }
                    SetMainColor(c);

                    // Reset PASC and start sending beep
                    for (int i = 0; i < 6; i++)
                        pascActive[i].SetValue(true);
                    pc = SetupPASC();


                    // Send beep on primary circuit if we are direction leader
                    for (int i = 0; i < 6; i++)
                    {
                        if (IsDirectionCand(i))
                            pc.SendBeepOnPartitionSet(2 * i);
                    }

                    // Reset counter and comparison results
                    counterIdx.SetValue(0);
                    for (int i = 0; i < 6; i++)
                        comparison[i].SetValue(Comparison.EQUAL);

                    round.SetValue(1);

                    return;
                }

                // Setup PASC circuit again
                pc = SetupPASC();

                // Direction leader sends beep on primary circuit
                for (int i = 0; i < 6; i++)
                {
                    if (!HasNeighborAt(DirectionHelpers.Cardinal(i)))
                        pc.SendBeepOnPartitionSet(2 * i);
                }

                round.SetValue(1);
            }
            else if (round == 1)
            {
                ActivateRound1();
            }
        }

        private void ActivatePartnerTest()
        {
            if (round == 0)
            {
                // Receive beep on global circuit
                PinConfiguration pc = GetCurrentPinConfiguration();
                // Transition to next phase if no beep was received
                if (!pc.ReceivedBeepOnPartitionSet(0))
                {
                    finished.SetValue(true);
                    SetupPASC();

                    // Determine which triangle corners we can no longer be
                    bool[] resultsByDirection = new bool[6];
                    for (int i = 0; i < 6; i++)
                    {
                        if (comparison[i] == Comparison.LESS || comparison[i] == Comparison.EQUAL)
                            resultsByDirection[i] = true;
                    }
                    // Set color based on the result
                    Color c = new Color(0.25f, 0.25f, 0.25f);
                    if (cornerCandidate[0])     // lower left, green
                    {
                        if (!resultsByDirection[0] || !resultsByDirection[1])
                            cornerCandidate[0].SetValue(false);
                        else
                            c.g += 0.5f;
                    }
                    if (cornerCandidate[1])     // lower right, blue
                    {
                        if (!resultsByDirection[2] || !resultsByDirection[3])
                            cornerCandidate[1].SetValue(false);
                        else
                            c.b += 0.5f;
                    }
                    if (cornerCandidate[2])     // top, red
                    {
                        if (!resultsByDirection[4] || !resultsByDirection[5])
                            cornerCandidate[2].SetValue(false);
                        else
                            c.r += 0.5f;
                    }

                    //phase.SetValue(Phase.PARTNER_TEST);

                    //// Determine which triangle corners we can be

                    //bool lowerLeft = spaceInDirection[0] && spaceInDirection[1] &&
                    //    HasNeighborAt(Direction.W) && HasNeighborAt(Direction.SSW);
                    //bool lowerRight = spaceInDirection[2] && spaceInDirection[3] &&
                    //    HasNeighborAt(Direction.SSE) && HasNeighborAt(Direction.E);
                    //bool top = spaceInDirection[4] && spaceInDirection[5] &&
                    //    HasNeighborAt(Direction.NNW) && HasNeighborAt(Direction.NNE);

                    //// Set color based on that
                    //Color c = new Color(0.25f, 0.25f, 0.25f);
                    //if (lowerLeft)  // green
                    //{
                    //    c.g += 0.5f;
                    //    cornerCandidate[0].SetValue(true);
                    //}
                    //if (lowerRight) // blue
                    //{
                    //    c.b += 0.5f;
                    //    cornerCandidate[1].SetValue(true);
                    //}
                    //if (top)        // red
                    //{
                    //    c.r += 0.5f;
                    //    cornerCandidate[2].SetValue(true);
                    //}
                    SetMainColor(c);

                    //// Reset PASC and start sending beep
                    //pc = SetupPASC();

                    //// Send beep on primary circuit if we are direction leader
                    //for (int i = 0; i < 6; i++)
                    //{
                    //    if (IsDirectionCand(i))
                    //        pc.SendBeepOnPartitionSet(2 * i);
                    //}

                    return;
                }

                // Setup PASC circuit again
                pc = SetupPASC();

                // Direction leader sends beep on primary circuit
                for (int i = 0; i < 6; i++)
                {
                    if (IsDirectionCand(i))
                        pc.SendBeepOnPartitionSet(2 * i);
                }

                round.SetValue(1);
            }
            else if (round == 1)
            {
                bool[] receivedBeeps = ActivateRound1();
                // If we did not receive a beep in one direction:
                // The distance is infinite, so the comparison result is greater
                for (int i = 0; i < 6; i++)
                    if (!receivedBeeps[i])
                        comparison[i].SetValue(Comparison.GREATER);
            }
        }

        // Round 1 activation shared by the phases
        // Returns true for each direction in which a beep was received
        private bool[] ActivateRound1()
        {
            // Receive beep on PASC circuit
            PinConfiguration pc = GetCurrentPinConfiguration();

            bool becamePassive = false;
            bool[] receivedBeeps = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                int primary = 2 * i;
                int secondary = 2 * i + 1;

                bool beepOnPrimary = pc.ReceivedBeepOnPartitionSet(primary);
                bool beepOnSecondary = pc.ReceivedBeepOnPartitionSet(secondary);

                receivedBeeps[i] = beepOnPrimary || beepOnSecondary;

                // Store received bit
                bool bit;
                // The beep is only 0 of we receive a beep on the primary circuit
                // => If we receive no beep at all, the distance will be maximal
                // (but this is not sufficient yet)
                bit = !beepOnPrimary;

                // Compare counter and distance bit
                bool counterBit;
                if (counterIdx < counter.GetValue().Length)
                {
                    counterBit = counter.GetValue()[counterIdx] == '1';
                    counterIdx.SetValue(counterIdx + 1);
                }
                else
                    counterBit = false;

                if (bit && !counterBit)
                    comparison[i].SetValue(Comparison.GREATER);    // Distance is greater than counter
                else if (!bit && counterBit)
                    comparison[i].SetValue(Comparison.LESS);       // Distance is less than counter

                // Become inactive if beep was received on secondary set
                if (pascActive[i] && HasNeighborAt(d) && !beepOnPrimary)
                {
                    pascActive[i].SetValue(false);
                    becamePassive = true;
                }
            }

            // Setup global circuit
            pc.SetToGlobal(0);
            pc.ResetPartitionSetPlacement();
            SetPlannedPinConfiguration(pc);

            // Send beep if we became passive
            if (becamePassive)
                pc.SendBeepOnPartitionSet(0);

            round.SetValue(0);
            return receivedBeeps;
        }

        private void Initialize()
        {
            for (int i = 0; i < 6; i++)
                pascActive[i].SetValue(true);

            PinConfiguration pc = SetupPASC();
            // Send beep on all primary partition sets
            for (int i = 0; i < 6; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                if (!HasNeighborAt(d))
                {
                    pc.SendBeepOnPartitionSet(2 * i);
                }
            }
        }

        private PinConfiguration SetupPASC()
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            for (int i = 0; i < 6; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);

                // Setup primary and secondary partition sets
                int primaryId = 2 * i;
                int secondaryId = 2 * i + 1;

                // Check if we are the direction leader
                // (Meaning that we send beeps in the opposite direction)
                bool isDirectionLeader;
                bool blockOppositeSide;
                if (phase.GetCurrentValue() == Phase.CORNER_DIST)
                {
                    isDirectionLeader = !HasNeighborAt(d);
                    blockOppositeSide = !HasNeighborAt(d.Opposite());
                }
                else
                {
                    isDirectionLeader = IsDirectionCand(i);
                    blockOppositeSide = false;
                }

                if (isDirectionLeader || blockOppositeSide)
                {
                    // Direction leader: Do not connect both directions
                    pc.MakePartitionSet(new int[] {
                        pc.GetPinAt(d.Opposite(), 0).Id
                    }, primaryId);
                    pc.MakePartitionSet(new int[] {
                        pc.GetPinAt(d.Opposite(), 1).Id
                    }, secondaryId);
                }
                else
                {
                    // Not the leader
                    bool isActive = pascActive[i].GetCurrentValue();
                    pc.MakePartitionSet(new int[] {
                        pc.GetPinAt(d, isActive ? 2 : 3).Id,
                        pc.GetPinAt(d.Opposite(), 0).Id
                    }, primaryId);
                    pc.MakePartitionSet(new int[] {
                        pc.GetPinAt(d, isActive ? 3 : 2).Id,
                        pc.GetPinAt(d.Opposite(), 1).Id
                    }, secondaryId);

                    pc.SetPartitionSetPosition(primaryId, new Vector2(i * 60.0f + 30.0f, pSetDistance));
                    pc.SetPartitionSetPosition(secondaryId, new Vector2(i * 60.0f + 10.0f, pSetDistance * 0.9f));
                }
            }

            SetPlannedPinConfiguration(pc);
            return pc;
        }

        private bool IsDirectionCand(int i)
        {
            return (i == 0 || i == 5) && cornerCandidate[1].GetCurrentValue() ||
                (i == 1 || i == 2) && cornerCandidate[2].GetCurrentValue() ||
                (i == 3 || i == 4) && cornerCandidate[0].GetCurrentValue();
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class TriangleTestInitializer : InitializationMethod
    {
        public TriangleTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 150, float holeProb = 0.1f, bool fillHoles = false, bool prioritizeInner = true, float lambda = 0.1f,
            string counter = "0001")
        {
            InitializationParticle p;
            //foreach (Vector2Int v in GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb, fillHoles, null, true, prioritizeInner, lambda))
            //{
            //    p = AddParticle(v);
            //    p.SetAttribute("counter", counter);
            //}

            // Build a triangle
            int l = 15;
            for (int y = 0; y < l; y++)
            {
                for (int x = 0; x < l - y; x++)
                {
                    p = AddParticle(new Vector2Int(x, y));
                    p.SetAttribute("counter", counter);
                }
            }
            // Remove some parts
            for (int y = 2; y < 7; y += 2)
            {
                for (int x = 2; x < l - y - 1; x += 2)
                {
                    RemoveParticleAt(new Vector2Int(x, y));
                }
            }
        }
    }

} // namespace AS2.Algos.TriangleTest
