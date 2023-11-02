using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.ShapeRecogTest
{

    public class ShapeRecogTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "ShapeRecogTest";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // If the algorithm has a special generation method, specify its full name here
        //public static new string GenerationMethod => typeof(ShapeRecogTestInitializer).FullName;

        // Declare attributes here
        private ParticleAttribute<int> round;

        public ShapeRecogTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("round", 0);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
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
            if (round == 0)
            {
                if (!HasNeighborAt(Direction.W) && !HasNeighborAt(Direction.NNW))
                    SetMainColor(ColorData.Particle_Blue);
                else
                    SetMainColor(ColorData.Particle_Black);
            }
            else if (round == 1)
            {
                if (!HasNeighborAt(Direction.SSW))
                    SetMainColor(ColorData.Particle_Green);
                else
                    SetMainColor(ColorData.Particle_Black);
            }

            round.SetValue((round + 1) % 2);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    //public class ShapeRecogTestInitializer : InitializationMethod
    //{
    //    public ShapeRecogTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

    //    // This method implements the system generation
    //    // Its parameters will be shown in the UI and they must have default values
    //    public void Generate(/* Parameters with default values */)
    //    {
    //        // The parameters of the Init() method can be set as particle attributes here
    //    }
    //}

} // namespace AS2.Algos.ShapeRecogTest
