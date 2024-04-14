using AS2.ShapeContainment;
using AS2.Sim;
using AS2.UI;
using AS2.Subroutines.ShapeConstruction;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.ShapeConstruction
{

    /// <summary>
    /// Test algorithm for the shape construction subroutine.
    /// <para>
    /// <b>Disclaimer: The save/load feature does not work for
    /// this algorithm because it stores the target shape in a
    /// static member. Always generate this algorithm from
    /// Init Mode.</b>
    /// </para>
    /// </summary>
    
    // Round 0:
    //  - Initialize subroutine
    //  - Send first beeps
    // Round 1:
    //  Receive:
    //  - Receive subroutine beeps
    //  - If finished: Terminate
    //  Send:
    //  - Setup subroutine PC
    //  - Send subroutine beeps

    public class ShapeConstructionParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Shape Construction";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(ShapeConstructionInitializer).FullName;

        [StatusInfo("Display Shape", "Displays the target shape")]
        public static void ShowShape(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            // Get rotation and scale from some amoebot
            Particle p = system.particles[0];
            ShapeConstructionParticle scp = (ShapeConstructionParticle)p.algorithm;
            int rotation = scp.rotation.GetValue();
            string scaleStr = scp.scale.GetValue();
            int scale = 0;
            int incr = 1;
            for (int i = 0; i < scaleStr.Length; i++)
            {
                if (scaleStr[i] == '1')
                    scale += incr;
                incr *= 2;
            }

            LineDrawer.Instance.Clear();
            shape.Draw(Vector2Int.zero, rotation, scale);
            LineDrawer.Instance.SetTimer(20);
        }

        [StatusInfo("Display Traversal", "Displays the target shape's traversal")]
        public static void ShowTraversal(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            // Get rotation and scale from some amoebot
            Particle p = system.particles[0];
            ShapeConstructionParticle scp = (ShapeConstructionParticle)p.algorithm;
            int rotation = scp.rotation.GetValue();
            string scaleStr = scp.scale.GetValue();
            int scale = 0;
            int incr = 1;
            for (int i = 0; i < scaleStr.Length; i++)
            {
                if (scaleStr[i] == '1')
                    scale += incr;
                incr *= 2;
            }

            LineDrawer.Instance.Clear();
            shape.DrawTraversal(Vector2Int.zero, rotation, scale);
            LineDrawer.Instance.SetTimer(20);
        }

        public static Shape shape;

        // Declare attributes here
        ParticleAttribute<int> round;           // Round counter
        ParticleAttribute<bool> isRepr;         // Representative of the shape
        ParticleAttribute<int> rotation;        // Shape rotation
        ParticleAttribute<string> scale;        // Scale as binary number (LSB...MSB)
        ParticleAttribute<int> scaleIndex;      // Index used to traverse the scale (replaced by binary counter in actual algorithm)

        SubShapeConstruction shapeConstr;

        public ShapeConstructionParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            isRepr = CreateAttributeBool("Representative", false);
            rotation = CreateAttributeInt("Rotation", 0);
            scale = CreateAttributeString("Scale", "1");
            scaleIndex = CreateAttributeInt("Scale Index", 0);

            shapeConstr = new SubShapeConstruction(p, shape);

            // Also, set the default initial color
            SetMainColor(Color.gray);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool repr = false, int rotation = 0, string scale = "1")
        {
            this.isRepr.SetValue(repr);
            this.rotation.SetValue(rotation);
            this.scale.SetValue(scale);

            if (repr)
                SetMainColor(ColorData.Particle_Green);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return round.GetValue() == 2;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (round == 0)
            {
                shapeConstr.Init(isRepr.GetCurrentValue(), rotation.GetCurrentValue());

                PinConfiguration pc = GetContractedPinConfiguration();
                shapeConstr.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                shapeConstr.ActivateSend();

                round.SetValue(1);
            }
            else if (round == 1)
            {
                shapeConstr.ActivateReceive();
                
                // Update color
                if (shapeConstr.ElementType() == SubShapeConstruction.ShapeElement.NODE)
                    SetMainColor(ColorData.Particle_Green);
                else if (shapeConstr.ElementType() == SubShapeConstruction.ShapeElement.EDGE)
                    SetMainColor(ColorData.Particle_Blue);
                else if (shapeConstr.ElementType() == SubShapeConstruction.ShapeElement.FACE)
                    SetMainColor(ColorData.Particle_Aqua);

                if (shapeConstr.IsFinished())
                {
                    if (shapeConstr.IsSuccessful())
                    {
                        SetPlannedPinConfiguration(GetContractedPinConfiguration());
                    }
                    else
                    {
                        SetMainColor(ColorData.Particle_Red);
                    }
                    round.SetValue(2);
                    return;
                }
                
                // Check if we have to reset the scale index
                if (shapeConstr.ResetScaleCounter())
                {
                    scaleIndex.SetValue(0);
                }

                PinConfiguration pc = GetContractedPinConfiguration();
                shapeConstr.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                if (shapeConstr.NeedScaleBit())
                {
                    shapeConstr.ActivateSend(scale.GetValue()[scaleIndex.GetCurrentValue()] == '1', scaleIndex.GetCurrentValue() == scale.GetValue().Length - 1);
                    scaleIndex.SetValue(scaleIndex.GetCurrentValue() + 1);
                }
                else
                {
                    shapeConstr.ActivateSend();
                }
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class ShapeConstructionInitializer : InitializationMethod
    {
        public ShapeConstructionInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(string shape = "shape1.json", bool fromFile = true, int rotation = 0, int scale = 1, int numAmoebots = 50, bool fillShape = true, float holeProb = 0.3f, bool fillHoles = false)
        {
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
                ShapeConstructionParticle.shape = s;
            }

            rotation = rotation % 6;
            if (rotation < 0)
            {
                rotation += 6;
            }

            if (scale < 1)
            {
                Log.Error("Scale must be >= 1");
                scale = 1;
            }

            string scaleStr = IntToBinary(scale);

            // Place amoebot system
            InitializationParticle p;
            foreach (Vector2Int v in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles))
            {
                p = AddParticle(v);
                p.SetAttributes(new object[] { v == Vector2Int.zero, rotation, scaleStr });
            }

            // Fill up positions of the shape
            if (fillShape)
            {
                if (scale == 1)
                {
                    foreach (Shape.Node node in s.nodes)
                    {
                        TryPlaceParticle(AmoebotFunctions.RotateVector(node, rotation), rotation, scaleStr);
                    }
                }
                else
                {
                    // Fill edges
                    foreach (Shape.Edge edge in s.edges)
                    {
                        Vector2Int n1 = s.nodes[edge.u];
                        Vector2Int n2 = s.nodes[edge.v];
                        n1 = AmoebotFunctions.RotateVector(n1, rotation);
                        n2 = AmoebotFunctions.RotateVector(n2, rotation);
                        Vector2Int to = n2 - n1;
                        n1 *= scale;
                        for (int i = 0; i < scale + 1; i++)
                        {
                            TryPlaceParticle(n1 + i * to, rotation, scaleStr);
                        }
                    }

                    // Fill faces
                    foreach (Shape.Face face in s.faces)
                    {
                        Vector2Int n1 = s.nodes[face.u];
                        Vector2Int n2 = s.nodes[face.v];
                        Vector2Int n3 = s.nodes[face.w];
                        n1 = AmoebotFunctions.RotateVector(n1, rotation);
                        n2 = AmoebotFunctions.RotateVector(n2, rotation);
                        n3 = AmoebotFunctions.RotateVector(n3, rotation);
                        Vector2Int to1 = n2 - n1;
                        Vector2Int to2 = n3 - n1;
                        n1 *= scale;
                        for (int i = 1; i < scale - 1; i++)
                        {
                            Vector2Int start = n1 + i * to1;
                            for (int j = 1; j < scale - i; j++)
                            {
                                TryPlaceParticle(start + j * to2, rotation, scaleStr);
                            }
                        }
                    }
                }
            }

            // Draw shape preview
            LineDrawer.Instance.Clear();
            s.Draw(Vector2Int.zero, rotation, scale);
            LineDrawer.Instance.SetTimer(20);
        }

        private void TryPlaceParticle(Vector2Int pos, int rotation, string scale)
        {
            if (!TryGetParticleAt(pos, out _))
            {
                InitializationParticle p = AddParticle(pos);
                p.SetAttributes(new object[] { pos == Vector2Int.zero, rotation, scale });
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

} // namespace AS2.Algos.ShapeConstruction
