using UnityEngine;

public class JMTestParticle : ParticleAlgorithm
{
    public override int PinsPerEdge => 1;

    private ParticleAttribute<int> mode;
    private ParticleAttribute<int> role;
    private ParticleAttribute<bool> terminated;

    public JMTestParticle(Particle p, int mode_, int role_) : base(p)
    {
        if (mode_ == 6)
        {
            if (role_ == 0)
            {
                SetMainColor(ColorData.Particle_Blue);
            }
            else if (role_ == 1)
            {
                SetMainColor(ColorData.Particle_Green);
            }
            else
            {
                SetMainColor(ColorData.Particle_Orange);
            }
        }
        else
        {
            SetMainColor(ColorData.Particle_Black);
        }

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
            case 4: Activate4();
                break;
            case 5: Activate5();
                break;
            case 6: Activate6();
                break;
            case 7: Activate7();
                break;
            case 8: Activate8();
                break;
            case 9:
            case 10:
                Activate9_10();
                break;
            case 11:
                Activate11();
                break;
            case 12: Activate12();
                break;
            case 13: Activate13();
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

    // Handover with a single bond
    private void Activate4()
    {
        // Particle 0 is contracted, particle 1 is expanded
        if (role == 0)
        {
            // Only keep the NNW bond
            ReleaseBond(Direction.NNE);

            // Push in that direction
            PushHandover(Direction.NNW);
        }
        else
        {
            // Only keep the tail bond
            ReleaseBond(Direction.SSW, true);

            // Pull the contracted neighbor into our tail
            PullHandoverHead(Direction.SSE);
        }
        terminated.SetValue(true);
    }

    // Handover with two bonds
    private void Activate5()
    {
        // Particle 0 is contracted, particle 1 is expanded
        if (role == 0)
        {
            // Push in that direction
            PushHandover(Direction.NNW);
        }
        else
        {
            // Pull the contracted neighbor into our tail
            PullHandoverHead(Direction.SSE);
        }
        terminated.SetValue(true);
    }

    // Handover with one bond and a third particle
    private void Activate6()
    {
        // Particle 0 is contracted and pushes, particle 1 is expanded and pulls
        if (role == 0)
        {
            // Only keep the NNW bond to the pulling particle
            ReleaseBond(Direction.NNE);

            // Push in one direction
            PushHandover(Direction.NNW);

            // Pull West neighbor with us
            MarkBond(Direction.W);
        }
        else if (role == 1)
        {
            // Only keep the tail bond to the pushing particle
            ReleaseBond(Direction.SSW, true);
            // Must release one of the bonds to our top neighbor
            ReleaseBond(Direction.NNW, true);
            // Must release the bond to the bottom left neighbor
            ReleaseBond(Direction.SSW, false);

            // Pull the contracted neighbor into our tail
            PullHandoverHead(Direction.SSE);

            // Transfer NNW neighbor to the pushing particle
            MarkBond(Direction.NNW, false);
        }
        // Particles 2, 3, 4, 5 have to release some bonds to enable the desired movements
        else if (role == 2)
        {
            ReleaseBond(Direction.NNE);
            ReleaseBond(Direction.SSE);
        }
        else if (role == 3)
        {
            ReleaseBond(Direction.NNW);
        }
        else if (role == 4)
        {
            ReleaseBond(Direction.E);
        }
        else if (role == 5)
        {
            ReleaseBond(Direction.W);
            ReleaseBond(Direction.SSE);
        }
        terminated.SetValue(true);
    }

    // Two expanded particles with a single bond
    private void Activate7()
    {
        string s = "";
        // Particle 0 is bottom, 1 is top
        if (role == 0)
        {
            // Release two of the bonds
            ReleaseBond(Direction.NNW, true);
            ReleaseBond(Direction.NNE, true);

            s += "Bottom particle:\n";
        }
        else
        {
            // Release two of the bonds
            ReleaseBond(Direction.SSE, false);
            ReleaseBond(Direction.SSW, true);

            s += "Top particle:\n";
        }

        // Randomly decide to contract
        if (Random.Range(0, 2) == 0)
        {
            if (Random.Range(0, 2) == 0)
            {
                s += "Contract HEAD";
                ContractHead();
            }
            else
            {
                s += "Contract TAIL";
                ContractTail();
            }
        }
        else
        {
            s += "Do not contract";
        }

        Debug.Log(s);
        terminated.SetValue(true);
    }

    // Two expanded particles with one bond plus contracted neighbors for handover
    private void Activate8()
    {
        string s = "";
        // Particles 0 and 1 are the bottom and top expanded ones
        // Particles 2 and 3 are the bottom and top contracted neighbors
        if (role == 0)
        {
            // Release two of the bonds
            ReleaseBond(Direction.NNW, true);
            ReleaseBond(Direction.NNE, true);

            // Perform handover with bottom neighbor
            PullHandoverHead(Direction.SSW);

            s += "Bottom particle:\n";

            // Randomly mark the bond to the expanded neighbor or not
            if (Random.Range(0, 2) == 0)
            {
                s += "MARK";
                MarkBond(Direction.NNE, false);
                SetMainColor(ColorData.Particle_Green);
            }
            else
            {
                s += "Do not mark";
                SetMainColor(ColorData.Particle_Red);
            }
            Debug.Log(s);
        }
        else if (role == 1)
        {
            // Release two of the bonds
            ReleaseBond(Direction.SSE, false);
            ReleaseBond(Direction.SSW, true);

            // Perform handover with top neighbor
            PullHandoverHead(Direction.NNW);

            s += "Top particle:\n";

            // Randomly mark the bond to the expanded neighbor or not
            if (Random.Range(0, 2) == 0)
            {
                s += "MARK";
                MarkBond(Direction.SSW, false);
                SetMainColor(ColorData.Particle_Green);
            }
            else
            {
                s += "Do not mark";
                SetMainColor(ColorData.Particle_Red);
            }
            Debug.Log(s);
        }
        else if (role == 2)
        {
            // Perform handover with top neighbor
            PushHandover(Direction.NNE);
        }
        else if (role == 3)
        {
            // Perform handover with bottom neighbor
            PushHandover(Direction.SSE);
        }

        terminated.SetValue(true);
    }

    // Two expanded particles with two bonds that share an end
    // In mode 10, there is also a contracted particle for handover
    private void Activate9_10()
    {
        if (role == 0)
        {
            // Randomly decide to contract or not
            if (Random.Range(0, 2) == 0)
            {
                if (Random.Range(0, 2) == 0)
                {
                    ContractHead();
                    SetMainColor(ColorData.Particle_Green);
                }
                else
                {
                    ContractTail();
                    SetMainColor(ColorData.Particle_Blue);
                }
            }
        }
        else if (mode == 10)
        {
            // Expanded and contracted particle perform handover and transfer one of the bonds
            if (role == 1)
            {
                PullHandoverTail(Direction.NNE);
                MarkBond(Direction.SSW, true);
            }
            else if (role == 2)
            {
                PushHandover(Direction.SSW);
            }
        }
        terminated.SetValue(true);
    }

    // Multiple parallel expanded particles with parallel bonds
    // All contract in random directions
    private void Activate11()
    {
        // Release the unused bonds
        ReleaseBond(Direction.SSE, false);
        ReleaseBond(Direction.NNW, true);

        // Contract in random direction
        if (Random.Range(0, 2) == 0)
        {
            ContractHead();
            SetMainColor(ColorData.Particle_Green);
        }
        else
        {
            ContractTail();
            SetMainColor(ColorData.Particle_Blue);
        }
        terminated.SetValue(true);
    }

    // Multiple parallel expanded particles with parallel bonds and some with neighbors
    // All either contract/perform handover with unmarked bonds or do not contract/
    // perform handover with marked bonds
    private void Activate12()
    {
        Debug.Log("Role: " + role.GetValue());
        // Roles 0-5 are expanded
        if (role < 6)
        {
            // Release diagonal bonds
            ReleaseBond(Direction.NNW, true);
            ReleaseBond(Direction.SSE, false);

            // Also release bonds to top and bottom contracted neighbors
            ReleaseBond(Direction.NNW, false);
            ReleaseBond(Direction.SSE, true);

            // 0 = do nothing (no neighbor), 1 = contract in random direction (no neighbor),
            // 2 = handover head with marked bond, 3 = handover tail with marked bond,
            // 4 = handover head with unmarked bond, 5 = handover tail with unmarked bond
            if (role == 1)
            {
                // Contract in random direction
                if (Random.Range(0, 2) == 0)
                {
                    ContractHead();
                    SetMainColor(ColorData.Particle_Green);
                }
                else
                {
                    ContractTail();
                    SetMainColor(ColorData.Particle_Blue);
                }
            }
            else if (role == 2 || role == 4)
            {
                // Pull into head and maybe mark bonds
                PullHandoverHead(Direction.W);
                SetMainColor(ColorData.Particle_Green);
                if (role == 2)
                {
                    // Mark tail bonds
                    MarkBond(Direction.NNE, false);
                    MarkBond(Direction.SSW, false);
                }
            }
            else if (role == 3 || role == 5)
            {
                // Pull into tail and maybe mark bonds
                PullHandoverTail(Direction.E);
                SetMainColor(ColorData.Particle_Blue);
                if (role == 3)
                {
                    // Mark head bonds
                    MarkBond(Direction.NNE, true);
                    MarkBond(Direction.SSW, true);
                }
            }
        }
        else if (role == 6)
        {
            // Left contracted neighbor: Perform handover to the right
            PushHandover(Direction.E);
            // Must release one bond to make movement valid
            // (cannot mark because that neighbor may not want to contract)
            ReleaseBond(Direction.SSE);
        }
        else if (role == 7)
        {
            // Right contracted neighbor: Perform handover to the left
            PushHandover(Direction.W);
            // Must release one bond to make movement valid
            ReleaseBond(Direction.NNW);
        }

        terminated.SetValue(true);
    }

    // Parallelogram of particles with one open side
    private void Activate13()
    {
        // Particles 0 and 2 are lower and upper row, particles 1 are vertical column
        if (IsContracted())
        {
            // Everybody expands
            if (role == 0 || role == 2)
            {
                // Release bond to NNW because of the tight corner
                ReleaseBond(Direction.NNW);
                Expand(Direction.E);
            }
            else
            {
                // Release bond to SSE because of the tight corner
                ReleaseBond(Direction.SSE);
                Expand(Direction.NNE);
            }
        }
        else
        {
            // Everybody contracts
            if (role == 0)
            {
                ReleaseBond(Direction.NNW, true);
            }
            else if (role == 1)
            {
                ReleaseBond(Direction.SSE, false);
            }
            ContractTail();
        }
    }
}
