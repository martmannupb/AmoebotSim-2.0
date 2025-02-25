// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


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
    // the floor(n / 2) easternmost amoebots and make them expand in the East
    // direction. After that, the algorithm should terminate so that the simulation
    // stops (implement IsFinished() for this).
    // You may not use any existing subroutines.
    // To check your solution, the amoebots that should expand are initially colored
    // green and the others are colored red.
    // 
    // Hints:
    // - First, think about the given amoebot structure: You will need to identify a
    //   start point for the PASC chain (how can it identify itself?) and fix a
    //   direction in which the PASC algorithm is oriented.
    // - How many partition sets does each amoebot need? How does it connect them
    //   to its predecessor/successor based on its own current state?
    // - Use amoebot colors to visualize which amoebots are still active and which are
    //   not (call e.g. SetMainColor(ColorData.Particle_Blue) when becoming passive).
    // - The amoebots need to find out when the PASC algorithm is finished. Which
    //   amoebots know that the algorithm is NOT yet finished? And how can they inform
    //   the other amoebots? (You may need to switch back and forth between running the
    //   PASC algorithm and checking whether it is finished)
    // - If you implement the PASC algorithm correctly, each amoebot will receive
    //   its own "index" i on the chain (0, 1, ..., n-1) in binary, as a sequence of
    //   bits from the lowest to the highest value.
    // - Try to let each amoebot compare its index i to floor((n-1) / 2). How do you
    //   compare two numbers when they are given as bit sequences? For which comparison
    //   result should the amoebot expand (i > / >= / < / <= floor((n-1) / 2))? Draw a
    //   few small examples for even and odd n on paper. It is less complicated than you
    //   might think!
    // - Compare i to n-1 first and then figure out how to obtain the bit sequence of
    //   floor((n-1) / 2).
    // - If you use the 2 available pins efficiently, you will not have to add a third
    //   round to the PASC iterations. One mode for the main PASC beeps and one mode for
    //   termination and other beeps is sufficient.

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

        // <DO NOT MODIFY THIS>
        public void Init(bool expand = false)
        {
            if (expand)
                SetMainColor(ColorData.Particle_Green);
            else
                SetMainColor(ColorData.Particle_Red);
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
        public void Generate(int maxAmoebots = 50)
        {
            maxAmoebots = Mathf.Max(maxAmoebots, 2);
            int n = Random.Range(2, maxAmoebots + 1);
            int border = n - Mathf.FloorToInt(n / 2);
            for (int i = 0; i < n; i++)
            {
                InitializationParticle ip = AddParticle(new Vector2Int(i, 0));
                if (i >= border)
                    ip.SetAttribute("expand", true);
            }
        }
    }

} // namespace AS2.Algos.Exercise
