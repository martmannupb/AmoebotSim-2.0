using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.BinaryOps;

namespace AS2.Algos.BinOpTest
{

    public class BinOpTestParticle : ParticleAlgorithm
    {
        // TODO: Find a way around the isActive flag?
        [StatusInfo("Display Mult Progress", null, false)]
        public static void StatusInfo(AS2.Sim.ParticleSystem system, Particle selectedParticle)
        {
            int x = 0;
            string a = "a = ";
            string b = "b = ";
            string c = "c = ";
            while (true)
            {
                if (system.TryGetParticleAt(new Vector2Int(x, 0), out Visuals.IParticleState particle))
                {
                    Particle part = (Particle)particle;
                    part.isActive = true;
                    BinOpTestParticle p = (BinOpTestParticle)part.algorithm;
                    a += p.mult.Bit_A() ? '1' : '0';
                    b += p.mult.Bit_B() ? '1' : '0';
                    c += p.mult.Bit_C() ? '1' : '0';
                    part.isActive = false;
                    x += 1;
                }
                else
                {
                    Debug.Log(a);
                    Debug.Log(b);
                    Debug.Log(c);
                    Debug.Log("\n");
                    return;
                }
            }
        }

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
        ParticleAttribute<bool> c;

        ParticleAttribute<Direction> pred;
        ParticleAttribute<Direction> succ;
        ParticleAttribute<bool> isStart;
        ParticleAttribute<bool> isMSBA;
        ParticleAttribute<bool> isMSBB;
        ParticleAttribute<bool> overflow;

        ParticleAttribute<int> round;
        ParticleAttribute<Mode> mode;

        SubMultiplication mult;

        public BinOpTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            a = CreateAttributeBool("a", false);
            b = CreateAttributeBool("b", false);
            c = CreateAttributeBool("c", false);

            pred = CreateAttributeDirection("Pred", Direction.NONE);
            succ = CreateAttributeDirection("Succ", Direction.NONE);
            isStart = CreateAttributeBool("Start", false);
            isMSBA = CreateAttributeBool("MSB a", false);
            isMSBB = CreateAttributeBool("MSB b", false);
            overflow = CreateAttributeBool("Overflow", false);

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
                mult.Init(a, b, isStart, isMSBA, pred, succ);
                PinConfiguration pc = GetContractedPinConfiguration();
                mult.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                mult.ActivateSend();
                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                // Run multiplication subroutine
                mult.ActivateReceive();
                if (mult.IsFinished())
                {
                    round.SetValue(2);
                    return;
                }

                PinConfiguration pc = GetCurrentPinConfiguration();
                mult.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                mult.ActivateSend();
            }
            else if (round == 2)
            {
                // Copy multiplication result
                c.SetValue(mult.Bit_C());

                overflow.SetValue(mult.HaveOverflow());
                if (c.GetCurrentValue())
                    SetMainColor(ColorData.Particle_Green);
                else
                    SetMainColor(ColorData.Particle_BlueDark);
                round.SetValue(3);
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
            string binATimesB = IntToBinary(a * b);
            int num = Mathf.Max(numParticles, binA.Length, binB.Length);

            if (mode == BinOpTestParticle.Mode.MULT && num < binATimesB.Length)
            {
                Log.Warning("Not enough amoebots: Overflow will happen!");
            }

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
            Log.Debug(a + " = " + binA);
            Log.Debug(b + " = " + binB);
            Log.Debug(a + " * " + b + " = " + binATimesB);
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
