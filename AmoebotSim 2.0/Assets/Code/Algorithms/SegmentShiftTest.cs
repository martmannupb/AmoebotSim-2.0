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
        ParticleAttribute<Direction> shiftDir;

        SubSegmentShift segShift;

        public SegmentShiftTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            round = CreateAttributeInt("Round", 0);
            onCounter = CreateAttributeBool("On Counter", false);
            distanceBit = CreateAttributeBool("Dist bit", false);
            distanceMSB = CreateAttributeBool("Dist MSB", false);
            highlighted = CreateAttributeBool("Highlighted", false);
            shiftDir = CreateAttributeDirection("Shift Dir", Direction.W);

            segShift = new SubSegmentShift(p);

            // Also, set the default initial color
            SetMainColor(ColorData.Particle_Black);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool onCounter = false, bool distanceBit = false, bool distanceMSB = false, bool highlighted = false,
            Direction shiftDir = Direction.W)
        {
            this.onCounter.SetValue(onCounter);
            this.distanceBit.SetValue(distanceBit);
            this.distanceMSB.SetValue(distanceMSB);
            this.highlighted.SetValue(highlighted);
            this.shiftDir.SetValue(shiftDir);
            if (highlighted)
                SetMainColor(ColorData.Particle_Blue);
        }

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
                        if (onCounter)
                            segShift.Init(highlighted, shiftDir, 0, 1, 2, HasNeighborAt(Direction.W) && ((SegmentShiftTestParticle)GetNeighborAt(Direction.W)).onCounter ? Direction.W : Direction.NONE, HasNeighborAt(Direction.E) ? Direction.E : Direction.NONE, distanceBit, distanceMSB);
                        else
                            segShift.Init(highlighted, shiftDir, 0, 1, 2);

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

                        if (segShift.IsFinished())
                        {
                            if (segShift.IsOnNewSegment())
                                SetMainColor(ColorData.Particle_Green);
                            else
                                SetMainColor(ColorData.Particle_Black);
                            round.SetValue(round + 1);
                            SetPlannedPinConfiguration(GetContractedPinConfiguration());
                            break;
                        }

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
        public void Generate(int distance = 5, Direction shiftDirection = Direction.W, int numMiddleSegments = 1, int numAmoebots = 150, float holeProb = 0.05f, bool fillHoles = false)
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
                p.SetAttribute("onCounter", true);
                p.SetAttribute("distanceBit", distString[i] == '1');
                p.SetAttribute("distanceMSB", i == distString.Length - 1);
            }

            // Add segments

            int firstSegment = Random.Range(0, distance);
            int lastSegment = Random.Range(0, distance);
            int[] seps = new int[numMiddleSegments + 1];
            int[] middleSegments = new int[numMiddleSegments];
            for (int i = 0; i <= numMiddleSegments; i++)
            {
                seps[i] = Random.Range(2, distance);
                if (i < numMiddleSegments)
                    middleSegments[i] = Random.Range(distance - 1, 2 * distance);
            }
            // Find the start and end coordinates of the segments
            int[] starts = new int[numMiddleSegments + 2];
            int[] ends = new int[numMiddleSegments + 2];
            starts[0] = 0;
            ends[0] = firstSegment;
            for (int i = 1; i < numMiddleSegments + 1; i++)
            {
                starts[i] = ends[i - 1] + seps[i - 1];
                ends[i] = starts[i] + middleSegments[i - 1];
            }
            starts[numMiddleSegments + 1] = ends[numMiddleSegments] + seps[numMiddleSegments];
            ends[numMiddleSegments + 1] = starts[numMiddleSegments + 1] + lastSegment;

            //Log.Debug("First segment: " + firstSegment);
            //Log.Debug("Middle segments:");
            //for (int i = 0; i < numMiddleSegments; i++)
            //{
            //    Log.Debug("Sep " + seps[i] + ", segment " + middleSegments[i]);
            //}
            //Log.Debug("Sep " + seps[numMiddleSegments] + ", last segment: " + lastSegment);

            int l = ends[numMiddleSegments + 1];

            Vector2Int inc = -AmoebotFunctions.unitVectors[shiftDirection.ToInt()];
            for (int x = 0; x <= l; x++)
            {
                Vector2Int pos = inc * x;
                InitializationParticle p;
                if (!TryGetParticleAt(pos, out p))
                {
                    p = AddParticle(pos);
                }
                bool highlighted = false;
                for (int i = 0; i < numMiddleSegments + 2; i++)
                {
                    if (x >= starts[i] && x <= ends[i])
                    {
                        highlighted = true;
                        break;
                    }
                }
                if (highlighted)
                    p.SetAttribute("highlighted", true);
            }

            foreach (InitializationParticle p in GetParticles())
                p.SetAttribute("shiftDir", shiftDirection);
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
