using AS2.Sim;
using UnityEngine;
using AS2.Subroutines;

namespace AS2.Algos.SubroutineTest
{

    /// <summary>
    /// Tests the subroutine mechanism by electing a leader
    /// on every boundary of the system independently.
    /// <para>
    /// Runs 3 instances of the leader election subroutine in
    /// parallel. Uses a global circuit on which a beep is
    /// sent regularly as long as some leader election
    /// procedure has not finished.
    /// </para>
    /// </summary>
    public class SubroutineTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Subroutine Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 2;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SubroutineTestInitializer).FullName;

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
        private ParticleAttribute<bool> finished;

        private ParticleAttribute<int> round;

        private ParticleAttribute<int> numBoundaries;
        private ParticleAttribute<int>[,] boundaries;

        private Subroutines.LeaderElection.SubLeaderElection[] subLEs;

        public SubroutineTestParticle(Particle p) : base(p)
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

            subLEs = new Subroutines.LeaderElection.SubLeaderElection[3];
            for (int i = 0; i < 3; i++)
                subLEs[i] = new Subroutines.LeaderElection.SubLeaderElection(p);
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
            // No movements
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (finished)
                return;

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
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (numBoundaries.GetCurrentValue() > 0)
                    SetupPinConfiguration(pc);
                else
                    pc.SetToGlobal(0);
                SetPlannedPinConfiguration(pc);

                // Initialize and activate leader election subroutines
                for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                {
                    subLEs[i].Init(i, false, 3);
                    subLEs[i].ActivateReceive();
                    subLEs[i].ActivateSend();
                }

                UpdateColor();
                round.SetValue(1);
                return;
            }

            // During the computation
            // Establish global circuit and send beep if we are not finished every 4 rounds
            if (round == 0)
            {
                // Listen for beep on global circuit
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (!pc.ReceivedBeepOnPartitionSet(0))
                {
                    // Received no beep, that means everybody is finished
                    finished.SetValue(true);
                    if (numBoundaries > 0)
                    {
                        SetupPinConfiguration(pc);
                        SetPlannedPinConfiguration(pc);
                    }
                    UpdateColor();
                    return;
                }

                // Not finished, continue by sending beeps
                if (numBoundaries > 0)
                {
                    SetupPinConfiguration(pc);
                    SetPlannedPinConfiguration(pc);

                    for (int i = 0; i < numBoundaries; i++)
                        subLEs[i].ActivateSend();
                }
            }
            else if (round == 3)
            {
                // Activate receive
                bool leFinished = true;
                for (int i = 0; i < numBoundaries; i++)
                {
                    subLEs[i].ActivateReceive();
                    if (!subLEs[i].IsFinished())
                        leFinished = false;
                }

                // Send beep on global circuit if we are not finished yet
                PinConfiguration pc = GetCurrentPinConfiguration();
                pc.SetToGlobal(0);
                pc.ResetPartitionSetPlacement();
                SetPlannedPinConfiguration(pc);

                if (!leFinished)
                    pc.SendBeepOnPartitionSet(0);
            }
            else
            {
                // Normal computation round
                for (int i = 0; i < numBoundaries.GetCurrentValue(); i++)
                {
                    subLEs[i].ActivateReceive();
                    subLEs[i].ActivateSend();
                }
            }

            UpdateColor();
            round.SetValue((round + 1) % 4);
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

        private void UpdateColor()
        {
            if (numBoundaries.GetCurrentValue() == 0)
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
    public class SubroutineTestInitializer : InitializationMethod
    {
        public SubroutineTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 100, float holeProb = 0.1f, bool fillHoles = false, bool prioritizeInner = true, float lambda = 0.1f)
        {
            // The parameters of the Init() method can be set as particle attributes here
            foreach (Vector2Int pos in GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb, fillHoles, null, false, prioritizeInner, lambda))
                AddParticle(pos);
        }
    }

} // namespace AS2.Algos.SubroutineTest
