using AS2.Sim;
using AS2.UI;
using UnityEngine;

namespace AS2.Algos.CollisionTestAlgo2
{

    public enum Role
    {
        Dummy1,
        Dummy2,
        Static,
        Expanding,
        Contracting,
        HandoverHelper
    }

    public class CollisionTestAlgo2Particle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Collision TEST 2";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 0;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(CollisionTestAlgo2Initializer).FullName;

        // Declare attributes here
        public ParticleAttribute<Role> role;
        public ParticleAttribute<Direction> expansionDir;
        public ParticleAttribute<bool> handover;
        public ParticleAttribute<bool> finished;

        public CollisionTestAlgo2Particle(Particle p) : base(p)
        {
            // Initialize the attributes here
            role = CreateAttributeEnum<Role>("Role", Role.Static);
            expansionDir = CreateAttributeDirection("Expansion Dir", Direction.NONE);
            handover = CreateAttributeBool("Handover", false);
            finished = CreateAttributeBool("Finished", false);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(Role role = Role.Static, Direction expansionDir = Direction.NONE,
            bool handover = false)
        {
            this.role.SetValue(role);
            this.expansionDir.SetValue(expansionDir);
            this.handover.SetValue(handover);

            if (role == Role.Dummy1)
                SetMainColor(ColorData.Particle_Green);
            else if (role == Role.Dummy2)
                SetMainColor(ColorData.Particle_Blue);
            else if (role == Role.Expanding)
                SetMainColor(ColorData.Particle_Aqua);
            else if (role == Role.Contracting)
                SetMainColor(ColorData.Particle_Orange);
            else if (role == Role.HandoverHelper)
                SetMainColor(ColorData.Particle_Yellow);
        }

        // Implement this method if the algorithm terminates at some point
        //public override bool IsFinished()
        //{
        //    // Return true when this particle has terminated
        //    return false;
        //}

        // The movement activation method
        public override void ActivateMove()
        {
            if (finished)
                return;
            
            if ((role == Role.Dummy1 || role == Role.Dummy2) && expansionDir != Direction.NONE)
            {
                if (IsExpanded())
                {
                    // Release all head bonds and contract into the tail
                    for (int i = 0; i < 6; i++)
                        ReleaseBond(DirectionHelpers.Cardinal(i), true);
                    ContractTail();
                }
                else if (handover)
                {
                    PushHandover(expansionDir);
                }
                else
                {
                    Expand(expansionDir);
                }
            }
            else if (role == Role.Expanding)
            {
                // Necessary for joint between top horizontal line and vertical line
                MarkBond(Direction.SSE);
                Expand(expansionDir);
            }
            else if (role == Role.Contracting)
            {
                ContractTail();
            }
            else if (role == Role.HandoverHelper)
            {
                PullHandoverTail(expansionDir);
            }

            finished.SetValue(true);
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            // Implement the communication code here
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class CollisionTestAlgo2Initializer : InitializationMethod
    {
        public CollisionTestAlgo2Initializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int offsetX = 3, int offsetY = 3,
            Direction expansionDir1 = Direction.NNE, Direction expansionDir2 = Direction.SSW,
            bool expanding1 = true, bool expanding2 = true,
            int movementX = -4, int movementY = -2,
            bool handover1 = false, bool handover2 = false)
        {
            if (offsetY < 0)
            {
                Log.Error("Y offset must be >= 0.");
                return;
            }
            else if (movementY > 0)
            {
                Log.Error("Y movement must be <= 0.");
                return;
            }
            else if (expansionDir1.IsSecondary() || (!handover1 && expansionDir1 == Direction.SSW) || (handover1 && expansionDir1 == Direction.NNE))
            {
                Log.Error("Invalid expansion direction 1: Must be cardinal and not SSW expansion or NNE handover");
                return;
            }
            else if (expansionDir2.IsSecondary() || (!handover2 && expansionDir2 == Direction.NNE) || (handover2 && expansionDir2 == Direction.SSW))
            {
                Log.Error("Invalid expansion direction 2: Must be cardinal and not NNE expansion or SSW handover");
                return;
            }

            // Place the first dummy particle
            InitializationParticle p1;
            if (handover1)
                p1 = AddParticle(new Vector2Int(0, -1) + ParticleSystem_Utils.DirectionToVector(expansionDir1.Opposite()));
            else
                p1 = AddParticle(Vector2Int.zero, expanding1 ? Direction.NONE : expansionDir1);
            p1.SetAttribute("role", Role.Dummy1);
            p1.SetAttribute("expansionDir", expansionDir1);
            p1.SetAttribute("handover", handover1);

            // Place the second dummy particle and its support particles
            InitializationParticle p2;
            if (handover2)
            {
                p2 = AddParticle(new Vector2Int(offsetX, offsetY + 1) + ParticleSystem_Utils.DirectionToVector(expansionDir2.Opposite()));
                InitializationParticle p = AddParticle(new Vector2Int(offsetX, offsetY + 2), Direction.SSW);
                p.SetAttribute("role", Role.HandoverHelper);
                p.SetAttribute("expansionDir", expansionDir2.Opposite());
            }
            else
            {
                p2 = AddParticle(new Vector2Int(offsetX, offsetY), expanding2 ? Direction.NONE : expansionDir2);
                AddParticle(new Vector2Int(offsetX, offsetY + 1));
                AddParticle(new Vector2Int(offsetX, offsetY + 2));
            }
            p2.SetAttribute("role", Role.Dummy2);
            p2.SetAttribute("expansionDir", expansionDir2);
            p2.SetAttribute("handover", handover2);

            // Build support and movement structure
            int xMin = Mathf.Min(Mathf.Min(p1.Head().x, p1.Tail().x) - 3, Mathf.Min(p2.Head().x, p2.Tail().x) - 2);
            int yMin = Mathf.Min(-3, offsetX + movementX < 0 ? offsetY + movementY - 2 : 0);
            int yMax = Mathf.Max(p2.Head().y, p2.Tail().y) + 3 - movementY;

            // Add expanding particles moving the second particle down
            for (int y = offsetY + 3; y < yMax; y++)
            {
                InitializationParticle p = AddParticle(new Vector2Int(offsetX, y));
                p.SetAttribute("role", Role.Expanding);
                p.SetAttribute("expansionDir", Direction.SSW);
            }

            // Add expanding or contracting particles to move the second particle right or left
            int numXSupports = offsetX - xMin;      // Initial number of particles in top support line
            if (movementX > 0)
            {
                // Make whole structure wider if more expanding particles are required
                if (numXSupports < movementX)
                {
                    xMin -= (movementX - numXSupports);
                    numXSupports = movementX;
                }

                // Replace sufficiently many particles of the upper support line with expanding particles
                numXSupports -= movementX;
                for (int x = xMin + numXSupports; x < offsetX; x++)
                {
                    InitializationParticle p = AddParticle(new Vector2Int(x, yMax));
                    p.SetAttribute("role", Role.Expanding);
                    p.SetAttribute("expansionDir", Direction.E);
                }
            }
            else if (movementX < 0)
            {
                // Make whole structure wider if more contracting particles are required
                if ((numXSupports / 2) < -movementX)
                {
                    xMin -= (-movementX - (numXSupports / 2)) * 2 - (numXSupports % 2 == 1 ? 1 : 0);
                    numXSupports = offsetX - xMin;
                }

                // Replace sufficiently many particles of the upper support line with contracting particles
                numXSupports -= (-movementX) * 2;
                for (int x = xMin + numXSupports; x < offsetX; x += 2)
                {
                    InitializationParticle p = AddParticle(new Vector2Int(x, yMax), Direction.E);
                    p.SetAttribute("role", Role.Contracting);
                    p.SetAttribute("expansionDir", Direction.E);
                }
            }

            // Rest of horizontal support line for dummy 2
            for (int x = xMin; x < xMin + numXSupports; x++)
                AddParticle(new Vector2Int(x, yMax));

            // Place dummy 1 support particles
            for (int y = yMin; y < (handover1 ? -2 : 0); y++)
                AddParticle(new Vector2Int(0, y));

            // Handover: Replace last two support particles with handover helper
            if (handover1)
            {
                InitializationParticle p = AddParticle(new Vector2Int(0, -2), Direction.NNE);
                p.SetAttribute("role", Role.HandoverHelper);
                p.SetAttribute("expansionDir", expansionDir1.Opposite());
            }

            // Horizontal support line for dummy 1
            for (int x = xMin; x < 0; x++)
                AddParticle(new Vector2Int(x, yMin));
            // Vertical support line
            for (int y = yMin + 1; y < yMax; y++)
                AddParticle(new Vector2Int(xMin, y));

            // Visualization of the movement
            float displayTime = 5f;
            LineDrawer.Instance.Clear();
            if (expansionDir1 != Direction.NONE)
            {
                if (handover1)
                {
                    Vector2Int pull1 = new Vector2Int(0, -2);
                    Vector2Int pull2 = new Vector2Int(0, -1);
                    LineDrawer.Instance.AddLine(pull1, pull2, Color.green);
                    LineDrawer.Instance.AddLine(pull2, p1.Head(), Color.green);
                }
                else
                {
                    LineDrawer.Instance.AddLine(Vector2Int.zero, ParticleSystem_Utils.DirectionToVector(expansionDir1), Color.green);
                }
            }

            if (handover2)
            {
                Vector2Int pos1 = new Vector2Int(offsetX, offsetY + 2);
                Vector2Int pos2 = new Vector2Int(offsetX, offsetY + 1);
                Vector2Int pos3 = new Vector2Int(offsetX, offsetY + 1) + ParticleSystem_Utils.DirectionToVector(expansionDir2.Opposite());
                Vector2Int offset = new Vector2Int(movementX, movementY);

                LineDrawer.Instance.AddLine(pos1, pos1 + offset, Color.blue, true);
                LineDrawer.Instance.AddLine(pos2, pos2 + offset, Color.blue, true);
                LineDrawer.Instance.AddLine(pos3, pos3 + offset, Color.blue, true);
            }
            else
            {
                Vector2Int part2Start1 = new Vector2Int(offsetX, offsetY);
                Vector2Int part2End1 = (!expanding2) && expansionDir2 != Direction.NONE ?
                    new Vector2Int(offsetX, offsetY) + ParticleSystem_Utils.DirectionToVector(expansionDir2)
                    : part2Start1;
                Vector2Int part2Start2 = new Vector2Int(offsetX + movementX, offsetY + movementY);
                Vector2Int part2End2 = expanding2 && expansionDir2 != Direction.NONE ?
                    new Vector2Int(offsetX + movementX, offsetY + movementY) + ParticleSystem_Utils.DirectionToVector(expansionDir2)
                    : part2Start2;

                LineDrawer.Instance.AddLine(part2Start1, part2Start2, Color.blue, true);
                LineDrawer.Instance.AddLine(part2End1, part2End2, Color.blue, true);
            }

            LineDrawer.Instance.SetTimer(displayTime);
        }
    }

} // namespace AS2.Algos.CollisionTestAlgo2
