using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.ExpandedCircuitTest
{

    public class ExpandedCircuitTestParticle : ParticleAlgorithm
    {
        public override int PinsPerEdge => 2;

        public static new string Name => "Expanded Circuit TEST";

        public static new string GenerationMethod => typeof(ExpandedCircuitTestInitializer).FullName;

        public ParticleAttribute<Direction> moveDir;

        public ExpandedCircuitTestParticle(Particle p) : base(p)
        {
            moveDir = CreateAttributeDirection("moveDir", Direction.NONE);

            SetMainColor(Color.gray);
        }

        public void Init(Direction moveDir)
        {
            this.moveDir.SetValue(moveDir);
        }

        public override void ActivateBeep()
        {
            SetRandomPC(IsExpanded());
        }

        public override void ActivateMove()
        {
            UseAutomaticBonds();

            if (IsExpanded())
            {
                ContractHead();
            }
            else
            {
                Expand(moveDir);
            }
        }

        private void SetRandomPC(bool expanded)
        {
            PinConfiguration pc = expanded ? GetExpandedPinConfiguration(HeadDirection()) : GetContractedPinConfiguration();
            int nPins = expanded ? (10 * PinsPerEdge) : (6 * PinsPerEdge);
            // Create random pin configuration
            // Change random number of partition sets by adding random pins
            int numChanges = Random.Range(1, 5);
            for (int i = 0; i < numChanges; i++)
            {
                int ps = Random.Range(0, nPins);
                int numChangedPins = Random.Range(1, 7);
                for (int j = 0; j < numChangedPins; j++)
                {
                    int pin = Random.Range(0, nPins);
                    pc.GetPartitionSet(ps).AddPin(pin);
                }
            }
            SetPlannedPinConfiguration(pc);
            BeepRandom(pc);
        }

        private void BeepRandom(PinConfiguration pc)
        {
            foreach (PartitionSet ps in pc.GetPartitionSets())
            {
                if (!ps.IsEmpty() && Random.Range(0.0f, 1.0f) < 0.25f)
                {
                    ps.SendBeep();
                }
            }
        }
    }


    // Initialization method
    public class ExpandedCircuitTestInitializer : InitializationMethod
    {
        public ExpandedCircuitTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        public void Generate(int numParticles = 10, Initialization.Compass moveDir = Initialization.Compass.E)
        {
            Direction movementDir = DirectionHelpers.FromInitDir(moveDir);

            PlaceParallelogram(Vector2Int.zero, movementDir.Rotate60(1), numParticles);
            foreach (InitializationParticle ip in GetParticles())
            {
                ip.SetAttributes(new object[] { movementDir });
            }
        }
    }

} // namespace AS2.Algos.ExpandedCircuitTest
