using AS2.Sim;
using UnityEngine;


namespace AS2.Algos.SubroutineTest2
{

    /// <summary>
    /// Tests the subroutine mechanism by electing a leader
    /// on every boundary of the system independently.
    /// <para>
    /// Runs 3 instances of the synchronized leader election subroutine in
    /// parallel, using a global circuit for synchronization. All particles
    /// participate in the leader election but only boundary particles start
    /// as candidates. Each boundary forms an election circuit
    /// </para>
    /// </summary>
    public class SubroutineTest2Particle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Subroutine Test 2";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 2;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SubroutineTest2Initializer).FullName;

        public static readonly float partitionSetDistance = 0.5f;               // Distance of partition sets from boundary
        public static readonly Color[] candidateColors = new Color[] {          // Candidate colors by number of candidacies (1, 2, 3)
            new Color(0.25f, 0.5f, 0.25f),
            new Color(0.25f, 0.75f, 0.25f),
            new Color(0.25f, 1f, 0.25f)
        };
        public static readonly Color[] phase2CandidateColors = new Color[] {    // Phase 2 candidate colors by number of candidacies (0, 1, 2, 3)
            new Color(0.15f, 0.15f, 0.25f),
            new Color(0.25f, 0.25f, 0.5f),
            new Color(0.25f, 0.25f, 0.75f),
            new Color(0.25f, 0.25f, 1f)
        };

        // Declare attributes here
        private ParticleAttribute<bool> finished;                               // Whether the algorithm has finished
        private ParticleAttribute<int> round;                                   // Round counter for synchronization

        private ParticleAttribute<int> numBoundaries;                           // The number of our boundaries
        private ParticleAttribute<int>[,] boundaries;                           // Boundary directions: [boundaryIndex, 0=predecessor;1=successor direction]

        private ParticleAttribute<int> kappa;                                   // Number of repetitions

        private Subroutines.LeaderElectionSync.SubLeaderElectionSync[] subLEs;  // Leader election subroutines by boundary index

        public SubroutineTest2Particle(Particle p) : base(p)
        {
            // Initialize the attributes here
            finished = CreateAttributeBool("Finished", false);
            round = CreateAttributeInt("Round", -1);

            numBoundaries = CreateAttributeInt("Num boundaries", 0);
            boundaries = new ParticleAttribute<int>[3, 2];
            for (int boundaryIdx = 0; boundaryIdx < 3; boundaryIdx++)
            {
                for (int predSuc = 0; predSuc < 2; predSuc++)
                {
                    boundaries[boundaryIdx, predSuc] = CreateAttributeInt("Boundary " + (boundaryIdx + 1) + " " + (predSuc == 0 ? "Pred" : "Succ"), -1);
                }
            }

            kappa = CreateAttributeInt("Kappa", 0);

            subLEs = new Subroutines.LeaderElectionSync.SubLeaderElectionSync[3];
            for (int i = 0; i < 3; i++)
                subLEs[i] = new Subroutines.LeaderElectionSync.SubLeaderElectionSync(p);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(int kappa = 3)
        {
            // This code is executed directly after the constructor
            this.kappa.SetValue(kappa);
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
            // No movements
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (finished)
                return;

            PinConfiguration pc = GetCurrentPinConfiguration();

            // Initialization in very first round
            if (round == -1)
            {
                if (!FindBoundaries())
                {
                    // If we are the only particle in the system: Return
                    finished.SetValue(true);
                    SetMainColor(ColorData.Particle_Green);
                    return;
                }

                // Setup pin configuration
                if (numBoundaries.GetCurrentValue() > 0)
                    SetupPinConfiguration(pc);
                else
                    pc.SetToGlobal(0);
                SetPlannedPinConfiguration(pc);

                // Initialize and activate leader election subroutines
                for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                {
                    subLEs[i].Init(i, 0, false, kappa, true);
                    subLEs[i].ActivateSend();
                }
                if (numBoundaries.GetCurrentValue() == 0)
                {
                    subLEs[0].Init(0, 0, false, kappa, false);
                    subLEs[0].ActivateSend();
                }

                UpdateColor();
                round.SetValue(1);
                return;
            }

            // During the computation
            // Activate LE subroutines (always have number 0)
            subLEs[0].ActivateReceive();
            for (int i = 1; i < numBoundaries; i++)
                subLEs[i].ActivateReceive();

            // Check if we are finished
            if (subLEs[0].IsFinished())
            {
                finished.SetValue(true);
                if (numBoundaries > 0)
                {
                    SetupPinConfiguration(pc);
                    SetPlannedPinConfiguration(pc);
                }
                UpdateColor();
                return;
            }

            // Setup the correct circuit
            if (subLEs[0].NeedSyncCircuit())
            {
                pc.SetToGlobal(0);
                pc.ResetPartitionSetPlacement();
            }
            else if (numBoundaries > 0)
                SetupPinConfiguration(pc);
            SetPlannedPinConfiguration(pc);

            // Activate
            subLEs[0].ActivateSend();
            for (int i = 1; i < numBoundaries; i++)
                subLEs[i].ActivateSend();

            //// During the computation
            //// Establish global circuit and send beep if we are not finished every 4 rounds
            //if (round == 0)
            //{
            //    // Listen for beep on global circuit
            //    PinConfiguration pc = GetCurrentPinConfiguration();
            //    if (!pc.ReceivedBeepOnPartitionSet(0))
            //    {
            //        // Received no beep, that means everybody is finished
            //        finished.SetValue(true);
            //        if (numBoundaries > 0)
            //        {
            //            SetupPinConfiguration(pc);
            //            SetPlannedPinConfiguration(pc);
            //        }
            //        UpdateColor();
            //        return;
            //    }

            //    // Not finished, continue by sending beeps
            //    if (numBoundaries > 0)
            //    {
            //        SetupPinConfiguration(pc);
            //        SetPlannedPinConfiguration(pc);

            //        for (int i = 0; i < numBoundaries; i++)
            //            subLEs[i].ActivateSend();
            //    }
            //}
            //else if (round == 3)
            //{
            //    // Activate receive
            //    bool leFinished = true;
            //    for (int i = 0; i < numBoundaries; i++)
            //    {
            //        subLEs[i].ActivateReceive();
            //        if (!subLEs[i].IsFinished())
            //            leFinished = false;
            //    }

            //    // Send beep on global circuit if we are not finished yet
            //    PinConfiguration pc = GetCurrentPinConfiguration();
            //    pc.SetToGlobal(0);
            //    pc.ResetPartitionSetPlacement();
            //    SetPlannedPinConfiguration(pc);

            //    if (!leFinished)
            //        pc.SendBeepOnPartitionSet(0);
            //}
            //else
            //{
            //    // Normal computation round
            //    for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
            //    {
            //        subLEs[i].ActivateBeep();
            //    }
            //}

            UpdateColor();
            //round.SetValue((round + 1) % 4);
        }

        /// <summary>
        /// Helper that detects all boundaries in the first round.
        /// </summary>
        /// <returns><c>true</c> if and only if we are not the only
        /// particle in the system.</returns>
        private bool FindBoundaries()
        {
            bool[] occupied = new bool[6];
            int firstNbr = -1;
            for (int i = 0; i < 6; i++)
            {
                occupied[i] = HasNeighborAt(DirectionHelpers.Cardinal(i));
                if (occupied[i] && firstNbr == -1)
                    firstNbr = i;
            }

            // No neighbor found: We are alone!
            if (firstNbr == -1)
                return false;

            int boundaryIdx = 0;
            int curDir = firstNbr;
            for (int i = 0; i < 6; i++)
            {
                bool nbrAtCurrent = occupied[curDir];
                bool nbrAtNext = occupied[(curDir + 5) % 6];

                if (nbrAtCurrent && !nbrAtNext)
                {
                    // Boundary predecessor
                    boundaries[boundaryIdx, 0].SetValue(curDir);
                }
                else if (!nbrAtCurrent && nbrAtNext)
                {
                    // Boundary successor
                    boundaries[boundaryIdx, 1].SetValue((curDir + 5) % 6);
                    boundaryIdx++;
                }

                curDir = (curDir + 5) % 6;
            }

            numBoundaries.SetValue(boundaryIdx);

            return true;
        }

        /// <summary>
        /// Sets up the boundary pin configuration by connecting the
        /// predecessor and successor using the pins closest to that
        /// boundary. Partition set IDs are the boundary indices.
        /// </summary>
        /// <param name="pc">The pin configuration to be changed.</param>
        private void SetupPinConfiguration(PinConfiguration pc)
        {
            for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
            {
                Direction dirPred = DirectionHelpers.Cardinal(boundaries[i, 0].GetCurrentValue());
                Direction dirSucc = DirectionHelpers.Cardinal(boundaries[i, 1].GetCurrentValue());

                pc.MakePartitionSet(new int[] {
                    pc.GetPinAt(dirPred, 0).Id,
                    pc.GetPinAt(dirSucc, 1).Id
                }, i);

                // Also set nicer partition set placement
                float angle = (dirSucc.ToInt() + dirSucc.DistanceTo(dirPred) / 4f) * 60f;
                pc.SetPartitionSetPosition(i, new Vector2(angle, partitionSetDistance));
            }
        }

        /// <summary>
        /// Updates the particle's color based on its candidacies.
        /// Leaders and candidates are green, being brighter the
        /// more leaderships/candidacies they have. Phase 2
        /// candidates are shades of blue, again depending on
        /// how many candidacies they have. Inner particles turn
        /// black when the leader election is finished.
        /// </summary>
        private void UpdateColor()
        {
            if (numBoundaries.GetCurrentValue() == 0 && subLEs[0].IsFinished())
                SetMainColor(ColorData.Particle_Black);
            else
            {
                // Count on how many boundaries we are a (phase 2) candidate
                int numCandidacies = 0;
                int numPhase2Candidacies = 0;

                for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                {
                    if (subLEs[i].IsCandidate() || subLEs[i].IsLeader())
                        numCandidacies++;
                    if (subLEs[i].IsPhase2Candidate())
                        numPhase2Candidacies++;
                }
                if (numBoundaries.GetCurrentValue() == 0 && subLEs[0].IsPhase2Candidate())
                    numPhase2Candidacies++;

                // Color depends on candidacies
                if (numCandidacies > 0)
                    SetMainColor(candidateColors[numCandidacies - 1]);
                else
                    SetMainColor(phase2CandidateColors[numPhase2Candidacies]);
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SubroutineTest2Initializer : InitializationMethod
    {
        public SubroutineTest2Initializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 100, float holeProb = 0.1f, bool fillHoles = false, bool prioritizeInner = true, float lambda = 0.1f, int kappa = 3)
        {
            InitializationParticle p;
            foreach (Vector2Int pos in GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb, fillHoles, null, false, prioritizeInner, lambda))
            {
                p = AddParticle(pos);
                p.SetAttribute("kappa", kappa);
            }
        }
    }

} // namespace AS2.Algos.SubroutineTest2
