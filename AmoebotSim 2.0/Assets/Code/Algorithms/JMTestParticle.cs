using UnityEngine;

public class JMTestParticle : ParticleAlgorithm
{
    public override int PinsPerEdge => 1;

    private ParticleAttribute<int> mode;
    private ParticleAttribute<int> role;
    private ParticleAttribute<bool> terminated;

    public JMTestParticle(Particle p, int mode_, int role_) : base(p)
    {
        Debug.Log("Initial role: " + role_);
        SetMainColor(ColorData.Particle_Black);

        this.mode = CreateAttributeInt("Mode", mode_);
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
            case 2: Activate2();
                break;
            case 3: Activate3();
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
        MarkBond(Direction.SSE);
        terminated.SetValue(true);
    }

    // Different situations of one contracted and one expanded particle

    // Contracted particle moves with none or both bonds active, expanded particle does not move
    private void Activate2()
    {
        // Particle 0 is contracted, particle 1 is expanded
        if (role == 0)
        {
            string s = "Contracted particle:";

            Direction d = DirectionHelpers.Cardinal(Random.Range(0, 6));
            s += "\nDirection: " + d;
            // If expanding into neighbor: Mark both bonds
            if (d == Direction.NNW || d == Direction.NNE)
            {
                s += "\nMark both bonds";
                MarkBond(Direction.NNW);
                MarkBond(Direction.NNE);
            }
            // If expanding horizontally: Either mark both bonds or mark none
            if (d == Direction.W || d == Direction.E)
            {
                s += "\nExpanding horizontally, maybe mark the bonds...";
                if (Random.Range(0f, 1f) < 0.5f)
                {
                    s += "\n    Mark both bonds";
                    MarkBond(Direction.NNW);
                    MarkBond(Direction.NNE);
                }
                else
                {
                    s += "\n    Don't mark the bonds";
                }
            }
            Expand(d);
            Debug.Log(s);
        }
        else
        {
            Debug.Log("Expanded particle doing nothing");
        }
        terminated.SetValue(true);
    }

    // Only one bond, both particles can move
    private void Activate3()
    {
        // Particle 0 is contracted, particle 1 is expanded
        if (role == 0)
        {
            string s = "Contracted particle:";

            // Only keep the NNW bond
            ReleaseBond(Direction.NNE);

            // Expand randomly
            if (Random.Range(0f, 1f) < 0.5f)
            {
                Direction d = DirectionHelpers.Cardinal(Random.Range(0, 6));
                s += "\nExpand in direction " + d;

                // Must mark bond if expanding to NNE, must not mark it when expanding to SSW
                // Every other direction: Choose randomly (NNW and SSE don't leave a choice, though)
                if (d == Direction.NNE)
                {
                    s += "\nMust mark the bond";
                    MarkBond(Direction.NNW);
                }
                else if (d == Direction.W || d == Direction.E)
                {
                    s += "\nCan choose to mark randomly";
                    if (Random.Range(0f, 1f) < 0.5f)
                    {
                        s += "\n    Choose to mark";
                        MarkBond(Direction.NNW);
                    }
                    else
                    {
                        s += "\n    Choose not to mark";
                    }
                }

                Expand(d);
            }
            else
            {
                s += "\nDo not expand";
            }
            Debug.Log(s);
        }
        else
        {
            string s = "Expanded particle:";

            // Only keep the tail bond
            ReleaseBond(Direction.SSW, true);

            if (Random.Range(0f, 1f) < 0.5f)
            {
                if (Random.Range(0f, 1f) < 0.5f)
                {
                    s += "\nContract into Tail";
                    ContractTail();
                }
                else
                {
                    s += "\nContract into Head";
                    ContractHead();
                }
            }
            else
            {
                s += "\nDo not contract";
            }
            Debug.Log(s);
        }
        terminated.SetValue(true);
    }
}
