using AS2.Sim;
using UnityEngine;
using AS2.Subroutines;

namespace AS2.Algos.SubroutineTest
{

    /// <summary>
    /// Tests the subroutine mechanism by electing a leader
    /// on every boundary of the system independently.
    /// <para>
    /// Runs 3 instances of the leader election subroutine in
    /// parallel. Uses a global circuit on which a beep is
    /// sent regularly as long as some leader election
    /// procedure has not finished.
    /// </para>
    /// </summary>
    public class SubroutineTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Subroutine Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 2;

        // If the algorithm has a special generation method, specify its full name here
        //public static new string GenerationMethod => typeof(SubroutineTestInitializer).FullName;

        // Declare attributes here
        // ...
        private ParticleAttribute<bool> firstRound;
        private ParticleAttribute<bool> finished;

        private Subroutines.LeaderElection.SubLeaderElection subLE1;
        private Subroutines.LeaderElection.SubLeaderElection subLE2;
        private Subroutines.LeaderElection.SubLeaderElection subLE3;

        public SubroutineTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            // Also, set the default initial color
            //SetMainColor(ColorData.Particle_Black);

            firstRound = CreateAttributeBool("First Round", true);
            finished = CreateAttributeBool("Finished", false);

            subLE1 = new Subroutines.LeaderElection.SubLeaderElection(p);
            subLE2 = new Subroutines.LeaderElection.SubLeaderElection(p);
            subLE3 = new Subroutines.LeaderElection.SubLeaderElection(p);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        //public void Init(/* Custom parameters with default values */)
        //{
        //    // This code is executed directly after the constructor
        //}

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return finished;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            PinConfiguration pc = GetCurrentPinConfiguration();
            if (firstRound)
            {
                pc.SetToGlobal(0);
                SetPlannedPinConfiguration(pc);
                subLE1.Init(0, 3);
                firstRound.SetValue(false);
            }
            subLE1.ActivateReceive();
            subLE1.ActivateSend();

            if (subLE1.IsCandidate())
                SetMainColor(ColorData.Particle_Blue);
            else
                SetMainColor(ColorData.Particle_BlueDark);

            if (subLE1.IsFinished())
            {
                finished.SetValue(true);
                if (subLE1.IsLeader())
                    SetMainColor(ColorData.Particle_Green);
                else
                    SetMainColor(ColorData.Particle_Black);
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    //public class SubroutineTestInitializer : InitializationMethod
    //{
    //    public SubroutineTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

    //    // This method implements the system generation
    //    // Its parameters will be shown in the UI and they must have default values
    //    public void Generate(/* Parameters with default values */)
    //    {
    //        // The parameters of the Init() method can be set as particle attributes here
    //    }
    //}

} // namespace AS2.Algos.SubroutineTest
