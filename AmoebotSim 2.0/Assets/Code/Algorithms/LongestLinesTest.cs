using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.LongestLines;

namespace AS2.Algos.LongestLinesTest
{

    /// <summary>
    /// Very simple test algorithm to test the longest lines subroutine.
    /// </summary>
    public class LongestLinesTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Longest Lines Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        //public static new string GenerationMethod => typeof(LongestLinesTestInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<int> round;
        ParticleAttribute<bool> finished;

        SubLongestLines ll;

        public LongestLinesTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            finished = CreateAttributeBool("Finished", false);

            ll = new SubLongestLines(p);
            
            SetMainColor(ColorData.Particle_Black);
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
            if (round == 0)
            {
                ll.Init();
                PinConfiguration pc = GetContractedPinConfiguration();
                ll.SetupPC(pc);
                SetPlannedPinConfiguration(pc);
                ll.ActivateSend();
                round.SetValue(1);
            }
            else if (round == 1)
            {
                ll.ActivateReceive();
                if (ll.IsFinished())
                {
                    finished.SetValue(true);
                    if (ll.IsOnMaxLine())
                    {
                        if (ll.IsMSB())
                            SetMainColor(Color.yellow);
                        else if (ll.GetBit())
                            SetMainColor(ColorData.Particle_Green);
                        else
                            SetMainColor(ColorData.Particle_Blue);
                    }
                    else
                    {
                        SetMainColor(ColorData.Particle_Black);
                    }
                    round.SetValue(2);
                    return;
                }

                PinConfiguration pc = GetContractedPinConfiguration();
                ll.SetupPC(pc);
                SetPlannedPinConfiguration(pc);
                ll.ActivateSend();
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    //public class LongestLinesTestInitializer : InitializationMethod
    //{
    //    public LongestLinesTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

    //    // This method implements the system generation
    //    // Its parameters will be shown in the UI and they must have default values
    //    public void Generate(/* Parameters with default values */)
    //    {
    //        // The parameters of the Init() method can be set as particle attributes here
    //    }
    //}

} // namespace AS2.Algos.LongestLinesTest
