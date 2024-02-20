using AS2.Sim;
using AS2.UI;
using AS2.ShapeContainment;
using UnityEngine;
using static AS2.Constants;

using AS2.Subroutines.SnowflakeContainment;

namespace AS2.Algos.SnowflakeTest
{

    /// <summary>
    /// Simple algorithm for testing the snowflake containment check.
    /// </summary>
    public class SnowflakeTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Snowflake Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SnowflakeTestInitializer).FullName;

        [StatusInfo("Display Shape", "Displays the target shape")]
        public static void ShowShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            LineDrawer.Instance.Clear();
            snowflake.Draw(selectedParticle is null ? Vector2Int.zero : selectedParticle.Head(), 0, scaleFactor);
            LineDrawer.Instance.SetTimer(20);
        }

        // Declare attributes here
        ParticleAttribute<int> round;
        ParticleAttribute<bool> onCounter;
        ParticleAttribute<bool> scaleBit;
        ParticleAttribute<bool> scaleMSB;

        public static Shape snowflake;
        public static int scaleFactor;
        public static SnowflakeInfo snowflakeInfo;

        SubSnowflakeContainment snowflakeCheck;

        public SnowflakeTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            onCounter = CreateAttributeBool("On Counter", false);
            scaleBit = CreateAttributeBool("Scale Bit", false);
            scaleMSB = CreateAttributeBool("Scale MSB", false);

            snowflakeCheck = new SubSnowflakeContainment(p, snowflakeInfo);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool onCounter = false, bool scaleBit = false, bool scaleMSB = false)
        {
            this.onCounter.SetValue(onCounter);
            this.scaleBit.SetValue(scaleBit);
            this.scaleMSB.SetValue(scaleMSB);
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
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            // Implement the communication code here
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SnowflakeTestInitializer : InitializationMethod
    {
        public SnowflakeTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        private int nPlaced;

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(string shape = "snowflake.json", bool fromFile = true, int scale = 1, int numAmoebots = 250, bool fillShape = true, float holeProb = 0.3f, bool fillHoles = false)
        {
            nPlaced = 0;

            // Read the shape
            Shape s;
            ShapeContainer sc;
            if (fromFile)
            {
                sc = ShapeContainer.ReadFromJson(FilePaths.path_shapes + shape);
            }
            else
            {
                sc = JsonUtility.FromJson<ShapeContainer>(shape);
            }
            if (sc is null || sc.shape is null)
            {
                Log.Error("Failed to read shape");
                return;
            }
            s = sc.shape;
            if (!s.IsConsistent())
            {
                Log.Warning("Shape is inconsistent!");
            }
            else
            {
                s.GenerateTraversal();
                SnowflakeTestParticle.snowflake = s;
            }

            // Compute snowflake data
            // TODO
            //  - Find all arm lengths and sort them in ascending order
            //  - Replace actual arm lengths with indices for this list
            //  - Compute string representations of all line lengths
            //  - Find the largest parameter (longest binary encoding)

            if (scale < 1)
            {
                Log.Error("Scale must be >= 1");
                scale = 1;
            }
            SnowflakeTestParticle.scaleFactor = scale;

            // Place amoebot system
            foreach (Vector2Int v in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles))
            {
                AddParticle(v);
                nPlaced++;
            }

            // Fill up positions of the shape
            if (fillShape)
            {
                if (scale == 1)
                {
                    foreach (Shape.Node node in s.nodes)
                    {
                        TryPlaceParticle(node);
                    }
                }
                else
                {
                    // Fill edges
                    foreach (Shape.Edge edge in s.edges)
                    {
                        Vector2Int n1 = s.nodes[edge.u];
                        Vector2Int n2 = s.nodes[edge.v];
                        Vector2Int to = n2 - n1;
                        n1 *= scale;
                        for (int i = 0; i < scale + 1; i++)
                        {
                            TryPlaceParticle(n1 + i * to);
                        }
                    }

                    // Fill faces
                    foreach (Shape.Face face in s.faces)
                    {
                        Vector2Int n1 = s.nodes[face.u];
                        Vector2Int n2 = s.nodes[face.v];
                        Vector2Int n3 = s.nodes[face.w];
                        Vector2Int to1 = n2 - n1;
                        Vector2Int to2 = n3 - n1;
                        n1 *= scale;
                        for (int i = 1; i < scale - 1; i++)
                        {
                            Vector2Int start = n1 + i * to1;
                            for (int j = 1; j < scale - i; j++)
                            {
                                TryPlaceParticle(start + j * to2);
                            }
                        }
                    }
                }
            }

            // Place the counter
            string scaleStr = IntToBinary(scale);
            for (int x = 0; x < scaleStr.Length; x++)
            {
                InitializationParticle p;
                Vector2Int pos = new Vector2Int(x, 0);
                if (!TryGetParticleAt(pos, out p))
                {
                    p = AddParticle(pos);
                    nPlaced++;
                }
                p.SetAttribute("onCounter", true);
                p.SetAttribute("scaleBit", scaleStr[x] == '1');
                p.SetAttribute("scaleMSB", x == scaleStr.Length - 1);
            }

            Log.Debug("Generated system has " + nPlaced + " amoebots");

            // Draw shape preview
            LineDrawer.Instance.Clear();
            s.Draw(Vector2Int.zero, 0, scale);
            LineDrawer.Instance.SetTimer(20);
        }

        private void TryPlaceParticle(Vector2Int pos)
        {
            if (!TryGetParticleAt(pos, out _))
            {
                AddParticle(pos);
                nPlaced++;
            }
        }

        private string IntToBinary(int num)
        {
            string s = "";

            while (num > 0)
            {
                s += (num & 1) > 0 ? '1' : '0';
                num >>= 1;
            }

            return s;
        }
    }

} // namespace AS2.Algos.SnowflakeTest
