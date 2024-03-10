using System.Collections.Generic;
using UnityEngine;
using static AS2.Constants;
using AS2.Sim;
using AS2.Subroutines.SingleSourceSP;

namespace AS2.Algos.Sub1SPFTest
{

    public class Sub1SPFTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Sub 1-SPF Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(Sub1SPFTestInitializer).FullName;

        [StatusInfo("Show SP Tree", "Draws the parent edges of all amoebots as soon as the algorithm has finished.")]
        public static void ShowTree(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            AS2.UI.LineDrawer ld = AS2.UI.LineDrawer.Instance;
            ld.Clear();

            foreach (Particle p in system.particles)
            {
                Sub1SPFTestParticle algo = (Sub1SPFTestParticle)p.algorithm;
                Vector2Int pos = p.Head();
                Direction d = algo.parent;
                if (d == Direction.NONE)
                    continue;

                Vector2 parent = pos + (Vector2)ParticleSystem_Utils.DirectionToVector(d) * 0.9f;
                ld.AddLine(pos, parent, Color.cyan, true, 1.5f, 1.5f);
            }
            ld.SetTimer(20f);
        }

        // Declare attributes here
        ParticleAttribute<int> round;
        ParticleAttribute<bool> isSource;
        ParticleAttribute<bool> isDest;
        ParticleAttribute<Direction> parent;

        Sub1SPF singleSourceSP;

        public Sub1SPFTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            isSource = CreateAttributeBool("Source", false);
            isDest = CreateAttributeBool("Destination", false);
            parent = CreateAttributeDirection("Parent", Direction.NONE);

            singleSourceSP = new Sub1SPF(p);
            // Also, set the default initial color
            SetMainColor(Color.gray);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool isSource = false, bool isDest = false)
        {
            this.isSource.SetValue(isSource);
            this.isDest.SetValue(isDest);
            if (isSource)
                SetMainColor(ColorData.Particle_Red);
            else if (isDest)
                SetMainColor(ColorData.Particle_BlueDark);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return round > 1;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            switch (round.GetValue())
            {
                case 0:
                    {
                        List<Direction> ignoreDirs = new List<Direction>();
                        foreach (Direction dir in DirectionHelpers.Iterate60(Direction.E, 6))
                        {
                            if (!HasNeighborAt(dir))
                                ignoreDirs.Add(dir);
                        }
                        singleSourceSP.Init(isSource, isDest, true, ignoreDirs);

                        PinConfiguration pc = GetContractedPinConfiguration();
                        singleSourceSP.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        singleSourceSP.ActivateSend();
                        round.SetValue(round + 1);
                    }
                    break;
                case 1:
                    {
                        singleSourceSP.ActivateReceive();

                        if (singleSourceSP.IsFinished())
                        {
                            parent.SetValue(singleSourceSP.Parent());
                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                            round.SetValue(round + 1);
                            break;
                        }

                        PinConfiguration pc = GetContractedPinConfiguration();
                        singleSourceSP.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        singleSourceSP.ActivateSend();
                    }
                    break;
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class Sub1SPFTestInitializer : InitializationMethod
    {
        public Sub1SPFTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numAmoebots = 100, int numDestinations = 10, float holeProb = 0.05f)
        {
            if (numAmoebots < 1)
            {
                Log.Error("Number of amoebots must be at least 1");
                throw new SimulatorStateException("Invalid number of amoebots");
            }

            // Generate positions without holes
            List<Vector2Int> positions = GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, true);
            foreach (Vector2Int v in positions)
                AddParticle(v);

            InitializationParticle ip;

            // Choose random source
            int sourceIdx = Random.Range(0, positions.Count);
            if (TryGetParticleAt(positions[sourceIdx], out ip))
                ip.SetAttribute("isSource", true);
            positions.RemoveAt(sourceIdx);

            // Choose random destinations
            numDestinations = Mathf.Min(numDestinations, positions.Count);
            for (int i = 0; i < numDestinations; i++)
            {
                int destIdx = Random.Range(0, positions.Count);
                if (TryGetParticleAt(positions[destIdx], out ip))
                    ip.SetAttribute("isDest", true);
                positions.RemoveAt(destIdx);
            }
        }
    }

} // namespace AS2.Algos.Sub1SPFTest
