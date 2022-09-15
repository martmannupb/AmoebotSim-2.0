
public class JMTestParticle : ParticleAlgorithm
{
    public override int PinsPerEdge => 1;

    private ParticleAttribute<int> index;
    private ParticleAttribute<bool> terminated;

    public JMTestParticle(Particle p, int i) : base(p)
    {
        SetMainColor(ColorData.Particle_Black);

        index = CreateAttributeInt("Index", i);
        terminated = CreateAttributeBool("Terminated", false);
    }

    public override void Activate()
    {
    }

    public override void ActivateBeep()
    {
    }

    public override void ActivateMove()
    {
        if (terminated)
            return;

        switch (index)
        {
            case 0: Activate0();
                break;
            default: return;
        }
    }

    // Two contracted particles, not doing anything
    private void Activate0()
    {
        Expand(Direction.E);
        if (HasNeighborAt(Direction.NNE))
        {
            MarkBond(Direction.NNE);
        }
        terminated.SetValue(true);
    }
}
