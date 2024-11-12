using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.PASC;

namespace AS2.Algos.PASCTestAlgo
{

    public class PASCTestAlgoParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "PASC Sub Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(PASCTestAlgoInitializer).FullName;

        private SubPASC pasc1;
        private SubPASC pasc2;

        // Declare attributes here
        ParticleAttribute<bool> firstRound;

        ParticleAttribute<string> dist1;
        ParticleAttribute<string> dist2;

        public PASCTestAlgoParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            dist1 = CreateAttributeString("Distance 1", string.Empty);
            dist2 = CreateAttributeString("Distance 2", string.Empty);

            firstRound = CreateAttributeBool("First Round", true);
            
            pasc1 = new SubPASC(p);
            pasc2 = new SubPASC(p);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Aqua);
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

            if (firstRound)
            {
                bool hasNbrW = HasNeighborAt(Direction.W);
                bool hasNbrE = HasNeighborAt(Direction.E);
                pasc1.Init(!hasNbrW, hasNbrW ? Direction.W : Direction.NONE, hasNbrE ? Direction.E : Direction.NONE, 0, 3, 3, 0, 0, 1);
                pasc2.Init(!hasNbrE, hasNbrE ? Direction.E : Direction.NONE, hasNbrW ? Direction.W : Direction.NONE, 2, 1, 1, 2, 2, 3);

                firstRound.SetValue(false);
            }
            else
            {
                pasc1.ActivateReceive();
                pasc2.ActivateReceive();
                dist1.SetValue(dist1 + pasc1.GetReceivedBit().ToString());
                dist2.SetValue(dist2 + pasc2.GetReceivedBit().ToString());
            }

            PinConfiguration pc = GetNextPinConfiguration();
            pasc1.SetupPC(pc);
            pasc2.SetupPC(pc);
            pasc1.ActivateSend();
            pasc2.ActivateSend();
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class PASCTestAlgoInitializer : InitializationMethod
    {
        public PASCTestAlgoInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 10)
        {
            for (int i = 0; i < numParticles; i++)
                AddParticle(new Vector2Int(i, 0));
        }
    }

} // namespace AS2.Algos.PASCTestAlgo
