using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.BeepFailureTest
{

    /// <summary>
    /// Simple algorithm to test beep failure feature.
    /// <para>
    /// All particles construct circuits along all six cardinal directions.
    /// The boundary particles send beeps in the opposite directions of their
    /// empty neighbors. All particles expect beeps on all of their
    /// partition sets. If one beep does not arrive, they change their
    /// color to red for the next round.
    /// </para>
    /// </summary>
    public class BeepFailureTestParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Beep Failure Test";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 2;

        // If the algorithm has a special generation method, specify its full name here
        //public static new string GenerationMethod => typeof(BeepFailureTestInitializer).FullName;

        // Declare attributes here
        ParticleAttribute<bool> firstRound;

        public BeepFailureTestParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            firstRound = CreateAttributeBool("First Round", true);

            // Also, set the default initial color
            SetMainColor(Color.gray);
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
                // Build pin configuration
                PinConfiguration pc = GetNextPinConfiguration();
                // Partition set i constructs circuit on which
                // beeps are sent in direction i
                for (int i = 0; i < 6; i++)
                {
                    Direction d = DirectionHelpers.Cardinal(i);
                    if (HasNeighborAt(d) && HasNeighborAt(d.Opposite()))
                    {
                        // Neighbors on both sides: Connect them
                        pc.MakePartitionSet(new int[] {
                            pc.GetPinAt(d, 0).Id,
                            pc.GetPinAt(d.Opposite(), 1).Id
                        }, i);
                        pc.SetPartitionSetPosition(i, new Vector2(i * 60f - 16f, 0.7f));
                    }
                    else if (!HasNeighborAt(d.Opposite()))
                    {
                        // Have no neighbor in opposite direction:
                        // Expect beep on the pin where we send it
                        pc.MakePartitionSet(new int[] {
                            pc.GetPinAt(d, 0).Id
                        }, i);
                    }
                    else
                    {
                        // Have no neighbor in direction d but have
                        // the opposite neighbor: Expect to receive beep
                        pc.MakePartitionSet(new int[] {
                            pc.GetPinAt(d.Opposite(), 1).Id
                        }, i);
                        //pc.SetPartitionSetDrawHandle(i, true);
                        //pc.SetPartitionSetPosition(i, new Vector2(((i + 3) % 6) * 60f + 16f, 0.7f));
                    }
                }
                firstRound.SetValue(false);
            }
            else
            {
                // Listen for beeps on partition sets
                // If one beep was not received, change color to red
                bool receivedBeeps = true;
                for (int i = 0; i < 6; i++)
                    if (!ReceivedBeepOnPartitionSet(i))
                    {
                        receivedBeeps = false;
                        break;
                    }
                if (!receivedBeeps)
                    SetMainColor(ColorData.Particle_Red);
                else
                    SetMainColor(Color.gray);
            }
            
            // Boundary particles send beeps in the opposite direction of the empty neighbors
            for (int i = 0; i < 6; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                if (!HasNeighborAt(d.Opposite()))
                    SendBeepOnPartitionSet(i);
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    //public class BeepFailureTestInitializer : InitializationMethod
    //{
    //    public BeepFailureTestInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

    //    // This method implements the system generation
    //    // Its parameters will be shown in the UI and they must have default values
    //    public void Generate(/* Parameters with default values */)
    //    {
    //        // The parameters of the Init() method can be set as particle attributes here
    //    }
    //}

} // namespace AS2.Algos.BeepFailureTest
