using UnityEngine;

public class JMTestParticle : ParticleAlgorithm
{
    public override int PinsPerEdge => 1;

    private ParticleAttribute<int> mode;
    private ParticleAttribute<int> role;
    private ParticleAttribute<bool> terminated;

    public JMTestParticle(Particle p, int mode, int role_) : base(p)
    {
        Debug.Log("Initial role: " + role_);
        SetMainColor(ColorData.Particle_Black);

        this.mode = CreateAttributeInt("Mode", mode);
        this.role = CreateAttributeInt("Role", role_);
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

        switch (mode)
        {
            case 0: Activate0();
                break;
            case 1: Activate1();
                break;
            case 2:
                Activate2();
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

    private void Activate2()
    {
        Debug.Log("Role: " + role);
        // Particle 0 is contracted, particle 1 is expanded
        if (role == 0)
        {
            Direction d = DirectionHelpers.Cardinal(Random.Range(0, 6));
            // If expanding into neighbor: Mark both bonds
            if (d == Direction.NNW || d == Direction.NNE)
            {
                MarkBond(Direction.NNW);
                MarkBond(Direction.NNE);
            }
            // If expanding horizontally: Either mark both bonds or mark none
            if (d == Direction.W || d == Direction.E)
            {
                if (Random.Range(0f, 1f) < 0.5f)
                {
                    MarkBond(Direction.NNW);
                    MarkBond(Direction.NNE);
                }
            }
            Expand(d);
        }
        else
        {
            Debug.Log("Role: 1");
        }
        terminated.SetValue(true);
    }
}
