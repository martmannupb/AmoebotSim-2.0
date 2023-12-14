using System.Collections.Generic;
using AS2.Sim;
using AS2.Subroutines.ETT;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.SingleSourceSP
{

    /// <summary>
    /// Implements the (1, l)-SPF algorithm.
    /// <para>
    /// In the first phase, we run the Euler Tour Technique (ETT) on the
    /// implicit portal graphs for the 3 axes, using the source amoebot as
    /// root and the destination amoebots to define the weight function.
    /// After each iteration, each amoebot knows which of its adjacent portals
    /// is a parent portal. For each neighbor, we count in how many portal graphs
    /// this neighbor is on a parent portal.<br/>
    /// At the end of the first phase, each amoebot elects one neighbor that
    /// occurred as parent portal twice to be its parent in the shortest path tree.
    /// </para>
    /// <para>
    /// In the second phase, we run ETT again on the resulting forest to prune all
    /// subtrees without destinations and to identify and remove components that
    /// are not connected to the root.
    /// </para>
    /// </summary>
    // Round 0:
    //  - Setup portal circuits and let the source amoebot beep to identify the source portal
    //  - Find the amoebots with edges to the neighbor portals
    // Round 1:
    //  - Identify source portal and representative based on the received beep
    //  - Let the destination amoebots beep to identify the destination portals
    // Round 2:
    //  - Identify the destination portals and their representatives
    // Round 3:
    //  - Setup ETT circuit:
    //      - Find the incoming and outgoing edges
    //      - Root representative chooses a place to split the cycle
    //      - Dest representatives choose exactly one outgoing edge to mark
    public class SingleSourceSPParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "1-SPF";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 4;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(SingleSourceSPInitializer).FullName;

        // Colors
        static readonly Color sourceColor = ColorData.Particle_Red;
        static readonly Color destColor = ColorData.Particle_BlueDark;
        static readonly Color sourcePortalColor = ColorData.Particle_Orange;
        static readonly Color destPortalColor = ColorData.Particle_Blue;
        static readonly Color mixedPortalColor = ColorData.Particle_Purple;

        // Declare attributes here
        ParticleAttribute<bool> isFinished;     // Finished flag
        ParticleAttribute<int> round;           // Round counter

        ParticleAttribute<bool> isSource;   // Source flag
        ParticleAttribute<bool> isDest;     // Destination flag

        ParticleAttribute<Direction> portalAxisDir;     // The current portal axis (should be E, NNE or NNW)
        ParticleAttribute<bool> onRootPortal;           // Flag for root/source portal (only visual)
        ParticleAttribute<bool> onDestPortal;           // Flag for destination portal (only visual)
        ParticleAttribute<bool> isRootRepr;             // Representative flag for root/source portal
        ParticleAttribute<bool> isDestRepr;             // Representative flag for destination portal
        ParticleAttribute<Direction> nbrPortalDir1;     // Direction of the first neighbor portal edge (portalDir + 60 or portalDir + 120 or NONE)
        ParticleAttribute<Direction> nbrPortalDir2;     // Direction of the second neighbor portal edge (portalDir - 60 or portalDir - 120 or NONE)

        // ETT subroutine
        SubETT ett;

        public SingleSourceSPParticle(Particle p) : base(p)
        {
            // Initialize the attributes here
            isFinished = CreateAttributeBool("Finished", false);
            round = CreateAttributeInt("Round", 0);

            isSource = CreateAttributeBool("Is Source", false);
            isDest = CreateAttributeBool("Is Destination", false);

            portalAxisDir = CreateAttributeDirection("Portal Axis Dir", Direction.E);
            onRootPortal = CreateAttributeBool("Root Portal", false);
            onDestPortal = CreateAttributeBool("Dest Portal", false);
            isRootRepr = CreateAttributeBool("Root Representative", false);
            isDestRepr = CreateAttributeBool("Destination Representative", false);
            nbrPortalDir1 = CreateAttributeDirection("Nbr Portal Dir 1", Direction.NONE);
            nbrPortalDir2 = CreateAttributeDirection("Nbr Portal Dir 2", Direction.NONE);

            ett = new SubETT(p);

            // Also, set the default initial color
            SetMainColor(Color.gray);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool isSource = false, bool isDest = false)
        {
            // This code is executed directly after the constructor
            this.isSource.SetValue(isSource);
            this.isDest.SetValue(isDest);

            if (this.isSource.GetCurrentValue())
                SetMainColor(sourceColor);
            else if (this.isDest.GetCurrentValue())
                SetMainColor(destColor);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            // Return true when this particle has terminated
            return isFinished;
        }

        // The movement activation method
        public override void ActivateMove()
        {
            // Just hide a few bonds
            foreach (Direction d in DirectionHelpers.Iterate60(Direction.E, 6))
            {
                if (d == portalAxisDir || d.Opposite() == portalAxisDir)
                    continue;
                if (d != nbrPortalDir1 && d != nbrPortalDir2)
                    HideBond(d);
            }
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            if (round == 0)
            {
                DeterminePortalNeighbors(portalAxisDir);
                SetupSimplePortalCircuit(portalAxisDir);
                if (isSource)
                {
                    PinConfiguration pc = GetPlannedPinConfiguration();
                    pc.SendBeepOnPartitionSet(0);
                }
            }
            else if (round == 1)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    // Received source portal beep
                    onRootPortal.SetValue(true);
                    // Become root/source representative if we have no predecessor
                    if (!HasNeighborAt(portalAxisDir.GetValue().Opposite()))
                        isRootRepr.SetValue(true);
                }
                SetPlannedPinConfiguration(pc);
                if (isDest)
                    pc.SendBeepOnPartitionSet(0);

                SetPortalColor();
            }
            else if (round == 2)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();
                if (pc.ReceivedBeepOnPartitionSet(0))
                {
                    // Received dest portal beep
                    onDestPortal.SetValue(true);
                    // Become dest representative if we have no predecessor
                    if (!HasNeighborAt(portalAxisDir.GetValue().Opposite()))
                        isDestRepr.SetValue(true);
                }

                SetPortalColor();
            }
            else if (round == 3)
            {
                // Setup ETT
                // First collect the neighbor directions (order is important)
                List<Direction> nbrDirs = new List<Direction>();
                if (HasNeighborAt(portalAxisDir))
                    nbrDirs.Add(portalAxisDir);
                if (nbrPortalDir1 != Direction.NONE)
                    nbrDirs.Add(nbrPortalDir1);
                if (HasNeighborAt(portalAxisDir.GetValue().Opposite()))
                    nbrDirs.Add(portalAxisDir.GetValue().Opposite());
                if (nbrPortalDir2 != Direction.NONE)
                    nbrDirs.Add(nbrPortalDir2);
                // Mark the first edge if we are a dest portal representative
                int markedIdx = isDestRepr.GetCurrentValue() ? 0 : -1;
                // Split if we are the source portal representative
                bool split = isRootRepr;
                ett.Init(nbrDirs.ToArray(), markedIdx, split);

                PinConfiguration pc = GetContractedPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);

                // Root representative sends a beep
                if (isRootRepr)
                    ett.ActivateSend();
            }
            else if (round > 3)
            {
                ett.ActivateReceive();

                if (ett.IsFinished())
                {
                    isFinished.SetValue(true);
                    return;
                }

                PinConfiguration pc = GetCurrentPinConfiguration();
                ett.SetupPinConfig(pc);
                SetPlannedPinConfiguration(pc);
                if (ett.IsTerminationRound())
                    ett.ActivateSend();
                else
                {
                    if (isRootRepr)
                        ett.ActivateSend();
                }
            }

            round.SetValue(round + 1);
        }

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

        private void SetPortalColor()
        {
            if (isSource.GetCurrentValue())
                SetMainColor(sourceColor);
            else if (isDest.GetCurrentValue())
                SetMainColor(destColor);
            else if (onRootPortal.GetCurrentValue() && onDestPortal.GetCurrentValue())
                SetMainColor(mixedPortalColor);
            else if (onRootPortal.GetCurrentValue())
                SetMainColor(sourcePortalColor);
            else if (onDestPortal.GetCurrentValue())
                SetMainColor(destPortalColor);
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class SingleSourceSPInitializer : InitializationMethod
    {
        public SingleSourceSPInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
        public void Generate(int numParticles = 100, int numDestinations = 10, float holeProb = 0.05f)
        {
            if (numParticles < 1)
            {
                Log.Error("Number of particles must be at least 1");
                throw new SimulatorStateException("Invalid number of particles");
            }

            // Generate positions without holes
            List<Vector2Int> positions = GenerateRandomConnectedPositions(Vector2Int.zero, numParticles, holeProb, true);
            foreach (Vector2Int v in positions)
                AddParticle(v);

            InitializationParticle ip;

            // Choose random source
            int sourceIdx = Random.Range(0, positions.Count);
            if (TryGetParticleAt(positions[sourceIdx], out ip))
                ip.SetAttribute("isSource", true);
            positions.RemoveAt(sourceIdx);

            // Choose random destinations
            numDestinations = Mathf.Min(numDestinations, positions.Count);
            for (int i = 0; i < numDestinations; i++)
            {
                int destIdx = Random.Range(0, positions.Count);
                if (TryGetParticleAt(positions[destIdx], out ip))
                    ip.SetAttribute("isDest", true);
                positions.RemoveAt(destIdx);
            }
        }
    }

} // namespace AS2.Algos.SingleSourceSP
