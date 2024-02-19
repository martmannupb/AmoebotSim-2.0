using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.SegmentShift;

namespace AS2.Algos.SegmentShiftTest
{

    /// <summary>
    /// Simple algorithm for testing the segment shift subroutine
    /// </summary>
    public class SegmentShiftTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Segment Shift Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SegmentShiftTestInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<int> round;
        ParticleAttribute<bool> onCounter;
        ParticleAttribute<bool> distanceBit;
        ParticleAttribute<bool> distanceMSB;
        ParticleAttribute<bool> highlighted;

        SubSegmentShift segShift;

        public SegmentShiftTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            onCounter = CreateAttributeBool("On Counter", false);
            distanceBit = CreateAttributeBool("Dist bit", false);
            distanceMSB = CreateAttributeBool("Dist MSB", false);
            highlighted = CreateAttributeBool("Highlighted", false);

            segShift = new SubSegmentShift(p);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool onCounter = false, bool distanceBit = false, bool distanceMSB = false, bool highlighted = false)
        {
            this.onCounter.SetValue(onCounter);
            this.distanceBit.SetValue(distanceBit);
            this.distanceMSB.SetValue(distanceMSB);
            this.highlighted.SetValue(highlighted);
            if (highlighted)
                SetMainColor(ColorData.Particle_Blue);
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
            switch (round.GetValue())
            {
                case 0:
                    {
                        if (onCounter)
                            segShift.Init(highlighted, Direction.W, 0, 1, 2, HasNeighborAt(Direction.W) ? Direction.W : Direction.NONE, HasNeighborAt(Direction.E) ? Direction.E : Direction.NONE, distanceBit, distanceMSB);
                        else
                            segShift.Init(highlighted, Direction.W, 0, 1, 2);

                        PinConfiguration pc = GetContractedPinConfiguration();
                        segShift.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        segShift.ActivateSend();

                        round.SetValue(1);
                    }
                    break;
                case 1:
                    {
                        segShift.ActivateReceive();

                        PinConfiguration pc = GetContractedPinConfiguration();
                        segShift.SetupPC(pc);
                        SetPlannedPinConfiguration(pc);
                        segShift.ActivateSend();
                    }
                    break;
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SegmentShiftTestInitializer : InitializationMethod
    {
        public SegmentShiftTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int distance = 5, int numAmoebots = 50, float holeProb = 0.1f, bool fillHoles = false)
        {
            string distString = IntToBinary(distance);
            foreach (Vector2Int p in GenerateRandomConnectedPositions(Vector2Int.zero, numAmoebots, holeProb, fillHoles))
                AddParticle(p);

            // Add the counter
            int xmin = 0;
            while (true)
            {
                if (!TryGetParticleAt(new Vector2Int(xmin - 1, 0), out _))
                    break;
                xmin--;
            }
            for (int i = 0; i < distString.Length; i++)
            {
                Vector2Int pos = new Vector2Int(xmin + i, 0);
                InitializationParticle p;
                if (!TryGetParticleAt(pos, out p))
                {
                    p = AddParticle(pos);
                }
                else
                {
                    p.SetAttribute("onCounter", true);
                    p.SetAttribute("distanceBit", distString[i] == '1');
                    p.SetAttribute("distanceMSB", i == distString.Length - 1);
                }
            }
        }

        private string IntToBinary(int num)
        {
            if (num == 0)
                return "0";

            string s = "";
            while (num > 0)
            {
                s += (num & 1) > 0 ? '1' : '0';
                num >>= 1;
            }

            return s;
        }
    }

} // namespace AS2.Algos.SegmentShiftTest
