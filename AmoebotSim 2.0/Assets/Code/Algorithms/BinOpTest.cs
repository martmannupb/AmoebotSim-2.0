using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.BinaryOps;

namespace AS2.Algos.BinOpTest
{

    public class BinOpTestParticle : ParticleAlgorithm
    {
        public enum Mode
        {
            MULT
        }

        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Binary Op Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(BinOpTestInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<bool> a;
        ParticleAttribute<bool> b;

        ParticleAttribute<Direction> pred;
        ParticleAttribute<Direction> succ;
        ParticleAttribute<bool> isStart;
        ParticleAttribute<bool> isMSBA;
        ParticleAttribute<bool> isMSBB;

        ParticleAttribute<int> round;
        ParticleAttribute<Mode> mode;

        SubMultiplication mult;

        public BinOpTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            a = CreateAttributeBool("a", false);
            b = CreateAttributeBool("b", false);

            pred = CreateAttributeDirection("Pred", Direction.NONE);
            succ = CreateAttributeDirection("Succ", Direction.NONE);
            isStart = CreateAttributeBool("Start", false);
            isMSBA = CreateAttributeBool("MSB a", false);
            isMSBB = CreateAttributeBool("MSB b", false);

            round = CreateAttributeInt("Round", -2);
            mode = CreateAttributeEnum<Mode>("Mode", Mode.MULT);

            mult = new SubMultiplication(p);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Blue);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool a = false, bool b = false, Mode mode = Mode.MULT)
        {
            // this code is executed directly after the constructor
            this.a.SetValue(a);
            this.b.SetValue(b);
            this.mode.SetValue(mode);
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
            if (round == -2)
            {
                // Initialize
                if (HasNeighborAt(Direction.W))
                    pred.SetValue(Direction.W);
                else
                    isStart.SetValue(true);

                if (HasNeighborAt(Direction.E))
                    succ.SetValue(Direction.E);

                // Setup circuits to find MSBs
                PinConfiguration pc = GetContractedPinConfiguration();
                bool sendA = false;
                bool sendB = false;
                if (a)
                {
                    // Send beep backwards
                    sendA = true;
                }
                else
                {
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(Direction.W, 0).Id, pc.GetPinAt(Direction.E, 3).Id }, 0);
                }

                if (b)
                {
                    // Send beep backwards
                    sendB = true;
                }
                else
                {
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(Direction.W, 3).Id, pc.GetPinAt(Direction.E, 0).Id }, 2);
                }
                SetPlannedPinConfiguration(pc);
                if (sendA)
                    pc.GetPinAt(Direction.W, 0).PartitionSet.SendBeep();
                if (sendB)
                    pc.GetPinAt(Direction.W, 3).PartitionSet.SendBeep();

                round.SetValue(round + 1);
            }
            else if (round == -1)
            {
                // Receive beep and determine whether we are the MSB
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (!pc.GetPinAt(Direction.E, 3).PartitionSet.ReceivedBeep())
                {
                    // Received no beep, check if we are the MSB
                    isMSBA.SetValue(a || isStart);
                }
                if (!pc.GetPinAt(Direction.E, 0).PartitionSet.ReceivedBeep())
                {
                    // Received no beep, check if we are the MSB
                    isMSBB.SetValue(b || isStart);
                }

                round.SetValue(round + 1);
            }
            else
            {
                if (mode == Mode.MULT)
                {
                    ActivateMult();
                }
            }
        }

        private void ActivateMult()
        {
            if (round == 0)
            {
                // Initialization, round 0
                mult.Init(a, b, isStart, isMSBA, isMSBB, pred, succ);
                PinConfiguration pc = GetContractedPinConfiguration();
                mult.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                mult.ActivateSend();
                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                // Round 1
                mult.ActivateReceive();
                if (mult.Finished())
                {
                    round.SetValue(2);
                    return;
                }

                PinConfiguration pc = GetCurrentPinConfiguration();
                mult.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                mult.ActivateSend();
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class BinOpTestInitializer : InitializationMethod
    {
        public BinOpTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 10, int a = 42, int b = 13, BinOpTestParticle.Mode mode = BinOpTestParticle.Mode.MULT)
        {
            string binA = IntToBinary(a);
            string binB = IntToBinary(b);
            int num = Mathf.Max(numParticles, binA.Length, binB.Length);
            InitializationParticle p;
            for (int x = 0; x < num; x++)
            {
                p = AddParticle(new Vector2Int(x, 0));
                if (x < binA.Length && binA[x] == '1')
                    p.SetAttribute("a", true);
                if (x < binB.Length && binB[x] == '1')
                    p.SetAttribute("b", true);
                p.SetAttribute("mode", mode);
            }
            Log.Debug("42 = " + IntToBinary(42));
        }

        private string IntToBinary(int num)
        {
            string s = "";

            while (num > 0)
            {
                s += (num & 1) > 0 ? '1' : '0';
                num >>= 1;
            }

            return s;
        }
    }

} // namespace AS2.Algos.BinOpTest
