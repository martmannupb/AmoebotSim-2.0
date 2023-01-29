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
        public ParticleAttribute<PSPlacementMode> placementHead;
        public ParticleAttribute<PSPlacementMode> placementTail;

        public ExpandedCircuitTestParticle(Particle p) : base(p)
        {
            moveDir = CreateAttributeDirection("Move Direction", Direction.NONE);
            placementHead = CreateAttributeEnum<PSPlacementMode>("Placement Head", PSPlacementMode.NONE);
            placementTail = CreateAttributeEnum<PSPlacementMode>("Placement Tail", PSPlacementMode.NONE);

            SetMainColor(Color.gray);
        }

        public void Init(Direction moveDir = Direction.NONE, PSPlacementMode placementHead = PSPlacementMode.NONE,
            PSPlacementMode placementTail = PSPlacementMode.NONE)
        {
            this.moveDir.SetValue(moveDir);
            this.placementHead.SetValue(placementHead);
            this.placementTail.SetValue(placementTail);
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

            // Set partition set placement
            Log.Debug("My placement:\nHEAD: " + placementHead.GetValue() + "\nTAIL: " + placementTail.GetValue());
            if (placementHead != PSPlacementMode.NONE)
            {
                SetPlacement(pc, placementHead, true);
            }
            if (expanded && placementTail != PSPlacementMode.NONE)
            {
                SetPlacement(pc, placementTail, false);
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

        private void SetPlacement(PinConfiguration pc, PSPlacementMode placementMode, bool head)
        {
            pc.SetPSPlacementMode(placementMode, head);
            if (placementMode == PSPlacementMode.LINE_ROTATED)
            {
                float angle = Random.Range(0f, 360f);
                pc.SetLineRotation(angle, head);
            }
            else if (placementMode == PSPlacementMode.MANUAL)
            {
                string s = "Manual partition set positions (" + (head ? "HEAD" : "TAIL") + "):\n";
                foreach (PartitionSet ps in pc.GetNonEmptyPartitionSets())
                {
                    // Only look at partition sets with at least 2 pins
                    if (ps.GetPinIds().Length < 2)
                        continue;

                    // Count number of pins on head/tail
                    int n = 0;
                    foreach (Pin p in ps.GetPins())
                    {
                        if (p.IsOnHead && head || p.IsOnTail && !head)
                            n++;
                    }
                    if (n > 0)
                    {
                        Vector2 coords = new Vector2(Random.Range(0f, 360f), Random.Range(0.3f, 0.9f));
                        ps.SetPosition(coords, head);
                        s += coords + "\n";
                    }
                }
                Log.Debug(s);
            }
        }
    }


    // Initialization method
    public class ExpandedCircuitTestInitializer : InitializationMethod
    {
        public ExpandedCircuitTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        public void Generate(int numParticles = 10, Initialization.Compass moveDir = Initialization.Compass.E,
            PSPlacementMode placementHead = PSPlacementMode.NONE, PSPlacementMode placementTail = PSPlacementMode.NONE)
        {
            Direction movementDir = DirectionHelpers.FromInitDir(moveDir);

            PlaceParallelogram(Vector2Int.zero, movementDir.Rotate60(1), numParticles);
            foreach (InitializationParticle ip in GetParticles())
            {
                ip.SetAttributes(new object[] { movementDir, placementHead, placementTail });
            }
        }
    }

} // namespace AS2.Algos.ExpandedCircuitTest
