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
                // Find a direction into which we can expand
                Direction objDir = Direction.NONE;
                for (int i = 0; i < 6; i++)
                {
                    Direction d = DirectionHelpers.Cardinal(i);
                    if (HasObjectAt(d))
                    {
                        Log.Debug("Object in direction " + d);
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
                    // Must mark all bonds for the movement to work
                    foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
                        MarkBond(d);
                    Expand(objDir);
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
        public void Generate(int numPositions = 10, float holeProb = 0.3f)
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
            int n = 1;
            // Start the object at some root position
            List<Vector2Int> candidates = new List<Vector2Int>();
            Vector2Int node = new Vector2Int(10, 0);
            ParticleObject o = CreateObject(node);

            for (int d = 0; d < 6; d++)
            {
                Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d));
                if (nbr != Vector2Int.zero)
                    candidates.Add(nbr);
            }

            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();       // Occupied by particles
            HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();       // Reserved for holes
            occupied.Add(node);

            int numExcludedChosen = 0;

            while (n < numPositions)
            {
                // Find next position
                Vector2Int newPos = Vector2Int.zero;
                bool choseExcluded = false;
                if (candidates.Count > 0)
                {
                    int randIdx = Random.Range(0, candidates.Count);
                    newPos = candidates[randIdx];
                    candidates.RemoveAt(randIdx);
                }
                else
                {
                    // Choose random excluded position
                    int randIdx = Random.Range(0, excluded.Count);
                    int i = 0;
                    foreach (Vector2Int v in excluded)
                    {
                        if (i == randIdx)
                        {
                            newPos = v;
                            break;
                        }
                        i++;
                    }
                    numExcludedChosen++;
                    excluded.Remove(newPos);
                    choseExcluded = true;
                }

                // Either use newPos to insert object or to insert hole
                if (choseExcluded || Random.Range(0.0f, 1.0f) >= holeProb)
                {
                    for (int d = 0; d < 6; d++)
                    {
                        Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                        if (nbr != Vector2Int.zero && !occupied.Contains(nbr) && !excluded.Contains(nbr) && !candidates.Contains(nbr))
                            candidates.Add(nbr);
                    }

                    o.AddPosition(newPos);

                    occupied.Add(newPos);
                    n++;
                }
                else
                {
                    excluded.Add(newPos);
                }
            }
            Log.Debug("Created object with " + n + " nodes, had to choose " + numExcludedChosen + " excluded positions");

            AddObjectToSystem(o);
        }
    }

} // namespace AS2.Algos.ObjectTest
