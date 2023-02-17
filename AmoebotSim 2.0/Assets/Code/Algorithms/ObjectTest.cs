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
    public class ObjectTestInitializer : InitializationMethod
    {
        public ObjectTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(/* Parameters with default values */)
        {
            // Must add at least one particle
            AddParticle(Vector2Int.zero);

            ParticleObject o = CreateObject(new Vector2Int(3, 0));
            o.AddPosition(new Vector2Int(4, 0));
            o.AddPosition(new Vector2Int(4, 1));
            o.AddPosition(new Vector2Int(4, 2));
            o.AddPosition(new Vector2Int(4, 3));
            o.AddPosition(new Vector2Int(4, 4));
            o.AddPosition(new Vector2Int(5, 4));
            o.AddPosition(new Vector2Int(6, 4));
            o.AddPosition(new Vector2Int(6, 3));
            o.AddPosition(new Vector2Int(3, 1));
            o.AddPosition(new Vector2Int(3, 2));

            AddObjectToSystem(o);
        }
    }

} // namespace AS2.Algos.ObjectTest
