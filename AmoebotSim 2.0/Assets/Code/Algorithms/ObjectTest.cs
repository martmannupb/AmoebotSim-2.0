using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.ObjectTest
{

    public class ObjectTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "ObjectTest";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(ObjectTestInitializer).FullName;

        // Declare attributes here
        // ...

        public ObjectTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            // Also, set the default initial color
            //SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        //public void Init(/* Custom parameters with default values */)
        //{
        //    // This code is executed directly after the constructor
        //}

        // Implement this method if the algorithm terminates at some point
        //public override bool IsFinished()
        //{
        //    // Return true when this particle has terminated
        //    return false;
        //}

        // The movement activation method
        public override void ActivateMove()
        {
            if (IsContracted())
            {
                //TriggerObjectBondRelease(Direction.E);
                //Expand(Direction.E);
                //return;

                // Find a direction into which we can expand
                Direction objDir = Direction.NONE;
                for (int i = 0; i < 6; i++)
                {
                    Direction d = DirectionHelpers.Cardinal(i);
                    if (HasObjectAt(d))
                    {
                        Log.Debug("Object in direction " + d + ", ID is " + GetObjectAt(d).Identifier);
                        if (!HasObjectAt(d.Opposite()))
                        {
                            // Can expand here!
                            objDir = d;
                        }
                    }
                    else
                        Log.Debug("No object in direction " + d);
                }

                if (objDir != Direction.NONE)
                {
                    TriggerObjectBondRelease(objDir);
                    // Must mark all bonds for the movement to work
                    foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                        if (HasObjectAt(d))
                            MarkBond(d);
                    Expand(objDir);
                }
            }
            else
            {
                // Find neighbor object
                if (FindFirstObjectNeighbor(out Neighbor<IParticleObject> nbr))
                {
                    // Set random color
                    nbr.neighbor.SetColor(new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
                }
            }
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            // Implement the communication code here
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class ObjectTestInitializer : InitializationMethod
    {
        public ObjectTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numPositions = 10, float holeProb = 0.3f, bool fillHoles = false,
            bool allowExcludedHoles = false, bool prioritizeInner = false, float lambda = 0.1f)
        {
            // Must add at least one particle
            AddParticle(Vector2Int.zero);

            //ParticleObject o = CreateObject(new Vector2Int(3, 0));
            //o.AddPosition(new Vector2Int(4, 0));
            //o.AddPosition(new Vector2Int(4, 1));
            //o.AddPosition(new Vector2Int(4, 2));
            //o.AddPosition(new Vector2Int(4, 3));
            //o.AddPosition(new Vector2Int(4, 4));
            //o.AddPosition(new Vector2Int(5, 4));
            //o.AddPosition(new Vector2Int(6, 4));
            //o.AddPosition(new Vector2Int(6, 3));
            //o.AddPosition(new Vector2Int(3, 1));
            //o.AddPosition(new Vector2Int(3, 2));
            //o.AddPosition(new Vector2Int(5, 2));
            //o.AddPosition(new Vector2Int(6, 2));
            //o.AddPosition(new Vector2Int(5, 0));
            //o.AddPosition(new Vector2Int(6, 0));
            //o.AddPosition(new Vector2Int(7, 0));
            //o.AddPosition(new Vector2Int(7, 1));

            //AddObjectToSystem(o);

            // Create an object using the Random With Holes algorithm
            ParticleObject o = CreateObject(new Vector2Int(10, 0));
            System.Func<Vector2Int, bool> excludeFunc = (Vector2Int v) => v == Vector2Int.zero;
            //System.Func<Vector2Int, bool> excludeFunc = (Vector2Int v) => {
            //    int dist = ParticleSystem_Utils.GridDistance(v, Vector2Int.zero);
            //    return dist <= 3 || dist >= 12 || v == new Vector2Int(3, 2) || v == new Vector2Int(4, 2)
            //    || v == new Vector2Int(-6, -3);
            //};
            List<Vector2Int> positions = GenerateRandomConnectedPositions(o.Position, numPositions, holeProb, fillHoles, excludeFunc, allowExcludedHoles, prioritizeInner, lambda);
            foreach (Vector2Int p in positions)
                o.AddPosition(p);

            AddObjectToSystem(o);
        }
    }

} // namespace AS2.Algos.ObjectTest
