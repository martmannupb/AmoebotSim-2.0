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
            MULT, DIV, COMP, ADD, SUB
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
        ParticleAttribute<bool> overflow;

        ParticleAttribute<int> round;
        ParticleAttribute<Mode> mode;

        ParticleAttribute<SubComparison.ComparisonResult> compResult;

        SubBinOps binOps;

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
            overflow = CreateAttributeBool("Overflow", false);

            round = CreateAttributeInt("Round", -2);
            mode = CreateAttributeEnum<Mode>("Mode", Mode.MULT);

            compResult = CreateAttributeEnum<SubComparison.ComparisonResult>("Result", SubComparison.ComparisonResult.NONE);

            binOps = new SubBinOps(p);

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
                binOps.Init(SubBinOps.Mode.MSB, a, pred.GetCurrentValue(), succ.GetCurrentValue());
                PinConfiguration pc = GetContractedPinConfiguration();
                binOps.SetupPinConfig(pc);
                SetNextPinConfiguration(pc);
                binOps.ActivateSend();

                round.SetValue(round + 1);
            }
            else if (round == -1)
            {
                // Receive beep and determine whether we are the MSB
                binOps.ActivateReceive();
                isMSBA.SetValue(binOps.IsMSB());

                round.SetValue(round + 1);
            }
            else if (round == 0)
            {
                // Initialize
                if (mode == Mode.MULT)
                {
                    binOps.Init(SubBinOps.Mode.MULT, a, pred, succ, b, isMSBA);
                }
                else if (mode == Mode.DIV)
                {
                    binOps.Init(SubBinOps.Mode.DIV, a, pred, succ, b, isMSBA);
                }
                else if (mode == Mode.COMP)
                {
                    binOps.Init(SubBinOps.Mode.COMP, a, pred, succ, b);
                }
                else if (mode == Mode.ADD)
                {
                    binOps.Init(SubBinOps.Mode.ADD, a, pred, succ, b);
                }
                else if (mode == Mode.SUB)
                {
                    binOps.Init(SubBinOps.Mode.SUB, a, pred, succ, b);
                }

                PinConfiguration pc = GetContractedPinConfiguration();
                binOps.SetupPinConfig(pc);
                SetNextPinConfiguration(pc);
                binOps.ActivateSend();
                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                // Run the subroutine
                binOps.ActivateReceive();
                if ((mode == Mode.ADD || mode == Mode.SUB) && binOps.IsFinishedOverflow()
                    || (mode != Mode.ADD && mode != Mode.SUB) && binOps.IsFinished())
                {
                    round.SetValue(2);
                    return;
                }

                PinConfiguration pc = GetCurrPinConfiguration();
                binOps.SetupPinConfig(pc);
                SetNextPinConfiguration(pc);
                binOps.ActivateSend();
            }
            else if (round == 2)
            {
                // Subroutine has finished
                if (mode == Mode.MULT)
                {
                    c.SetValue(binOps.ResultBit());
                    overflow.SetValue(binOps.HaveOverflow());
                }
                else if (mode == Mode.DIV)
                {
                    a.SetValue(binOps.ResultBit());
                    c.SetValue(binOps.RemainderBit());
                }
                else if (mode == Mode.COMP)
                {
                    compResult.SetValue(binOps.CompResult());
                }
                else if (mode == Mode.ADD)
                {
                    c.SetValue(binOps.ResultBit());
                    overflow.SetValue(binOps.HaveOverflow());
                }
                else if (mode == Mode.SUB)
                {
                    c.SetValue(binOps.ResultBit());
                    overflow.SetValue(binOps.HaveOverflow());
                }

                if (c.GetCurrentValue())
                    SetMainColor(ColorData.Particle_Green);
                else
                    SetMainColor(ColorData.Particle_BlueDark);

                round.SetValue(round + 1);
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
            string binADivB = IntToBinary(b > 0 ? a / b : 0);
            string binAModB = IntToBinary(b > 0 ? a % b : 0);
            string binAPlusB = IntToBinary(a + b);
            string binAMinusB = IntToBinary(a >= b ? a - b : 0);
            int num = Mathf.Max(numParticles, binA.Length, binB.Length);

            if (mode == BinOpTestParticle.Mode.MULT && num < binATimesB.Length || mode == BinOpTestParticle.Mode.ADD && num < binAPlusB.Length)
            {
                Log.Warning("Not enough amoebots: Overflow will happen!");
            }
            else if (mode == BinOpTestParticle.Mode.DIV && a < b)
            {
                Log.Error("Binary division will not work if a < b");
            }
            else if (mode == BinOpTestParticle.Mode.SUB && a < b)
            {
                Log.Warning("Subtraction: Overflow will happen due to a < b!");
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
            if (mode == BinOpTestParticle.Mode.MULT)
            {
                Log.Debug(a + " * " + b + " = " + binATimesB);
            }
            else if (mode == BinOpTestParticle.Mode.DIV)
            {
                Log.Debug(a + " / " + b + " = " + binADivB);
                Log.Debug(a + " mod " + b + " = " + binAModB);
            }
            else if (mode == BinOpTestParticle.Mode.ADD)
            {
                Log.Debug(a + " + " + b + " = " + binAPlusB);
            }
            else if (mode == BinOpTestParticle.Mode.SUB)
            {
                Log.Debug(a + " - " + b + " = " + binAMinusB);
            }
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
