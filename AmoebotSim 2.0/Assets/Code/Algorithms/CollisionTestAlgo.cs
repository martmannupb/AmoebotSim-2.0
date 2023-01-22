using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.CollisionTestAlgo
{

    public class CollisionTestAlgoParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Collision TEST";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(CollisionTestAlgoInitializer).FullName;

        // Declare attributes here
        public ParticleAttribute<int> algo;
        public ParticleAttribute<int> mode;
        public ParticleAttribute<bool> finished;

        public CollisionTestAlgoParticle(Particle p) : base(p)
        {
            algo = CreateAttributeInt("Algo", 0);
            mode = CreateAttributeInt("Mode", 0);
            finished = CreateAttributeBool("Finished", false);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(int algo = 0, int mode = 0)
        {
            // algo determines which example algorithm to run, mode determines which
            // role we play in this algorithm
            this.algo.SetValue(algo);
            this.mode.SetValue(mode);

            // Initialize algo
            if (algo == 0)
            {
                // Set colors
                if (mode == 1)
                    SetMainColor(ColorData.Particle_Blue);
                else if (mode == 2)
                    SetMainColor(ColorData.Particle_Green);
            }
        }

        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return finished;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            if (finished)
                return;

            if (algo == 0)
            {
                // Mode 1 particles expand to SSW
                if (mode == 1)
                {
                    // Setup bonds accordingly
                    foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                    {
                        // Bond to NNE always stays
                        if (d == Direction.NNE)
                            continue;
                        // Bond to SSE is always removed
                        if (d == Direction.SSE)
                        {
                            ReleaseBond(d);
                            continue;
                        }
                        if (HasNeighborAt(d))
                        {
                            CollisionTestAlgoParticle nbr = (CollisionTestAlgoParticle)GetNeighborAt(d);
                            // NNW: Release bond to mode 1 neighbor, keep bond to mode 0
                            if (d == Direction.NNW)
                            {
                                if (nbr.mode == 1)
                                    ReleaseBond(d);
                            }
                            // W and E: Release bond to anything other than mode 1 neighbors, mark these
                            else if (d == Direction.W || d == Direction.E)
                            {
                                if (nbr.mode == 1)
                                    MarkBond(d);
                                else
                                    ReleaseBond(d);
                            }
                            // SSW: Release bond if it does not belong to mode 1 particle
                            else if (d == Direction.SSW)
                            {
                                if (nbr.mode != 1)
                                    ReleaseBond(d);
                            }
                        }
                    }
                    Expand(Direction.SSW);
                }
                // Mode 2 particles contract along West-East axis
                else if (mode == 2)
                {
                    // Setup bonds
                    // SSE tail bond or NNW head bond to other contracting particle must be released
                    if (HasNeighborAt(Direction.SSE, false) && ((CollisionTestAlgoParticle)GetNeighborAt(Direction.SSE, false)).mode == 2)
                    {
                        ReleaseBond(Direction.SSE, false);
                    }
                    else if (HasNeighborAt(Direction.NNW, true) && ((CollisionTestAlgoParticle)GetNeighborAt(Direction.NNW, true)).mode == 2)
                    {
                        ReleaseBond(Direction.NNW, true);
                    }
                    // Release all NX bonds to mode 1 neighbors
                    foreach (Direction d in new Direction[] { Direction.NNW, Direction.NNE })
                    {
                        foreach (bool head in new bool[] { true, false })
                        {
                            if (HasNeighborAt(d, head) && ((CollisionTestAlgoParticle)GetNeighborAt(d, head)).mode == 1)
                                ReleaseBond(d, head);
                        }
                    }
                    ContractTail();
                }
                // Mode 0 particles have to agree to the released bonds
                else if (mode == 0)
                {
                    // Simply release all bonds to mode 1 particles that are not in SX directions
                    foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 4))
                    {
                        if (HasNeighborAt(d) && ((CollisionTestAlgoParticle)GetNeighborAt(d)).mode == 1)
                            ReleaseBond(d);
                    }
                }

                finished.SetValue(true);
            }
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (finished)
                return;
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class CollisionTestAlgoInitializer : InitializationMethod
    {
        public CollisionTestAlgoInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int heightLeft = 8, int heightRight = 4, int widthTop = 8, int numExpanding = 2, int numContracting = 4)
        {
            // Place particles like in model description example:
            // Parallelogram of thickness 2 where the bottom line contracts and the
            // upper half of the right line expands down

            // Height of the left support line (must be at least 4)
            int hl = heightLeft;
            // Height of the right support line (the part that collides into the expanding particles)
            int hr = heightRight;
            // Number of expanding particles making up the top part of the right line
            int he = numExpanding;
            // Width of the upper support line (must be at least 2)
            int wu = widthTop;
            // Number of expanded particles making up the bottom line (must be at least 1)
            int wl = numContracting;

            // Place scaffold particles first (inactive)
            // Left line
            for (int i = 0; i < hl; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    InitializationParticle ip = AddParticle(new Vector2Int(j, i));
                    ip.SetAttributes(new object[] { 0, 0 });
                }
            }
            // Top line
            for (int i = 0; i < wu; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    InitializationParticle ip = AddParticle(new Vector2Int(i + 2, hl - 1 - j));
                    ip.SetAttributes(new object[] { 0, 0 });
                }
            }
            // Right support line
            for (int i = 2 + 2 * wl; i < 4 + 2 * wl; i++)
            {
                for (int j = 0; j < hr; j++)
                {
                    InitializationParticle ip = AddParticle(new Vector2Int(i, j));
                    ip.SetAttributes(new object[] { 0, 0 });
                }
            }
            // Expanding particles
            for (int i = wu; i < wu + 2; i++)
            {
                for (int j = hl - 3; j > hl - 3 - he; j--)
                {
                    InitializationParticle ip = AddParticle(new Vector2Int(i, j));
                    ip.SetAttributes(new object[] { 0, 1 });
                }
            }
            // Contracting particles
            for (int i = 0; i < wl; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    InitializationParticle ip = AddParticle(new Vector2Int(2 + 2 * i, j), Direction.E);
                    ip.SetAttributes(new object[] { 0, 2 });
                }
            }
        }
    }

} // namespace AS2.Algos.CollisionTestAlgo
