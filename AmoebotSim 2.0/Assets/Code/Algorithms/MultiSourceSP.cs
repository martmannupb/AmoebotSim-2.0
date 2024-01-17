using System.Collections.Generic;
using AS2.Sim;
using AS2.Subroutines.ETT;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.MultiSourceSP
{

    /// <summary>
    /// Implements the (k, l)-SPF algorithm.
    /// </summary>

    // SETUP PHASE
    // Round 0:
    //  - Find out which amoebots represent portal edges and in which directions
    //  - Setup simple portal circuits for main direction
    //  - Source amoebots send beeps
    // Round 1:
    //  - Receive beeps to determine all source portals
    //  - Setup ETT for augmentation set computation
    //  - Start ETT
    // Round 2:
    //  - Continue running ETT until it is finished
    // Round 3:
    //  - ETT has finished: All amoebots know which edges have a non-zero result
    //  - Setup circuit for checking whether degree is >= 3:
    //      - We have 4 pins: If we have 0 neighbors, connect pin i to i on the other side
    //      - If we have 1 neighbor, connect pin 0 to 1, 1 to 2 and 2 and 3 to 3
    //      - If we have 2 neighbors, connect pin 0 to 2, 1-3 to 3
    //  - The "leftmost" amoebot sends a beep on pin i, where i is the number of its own neighbors
    // Round 4:
    //  - Receive neighbor counting beep
    //      - If we have degree at least 3 (and are not already a source): Become an augmentation portal
    //  - Setup markers and portal circuit that is cut at each marker (one circuit for each side)
    //  - "Leftmost" amoebot of the portal sends beep (unless it is marked itself)
    // Round 5:
    //  - Marked amoebots receiving a beep from their "left" neighbor (or leftmost AND marked amoebots) remove their marker

    //  - Setup neighbor circuits, beep on the circuit belonging to the parent
    //      - One circuit for each neighboring region, only in Q' portals
    // Round 6:
    //  - Receive neighbor beeps and set parent direction

    public class MultiSourceSPParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "k-SPF";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(MultiSourceSPInitializer).FullName;

        // Colors
        static readonly Color baseColor = Color.gray;
        static readonly Color leaderColor = ColorData.Particle_Yellow;
        static readonly Color sourceColor = ColorData.Particle_Red;
        static readonly Color destColor = ColorData.Particle_BlueDark;
        static readonly Color sourcePortalColor = ColorData.Particle_Orange;
        static readonly Color augPortalColor = ColorData.Particle_Purple;
        static readonly Color markerColor = ColorData.Particle_Blue;
        static readonly Color destPortalColor = ColorData.Particle_Blue;
        static readonly Color mixedPortalColor = ColorData.Particle_Purple;

        enum Phase
        {
            SETUP,
            FINAL_PRUNE,
            FINISHED
        }

        private const Direction mainDir = Direction.E;  // The main direction defining the orientation of the portals

        // Declare attributes here
        ParticleAttribute<Phase> phase;                 // Current phase
        ParticleAttribute<int> round;                   // Round counter

        ParticleAttribute<bool> isLeader;               // Leader flag
        ParticleAttribute<bool> isSource;               // Source flag
        ParticleAttribute<bool> isDest;                 // Destination flag

        ParticleAttribute<bool> sourcePortal;           // Whether we are on a portal with a source
        ParticleAttribute<bool> augPortal;              // Whether we are on a portal in the augmentation set
        ParticleAttribute<Direction> nbrPortalDir1;     // Direction of the first neighbor portal edge (mainDir + 60 or mainDir + 120 or NONE)
        ParticleAttribute<Direction> nbrPortalDir2;     // Direction of the second neighbor portal edge (mainDir - 60 or mainDir - 120 or NONE)

        ParticleAttribute<bool> parent1L;               // Whether the "top left" portal neighbor is a portal parent
        ParticleAttribute<bool> parent1R;               // Whether the "top right" portal neighbor is a portal parent
        ParticleAttribute<bool> parent2L;               // Whether the "bottom left" portal neighbor is a portal parent
        ParticleAttribute<bool> parent2R;               // Whether the "bottom right" portal neighbor is a portal parent
        ParticleAttribute<bool> marker1;                // Whether we are marked for the first neighbor
        ParticleAttribute<bool> marker2;                // Whether we are marked for the second neighbor

        SubETT ett;

        public MultiSourceSPParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            phase = CreateAttributeEnum<Phase>("Phase", Phase.SETUP);
            round = CreateAttributeInt("Round", 0);

            isLeader = CreateAttributeBool("Leader", false);
            isSource = CreateAttributeBool("Source", false);
            isDest = CreateAttributeBool("Destination", false);

            sourcePortal = CreateAttributeBool("Source Portal", false);
            augPortal = CreateAttributeBool("Augmentation Portal", false);
            nbrPortalDir1 = CreateAttributeDirection("Nbr Portal Dir 1", Direction.NONE);
            nbrPortalDir2 = CreateAttributeDirection("Nbr Portal Dir 2", Direction.NONE);

            parent1L = CreateAttributeBool("Parent 1 L", false);
            parent1R = CreateAttributeBool("Parent 1 R", false);
            parent2L = CreateAttributeBool("Parent 2 L", false);
            parent2R = CreateAttributeBool("Parent 2 R", false);
            marker1 = CreateAttributeBool("Marker 1", false);
            marker2 = CreateAttributeBool("Marker 2", false);

            ett = new SubETT(p);

            // Also, set the default initial color
            SetMainColor(baseColor);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool isLeader = false, bool isSource = false, bool isDest = false)
        {
            this.isLeader.SetValue(isLeader);
            this.isSource.SetValue(isSource);
            this.isDest.SetValue(isDest);

            SetColor();
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
            switch (phase.GetValue())
            {
                case Phase.SETUP:
                    ActivateSetupPhase();
                    break;
            }
        }

        private void ActivateSetupPhase()
        {
            if (round == 0)
            {
                // Find edge amoebots
                DeterminePortalNeighbors(mainDir);
                // Send source beeps on portal circuits
                SetupSimplePortalCircuit(mainDir);
                if (isSource)
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    pc.SendBeepOnPartitionSet(0);
                }
                round.SetValue(round + 1);
            }
            else if (round == 1)
            {
                // Receive source beep to determine source portals
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    sourcePortal.SetValue(true);
                }

                // Setup ETT
                // First collect the neighbor directions (order is important)
                List<Direction> nbrDirs = new List<Direction>();
                if (HasNeighborAt(mainDir))
                    nbrDirs.Add(mainDir);
                if (nbrPortalDir1 != Direction.NONE)
                    nbrDirs.Add(nbrPortalDir1);
                if (HasNeighborAt(mainDir.Opposite()))
                    nbrDirs.Add(mainDir.Opposite());
                if (nbrPortalDir2 != Direction.NONE)
                    nbrDirs.Add(nbrPortalDir2);
                // Mark the first edge if we are a dest portal representative
                int markedIdx = IsQPortalMarker() ? 0 : -1;
                // Split if we are the source portal representative
                bool split = isLeader.GetCurrentValue();
                ett.Init(nbrDirs.ToArray(), markedIdx, split);

                pc = GetContractedPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);

                if (isLeader.GetCurrentValue())
                    ett.ActivateSend();

                round.SetValue(round + 1);
            }
            else if (round == 2)
            {
                // Continue running ETT until it is finished
                ett.ActivateReceive();

                if (ett.IsFinished())
                {
                    round.SetValue(round + 1);
                    return;
                }

                PinConfiguration pc = GetCurrentPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                if (ett.IsTerminationRound())
                    ett.ActivateSend();
                else
                {
                    if (isLeader)
                        ett.ActivateSend();
                }
            }
            else if (round == 3)
            {
                // Count how many portal neighbors we have
                int numNbrs = 0;
                if (nbrPortalDir1 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir1) != Comparison.EQUAL)
                    numNbrs++;
                if (nbrPortalDir2 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir2) != Comparison.EQUAL)
                    numNbrs++;

                SetupCountingCircuit(mainDir, numNbrs);

                // "Leftmost" amoebot sends beep
                if (!HasNeighborAt(mainDir.Opposite()))
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    pc.GetPinAt(mainDir, PinsPerEdge - 1 - numNbrs).PartitionSet.SendBeep();
                }

                round.SetValue(round + 1);
            }
            else if (round == 4)
            {
                // Check whether our portal's degree is at least 3
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.GetPinAt(mainDir, 0).PartitionSet.ReceivedBeep())
                {
                    augPortal.SetValue(true);
                }

                // Setup markers and marker circuit
                if (IsOnQPrime())
                {
                    if (nbrPortalDir1 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir1) != Comparison.EQUAL)
                        marker1.SetValue(true);
                    if (nbrPortalDir2 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir2) != Comparison.EQUAL)
                        marker2.SetValue(true);

                    SetupMarkerCircuit(mainDir);

                    // End of the portal sends beep on both circuits
                    if (!HasNeighborAt(mainDir.Opposite()))
                    {
                        pc = GetPlannedPinConfiguration();
                        if (!marker1.GetCurrentValue())
                            pc.GetPinAt(mainDir, PinsPerEdge - 1).PartitionSet.SendBeep();
                        if (!marker2.GetCurrentValue())
                            pc.GetPinAt(mainDir, 0).PartitionSet.SendBeep();
                    }
                }

                round.SetValue(round + 1);
            }
            else if (round == 5)
            {
                // Remove marker
                if (IsOnQPrime())
                {
                    PinConfiguration pc = GetCurrentPinConfiguration();
                    if (marker1)
                    {
                        if (!HasNeighborAt(mainDir.Opposite()) || pc.GetPinAt(mainDir.Opposite(), 0).PartitionSet.ReceivedBeep())
                            marker1.SetValue(false);
                    }
                    if (marker2)
                    {
                        if (!HasNeighborAt(mainDir.Opposite()) || pc.GetPinAt(mainDir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                            marker2.SetValue(false);
                    }
                }

                // Send beeps indicating parent portals
                SetupPortalNeighborCircuits(mainDir);
                if (nbrPortalDir1 != Direction.NONE || nbrPortalDir2 != Direction.NONE)
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    // Comparison result GREATER means that the edge indicates the parent
                    if (nbrPortalDir1 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir1) == Comparison.GREATER)
                        pc.SendBeepOnPartitionSet(marker1.GetCurrentValue() ? 1 : 0);
                    if (nbrPortalDir2 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir2) == Comparison.GREATER)
                        pc.SendBeepOnPartitionSet(marker2.GetCurrentValue() ? 3 : 2);
                }

                round.SetValue(round + 1);
            }
            else if (round == 6)
            {
                // Receive parent beeps
                PinConfiguration pc = GetCurrentPinConfiguration();
                // Amoebots with neighbor portal edges could have different results for sides L and R
                if (nbrPortalDir1 == Direction.NONE)
                {
                    if (pc.ReceivedBeepOnPartitionSet(0))
                    {
                        parent1L.SetValue(true);
                        parent1R.SetValue(true);
                    }
                }
                else
                {
                    if (pc.ReceivedBeepOnPartitionSet(0))
                        parent1L.SetValue(true);
                    if (pc.ReceivedBeepOnPartitionSet(1))
                        parent1R.SetValue(true);
                }

                if (nbrPortalDir2 == Direction.NONE)
                {
                    if (pc.ReceivedBeepOnPartitionSet(2))
                    {
                        parent2L.SetValue(true);
                        parent2R.SetValue(true);
                    }
                }
                else
                {
                    if (pc.ReceivedBeepOnPartitionSet(2))
                        parent2L.SetValue(true);
                    if (pc.ReceivedBeepOnPartitionSet(3))
                        parent2R.SetValue(true);
                }

                round.SetValue(round + 1);
            }

            SetColor();
        }


        // Helpers

        /// <summary>
        /// Sets up a simple circuit connecting all pins along the given axis
        /// into partition set 0.
        /// </summary>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void SetupSimplePortalCircuit(Direction portalDir)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            int[] pinIds = new int[PinsPerEdge * 2];
            for (int i = 0; i < PinsPerEdge; i++)
            {
                pinIds[2 * i] = pc.GetPinAt(portalDir, i).Id;
                pinIds[2 * i + 1] = pc.GetPinAt(portalDir.Opposite(), i).Id;
            }
            pc.MakePartitionSet(pinIds, 0);
            SetPlannedPinConfiguration(pc);
        }

        /// <summary>
        /// TODO
        /// 
        /// 
        /// Sets up one circuit for each side of a portal along
        /// the given axis, split at each neighbor edge representative.
        /// The "top" partition set(s) will have IDs 0 (and 1)
        /// and the "bottom" partition set(s) will have IDs 2 (and 3).
        /// </summary>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void SetupPortalNeighborCircuits(Direction portalDir)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            if (nbrPortalDir1 == Direction.NONE || !marker1.GetCurrentValue())
            {
                // No neighbor edge, simply connect
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id, pc.GetPinAt(portalDir, PinsPerEdge - 1).Id }, 0);
                pc.SetPartitionSetPosition(0, new Vector2((portalDir.ToInt() + 1.5f) * 60f, 0.5f));
            }
            else
            {
                // Have neighbor, split the two pins
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id }, 0);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir, PinsPerEdge - 1).Id }, 1);
            }

            if (nbrPortalDir2 == Direction.NONE || !marker2.GetCurrentValue())
            {
                // No neighbor edge, simply connect
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id, pc.GetPinAt(portalDir, 0).Id }, 2);
                pc.SetPartitionSetPosition(2, new Vector2((portalDir.Opposite().ToInt() + 1.5f) * 60f, 0.5f));
            }
            else
            {
                // Have neighbor, split the two pins
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id }, 2);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir, 0).Id }, 3);
            }

            SetPlannedPinConfiguration(pc);
        }

        /// <summary>
        /// Sets up a circuit along a portal that is used to compare the number
        /// of portal neighbors to 3. We use the 4 pins as "lanes" representing
        /// 0, 1, 2 and >= 3 neighbors. Every amoebot with at least one neighbor
        /// merges each lane into the next higher one according to how many
        /// neighbors it has (up to 2). The leftmost amoebot sends a beep on
        /// the lane corresponding to its own number of neighbors. If the
        /// number ever reaches the >= 3 lane, all amoebots on the portal will
        /// receive the beep.
        /// </summary>
        /// <param name="portalDir">The portal direction.</param>
        /// <param name="numNbrs">How many neighbors this amoebot
        /// contributes to the sum.</param>
        private void SetupCountingCircuit(Direction portalDir, int numNbrs)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            if (numNbrs == 0)
            {
                for (int i = 0; i < PinsPerEdge; i++)
                {
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), i).Id, pc.GetPinAt(portalDir, 3 - i).Id }, i);
                }
            }
            else if (numNbrs == 1)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id, pc.GetPinAt(portalDir, 2).Id }, 0);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 1).Id, pc.GetPinAt(portalDir, 1).Id }, 1);
                pc.MakePartitionSet(new int[] {
                    pc.GetPinAt(portalDir.Opposite(), 2).Id,
                    pc.GetPinAt(portalDir.Opposite(), 3).Id,
                    pc.GetPinAt(portalDir, 0).Id }, 2);
            }
            else if (numNbrs == 2)
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id, pc.GetPinAt(portalDir, 1).Id }, 0);
                pc.MakePartitionSet(new int[] {
                    pc.GetPinAt(portalDir.Opposite(), 1).Id,
                    pc.GetPinAt(portalDir.Opposite(), 2).Id,
                    pc.GetPinAt(portalDir.Opposite(), 3).Id,
                    pc.GetPinAt(portalDir, 0).Id }, 2);
            }
            pc.SetPSPlacementMode(PSPlacementMode.LINE_ROTATED);
            pc.SetLineRotation(90f + mainDir.ToInt() * 60f);
            SetPlannedPinConfiguration(pc);
        }

        /// <summary>
        /// Sets up two circuits along the portal, each being split at all
        /// marked amoebots for that side.
        /// </summary>
        /// <param name="portalDir">The portal direction.</param>
        private void SetupMarkerCircuit(Direction portalDir)
        {
            PinConfiguration pc = GetContractedPinConfiguration();
            if (!marker1.GetCurrentValue())
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id, pc.GetPinAt(portalDir, PinsPerEdge - 1).Id }, 0);
            }
            if (!marker2.GetCurrentValue())
            {
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id, pc.GetPinAt(portalDir, 0).Id }, 1);
            }
            SetPlannedPinConfiguration(pc);
        }

        /// <summary>
        /// Sets the neighbor portal flags of this amoebot. Direction 1
        /// indicates the "top" neighbor edge, direction 2 the "bottom"
        /// neighbor edge. The direction will be unequal to
        /// <see cref="Direction.NONE"/> if and only if this amoebot
        /// is the unique amoebot with the edge to that neighboring portal.
        /// </summary>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void DeterminePortalNeighbors(Direction portalDir)
        {
            // Reset neighbor directions
            nbrPortalDir1.SetValue(Direction.NONE);
            nbrPortalDir2.SetValue(Direction.NONE);

            // Check which neighbors exist
            Direction d60p = portalDir.Rotate60(1);
            Direction d120p = portalDir.Rotate60(2);
            Direction d60n = portalDir.Rotate60(-1);
            Direction d120n = portalDir.Rotate60(-2);
            bool nbr60p = HasNeighborAt(d60p);
            bool nbr120p = HasNeighborAt(d120p);
            bool nbr60n = HasNeighborAt(d60n);
            bool nbr120n = HasNeighborAt(d120n);
            bool nbrPortal = HasNeighborAt(portalDir.Opposite());

            if (nbrPortal)
            {
                // We have a predecessor in the portal, neighbor portal must be at 60 position
                if (!nbr120p && nbr60p)
                {
                    nbrPortalDir1.SetValue(d60p);
                }

                if (!nbr120n && nbr60n)
                {
                    nbrPortalDir2.SetValue(d60n);
                }
            }
            else
            {
                // We have no predecessor in the portal, neighbor portal may be at 120 position
                if (nbr120p)
                {
                    nbrPortalDir1.SetValue(d120p);
                }
                else if (nbr60p)
                {
                    nbrPortalDir1.SetValue(d60p);
                }

                if (nbr120n)
                {
                    nbrPortalDir2.SetValue(d120n);
                }
                else if (nbr60n)
                {
                    nbrPortalDir2.SetValue(d60n);
                }
            }
        }

        /// <summary>
        /// Checks whether this amoebot should mark one of its edges for
        /// ETT because it represents a Q-portal. This can be implemented
        /// by marking every source amoebot or only one representative of
        /// each portal.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot is a source.</returns>
        private bool IsQPortalMarker()
        {
            return isSource.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether or not this amoebot's portal is in Q'.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot's portal contains
        /// a source or is an augmentation portal.</returns>
        private bool IsOnQPrime()
        {
            return sourcePortal.GetCurrentValue() || augPortal.GetCurrentValue();
        }

        /// <summary>
        /// Sets the color of this amoebot according to its
        /// current phase and state.
        /// </summary>
        private void SetColor()
        {
            if (isLeader.GetCurrentValue())
                SetMainColor(leaderColor);
            else if (isSource.GetCurrentValue())
                SetMainColor(sourceColor);
            else if (isDest.GetCurrentValue() && phase.GetCurrentValue() == Phase.SETUP)
                SetMainColor(destColor);
            else if (marker1.GetCurrentValue() || marker2.GetCurrentValue())
                SetMainColor(markerColor);
            else if (sourcePortal.GetCurrentValue())
                SetMainColor(sourcePortalColor);
            else if (augPortal.GetCurrentValue())
                SetMainColor(augPortalColor);
            else
                SetMainColor(baseColor);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class MultiSourceSPInitializer : InitializationMethod
    {
        public MultiSourceSPInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 200, int numSources = 5, int numDestinations = 15, float holeProb = 0.05f)
        {
            if (numParticles < 1)
            {
                Log.Error("Number of particles must be at least 1");
                throw new SimulatorStateException("Invalid number of particles");
            }
            if (numSources < 1)
            {
                Log.Error("Number of sources must be at least 1");
                throw new SimulatorStateException("Invalid number of sources");
            }

            // Generate positions without holes
            List<Vector2Int> positions = GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb, true);
            foreach (Vector2Int v in positions)
                AddParticle(v);

            InitializationParticle ip;

            // Choose random sources and destinations
            // First source is the leader
            numSources = Mathf.Clamp(numSources, 0, positions.Count);
            numDestinations = Mathf.Clamp(numDestinations, 0, positions.Count - numSources);

            for (int i = 0; i < numSources + numDestinations; i++)
            {
                int idx = Random.Range(0, positions.Count);
                if (TryGetParticleAt(positions[idx], out ip))
                {
                    if (i == 0)
                        ip.SetAttribute("isLeader", true);
                    if (i < numSources)
                        ip.SetAttribute("isSource", true);
                    else
                        ip.SetAttribute("isDest", true);
                }
                positions.RemoveAt(idx);
            }
        }
    }

} // namespace AS2.Algos.MultiSourceSP
