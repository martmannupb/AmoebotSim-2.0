using AS2.Sim;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.Workshop1
{

    // WORKSHOP - EXERCISE 1
    // =====================
    // Task: Implement an algorithm that performs joint movements in random intervals
    // 
    // You are given a simple algorithm template that places 10 amoebots in a line and selects a
    // random leader (indicated by the value of the isLeader attribute).
    // In each round, the leader should send a beep to all amoebots with some probability (e.g., 0.3).
    // If an amoebot receives a beep, it should perform a movement: It should expand in the East direction
    // if it is currently contracted and contract into its tail if it is currently expanded.
    // You will have to establish an appropriate circuit so that the beep sent by the leader can reach
    // all amoebots.
    //
    // Hint: Read the implementation walkthrough in the User Guide!
    
    // BONUS TASKS:
    // ============
    // (a)
    // Print a debug log message when the leader decides to move.
    // - What is the difference between Debug.Log("...") and Log.Debug("...")?
    
    // (b)
    // Turn the number of amoebots into a parameter that can be modified in Init Mode.
    
    // (c)
    // Turn the movement probability of the leader into a parameter that can be modified in Init Mode.
    
    // (d) (challenge)
    // Introduce new movement directions to make the end of the line move far away from its initial position.
    // Hints:
    // - Try doing directions NNE and SSE first. They require less attention to bonds.
    // - When adding directions NNW and SSW, draw a picture to find out which bonds need to be released
    //   (or read further on in the User Guide).
    // - Choose one of these recommended solutions:
    //   Option 1: Define a new Message type that stores the selected movement direction
    //             The leader then sends the message for expansion and a simple beep for contraction.
    //   Option 2: Spread the communication over several rounds, reserving one round for each possible direction.
    //             The leader beeps in the round corresponding to the chosen direction. For this, you will need
    //             a new counter attribute that counts the number of elapsed rounds.

    public class Workshop1Particle : ParticleAlgorithm
    {
        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Workshop 1";

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(Workshop1ParticleInitializer).FullName;

        // Declare attributes here
        private ParticleAttribute<bool> isLeader;
        // <ADD YOUR ATTRIBUTES HERE>

        public Workshop1Particle(Particle p) : base(p)
        {
            // Initialize the attributes here
            isLeader = CreateAttributeBool("Leader", false);
            // <INITIALIZE YOUR ATTRIBUTES HERE>

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Blue);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool leader = false)
        {
            // This code is executed directly after the constructor
            if (leader)
            {
                isLeader.SetValue(true);
                SetMainColor(ColorData.Particle_Yellow);
            }
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
            // <IMPLEMENT YOUR MOVEMENT CODE HERE>
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            // <IMPLEMENT YOUR COMMUNICATION CODE HERE>
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class Workshop1ParticleInitializer : InitializationMethod
    {
        public Workshop1ParticleInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(/* Parameters with default values */)
        {
            // Place a line of 10 particles in East direction
            PlaceParallelogram(Vector2Int.zero, Direction.E, 10);

            // Select a random leader
            InitializationParticle[] particles = GetParticles();
            particles[Random.Range(0, particles.Length)].SetAttribute("leader", true);
        }
    }

} // namespace AS2.Algos.Workshop1
