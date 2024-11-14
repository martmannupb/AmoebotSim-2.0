using AS2.Sim;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.Exercise
{

    // HOMEWORK - EXERCISE 2
    // =====================
    // Task: Implement the PASC algorithm and use it to expand half of the amoebots.
    // 
    // You are given a line of n >= 2 amoebots on the West-East axis, where n is
    // chosen randomly and the amoebots have a common compass orientation aligned
    // with the grid axes. The amoebots should run the PASC algorithm to determine
    // the floor(n / 2) easternmost amoebots and make them expand. After that,
    // the algorithm should terminate so that the simulation stops.
    // You may not use any existing subroutines.
    // 
    // Hints:
    // - First, think about the given amoebot structure: You will need to identify a
    //   start point for the PASC chain (how can it identify itself?) and a direction
    //   in which the PASC algorithm is oriented.
    // - How many partition sets does each amoebot need? How does it connect them
    //   to its predecessor/successor based on its own current state?
    // - The amoebots need to find out when the PASC algorithm is finished. Which
    //   amoebots know that the algorithm is NOT yet finished? And how can they inform
    //   the other amoebots? (You may need to switch back and forth between running the
    //   PASC algorithm and checking whether it is finished)
    // - If you implement the PASC algorithm correctly, each amoebot will receive
    //   its own "index" i on the chain (0, 1, ..., n-1) in binary, as a sequence of
    //   bits. Try to let each amoebot compare its index i to floor((n-1) / 2).
    //   For which comparison result should the amoebot expand
    //   (i > / >= / < / <= floor((n-1) / 2))? Draw a few small examples for even
    //   and odd n on paper. It is less complicated than you might think!

    public class ExerciseParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Exercise";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 2;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(ExerciseInitializer).FullName;

        // Declare attributes here
        // <ADD YOUR ATTRIBUTES HERE>

        public ExerciseParticle(Particle p) : base(p)
        {
            // <INITIALIZE YOUR ATTRIBUTES HERE>

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Blue);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            // <IMPLEMENT THIS TO MAKE THE ALGORITHM TERMINATE>
            return false;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // <IMPLEMENT YOUR MOVEMENT CODE HERE>
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            // <IMPLEMENT YOUR COMMUNICATION CODE HERE>
        }
    }


    // <DO NOT MODIFY THIS>
    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class ExerciseInitializer : InitializationMethod
    {
        public ExerciseInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(/* Parameters with default values */)
        {
            int n = Random.Range(2, 51);
            PlaceParallelogram(Vector2Int.zero, Direction.E, n);
        }
    }

} // namespace AS2.Algos.Exercise
