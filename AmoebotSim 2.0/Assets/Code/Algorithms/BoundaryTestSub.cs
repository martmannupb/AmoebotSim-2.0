using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.BoundaryTest;

namespace AS2.Algos.BoundaryTestSub
{

    public class BoundaryTestSubParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Boundary Test Subroutine";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        //public static new string GenerationMethod => typeof(BoundaryTestSubInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<int> round;

        SubBoundaryTest boundaryTest;

        public BoundaryTestSubParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);

            boundaryTest = new SubBoundaryTest(p);

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
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return round > 1;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Implement the movement code here
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            switch (round.GetValue())
            {
                case 0:
                    {
                        boundaryTest.Init(true);

                        PinConfiguration pc = GetContractedPinConfiguration();
                        boundaryTest.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        boundaryTest.ActivateSend();
                        round.SetValue(round + 1);
                    }
                    break;
                case 1:
                    {
                        boundaryTest.ActivateReceive();

                        if (boundaryTest.IsFinished())
                        {
                            bool isLeader = false;
                            int nBoundaries = boundaryTest.NumBoundaries();
                            for (int i = 0; i < nBoundaries; i++)
                            {
                                if (boundaryTest.IsBoundaryLeader(i))
                                {
                                    isLeader = true;
                                    break;
                                }
                            }
                            if (boundaryTest.OnOuterBoundary())
                            {
                                if (boundaryTest.IsOuterBoundaryLeader())
                                    SetMainColor(ColorData.Particle_Green);
                                else if (isLeader)
                                    SetMainColor(ColorData.Particle_Red);
                                else
                                    SetMainColor(ColorData.Particle_Aqua);
                            }
                            else if (boundaryTest.OnInnerBoundary())
                            {
                                if (isLeader)
                                    SetMainColor(ColorData.Particle_Red);
                                else
                                    SetMainColor(ColorData.Particle_Orange);
                            }
                            else
                                SetMainColor(ColorData.Particle_Black);

                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                            round.SetValue(round + 1);
                            break;
                        }

                        PinConfiguration pc = GetContractedPinConfiguration();
                        boundaryTest.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        boundaryTest.ActivateSend();
                    }
                    break;
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    //public class BoundaryTestSubInitializer : InitializationMethod
    //{
    //    public BoundaryTestSubInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

    //    // This method implements the system generation
    //    // Its parameters will be shown in the UI and they must have default values
    //    public void Generate(/* Parameters with default values */)
    //    {
    //        // The parameters of the Init() method can be set as particle attributes here
    //    }
    //}

} // namespace AS2.Algos.SubBoundaryTest
