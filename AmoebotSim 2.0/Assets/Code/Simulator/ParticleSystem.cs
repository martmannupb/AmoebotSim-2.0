using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// TODO: Move simulation logic into a separate class and use this only as container for particles?

/// <summary>
/// Main container for a system of particles in the grid together with
/// the execution logic for simulating particle activations and entire rounds.
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

    // References
    public AmoebotSimulator sim;
    public RenderSystem renderSystem;

    public List<Particle> particles = new List<Particle>();
    private Dictionary<Vector2Int, Particle> particleMap = new Dictionary<Vector2Int, Particle>();

    public Queue<ParticleAction> actionQueue = new Queue<ParticleAction>();

    public bool useFCFS = true;     // <<<TEMPORARY>>> If true, do not crash on expansion conflicts but simply abort the expansion

    public ParticleSystem(AmoebotSimulator sim, RenderSystem renderSystem)
    {
        this.sim = sim;
        this.renderSystem = renderSystem;
    }


    /**
     * Methods for controlling the system
     */

    /// <summary>
    /// Initializes the system with ExampleParticles for testing purposes.
    /// This should be removed after a proper initialization method has been created.
    /// </summary>
    public void InitializeExample(int width, int height, float spawnProb, int left = 0, int bottom = 0)
    {
        // Fill a "rectangle" randomly with particles (it should be an actual rectangle)
        int num = 0;
        
        for (int col = 0; col < width; ++col)
        {
            for (int row = 0; row < height; ++row)
            {
                if (Random.Range(0f, 1f) <= spawnProb)
                {
                    // TODO: Create functions for adding and removing particles
                    // Don't use column as x coordinate but shift it to stay in a rectangular shape
                    Particle p = ParticleFactory.CreateExampleParticle(this, new Vector2Int(left + col - row / 2, bottom + row));
                    particles.Add(p);
                    particleMap.Add(p.Head(), p);
                    ++num;

                    // <<<TEMPORARY>>> Add GameObject to the scene for visualization
                    //sim.AddParticle(p.Head().x + 0.5f * p.Head().y, p.Head().y * Mathf.Sqrt(0.75f));      // Note: Replaced by RenderSystem
                }
            }
        }
        string s = "Created system with " + num + " particles:\n";
        foreach (Particle part in particles)
        {
            s += part.Head() + "\n";
        }
        Debug.Log(s);
    }

    public void InitializeExpandedTest(int numParticles)
    {
        // Arrange the particles in a line
        for (int i = 0; i < numParticles; i++)
        {
            Particle p = ParticleFactory.CreateExpandedTestParticle(this, new Vector2Int(0, i));
            particles.Add(p);
            particleMap.Add(p.Head(), p);
        }

        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
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
        // Ensure that a new leader will be chosen
        // (This is bad practice and should be handled differently later!)
        LineFormationParticleSync.leaderCreated = false;

        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        //Particle p = ParticleFactory.CreateLineFormationParticleSeq(this, node);
        Particle p = ParticleFactory.CreateLineFormationParticleSync(this, node);
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
                p = ParticleFactory.CreateLineFormationParticleSync(this, newPos);
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

        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
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

        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
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

        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
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

        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
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



        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
    }

    /// <summary>
    /// Resets the entire system to a state from which it can be
    /// initialized again.
    /// </summary>
    public void Reset()
    {
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


    /**
     * Simulation functions
     */

    /// <summary>
    /// Simulates a round in which a single, randomly chosen particle
    /// is activated and all other particles remain inactive.
    /// </summary>
    public void ActivateRandomParticle()
    {
        if (particles.Count > 0)
        {
            _currentRound++;
            _latestRound++;
            int pIdx = Random.Range(0, particles.Count);
            particles[pIdx].Activate();
            ApplyAllActionsInQueue();
            particles[pIdx].ApplyPlannedPinConfiguration();
            DiscoverCircuits();
            FinishBeepAndMessageInfo();
            CleanupAfterRound();
            _previousRound++;
            UpdateAllParticleVisuals(false);     // Need to update all visuals because handovers can affect multiple particles! (Could use queue though)
            //particles[pIdx].graphics.Update();
        }
    }

    // TODO: Refactor the sequence of phases (including cleanup)

    /// <summary>
    /// Simulates a round in which each particle is activated
    /// exactly once. The order of activations is the same in
    /// each round as long as no particles are added to or
    /// removed from the system.
    /// </summary>
    public void SimulateRound()
    {
        _currentRound++;
        Debug.Log("Simulate round " + _currentRound + " (previous round: " + _previousRound + ")");
        _latestRound++;
        ActivateParticles();
        ApplyAllActionsInQueue();
        ApplyNewPinConfigurations();
        DiscoverCircuits();
        FinishBeepAndMessageInfo();
        CleanupAfterRound();
        _previousRound++;
        UpdateAllParticleVisuals(false);
    }

    public void SimulateJMRound()
    {
        _currentRound++;
        Debug.Log("Simulate round " + _currentRound + " (previous round: " + _previousRound + ")");
        _latestRound++;

        // First: Move cycle
        // All particles set their new bonds and schedule movements
        // Then the movements are evaluated and applied
        inMovePhase = true;
        ActivateParticlesMove();
        inMovePhase = false;
        SimulateJointMovements();
        FinishMovementInfo();

        // Second: Beep cycle
        // All particles set their new pin configurations and send beeps and messages
        // Then the circuits are computed and the sent information is distributed
        inBeepPhase = true;
        ActivateParticlesBeep();
        inBeepPhase = false;
        ApplyNewPinConfigurations();
        DiscoverCircuits();
        FinishBeepAndMessageInfo();
        CleanupAfterRound();
        _previousRound++;
        UpdateAllParticleVisuals(false);
    }

    /// <summary>
    /// Activates each particle once.
    /// </summary>
    public void ActivateParticles()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].Activate();
        }
    }

    private void ActivateParticlesMove()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            Particle p = particles[i];
            p.ActivateMove();

            // Compute bonds by global labels
            int numLabels = p.IsExpanded() ? 10 : 6;
            for (int label = 0; label < numLabels; label++)
            {
                int localLabel = ParticleSystem_Utils.GlobalToLocalLabel(label, p.GlobalHeadDirection(), p.comDir, p.chirality);
                p.activeBondsGlobal[label] = p.BondActive(localLabel);
                p.markedBondsGlobal[label] = p.BondMarked(localLabel);
            }

            p.movementOffset = Vector2Int.zero;

            ParticleAction a = p.ScheduledMovement;

            // Determine the local origin
            p.isHeadOrigin = p.IsContracted() && a == null || p.IsExpanded() && a != null && (a.type == ActionType.CONTRACT_HEAD || a.type == ActionType.PULL_HEAD);

            // TODO: For handovers check if the bond in handover direction is active

            // Apply automatic bond restrictions
            if (a == null) continue;

            // For expanding particles: Bond in expansion direction is always marked, opposite bond is never marked
            if (a.type == ActionType.EXPAND)
            {
                Direction d = ParticleSystem_Utils.LocalToGlobalDir(a.localDir, p.comDir, p.chirality);
                p.markedBondsGlobal[ParticleSystem_Utils.GetLabelInDir(d)] = true;
                p.markedBondsGlobal[ParticleSystem_Utils.GetLabelInDir(d.Opposite())] = false;
            }
            // For pushing particles in handover: Opposite bond is not marked
            else if (a.type == ActionType.PUSH)
            {
                Direction d = ParticleSystem_Utils.LocalToGlobalDir(a.localDir, p.comDir, p.chirality);
                p.markedBondsGlobal[ParticleSystem_Utils.GetLabelInDir(d.Opposite())] = false;
            }
            // For pulling particles in handover: Bonds at origin (non-moving part) cannot be marked
            else if (a.type == ActionType.PULL_HEAD)
            {
                for (int l = 0; l < 10; l++)
                {
                    if (ParticleSystem_Utils.IsHeadLabel(l, p.GlobalHeadDirection()))
                    {
                        p.markedBondsGlobal[l] = false;
                    }
                }
            }
            else if (a.type == ActionType.PULL_TAIL)
            {
                for (int l = 0; l < 10; l++)
                {
                    if (ParticleSystem_Utils.IsTailLabel(l, p.GlobalHeadDirection()))
                    {
                        p.markedBondsGlobal[l] = false;
                    }
                }
            }

            // Compute movement offset
            Direction offsetDir = Direction.NONE;
            if (a.type == ActionType.EXPAND || a.type == ActionType.PUSH)
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
    }

    private void ActivateParticlesBeep()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].ActivateBeep();
        }
    }

    /// <summary>
    /// Applies <see cref="ParticleAction"/>s from the <see cref="ParticleSystem.actionQueue"/>
    /// until the queue is empty.
    /// </summary>
    public void ApplyAllActionsInQueue()
    {
        while (actionQueue.Count > 0)
        {
            ApplyParticleAction(actionQueue.Dequeue());
        }
    }

    /// <summary>
    /// Applies the next <see cref="ParticleAction"/> from the <see cref="ParticleSystem.actionQueue"/>,
    /// if the queue is not empty.
    /// </summary>
    public void ApplyNextActionInQueue()
    {
        if (actionQueue.Count > 0)
        {
            ApplyParticleAction(actionQueue.Dequeue());
        }
    }

    /// <summary>
    /// Applies the given action to the particle for which it was created.
    /// If the particle's <see cref="Particle.ScheduledMovement"/> is the
    /// same as this one, it is reset to <c>null</c>.
    /// </summary>
    /// <param name="a">The action to be applied.</param>
    public void ApplyParticleAction(ParticleAction a)
    {
        switch (a.type)
        {
            case ActionType.EXPAND: Apply_ExpandParticle(a.particle, a.localDir);
                break;
            case ActionType.CONTRACT_HEAD: Apply_ContractParticleHead(a.particle);
                break;
            case ActionType.CONTRACT_TAIL: Apply_ContractParticleTail(a.particle);
                break;
            case ActionType.PUSH: Apply_PerformPushHandover(a.particle, a.localDir);
                break;
            case ActionType.PULL_HEAD: Apply_PerformPullHandoverHead(a.particle, a.localDir);
                break;
            case ActionType.PULL_TAIL: Apply_PerformPullHandoverTail(a.particle, a.localDir);
                break;
            default:
                throw new System.ArgumentException("Unknown ParticleAction type " + a.type);
        }
        // Finally remove the particle's scheduled action if it is this one
        if (a == a.particle.ScheduledMovement)
        {
            a.particle.ScheduledMovement = null;
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

            for (int label = 0; label < numNbrs; label++)
            {
                Direction dir = ParticleSystem_Utils.GetDirOfLabel(label, globalHeadDir);
                bool head = ParticleSystem_Utils.IsHeadLabel(label, globalHeadDir);
                Vector2Int nbrPos = ParticleSystem_Utils.GetNbrInDir(head ? p.Head() : p.Tail(), dir);
                if (particleMap.TryGetValue(nbrPos, out Particle nbr))
                {
                    // We have a neighbor at this location

                    // Skip if the neighbor was already processed
                    if (nbr.processedJointMovement)
                        continue;

                    // Collect information and check if we have a bond to this neighbor
                    bool isNbrHead = nbr.Head() == nbrPos;
                    int nbrLabel = ParticleSystem_Utils.GetLabelInDir(dir.Opposite(), nbr.GlobalHeadDirection(), isNbrHead);

                    // Have connection if both of the bonds are active
                    bool myBondActive = p.activeBondsGlobal[label];
                    bool nbrBondActive = nbr.activeBondsGlobal[nbrLabel];

                    bool bondsActive = myBondActive && nbrBondActive;
                    bool bondsDisagree = myBondActive ^ nbrBondActive;
                    // TODO: Maybe remove this warning?
                    if (bondsDisagree)
                    {
                        Debug.LogWarning("Bonds disagree at position " + nbrPos);
                    }

                    // Add the neighbor to the list if there is a bond
                    if (bondsActive)
                    {
                        nbrParts[label] = nbr;
                        nbrHead[label] = isNbrHead;
                        nbrLabels[label] = nbrLabel;
                    }
                }
            }

            // Neighbors have been found
            // Now check if the particle's movement agrees with its neighbors
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

                    // If we have marked the bond and want to move, we will definitely apply our own movement offset
                    if (weMove && weMarked)
                        nbrOffset = p.movementOffset;

                    // If the neighbor has marked the bond and wants to move, its offset has to be applied in reverse direction
                    if (nbrMove && nbrMarked)
                        nbrOffset -= nbr.movementOffset;
                }
                // We are contracted and the neighbor is expanded
                else if (p.IsContracted() && nbr.IsExpanded())
                {
                    Debug.Log("Label: " + label);
                    Direction bondDir = ParticleSystem_Utils.GetDirOfLabel(label);
                    Debug.Log("bondDir: " + bondDir);
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
                        secondLabel != -1 && ParticleSystem_Utils.IsHeadLabel(secondLabel, p.GlobalHeadDirection()));
                }
                // TODO: Other initial expansion cases




                // Have computed the relative offset of this neighbor
                // If the neighbor is already queued, its offset has already been set by another particle
                // and we must check if it matches our computed offset
                nbrOffset += p.jmOffset;
                if (nbr.queuedForJMProcessing)
                {
                    if (nbr.jmOffset != nbrOffset)
                    {
                        Debug.LogError("Offset for particle does not match! Previous offset: " + nbr.jmOffset + ", new offset: " + nbrOffset);
                        throw new System.InvalidOperationException("Conflict during joint movement");
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
                    Debug.LogError("Error: Conflict during joint movement, target location of expanded particle is already occupied.");
                    throw new System.InvalidOperationException();
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
                    Debug.LogError("Error: Conflict during joint movement, target location of contracted particle is already occupied.");
                    throw new System.InvalidOperationException();
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
                Debug.LogError("Bond structure is not connected, some particle movements were not processed!");
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

        particleMap = newPositions;

        // TODO

        Debug.Log("Computed joint movements in " + (Time.realtimeSinceStartup - tStart) + "s");
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
    /// <returns>The global offset vector of the expanded particle's origin relative to the
    /// contracted particle's origin.</returns>
    private Vector2Int JointMovementContractedExpanded(Particle c, Particle e, Direction bondDir, Direction secondBondDir,
        int cLabel1, int cLabel2, int eLabel1, int eLabel2, bool eHead1, bool eHead2)
    {
        Vector2Int eOffset = Vector2Int.zero;
        ParticleAction cAction = c.ScheduledMovement;
        ParticleAction eAction = e.ScheduledMovement;

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
                Debug.LogError("Error: Disagreement on handover with one bond");
                throw new System.InvalidOperationException("Conflict during movements");
            }

            if (weWantHandover)
            {
                // Easy: No offset is applied at all
            }
            else
            {
                // No handover
                // Apply both offsets
                if (cAction != null && c.markedBondsGlobal[cLabel1])
                {
                    eOffset += c.movementOffset;
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
                    }
                }
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
                Debug.LogError("Error: Disagreement on handover with two bonds");
                throw new System.InvalidOperationException("Conflict during movements");
            }

            bool handoverFirst = weWantHandoverFirst && nbrWantsHandoverFirst;
            bool handoverSecond = weWantHandoverSecond && nbrWantsHandoverSecond;

            if (handoverFirst || handoverSecond)
            {
                // We want a handover on one of the two bonds
                // Easy: No offset must be applied
                // But we have to check that the other bond is not marked
                if (handoverFirst && c.markedBondsGlobal[cLabel2] || handoverSecond && c.markedBondsGlobal[cLabel1])
                {
                    Debug.LogError("Error: Handover with two bonds and one bond is marked");
                    throw new System.InvalidOperationException("Conflict during movements");
                }
            }
            else
            {
                // No handover
                // Neighbor must not contract and both bonds must have the same marked status if we want to move

                // TODO: Maybe a handover would be possible if one of the bonds stays in place

                if (eAction != null)
                {
                    Debug.LogError("Error: Expanded neighbor with two bonds tries to contract");
                    throw new System.InvalidOperationException("Conflict during movements");
                }

                if (c.ScheduledMovement != null)
                {
                    if (c.markedBondsGlobal[cLabel1] ^ c.markedBondsGlobal[cLabel2])
                    {
                        Debug.LogError("Error: Bonds to expanded neighbor have different marked status");
                        throw new System.InvalidOperationException("Conflict during movements");
                    }

                    // If the bonds are marked, we apply our offset
                    if (c.markedBondsGlobal[cLabel1])
                        eOffset += c.movementOffset;
                }
            }
        }
        return eOffset;
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
    public void DiscoverCircuits(bool sendBeepsAndMessages = true)
    {
        float tStart = Time.realtimeSinceStartup;
        // TODO: Experiment with initial capacity
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
                        Circuit c = new Circuit(circuits.Count);
                        c.AddPartitionSet(ps);
                        circuits.Add(c);
                    }
                }
                p.processedPinConfig = true;
            }
        }

        string s = "Found " + circuits.Count + " circuits in " + (Time.realtimeSinceStartup - tStart) + " seconds\n";
        //string s = "Found " + circuits.Count + " circuits:\n";
        //foreach (Circuit c in circuits)
        //{
        //    //s += c.Print() + "\n";
        //    if (c.partitionSets.Count > 1)
        //    {
        //        s += "Circuit with " + c.partitionSets.Count + " partition sets: " + (c.hasBeep ? "HAS BEEP" : "HAS NO BEEP") + "\n";
        //        foreach (SysPartitionSet ps in c.partitionSets)
        //        {
        //            s += "Partition set with ID " + ps.Id + " and pins: ";
        //            foreach (int i in ps.GetPinIds())
        //            {
        //                s += i + " ";
        //            }
        //            s += "\n";
        //        }
        //    }
        //}
        Debug.Log(s);

        // Apply beeps and send messages to all circuits
        // Also apply colors to circuits
        int colIdx = 0;
        foreach (Circuit c in circuits)
        {
            if (!c.active)
            {
                continue;
            }
            if (sendBeepsAndMessages)
            {
                if (c.hasBeep)
                {
                    foreach (SysPartitionSet ps in c.partitionSets)
                    {
                        ps.pinConfig.particle.ReceiveBeep(ps.id);
                    }
                }
                if (c.message != null)
                {
                    foreach (SysPartitionSet ps in c.partitionSets)
                    {
                        ps.pinConfig.particle.ReceiveMessage(ps.id, c.message);
                    }
                }
            }

            // Only apply color to circuits with more than 2 partition sets
            if (c.partitionSets.Count > 2 && !c.colorOverride)
            {
                c.color = c.colorOverride ? c.color : ColorData.Circuit_Colors[colIdx];
                colIdx = (colIdx + 1) % ColorData.Circuit_Colors.Length;
            }
        }

        // Complete graphics information for all particles
        foreach (Particle p in particles)
        {
            // Compute each partition set
            foreach (SysPartitionSet ps in p.PinConfiguration.partitionSets)
            {
                if (ps.IsEmpty()) continue;

                // Singleton sets must be created independently
                if (ps.NumStoredPins == 1)
                {
                    SysPin pin = ps.GetPins()[0] as SysPin;
                    Direction pinDir = ParticleSystem_Utils.GetDirOfLabel(pin.globalLabel, p.GlobalHeadDirection());
                    ParticlePinGraphicState.PSetData pset = ParticlePinGraphicState.PSetData.PoolCreate();
                    pset.UpdatePSetData(
                        circuits[ps.circuit].color,
                        circuits[ps.circuit].hasBeep,
                        p.HasPlannedBeep(ps.Id),
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
                        circuits[ps.circuit].color,
                        circuits[ps.circuit].hasBeep,
                        p.HasPlannedBeep(ps.Id),
                        pins);
                    p.gCircuit.partitionSets.Add(pset);
                }
            }
        }
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
            // Also reset attribute intermediate values
            p.ResetAttributeIntermediateValues();
        }
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
            else p.graphics.Update(true);
        }
        foreach (Particle p in particles)
        {
            p.graphics.CircuitUpdate(p.gCircuit);
        }
        renderSystem.ParticleMovementOver();
        renderSystem.CircuitCalculationOver();
    }

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
    // TODO: How to handle case that neighbor does not exist? For now just return null
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
            throw new System.InvalidOperationException("Expanded particle cannot expand again.");
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
        //actionQueue.Enqueue(a);
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
            throw new System.InvalidOperationException("Contracted particle cannot contract again.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Contracting particle already has a scheduled movement.");
        }

        // Store contraction action in particle and queue
        ParticleAction a = new ParticleAction(p, head ? ActionType.CONTRACT_HEAD : ActionType.CONTRACT_TAIL);
        p.ScheduledMovement = a;
        actionQueue.Enqueue(a);
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
            throw new System.InvalidOperationException("Expanded particle cannot perform a push handover.");
        }

        // Reject if there is no expanded particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);
        if (!particleMap.TryGetValue(targetLoc, out Particle p2) || p2.IsContracted())
        {
            throw new System.InvalidOperationException("Particle cannot perform push handover onto node occupied by no or contracted particle.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Particle scheduling push handover already has a scheduled movement.");
        }

        // Store push handover action in particle and queue
        ParticleAction a = new ParticleAction(p, ActionType.PUSH, locDir);
        p.ScheduledMovement = a;
        actionQueue.Enqueue(a);
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
            throw new System.InvalidOperationException("Contracted particle cannot perform pull handover.");
        }

        // Reject if there is no contracted particle on the target node
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, !head);
        if (!particleMap.TryGetValue(targetLoc, out Particle p2) || p2.IsExpanded())
        {
            throw new System.InvalidOperationException("Particle cannot perform pull handover onto node occupied by no or expanded particle.");
        }

        // Warning if particle already has a scheduled movement operation
        // TODO: Turn this into an error?
        if (p.ScheduledMovement != null)
        {
            Debug.LogWarning("Particle scheduling pull handover already has a scheduled movement.");
        }

        // Store pull handover action in particle and queue
        ParticleAction a = new ParticleAction(p, head ? ActionType.PULL_HEAD : ActionType.PULL_TAIL, locDir);
        p.ScheduledMovement = a;
        actionQueue.Enqueue(a);
    }


    /**
     * State change functions (called to update the simulation state).
     * These should be called after all particles were activated.
     */

    /// <summary>
    /// Applies an expansion as scheduled by <see cref="ExpandParticle(Particle, Direction)"/>
    /// if the action can still be applied.
    /// <para>
    /// The action cannot be applied if the node in expansion direction is occupied by
    /// a particle that has already moved or that does not intend to move away from the
    /// node within this round.
    /// This will lead to an exception unless the <see cref="useFCFS"/> flag is set,
    /// in which case the action is simply aborted and counted as a movement without
    /// effect.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the node in expansion direction is occupied by a particle blocking
    /// this particle's expansion and the <see cref="useFCFS"/> flag is not set.
    /// </exception>
    /// <param name="p">The particle that should expand.</param>
    /// <param name="locDir">The local direction into which the particle should expand.</param>
    public void Apply_ExpandParticle(Particle p, Direction locDir)
    {
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);

        // Error if the target location is already occupied and the occupying particle
        // does not intend to move away
        if (particleMap.TryGetValue(targetLoc, out Particle p2))
        {
            // Target node is occupied, check if the occupying particle intends to move away
            if (p2.hasMoved || !MovementMatchesExpansion(p, p2, targetLoc))
            {
                if (!useFCFS)
                {
                    throw new System.InvalidOperationException("Particle tries to expand onto occupied node and occupying particle does not intend to move away.");
                }
                else
                {
                    // This movement would lead to a conflict, abort
                    p.hasMoved = true;
                    return;
                }
            }
        }

        // Action is allowed
        // First let the particle update its internal state
        p.Apply_Expand(locDir);
        // Then update the particle map (need a second entry that points to this particle)
        particleMap[p.Head()] = p;
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    /// <summary>
    /// Applies a contraction as scheduled by <see cref="ContractParticleHead(Particle)"/>.
    /// This action can always be applied.
    /// The case that another particle has already expanded onto the node occupied by this
    /// particle's tail is accounted for.
    /// </summary>
    /// <param name="p">The particle that should contract.</param>
    public void Apply_ContractParticleHead(Particle p)
    {
        // Action is always allowed
        // First let the particle update its internal state
        Vector2Int tailPos = p.Tail();
        p.Apply_ContractHead();
        // Then update the particle map (need to remove the particle's tail entry if not removed yet)
        if (particleMap.TryGetValue(tailPos, out Particle p2) && p2 == p)
        {
            particleMap.Remove(tailPos);
        }
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    /// <summary>
    /// Applies a contraction as scheduled by <see cref="ContractParticleTail(Particle)"/>.
    /// This action can always be applied.
    /// The case that another particle has already expanded onto the node occupied by this
    /// particle's head is accounted for.
    /// </summary>
    /// <param name="p">The particle that should contract.</param>
    public void Apply_ContractParticleTail(Particle p)
    {
        // Action is always allowed
        // First let the particle update its internal state
        Vector2Int headPos = p.Head();
        p.Apply_ContractTail();
        // Then update the particle map (need to remove the particle's head entry if not removed yet)
        if (particleMap.TryGetValue(headPos, out Particle p2) && p2 == p)
        {
            particleMap.Remove(headPos);
        }
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    /// <summary>
    /// Applies a push handover as scheduled by <see cref="PerformPushHandover(Particle, Direction)"/>
    /// if the action can still be applied.
    /// <para>
    /// The action cannot be applied if the node in expansion direction is occupied by
    /// a particle that has already moved or that intends to perform a movement that is
    /// inconsistent with the handover, such as contracting into the target node or
    /// performing a pull handover that is directed at a third particle.
    /// This will lead to an exception unless the <see cref="useFCFS"/> flag is set,
    /// in which case the action is simply aborted and counted as a movement without
    /// effect.
    /// </para>
    /// <para>
    /// If the node in expansion direction is occupied by an expanded particle without
    /// a scheduled movement, this particle is contracted away from that node.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the node in expansion direction is occupied by a particle blocking
    /// the handover and the <see cref="useFCFS"/> flag is not set.
    /// </exception>
    /// <param name="p">The particle that should expand during the handover.</param>
    /// <param name="locDir">The local direction into which the particle should expand.</param>
    public void Apply_PerformPushHandover(Particle p, Direction locDir)
    {
        Vector2Int targetLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, true);

        // If the target location is already occupied: Make sure that the occupying particle
        // has not moved already (i.e., is the same one as before), and maybe move it if it
        // has a null action
        if (particleMap.TryGetValue(targetLoc, out Particle p2))
        {
            // Error if the other particle has already moved or intends to perform a non-matching movement
            if (p2.hasMoved || p2.ScheduledMovement != null && !MovementMatchesExpansion(p, p2, targetLoc))
            {
                if (!useFCFS)
                {
                    throw new System.InvalidOperationException("Particle tries to perform push handover but pushed particle has already moved or intends to perform a different movement.");
                }
                else
                {
                    // This movement would lead to a conflict, abort
                    p.hasMoved = true;
                    return;
                }
            }
            
            // If the other particle does not intend to do anything: Contract it manually
            if (p2.ScheduledMovement == null)
            {
                // p2 must be expanded at this point because it has not moved yet and cannot have been contracted when the action was scheduled
                if (targetLoc == p2.Tail())
                {
                    Apply_ContractParticleHead(p2);
                }
                else
                {
                    Apply_ContractParticleTail(p2);
                }
            }
        }

        // Action is allowed
        // First let the particle update its internal state
        p.Apply_PushHandover(locDir);
        // Then update the particle map (need a second entry that points to this particle)
        particleMap[p.Head()] = p;
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }

    /// <summary>
    /// Applies a pull handover as scheduled by <see cref="PerformPullHandoverHead(Particle, Direction)"/>
    /// if the action can still be applied.
    /// <para>
    /// The action cannot be applied if the node where the neighbor to pull should be
    /// is unoccupied or the neighbor has performed or intends to perform a movement
    /// that is inconsistent with the handover, such as expanding into a different
    /// direction.
    /// This will lead to an exception unless the <see cref="useFCFS"/> flag is set,
    /// in which case the action is simply aborted and counted as a movement without
    /// effect.
    /// </para>
    /// <para>
    /// If the node where the pulled neighbour should be is occupied by a contracted
    /// particle without a scheduled movement, that particle is expanded onto the
    /// current tail node of this particle.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the node where the neighbor should be is unoccupied or the neighbor
    /// particle has moved or intends to move inconsistently with the handover.
    /// The second case only throws an exception if the <see cref="useFCFS"/> flag is
    /// not set.
    /// </exception>
    /// <param name="p">The particle that should contract during the handover.</param>
    /// <param name="locDir">The local direction relative to <paramref name="p"/>'s tail
    /// from where a contracted neighbor particle should be expanded to occupy the
    /// tail node.</param>
    public void Apply_PerformPullHandoverHead(Particle p, Direction locDir)
    {
        Apply_PerformPullHandover(p, locDir, true);
    }

    /// <summary>
    /// Applies a pull handover as scheduled by <see cref="PerformPullHandoverTail(Particle, Direction)"/>
    /// if the action can still be applied.
    /// <para>
    /// The action cannot be applied if the node where the neighbor to pull should be
    /// is unoccupied or the neighbor has performed or intends to perform a movement
    /// that is inconsistent with the handover, such as expanding into a different
    /// direction.
    /// This will lead to an exception unless the <see cref="useFCFS"/> flag is set,
    /// in which case the action is simply aborted and counted as a movement without
    /// effect.
    /// </para>
    /// <para>
    /// If the node where the pulled neighbour should be is occupied by a contracted
    /// particle without a scheduled movement, that particle is expanded onto the
    /// current head node of this particle.
    /// </para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the node where the neighbor should be is unoccupied or the neighbor
    /// particle has moved or intends to move inconsistently with the handover.
    /// The second case only throws an exception if the <see cref="useFCFS"/> flag is
    /// not set.
    /// </exception>
    /// <param name="p">The particle that should contract during the handover.</param>
    /// <param name="locDir">The local direction relative to <paramref name="p"/>'s head
    /// from where a contracted neighbor particle should be expanded to occupy the
    /// head node.</param>
    public void Apply_PerformPullHandoverTail(Particle p, Direction locDir)
    {
        Apply_PerformPullHandover(p, locDir, false);
    }

    private void Apply_PerformPullHandover(Particle p, Direction locDir, bool head)
    {
        Vector2Int targetLoc = head ? p.Tail() : p.Head();
        Vector2Int nbrLoc = ParticleSystem_Utils.GetNeighborPosition(p, locDir, !head);

        // Error if the position from which we wanted to pull the particle is empty
        if (!particleMap.TryGetValue(nbrLoc, out Particle p2))
        {
            // FCFS: This should not happen at all, so throwing an exception is fine
            throw new System.InvalidOperationException("Particle tries to perform pull handover but there is no particle to pull.");
        }

        // Also throw error if the neighbor has already moved to a different position or intends to perform a different non-matching movement
        if (p2.hasMoved && p2.Head() != targetLoc && p2.Tail() != targetLoc || p2.ScheduledMovement != null && !MovementMatchesContraction(p, p2, targetLoc))
        {
            if (!useFCFS)
            {
                throw new System.InvalidOperationException("Particle tries to perform pull handover but pulled particle has already expanded somewhere else or intends to perform a different non-matching movement.");
            }
            else
            {
                // This movement would lead to a conflict, abort
                p.hasMoved = true;
                return;
            }
        }

        // If the other particle does not intend to do anything and has not moved yet: Expand it manually
        if (!p2.hasMoved && p2.ScheduledMovement == null)
        {
            // p2 must be contracted at this point because it has not moved yet and cannot have been expanded when the action was scheduled
            // Expansion direction is local((global(locDir) + 3) % 6)
            Apply_ExpandParticle(p2, ParticleSystem_Utils.GlobalToLocalDir(ParticleSystem_Utils.LocalToGlobalDir(locDir, p.comDir, p.chirality).Opposite(), p2.comDir, p2.chirality));
        }

        // Action is allowed
        // First let the particle update its internal state
        Vector2Int rmPos = head ? p.Tail() : p.Head();
        if (head)
        {
            p.Apply_PullHandoverHead(locDir);
        }
        else
        {
            p.Apply_PullHandoverTail(locDir);
        }
        // Then update the particle map (need to remove the particle's second entry if not removed yet)
        if (particleMap.TryGetValue(targetLoc, out p2) && p2 == p)
        {
            particleMap.Remove(rmPos);
        }
        // Also remember that the particle has moved in this round
        p.hasMoved = true;
    }


    /**
     * Helpers
     */

    /// <summary>
    /// Checks if the scheduled <see cref="ParticleAction"/> of an expanded Particle
    /// matches the expansion action of a contracted neighbor.
    /// </summary>
    /// <param name="expandingPart">The Particle that wants to expand.</param>
    /// <param name="otherPart">The Particle whose action should be checked.</param>
    /// <param name="targetLoc">The grid node onto which <paramref name="expandingPart"/>
    /// wants to expand and which is occupied by <paramref name="otherPart"/>.</param>
    /// <returns><c>true</c> if and only if the <see cref="Particle.ScheduledMovement"/> of
    /// <paramref name="otherPart"/> is not <c>null</c> and allows <paramref name="expandingPart"/>
    /// to expand, i.e., if <paramref name="otherPart"/> intends to contract away from
    /// <paramref name="targetLoc"/> either through a regular contraction or through a
    /// pull handover directed at <paramref name="expandingPart"/>.</returns>
    private bool MovementMatchesExpansion(Particle expandingPart, Particle otherPart, Vector2Int targetLoc)
    {
        return otherPart.ScheduledMovement != null && (
            (otherPart.ScheduledMovement.type == ActionType.CONTRACT_HEAD && otherPart.Tail() == targetLoc) ||
            (otherPart.ScheduledMovement.type == ActionType.CONTRACT_TAIL && otherPart.Head() == targetLoc) ||
            (otherPart.ScheduledMovement.type == ActionType.PULL_HEAD && otherPart.Tail() == targetLoc && ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.ScheduledMovement.localDir, false) == expandingPart.Head()) ||
            (otherPart.ScheduledMovement.type == ActionType.PULL_TAIL && otherPart.Head() == targetLoc && ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.ScheduledMovement.localDir, true) == expandingPart.Head()));
    }

    /// <summary>
    /// Checks if the scheduled <see cref="ParticleAction"/> of a contracted Particle
    /// matches the contraction action of an expanded neighbor.
    /// </summary>
    /// <param name="contractingPart">The Particle that wants to contract with a pull handover.</param>
    /// <param name="otherPart">The Particle that is supposed to follow using an expansion.</param>
    /// <param name="targetLoc">The grid node from which <paramref name="contractingPart"/> wants to
    /// contract and onto which <paramref name="otherPart"/> is supposed to expand.</param>
    /// <returns><c>true</c> if and only if the <see cref="Particle.ScheduledMovement"/> of
    /// <paramref name="otherPart"/> is not <c>null</c> and follows the pull handover of
    /// <paramref name="contractingPart"/>, i.e., it is a regular expansion or a push handover
    /// directed at <paramref name="contractingPart"/>.</returns>
    private bool MovementMatchesContraction(Particle contractingPart, Particle otherPart, Vector2Int targetLoc)
    {
        return otherPart.ScheduledMovement != null && (
            (otherPart.ScheduledMovement.type == ActionType.EXPAND && targetLoc == ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.ScheduledMovement.localDir, true)) ||
            (otherPart.ScheduledMovement.type == ActionType.PUSH && targetLoc == ParticleSystem_Utils.GetNeighborPosition(otherPart, otherPart.ScheduledMovement.localDir, true)));
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
            throw new System.ArgumentOutOfRangeException("Cannot set system to round " + round + "; must be between " + _earliestRound + " and " + _latestRound);
        }
        if (isTracking || round != _currentRound)
        {
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
            // TODO: This should be structured better
            DiscoverCircuits(false);
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
            UpdateAllParticleVisuals(true);
        }
    }

    public void StepBack()
    {
        if (_currentRound == _earliestRound)
        {
            throw new System.InvalidOperationException("Cannot step back because the system is in the earliest round " + _earliestRound);
        }
        // Have to synchronize to given round if we are still tracking
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
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
            UpdateAllParticleVisuals(true);
        }
    }

    public void StepForward()
    {
        if (_currentRound == _latestRound)
        {
            throw new System.InvalidOperationException("Cannot step forward because the system is in the latest round " + _latestRound);
        }
        // Have to synchronize to given round if we are still tracking
        if (isTracking)
        {
            SetMarkerToRound(_currentRound + 1);
        }
        // Otherwise particles are already synchronized
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
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
            UpdateAllParticleVisuals(false);
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
            DiscoverCircuits();
            CleanupAfterRound();
            foreach (Particle p in particles)
                p.ResetPlannedBeepsAndMessages();
            UpdateAllParticleVisuals(true);
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
            }
        }
    }

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
    }


    /**
     * Saving and loading functionality.
     */

    public SimulationStateSaveData GenerateSaveData()
    {
        SimulationStateSaveData data = new SimulationStateSaveData();

        data.earliestRound = _earliestRound;
        data.latestRound = _latestRound;

        data.particles = new ParticleStateSaveData[particles.Count];
        for (int i = 0; i < particles.Count; i++)
        {
            data.particles[i] = particles[i].GenerateSaveData();
        }

        return data;
    }

    public void InitializeFromSaveState(SimulationStateSaveData data)
    {
        _earliestRound = data.earliestRound;
        _latestRound = data.latestRound;
        _currentRound = _latestRound;
        _previousRound = _latestRound;

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

        DiscoverCircuits(false);
        CleanupAfterRound();
        UpdateAllParticleVisuals(true);
    }




    

    public void GenerateParticles(int particleAmount, InitializationUIHandler.SettingChirality chirality, bool randomCompassDir, Direction compassDir)
    {
        // If randomCompassDir is true the value of direction can be ignored
        throw new System.NotImplementedException();
    }

    public void InitializationModeStarted()
    {
        // Note: The initialization window has just been opened. So it might be possible to save the state of a running algorithm and convert its particles to the initialization particles
        // in order to be able to use the given state for new algorithms / save the particle configuration.
        Log.Debug("InitializationModeStarted: Not implemented yet (I dont wanna throw an error here).");
    }

    public void InitializationModeAborted()
    {
        // Note: The initialization mode has just been aborted and the window is closed. Here the previously saved state could be loaded again to continue with the old algorithm.
        Log.Debug("InitializationModeAborted: Not implemented yet (I dont wanna throw an error here).");
    }

    public bool IsInLatestRound()
    {
        return CurrentRound == LatestRound;
    }
}
