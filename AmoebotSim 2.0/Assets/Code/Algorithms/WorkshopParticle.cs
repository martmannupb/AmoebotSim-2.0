using AS2.Sim;
using UnityEngine;

namespace Algorithms.Workshop
{

    public class WorkshopParticle : ParticleAlgorithm
    {
        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Workshop";

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(WorkshopParticleInitializer).FullName;

        // Declare attributes here
        private ParticleAttribute<bool> isLeader;
        // ...

        public WorkshopParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            isLeader = CreateAttributeBool("Leader", false);

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
    public class WorkshopParticleInitializer : InitializationMethod
    {
        public WorkshopParticleInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

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

} // namespace Algorithms.Workshop
