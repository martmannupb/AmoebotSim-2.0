using System.Collections.Generic;
using UnityEngine;


// TODO: Move simulation logic into a separate class and use this only as container for particles?

/// <summary>
/// Main container for a system of particles in the grid together with
/// the execution logic for simulating rounds.
/// <para>
/// Unlike other <see cref="IReplayHistory"/> implementations, the system will
/// automatically reactivate the tracking state as soon as the marker is set to
/// the latest round.
/// </para>
/// </summary>
public class ParticleSystem : IReplayHistory
{
    // Round indexing
    private int _currentRound = 0;
    private int _previousRound = 0;
    private int _earliestRound = 0;
    private int _latestRound = 0;

    /// <summary>
    /// The current round describing the simulation state.
    /// <para>
    /// Before the first round is simulated, the current round is <c>0</c>.
    /// After each simulated round, this counter is incremented by <c>1</c>.
    /// </para>
    /// </summary>
    public int CurrentRound
    {
        get { return _currentRound; }
        private set { _currentRound = value; }
    }

    /// <summary>
    /// The round that was previously simulated.
    /// <para>
    /// While a simulation round is being computed and the particles are
    /// being activated, this value will be <see cref="CurrentRound"/><c> - 1</c>.
    /// Inbetween rounds, <see cref="CurrentRound"/> and <see cref="PreviousRound"/>
    /// will have the same value.
    /// </para>
    /// </summary>
    public int PreviousRound
    {
        get { return _previousRound; }
        private set { _previousRound = value; }
    }

    // TODO: Maybe earliest round can change when loading a (partial) history or doing something else that changes the rounds?
    /// <summary>
    /// The round of the initial configuration before the first simulation round.
    /// <para>
    /// This is assumed to be always <c>0</c>.
    /// </para>
    /// </summary>
    public int EarliestRound
    {
        get { return _earliestRound; }
        private set { _earliestRound = value; }
    }

    /// <summary>
    /// The latest round that has been reached through simulation.
    /// <para>
    /// Any simulation state between round <c>0</c> and this round
    /// can be restored at any time without having to simulate the
    /// particles. To proceed to later rounds, the simulation must
    /// be continued from this round.
    /// </para>
    /// </summary>
    public int LatestRound
    {
        get { return _latestRound; }
        private set { _latestRound = value; }
    }

    private bool isTracking = true;     // For IReplayHistory

    // State information

    /// <summary>
    /// Indicates whether the system is currently in the initialization state.
    /// In this state, particles can be added, removed, moved and modified
    /// freely and new configurations can be generated.
    /// <para>
    /// In this state, no methods controlling the simulation or the history
    /// replay should be called.
    /// </para>
    /// </summary>
    public bool InInitializationState
    {
        get { return inInitializationState; }
    }
    private bool inInitializationState = false;

    // Is true when we entered initialization mode and saved the previous simulation state
    private bool storedSimulationState = false;
    // The round we were in when we stored the simulation state
    private int storedSimulationRound = -1;
    // The number of generic parameters stored in the initialization particles
    private int numGenericParameters = 0;
    /// <summary>
    /// The number of generic parameters used by the particle templates in
    /// initialization mode.
    /// </summary>
    public int NumGenericParameters
    {
        get { return numGenericParameters; }
    }

    // Simulation phase flags
    // Used to indicate which of the two cycles is currently being executed
    // so that all parts of the simulator have the corresponding behavior
    private bool inMovePhase = false;
    private bool inBeepPhase = false;

    /// <summary>
    /// <c>true</c> while the look-compute-move cycle of a round is being simulated.
    /// </summary>
    public bool InMovePhase
    {
        get { return inMovePhase; }
    }

    /// <summary>
    /// <c>true</c> while the look-compute-beep cycle of a round is being simulated.
    /// </summary>
    public bool InBeepPhase
    {
        get { return inBeepPhase; }
    }

    private bool finished = false;
    /// <summary>
    /// <c>true</c> if the simulation has reached a round after which all
    /// particles in the system were finished, even if this round lies later
    /// in the history than the currently marked round.
    /// </summary>
    public bool Finished
    {
        get { return finished; }
    }
    // The first round in which all particles were finished
    private int finishedRound = -1;

    // References
    public AmoebotSimulator sim;
    public RenderSystem renderSystem;

    // Main data structures
    public List<Particle> particles = new List<Particle>();
    private Dictionary<Vector2Int, Particle> particleMap = new Dictionary<Vector2Int, Particle>();

    // Initialization mode data structures
    private List<OpenInitParticle> particlesInit = new List<OpenInitParticle>();
    private Dictionary<Vector2Int, OpenInitParticle> particleMapInit = new Dictionary<Vector2Int, OpenInitParticle>();

    private float totalCircuitCompTime = 0f;

    public ParticleSystem(AmoebotSimulator sim, RenderSystem renderSystem)
    {
        this.sim = sim;
        this.renderSystem = renderSystem;
    }


    /**
     * Methods for controlling the system
     */

    #region AlgorithmInitialization

    public void InitializeExpandedTest(int numParticles)
    {
        // Arrange the particles in a line
        for (int i = 0; i < numParticles; i++)
        {
            Particle p = ParticleFactory.CreateExpandedTestParticle(this, new Vector2Int(0, i));
            particles.Add(p);
            particleMap.Add(p.Head(), p);
        }

        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    /// <summary>
    /// Initializes the system with connected LineFormationParticles for testing purposes.
    /// This should be removed after a proper initialization method has been created.
    /// </summary>
    /// <param name="numParticles">The number of particles to create.</param>
    /// <param name="holeProb">The probability of a position not being occupied by
    /// a particle and being left empty instead.</param>
    public void InitializeLineFormation(int numParticles, float holeProb)
    {
        // Determine which particle will be the leader
        int leaderIdx = Random.Range(0, Mathf.Max(1, numParticles));

        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        //Particle p = ParticleFactory.CreateLineFormationParticleSeq(this, node);
        //Particle p = ParticleFactory.CreateLineFormationParticleSync(this, node);
        Particle p = ParticleFactory.CreateParticle(this, "Line Formation", new List<int>() { leaderIdx == 0 ? 1 : 0 }, node);
        particles.Add(p);
        particleMap.Add(p.Head(), p);

        for (int d = 0; d < 6; d++)
            candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        occupied.Add(node);

        while (n < numParticles && candidates.Count > 0)
        {
            int randIdx = Random.Range(0, candidates.Count);
            Vector2Int newPos = candidates[randIdx];
            candidates.RemoveAt(randIdx);

            // Either use newPos to insert particle or to insert hole
            if (Random.Range(0.0f, 1.0f) >= holeProb)
            {
                for (int d = 0; d < 6; d++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                    if (!occupied.Contains(nbr) && !candidates.Contains(nbr))
                        candidates.Add(nbr);
                }

                //p = ParticleFactory.CreateLineFormationParticleSeq(this, newPos);
                //p = ParticleFactory.CreateLineFormationParticleSync(this, newPos);
                p = ParticleFactory.CreateParticle(this, "Line Formation", new List<int>() { leaderIdx == n ? 1 : 0 }, newPos);
                particles.Add(p);
                particleMap.Add(p.Head(), p);

                n++;
            }

            occupied.Add(newPos);
        }
        string s = "Created system with " + n + " particles:\n";
        foreach (Particle part in particles)
        {
            s += part.Head() + "\n";
        }
        Debug.Log(s);

        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    public void InitializeLeaderElection(int numParticles, float holeProb)
    {
        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        Particle p = ParticleFactory.CreateLeaderElectionParticle(this, node);
        particles.Add(p);
        particleMap.Add(p.Head(), p);

        for (int d = 0; d < 6; d++)
            candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        occupied.Add(node);

        while (n < numParticles && candidates.Count > 0)
        {
            int randIdx = Random.Range(0, candidates.Count);
            Vector2Int newPos = candidates[randIdx];
            candidates.RemoveAt(randIdx);

            // Either use newPos to insert particle or to insert hole
            if (Random.Range(0.0f, 1.0f) >= holeProb)
            {
                for (int d = 0; d < 6; d++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                    if (!occupied.Contains(nbr) && !candidates.Contains(nbr))
                        candidates.Add(nbr);
                }

                p = ParticleFactory.CreateLeaderElectionParticle(this, newPos);
                particles.Add(p);
                particleMap.Add(p.Head(), p);

                n++;
            }

            occupied.Add(newPos);
        }
        string s = "Created system with " + n + " particles:\n";
        foreach (Particle part in particles)
        {
            s += part.Head() + "\n";
        }
        Debug.Log(s);

        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    public void InitializeChiralityCompass(int numParticles, float holeProb)
    {
        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        Particle p = ParticleFactory.CreateChiralityAndCompassParticle(this, node);
        particles.Add(p);
        particleMap.Add(p.Head(), p);

        for (int d = 0; d < 6; d++)
            candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        occupied.Add(node);

        while (n < numParticles && candidates.Count > 0)
        {
            int randIdx = Random.Range(0, candidates.Count);
            Vector2Int newPos = candidates[randIdx];
            candidates.RemoveAt(randIdx);

            // Either use newPos to insert particle or to insert hole
            if (Random.Range(0.0f, 1.0f) >= holeProb)
            {
                for (int d = 0; d < 6; d++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                    if (!occupied.Contains(nbr) && !candidates.Contains(nbr))
                        candidates.Add(nbr);
                }

                p = ParticleFactory.CreateChiralityAndCompassParticle(this, newPos);
                particles.Add(p);
                particleMap.Add(p.Head(), p);

                n++;
            }

            occupied.Add(newPos);
        }
        string s = "Created system with " + n + " particles:\n";
        foreach (Particle part in particles)
        {
            s += part.Head() + "\n";
        }
        Debug.Log(s);

        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    public void InitializeBoundaryTest(int numParticles, float holeProb)
    {
        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        Particle p = ParticleFactory.CreateBoundaryTestParticle(this, node);
        particles.Add(p);
        particleMap.Add(p.Head(), p);

        for (int d = 0; d < 6; d++)
            candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        occupied.Add(node);

        while (n < numParticles && candidates.Count > 0)
        {
            int randIdx = Random.Range(0, candidates.Count);
            Vector2Int newPos = candidates[randIdx];
            candidates.RemoveAt(randIdx);

            // Either use newPos to insert particle or to insert hole
            if (Random.Range(0.0f, 1.0f) >= holeProb)
            {
                for (int d = 0; d < 6; d++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                    if (!occupied.Contains(nbr) && !candidates.Contains(nbr))
                        candidates.Add(nbr);
                }

                p = ParticleFactory.CreateBoundaryTestParticle(this, newPos);
                particles.Add(p);
                particleMap.Add(p.Head(), p);

                n++;
            }

            occupied.Add(newPos);
        }
        string s = "Created system with " + n + " particles:\n";
        foreach (Particle part in particles)
        {
            s += part.Head() + "\n";
        }
        Debug.Log(s);

        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    public void InitializeJMTest(int mode)
    {
        Particle p;

        if (mode == 0)
        {
            // A block of particles that expands East while being sheared
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(x, y), mode, 0, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                }
            }
        }
        else if (mode == 1)
        {
            // A block of particles that expands East without being sheared
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(x, y), mode, 0, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                }
            }
        }
        else if (mode == 2)
        {
            // A contracted and an expanded particle (will have two bonds)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            // Expanded
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);
        }
        else if (mode == 3)
        {
            // A contracted and an expanded particle (will have only one bond)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            // Expanded
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);
        }
        else if (mode == 4)
        {
            // A contracted and an expanded particle (will have only one bond and perform handover)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            // Expanded
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);
        }
        else if (mode == 5)
        {
            // A contracted and an expanded particle (will have two bonds and perform handover)
            // Can also swap the order of the two to make the expanded particle the seed

            // Contracted
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            // Expanded
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);
        }
        else if (mode == 6)
        {
            // A contracted and an expanded particle performing a handover with additional particles
            // being pulled or transferred

            // Contracted particle pushing
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            // Expanded particle pulling
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Four particles that are passive and will be moved around by the other particles
            int role = 2;
            foreach (Vector2Int pos in new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(-2, 2), new Vector2Int(-1, 2) }) {
                p = ParticleFactory.CreateJMTestParticle(this, pos, mode, role, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
                role++;
            }
        }
        else if (mode == 7)
        {
            // Two expanded particles with one bond, randomly moving or not

            // Bottom particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Top particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);
        }
        else if (mode == 8)
        {
            // Two expanded particles with one bond and contracted neighbors, randomly decide to mark bond or not

            // Bottom particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Top particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Contracted neighbors
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, -1), mode, 2, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 2), mode, 3, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
        }
        else if (mode == 9)
        {
            // Two expanded particles with two bonds sharing one end
            // Swap the particles to change the anchor

            // Left particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Right particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(2, 0), mode, 1, Direction.E, true, Direction.NNW);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);
        }
        else if (mode == 10)
        {
            // Same as mode 9 but with a contracted particle for handover
            // Swap the particles to change the anchor

            // Left particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Right particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(2, 0), mode, 1, Direction.E, true, Direction.NNW);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Contracted particle
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(1, 2), mode, 2, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
        }
        else if (mode == 11)
        {
            // Stack of parallel expanded particles with two parallel bonds
            // All of them contract but the direction is random

            for (int i = 0; i < 10; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, i), mode, 0, Direction.E, true, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
                particleMap.Add(p.Tail(), p);
            }
        }
        else if (mode == 12)
        {
            // Stack of parallel expanded particles with two parallel bonds
            // Some of them have a contracted neighbor for handover
            // The system either contracts or stays expanded
            // IMPORTANT: CASE WITH UNMARKED HANDOVERS DOES NOT WORK DUE TO
            // UPDATED MODEL, SYSTEM NEVER CONTRACTS

            //bool contract = Random.Range(0, 2) == 0;
            bool contract = false;

            for (int i = 0; i < 15; i++)
            {
                // Randomly either place just the expanded particle or an additional contracted particle on one side
                // 0 = none, 1 = left, 2 = right
                int neighbor = Random.Range(0, 3);

                // 0 = do nothing (no neighbor), 1 = contract in random direction (no neighbor),
                // 2 = handover head with marked bond, 3 = handover tail with marked bond,
                // 4 = handover head with unmarked bond, 5 = handover tail with unmarked bond
                int role = neighbor == 0 ? (contract ? 1 : 0) :
                    neighbor == 1 ? (contract ? 4 : 2) :
                    (contract ? 5 : 3);

                // Expanded particle
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, i), mode, role, Direction.E, true, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
                particleMap.Add(p.Tail(), p);

                // Place the neighbor
                if (neighbor == 1)
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, i), mode, 6, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                }
                else if (neighbor == 2)
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(2, i), mode, 7, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                }
            }
        }
        else if (mode == 13)
        {
            // Three lines of particles forming a parallelogram with one missing side
            // All of the particles expand and contract alternatingly
            int widthLower = 10;
            int widthUpper = 12;
            int height = 8;

            // Lower row
            for (int i = 0; i < widthLower; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(i, 0), mode, 0, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }

            // Vertical column
            for (int i = 0; i < height; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, i + 1), mode, 1, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }

            // Upper row
            for (int i = 0; i < widthUpper; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(i, height + 1), mode, 2, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }
        }
        else if (mode == 14)
        {
            // Contracted and expanded particle with 2 bonds
            // Expanded particle performs handover with another contracted neighbor

            // Contracted
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, 0), mode, 0, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            // Expanded
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-1, 1), mode, 1, Direction.E, true, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
            particleMap.Add(p.Tail(), p);

            // Contracted neighbor for handover
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(-2, 2), mode, 2, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);
        }
        else if (mode == 15)
        {
            // Block of particles with a stripe in the middle that expands and contracts
            // to move the entire block

            int width = 20;
            int height = 10;
            // Idle particles have role 0, stripe particles have role 1
            // The lowest stripe particle is the anchor
            p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(width / 2, 0), mode, 1, Direction.E);
            particles.Add(p);
            particleMap.Add(p.Head(), p);

            for (int x = 0; x < width; x++)
            {
                int role = x == width / 2 ? 1 : 0;
                for (int y = (role == 0 ? 0 : 1); y < height; y++)
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(x, y), mode, role, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                }
            }
        }
        else if (mode == 16)
        {
            // A "floor" made out of contracted particles and a "worm" of particles that moves
            // across it by expanding and contracting
            int floorSize = 250;
            int wormSize = 7;

            // Role 0 is floor, role 1 is worm
            // Anchor is part of the floor
            for (int i = 0; i < floorSize; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(i, 0), mode, 0, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }

            for (int i = 0; i < wormSize; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(i, 1), mode, 1, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }
        }
        else if (mode == 17)
        {
            // A parallelogram of particles where the bottom line consists of expanded and contracted
            // particles which alternatingly expand and contract

            int numSegments = 5;
            int height = 4;     // Must not be less than 3

            // Role 0 means static, role 1 means moving
            // Left and right sides
            for (int i = 0; i < height; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(0, i), mode, 0, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(3 * numSegments + 1, i), mode, 0, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }
            // Top line
            for (int i = 1; i < numSegments * 3 + 1; i++)
            {
                p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(i, height - 1), mode, 0, Direction.E);
                particles.Add(p);
                particleMap.Add(p.Head(), p);
            }

            // Bottom line
            int numExpanded = 0;
            int numContracted = 0;
            int x = 1;
            for (int i = 0; i < numSegments * 2; i++)
            {
                bool random = Random.Range(0, 2) == 0;
                bool expanded = numContracted == numSegments || (numExpanded < numSegments && random);
                if (expanded)
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(x, 0), mode, 1, Direction.E, true, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                    particleMap.Add(p.Tail(), p);
                    numExpanded++;
                    x += 2;
                }
                else
                {
                    p = ParticleFactory.CreateJMTestParticle(this, new Vector2Int(x, 0), mode, 1, Direction.E);
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                    numContracted++;
                    x++;
                }
            }
        }



        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    #endregion

    /// <summary>
    /// Resets the entire system to a state from which it can be
    /// initialized again.
    /// </summary>
    public void Reset()
    {
        totalCircuitCompTime = 0f;
        // Remove particles from the renderer system
        foreach (Particle p in particles)
        {
            p.graphics.RemoveParticle();
        }

        // Clear particle list and map
        particles.Clear();
        particleMap.Clear();

        // Reset history state
        _earliestRound = 0;
        _latestRound = 0;
        _currentRound = 0;
        _previousRound = 0;
        isTracking = true;

        finished = false;
        finishedRound = -1;
    }

    /// <summary>
    /// Resets the current particle configuration while in
    /// initialization mode.
    /// </summary>
    public void ResetInit()
    {
        foreach (InitializationParticle p in particlesInit)
        {
            p.graphics.RemoveParticle();
        }
        particlesInit.Clear();
        particleMapInit.Clear();
    }

    /// <summary>
    /// Tries to get the <see cref="Particle"/> at the given position.
    /// </summary>
    /// <param name="position">The grid position at which to look for the particle.</param>
    /// <param name="particle">The particle at the given position, if it exists,
    /// otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if and only if a particle was found at the given position.</returns>
    public bool TryGetParticleAt(Vector2Int position, out Particle particle)
    {
        if (particleMap.TryGetValue(position, out particle))
        {
            return true;
        }
        else
        {
            particle = null;
            return false;
        }
    }

    /// <summary>
    /// Tries to get the <see cref="IParticleState"/> at the given position.
    /// </summary>
    /// <param name="position">The grid position at which to look for the particle.</param>
    /// <param name="particle">The particle at the given position, if it exists,
    /// otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if and only if a particle was found at the given position.</returns>
    public bool TryGetParticleAt(Vector2Int position, out IParticleState particle)
    {
        if (inInitializationState)
        {
            if (particleMapInit.TryGetValue(position, out OpenInitParticle p))
            {
                particle = p;
                return true;
            }
        }
        else
        {
            if (TryGetParticleAt(position, out Particle p))
            {
                particle = p;
                return true;
            }
        }

        particle = null;
        return false;
    }


    /**
     * Simulation functions
     */

    #region Simulation

    /// <summary>
    /// Simulates a round in which each particle is activated
    /// twice. First, all particles execute their movement activations
    /// and the resulting movements are simulated and applied. Afterwards,
    /// the particles' beep activations are executed, the resulting circuits
    /// are determined and the sent beeps and messages are delivered.
    /// The order of activations is the same in
    /// both rounds as long as no particles are added to or
    /// removed from the system.
    /// </summary>
    public void SimulateRound()
    {
        if (!isTracking)
        {
            Log.Error("Simulation cannot proceed while not in tracking state.");
            return;
        }

        _currentRound++;
        Debug.Log("Simulate round " + _currentRound + " (previous round: " + _previousRound + ")");
        _latestRound++;

        // First: Move cycle
        // All particles set their new bonds and schedule movements
        // Then the movements are evaluated and applied
        inMovePhase = true;
        bool particlesMove = false;
        try
        {
            particlesMove = ActivateParticlesMove();
        }
        catch (ParticleException pe)
        {
            Log.Error("Exception caught during movement activations: " + pe + "\n" + pe.StackTrace + "\nParticle: " +
                    (pe.particle == null ? "NULL" : (pe.particle.Head() + ", " + pe.particle.Tail())));
            ResetAfterException();
            return;
        }
        inMovePhase = false;

        // Skip the movement computation if no particle has moved
        // TODO: No connectivity check is performed then! Should this always be executed?
        try
        {
            if (particlesMove)
                SimulateJointMovements();
            else
                // Compute bond information anyway
                ComputeBondsStatic();
            FinishMovementInfo();
        }
        catch (AmoebotSimException ase)
        {
            Log.Error("Exception caught during movement simulation: " + ase + "\n" + ase.StackTrace);
            ResetAfterException();
            return;
        }

        // Second: Beep cycle
        // All particles set their new pin configurations and send beeps and messages
        // Then the circuits are computed and the sent information is distributed
        inBeepPhase = true;
        try
        {
            ActivateParticlesBeep();
        }
        catch (ParticleException pe)
        {
            Log.Error("Exception caught during beep activations: " + pe + "\n" + pe.StackTrace + "\nParticle: " +
                    (pe.particle == null ? "NULL" : (pe.particle.Head() + ", " + pe.particle.Tail())));
            ResetAfterException();
            return;
        }

        inBeepPhase = false;
        ApplyNewPinConfigurations();
        DiscoverCircuits(true);
        FinishBeepAndMessageInfo();
        _previousRound++;
        UpdateAllParticleVisuals(false);
        CleanupAfterRound();

        Log.Debug("Average circuit computation time: " + totalCircuitCompTime / _currentRound);

        // Check if the simulation is over
        if (!finished && HasSimulationFinished())
        {
            // TODO: Stop the simulation (?)
            finished = true;
            finishedRound = _currentRound;
            Log.Debug("Simulation finished.");
        }
    }

    private void ResetAfterException()
    {
        inMovePhase = false;
        inBeepPhase = false;
        CleanupAfterRound();
        SetMarkerToRound(_currentRound - 1);
        CutOffAtMarker();
    }

    /// <summary>
    /// Executes the movement activations of all particles in the system and
    /// prepares the particles for the joint movement computation.
    /// <para>
    /// The final bonds of each particle are computed by their global positions
    /// and adjusted based on the scheduled movement. Additionally, it is
    /// determined whether the head of the particle is its local origin and
    /// the local movement offset is calculated. The global offset of the
    /// particles is reset to <c>(0, 0)</c>.
    /// </para>
    /// </summary>
    /// <returns><c>true</c> if any particle has scheduled a movement.</returns>
    private bool ActivateParticlesMove()
    {
        bool anyParticleMoved = false;
        for (int i = 0; i < particles.Count; i++)
        {
            Particle p = particles[i];

            try
            {
                p.ActivateMove();
            }
            catch (AmoebotSimException ase)
            {
                // Keep stack trace
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ase).Throw();
            }
            catch (System.Exception e)
            {
                Log.Error("Caught non-AmoebotSim exception in movement activation");
                throw new AlgorithmException(p, e.ToString() + "\n" + e.StackTrace);
            }

            ParticleAction a = p.ScheduledMovement;
            // Determine the local origin
            p.isHeadOrigin = p.IsContracted() && a == null || p.IsExpanded() && a != null && (a.type == ActionType.CONTRACT_HEAD || a.type == ActionType.PULL_HEAD);

            // Compute bonds by global labels
            int numLabels = p.IsExpanded() ? 10 : 6;
            if (p.markedForAutomaticBonds)
            {
                for (int label = 0; label < numLabels; label++)
                {
                    // Default (e.g. no action): All bonds are active, none are marked
                    bool active = true;
                    bool marked = false;
                    if (a != null)
                    {
                        Direction gDir = ParticleSystem_Utils.LocalToGlobalDir(a.localDir, p.comDir, p.chirality);
                        int dirLabelHead = gDir != Direction.NONE ? ParticleSystem_Utils.GetLabelInDir(gDir, p.GlobalHeadDirection(), true) : -1;
                        bool isHeadLabel = ParticleSystem_Utils.IsHeadLabel(label, p.GlobalHeadDirection());

                        // Expansion and Push: Leading bond must be marked
                        // We log a warning if the target node is occupied in a normal expansion
                        if (a.type == ActionType.EXPAND || a.type == ActionType.PUSH)
                        {
                            if (label == dirLabelHead)
                            {
                                marked = true;
                                if (a.type == ActionType.EXPAND)
                                {
                                    Vector2Int targetPos = ParticleSystem_Utils.GetNbrInDir(p.Head(), gDir);
                                    if (particleMap.ContainsKey(targetPos))
                                    {
                                        Debug.LogWarning("Particle at position " + p.Head() +
                                            " has switched to automatic bonds but expands into neighbor in global direction " + gDir);
                                    }
                                }
                            }
                        }
                        // Contraction: Release bond if it is not at the origin
                        else if (a.type == ActionType.CONTRACT_HEAD || a.type == ActionType.CONTRACT_TAIL)
                        {
                            if (isHeadLabel ^ p.isHeadOrigin)
                                active = false;
                        }
                        // Pull handover: Mark all non-origin bonds
                        else if (a.type == ActionType.PULL_HEAD || a.type == ActionType.PULL_TAIL)
                        {
                            if (isHeadLabel ^ p.isHeadOrigin)
                                marked = true;
                        }
                    }

                    p.activeBondsGlobal[label] = active;
                    p.markedBondsGlobal[label] = marked;
                }
            }
            else
            {
                // Bonds are not automatic: Use bonds set by algorithm
                for (int label = 0; label < numLabels; label++)
                {
                    int localLabel = ParticleSystem_Utils.GlobalToLocalLabel(label, p.GlobalHeadDirection(), p.comDir, p.chirality);
                    p.activeBondsGlobal[label] = p.BondActive(localLabel);
                    p.markedBondsGlobal[label] = p.BondMarked(localLabel);
                }
            }

            p.movementOffset = Vector2Int.zero;
            p.jmOffset = Vector2Int.zero;

            // Apply automatic bond restrictions
            if (a == null) continue;

            anyParticleMoved = true;

            // For expanding particles: Bond in expansion direction is always marked, opposite bond is never marked
            if (a.type == ActionType.EXPAND)
            {
                Direction d = ParticleSystem_Utils.LocalToGlobalDir(a.localDir, p.comDir, p.chirality);
                p.markedBondsGlobal[ParticleSystem_Utils.GetLabelInDir(d)] = true;
                p.markedBondsGlobal[ParticleSystem_Utils.GetLabelInDir(d.Opposite())] = false;
            }
            // For pushing particles in handover: No bond except the one in movement direction is marked
            else if (a.type == ActionType.PUSH)
            {
                Direction d = ParticleSystem_Utils.LocalToGlobalDir(a.localDir, p.comDir, p.chirality);
                int moveDirLabel = ParticleSystem_Utils.GetLabelInDir(d);

                for (int l = 0; l < 6; l++)
                {
                    p.markedBondsGlobal[l] = l == moveDirLabel;
                }
            }
            // For pulling particles in handover: Bonds at origin (non-moving part) cannot be marked,
            // other bonds must be marked
            else if (a.IsHandoverContraction())
            {
                Direction globalHeadDir = p.GlobalHeadDirection();
                for (int l = 0; l < 10; l++)
                {
                    p.markedBondsGlobal[l] = ParticleSystem_Utils.IsHeadLabel(l, globalHeadDir) ^ p.isHeadOrigin;
                }
            }

            // Compute movement offset
            Direction offsetDir = Direction.NONE;
            if (a.IsExpansion())
            {
                offsetDir = ParticleSystem_Utils.LocalToGlobalDir(a.localDir, p.comDir, p.chirality);
            }
            else if (a.type == ActionType.CONTRACT_HEAD || a.type == ActionType.PULL_HEAD)
            {
                offsetDir = p.GlobalHeadDirection();
            }
            else if (a.type == ActionType.CONTRACT_TAIL || a.type == ActionType.PULL_TAIL)
            {
                offsetDir = p.GlobalTailDirection();
            }
            p.movementOffset = ParticleSystem_Utils.DirectionToVector(offsetDir);
        }

        return anyParticleMoved;
    }

    /// <summary>
    /// Executes the beep activation of each particle.
    /// </summary>
    private void ActivateParticlesBeep()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            try
            {
                particles[i].ActivateBeep();
            }
            catch (AmoebotSimException ase)
            {
                // Keep stack trace
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ase).Throw();
            }
            catch (System.Exception e)
            {
                Log.Error("Caught non-AmoebotSim exception in beep activation");
                throw new AlgorithmException(particles[i], e.ToString() + "\n" + e.StackTrace);
            }
        }
    }

    /// <summary>
    /// Updates the pin configuration for each particle.
    /// <para>
    /// If a particle has moved, it must have set a new
    /// pin configuration, otherwise it will be reset
    /// to a singleton pattern.
    /// </para>
    /// </summary>
    public void ApplyNewPinConfigurations()
    {
        foreach (Particle p in particles)
        {
            p.ApplyPlannedPinConfiguration();
        }
    }

    /// <summary>
    /// Computes the joint movements of the particle system after
    /// all movement activations were executed. Each particle gets
    /// a global offset that is applied in addition to its own local
    /// movement.
    /// <para>
    /// Performs a breadth-first search on the graph induced by the
    /// particles' bonds, starting at the anchor particle. For each
    /// visited particle, offsets for all of its unvisited neighbors
    /// are computed based on the particle's own offset. The computation
    /// fails if two neighboring particles try to perform conflicting
    /// movements or any two particles end up at the same final
    /// location.
    /// </para>
    /// </summary>
    private void SimulateJointMovements()
    {
        float tStart = Time.realtimeSinceStartup;

        // New particle positions will be stored in new map
        Dictionary<Vector2Int, Particle> newPositions = new Dictionary<Vector2Int, Particle>(particles.Count);

        // Use BFS
        Queue<Particle> queue = new Queue<Particle>();

        // Start with the anchor particle
        Particle anchor = particles[0];
        anchor.jmOffset = Vector2Int.zero;

        queue.Enqueue(anchor);
        anchor.queuedForJMProcessing = true;

        // A particle's processing is finished when its final location in the new configuration has
        // been determined and its movement has been validated against its bonded neighbors.
        // Every particle added to the queue already has an offset vector and may have been validated
        // against some of its neighbors.
        // To complete a particle's processing, the remaining neighbors must be validated. For those
        // neighbors that have already been added to the queue, the offset vectors must be compared.

        while (queue.Count > 0)
        {
            Particle p = queue.Dequeue();

            Direction globalHeadDir = p.GlobalHeadDirection();
            ParticleAction movementAction = p.ScheduledMovement;

            // Find the bonded neighbors of the particle
            int numNbrs = p.IsExpanded() ? 10 : 6;
            Particle[] nbrParts = new Particle[numNbrs];
            bool[] nbrHead = new bool[numNbrs]; // True if the neighbor's head is at this position
            int[] nbrLabels = new int[numNbrs]; // Stores global neighbor label opposite of our label
            bool[] bondNbrs = new bool[numNbrs];    // True wherever we have a bonded neighbor

            // If a handover is scheduled, also ensure that there is a bond to the
            // partner in the handover
            int handoverLabelToCheck = -1;
            if (movementAction != null)
            {
                Direction dir = ParticleSystem_Utils.LocalToGlobalDir(movementAction.localDir, p.comDir, p.chirality);
                if (movementAction.type == ActionType.PUSH)
                    handoverLabelToCheck = ParticleSystem_Utils.GetLabelInDir(dir, globalHeadDir);
                else if (movementAction.type == ActionType.PULL_HEAD)
                    handoverLabelToCheck = ParticleSystem_Utils.GetLabelInDir(dir, globalHeadDir, false);
                else if (movementAction.type == ActionType.PULL_TAIL)
                    handoverLabelToCheck = ParticleSystem_Utils.GetLabelInDir(dir, globalHeadDir, true);
            }

            CollectBondInfo(p, numNbrs, globalHeadDir, nbrParts, bondNbrs, nbrHead, nbrLabels, handoverLabelToCheck);

            // Neighbors have been found
            // Now check if the particle's movement agrees with its neighbors
            // While doing this, we compute the movement of all bonds that have not been considered yet,
            // i.e., all bonds to neighbors that have not been processed
            for (int label = 0; label < numNbrs; label++)
            {
                Particle nbr = nbrParts[label];
                if (nbr == null)
                    continue;

                // The offset by which the neighbor's origin will move relative to our origin
                Vector2Int nbrOffset = Vector2Int.zero;

                // Easy case: Both particles are contracted
                // There is only a single bond between the two particles
                if (p.IsContracted() && nbr.IsContracted())
                {
                    bool weMove = movementAction != null;
                    bool nbrMove = nbr.ScheduledMovement != null;
                    bool weMarked = p.markedBondsGlobal[label];
                    bool nbrMarked = nbr.markedBondsGlobal[nbrLabels[label]];

                    // Initialize bond info
                    Vector2Int bondStart1 = p.Tail();
                    Vector2Int bondEnd1 = nbr.Tail();
                    Vector2Int bondStart2 = p.Tail() + p.jmOffset;
                    Vector2Int bondEnd2 = nbr.Tail() + p.jmOffset;

                    // If we have marked the bond and want to move, we will definitely apply our own movement offset
                    if (weMove && weMarked)
                    {
                        nbrOffset = p.movementOffset;
                        // Bond moves as well
                        bondStart2 += p.movementOffset;
                        bondEnd2 += p.movementOffset;
                    }

                    // If the neighbor has marked the bond and wants to move, its offset has to be applied in reverse direction
                    if (nbrMove && nbrMarked)
                        nbrOffset -= nbr.movementOffset;

                    p.bondGraphicInfo.Add(new ParticleBondGraphicState(bondStart2, bondEnd2, bondStart1, bondEnd1));
                }
                // We are contracted and the neighbor is expanded
                else if (p.IsContracted() && nbr.IsExpanded())
                {
                    Direction bondDir = ParticleSystem_Utils.GetDirOfLabel(label);
                    // First check if we have two bonds to this neighbor
                    // If that is the case, remove the second occurrence from the list of neighbors
                    Direction secondBondDir = Direction.NONE;
                    int secondLabel = -1;
                    if (nbrParts[(label + 1) % 6] == nbr)
                    {
                        secondBondDir = bondDir.Rotate60(1);
                        nbrParts[(label + 1) % 6] = null;
                        secondLabel = (label + 1) % 6;
                    }
                    else if (nbrParts[(label + 5) % 6] == nbr)
                    {
                        secondBondDir = bondDir.Rotate60(-1);
                        nbrParts[(label + 5) % 6] = null;
                        secondLabel = (label + 5) % 6;
                    }

                    nbrOffset = JointMovementContractedExpanded(p, nbr, bondDir, secondBondDir,
                        label, secondLabel, nbrLabels[label], secondLabel != -1 ? nbrLabels[secondLabel] : -1,
                        nbrHead[label], secondLabel != -1 && nbrHead[secondLabel]);
                }
                // We are expanded and the neighbor is contracted
                else if (p.IsExpanded() && nbr.IsContracted())
                {
                    // This is the inverse of the previous case and we can handle it with the same logic

                    Direction bondDir = ParticleSystem_Utils.GetDirOfLabel(label, p.GlobalHeadDirection());
                    // First check if we have two bonds to this neighbor
                    // If that is the case, remove the second occurrence from the list of neighbors
                    // Note that since this is from the view of the expanded particle, the directions
                    // must be turned the other way around because the neighbor is at the node "between"
                    // the two nodes occupied by us
                    Direction secondBondDir = Direction.NONE;
                    int secondLabel = -1;
                    if (nbrParts[(label + 1) % 10] == nbr)
                    {
                        secondBondDir = bondDir.Rotate60(-1);
                        nbrParts[(label + 1) % 10] = null;
                        secondLabel = (label + 1) % 10;
                    }
                    else if (nbrParts[(label + 9) % 10] == nbr)
                    {
                        secondBondDir = bondDir.Rotate60(1);
                        nbrParts[(label + 9) % 10] = null;
                        secondLabel = (label + 9) % 10;
                    }

                    // Offset must be inverted because it is from the perspective of the contracted neighbor
                    nbrOffset -= JointMovementContractedExpanded(nbr, p, bondDir.Opposite(), secondBondDir.Opposite(),
                        nbrLabels[label], secondLabel != -1 ? nbrLabels[secondLabel] : -1,
                        label, secondLabel,
                        ParticleSystem_Utils.IsHeadLabel(label, p.GlobalHeadDirection()),
                        secondLabel != -1 && ParticleSystem_Utils.IsHeadLabel(secondLabel, p.GlobalHeadDirection()),
                        // Associate bond with us, not with the neighbor
                        false);
                }
                // We and the neighbor are both expanded
                else if (p.IsExpanded() && nbr.IsExpanded())
                {
                    // First find all bonds connecting the two neighbors
                    int numBonds = 0;
                    int[] bondLabels = new int[3] { -1, -1, -1 };
                    int[] nbrBondLabels = new int[3] { -1, -1, -1 };
                    bool[] bondNbrHead = new bool[] { false, false, false };
                    bool[] bondOwnHead = new bool[] { false, false, false };

                    // We can have a maximum of 3 bonds, must look 2 positions in each direction (middle is the current bond)
                    // Also, remove the other occurrences from the list of neighbors
                    foreach (int offset in new int[] { 8, 9, 0, 1, 2 })
                    {
                        int lb = (label + offset) % 10;
                        if (nbrParts[lb] == nbr)
                        {
                            bondLabels[numBonds] = lb;
                            nbrBondLabels[numBonds] = nbrLabels[lb];
                            bondNbrHead[numBonds] = nbrHead[lb];
                            bondOwnHead[numBonds] = ParticleSystem_Utils.IsHeadLabel(lb, p.GlobalHeadDirection());
                            if (offset != 0)
                            {
                                nbrParts[lb] = null;
                            }
                            numBonds++;
                        }
                    }

                    // Prepare bond info
                    Vector2Int ourBond1_1 = bondOwnHead[0] ? p.Head() : p.Tail();
                    Vector2Int nbrBond1_1 = bondNbrHead[0] ? nbr.Head() : nbr.Tail();
                    Vector2Int ourBond1_2 = ourBond1_1 + p.jmOffset;
                    Vector2Int nbrBond1_2 = nbrBond1_1 + p.jmOffset;

                    // Exactly one bond:
                    //      Apply our movement and the inverted neighbor movement
                    // Exactly two bonds:
                    //      Both to the same part of the neighbor:
                    //          We must not contract normally
                    //          If we move: Only handover is allowed
                    //              The bond on our moving part must be marked
                    //          If we do not move: Neighbor must have same marked status on the bonds
                    //      Both start at the same part of us:
                    //          Inverse case to above
                    //          Uses the same logic
                    //      From different parts to different parts (neighbor is parallel):
                    //          Both can contract simultaneously (origin is determined by us)
                    //              Also works with handovers without marked bonds
                    //          If one performs a handover with one of its bonds marked, the neighbor
                    //              must not move or it must perform a handover with a marked bond as well
                    //              TODO: Are two handovers with the same marked bond allowed?
                    //                  Answer: Same result as handover with both bonds unmarked!
                    //                  Demand that both have the same marked status
                    // Three bonds:
                    //      None of the bonds must move
                    //      Only allowed movements are handovers where bonds at moving parts are marked

                    if (numBonds == 1)
                    {
                        // Apply our own movement offset
                        // Only necessary if the bond is not at the origin and not marked for handover transferral
                        if (movementAction != null && (p.isHeadOrigin ^ bondOwnHead[0]) &&
                            (!p.markedBondsGlobal[bondLabels[0]] ||
                            movementAction.type == ActionType.CONTRACT_HEAD || movementAction.type == ActionType.CONTRACT_TAIL))
                        {
                            nbrOffset += p.movementOffset;
                            // Also apply offset to bond
                            ourBond1_2 += p.movementOffset;
                            nbrBond1_2 += p.movementOffset;
                        }

                        // Apply inverted neighbor movement offset
                        // Only necessary if we are not bonded to its origin and the bond is not marked for handover transferral
                        if (nbr.ScheduledMovement != null && (nbr.isHeadOrigin ^ bondNbrHead[0]) &&
                            (!nbr.markedBondsGlobal[nbrBondLabels[0]]
                            || nbr.ScheduledMovement.type == ActionType.CONTRACT_HEAD || nbr.ScheduledMovement.type == ActionType.CONTRACT_TAIL))
                        {
                            nbrOffset -= nbr.movementOffset;
                        }

                        // Store the bond info
                        p.bondGraphicInfo.Add(new ParticleBondGraphicState(ourBond1_2, nbrBond1_2, ourBond1_1, nbrBond1_1));
                    }
                    else if (numBonds == 2)
                    {
                        // Prepare bond info for second bond
                        Vector2Int ourBond2_1 = bondOwnHead[1] ? p.Head() : p.Tail();
                        Vector2Int nbrBond2_1 = bondNbrHead[1] ? nbr.Head() : nbr.Tail();
                        Vector2Int ourBond2_2 = ourBond2_1 + p.jmOffset;
                        Vector2Int nbrBond2_2 = nbrBond2_1 + p.jmOffset;

                        // Case distinction: Either the two bonds share one end or they do not
                        // They share an end iff the two bond labels are consecutive
                        if (bondLabels[1] == (bondLabels[0] + 1) % 10)
                        {
                            // The bonds share one end: Find out which particle that end belongs to
                            if (bondOwnHead[0] == bondOwnHead[1])
                            {
                                // Shared end belongs to us
                                int nbrHeadLabel = bondNbrHead[0] ? nbrBondLabels[0] : nbrBondLabels[1];
                                int nbrTailLabel = bondNbrHead[0] ? nbrBondLabels[1] : nbrBondLabels[0];

                                nbrOffset += JointMovementExpandedTwoBonds(p, nbr, bondOwnHead[0],
                                    p.markedBondsGlobal[bondLabels[0]], p.markedBondsGlobal[bondLabels[1]],
                                    nbr.markedBondsGlobal[nbrHeadLabel], nbr.markedBondsGlobal[nbrTailLabel]);

                                // We have to move both bonds if we move and the bonds are not at our origin
                                if (movementAction != null && (bondOwnHead[0] ^ p.isHeadOrigin))
                                {
                                    ourBond1_2 += p.movementOffset;
                                    nbrBond1_2 += p.movementOffset;
                                    ourBond2_2 += p.movementOffset;
                                    nbrBond2_2 += p.movementOffset;
                                }
                            }
                            else
                            {
                                // Shared end belongs to neighbor
                                int headLabel = bondOwnHead[0] ? bondLabels[0] : bondLabels[1];
                                int tailLabel = bondOwnHead[0] ? bondLabels[1] : bondLabels[0];

                                // Must apply inverted offset
                                nbrOffset -= JointMovementExpandedTwoBonds(nbr, p, bondNbrHead[0],
                                    nbr.markedBondsGlobal[nbrBondLabels[0]], nbr.markedBondsGlobal[nbrBondLabels[1]],
                                    p.markedBondsGlobal[headLabel], p.markedBondsGlobal[tailLabel]);
                            }
                        }
                        else
                        {
                            // The bonds do not share an end, so the particles are parallel
                            // Both particles must agree on the bonds either merging or staying apart

                            // Find out the kind of the two movements
                            int headLabel = bondOwnHead[0] ? bondLabels[0] : bondLabels[1];
                            int tailLabel = bondOwnHead[0] ? bondLabels[1] : bondLabels[0];
                            int nbrHeadLabel = bondNbrHead[0] ? nbrBondLabels[0] : nbrBondLabels[1];
                            int nbrTailLabel = bondNbrHead[0] ? nbrBondLabels[1] : nbrBondLabels[0];

                            ParticleAction nbrMovement = nbr.ScheduledMovement;

                            // Doing a handover with marked bonds has the same effect as not moving at all,
                            // so we only check for handovers where the moving bond is not marked
                            bool weDoHandover = movementAction != null && (
                                movementAction.type == ActionType.PULL_HEAD && !p.markedBondsGlobal[tailLabel] ||
                                movementAction.type == ActionType.PULL_TAIL && !p.markedBondsGlobal[headLabel]);
                            bool weMove = movementAction != null && (
                                movementAction.type == ActionType.CONTRACT_HEAD ||
                                movementAction.type == ActionType.CONTRACT_TAIL ||
                                weDoHandover);

                            bool nbrDoesHandover = nbrMovement != null && (
                                nbrMovement.type == ActionType.PULL_HEAD && !nbr.markedBondsGlobal[nbrTailLabel] ||
                                nbrMovement.type == ActionType.PULL_TAIL && !nbr.markedBondsGlobal[nbrHeadLabel]);
                            bool nbrMoves = nbrMovement != null && (
                                nbrMovement.type == ActionType.CONTRACT_HEAD ||
                                nbrMovement.type == ActionType.CONTRACT_TAIL ||
                                nbrDoesHandover);

                            if (weMove ^ nbrMoves)
                            {
                                throw new SimulationException("Conflict during joint movement: Two parallel expanded particles with two bonds have conflicting movements."
                                    + "\nParticles at " + p.Head() + ", " + p.Tail() + " and " + nbr.Head() + ", " + nbr.Tail());
                            }

                            // If we move, we have to apply an offset if the movement directions are inverted
                            if (weMove && p.movementOffset != nbr.movementOffset)
                            {
                                nbrOffset += p.movementOffset;
                            }

                            // The moving bond needs an offset
                            if (weMove)
                            {
                                if (movementAction.type == ActionType.CONTRACT_HEAD || movementAction.type == ActionType.PULL_HEAD)
                                {
                                    // Tail moves
                                    if (bondOwnHead[0])
                                    {
                                        ourBond2_2 += p.movementOffset;
                                        nbrBond2_2 += p.movementOffset;
                                    }
                                    else
                                    {
                                        ourBond1_2 += p.movementOffset;
                                        nbrBond1_2 += p.movementOffset;
                                    }
                                }
                                else
                                {
                                    // Head moves
                                    if (bondOwnHead[0])
                                    {
                                        ourBond1_2 += p.movementOffset;
                                        nbrBond1_2 += p.movementOffset;
                                    }
                                    else
                                    {
                                        ourBond2_2 += p.movementOffset;
                                        nbrBond2_2 += p.movementOffset;
                                    }
                                }
                            }
                        }

                        // Store the bond info
                        p.bondGraphicInfo.Add(new ParticleBondGraphicState(ourBond1_2, nbrBond1_2, ourBond1_1, nbrBond1_1));
                        p.bondGraphicInfo.Add(new ParticleBondGraphicState(ourBond2_2, nbrBond2_2, ourBond2_1, nbrBond2_1));
                    }
                    else
                    {
                        // 3 bonds
                        // No relative movement occurs, we only have to check if the movements are valid
                        // The only allowed movements are no movement at all or handovers where all involved bonds
                        // are marked for transfer
                        if (movementAction != null)
                        {
                            if (movementAction.type == ActionType.CONTRACT_HEAD || movementAction.type == ActionType.CONTRACT_TAIL)
                            {
                                throw new SimulationException("Conflict during joint movement: Expanded particle with three bonds to expanded neighbor tries to contract."
                                    + "\nParticles at " + p.Head() + ", " + p.Tail() + " and " + nbr.Head() + ", " + nbr.Tail());
                            }

                            // Movement is handover, check if the non-origin bonds are marked
                            for (int i = 0; i < 3; i++)
                            {
                                if ((p.isHeadOrigin ^ bondOwnHead[i]) && !p.markedBondsGlobal[bondLabels[i]])
                                {
                                    throw new SimulationException("Conflict during joint movement: Expanded particle with three bonds to expanded neighbor performs handover with unmarked bond."
                                        + "\nParticles at " + p.Head() + ", " + p.Tail() + " and " + nbr.Head() + ", " + nbr.Tail());
                                }
                            }
                        }

                        // Same check for the neighbor
                        ParticleAction nbrAction = nbr.ScheduledMovement;
                        if (nbrAction != null)
                        {
                            if (nbrAction.type == ActionType.CONTRACT_HEAD || nbrAction.type == ActionType.CONTRACT_TAIL)
                            {
                                throw new SimulationException("Conflict during joint movement: Expanded particle with three bonds to expanded neighbor tries to contract."
                                    + "\nParticles at " + p.Head() + ", " + p.Tail() + " and " + nbr.Head() + ", " + nbr.Tail());
                            }

                            // Movement is handover, check if the non-origin bonds are marked
                            for (int i = 0; i < 3; i++)
                            {
                                if ((nbr.isHeadOrigin ^ bondNbrHead[i]) && !nbr.markedBondsGlobal[nbrBondLabels[i]])
                                {
                                    throw new SimulationException("Conflict during joint movement: Expanded particle with three bonds to expanded neighbor performs handover with unmarked bond."
                                        + "\nParticles at " + p.Head() + ", " + p.Tail() + " and " + nbr.Head() + ", " + nbr.Tail());
                                }
                            }
                        }

                        // We still need to draw the bonds
                        Vector2Int ourBond2_1 = bondOwnHead[1] ? p.Head() : p.Tail();
                        Vector2Int nbrBond2_1 = bondNbrHead[1] ? nbr.Head() : nbr.Tail();
                        Vector2Int ourBond2_2 = ourBond2_1 + p.jmOffset;
                        Vector2Int nbrBond2_2 = nbrBond2_1 + p.jmOffset;
                        Vector2Int ourBond3_1 = bondOwnHead[2] ? p.Head() : p.Tail();
                        Vector2Int nbrBond3_1 = bondNbrHead[2] ? nbr.Head() : nbr.Tail();
                        Vector2Int ourBond3_2 = ourBond3_1 + p.jmOffset;
                        Vector2Int nbrBond3_2 = nbrBond3_1 + p.jmOffset;
                        p.bondGraphicInfo.Add(new ParticleBondGraphicState(ourBond1_2, nbrBond1_2, ourBond1_1, nbrBond1_1));
                        p.bondGraphicInfo.Add(new ParticleBondGraphicState(ourBond2_2, nbrBond2_2, ourBond2_1, nbrBond2_1));
                        p.bondGraphicInfo.Add(new ParticleBondGraphicState(ourBond3_2, nbrBond3_2, ourBond3_1, nbrBond3_1));

                    }
                }

                // Have computed the relative offset of this neighbor
                // If the neighbor is already queued, its offset has already been set by another particle
                // and we must check if it matches our computed offset
                nbrOffset += p.jmOffset;
                if (nbr.queuedForJMProcessing)
                {
                    if (nbr.jmOffset != nbrOffset)
                    {
                        throw new SimulationException("Conflict during joint movement: Offset for particle does not match! Previous offset: " + nbr.jmOffset + ", new offset: " + nbrOffset
                            + "\nCurrent particle at " + p.Head() + ", " + p.Tail() + ", neighbor at " + nbr.Head() + ", " + nbr.Tail());
                    }
                }
                else
                {
                    nbr.jmOffset = nbrOffset;
                    queue.Enqueue(nbr);
                    nbr.queuedForJMProcessing = true;
                }
            }

            // Neighbors have been handled, now compute our location in the new particle system
            // and place it if the location is not already occupied
            bool expandedAfterMovement = p.IsExpanded() && movementAction == null ||
                movementAction != null && (movementAction.type == ActionType.EXPAND || movementAction.type == ActionType.PUSH);
            if (expandedAfterMovement)
            {
                Vector2Int head = p.Head() + p.jmOffset;
                Vector2Int tail = p.Tail() + p.jmOffset;
                if (movementAction != null)
                {
                    head = tail + p.movementOffset;
                }

                if (newPositions.ContainsKey(head) || newPositions.ContainsKey(tail))
                {
                    throw new SimulationException("Conflict during joint movement: Target location of expanded particle is already occupied."
                        + "\nParticle at " + p.Head() + ", " + p.Tail() + ", target locations are " + head + ", " + tail);
                }
                newPositions[head] = p;
                newPositions[tail] = p;
            }
            else
            {
                Vector2Int pos = p.Head() + p.jmOffset;
                // If the origin is not the head, add the movement offset
                if (movementAction != null && (movementAction.type == ActionType.CONTRACT_TAIL || movementAction.type == ActionType.PULL_TAIL))
                {
                    pos += p.movementOffset;
                }
                if (newPositions.ContainsKey(pos))
                {
                    throw new SimulationException("Conflict during joint movement: target location of contracted particle is already occupied."
                        + "\nParticle at " + p.Head() + ", target location is " + pos);
                }
                newPositions[pos] = p;
            }

            p.processedJointMovement = true;
        }

        // BFS has finished, now apply the movements to the particles locally
        // Also, check if any particle was not processed
        foreach (Particle p in particles)
        {
            if (!p.processedJointMovement)
            {
                throw new SimulationException("Bond structure is not connected, some particle movements were not processed!"
                    + "\nFirst encountered non-processed particle at " + p.Head() + ", " + p.Tail());
            }
            else if (p.ScheduledMovement != null)
            {
                if (p.ScheduledMovement.type == ActionType.EXPAND || p.ScheduledMovement.type == ActionType.PUSH)
                {
                    p.Apply_Expand(p.ScheduledMovement.localDir, p.jmOffset);
                }
                else if (p.ScheduledMovement.type == ActionType.CONTRACT_HEAD || p.ScheduledMovement.type == ActionType.PULL_HEAD)
                {
                    p.Apply_ContractHead(p.jmOffset);
                }
                else if (p.ScheduledMovement.type == ActionType.CONTRACT_TAIL || p.ScheduledMovement.type == ActionType.PULL_TAIL)
                {
                    p.Apply_ContractTail(p.jmOffset);
                }
            }
            else
            {
                p.Apply_Offset(p.jmOffset);
            }
        }

        // Replace the particle map with the new positions
        particleMap = newPositions;

        Debug.Log("Computed joint movements in " + (Time.realtimeSinceStartup - tStart) + "s");
    }

    /// <summary>
    /// Helper method for finding the bonded neighbors of a particle.
    /// </summary>
    /// <param name="p">The particle whose neighbors shall be found.</param>
    /// <param name="numNbrs">The number of labels to be checked. Should be
    /// <c>6</c> or <c>10</c>, depending on the expansion state of the
    /// particle. All provided array parameters must have at least this length.</param>
    /// <param name="globalHeadDir">The global head direction of the particle.</param>
    /// <param name="nbrParts">A particle array that should hold references to the
    /// bonded neighbor particles that still have to be processed.</param>
    /// <param name="bondNbrs">A bool array that should have <c>true</c> entries for
    /// all labels at which there is a bonded neighbor.</param>
    /// <param name="nbrHead">A bool array that should have <c>true</c> entries for
    /// all labels that are adjacent to the head of a bonded neighbor.</param>
    /// <param name="nbrLabels">An int array that should hold the bonded neighbor's
    /// label corresponding to each of the current particle's labels.</param>
    /// <param name="handoverLabelToCheck">One label that must have a bonded neighbor
    /// because the current particle intends to perform a handover in that direction.
    /// If no such neighbor is found, a <see cref="System.InvalidOperationException"/>
    /// is thrown. Use <c>-1</c> to indicate that no label must be checked.</param>
    private void CollectBondInfo(Particle p, int numNbrs, Direction globalHeadDir,
        Particle[] nbrParts, bool[] bondNbrs, bool[] nbrHead, int[] nbrLabels,
        int handoverLabelToCheck = -1)
    {
        for (int label = 0; label < numNbrs; label++)
        {
            Direction dir = ParticleSystem_Utils.GetDirOfLabel(label, globalHeadDir);
            bool head = ParticleSystem_Utils.IsHeadLabel(label, globalHeadDir);
            Vector2Int nbrPos = ParticleSystem_Utils.GetNbrInDir(head ? p.Head() : p.Tail(), dir);
            if (particleMap.TryGetValue(nbrPos, out Particle nbr))
            {
                // We have a neighbor at this location

                // Collect information and check if we have a bond to this neighbor
                bool isNbrHead = nbr.Head() == nbrPos;
                int nbrLabel = ParticleSystem_Utils.GetLabelInDir(dir.Opposite(), nbr.GlobalHeadDirection(), isNbrHead);

                // Have connection if both of the bonds are active
                bool myBondActive = p.activeBondsGlobal[label];
                bool nbrBondActive = nbr.activeBondsGlobal[nbrLabel];

                bool bondsActive = myBondActive && nbrBondActive;
                bool bondsDisagree = myBondActive ^ nbrBondActive;
                // TODO: Maybe remove this warning?
                if (bondsDisagree && !p.markedForAutomaticBonds && !nbr.markedForAutomaticBonds)
                {
                    Debug.LogWarning("Bonds disagree between particles at " + p.Head() + ", " + p.Tail() + " and " + nbrPos);
                }

                // Add the neighbor to the list if there is a bond and the neighbor has not been processed yet
                if (bondsActive)
                {
                    // This info is useful for bond computation as well
                    bondNbrs[label] = true;
                    nbrHead[label] = isNbrHead;
                    nbrLabels[label] = nbrLabel;
                    // This is only set if we want to process the neighbor
                    if (!nbr.processedJointMovement)
                    {
                        nbrParts[label] = nbr;
                    }
                }
            }

            // Error if there is no neighbor here although a handover is scheduled
            if (label == handoverLabelToCheck && !bondNbrs[label])
            {
                throw new SimulationException("Error computing movements: Particle at position " + p.Head() + ", " + p.Tail() + " has scheduled handover at label " + handoverLabelToCheck
                    + " but has no neighbor at that label.");
            }
        }
    }

    /// <summary>
    /// Helper method for handling the joint movement of a contracted particle that is bonded
    /// to an expanded particle.
    /// </summary>
    /// <param name="c">The contracted particle.</param>
    /// <param name="e">The expanded particle.</param>
    /// <param name="bondDir">The global direction of the primary bond from the contracted
    /// to the expanded particle. The primary bond is simply the first one we encounter
    /// while processing the movements.</param>
    /// <param name="secondBondDir">The global direction of the second bond from the
    /// contracted to the expanded particle. Must be <see cref="Direction.NONE"/> if there
    /// is no second bond.</param>
    /// <param name="cLabel1">The global label of the contracted particle identifying the primary bond.</param>
    /// <param name="cLabel2">The global label of the contracted particle identifying the second bond.
    /// Must be <c>-1</c> if there is no second bond.</param>
    /// <param name="eLabel1">The global label of the expanded particle identifying the primary bond.</param>
    /// <param name="eLabel2">The global label of the expanded particle identifying the second bond.
    /// Must be <c>-1</c> if there is no second bond.</param>
    /// <param name="eHead1">Flag indicating whether the first bond is incident to the head of the
    /// expanded particle.</param>
    /// <param name="eHead2">Flag indicating whether the second bond is incident to the head of the
    /// expanded particle. Must be <c>false</c> if there is no second bond.</param>
    /// <param name="bondForContracted">Flag indicating whether the bond info should be
    /// associated with the contracted particle.</param>
    /// <returns>The global offset vector of the expanded particle's origin relative to the
    /// contracted particle's origin.</returns>
    private Vector2Int JointMovementContractedExpanded(Particle c, Particle e, Direction bondDir, Direction secondBondDir,
        int cLabel1, int cLabel2, int eLabel1, int eLabel2, bool eHead1, bool eHead2,
        bool bondForContracted = true)
    {
        Vector2Int eOffset = Vector2Int.zero;
        ParticleAction cAction = c.ScheduledMovement;
        ParticleAction eAction = e.ScheduledMovement;

        // Setup bond info (for first bond only)
        Vector2Int cBond1 = c.Tail();
        Vector2Int eBond1 = eHead1 ? e.Head() : e.Tail();
        Vector2Int cBond2 = cBond1;
        Vector2Int eBond2 = eBond1;

        if (secondBondDir == Direction.NONE)
        {
            // We have only one bond
            // Check if we want to perform a handover
            bool weWantHandover = cAction != null &&
                cAction.type == ActionType.PUSH &&
                ParticleSystem_Utils.LocalToGlobalDir(cAction.localDir, c.comDir, c.chirality) == bondDir;
            bool nbrWantsHandover = eAction != null &&
                (eAction.type == ActionType.PULL_HEAD && !eHead1 ||
                eAction.type == ActionType.PULL_TAIL && eHead1) &&
                ParticleSystem_Utils.LocalToGlobalDir(eAction.localDir, e.comDir, e.chirality) == bondDir.Opposite();

            if (weWantHandover ^ nbrWantsHandover)
            {
                throw new SimulationException("Conflict during movements: Disagreement on handover with one bond."
                    + "\nParticles at " + c.Head() + " and " + e.Head() + ", " + e.Tail());
            }

            if (weWantHandover)
            {
                // Easy: No offset is applied at all
                // Bond rotates, both ends are moved with the particles
                cBond2 += c.movementOffset;
                eBond2 += e.movementOffset;
            }
            else
            {
                // No handover
                // Apply both offsets
                if (cAction != null && c.markedBondsGlobal[cLabel1])
                {
                    eOffset += c.movementOffset;
                    // This only affects the bond if it is associated with the contracted particle
                    if (bondForContracted)
                    {
                        cBond2 += c.movementOffset;
                        eBond2 += c.movementOffset;
                    }
                }
                // Neighbor offset only applies if we are not bonded to its origin
                if (eAction != null &&
                    (eHead1 && (eAction.type == ActionType.CONTRACT_TAIL || eAction.type == ActionType.PULL_TAIL) ||
                    !eHead1 && (eAction.type == ActionType.CONTRACT_HEAD || eAction.type == ActionType.PULL_HEAD)))
                {
                    if (e.markedBondsGlobal[eLabel1] && (eAction.type == ActionType.PULL_HEAD || eAction.type == ActionType.PULL_TAIL))
                    {
                        // Neighbor has marked this bond and is performing a handover
                        // We stay in place, the neighbor contracts toward the other direction
                        // The effect is that we do not apply any offset
                    }
                    else
                    {
                        // Bond is not marked for handover transferral, movement creates offset
                        eOffset -= e.movementOffset;

                        // Have to move the bond if it belongs to the expanded particle
                        if (!bondForContracted)
                        {
                            cBond2 += e.movementOffset;
                            eBond2 += e.movementOffset;
                        }
                    }
                }
            }

            // Store the bond info
            if (bondForContracted)
            {
                c.bondGraphicInfo.Add(new ParticleBondGraphicState(cBond2 + c.jmOffset, eBond2 + c.jmOffset, cBond1, eBond1));
            }
            else
            {
                e.bondGraphicInfo.Add(new ParticleBondGraphicState(eBond2 + e.jmOffset, cBond2 + e.jmOffset, eBond1, cBond1));
            }
        }
        else
        {
            // We have two bonds
            // Check if we want to perform a handover on either bond
            bool weWantHandover = cAction != null &&
                cAction.type == ActionType.PUSH;
            bool weWantHandoverFirst = weWantHandover &&
                ParticleSystem_Utils.LocalToGlobalDir(cAction.localDir, c.comDir, c.chirality) == bondDir;
            bool weWantHandoverSecond = weWantHandover &&
                ParticleSystem_Utils.LocalToGlobalDir(cAction.localDir, c.comDir, c.chirality) == secondBondDir;

            bool nbrWantsHandoverFirst = eAction != null &&
                (eAction.type == ActionType.PULL_HEAD && !eHead1 ||
                eAction.type == ActionType.PULL_TAIL && eHead1) &&
                ParticleSystem_Utils.LocalToGlobalDir(eAction.localDir, e.comDir, e.chirality) == bondDir.Opposite();
            bool nbrWantsHandoverSecond = eAction != null &&
                (eAction.type == ActionType.PULL_HEAD && !eHead2 ||
                eAction.type == ActionType.PULL_TAIL && eHead2) &&
                ParticleSystem_Utils.LocalToGlobalDir(eAction.localDir, e.comDir, e.chirality) == secondBondDir.Opposite();

            if ((weWantHandoverFirst ^ nbrWantsHandoverFirst) || (weWantHandoverSecond ^ nbrWantsHandoverSecond))
            {
                throw new SimulationException("Conflict during movements: Disagreement on handover with two bonds."
                    + "\nParticles at " + c.Head() + " and " + e.Head() + ", " + e.Tail());
            }

            // Prepare info for second bond
            Vector2Int eBond1_2 = eHead2 ? e.Head() : e.Tail();
            Vector2Int cBond2_2 = cBond1;
            Vector2Int eBond2_2 = eBond1_2;

            bool handoverFirst = weWantHandoverFirst && nbrWantsHandoverFirst;
            bool handoverSecond = weWantHandoverSecond && nbrWantsHandoverSecond;

            if (handoverFirst || handoverSecond)
            {
                // We want a handover on one of the two bonds
                // Easy: No offset must be applied
                // But we have to check that the other bond is not marked by the contracted particle
                if (handoverFirst && c.markedBondsGlobal[cLabel2] || handoverSecond && c.markedBondsGlobal[cLabel1])
                {
                    throw new SimulationException("Conflict during movements: Handover with two bonds and one bond is marked."
                        + "\nParticles at " + c.Head() + " and " + e.Head() + ", " + e.Tail());
                }

                // We also have to rotate the corresponding bond
                if (handoverFirst)
                {
                    cBond2 += c.movementOffset;
                    eBond2 += e.movementOffset;
                }
                else
                {
                    cBond2_2 += c.movementOffset;
                    eBond2_2 += e.movementOffset;
                }
            }
            else
            {
                // No handover
                // Neighbor must not contract without leaving one bond and both bonds must have the same
                // marked status if we want to move

                // Handover is possible for neighbor if one of the bonds stays in place
                if (eAction != null)
                {
                    int eHeadBond = eHead1 ? eLabel1 : eLabel2;
                    int eTailBond = eHead1 ? eLabel2 : eLabel1;
                    if (eAction.type == ActionType.CONTRACT_HEAD || eAction.type == ActionType.CONTRACT_TAIL ||
                        (eAction.type == ActionType.PULL_HEAD && !e.markedBondsGlobal[eTailBond]) ||
                        (eAction.type == ActionType.PULL_TAIL && !e.markedBondsGlobal[eHeadBond]))
                    {
                        throw new SimulationException("Conflict during movements: Expanded neighbor with two bonds tries to contract."
                            + "\nParticles at " + c.Head() + " and " + e.Head() + ", " + e.Tail());
                    }
                }

                if (c.ScheduledMovement != null)
                {
                    if (c.markedBondsGlobal[cLabel1] ^ c.markedBondsGlobal[cLabel2])
                    {
                        throw new SimulationException("Conflict during movements: Bonds to expanded neighbor have different marked status."
                            + "\nParticles at " + c.Head() + " and " + e.Head() + ", " + e.Tail());
                    }

                    // If the bonds are marked, we apply our offset
                    if (c.markedBondsGlobal[cLabel1])
                    {
                        eOffset += c.movementOffset;
                        // Also apply the offset to the bonds if they are associated to the contracted particle
                        if (bondForContracted)
                        {
                            cBond2 += c.movementOffset;
                            eBond2 += c.movementOffset;
                            cBond2_2 += c.movementOffset;
                            eBond2_2 += c.movementOffset;
                        }
                    }
                }
            }

            // Store the bond info
            if (bondForContracted)
            {
                c.bondGraphicInfo.Add(new ParticleBondGraphicState(cBond2 + c.jmOffset, eBond2 + c.jmOffset, cBond1, eBond1));
                c.bondGraphicInfo.Add(new ParticleBondGraphicState(cBond2_2 + c.jmOffset, eBond2_2 + c.jmOffset, cBond1, eBond1_2));
            }
            else
            {
                e.bondGraphicInfo.Add(new ParticleBondGraphicState(eBond2 + e.jmOffset, cBond2 + e.jmOffset, eBond1, cBond1));
                e.bondGraphicInfo.Add(new ParticleBondGraphicState(eBond2_2 + e.jmOffset, cBond2_2 + e.jmOffset, eBond1_2, cBond1));
            }
        }
        return eOffset;
    }

    /// <summary>
    /// Helper method for handling the joint movements of two neighboring expanded
    /// particles that from a triangle with two bonds.
    /// </summary>
    /// <param name="pCorner">The particle owning the part where the two bonds meet.</param>
    /// <param name="pEdge">The particle having one bond on each of its parts.</param>
    /// <param name="cornerAtHead">Indicates whether the part where the two bonds meet
    /// is the head of the <paramref name="pCorner"/> particle.</param>
    /// <param name="cornerMarked1">Indicates whether the particle owning the corner has
    /// marked the first bond.</param>
    /// <param name="cornerMarked2">Indicates whether the particle owning the corner has
    /// marked the second bond.</param>
    /// <param name="edgeMarkedHead">Indicates whether the edge particle has marked the
    /// bond at its head.</param>
    /// <param name="edgeMarkedTail">Indicates whether the edge particle has marked the
    /// bond at its tail.</param>
    /// <returns>The global offset vector of the edge particle's origin relative to the
    /// corner particle's origin.</returns>
    private Vector2Int JointMovementExpandedTwoBonds(Particle pCorner, Particle pEdge, bool cornerAtHead,
        bool cornerMarked1, bool cornerMarked2,
        bool edgeMarkedHead, bool edgeMarkedTail)
    {
        Vector2Int edgeOffset = Vector2Int.zero;

        ParticleAction cAction = pCorner.ScheduledMovement;
        ParticleAction eAction = pEdge.ScheduledMovement;

        // The particle with the edge cannot add any offset, but its movements must be checked
        if (eAction != null)
        {
            // The particle must not contract without handover
            if (eAction.type == ActionType.CONTRACT_HEAD || eAction.type == ActionType.CONTRACT_TAIL)
            {
                throw new SimulationException("Conflict during joint movements: Two expanded particles form triangle of bonds which is broken by contraction."
                    + "\nParticles at " + pCorner.Head() + ", " + pCorner.Tail() + " and " + pEdge.Head() + ", " + pEdge.Tail());
            }

            // It may perform a handover if it has marked the moving bond
            if (eAction.type == ActionType.PULL_HEAD && !edgeMarkedTail || eAction.type == ActionType.PULL_TAIL && !edgeMarkedHead)
            {
                throw new SimulationException("Conflict during joint movements: Two expanded particles form triangle of bonds which is broken by handover."
                    + "\nParticles at " + pCorner.Head() + ", " + pCorner.Tail() + " and " + pEdge.Head() + ", " + pEdge.Tail());
            }
        }

        // If the particle with the corner moves the part with the two bonds, its offset may be applied
        if (cAction != null && (cornerAtHead ^ pCorner.isHeadOrigin))
        {
            // Bonds are not adjacent to the origin
            // Apply offset if this is not a handover with both bonds marked
            bool applyOffset = true;
            if (cAction.type == ActionType.PULL_HEAD || cAction.type == ActionType.PULL_TAIL)
            {
                if (cornerMarked1 ^ cornerMarked2)
                {
                    throw new SimulationException("Conflict during joint movements: Two expanded particles form triangle of bonds which is broken by unequal handover transfer."
                        + "\nParticles at " + pCorner.Head() + ", " + pCorner.Tail() + " and " + pEdge.Head() + ", " + pEdge.Tail());
                }
                if (cornerMarked1)
                    applyOffset = false;
            }
            if (applyOffset)
            {
                edgeOffset += pCorner.movementOffset;
            }
        }

        return edgeOffset;
    }

    /// <summary>
    /// Computes bond information in the case that no movements have occurred.
    /// This method works similar to the joint movement simulation but it does
    /// not process any movements. It also does not check whether the particle
    /// bond structure is connected or not. Bonds will only be computed for the
    /// connected component that contains the anchor particle.
    /// </summary>
    private void ComputeBondsStatic()
    {
        // Use BFS, just like for movement simulation
        Queue<Particle> queue = new Queue<Particle>();

        // Start with the anchor particle
        Particle anchor = particles[0];
        anchor.jmOffset = Vector2Int.zero;

        queue.Enqueue(anchor);
        anchor.queuedForJMProcessing = true;

        while (queue.Count > 0)
        {
            Particle p = queue.Dequeue();

            p.jmOffset = Vector2Int.zero;

            // Find the bonded neighbors of the particle
            Direction globalHeadDir = p.GlobalHeadDirection();
            int numNbrs = p.IsExpanded() ? 10 : 6;
            Particle[] nbrParts = new Particle[numNbrs];
            bool[] nbrHead = new bool[numNbrs];     // True if the neighbor's head is at this position
            int[] nbrLabels = new int[numNbrs];     // Stores global neighbor label opposite of our label
            bool[] bondNbrs = new bool[numNbrs];    // True wherever we have a bonded neighbor

            CollectBondInfo(p, numNbrs, globalHeadDir, nbrParts, bondNbrs, nbrHead, nbrLabels);

            // Go through the labels and process the bonds to each neighbor
            for (int label = 0; label < numNbrs; label++)
            {
                Particle nbr = nbrParts[label];
                if (nbr == null)
                    continue;

                // Find out how many bonds we have to that neighbor
                int numBonds = 0;
                int[] bondLabels = new int[3] { -1, -1, -1 };
                int[] nbrBondLabels = new int[3] { -1, -1, -1 };
                bool[] bondNbrHead = new bool[] { false, false, false };
                bool[] bondOwnHead = new bool[] { false, false, false };

                int[] offsets;

                if (p.IsContracted() || nbr.IsContracted())
                {
                    // We can have a maximum of 2 bonds, look 1 position in each direction
                    offsets = new int[] { 0, 1, numNbrs - 1 };
                }
                else
                {
                    // We can have a maximum of 3 bonds, must look 2 positions in each direction (middle is the current bond)
                    offsets = new int[] { 8, 9, 0, 1, 2 };
                    // Also, remove the other occurrences from the list of neighbors
                }
                // Collect information about the bonds and remove the neighbor's other occurrences
                // so that it is not processed multiple times
                foreach (int offset in offsets)
                {
                    int lb = (label + offset) % numNbrs;
                    if (nbrParts[lb] == nbr)
                    {
                        bondLabels[numBonds] = lb;
                        nbrBondLabels[numBonds] = nbrLabels[lb];
                        bondNbrHead[numBonds] = nbrHead[lb];
                        bondOwnHead[numBonds] = ParticleSystem_Utils.IsHeadLabel(lb, globalHeadDir);
                        if (offset != 0)
                        {
                            nbrParts[lb] = null;
                        }
                        numBonds++;
                    }
                }

                // Add bond graphics info for each bond
                for (int i = 0; i < numBonds; i++)
                {
                    Vector2Int start = bondOwnHead[i] ? p.Head() : p.Tail();
                    Vector2Int end = bondNbrHead[i] ? nbr.Head() : nbr.Tail();
                    p.bondGraphicInfo.Add(new ParticleBondGraphicState(start, end, start, end));
                }

                // Enqueue the neighbor if necessary
                if (!nbr.queuedForJMProcessing)
                {
                    queue.Enqueue(nbr);
                    nbr.queuedForJMProcessing = true;
                }
            }

            p.processedJointMovement = true;
        }
    }

    /// <summary>
    /// Sets all global bonds of all particles so that
    /// their initial bonds can be computed easily.
    /// </summary>
    public void SetInitialParticleBonds()
    {
        foreach (Particle p in particles)
        {
            for (int i = 0; i < 10; i++)
                p.activeBondsGlobal[i] = true;
        }
    }

    /// <summary>
    /// Sets the bond graphics info of all particles to the
    /// currently loaded state so that it can be displayed.
    /// If animations are required, the joint movement info is
    /// also loaded and set up to produce the correct animations.
    /// This should be called when stepping or jumping through
    /// the history.
    /// </summary>
    /// <param name="withAnimation">Flag indicating whether the
    /// loaded movement information should include an animation
    /// or not.</param>
    private void LoadMovementGraphicsInfo(bool withAnimation)
    {
        foreach (Particle p in particles)
        {
            BondMovementInfoList bondInfo = p.GetCurrentBondGraphicsInfo();
            JointMovementInfo movementInfo = p.GetCurrentMovementGraphicsInfo();
            Vector2Int beforeOffset = p.Tail() - movementInfo.jmOffset;
            Vector2Int afterOffset = p.Tail();
            foreach (BondMovementInfo bmi in bondInfo.bondMovements)
            {
                if (withAnimation)
                    p.bondGraphicInfo.Add(new ParticleBondGraphicState(bmi.start2 + afterOffset, bmi.end2 + afterOffset, bmi.start1 + beforeOffset, bmi.end1 + beforeOffset));
                else
                    p.bondGraphicInfo.Add(new ParticleBondGraphicState(bmi.start2 + afterOffset, bmi.end2 + afterOffset, bmi.start2 + afterOffset, bmi.end2 + afterOffset));
            }
            // Also load joint movement info if required
            if (withAnimation)
            {
                p.jmOffset = movementInfo.jmOffset;
                p.movementOffset = movementInfo.movementOffset;
                if (movementInfo.movementAction != ActionType.NULL)
                    p.ScheduledMovement = new ParticleAction(p, movementInfo.movementAction);
            }
        }
    }

    // TODO: Circuit computation and beep/message handling should probably be done separately

    /// <summary>
    /// Uses the particles' pin configurations to determine
    /// all circuits in the system from scratch and send the
    /// planned beeps and messages accordingly.
    /// <para>
    /// The algorithm performs a breadth-first search on each
    /// connected component of the particle system to assign
    /// every partition set of each particle to a circuit,
    /// merging the circuits if they happen to be connected
    /// when a partition set is added.
    /// </para>
    /// </summary>
    /// <param name="sendBeepsAndMessages">Determines whether beeps
    /// and messages are sent. Set to <c>false</c> to only recompute
    /// the current circuits and update their graphics info.</param>
    public void DiscoverCircuits(bool sendBeepsAndMessages)
    {
        float tStart = Time.realtimeSinceStartup;
        List<Circuit> circuits = new List<Circuit>(particles.Count * 6);

        // Go through all particles, start search for each unfinished one
        foreach (Particle particle in particles)
        {
            if (particle.processedPinConfig)
            {
                continue;
            }
            // Particle is not finished, start BFS and collect circuits
            Queue<Particle> queue = new Queue<Particle>();
            particle.queuedForPinConfigProcessing = true;
            queue.Enqueue(particle);
            while (queue.Count > 0)
            {
                Particle p = queue.Dequeue();
                Direction globalHeadDir = p.GlobalHeadDirection();
                int pinsPerEdge = p.algorithm.PinsPerEdge;

                // Initialize graphics computation as well
                p.gCircuit = ParticlePinGraphicState.PoolCreate(pinsPerEdge);
                p.gCircuit.neighbor1ToNeighbor2Direction = p.GlobalTailDirection().ToInt();

                // First of all, find all neighboring particles and some relative positional information
                int numNbrs = p.IsExpanded() ? 10 : 6;
                Particle[] nbrParts = new Particle[numNbrs];
                bool[] nbrHead = new bool[numNbrs]; // True if the neighbor's head is at this position
                int[] nbrLabels = new int[numNbrs]; // Stores neighbor label opposite of our label
                for (int label = 0; label < numNbrs; label++)
                {
                    Direction dir = ParticleSystem_Utils.GetDirOfLabel(label, globalHeadDir);
                    int dirInt = dir.ToInt();
                    bool head = ParticleSystem_Utils.IsHeadLabel(label, globalHeadDir);
                    Vector2Int nbrPos = ParticleSystem_Utils.GetNbrInDir(head ? p.Head() : p.Tail(), dir);
                    if (particleMap.TryGetValue(nbrPos, out Particle nbr))
                    {
                        // Has neighbor in this position, record in circuit graphics info
                        if (p.IsExpanded() && !head)
                        {
                            // neighbor2 is tail
                            p.gCircuit.hasNeighbor2[dirInt] = true;
                        }
                        else
                        {
                            // neighbor1 is head
                            p.gCircuit.hasNeighbor1[dirInt] = true;
                        }

                        // If the neighbor has already been processed: Compute required information
                        if (nbr.processedPinConfig)
                        {
                            nbrParts[label] = nbr;
                            bool isNbrHead = nbr.Head() == nbrPos;
                            nbrHead[label] = isNbrHead;
                            nbrLabels[label] = ParticleSystem_Utils.GetLabelInDir(dir.Opposite(), nbr.GlobalHeadDirection(), isNbrHead);
                        }
                        // Otherwise: Enqueue if necessary
                        else
                        {
                            if (!nbr.queuedForPinConfigProcessing)
                            {
                                nbr.queuedForPinConfigProcessing = true;
                                queue.Enqueue(nbr);
                            }
                            nbrParts[label] = null;
                        }
                    }
                    else
                    {
                        nbrParts[label] = null;
                    }
                }

                // Have found all neighbors, now determine circuits into which the partition sets belong
                foreach (SysPartitionSet ps in p.PinConfiguration.partitionSets)
                {
                    if (ps.IsEmpty())
                    {
                        continue;
                    }
                    // Partition set contains at least one pin
                    // For each pin, find out if it is connected to a partition set of an
                    // already processed neighbor
                    // Put the partition set into the first encountered circuit
                    // Merge this circuit with any other circuits encountered on the way
                    // If no circuit is encountered, create a new one for the partition set
                    bool foundCircuit = false;
                    foreach (Pin _pin in ps.GetPins())
                    {
                        SysPin pin = (SysPin) _pin;
                        int pinLabel = pin.globalLabel;
                        int pinOffset = pin.globalEdgeOffset;
                        if (nbrParts[pinLabel] != null)
                        {
                            // Find the neighbor's corresponding pin
                            int nbrPinLabel = nbrLabels[pinLabel];
                            int nbrPinId = nbrPinLabel * pinsPerEdge + pinsPerEdge - 1 - pinOffset;
                            SysPin nbrPin = nbrParts[pinLabel].PinConfiguration.pinsGlobal[nbrPinId];
                            // If we have not found a circuit yet, add our partition set to the circuit
                            if (!foundCircuit)
                            {
                                circuits[nbrPin.partitionSet.circuit].AddPartitionSet(ps);
                                foundCircuit = true;
                            }
                            // Otherwise merge our circuit with the neighbor's
                            else
                            {
                                circuits[nbrPin.partitionSet.circuit].MergeWith(circuits[ps.circuit]);
                            }
                        }
                    }
                    // If we have not found a circuit after going through all pins, create a new circuit
                    if (!foundCircuit)
                    {
                        Circuit c = Circuit.Get(circuits.Count);
                        c.AddPartitionSet(ps);
                        circuits.Add(c);
                    }
                }
                p.processedPinConfig = true;
            }
        }
        string s = "Found " + circuits.Count + " circuits in " + (Time.realtimeSinceStartup - tStart) + " seconds\n";

        Debug.Log(s);

        // Apply colors to circuits
        int colIdx = 0;
        foreach (Circuit c in circuits)
        {
            if (!c.IsRoot())
                continue;

            // Only apply color to circuits with more than 2 partition sets
            if (c.NumPartitionSets > 2 && !c.HasColorOverride())
            {
                c.SetColor(ColorData.Circuit_Colors[colIdx]);
                colIdx = (colIdx + 1) % ColorData.Circuit_Colors.Length;
            }
        }


        // Send beeps and messages
        // Also complete graphics information for all particles
        foreach (Particle p in particles)
        {
            // Compute each partition set
            foreach (SysPartitionSet ps in p.PinConfiguration.partitionSets)
            {
                if (ps.IsEmpty()) continue;

                Circuit circ = circuits[ps.circuit];

                if (sendBeepsAndMessages)
                {
                    if (circ.HasBeep())
                        p.ReceiveBeep(ps.Id);
                    Message msg = circ.GetMessage();
                    if (msg != null)
                        p.ReceiveMessage(ps.Id, msg);
                }

                // Singleton sets must be created independently
                if (ps.NumStoredPins == 1)
                {
                    SysPin pin = ps.GetPins()[0] as SysPin;
                    Direction pinDir = ParticleSystem_Utils.GetDirOfLabel(pin.globalLabel, p.GlobalHeadDirection());
                    ParticlePinGraphicState.PSetData pset = ParticlePinGraphicState.PSetData.PoolCreate();
                    pset.UpdatePSetData(
                        circ.GetColor(),
                        // TODO: Visualize messages differently?
                        circ.HasBeep() || circ.GetMessage() != null,
                        p.HasPlannedBeep(ps.Id) || p.HasPlannedMessage(ps.Id),
                        new ParticlePinGraphicState.PinDef[] { new ParticlePinGraphicState.PinDef(pinDir.ToInt(), pin.globalEdgeOffset, pin.head) });
                    p.gCircuit.singletonSets.Add(pset);
                }
                // Partition Set with more than one pin
                else
                {
                    ParticlePinGraphicState.PinDef[] pins = new ParticlePinGraphicState.PinDef[ps.NumStoredPins];
                    int i = 0;
                    foreach (Pin _pin in ps.GetPins())
                    {
                        SysPin pin = _pin as SysPin;
                        Direction pinDir = ParticleSystem_Utils.GetDirOfLabel(pin.globalLabel, p.GlobalHeadDirection());
                        pins[i].globalDir = pinDir.ToInt();
                        pins[i].dirID = pin.globalEdgeOffset;
                        pins[i].isHead = pin.head;
                        i++;
                    }
                    ParticlePinGraphicState.PSetData pset = ParticlePinGraphicState.PSetData.PoolCreate();
                    pset.UpdatePSetData(
                        circ.GetColor(),
                        // TODO: Visualize messages differently?
                        circ.HasBeep() || circ.GetMessage() != null,
                        p.HasPlannedBeep(ps.Id) || p.HasPlannedMessage(ps.Id),
                        pins);
                    p.gCircuit.partitionSets.Add(pset);
                }
            }
        }

        // Free the circuits
        foreach (Circuit c in circuits)
        {
            Circuit.Free(c);
        }

        totalCircuitCompTime += Time.realtimeSinceStartup - tStart;
    }

    /// <summary>
    /// Resets the helper information of all particles to prepare for
    /// the simulation of the next round.
    /// </summary>
    public void CleanupAfterRound()
    {
        // Remove simulation state flags from all particles
        foreach (Particle p in particles)
        {
            p.hasMoved = false;
            p.processedJointMovement = false;
            p.queuedForJMProcessing = false;
            p.processedPinConfig = false;
            p.queuedForPinConfigProcessing = false;
            p.ScheduledMovement = null;
            p.markedForAutomaticBonds = false;
            // Also reset attribute intermediate values
            p.ResetAttributeIntermediateValues();

            p.bondGraphicInfo.Clear();
        }
    }

    private bool HasSimulationFinished()
    {
        foreach (Particle p in particles)
        {
            try
            {
                if (!p.IsFinished())
                    return false;
            }
            catch (ParticleException pe)
            {
                Log.Error("Exception caught during termination check: " + pe + "\n" + pe.StackTrace + "\nParticle: " +
                    (pe.particle == null ? "NULL" : (pe.particle.Head() + ", " + pe.particle.Tail())));
                return false;
            }
            catch (System.Exception e)
            {
                Log.Error("Exception caught during termination check: " + e + "\n" + e.StackTrace);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Stores the released and marked bonds and resets
    /// them for each particle. Use this to prepare the
    /// particles for the joint movements in the next round.
    /// </summary>
    public void FinishMovementInfo()
    {
        foreach (Particle p in particles)
        {
            p.StoreAndResetMovementInfo();
        }
    }

    /// <summary>
    /// Stores the received beeps and messages and resets the
    /// planned beeps and messages for each particle. Use this to
    /// prepare the particles for the transmission and reception of
    /// beeps and messages in the next round.
    /// </summary>
    public void FinishBeepAndMessageInfo()
    {
        foreach (Particle p in particles)
        {
            p.StoreBeepsAndMessages();
            p.ResetPlannedBeepsAndMessages();
        }
    }

    /// <summary>
    /// Triggers a graphics update for each particle in the system.
    /// </summary>
    public void UpdateAllParticleVisuals(bool resetVisuals)
    {
        // TODO: Maybe only update particles with changes
        // First update position info, then update circuit data
        foreach (Particle p in particles)
        {
            p.graphics.SetParticleColor(p.GetParticleColor());
            if (resetVisuals) p.graphics.UpdateReset();
            else
            {
                // Direction is expansion direction for expansions and the opposite
                // movement direction for contractions
                int contractionDir = -1;
                if (p.ScheduledMovement != null)
                {
                    if (p.ScheduledMovement.type == ActionType.EXPAND || p.ScheduledMovement.type == ActionType.PUSH)
                        contractionDir = ParticleSystem_Utils.VectorToDirection(p.movementOffset).ToInt();
                    else
                        contractionDir = ParticleSystem_Utils.VectorToDirection(p.movementOffset).Opposite().ToInt();
                }
                else if (p.IsExpanded())
                    contractionDir = p.GlobalHeadDirectionInt();

                ParticleJointMovementState pjms = p.jmOffset != Vector2Int.zero ? new ParticleJointMovementState(true, p.jmOffset) : ParticleJointMovementState.None;
                ParticleMovementState pms = new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), contractionDir, pjms);
                p.graphics.Update(pms);
            }
        }
        foreach (Particle p in particles)
        {
            foreach (ParticleBondGraphicState pbgs in p.bondGraphicInfo)
                p.graphics.BondUpdate(pbgs);
        }
        foreach (Particle p in particles)
        {
            p.graphics.CircuitUpdate(p.gCircuit);
        }
        renderSystem.ParticleMovementOver();
        renderSystem.CircuitCalculationOver();
    }

    #endregion

    /**
     * Particle functions (called by particles to get information or trigger actions)
     */

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.HasNeighborAt(int, bool)"/>.
    /// <para>See also <seealso cref="GetNeighborAt(Particle, Direction, bool)"/>.</para>
    /// </summary>
    /// <param name="p">The particle checking for a neighbor.</param>
    /// <param name="locDir">The local direction of the particle in which to check.</param>
    /// <param name="fromHead">If <c>true</c>, check relative to <paramref name="p"/>'s head,
    /// otherwise check relative to the tail.</param>
    /// <returns><c>true</c> if and only if the node in local direction <paramref name="locDir"/>
    /// relative to <paramref name="p"/>'s head or tail is occupied by a particle other
    /// than <paramref name="p"/>.</returns>
    public bool HasNeighborAt(Particle p, Direction locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
        // Return true iff there is a particle at that position and it is not the
        // same as the querying particle
        return particleMap.TryGetValue(pos, out Particle nbr) && nbr != p;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.GetNeighborAt(Direction, bool)"/>.
    /// <para>See also <seealso cref="HasNeighborAt(Particle, Direction, bool)"/>.</para>
    /// </summary>
    /// <param name="p">The particle trying to get its neighbor.</param>
    /// <param name="locDir">The local direction of the particle in which to check.</param>
    /// <param name="fromHead">If <c>true</c>, check relative to <paramref name="p"/>'s head,
    /// otherwise check relative to the tail.</param>
    /// <returns>The neighboring particle in the specified position, if it exists,
    /// otherwise <c>null</c>.</returns>
    public Particle GetNeighborAt(Particle p, Direction locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
        if (particleMap.TryGetValue(pos, out Particle nbr) && nbr != p)
            return nbr;
        else
            return null;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.IsHeadAt(int, bool)"/>.
    /// </summary>
    /// <param name="p">The particle checking for a neighbor's head.</param>
    /// <param name="locDir">The local direction of the particle in which to check.</param>
    /// <param name="fromHead">If <c>true</c>, check relative to <paramref name="p"/>'s head,
    /// otherwise check relative to the tail.</param>
    /// <returns><c>true</c> if and only if the node in local direction <paramref name="locDir"/>
    /// relative to <paramref name="p"/>'s head or tail is occupied by the head of a particle
    /// other than <paramref name="p"/>.</returns>
    public bool IsHeadAt(Particle p, Direction locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
        return particleMap.TryGetValue(pos, out Particle nbr) && nbr != p && nbr.Head() == pos;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.IsTailAt(int, bool)"/>.
    /// </summary>
    /// <param name="p">The particle checking for a neighbor's tail.</param>
    /// <param name="locDir">The local direction of the particle in which to check.</param>
    /// <param name="fromHead">If <c>true</c>, check relative to <paramref name="p"/>'s head,
    /// otherwise check relative to the tail.</param>
    /// <returns><c>true</c> if and only if the node in local direction <paramref name="locDir"/>
    /// relative to <paramref name="p"/>'s head or tail is occupied by the tail of a particle
    /// other than <paramref name="p"/>.</returns>
    public bool IsTailAt(Particle p, Direction locDir, bool fromHead = true)
    {
        Vector2Int pos = ParticleSystem_Utils.GetNeighborPosition(p, locDir, fromHead);
        return particleMap.TryGetValue(pos, out Particle nbr) && nbr != p && nbr.Tail() == pos;
    }

    /// <summary>
    /// Iterates through the neighbor nodes of a given particle and returns the
    /// encountered particles.
    /// </summary>
    /// <typeparam name="T">The type of particles to search for, must be
    /// <see cref="ParticleAlgorithm"/> subclass.</typeparam>
    /// <param name="p">The particle searching for neighbors.</param>
    /// <param name="localStartDir">The local direction of <paramref name="p"/>
    /// indicating the place where the search should start.</param>
    /// <param name="startAtHead">If <c>true</c>, the search starts at <paramref name="p"/>'s
    /// head, otherwise it starts at its tail (no effect for contracted particles).</param>
    /// <param name="withChirality">If <c>true</c>, the search progresses in the same
    /// direction as <paramref name="p"/>'s chirality, otherwise it progresses in the
    /// opposite direction.</param>
    /// <param name="maxSearch">The maximum number of nodes to search.</param>
    /// <param name="maxReturn">The maximum number of neighbors to return.</param>
    /// <returns>Every neighbor <see cref="ParticleAlgorithm"/> encountered during the
    /// search, each wrapped in a <see cref="Neighbor{T}"/> instance.</returns>
    private IEnumerable<Neighbor<T>> IterateNeighbors<T>(Particle p, Direction localStartDir, bool startAtHead, bool withChirality, int maxSearch, int maxReturn) where T : ParticleAlgorithm
    {
        if (maxSearch > 6 && !p.IsExpanded() || maxSearch > 10)
        {
            Debug.LogWarning("Searching for " + maxSearch + " neighbors could lead to duplicate results!");
        }
        int numSearched = 0;
        int numReturned = 0;
        Direction currentGlobalDir = ParticleSystem_Utils.LocalToGlobalDir(localStartDir, p.comDir, p.chirality);
        Vector2Int refNode = startAtHead ? p.Head() : p.Tail();
        bool atHead = startAtHead;

        //int directionIncr = ((withChirality ? 1 : -1) * (p.chirality ? 1 : -1) + 6) % 6;
        int directionIncr = (withChirality ? 1 : -1) * (p.chirality ? 1 : -1);
        while (numSearched < maxSearch && numReturned < maxReturn)
        {
            // Must switch nodes if we have reached the point where we look at the other one
            if (p.IsExpanded() && (atHead && currentGlobalDir == p.GlobalTailDirection() || !atHead && currentGlobalDir == p.GlobalHeadDirection()))
            {
                atHead = !atHead;
                // Turn twice against the current turn direction, i.e., 4 times in current turn direction
                //currentGlobalDir = (currentGlobalDir + 4 * directionIncr) % 6;
                currentGlobalDir = currentGlobalDir.Rotate60(-2 * directionIncr);
            }

            // Check the next position
            Vector2Int nbrPos = ParticleSystem_Utils.GetNbrInDir(refNode, currentGlobalDir);
            if (particleMap.TryGetValue(nbrPos, out Particle nbr))
            {
                yield return new Neighbor<T>((T)nbr.algorithm, ParticleSystem_Utils.GlobalToLocalDir(currentGlobalDir, p.comDir, p.chirality), atHead);
                numReturned++;
            }
            
            currentGlobalDir = currentGlobalDir.Rotate60(directionIncr);
            numSearched++;
        }
    }

    // TODO: Documentation

    public bool FindFirstNeighbor<T>(Particle p, out Neighbor<T> neighbor, Direction startDir = Direction.NONE, bool startAtHead = true, bool withChirality = true, int maxNumber = -1) where T : ParticleAlgorithm
    {
        if (startDir == Direction.NONE)
            startDir = DirectionHelpers.Cardinal(0);
        if (maxNumber == -1)
        {
            maxNumber = p.IsExpanded() ? 10 : 6;
        }
        foreach(Neighbor<T> nbr in IterateNeighbors<T>(p, startDir, startAtHead, withChirality, maxNumber, maxNumber))
        {
            neighbor = nbr;
            return true;
        }
        neighbor = new Neighbor<T>(null, Direction.NONE, true);
        return false;
    }

    public bool FindFirstNeighborWithProperty<T>(Particle p, System.Func<T, bool> prop, out Neighbor<T> neighbor, Direction startDir = Direction.NONE, bool startAtHead = true, bool withChirality = true, int maxNumber = -1) where T : ParticleAlgorithm
    {
        if (startDir == Direction.NONE)
            startDir = DirectionHelpers.Cardinal(0);
        if (maxNumber == -1)
        {
            maxNumber = p.IsExpanded() ? 10 : 6;
        }
        foreach (Neighbor<T> nbr in IterateNeighbors<T>(p, startDir, startAtHead, withChirality, maxNumber, maxNumber))
        {
            if (prop(nbr.neighbor))
            {
                neighbor = nbr;
                return true;
            }
        }
        neighbor = new Neighbor<T>(null, Direction.NONE, true);
        return false;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.Expand(Direction)"/>.
    /// <para>
    /// Schedules a <see cref="ParticleAction"/> to expand the given particle in the
    /// specified direction if the action is applicable.
    /// An expansion action is definitely not applicable if the particle is already
    /// expanded.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the particle is already expanded.
    /// </exception>
    /// <param name="p">The particle that should expand.</param>
    /// <param name="locDir">The local direction into which the particle should expand.</param>
    public void ExpandParticle(Particle p, Direction locDir)
    {
        // Reject if the particle is already expanded
        if (p.IsExpanded())
        {
            throw new InvalidActionException(p, "Expanded particle cannot expand again.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Expanding particle already has a scheduled movement.");
        }

        // Store expansion action in particle
        ParticleAction a = new ParticleAction(p, ActionType.EXPAND, locDir);
        p.ScheduledMovement = a;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.ContractHead"/>.
    /// <para>
    /// Schedules a <see cref="ParticleAction"/> to contract the given particle into
    /// its head if the action is applicable.
    /// A contraction action is definitely not applicable if the particle is already
    /// contracted.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the particle is already contracted.
    /// </exception>
    /// <param name="p">The particle that should contract.</param>
    public void ContractParticleHead(Particle p)
    {
        ContractParticle(p, true);
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.ContractTail"/>.
    /// <para>
    /// Schedules a <see cref="ParticleAction"/> to contract the given particle into
    /// its tail if the action is applicable.
    /// A contraction action is definitely not applicable if the particle is already
    /// contracted.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the particle is already contracted.
    /// </exception>
    /// <param name="p">The particle that should contract.</param>
    public void ContractParticleTail(Particle p)
    {
        ContractParticle(p, false);
    }

    private void ContractParticle(Particle p, bool head)
    {
        // Reject if the particle is already contracted
        if (p.IsContracted())
        {
            throw new InvalidActionException(p, "Contracted particle cannot contract again.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Contracting particle already has a scheduled movement.");
        }

        // Store contraction action in particle
        ParticleAction a = new ParticleAction(p, head ? ActionType.CONTRACT_HEAD : ActionType.CONTRACT_TAIL);
        p.ScheduledMovement = a;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.PushHandover(int)"/>.
    /// <para>
    /// Schedules a <see cref="ParticleAction"/> to expand the given particle in the
    /// specified direction, pushing away an expanded neighbor, if the action is applicable.
    /// A push handover action is definitely not applicable if the particle is already
    /// expanded or the node in expansion direction is not occupied by an expanded
    /// particle.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the particle is already expanded or the node in expansion direction
    /// is not occupied by an expanded particle.
    /// </exception>
    /// <param name="p">The particle that should expand during the handover.</param>
    /// <param name="locDir">The local direction into which the particle should expand.</param>
    public void PerformPushHandover(Particle p, Direction locDir)
    {
        // Reject if the particle is already expanded
        if (p.IsExpanded())
        {
            throw new InvalidActionException(p, "Expanded particle cannot perform a push handover.");
        }

        // Reject if there is no expanded particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);
        if (!particleMap.TryGetValue(targetLoc, out Particle p2) || p2.IsContracted())
        {
            throw new InvalidActionException(p, "Particle cannot perform push handover onto node occupied by no or contracted particle.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Particle scheduling push handover already has a scheduled movement.");
        }

        // Store push handover action in particle
        ParticleAction a = new ParticleAction(p, ActionType.PUSH, locDir);
        p.ScheduledMovement = a;
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.PullHandoverHead(int)"/>.
    /// <para>
    /// Schedules a <see cref="ParticleAction"/> to contract the given particle into
    /// its head, pulling a contracted neighbor into its tail position, if the action
    /// is applicable.
    /// A pull handover action is definitely not applicable if the particle is already
    /// contracted or there is no contracted neighbor in the specified direction.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the particle is already contracted or there is no contracted
    /// neighbor in the specified local direction relative to the particle's tail.
    /// </exception>
    /// <param name="p">The particle that should contract during the handover.</param>
    /// <param name="locDir">The local direction relative to <paramref name="p"/>'s tail
    /// from where a contracted neighbor particle should be expanded to occupy the
    /// tail node.</param>
    public void PerformPullHandoverHead(Particle p, Direction locDir)
    {
        PerformPullHandover(p, locDir, true);
    }

    /// <summary>
    /// System-side implementation of <see cref="ParticleAlgorithm.PullHandoverTail(int)"/>.
    /// <para>
    /// Schedules a <see cref="ParticleAction"/> to contract the given particle into
    /// its tail, pulling a contracted neighbor into its head position, if the action
    /// is applicable.
    /// A pull handover action is definitely not applicable if the particle is already
    /// contracted or there is no contracted neighbor in the specified direction.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the particle is already contracted or there is no contracted
    /// neighbor in the specified local direction relative to the particle's head.
    /// </exception>
    /// <param name="p">The particle that should contract during the handover.</param>
    /// <param name="locDir">The local direction relative to <paramref name="p"/>'s head
    /// from where a contracted neighbor particle should be expanded to occupy the
    /// head node.</param>
    public void PerformPullHandoverTail(Particle p, Direction locDir)
    {
        PerformPullHandover(p, locDir, false);
    }

    private void PerformPullHandover(Particle p, Direction locDir, bool head)
    {
        // Reject if the particle is already contracted
        if (p.IsContracted())
        {
            throw new InvalidActionException(p, "Contracted particle cannot perform pull handover.");
        }

        // Reject if there is no contracted particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, !head);
        if (!particleMap.TryGetValue(targetLoc, out Particle p2) || p2.IsExpanded())
        {
            throw new InvalidActionException(p, "Particle cannot perform pull handover onto node occupied by no or expanded particle.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Particle scheduling pull handover already has a scheduled movement.");
        }

        // Store pull handover action in particle
        ParticleAction a = new ParticleAction(p, head ? ActionType.PULL_HEAD : ActionType.PULL_TAIL, locDir);
        p.ScheduledMovement = a;
    }


    // <<<TEMPORARY: FOR DEBUGGING VALUE HISTORIES>>>
    public void Print()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Debug.Log("========== Particle " + i + " ==========");
            particles[i].Print();
        }
    }

    /**
     * Other system info and control
     */

    /// <summary>
    /// Calculates the average world position of all particles in the system.
    /// </summary>
    /// <returns>The average world of all particles. Will be
    /// <c>(0, 0)</c> if there are no particles.</returns>
    public Vector2 CenterPosition()
    {
        Vector2 avg = Vector2.zero;

        ICollection<Vector2Int> positions = inInitializationState ? particleMapInit.Keys : particleMap.Keys;

        foreach (Vector2Int pos in positions)
        {
            avg += pos;
        }

        if (positions.Count > 0)
            avg /= positions.Count;

        return AmoebotFunctions.CalculateAmoebotCenterPositionVector2(avg.x, avg.y);
    }

    /// <summary>
    /// Calculates the center position of the bounding box that encloses
    /// the particle system.
    /// <returns>The center of the bounding box of the system in world coordinates.
    /// Will be <c>(0, 0)</c> if there are no particles.</returns>
    public Vector2 BBoxCenterPosition()
    {
        ICollection<Vector2Int> positions = inInitializationState ? particleMapInit.Keys : particleMap.Keys;

        if (positions.Count == 0)
            return Vector2.zero;

        float xMin = float.PositiveInfinity;
        float xMax = float.NegativeInfinity;
        float yMin = float.PositiveInfinity;
        float yMax = float.NegativeInfinity;

        foreach (Vector2Int pos in positions)
        {
            Vector2 abs = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(pos);
            if (abs.x < xMin)
                xMin = abs.x;
            else if (abs.x > xMax)
                xMax = abs.x;

            if (abs.y < yMin)
                yMin = abs.y;
            else if (abs.y > yMax)
                yMax = abs.y;
        }

        return new Vector2((xMin + xMax) / 2.0f, (yMin + yMax) / 2.0f);
    }

    /// <summary>
    /// Returns the world coordinates of the system's current seed particle.
    /// </summary>
    /// <returns>The world coordinates of the system's seed particle, if it
    /// exists, otherwise <c>(0, 0)</c>.</returns>
    public Vector2 SeedPosition()
    {
        Vector2 result;
        IParticleState seed = null;
        int n;
        if (inInitializationState)
        {
            n = particlesInit.Count;
            if (n > 0)
                seed = particlesInit[0];
        }
        else
        {
            n = particles.Count;
            if (n > 0)
                seed = particles[0];
        }

        if (n > 0)
            result = 0.5f * (Vector2)(seed.Head() + seed.Tail());
        else
            result = Vector2.zero;

        return AmoebotFunctions.CalculateAmoebotCenterPositionVector2(result.x, result.y);
    }


    /**
     * IReplayHistory implementation
     */

    // TODO: Test this thoroughly
    // TODO: Maybe find better way to update particleMap

    public int GetFirstRecordedRound()
    {
        return _earliestRound;
    }

    public bool IsTracking()
    {
        return isTracking;
    }

    public void SetMarkerToRound(int round)
    {
        if (round < _earliestRound || round > _latestRound)
        {
            throw new SimulatorStateException("Cannot set system to round " + round + "; must be between " + _earliestRound + " and " + _latestRound);
        }
        if (round != _currentRound)
        {
            if (round == _latestRound)
            {
                ContinueTracking();
                return;
            }
            _currentRound = round;
            _previousRound = round;
            // Set all particles to track the given round
            // Have to update the particleMap as well
            particleMap.Clear();
            foreach (Particle p in particles)
            {
                p.SetMarkerToRound(round);
                particleMap[p.Tail()] = p;
                if (p.IsExpanded())
                {
                    particleMap[p.Head()] = p;
                }
            }
            isTracking = false;
            // TODO: Puth this into a helper method?
            DiscoverCircuits(false);
            LoadMovementGraphicsInfo(false);
            UpdateAllParticleVisuals(true);
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
        }
    }

    public void StepBack()
    {
        if (_currentRound == _earliestRound)
        {
            throw new SimulatorStateException("Cannot step back because the system is in the earliest round " + _earliestRound);
        }
        // Have to synchronize to given round if we are still tracking
        // (just to be sure, particles could be in different rounds while tracking)
        if (isTracking)
        {
            SetMarkerToRound(_currentRound - 1);
        }
        // Otherwise particles are already synchronized
        else
        {
            _currentRound--;
            _previousRound = _currentRound;
            // Reset all particles by one round and update particleMap
            particleMap.Clear();
            foreach (Particle p in particles)
            {
                p.StepBack();
                particleMap[p.Tail()] = p;
                if (p.IsExpanded())
                {
                    particleMap[p.Head()] = p;
                }
            }
            isTracking = false;
            DiscoverCircuits(false);
            LoadMovementGraphicsInfo(false);
            UpdateAllParticleVisuals(true);
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
            CleanupAfterRound();
        }
    }

    public void StepForward()
    {
        if (_currentRound == _latestRound)
        {
            throw new SimulatorStateException("Cannot step forward because the system is in the latest round " + _latestRound);
        }
        if (_currentRound == _latestRound - 1)
        {
            ContinueTracking();
        }
        else
        {
            _currentRound++;
            _previousRound = _currentRound;
            // Advance all particles by one round and update particleMap
            particleMap.Clear();
            foreach (Particle p in particles)
            {
                p.StepForward();
                particleMap[p.Tail()] = p;
                if (p.IsExpanded())
                {
                    particleMap[p.Head()] = p;
                }
            }
            isTracking = false;
            DiscoverCircuits(false);
            LoadMovementGraphicsInfo(true);
            UpdateAllParticleVisuals(false);
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
        }
    }

    public int GetMarkedRound()
    {
        return _currentRound;
    }

    public void ContinueTracking()
    {
        if (!isTracking)
        {
            bool stepFromSecondLastRound = (_currentRound == _latestRound - 1);
            _currentRound = _latestRound;
            _previousRound = _currentRound;
            particleMap.Clear();
            foreach (Particle p in particles)
            {
                p.ContinueTracking();
                particleMap[p.Tail()] = p;
                if (p.IsExpanded())
                {
                    particleMap[p.Head()] = p;
                }
            }
            isTracking = true;
            DiscoverCircuits(false);
            LoadMovementGraphicsInfo(stepFromSecondLastRound);
            UpdateAllParticleVisuals(!stepFromSecondLastRound);
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
        }
    }

    public void CutOffAtMarker()
    {
        if (!isTracking && _currentRound < _latestRound)
        {
            _latestRound = _currentRound;
            foreach (Particle p in particles)
            {
                p.CutOffAtMarker();
                p.ContinueTracking();
                // We must reset the planned beeps and messages because they have
                // been loaded for visualization
                p.ResetPlannedBeepsAndMessages();
            }
            isTracking = true;

            // Also reset finished state if necessary
            if (finished && _latestRound < finishedRound)
            {
                finished = false;
                finishedRound = -1;
            }
        }
    }

    // Warning: Shifting timescale into the negative can cause issue
    // when loading save state (if finishedRound is -1)
    public void ShiftTimescale(int amount)
    {
        _currentRound += amount;
        _previousRound = _currentRound;
        _earliestRound += amount;
        _latestRound += amount;
        foreach (Particle p in particles)
        {
            p.ShiftTimescale(amount);
        }

        if (finished)
            finishedRound += amount;
    }


    /**
     * Saving and loading functionality.
     */

    /// <summary>
    /// Writes the current simulation state including its
    /// entire history into a serializable object.
    /// </summary>
    /// <returns>A serializable object containing the current
    /// simulation state.</returns>
    public SimulationStateSaveData GenerateSaveData()
    {
        SimulationStateSaveData data = new SimulationStateSaveData();

        data.earliestRound = _earliestRound;
        data.latestRound = _latestRound;
        data.finishedRound = finishedRound;

        data.particles = new ParticleStateSaveData[particles.Count];
        for (int i = 0; i < particles.Count; i++)
        {
            data.particles[i] = particles[i].GenerateSaveData();
        }

        return data;
    }

    /// <summary>
    /// Initializes the system to the state that is stored in
    /// the given save data object.
    /// <para>
    /// This only works correctly if the system was reset using
    /// the <see cref="Reset"/> method before.
    /// </para>
    /// </summary>
    /// <param name="data">The object containing the saved
    /// simulation state.</param>
    /// <param name="updateVisuals">If <c>true</c>, the render system
    /// is notified about the updated system immediately after
    /// loading the simulation state.</param>
    public void InitializeFromSaveState(SimulationStateSaveData data, bool updateVisuals = true)
    {
        _earliestRound = data.earliestRound;
        _latestRound = data.latestRound;
        _currentRound = _latestRound;
        _previousRound = _latestRound;

        finishedRound = data.finishedRound;
        if (finishedRound != -1)
            finished = true;

        foreach (ParticleStateSaveData pData in data.particles)
        {
            Particle p = Particle.CreateFromSaveState(this, pData);
            particles.Add(p);
            particleMap.Add(p.Tail(), p);
            if (p.IsExpanded())
            {
                particleMap.Add(p.Head(), p);
            }
        }

        if (updateVisuals)
        {
            DiscoverCircuits(false);
            CleanupAfterRound();
            UpdateAllParticleVisuals(true);
        }
    }


    /**
     * Initialization mode functionality.
     */

    /// <summary>
    /// Invokes the specified system generation method to fill the system
    /// with particles in initialization mode.
    /// </summary>
    /// <param name="methodName">The name of the generation method.</param>
    /// <param name="parameters">The parameters for the generation method.</param>
    public void GenerateParticles(string methodName, object[] parameters)
    {
        if (!inInitializationState)
            return;

        ResetInit();

        InitializationMethodManager.Instance.GenerateSystem(this, methodName, parameters);
    }

    /// <summary>
    /// Switches the system state to initialization mode.
    /// </summary>
    public void InitializationModeStarted()
    {
        if (inInitializationState)
            return;

        // Note: The initialization window has just been opened. So it might be possible to save the state of a running algorithm and convert its particles to the initialization particles
        // in order to be able to use the given state for new algorithms / save the particle configuration.

        particlesInit.Clear();
        particleMapInit.Clear();

        // Use the current system if we have one
        if (particles.Count > 0)
        {
            // Store the simulation state
            SimulationStateSaveData saveData = GenerateSaveData();
            if (SaveStateUtility.Save(saveData, SaveStateUtility.tmpSaveFile))
            {
                storedSimulationState = true;
                storedSimulationRound = _currentRound;
            }
            else
            {
                Log.Warning("Could not save current simulation state.");
            }

            // Use the current system state as starting point for initialization system
            foreach (Particle p in particles)
            {
                OpenInitParticle ip = new OpenInitParticle(this, p.Tail(), p.chirality, p.comDir, p.GlobalHeadDirection());
                particlesInit.Add(ip);
                particleMapInit[p.Tail()] = ip;
                if (p.IsExpanded())
                    particleMapInit[p.Head()] = ip;
                ip.graphics.AddParticle(new ParticleMovementState(ip.Head(), ip.Tail(), ip.IsExpanded(), ip.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
                ip.graphics.UpdateReset();
            }
        }
        // Hide the circuits
        renderSystem.CircuitCalculationOver();

        Reset();
        inInitializationState = true;
    }

    /// <summary>
    /// Exits the initialization mode and initializes the particle
    /// system based on the current configuration.
    /// </summary>
    /// <param name="algorithmName">The name of the selected algorithm.</param>
    public void InitializationModeFinished(string algorithmName)
    {
        if (!inInitializationState)
            return;

        foreach (InitializationParticle ip in particlesInit)
        {
            Particle p = ParticleFactory.CreateParticle(this, algorithmName, ip);
            particles.Add(p);
            particleMap[p.Tail()] = p;
            if (p.IsExpanded())
                particleMap[p.Head()] = p;
        }

        ResetInit();
        storedSimulationState = false;
        inInitializationState = false;

        SetInitialParticleBonds();
        ComputeBondsStatic();
        FinishMovementInfo();
        DiscoverCircuits(false);
        UpdateAllParticleVisuals(true);
        CleanupAfterRound();
    }

    /// <summary>
    /// Aborts the initialization mode and reloads the
    /// previous simulation state if possible.
    /// </summary>
    public void InitializationModeAborted()
    {
        if (!inInitializationState)
            return;

        // Note: The initialization mode has just been aborted and the window is closed. Here the previously saved state could be loaded again to continue with the old algorithm.

        // Unload the temporary initialization system
        ResetInit();

        // Load previous system state if we have one
        if (storedSimulationState)
        {
            storedSimulationState = false;
            SimulationStateSaveData saveData = SaveStateUtility.Load(SaveStateUtility.tmpSaveFile);
            if (saveData == null)
            {
                Debug.LogWarning("Unable to load previous simulation state.");
                return;
            }
            InitializeFromSaveState(saveData, false);
            SetMarkerToRound(storedSimulationRound);
            DiscoverCircuits(false);
            CleanupAfterRound();
            UpdateAllParticleVisuals(true);
        }
        inInitializationState = false;
    }

    public bool IsInLatestRound()
    {
        return CurrentRound == LatestRound;
    }

    /// <summary>
    /// Adds a contracted particle at the given position when in
    /// initialization mode.
    /// </summary>
    /// <param name="gridPos">The grid position where the particle should be placed.</param>
    /// <param name="chirality">The current chirality setting.</param>
    /// <param name="compassDir">The current compass direction setting.</param>
    public void AddParticleContracted(Vector2Int gridPos,
        Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compassDir = Initialization.Compass.E)
    {
        if (!inInitializationState)
        {
            Log.Error("Cannot add particles in simulation mode.");
            return;
        }

        bool chiralityPart = true;
        if (chirality == Initialization.Chirality.Clockwise ||
            chirality == Initialization.Chirality.Random && Random.Range(0, 2) == 0)
            chiralityPart = false;

        Direction compassDirPart = DirectionHelpers.Cardinal(
            compassDir == Initialization.Compass.Random ? Random.Range(0, 6) :
            (int)compassDir
            );

        AddInitParticle(gridPos, chiralityPart, compassDirPart, Direction.NONE);
    }

    /// <summary>
    /// Adds an expanded particle at the given position when in
    /// initialization mode.
    /// </summary>
    /// <param name="gridPosHead">The grid position of the particle's head.</param>
    /// <param name="gridPosTail">The grid position of the particle's tail.</param>
    /// <param name="chirality">The current chirality setting.</param>
    /// <param name="compassDir">The current compass direction setting.</param>
    public void AddParticleExpanded(Vector2Int gridPosHead, Vector2Int gridPosTail,
        Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compassDir = Initialization.Compass.E)
    {
        if (!inInitializationState)
        {
            Log.Error("Cannot add particles in simulation mode.");
            return;
        }

        bool chiralityPart = true;
        if (chirality == Initialization.Chirality.Clockwise ||
            chirality == Initialization.Chirality.Random && Random.Range(0, 2) == 0)
            chiralityPart = false;

        Direction compassDirPart = DirectionHelpers.Cardinal(
            compassDir == Initialization.Compass.Random ? Random.Range(0, 6) :
            (int)compassDir
            );

        Direction headDirection = ParticleSystem_Utils.VectorToDirection(gridPosHead - gridPosTail);
        if (headDirection == Direction.NONE)
        {
            Log.Error("Invalid positions for expanded particle: " + gridPosHead + ", " + gridPosTail);
            return;
        }

        AddInitParticle(gridPosTail, chiralityPart, compassDirPart, headDirection);
    }

    /// <summary>
    /// General method for adding particles to the system in initialization mode.
    /// </summary>
    /// <param name="tailPos">The grid position of the particle's tail.</param>
    /// <param name="chirality">The chirality of the particle. <c>true</c> means
    /// counter-clockwise and <c>false</c> means clockwise.</param>
    /// <param name="compassDir">The compass direction of the particle. This is
    /// the global direction that corresponds to the particle's local
    /// <see cref="Direction.E"/> direction.</param>
    /// <param name="headDirection">The global direction pointing from the particle's
    /// tail towards its head. Set to <see cref="Direction.NONE"/> to make the
    /// particle contracted.</param>
    /// <returns>The newly added particle or <c>null</c> if the particle could
    /// not be added.</returns>
    public InitializationParticle AddInitParticle(Vector2Int tailPos, bool chirality, Direction compassDir, Direction headDirection = Direction.NONE)
    {
        if (!inInitializationState)
        {
            Log.Error("Cannot add particles in simulation mode.");
            return null;
        }

        OpenInitParticle ip = new OpenInitParticle(this, tailPos, chirality, compassDir, headDirection);

        if (particleMapInit.ContainsKey(ip.Tail()) || ip.IsExpanded() && particleMapInit.ContainsKey(ip.Head()))
        {
            Log.Error("Cannot add particle at " + ip.Tail() + ", " + ip.Head() + ", position is already occupied.");
            return null;
        }

        particlesInit.Add(ip);
        particleMapInit[tailPos] = ip;
        if (headDirection != Direction.NONE)
            particleMapInit[ip.Head()] = ip;
        ip.graphics.AddParticle(new ParticleMovementState(ip.Head(), ip.Tail(), ip.IsExpanded(), ip.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        ip.graphics.UpdateReset();
        return ip;
    }

    /// <summary>
    /// Resets the chirality of all particles to the given value
    /// when in initialization mode.
    /// </summary>
    /// <param name="chirality">The chirality to assign to each
    /// particle. If the value is <see cref="Initialization.Chirality.Random"/>,
    /// the particles get a clockwise chirality with a probability of 50%.</param>
    public void SetSystemChirality(Initialization.Chirality chirality)
    {
        if (!inInitializationState)
        {
            Log.Error("Cannot set system chirality outside of initialization mode.");
            return;
        }

        foreach (InitializationParticle ip in particlesInit)
        {
            bool chiralityPart = true;
            if (chirality == Initialization.Chirality.Clockwise ||
                chirality == Initialization.Chirality.Random && Random.Range(0, 2) == 0)
                chiralityPart = false;
            ip.Chirality = chiralityPart;
        }
    }

    /// <summary>
    /// Resets the compass direction of all particles to the given value
    /// when in initialization mode.
    /// </summary>
    /// <param name="compassDir">The compass direction to assign to each
    /// particle. If the value is <see cref="Initialization.Compass.Random"/>,
    /// each particle gets one of the cardinal directions chosen uniformly
    /// and independently.</param>
    public void SetSystemCompassDir(Initialization.Compass compassDir)
    {
        if (!inInitializationState)
        {
            Log.Error("Cannot set system compass direction outside of initialization mode.");
            return;
        }

        foreach (InitializationParticle ip in particlesInit)
        {
            Direction compass = DirectionHelpers.Cardinal(
                compassDir == Initialization.Compass.Random ? Random.Range(0, 6) : (int)compassDir
                );
            ip.CompassDir = compass;
        }
    }

    /// <summary>
    /// Tries to change a particle's expansion state when in initialization mode.
    /// This method should only be called from within the particle itself so that
    /// it can update its own state after this check is complete.
    /// </summary>
    /// <param name="ip">The initialization particle trying to change its
    /// expansion state.</param>
    /// <param name="newHeadDir">The desired new head direction.</param>
    /// <returns><c>true</c> if and only if changing the particle's expansion
    /// state was successful.</returns>
    public bool TryChangeInitParticleExpansion(InitializationParticle ip, Direction newHeadDir)
    {
        if (newHeadDir == Direction.NONE)
        {
            if (ip.IsExpanded())
                particleMapInit.Remove(ip.Head());
        }
        else if (!newHeadDir.IsCardinal())
        {
            return false;
        }
        else
        {
            if (!ip.IsExpanded() || newHeadDir != ip.ExpansionDir)
            {
                Vector2Int newHeadPos = ParticleSystem_Utils.GetNbrInDir(ip.Tail(), newHeadDir);
                if (TryGetInitParticleAt(newHeadPos, out _))
                {
                    Log.Warning("Cannot expand particle at " + ip.Tail() + " in direction " + newHeadDir + ", position is already occupied.");
                    return false;
                }
                if (ip.IsExpanded())
                    particleMapInit.Remove(ip.Head());
                particleMapInit[newHeadPos] = (OpenInitParticle)ip;
            }
        }

        return true;
    }

    /// <summary>
    /// Adds a new generic parameter with the given initial value
    /// to all particles in initialization mode.
    /// </summary>
    /// <param name="initialValue">The initial value given to
    /// the new parameter.</param>
    public void AddGenericParameter(int initialValue = 0)
    {
        foreach (InitializationParticle ip in particlesInit)
            ip.AddGenericParam(initialValue);
        numGenericParameters++;
    }

    /// <summary>
    /// Removes the generic parameter with the given index from
    /// all particles in initialization mode.
    /// </summary>
    /// <param name="index">The index of the parameter to
    /// be removed.</param>
    public void RemoveGenericParameter(int index)
    {
        if (index < 0 || index >= numGenericParameters)
        {
            Log.Error("Cannot remove generic parameter with index " + index + ", have " + numGenericParameters + " parameters.");
            return;
        }
        foreach (InitializationParticle ip in particlesInit)
            ip.RemoveGenericParam(index);
        numGenericParameters--;
    }

    /// <summary>
    /// Assigns values to the generic parameters of a fixed number of particles.
    /// The values of the other particles can be reset to 0 if desired. Each
    /// chosen particle's value is set to a random value between
    /// <paramref name="minVal"/> and <paramref name="maxVal"/>, inclusively.
    /// </summary>
    /// <param name="paramIdx">The index of the generic parameter to set.</param>
    /// <param name="minVal">The minimum value to be set.</param>
    /// <param name="maxVal">The maximum (inclusive) value to be set. Set to
    /// the same value as <paramref name="minVal"/> to fix the value.</param>
    /// <param name="numParticles">The number of particles to get assigned
    /// a new value.</param>
    /// <param name="reset">Flag indicating whether the parameter values should
    /// be reset to 0 before assigning the new values.</param>
    public void SetGenericParameterFixed(int paramIdx, int minVal, int maxVal, int numParticles, bool reset = true)
    {
        if (paramIdx < 0 || paramIdx >= numGenericParameters)
        {
            Log.Error("Invalid generic parameter index: " + paramIdx + "; have " + numGenericParameters + " parameters.");
            return;
        }

        numParticles = Mathf.Min(numParticles, particlesInit.Count);
        if (numParticles <= 0 && !reset)
            return;

        // Use the selection sampling algorithm to get the right number of particles
        // For choosing k out of n elements randomly, select the first with probability
        // k/n. If it is selected, repeat the algorithm to choose k-1 from the remaining n-1
        // elements. Otherwise, repeat the algorithm to choose k from the n-1 elements.
        int n = particlesInit.Count;
        int k = numParticles;
        foreach (InitializationParticle ip in particlesInit)
        {
            if (reset)
                ip.genericParams[paramIdx] = 0;

            if (k > 0 && Random.Range(0.0f, 1.0f) <= ((float)k) / n)
            {
                ip.genericParams[paramIdx] = Random.Range(minVal, maxVal + 1);
                k--;
            }
            n--;
        }

        // This should not happen, but keep the warning in case it needs to be debugged
        if (k != 0)
        {
            Log.Warning("Selected " + (numParticles - k) + " particles (wanted " + numParticles + ")");
        }
    }

    /// <summary>
    /// Assigns values to the generic parameters of a fixed fraction of particles.
    /// The values of the other particles can be reset to 0 if desired. Each
    /// chosen particle's value is set to a random value between
    /// <paramref name="minVal"/> and <paramref name="maxVal"/>, inclusively.
    /// </summary>
    /// <param name="paramIdx">The index of the generic parameter to set.</param>
    /// <param name="minVal">The minimum value to be set.</param>
    /// <param name="maxVal">The maximum (inclusive) value to be set. Set to
    /// the same value as <paramref name="minVal"/> to fix the value.</param>
    /// <param name="fraction">The fraction of particles to receive a new value.</param>
    /// <param name="reset">Flag indicating whether the parameter values should
    /// be reset to 0 before assigning the new values.</param>
    public void SetGenericParameterFraction(int paramIdx, int minVal, int maxVal, float fraction, bool reset = true)
    {
        int numParticles = Mathf.RoundToInt(fraction * particlesInit.Count);
        SetGenericParameterFixed(paramIdx, minVal, maxVal, numParticles, reset);
    }

    /// <summary>
    /// Assigns values to the generic parameters of randomly chosen particles.
    /// The values of the other particles can be reset to 0 if desired. Each
    /// chosen particle's value is set to a random value between
    /// <paramref name="minVal"/> and <paramref name="maxVal"/>, inclusively.
    /// </summary>
    /// <param name="paramIdx">The index of the generic parameter to set.</param>
    /// <param name="minVal">The minimum value to be set.</param>
    /// <param name="maxVal">The maximum (inclusive) value to be set. Set to
    /// the same value as <paramref name="minVal"/> to fix the value.</param>
    /// <param name="prob">The probability with which each particle is chosen to
    /// get a new value.</param>
    /// <param name="reset">Flag indicating whether the parameter values should
    /// be reset to 0 before assigning the new values.</param>
    public void SetGenericParameterProb(int paramIdx, int minVal, int maxVal, float prob, bool reset = true)
    {
        if (paramIdx < 0 || paramIdx >= numGenericParameters)
        {
            Log.Error("Invalid generic parameter index: " + paramIdx + "; have " + numGenericParameters + " parameters.");
            return;
        }

        if (prob <= 0.0f && !reset)
            return;

        foreach (InitializationParticle ip in particlesInit)
        {
            if (reset)
                ip.genericParams[paramIdx] = 0;

            if (prob > 0.0f && Random.Range(0.0f, 1.0f) <= prob)
            {
                ip.genericParams[paramIdx] = Random.Range(minVal, maxVal + 1);
            }
        }
    }

    /// <summary>
    /// Returns an array storing all current particles in
    /// initialization mode.
    /// </summary>
    /// <returns>An array containing all initialization particles.</returns>
    public InitializationParticle[] GetInitParticles()
    {
        return particlesInit.ToArray();
    }

    /// <summary>
    /// Removes the given particle when in initialization state.
    /// </summary>
    /// <param name="p">The particle to be removed.</param>
    public void RemoveParticle(IParticleState p)
    {
        if (!inInitializationState)
        {
            Log.Warning("Cannot remove particles in simulation mode.");
            return;
        }

        int idx = -1;
        for (int i = 0; i < particlesInit.Count; i++)
        {
            if (particlesInit[i] == p)
            {
                idx = i;
                break;
            }
        }

        if (idx == -1)
        {
            Log.Warning("Could not find particle to be removed.");
            return;
        }

        particleMapInit.Remove(p.Tail());
        if (p.IsExpanded())
            particleMapInit.Remove(p.Head());
        particlesInit[idx].graphics.RemoveParticle();
        particlesInit.RemoveAt(idx);
    }

    /// <summary>
    /// Tries to get the <see cref="InitializationParticle"/> at the given position.
    /// </summary>
    /// <param name="position">The grid position at which to look for the particle.</param>
    /// <param name="particle">The particle at the given position, if it exists,
    /// otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if and only if a particle was found at the given position.</returns>
    public bool TryGetInitParticleAt(Vector2Int position, out InitializationParticle particle)
    {
        if (particleMapInit.TryGetValue(position, out OpenInitParticle p))
        {
            particle = p;
            return true;
        }
        else
        {
            particle = null;
            return false;
        }
    }

    /// <summary>
    /// Moves the given particle to a new location in which it is contracted,
    /// when in initialization mode.
    /// </summary>
    /// <param name="p">The particle to be moved.</param>
    /// <param name="gridPos">The new grid position to which <paramref name="p"/>
    /// should be moved.</param>
    public void MoveParticleToNewContractedPosition(IParticleState p, Vector2Int gridPos)
    {
        if (!inInitializationState)
        {
            Log.Warning("Cannot move particles in simulation mode.");
            return;
        }

        int idx = -1;
        for (int i = 0; i < particlesInit.Count; i++)
        {
            if (particlesInit[i] == p)
            {
                idx = i;
                break;
            }
        }

        if (idx == -1)
        {
            Log.Warning("Could not find particle to be moved.");
            return;
        }

        OpenInitParticle ip = particlesInit[idx];

        if (particleMapInit.TryGetValue(gridPos, out OpenInitParticle prt) && prt != ip)
        {
            Log.Error("Cannot move particle to grid position " + gridPos + ": Already occupied.");
            return;
        }

        // This will already remove the head position from the map if p is expanded
        ip.ExpansionDir = Direction.NONE;

        particleMapInit.Remove(ip.Tail());
        ip.TailPosDirect = gridPos;
        ip.HeadPosDirect = gridPos;
        particleMapInit[gridPos] = ip;
        ip.graphics.UpdateReset();
    }

    /// <summary>
    /// Moves the given particle to a new location in which it is expanded,
    /// when in initialization mode.
    /// </summary>
    /// <param name="p">The particle to be moved.</param>
    /// <param name="gridPosHead">The new grid position to which
    /// <paramref name="p"/>'s head should be moved.</param>
    /// <param name="gridPosTail">The new grid position to which
    /// <paramref name="p"/>'s tail should be moved. Must be
    /// adjacent to <paramref name="gridPosHead"/>.</param>
    public void MoveParticleToNewExpandedPosition(IParticleState p, Vector2Int gridPosHead, Vector2Int gridPosTail)
    {
        if (!inInitializationState)
        {
            Log.Warning("Cannot move particles in simulation mode.");
            return;
        }

        int idx = -1;
        for (int i = 0; i < particlesInit.Count; i++)
        {
            if (particlesInit[i] == p)
            {
                idx = i;
                break;
            }
        }

        if (idx == -1)
        {
            Log.Warning("Could not find particle to be moved.");
            return;
        }

        OpenInitParticle ip = particlesInit[idx];

        if (particleMapInit.TryGetValue(gridPosHead, out OpenInitParticle prt1) && prt1 != ip || particleMapInit.TryGetValue(gridPosTail, out OpenInitParticle prt2) && prt2 != ip)
        {
            Log.Error("Cannot move particle to grid positions " + gridPosHead + ", " + gridPosTail + ": Already occupied.");
            return;
        }

        // This will remove the head position from the map if p is expanded
        ip.ExpansionDir = Direction.NONE;

        Direction newExpDir = ParticleSystem_Utils.VectorToDirection(gridPosHead - gridPosTail);
        particleMapInit.Remove(ip.Tail());
        ip.TailPosDirect = gridPosTail;
        ip.HeadPosDirect = gridPosHead;
        ip.ExpansionDirDirect = newExpDir;
        particleMapInit[gridPosTail] = ip;
        particleMapInit[gridPosHead] = ip;
        ip.graphics.UpdateReset();
    }
}
