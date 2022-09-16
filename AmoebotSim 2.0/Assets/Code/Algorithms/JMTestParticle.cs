
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
            case 1: Activate1();
                break;
            default: return;
        }
    }

    // Expand East with shearing
    private void Activate0()
    {
        Expand(Direction.E);
        if (HasNeighborAt(Direction.NNE))
        {
            MarkBond(Direction.NNE);
        }
        terminated.SetValue(true);
    }

    // Expand East without shearing
    private void Activate1()
    {
        Expand(Direction.E);
        ReleaseBond(Direction.SSE);
        ReleaseBond(Direction.NNW);
        terminated.SetValue(true);
    }
}
