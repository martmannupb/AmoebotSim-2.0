using System.Collections.Generic;
using AS2.Sim;
using AS2.Subroutines.ETT;
using AS2.Subroutines.PASC;
using UnityEngine;
using static AS2.Constants;

namespace AS2.Algos.MultiSourceSP
{

    /// <summary>
    /// Implements the (k, l)-SPF algorithm.
    /// </summary>

    // PHASE 1: SETUP

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
    //  - Setup neighbor portal circuits, beep on the circuit belonging to the parent
    //      - One circuit for each neighboring region, only in Q' portals

    // Round 6:
    //  - Receive neighbor beeps and set parent direction


    // PHASE 2: BASE CASE

    // We first perform the line algorithm on each source portal
    //  -> First the top side, then the bottom side

    // We then perform the line + propagation step for each of the two portals,
    // checking whether the portal exists and has sources first
    //  -> If a portal does not exist or has no sources, we skip and wait for the other regions to finish
    //  -> After this step, each region has 0, 1 or 2 shortest path trees

    // Finally, we perform a merge step on each region
    //  -> An actual merge procedure with PASC is only required if we have 2 trees, otherwise we just take
    //      the one we already have or stay without a tree

    // Round 0:
    //  - If counter > 1, jump to round 4
    //  - Setup directional portal circuits split at top/bottom markers (depending on counter value)
    //  - Sources send beeps in both directions

    // Round 1:
    //  - Listen for directional beeps
    //      - Non-sources that received only one beep save the source direction as parent direction
    //          - If we are the secondary portal of a region, we store the parent direction as the *secondary* direction
    //      - Non-sources that received two beeps setup PASC for both directions (ending at top/bottom markers)
    //      - Sources setup a PASC leader in each direction they received a beep from
    //      - Initialize comparison to EQUAL
    //      - Start PASC

    // Round 2:
    //  - Receive PASC beep
    //      - Update comparison
    //  - Setup global circuit
    //  - Beep if we became passive in this round

    // Round 3:
    //  - If no beep on global circuit:
    //      - Everybody is done
    //      - Set parent direction based on comparison result
    //      - Increment counter
    //      - Go to round 0
    //  - Otherwise:
    //      - Setup PASC circuit and send beep
    //      - Go to round 2

    // Find out which of the following propagation routines we have to run in our region

    // Round 4:
    //  - Setup 2 regional circuits
    //  - Sources in primary portal beep on first circuit if the region is "below" the portal
    //      - Beep on second circuit otherwise

    // Round 5:
    //  - Receive beeps on regional circuits
    //      - Remember whether we have received a beep and for which direction
    //  - Keep the 2 regional circuits
    //  - Sources beep again, this time for secondary portal

    // Round 6:
    //  - Receive regional circuit beeps again
    //      - Store results

    // Round 7:
    //  TODO




    // -- Do this later --
    //  -> When deciding whether or not to perform second propagation and merge

    //  - Setup regional circuits
    //      - One circuit for each region
    //  - Beep if the region is our parent, i.e., the region has two adjacent portals

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
            BASE_CASE,
            FINAL_PRUNE,
            FINISHED
        }

        private const Direction mainDir = Direction.E;  // The main direction defining the orientation of the portals

        class Instance
        {
            public ParticleAttribute<bool> incidentPortalIsParent;
            public ParticleAttribute<Direction> parentDir1;
            public ParticleAttribute<Direction> parentDir2;
            public ParticleAttribute<bool> regionHasPrimaryPortalSource;
            public ParticleAttribute<bool> primaryPortalIsAbove;
            public ParticleAttribute<bool> regionHasSecondaryPortalSource;
            public ParticleAttribute<bool> secondaryPortalIsAbove;

            public Instance(ParticleAlgorithm algo, int idx)
            {
                incidentPortalIsParent = algo.CreateAttributeBool("Portal Parent [" + idx + "]", false);
                parentDir1 = algo.CreateAttributeDirection("Parent Dir 1 [" + idx + "]", Direction.NONE);
                parentDir2 = algo.CreateAttributeDirection("Parent Dir 2 [" + idx + "]", Direction.NONE);
                regionHasPrimaryPortalSource = algo.CreateAttributeBool("Primary Portal Src [" + idx + "]", false);
                primaryPortalIsAbove = algo.CreateAttributeBool("Primary Portal Above [" + idx + "]", false);
                regionHasSecondaryPortalSource = algo.CreateAttributeBool("Secondary Portal Src [" + idx + "]", false);
                secondaryPortalIsAbove = algo.CreateAttributeBool("Secondary Portal Above [" + idx + "]", false);
            }
        }

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

        Instance[] instances = new Instance[4];         // Instances store state information for the possible 4 incident regions
                                                        // A marker can be part of up to 4 regions simultaneously
        ParticleAttribute<int> numInstances;            // Stores how many instances are currently active

        ParticleAttribute<bool> marker1;                // Whether we are marked for the first neighbor
        ParticleAttribute<bool> marker2;                // Whether we are marked for the second neighbor

        ParticleAttribute<int> counter;                 // Generic counter used to perform multiple iterations of the same task
        ParticleAttribute<bool> pasc1Participant;
        ParticleAttribute<bool> pasc2Participant;
        ParticleAttribute<Comparison> comparison;

        SubETT ett;
        SubPASC2 pasc1;
        SubPASC2 pasc2;

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

            for (int i = 0; i < 4; i++)
                instances[i] = new Instance(this, i);
            numInstances = CreateAttributeInt("Num Instances", 1);

            marker1 = CreateAttributeBool("Marker 1", false);
            marker2 = CreateAttributeBool("Marker 2", false);

            counter = CreateAttributeInt("Counter", 0);
            pasc1Participant = CreateAttributeBool("PASC 1 Part.", false);
            pasc2Participant = CreateAttributeBool("PASC 2 Part.", false);
            comparison = CreateAttributeEnum<Comparison>("Comparison", Comparison.EQUAL);

            ett = new SubETT(p);
            pasc1 = new SubPASC2(p);
            pasc2 = new SubPASC2(p);

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
                case Phase.BASE_CASE:
                    ActivateBaseCase();
                    break;
            }
        }

        private void ActivateSetupPhase()
        {
            int r = round;
            switch (r)
            {
                // PHASE 1: SETUP
                case 0:
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
                    break;
                case 1:
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
                    break;
                case 2:
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
                    break;
                case 3:
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
                    break;
                case 4:
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
                    break;
                case 5:
                    {
                        // Remove marker
                        // And setup number of used instances according to marker state
                        if (IsOnQPrime())
                        {
                            int nInst = 2;
                            PinConfiguration pc = GetCurrentPinConfiguration();
                            if (marker1)
                            {
                                if (!HasNeighborAt(mainDir.Opposite()) || pc.GetPinAt(mainDir.Opposite(), 0).PartitionSet.ReceivedBeep())
                                    marker1.SetValue(false);
                                else
                                    nInst++;
                            }
                            if (marker2)
                            {
                                if (!HasNeighborAt(mainDir.Opposite()) || pc.GetPinAt(mainDir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                                    marker2.SetValue(false);
                                else
                                    nInst++;
                            }

                            numInstances.SetValue(nInst);
                        }

                        // Send beeps indicating parent portals
                        SetupPortalNeighborCircuits(mainDir);
                        if (nbrPortalDir1 != Direction.NONE || nbrPortalDir2 != Direction.NONE)
                        {
                            PinConfiguration pc = GetPlannedPinConfiguration();
                            // Comparison result GREATER means that the edge indicates the parent
                            int pSet = 0;
                            if (nbrPortalDir1 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir1) == Comparison.GREATER)
                            {
                                // Marker sends only to the second instance
                                if (marker1.GetCurrentValue())
                                    pSet++;
                                pc.SendBeepOnPartitionSet(pSet);
                            }
                            pSet++;
                            if (nbrPortalDir2 != Direction.NONE && ett.GetComparisonResult(nbrPortalDir2) == Comparison.GREATER)
                            {
                                if (marker2.GetCurrentValue())
                                    pSet++;
                                pc.SendBeepOnPartitionSet(pSet);
                            }
                        }

                        round.SetValue(round + 1);
                    }
                    break;
                case 6:
                    {
                        // Portal amoebots receive parent beeps
                        if (numInstances > 1)
                        {
                            PinConfiguration pc = GetCurrentPinConfiguration();
                            // Have one partition set for each instance
                            for (int i = 0; i < numInstances; i++)
                            {
                                if (pc.ReceivedBeepOnPartitionSet(i))
                                    instances[i].incidentPortalIsParent.SetValue(true);
                            }
                        }

                        phase.SetValue(Phase.BASE_CASE);
                        round.SetValue(0);
                    }
                    break;
            }

            SetColor();
        }

        private void ActivateBaseCase()
        {
            int r = round;
            switch (r)
            {
                case 0:
                    {
                        // Line algorithm finished? => Continue
                        if (counter > 1)
                        {
                            counter.SetValue(0);
                            round.SetValue(4);
                            break;
                        }

                        // Setup directional portal circuits and split at top or bottom markers and sources
                        if (sourcePortal)
                        {
                            PinConfiguration pc = GetContractedPinConfiguration();
                            SetupDirectionalPortalCircuit(pc, mainDir, isSource || (counter == 0 && marker1 || counter == 1 && marker2));
                            SetPlannedPinConfiguration(pc);

                            // Sources send beeps in both directions
                            if (isSource)
                            {
                                pc.SendBeepOnPartitionSet(0);
                                pc.SendBeepOnPartitionSet(3);
                            }
                        }
                        round.SetValue(round + 1);
                    }
                    break;
                case 1:
                    {
                        // Listen for directional beeps
                        if (sourcePortal)
                        {
                            PinConfiguration pc = GetCurrentPinConfiguration();
                            bool beepLeft = pc.GetPinAt(mainDir.Opposite(), PinsPerEdge - 1).PartitionSet.ReceivedBeep();
                            bool beepRight = pc.GetPinAt(mainDir, PinsPerEdge - 1).PartitionSet.ReceivedBeep();
                            if (isSource)
                            {
                                // Setup one PASC instance for each received beep
                                if (beepLeft)
                                {
                                    pasc1.Init(null, new List<Direction>() { mainDir.Opposite() }, 0, 1, 0, 1, true);
                                    pasc1Participant.SetValue(true);
                                }

                                if (beepRight)
                                {
                                    pasc2.Init(null, new List<Direction>() { mainDir }, 0, 1, 2, 3, true);
                                    pasc2Participant.SetValue(true);
                                }
                            }
                            else if (counter == 0 && marker1 || counter == 1 && marker2)
                            {
                                if (beepLeft)
                                {
                                    int i = counter == 0 ? 0 : (marker1 ? 2 : 1);
                                    // If this instance represents a secondary portal, we store the parent direction as secondary direction
                                    if (instances[i].incidentPortalIsParent)
                                        instances[i].parentDir2.SetValue(mainDir.Opposite());
                                    else
                                        instances[i].parentDir1.SetValue(mainDir.Opposite());
                                }

                                if (beepRight)
                                {
                                    int i = counter == 0 ? 1 : (marker1 ? 3 : 2);
                                    if (instances[i].incidentPortalIsParent)
                                        instances[i].parentDir2.SetValue(mainDir);
                                    else
                                        instances[i].parentDir1.SetValue(mainDir);
                                }
                            }
                            else
                            {
                                // Sources on both sides, setup two PASC instances
                                if (beepLeft && beepRight)
                                {
                                    comparison.SetValue(Comparison.EQUAL);
                                    pasc1.Init(new List<Direction>() { mainDir }, new List<Direction>() { mainDir.Opposite() }, 0, 1, 0, 1, false);
                                    pasc2.Init(new List<Direction>() { mainDir.Opposite() }, new List<Direction>() { mainDir }, 0, 1, 2, 3, false);
                                    pasc1Participant.SetValue(true);
                                    pasc2Participant.SetValue(true);
                                }
                                // Only one side: Set that side as parent
                                else if (beepLeft && !beepRight)
                                {
                                    int i = counter == 0 ? 0 : (marker1 ? 2 : 1);
                                    // If this instance represents a secondary portal, we store the parent direction as secondary direction
                                    if (instances[i].incidentPortalIsParent)
                                        instances[i].parentDir2.SetValue(mainDir.Opposite());
                                    else
                                        instances[i].parentDir1.SetValue(mainDir.Opposite());
                                }
                                else if (beepRight && !beepLeft)
                                {
                                    int i = 0;
                                    if (marker1)
                                        i++;
                                    if (counter != 0)
                                    {
                                        i++;
                                        if (marker2)
                                            i++;
                                    }
                                    if (instances[i].incidentPortalIsParent)
                                        instances[i].parentDir2.SetValue(mainDir);
                                    else
                                        instances[i].parentDir1.SetValue(mainDir);
                                }
                            }

                            // Start PASC
                            bool runPasc1 = pasc1Participant.GetCurrentValue();
                            bool runPasc2 = pasc2Participant.GetCurrentValue();
                            if (runPasc1 || runPasc2)
                            {
                                if (runPasc1)
                                    pasc1.SetupPC(pc);
                                if (runPasc2)
                                    pasc2.SetupPC(pc);
                                SetPlannedPinConfiguration(pc);
                                if (runPasc1)
                                    pasc1.ActivateSend();
                                if (runPasc2)
                                    pasc2.ActivateSend();
                            }
                        }
                        round.SetValue(round + 1);
                    }
                    break;
                case 2:
                    {
                        // Receive PASC beeps
                        if (pasc1Participant)
                            pasc1.ActivateReceive();
                        if (pasc2Participant)
                            pasc2.ActivateReceive();

                        // Update comparison
                        if (!isSource && pasc1Participant && pasc2Participant)
                        {
                            bool b1 = pasc1.GetReceivedBit() != 0;
                            bool b2 = pasc2.GetReceivedBit() != 0;
                            if (b1 && !b2)
                                comparison.SetValue(Comparison.GREATER);
                            else if (b2 && !b1)
                                comparison.SetValue(Comparison.LESS);
                        }

                        // Setup global circuit
                        PinConfiguration pc = GetContractedPinConfiguration();
                        pc.SetToGlobal(0);
                        SetPlannedPinConfiguration(pc);

                        // Beep if we became passive
                        if (pasc1Participant && pasc1.BecamePassive() || pasc2Participant && pasc2.BecamePassive())
                            pc.SendBeepOnPartitionSet(0);

                        round.SetValue(round + 1);
                    }
                    break;
                case 3:
                    {
                        // Listen for continuation beep on global circuit
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        if (pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // Continue
                            if (pasc1Participant || pasc2Participant)
                            {
                                pc.SetToSingleton();
                                if (pasc1Participant)
                                    pasc1.SetupPC(pc);
                                if (pasc2Participant)
                                    pasc2.SetupPC(pc);
                                SetPlannedPinConfiguration(pc);
                                if (pasc1Participant)
                                    pasc1.ActivateSend();
                                if (pasc2Participant)
                                    pasc2.ActivateSend();
                            }
                            round.SetValue(2);
                        }
                        else
                        {
                            // Setup parent direction based on comparison result
                            if (!isSource && pasc1Participant && pasc2Participant)
                            {
                                Direction pDir = comparison.GetCurrentValue() == Comparison.GREATER ? mainDir.Opposite() : mainDir;
                                int i = counter == 0 ? 0 : (marker1 ? 2 : 1);
                                if (instances[i].incidentPortalIsParent)
                                    instances[i].parentDir2.SetValue(pDir);
                                else
                                    instances[i].parentDir1.SetValue(pDir);
                            }
                            // Increment counter and do next iteration
                            counter.SetValue(counter + 1);
                            round.SetValue(0);
                        }
                    }
                    break;
                case 4:
                    {
                        // Setup 2 circuits for each region
                        PinConfiguration pc = GetContractedPinConfiguration();
                        SetupRegionCircuits(pc, mainDir);
                        SetPlannedPinConfiguration(pc);

                        // Sources on primary portal beep on first circuit if the region is "below" the portal
                        if (isSource)
                        {
                            for (int i = 0; i < numInstances; i++)
                            {
                                if (!instances[i].incidentPortalIsParent)
                                {
                                    if (i > 1 || i > 0 && !marker1)
                                        pc.SendBeepOnPartitionSet(2 * i);
                                    else
                                        pc.SendBeepOnPartitionSet(2 * i + 1);
                                }
                            }
                        }

                        round.SetValue(r + 1);
                    }
                    break;
                case 5:
                    {
                        // Receive source beeps on the regional circuits
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        for (int i = 0; i < numInstances; i++)
                        {
                            if (pc.ReceivedBeepOnPartitionSet(2 * i))
                            {
                                instances[i].regionHasPrimaryPortalSource.SetValue(true);
                                instances[i].primaryPortalIsAbove.SetValue(true);
                            }
                            else if (pc.ReceivedBeepOnPartitionSet(2 * i + 1))
                            {
                                instances[i].regionHasPrimaryPortalSource.SetValue(true);
                                instances[i].primaryPortalIsAbove.SetValue(false);
                            }
                        }

                        // Send beeps like before but for secondary portal
                        SetPlannedPinConfiguration(pc);
                        if (isSource)
                        {
                            for (int i = 0; i < numInstances; i++)
                            {
                                if (instances[i].incidentPortalIsParent)
                                {
                                    if (i > 1 || i > 0 && !marker1)
                                        pc.SendBeepOnPartitionSet(2 * i);
                                    else
                                        pc.SendBeepOnPartitionSet(2 * i + 1);
                                }
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
                case 6:
                    {
                        // Receive source beeps from secondary portal
                        PinConfiguration pc = GetCurrentPinConfiguration();
                        for (int i = 0; i < numInstances; i++)
                        {
                            if (pc.ReceivedBeepOnPartitionSet(2 * i))
                            {
                                instances[i].regionHasSecondaryPortalSource.SetValue(true);
                                instances[i].secondaryPortalIsAbove.SetValue(true);
                            }
                            else if (pc.ReceivedBeepOnPartitionSet(2 * i + 1))
                            {
                                instances[i].regionHasSecondaryPortalSource.SetValue(true);
                                instances[i].secondaryPortalIsAbove.SetValue(false);
                            }
                        }
                        round.SetValue(r + 1);
                    }
                    break;
            }

            // TODO: Do this later
            // Setup region circuits and send beep if we are not the "root" of a region
            //SetupRegionCircuits(mainDir);
            //pc = GetPlannedPinConfiguration();
            //if (parent1L.GetCurrentValue())
            //    pc.SendBeepOnPartitionSet(0);
            //else if (marker1 && parent1R.GetCurrentValue())
            //    pc.SendBeepOnPartitionSet(1);

            //if (parent2L.GetCurrentValue())
            //    pc.SendBeepOnPartitionSet(2);
            //else if (marker2 && parent2R.GetCurrentValue())
            //    pc.SendBeepOnPartitionSet(3);

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
        /// Sets up one circuit for each side of a portal along
        /// the given axis, split at each marker.
        /// The partition set IDs correspond to the instance indices.
        /// </summary>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void SetupPortalNeighborCircuits(Direction portalDir)
        {
            PinConfiguration pc = GetContractedPinConfiguration();

            // Setup first partition set with ID 0
            int id = 0;
            pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id }, id);
            int pin2 = pc.GetPinAt(portalDir, PinsPerEdge - 1).Id;
            // Connect if we are not a marker
            if (!marker1.GetCurrentValue())
            {
                pc.GetPartitionSet(id).AddPin(pin2);
                pc.SetPartitionSetPosition(id, new Vector2((portalDir.ToInt() + 1.5f) * 60f, 0.5f));
            }
            else
            {
                // Split
                id++;
                pc.MakePartitionSet(new int[] { pin2 }, 1);
            }

            id++;
            // Same for the other half
            pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id }, id);
            pin2 = pc.GetPinAt(portalDir, 0).Id;
            // Connect if we are not a marker
            if (!marker2.GetCurrentValue())
            {
                pc.GetPartitionSet(id).AddPin(pin2);
                pc.SetPartitionSetPosition(id, new Vector2((portalDir.Opposite().ToInt() + 1.5f) * 60f, 0.5f));
            }
            else
            {
                // Split
                id++;
                pc.MakePartitionSet(new int[] { pin2 }, id);
            }

            SetPlannedPinConfiguration(pc);
        }

        /// <summary>
        /// Sets up two circuits for each region. The partition set IDs
        /// will be 2i and 2i + 1 for instance i.
        /// </summary>
        /// <param name="pc">The pin configuration to modify.</param>
        /// <param name="portalDir">The direction of the portal axis.</param>
        private void SetupRegionCircuits(PinConfiguration pc, Direction portalDir)
        {
            if (numInstances == 1)
            {
                // Simply setup two star configs
                bool[] inverted = new bool[6];
                int d = portalDir.ToInt();
                for (int i = 0; i < 3; i++)
                    inverted[(d + 3 + i) % 6] = true;
                pc.SetStarConfig(0, inverted, 0);
                pc.SetStarConfig(1, inverted, 1);
            }
            else
            {
                // Setup 8 partition sets (worst case, 4 instances)
                // Top left
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 1).Id, pc.GetPinAt(portalDir.Rotate60(2), 0).Id }, 0);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), 0).Id, pc.GetPinAt(portalDir.Rotate60(2), 1).Id }, 1);
                // Top right
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir, PinsPerEdge - 2).Id, pc.GetPinAt(portalDir.Rotate60(1), 0).Id }, 2);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir, PinsPerEdge - 1).Id, pc.GetPinAt(portalDir.Rotate60(1), 1).Id }, 3);
                // Bottom left
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id, pc.GetPinAt(portalDir.Rotate60(-2), PinsPerEdge - 1).Id }, 4);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 2).Id, pc.GetPinAt(portalDir.Rotate60(-2), PinsPerEdge - 2).Id }, 5);
                // Bottom right
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir, 0).Id, pc.GetPinAt(portalDir.Rotate60(-1), PinsPerEdge - 1).Id }, 6);
                pc.MakePartitionSet(new int[] { pc.GetPinAt(portalDir, 1).Id, pc.GetPinAt(portalDir.Rotate60(-1), PinsPerEdge - 2).Id }, 7);

                // Merge partition sets if necessary
                if (!marker2)
                {
                    pc.GetPartitionSet(4).AddPins(pc.GetPartitionSet(6).GetPinIds());
                    pc.GetPartitionSet(5).AddPins(pc.GetPartitionSet(7).GetPinIds());
                }
                if (!marker1)
                {
                    pc.GetPartitionSet(0).AddPins(pc.GetPartitionSet(2).GetPinIds());
                    pc.GetPartitionSet(1).AddPins(pc.GetPartitionSet(3).GetPinIds());
                    // Shift other partition sets
                    if (marker2)
                    {
                        pc.MakePartitionSet(pc.GetPartitionSet(4).GetPinIds(), 2);
                        pc.MakePartitionSet(pc.GetPartitionSet(5).GetPinIds(), 3);
                        pc.MakePartitionSet(pc.GetPartitionSet(6).GetPinIds(), 4);
                        pc.MakePartitionSet(pc.GetPartitionSet(7).GetPinIds(), 5);
                    }
                    else
                    {
                        pc.MakePartitionSet(pc.GetPartitionSet(4).GetPinIds(), 2);
                        pc.MakePartitionSet(pc.GetPartitionSet(5).GetPinIds(), 3);
                    }
                }
            }



            //int[] pinsTop = new int[] { pc.GetPinAt(portalDir, PinsPerEdge - 1).Id, pc.GetPinAt(portalDir.Rotate60(1), 0).Id, pc.GetPinAt(portalDir.Rotate60(2), 0).Id, pc.GetPinAt(portalDir.Opposite(), 0).Id };
            //int[] pinsBot = new int[] { pc.GetPinAt(portalDir, 0).Id, pc.GetPinAt(portalDir.Rotate60(-1), 0).Id, pc.GetPinAt(portalDir.Rotate60(-2), 0).Id, pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id };

            //if (!IsOnQPrime())
            //{
            //    pc.SetToGlobal(0);
            //}
            //else
            //{
            //    if (marker1.GetCurrentValue())
            //    {
            //        pc.MakePartitionSet(new int[] { pinsTop[3] }, 0);
            //        pc.MakePartitionSet(new int[] { pinsTop[0], pinsTop[1], pinsTop[2] }, 1);
            //        pc.SetPartitionSetPosition(1, new Vector2((portalDir.ToInt() + 1) * 60f, 0.6f));
            //    }
            //    else
            //    {
            //        pc.MakePartitionSet(pinsTop, 0);
            //        pc.SetPartitionSetPosition(0, new Vector2((portalDir.ToInt() + 1.5f) * 60f, 0.5f));
            //    }

            //    if (marker2.GetCurrentValue())
            //    {
            //        pc.MakePartitionSet(new int[] { pinsBot[3] }, 2);
            //        pc.MakePartitionSet(new int[] { pinsBot[0], pinsBot[1], pinsBot[2] }, 3);
            //        pc.SetPartitionSetPosition(3, new Vector2((portalDir.ToInt() - 1) * 60f, 0.6f));
            //    }
            //    else
            //    {
            //        pc.MakePartitionSet(pinsBot, 2);
            //        pc.SetPartitionSetPosition(2, new Vector2((portalDir.ToInt() - 1.5f) * 60f, 0.5f));
            //    }
            //}

            //SetPlannedPinConfiguration(pc);
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
        /// Sets up two portal circuits along the given axis such that
        /// there is a "top" and a "bottom" circuit. If the circuit is
        /// not split, the partition sets have IDs 0 and 2, otherwise
        /// they have 0 and 1 for outgoing and incoming top and
        /// 2 and 3 for incoming and outgoing bottom.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="portalDir">The portal direction.</param>
        /// <param name="split">Whether the circuit should be split here.</param>
        private void SetupDirectionalPortalCircuit(PinConfiguration pc, Direction portalDir, bool split)
        {
            int pinTopOut = pc.GetPinAt(portalDir.Opposite(), 0).Id;
            int pinTopIn = pc.GetPinAt(portalDir, PinsPerEdge - 1).Id;
            int pinBotOut = pc.GetPinAt(portalDir, 0).Id;
            int pinBotIn = pc.GetPinAt(portalDir.Opposite(), PinsPerEdge - 1).Id;

            if (split)
            {
                pc.MakePartitionSet(new int[] { pinTopOut }, 0);
                pc.MakePartitionSet(new int[] { pinTopIn }, 1);
                pc.MakePartitionSet(new int[] { pinBotIn }, 2);
                pc.MakePartitionSet(new int[] { pinBotOut }, 3);
            }
            else
            {
                pc.MakePartitionSet(new int[] { pinTopOut, pinTopIn }, 0);
                pc.MakePartitionSet(new int[] { pinBotOut, pinBotIn }, 2);
                pc.SetPartitionSetPosition(0, new Vector2((portalDir.ToInt() + 1.5f) * 60, 0.5f));
                pc.SetPartitionSetPosition(2, new Vector2((portalDir.ToInt() - 1.5f) * 60, 0.5f));
            }
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
