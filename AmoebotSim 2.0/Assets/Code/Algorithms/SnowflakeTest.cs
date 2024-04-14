using System.Collections.Generic;
using AS2.Sim;
using AS2.UI;
using AS2.ShapeContainment;
using UnityEngine;
using static AS2.Constants;

using AS2.Subroutines.SnowflakePlacementSearch;

namespace AS2.Algos.SnowflakeTest
{

    /// <summary>
    /// Simple algorithm for testing the snowflake placement search.
    /// <para>
    /// <b>Disclaimer: The save/load feature does not work for
    /// this algorithm because it stores the target shape in a
    /// static member. Always generate this algorithm from
    /// Init Mode.</b>
    /// </para>
    /// </summary>
    public class SnowflakeTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Snowflake Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SnowflakeTestInitializer).FullName;

        [StatusInfo("Display Shape", "Displays the target shape at the selecetd location")]
        public static void ShowShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            LineDrawer.Instance.Clear();
            int rotation = 0;
            if (!(selectedParticle is null))
            {
                SnowflakeTestParticle p = (SnowflakeTestParticle)selectedParticle.algorithm;
                if (p.IsFinished())
                {
                    for (int r = 0; r < 6; r++)
                    {
                        if (p.validPlacement[r])
                        {
                            rotation = r;
                            break;
                        }
                    }
                }
            }
            snowflake.Draw(selectedParticle is null ? Vector2Int.zero : selectedParticle.Head(), rotation, scaleFactor);
            LineDrawer.Instance.SetTimer(20);
        }

        // Declare attributes here
        ParticleAttribute<int> round;
        ParticleAttribute<bool> onCounter;
        ParticleAttribute<int> counterIndex;
        ParticleAttribute<bool> scaleBit;
        ParticleAttribute<bool> scaleMSB;
        ParticleAttribute<bool>[] validPlacement;

        public static Shape snowflake;
        public static int scaleFactor;
        public static SnowflakeInfo snowflakeInfo;

        SubSnowflakePlacementSearch snowflakeCheck;

        public SnowflakeTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            onCounter = CreateAttributeBool("On Counter", false);
            counterIndex = CreateAttributeInt("Counter Index", -1);
            scaleBit = CreateAttributeBool("Scale Bit", false);
            scaleMSB = CreateAttributeBool("Scale MSB", false);

            validPlacement = new ParticleAttribute<bool>[6];
            for (int i = 0; i < 6; i++)
            {
                validPlacement[i] = CreateAttributeBool("Valid Placement " + i, false);
            }

            snowflakeCheck = new SubSnowflakePlacementSearch(p, snowflakeInfo);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool onCounter = false, bool scaleBit = false, bool scaleMSB = false, int counterIndex = -1)
        {
            this.onCounter.SetValue(onCounter);
            this.scaleBit.SetValue(scaleBit);
            this.scaleMSB.SetValue(scaleMSB);
            this.counterIndex.SetValue(counterIndex);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return round > 1;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            int r = round.GetValue();
            switch (r)
            {
                case 0:
                    {
                        snowflakeCheck.Init(true, onCounter && counterIndex.GetValue() != -1, Mathf.Max(0, counterIndex),
                            onCounter && HasNeighborAt(Direction.W) && ((SnowflakeTestParticle)GetNeighborAt(Direction.W)).onCounter ? Direction.W : Direction.NONE,
                            onCounter && HasNeighborAt(Direction.E) && ((SnowflakeTestParticle)GetNeighborAt(Direction.E)).onCounter ? Direction.E : Direction.NONE, scaleBit, scaleMSB);
                        PinConfiguration pc = GetContractedPinConfiguration();
                        snowflakeCheck.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        snowflakeCheck.ActivateSend();
                        round.SetValue(r + 1);
                    }
                    break;
                case 1:
                    {
                        snowflakeCheck.ActivateReceive();

                        if (snowflakeCheck.IsFinished())
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                if (snowflakeCheck.IsRepresentative(i))
                                    validPlacement[i].SetValue(true);
                            }
                            round.SetValue(r + 1);
                        }

                        PinConfiguration pc = GetContractedPinConfiguration();
                        snowflakeCheck.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        snowflakeCheck.ActivateSend();
                    }
                    break;
            }
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

            if (scale < 1)
            {
                Log.Error("Scale must be >= 1");
                scale = 1;
            }

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
            SnowflakeInfo snowflakeInfo = new SnowflakeInfo();
            // Find all occurring arm lengths and sort them in ascending order
            List<int> armLengths = new List<int>();
            for (int i = 0; i < sc.dependencyTree.Length; i++)
            {
                foreach (int l in sc.dependencyTree[i].arms)
                {
                    if (l > 0 && !armLengths.Contains(l))
                        armLengths.Add(l);
                }
            }
            armLengths.Sort();

            // Replace all actual arm lengths with indices in the list
            for (int i = 0; i < sc.dependencyTree.Length; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    int l = sc.dependencyTree[i].arms[j];
                    if (l == 0)
                        sc.dependencyTree[i].arms[j] = -1;
                    else
                        sc.dependencyTree[i].arms[j] = armLengths.FindIndex(a => a == l);
                }
            }

            // Compute string representations of all line lengths
            string[] armLengthsStr = new string[armLengths.Count];
            for (int i = 0; i < armLengths.Count; i++)
            {
                armLengthsStr[i] = IntToBinary(armLengths[i]);
            }

            // Find the longest parameter string and store all data in the snowflake info container
            snowflakeInfo.longestParameter = armLengths.Count > 0 ? armLengthsStr[armLengths.Count - 1].Length : 0;
            snowflakeInfo.armLengths = armLengths.ToArray();
            snowflakeInfo.armLengthsStr = armLengthsStr;
            snowflakeInfo.nodes = sc.dependencyTree;
            SnowflakeTestParticle.scaleFactor = scale;
            SnowflakeTestParticle.snowflakeInfo = snowflakeInfo;

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

            // Place the counter (must have length at least 1!)
            // The counter must be able to store the longest line of the scaled shape
            // Also set counter index for amoebots storing the base arm lengths
            int longestLineScaled = scale * sc.shape.GetLongestLineLength();
            string scaleStr = IntToBinary(scale);
            for (int x = 0; x < Mathf.Max(scaleStr.Length, 2, snowflakeInfo.longestParameter, longestLineScaled + 1); x++)
            {
                InitializationParticle p;
                Vector2Int pos = new Vector2Int(x, 0);
                if (!TryGetParticleAt(pos, out p))
                {
                    p = AddParticle(pos);
                    nPlaced++;
                }
                p.SetAttribute("onCounter", true);
                if (x < scaleStr.Length)
                {
                    p.SetAttribute("scaleBit", scaleStr[x] == '1');
                    p.SetAttribute("scaleMSB", x == scaleStr.Length - 1);
                }
                if (x < snowflakeInfo.longestParameter)
                {
                    p.SetAttribute("counterIndex", x);
                }
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
