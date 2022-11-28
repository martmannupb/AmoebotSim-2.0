using UnityEngine;

public class WSDirectionMessage : Message
{
    public Direction dir;

    public WSDirectionMessage(Direction d)
    {
        dir = d;
    }

    public override Message Copy()
    {
        return new WSDirectionMessage(dir);
    }

    public override bool Equals(Message other)
    {
        return other != null && other.GetType().Equals(this.GetType()) && ((WSDirectionMessage)other).dir == dir;
    }

    public override bool GreaterThan(Message other)
    {
        return other != null && other.GetType().Equals(this.GetType()) && ((WSDirectionMessage)other).dir.ToInt() < dir.ToInt();
    }
}

public class WorkshopSolutionBonusParticle : ParticleAlgorithm
{
    // Specify the number of pins
    public override int PinsPerEdge => 1;

    // This is the display name of the algorithm (must be unique)
    public static new string Name => "Workshop Solution Bonus";

    // If the algorithm has a special generation method, specify its full name here
    public static new string GenerationMethod => typeof(WorkshopSolutionBonusParticleInitializer).FullName;

    // Declare attributes here
    private ParticleAttribute<bool> isLeader;
    private ParticleAttribute<float> movementProb;
    // ...

    public WorkshopSolutionBonusParticle(Particle p) : base(p)
    {
        // Initialize the attributes here
        isLeader = CreateAttributeBool("Leader", false);
        movementProb = CreateAttributeFloat("Movement prob.", 0.0f);

        // Also, set the default initial color
        SetMainColor(ColorData.Particle_Blue);
    }

    // Implement this if the particles require special initialization
    // The parameters will be converted to particle attributes for initialization
    public void Init(bool leader = false, float movementProb = 0.3f)
    {
        // This code is executed directly after the constructor
        if (leader)
        {
            isLeader.SetValue(true);
            this.movementProb.SetValue(movementProb);
            SetMainColor(ColorData.Particle_Yellow);
        }
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
        // Check for received beep or message
        PinConfiguration pc = GetCurrentPinConfiguration();

        if (IsContracted())
        {
            Message msg = pc.GetReceivedMessageOfPartitionSet(0);
            if (msg != null)
            {
                Direction moveDir = ((WSDirectionMessage)msg).dir;

                // Mark the East bond to drag the neighbor with us
                MarkBond(Direction.E);

                Expand(moveDir);
            }
        }
        else
        {
            bool rcvBeep = pc.ReceivedBeepOnPartitionSet(0);

            // Move if we received a beep
            if (rcvBeep)
            {
                // Decide which bonds must be released based on expansion direction
                // Only have to release bonds in directions going West
                Direction expDir = HeadDirection();
                if (expDir == Direction.NNW)
                {
                    ReleaseBond(Direction.NNE, false);
                    ReleaseBond(Direction.NNE, true);
                    ReleaseBond(Direction.SSW, false);
                    ReleaseBond(Direction.SSW, true);
                }
                else if (expDir == Direction.SSW)
                {
                    ReleaseBond(Direction.NNW, false);
                    ReleaseBond(Direction.NNW, true);
                    ReleaseBond(Direction.SSE, false);
                    ReleaseBond(Direction.SSE, true);
                }
                ContractTail();
            }
        }
    }

    // The beep activation method
    public override void ActivateBeep()
    {
        // Establish global circuit
        PinConfiguration pc = GetCurrentPinConfiguration();
        pc.SetToGlobal(0);
        SetPlannedPinConfiguration(pc);

        // Leader beeps with given probability
        if (isLeader && Random.Range(0f, 1f) < movementProb)
        {
            if (IsContracted())
            {
                // Decide what kind of movement to make
                int movement = Random.Range(0, 5);
                Direction moveDir = new Direction[] { Direction.NNW, Direction.NNE, Direction.E, Direction.SSE, Direction.SSW }[movement];
                Log.Debug("Move dir: " + moveDir);
                WSDirectionMessage msg = new WSDirectionMessage(moveDir);
                pc.SendMessageOnPartitionSet(0, msg);
            }
            else
            {
                // Otherwise, just send a beep
                pc.SendBeepOnPartitionSet(0);
            }
        }
    }
}

// Use this to implement a generation method for this algorithm
// Its class name must be specified as the algorithm's GenerationMethod
public class WorkshopSolutionBonusParticleInitializer : InitializationMethod
{
    public WorkshopSolutionBonusParticleInitializer(ParticleSystem system) : base(system) { }

    // This method implements the system generation
    // Its parameters will be shown in the UI and they must have default values
    public void Generate(int numParticles = 10, float movementProb = 0.3f)
    {
        // Place a line of n particles in East direction
        PlaceParallelogram(Vector2Int.zero, Direction.E, numParticles);

        // Select a random leader
        InitializationParticle[] particles = GetParticles();
        if (particles.Length > 0)
        {
            InitializationParticle leader = particles[Random.Range(0, particles.Length)];
            leader.SetAttribute("leader", true);
            leader.SetAttribute("movementProb", movementProb);
        }
    }
}
