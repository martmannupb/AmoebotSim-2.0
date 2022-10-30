using UnityEngine;

public class ExpandedCircuitTestParticle : ParticleAlgorithm
{
    public override int PinsPerEdge => 2;

    public static new string Name => "Expanded Circuit TEST";

    public static new string GenerationMethod => typeof(ExpandedCircuitTestInitializer).FullName;

    public ExpandedCircuitTestParticle(Particle p) : base(p)
    {
        SetMainColor(Color.gray);
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
            Expand(DirectionHelpers.Cardinal(0));
        }
    }

    private void SetRandomPC(bool expanded)
    {
        PinConfiguration pc = expanded ? GetExpandedPinConfiguration(0) : GetContractedPinConfiguration();
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
    public ExpandedCircuitTestInitializer(ParticleSystem system) : base(system) { }

    public void Generate(int numParticles = 10)
    {
        PlaceParallelogram(Vector2Int.zero, Direction.NNE, numParticles);
    }
}
