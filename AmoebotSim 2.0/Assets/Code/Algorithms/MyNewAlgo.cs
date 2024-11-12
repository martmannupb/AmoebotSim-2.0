using AS2.Sim;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.MyNewAlgo
{

    public class MyNewAlgoParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "MyNewAlgo";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // If the algorithm has a special generation method, specify its full name here
        //public static new string GenerationMethod => typeof(MyNewAlgoInitializer).FullName;

        // Declare attributes here
        // ...

        public MyNewAlgoParticle(Particle p) : base(p)
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
            PinConfiguration pc = GetNextPinConfiguration();
            pc.SetToGlobal(0);
            pc.SetPartitionSetColor(0, ColorData.Particle_Blue);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    //public class MyNewAlgoInitializer : InitializationMethod
    //{
    //    public MyNewAlgoInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

    //    // This method implements the system generation
    //    // Its parameters will be shown in the UI and they must have default values
    //    public void Generate(/* Parameters with default values */)
    //    {
    //        // The parameters of the Init() method can be set as particle attributes here
    //    }
    //}

} // namespace AS2.Algos.MyNewAlgo
