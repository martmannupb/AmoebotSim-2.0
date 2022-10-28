using UnityEngine;

public class JMTestParticle : ParticleAlgorithm
{
    public override int PinsPerEdge => 1;

    public static new string Name => "Joint Movement Test";

    public static new string GenerationMethod => JMTestInitializer.Name;

    private ParticleAttribute<int> mode;
    private ParticleAttribute<int> role;
    private ParticleAttribute<bool> terminated;

    public JMTestParticle(Particle p, int[] genericParams) : base(p)
    {
        //if (genericParams.Length < 2)
        //{
        //    Log.Error("JM Test particles requires 2 generic parameters.");
        //    return;
        //}
        //int m = genericParams[0];
        //int r = genericParams[1];

        //if (m == 6)
        //{
        //    if (r == 0)
        //    {
        //        SetMainColor(ColorData.Particle_Blue);
        //    }
        //    else if (r == 1)
        //    {
        //        SetMainColor(ColorData.Particle_Green);
        //    }
        //    else
        //    {
        //        SetMainColor(ColorData.Particle_Orange);
        //    }
        //}
        //else if (m == 15)
        //{
        //    if (r == 1)
        //    {
        //        SetMainColor(ColorData.Particle_Green);
        //    }
        //    else
        //    {
        //        SetMainColor(ColorData.Particle_Black);
        //    }
        //}
        //else
        //{
        //    SetMainColor(ColorData.Particle_Black);
        //}

        mode = CreateAttributeInt("Mode", 0);
        role = CreateAttributeInt("Role", 0);
        terminated = CreateAttributeBool("Terminated", false);
    }

    public void Init(int mode, int role)
    {
        int m = mode;
        int r = role;
        if (m == 6)
        {
            if (r == 0)
            {
                SetMainColor(ColorData.Particle_Blue);
            }
            else if (r == 1)
            {
                SetMainColor(ColorData.Particle_Green);
            }
            else
            {
                SetMainColor(ColorData.Particle_Orange);
            }
        }
        else if (m == 15)
        {
            if (r == 1)
            {
                SetMainColor(ColorData.Particle_Green);
            }
            else
            {
                SetMainColor(ColorData.Particle_Black);
            }
        }
        else
        {
            SetMainColor(ColorData.Particle_Black);
        }
        this.mode.SetValue(m);
        this.role.SetValue(r);
    }

    public JMTestParticle(Particle p) : base(p)
    {
        this.mode = CreateAttributeInt("Mode", -1);
        this.role = CreateAttributeInt("Role", -1);
        terminated = CreateAttributeBool("Terminated", false);
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
            case 14: Activate14();
                break;
            case 15: Activate15();
                break;
            case 16: Activate16();
                break;
            case 17: Activate17();
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
    // IMPORTANT: CASE WITH CONTRACTIONS/UNMARKED BOND HANDOVERS DOES NOT OCCUR ANYMORE
    private void Activate12()
    {
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

    // Contracted and expanded particle with two bonds,
    // expanded particle performs handover
    // Tests a special case that was allowed later
    private void Activate14()
    {
        // 0 is contracted, 1 is expanded, 2 is contracted for handover
        if (role == 0)
        {
            // Expand randomly
            if (Random.Range(0, 2) == 0)
            {
                Direction d = DirectionHelpers.Cardinal(Random.Range(0, 6));
                // Expanding up: Both bonds must be marked
                if (d == Direction.NNE || d == Direction.NNW)
                {
                    MarkBond(Direction.NNE);
                    MarkBond(Direction.NNW);
                    SetMainColor(ColorData.Particle_Green);
                }
                // Expanding left or right: Both bonds may be marked
                else if (d == Direction.W || d == Direction.E)
                {
                    if (Random.Range(0, 2) == 0)
                    {
                        MarkBond(Direction.NNE);
                        MarkBond(Direction.NNW);
                        SetMainColor(ColorData.Particle_Green);
                    }
                    else
                    {
                        SetMainColor(ColorData.Particle_Blue);
                    }
                }
                else
                {
                    SetMainColor(ColorData.Particle_Blue);
                }
                Expand(d);
            }
            else
            {
                SetMainColor(ColorData.Particle_Red);
            }
        }
        else if (role == 1)
        {
            // Perform handover with contracted neighbor, mark the left bond
            MarkBond(Direction.SSE, false);
            PullHandoverHead(Direction.NNW);
        }
        else if (role == 2)
        {
            // Perform handover with expanded neighbor
            PushHandover(Direction.SSE);
        }
        terminated.SetValue(true);
    }

    // Block of particles with a stripe of expanding and contracting particles
    private void Activate15()
    {
        // Role 0 is idle, role 1 moves
        if (role == 1)
        {
            if (IsContracted())
            {
                Expand(Direction.E);
                MarkBond(Direction.SSE);
            }
            else
            {
                ReleaseBond(Direction.NNW, true);
                ReleaseBond(Direction.SSE, false);
                ContractHead();
            }
        }
    }

    // "Floor" and "worm" of particles
    private void Activate16()
    {
        // Role 0 is floor, role 1 is worm
        if (role == 0)
        {
            // Floor disconnects all bonds unless it is the leftmost or rightmost particle of the worm
            bool hasNbrLeft = HasNeighborAt(Direction.NNW);
            bool hasNbrRight = HasNeighborAt(Direction.NNE);

            if (hasNbrLeft && hasNbrRight)
            {
                ReleaseBond(Direction.NNW);
                ReleaseBond(Direction.NNE);
            }
            else if (hasNbrLeft)
            {
                ReleaseBond(Direction.NNE);
                ParticleAlgorithm nbr = GetNeighborAt(Direction.NNW);
                if (nbr.IsContracted())
                {
                    ReleaseBond(Direction.NNW);
                }
            }
            else if (hasNbrRight)
            {
                ReleaseBond(Direction.NNW);
                ParticleAlgorithm nbr = GetNeighborAt(Direction.NNE);
                if (nbr.IsExpanded())
                {
                    ReleaseBond(Direction.NNE);
                }
            }
        }
        else
        {
            if (IsContracted())
            {
                // Only the leftmost particle keeps its bonds to the floor
                ReleaseBond(Direction.SSE);
                if (HasNeighborAt(Direction.W))
                {
                    ReleaseBond(Direction.SSW);
                }

                // Expand right
                Expand(Direction.E);
            }
            else
            {
                // Only the rightmost particle keeps its head bonded to the floor
                ReleaseBond(Direction.SSW, false);
                ReleaseBond(Direction.SSE, false);
                ReleaseBond(Direction.SSW, true);
                if (HasNeighborAt(Direction.E, true))
                {
                    ReleaseBond(Direction.SSE, true);
                }

                // Contract right
                ContractHead();
            }
        }
    }

    // Parallelogram where the bottom line consists of
    // expanding and contracting particles
    private void Activate17()
    {
        // Role 0 is static, role 1 is dynamic (bottom line)
        if (role == 0)
        {
            // Release bonds to simplify movements
            ReleaseBond(Direction.SSE);
            ReleaseBond(Direction.NNW);
        }
        else
        {
            if (IsContracted())
            {
                ReleaseBond(Direction.NNW);
                // Expand randomly to left or right
                if (Random.Range(0, 2) == 0)
                {
                    Expand(Direction.W);
                }
                else
                {
                    Expand(Direction.E);
                }
            }
            else
            {
                ReleaseBond(Direction.NNW, false);
                ReleaseBond(Direction.NNW, true);
                // Contract randomly to head or tail
                if (Random.Range(0, 2) == 0)
                {
                    ContractHead();
                }
                else
                {
                    ContractTail();
                }
            }
        }
    }
}


// Initialization method
public class JMTestInitializer : InitializationMethod
{
    public JMTestInitializer(ParticleSystem system) : base(system) { }

    public static new string Name => "JM Test";

    public void Generate(int mode)
    {
        while (NumGenericParameters() < 2)
            AddGenericParameter();

        InitializationParticle p;
        if (mode == 0)
        {
            // A block of particles that expands East while being sheared
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    p = AddParticle(new Vector2Int(x, y));
                    p.SetAttributes(new object[] { mode, 0 });
                }
            }
        }
        else if (mode == 1)
        {
            // A block of particles that expands East without being sheared
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    p = AddParticle(new Vector2Int(x, y));
                    p.SetAttributes(new object[] { mode, 0 });
                }
            }
        }
        else if (mode == 2)
        {
            // A contracted and an expanded particle (will have two bonds)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = AddParticle(new Vector2Int(0, 0));
            p.SetAttributes(new object[] { mode, 0 });

            // Expanded
            p = AddParticle(new Vector2Int(-1, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });
        }
        else if (mode == 3)
        {
            // A contracted and an expanded particle (will have only one bond)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = AddParticle(new Vector2Int(0, 0));
            p.SetAttributes(new object[] { mode, 0 });

            // Expanded
            p = AddParticle(new Vector2Int(-1, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });
        }
        else if (mode == 4)
        {
            // A contracted and an expanded particle (will have only one bond and perform handover)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = AddParticle(new Vector2Int(0, 0));
            p.SetAttributes(new object[] { mode, 0 });

            // Expanded
            p = AddParticle(new Vector2Int(-1, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });
        }
        else if (mode == 5)
        {
            // A contracted and an expanded particle (will have two bonds and perform handover)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = AddParticle(new Vector2Int(0, 0));
            p.SetAttributes(new object[] { mode, 0 });

            // Expanded
            p = AddParticle(new Vector2Int(-1, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });
        }
        else if (mode == 6)
        {
            // A contracted and an expanded particle performing a handover with additional particles
            // being pulled or transferred

            // Contracted particle pushing
            p = AddParticle(new Vector2Int(0, 0));
            p.SetAttributes(new object[] { mode, 0 });

            // Expanded particle pulling
            p = AddParticle(new Vector2Int(-1, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });

            // Four particles that are passive and will be moved around by the other particles
            int role = 2;
            foreach (Vector2Int pos in new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(-2, 2), new Vector2Int(-1, 2) })
            {
                p = AddParticle(pos);
                p.SetAttributes(new object[] { mode, role });
                role++;
            }
        }
        else if (mode == 7)
        {
            // Two expanded particles with one bond, randomly moving or not

            // Bottom particle
            p = AddParticle(new Vector2Int(0, 0), Direction.E);
            p.SetAttributes(new object[] { mode, 0 });

            // Top particle
            p = AddParticle(new Vector2Int(0, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });
        }
        else if (mode == 8)
        {
            // Two expanded particles with one bond and contracted neighbors, randomly decide to mark bond or not

            // Bottom particle
            p = AddParticle(new Vector2Int(0, 0), Direction.E);
            p.SetAttributes(new object[] { mode, 0 });

            // Top particle
            p = AddParticle(new Vector2Int(0, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });

            // Contracted neighbors
            p = AddParticle(new Vector2Int(0, -1));
            p.SetAttributes(new object[] { mode, 2 });

            p = AddParticle(new Vector2Int(-1, 2));
            p.SetAttributes(new object[] { mode, 3 });
        }
        else if (mode == 9)
        {
            // Two expanded particles with two bonds sharing one end
            // Swap the particles to change the anchor

            // Left particle
            p = AddParticle(new Vector2Int(0, 0), Direction.E);
            p.SetAttributes(new object[] { mode, 0 });

            // Right particle
            p = AddParticle(new Vector2Int(2, 0), Direction.NNW);
            p.SetAttributes(new object[] { mode, 1 });
        }
        else if (mode == 10)
        {
            // Same as mode 9 but with a contracted particle for handover
            // Swap the particles to change the anchor

            // Left particle
            p = AddParticle(new Vector2Int(0, 0), Direction.E);
            p.SetAttributes(new object[] { mode, 0 });

            // Right particle
            p = AddParticle(new Vector2Int(2, 0), Direction.NNW);
            p.SetAttributes(new object[] { mode, 1 });

            // Contracted particle
            p = AddParticle(new Vector2Int(1, 2));
            p.SetAttributes(new object[] { mode, 2 });
        }
        else if (mode == 11)
        {
            // Stack of parallel expanded particles with two parallel bonds
            // All of them contract but the direction is random

            for (int i = 0; i < 10; i++)
            {
                p = AddParticle(new Vector2Int(0, i), Direction.E);
                p.SetAttributes(new object[] { mode, 0 });
            }
        }
        else if (mode == 12)
        {
            // Stack of parallel expanded particles with two parallel bonds
            // Some of them have a contracted neighbor for handover
            // The system either contracts or stays expanded
            // IMPORTANT: CASE WITH UNMARKED HANDOVERS DOES NOT WORK DUE TO
            // UPDATED MODEL, SYSTEM NEVER CONTRACTS

            //bool contract = Random.Range(0, 2) == 0;
            bool contract = false;

            for (int i = 0; i < 15; i++)
            {
                // Randomly either place just the expanded particle or an additional contracted particle on one side
                // 0 = none, 1 = left, 2 = right
                int neighbor = Random.Range(0, 3);

                // 0 = do nothing (no neighbor), 1 = contract in random direction (no neighbor),
                // 2 = handover head with marked bond, 3 = handover tail with marked bond,
                // 4 = handover head with unmarked bond, 5 = handover tail with unmarked bond
                int role = neighbor == 0 ? (contract ? 1 : 0) :
                    neighbor == 1 ? (contract ? 4 : 2) :
                    (contract ? 5 : 3);

                // Expanded particle
                p = AddParticle(new Vector2Int(0, i), Direction.E);
                p.SetAttributes(new object[] { mode, role });

                // Place the neighbor
                if (neighbor == 1)
                {
                    p = AddParticle(new Vector2Int(-1, i));
                    p.SetAttributes(new object[] { mode, 6 });
                }
                else if (neighbor == 2)
                {
                    p = AddParticle(new Vector2Int(2, i));
                    p.SetAttributes(new object[] { mode, 7 });
                }
            }
        }
        else if (mode == 13)
        {
            // Three lines of particles forming a parallelogram with one missing side
            // All of the particles expand and contract alternatingly
            int widthLower = 10;
            int widthUpper = 12;
            int height = 8;

            // Lower row
            for (int i = 0; i < widthLower; i++)
            {
                p = AddParticle(new Vector2Int(i, 0));
                p.SetAttributes(new object[] { mode, 0 });
            }

            // Vertical column
            for (int i = 0; i < height; i++)
            {
                p = AddParticle(new Vector2Int(0, i + 1));
                p.SetAttributes(new object[] { mode, 1 });
            }

            // Upper row
            for (int i = 0; i < widthUpper; i++)
            {
                p = AddParticle(new Vector2Int(i, height + 1));
                p.SetAttributes(new object[] { mode, 2 });
            }
        }
        else if (mode == 14)
        {
            // Contracted and expanded particle with 2 bonds
            // Expanded particle performs handover with another contracted neighbor

            // Contracted
            p = AddParticle(new Vector2Int(0, 0));
            p.SetAttributes(new object[] { mode, 0 });

            // Expanded
            p = AddParticle(new Vector2Int(-1, 1), Direction.E);
            p.SetAttributes(new object[] { mode, 1 });

            // Contracted neighbor for handover
            p = AddParticle(new Vector2Int(-2, 2));
            p.SetAttributes(new object[] { mode, 2 });
        }
        else if (mode == 15)
        {
            // Block of particles with a stripe in the middle that expands and contracts
            // to move the entire block

            int width = 20;
            int height = 10;
            // Idle particles have role 0, stripe particles have role 1
            // The lowest stripe particle is the anchor
            p = AddParticle(new Vector2Int(width / 2, 0));
            p.SetAttributes(new object[] { mode, 1 });

            for (int x = 0; x < width; x++)
            {
                int role = x == width / 2 ? 1 : 0;
                for (int y = (role == 0 ? 0 : 1); y < height; y++)
                {
                    p = AddParticle(new Vector2Int(x, y));
                    p.SetAttributes(new object[] { mode, role });
                }
            }
        }
        else if (mode == 16)
        {
            // A "floor" made out of contracted particles and a "worm" of particles that moves
            // across it by expanding and contracting
            int floorSize = 250;
            int wormSize = 7;

            // Role 0 is floor, role 1 is worm
            // Anchor is part of the floor
            for (int i = 0; i < floorSize; i++)
            {
                p = AddParticle(new Vector2Int(i, 0));
                p.SetAttributes(new object[] { mode, 0 });
            }

            for (int i = 0; i < wormSize; i++)
            {
                p = AddParticle(new Vector2Int(i, 1));
                p.SetAttributes(new object[] { mode, 1 });
            }
        }
        else if (mode == 17)
        {
            // A parallelogram of particles where the bottom line consists of expanded and contracted
            // particles which alternatingly expand and contract

            int numSegments = 5;
            int height = 4;     // Must not be less than 3

            // Role 0 means static, role 1 means moving
            // Left and right sides
            for (int i = 0; i < height; i++)
            {
                p = AddParticle(new Vector2Int(0, i));
                p.SetAttributes(new object[] { mode, 0 });

                p = AddParticle(new Vector2Int(3 * numSegments + 1, i));
                p.SetAttributes(new object[] { mode, 0 });
            }
            // Top line
            for (int i = 1; i < numSegments * 3 + 1; i++)
            {
                p = AddParticle(new Vector2Int(i, height - 1));
                p.SetAttributes(new object[] { mode, 0 });
            }

            // Bottom line
            int numExpanded = 0;
            int numContracted = 0;
            int x = 1;
            for (int i = 0; i < numSegments * 2; i++)
            {
                bool random = Random.Range(0, 2) == 0;
                bool expanded = numContracted == numSegments || (numExpanded < numSegments && random);
                if (expanded)
                {
                    p = AddParticle(new Vector2Int(x, 0), Direction.E);
                    p.SetAttributes(new object[] { mode, 1 });

                    numExpanded++;
                    x += 2;
                }
                else
                {
                    p = AddParticle(new Vector2Int(x, 0));
                    p.SetAttributes(new object[] { mode, 1 });

                    numContracted++;
                    x++;
                }
            }
        }
    }
}
