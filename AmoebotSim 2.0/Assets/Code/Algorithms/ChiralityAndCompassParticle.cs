using System.Collections.Generic;
using UnityEngine;

public enum CoinTossResult { HEADS, TAILS, FAILED }

public class DirectionMessage : Message
{
    public Direction direction;

    public DirectionMessage()
    {
        this.direction = Direction.NONE;
    }
    public DirectionMessage(Direction direction)
    {
        this.direction = direction;
    }

    public override Message Copy()
    {
        return new DirectionMessage(direction);
    }

    public override bool Equals(Message other)
    {
        DirectionMessage otherMsg = other as DirectionMessage;
        if (otherMsg == null)
        {
            return false;
        }
        else
        {
            return this == otherMsg || direction == otherMsg.direction;
        }
    }

    public override bool GreaterThan(Message other)
    {
        DirectionMessage otherMsg = other as DirectionMessage;
        if (otherMsg == null)
        {
            return true;
        }
        else
        {
            return direction > otherMsg.direction;
        }
    }
}

/// <summary>
/// Implementation of the chirality agreement and compass alignment
/// algorithms described in https://arxiv.org/abs/2105.05071v1.
/// <para>
/// Both phases use the following basic procedure:
/// <list type="number">
/// <item><description>
/// Find out which neighbors agree with the particle's chirality/compass direction
/// and setup the pin configuration to contain all of these neighbors in one
/// partition set. This establishes the regional circuit.
/// </description></item>
/// <item><description>
/// Beep on the regional circuit if there is a neighbor particle that does not have
/// the same chirality/compass direction. If no particle has beeped in this round,
/// there is only one region and the algorithm can terminate.
/// </description></item>
/// <item><description>
/// Let all candidates in the region perform a coin toss. All particles in the
/// region observe the result, which can be HEADS, TAILS, or FAILED.<br/>
/// If the result is FAILED, all candidates that tossed TAILS withdraw their
/// candidacy. All boundary particles send the coin toss result to their neighbors
/// that are not part of the region.
/// </description></item>
/// <item><description>
/// Regions merge as follows: A region that tossed TAILS tries to merge into
/// a neighboring region that has tossed anything other than TAILS. The
/// boundary particles beep in reserved rounds if their neighboring region is
/// a candidate for a merge. If a candidate exists, the first round in which a
/// beep was sent determines how the merge can be performed. If no beeps are
/// sent, no neighbor region was eligible and the next iteration starts. If
/// a merge is performed, all candidates in the region withdraw.
/// </description></item>
/// </list>
/// The algorithm is split into 7 individual rounds per iteration for the
/// chirality agreement phase and 11 rounds per iteration for the compass
/// alignment phase.
/// </para>
/// </summary>
public class ChiralityAndCompassParticle : ParticleAlgorithm
{
    // GLOBAL INFO - Used only for visualization
    public ParticleAttribute<bool> realChirality;       // Initialized to global chirality, changes when chirality is flipped
    public ParticleAttribute<Direction> realCompassDir; // Initialized to global compass direction, changes when offset is changed according to current real chirality

    private static readonly Color chir1CandColor = new Color(143f / 255f, 0f / 255f, 255f / 255f);
    private static readonly Color chir1NoCandColor = new Color(116f / 255f, 0f / 255f, 204f / 255f);
    private static readonly Color chir0CandColor = new Color(227f / 255f, 66f / 255f, 52f / 255f);
    private static readonly Color chir0NoCandColor = new Color(176f / 255f, 52f / 255f, 40f / 255f);
    private static readonly Color chir1CircuitColor = new Color(230f / 255f, 255f / 255f, 0f / 255f);
    private static readonly Color chir0CircuitColor = new Color(52f / 255f, 227f / 255f, 130f / 255f);
    // Color by direction, candidates get brighter colors
    // Amber, Vermillion, Magenta, Violet, Teal, Chartreuse
    // Brightness is 20% lower for non-candidates
    private static readonly Color[] compCandColors = new Color[]
    {
        new Color(255f / 255f, 191f / 255f, 0f / 255f),
        new Color(227f / 255f, 66f / 255f, 52f / 255f),
        new Color(255f / 255f, 0f / 255f, 255f / 255f),
        new Color(143f / 255f, 0f / 255f, 255f / 255f),
        new Color(0f / 255f, 128f / 255f, 128f / 255f),
        new Color(127f / 255f, 255f / 255f, 0f / 255f)
    };
    private static readonly Color[] compNoCandColor = new Color[]
    {
        new Color(204f / 255f, 153f / 255f, 0f / 255f),
        new Color(176f / 255f, 52f / 255f, 40f / 255f),
        new Color(204f / 255f, 0f / 255f, 204f / 255f),
        new Color(116f / 255f, 0f / 255f, 204f / 255f),
        new Color(0f / 255f, 77f / 255f, 77f / 255f),
        new Color(102f / 255f, 204f / 255f, 0f / 255f)
    };



    // Settings for changing compass direction and chirality
    // Compass offset is our new 0 direction encoded using the original chirality
    // If the chirality is reversed, directions now increase clockwise from the
    // compass offset
    private ParticleAttribute<Direction> compassOffset;
    private ParticleAttribute<bool> reverseChirality;

    // Flag that indicates whether we are in the chirality agreement or compass alignment phase
    private ParticleAttribute<bool> chiralityAgreementPhase;

    // Counter to synchronize particles, goes from 0 up to 10
    private ParticleAttribute<int> round;

    // Flag indicating whether we are currently a candidate
    private ParticleAttribute<bool> isCandidate;

    // Result of last coin toss. true means HEADS, false means TAILS
    private ParticleAttribute<bool> heads;

    // Flag indicating whether a candidate with HEADS has beeped in its respective round
    private ParticleAttribute<bool> beepedForHeads;

    // Termination flag
    private ParticleAttribute<bool> finished;

    // Array of bools telling us where we have a neighbor and where not
    // Indices are original directions
    private ParticleAttribute<bool>[] nbrs;

    // Flag that is only true in the first activation, used for initialization
    private ParticleAttribute<bool> firstActivation;

    // Flag indicating whether or not we have a regional circuit (i.e., at least one neighbor that shares our data)
    private ParticleAttribute<bool> hasRegionalCircuit;

    // The result of the last coin toss
    public ParticleAttribute<CoinTossResult> coinTossResult;

    // Stores the offsets of all non-region neighbors for the compass alignment phase; indices are original directions
    // Offset of -1 means there is no non-region neighbor in that direction
    private ParticleAttribute<int>[] nbrOffsets;

    // The offset that is used to decide how to merge in the compass alignment phase
    private ParticleAttribute<int> mergeOffset;

    public ChiralityAndCompassParticle(Particle p) : base(p)
    {
        SetMainColor(ColorData.Particle_Green);

        compassOffset = CreateAttributeDirection("Compass offset", DirectionHelpers.Cardinal(0));
        reverseChirality = CreateAttributeBool("Reverse chirality", false);
        chiralityAgreementPhase = CreateAttributeBool("Chirality agreement phase", true);
        round = CreateAttributeInt("Round", 0);
        isCandidate = CreateAttributeBool("Is candidate", true);
        heads = CreateAttributeBool("HEADS in last coin toss", false);
        beepedForHeads = CreateAttributeBool("Beeped for HEADS", false);
        finished = CreateAttributeBool("Finished", false);
        nbrs = new ParticleAttribute<bool>[6];
        for (int i = 0; i < 6; i++)
        {
            nbrs[i] = CreateAttributeBool("Nbrs [" + i + "]", false);
        }
        firstActivation = CreateAttributeBool("First activation", true);
        hasRegionalCircuit = CreateAttributeBool("Has regional circuit", false);
        coinTossResult = CreateAttributeEnum("Coin toss result", CoinTossResult.FAILED);
        nbrOffsets = new ParticleAttribute<int>[6];
        for (int i = 0; i < 6; i++)
        {
            nbrOffsets[i] = CreateAttributeInt("Nbr offset [" + i + "]", -1);
        }
        mergeOffset = CreateAttributeInt("Merge offset", -1);


        realChirality = CreateAttributeBool("Real chirality", true);
        realCompassDir = CreateAttributeDirection("Real compass dir", Direction.NONE);
    }

    public override int PinsPerEdge => 2;

    public static new InitializationUIHandler.SettingChirality Chirality => InitializationUIHandler.SettingChirality.Random;

    public static new Direction Compass => Direction.NONE;

    public override void ActivateMove()
    {

    }

    public override void ActivateBeep()
    {
        if (finished)
        {
            return;
        }
        else if (firstActivation)
        {
            firstActivation.SetValue(false);
            // Find all neighbors
            bool hasAnyNeighbor = false;
            for (int i = 0; i < 6; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                if (HasNeighborAt(d))
                {
                    nbrs[i].SetValue(true);
                    hasAnyNeighbor = true;
                }
            }
            // If we don't have any neighbors: Terminate immediately
            if (!hasAnyNeighbor)
            {
                SetMainColor(ColorData.Particle_Purple);
                finished.SetValue(true);
            }
            return;
        }

        switch (round)
        {
            case 0:
                Activate0();
                break;
            case 1:
                Activate1();
                break;
            case 2:
                Activate2();
                break;
            case 3:
                Activate3();
                break;
            case 4:
                Activate4();
                break;
            case 5:
                Activate5();
                break;
            case 6:
                Activate6();
                break;
            case 7:
            case 8:
            case 9:
                Activate7To9();
                break;
            case 10:
                Activate10();
                break;
            default:
                throw new System.ArgumentOutOfRangeException("Round counter increased too far.");
        }

        SetColor();
    }

    private void Activate0()
    {
        // Send chirality or compass information to all of our neighbors using singleton configuration
        PinConfiguration pc = GetContractedPinConfiguration();
        SetPlannedPinConfiguration(pc);

        for (int origDir = 0; origDir < 6; origDir++)
        {
            if (!nbrs[origDir]) continue;

            Direction dir = DirectionHelpers.Cardinal(origDir);

            if (chiralityAgreementPhase)
            {
                // Beep on the pin with offset 0
                pc.GetPinAt(dir, reverseChirality ? 1 : 0).PartitionSet.SendBeep();
            }
            else
            {
                // Send message with my edge direction on pin 0
                Direction d = ToNewDir(dir);
                DirectionMessage msg = new DirectionMessage(d);
                pc.GetPinAt(dir, reverseChirality ? 1 : 0).PartitionSet.SendMessage(msg);
            }
        }

        // Proceed with round 1
        round.SetValue(1);
    }

    private void Activate1()
    {
        // Receive neighbors' chirality or compass information
        PinConfiguration pc = GetCurrentPinConfiguration();
        bool haveNbrOutOfRegion = false;
        List<int> regionNbrs = new List<int>();
        for (int origDir = 0; origDir < 6; origDir++)
        {
            if (!nbrs[origDir]) continue;

            Direction dir = DirectionHelpers.Cardinal(origDir);

            if (chiralityAgreementPhase)
            {
                // If neighbor beeped on pin 1, then we share chirality, otherwise we don't
                if (pc.GetPinAt(dir, reverseChirality ? 0 : 1).PartitionSet.ReceivedBeep())
                {
                    // Share chirality, remember this neighbor as regional neighbor
                    regionNbrs.Add(origDir);
                }
                else
                {
                    // Do not share chirality, remember as non-regional neighbor
                    haveNbrOutOfRegion = true;
                }
            }
            else
            {
                // Receive message on pin 1 (must have one) and read compass direction
                Direction myDir = ToNewDir(dir);
                DirectionMessage msg = pc.GetPinAt(dir, reverseChirality ? 0 : 1).PartitionSet.GetReceivedMessage() as DirectionMessage;
                if (msg == null)
                {
                    Debug.LogError("Did not receive direction message from neighbor in direction " + dir);
                    continue;
                }
                // Offset to neighbor dir is number of counter-clockwise rotations we have to make to match their compass
                // This is the direction of our neighbor corresponding to myDir
                Direction nbrDir = msg.direction.Opposite();
                int offset = myDir.DistanceTo(nbrDir) / 2;

                if (offset == 0)
                {
                    // No offset, neighbor is in the region
                    regionNbrs.Add(origDir);
                    nbrOffsets[origDir].SetValue(-1);
                }
                else
                {
                    // Remember neighbor with offset
                    nbrOffsets[origDir].SetValue(offset);
                    haveNbrOutOfRegion = true;
                }
            }
        }

        // Setup regional circuit and beep if there are neighbors that don't share our information
        if (regionNbrs.Count > 0)
        {
            hasRegionalCircuit.SetValue(true);
            // Collect all pins of the regional circuit in partition set 0
            int[] pinIds = new int[2 * regionNbrs.Count];
            for (int i = 0; i < regionNbrs.Count; i++)
            {
                Direction d = DirectionHelpers.Cardinal(regionNbrs[i]);
                pinIds[2 * i] = pc.GetPinAt(d, 0).Id;
                pinIds[2 * i + 1] = pc.GetPinAt(d, 1).Id;
            }
            pc.MakePartitionSet(pinIds, 0);
            SetPlannedPinConfiguration(pc);

            if (haveNbrOutOfRegion)
            {
                pc.SendBeepOnPartitionSet(0);
            }
        }
        else
        {
            hasRegionalCircuit.SetValue(false);
        }

        // Proceed with round 2
        round.SetValue(2);
    }

    private void Activate2()
    {
        // Receive regional circuit's beep for neighbor existence
        PinConfiguration pc = GetCurrentPinConfiguration();
        bool neighborsExist = !hasRegionalCircuit || pc.ReceivedBeepOnPartitionSet(0);

        // If no neighbors exist: Move on to next phase or terminate
        if (!neighborsExist)
        {
            if (chiralityAgreementPhase)
            {
                Debug.Log("END OF CHIRALITY AGREEMENT PHASE");
                chiralityAgreementPhase.SetValue(false);
                isCandidate.SetValue(true);
                round.SetValue(0);
            }
            else
            {
                Debug.Log("FINISHED");
                finished.SetValue(true);
            }
            return;
        }

        // Phase is not over yet: candidates toss coins and beep on regional circuit if HEADS is tossed
        if (isCandidate)
        {
            if (TossCoin())
            {
                SetPlannedPinConfiguration(pc);
                pc.SendBeepOnPartitionSet(0);
            }
        }

        // Proceed with round 3
        round.SetValue(3);
    }

    private void Activate3()
    {
        if (hasRegionalCircuit)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();
            // Check if HEADS candidates have sent on the regional circuit
            beepedForHeads.SetValue(pc.ReceivedBeepOnPartitionSet(0));

            // Candidates with TAILS send beep on regional circuit
            if (isCandidate && !heads)
            {
                SetPlannedPinConfiguration(pc);
                pc.SendBeepOnPartitionSet(0);
            }
        }

        // Proceed with round 4
        round.SetValue(4);
    }

    private void Activate4()
    {
        if (hasRegionalCircuit)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();

            // Receive TAILS beep
            bool hasTailsBeep = pc.ReceivedBeepOnPartitionSet(0);

            // Compute coin toss result
            // The attribute is public so neighbor particles can read it in the next round
            if (beepedForHeads && hasTailsBeep)
            {
                coinTossResult.SetValue(CoinTossResult.FAILED);
            }
            else if (!hasTailsBeep)
            {
                coinTossResult.SetValue(CoinTossResult.HEADS);
            }
            else
            {
                coinTossResult.SetValue(CoinTossResult.TAILS);
            }

            // Candidates with TAILS withdraw candidacy if result is FAILED
            if (isCandidate && !heads && coinTossResult.GetValue_After() == CoinTossResult.FAILED)
            {
                SetMainColor(ColorData.Particle_Black);
                isCandidate.SetValue(false);
            }
        }
        else
        {
            // We have no regional circuit, so we are alone
            // The coin toss result is simply our own result
            coinTossResult.SetValue(heads ? CoinTossResult.HEADS : CoinTossResult.TAILS);
        }

        // Proceed with round 5
        round.SetValue(5);
    }

    private void Activate5()
    {
        // Can now read coin toss result of neighbors
        // If our coin toss result is TAILS and we have a neighbor with a different result,
        // initiate the merge procedure

        if (coinTossResult == CoinTossResult.TAILS)
        {
            if (hasRegionalCircuit)
            {
                PinConfiguration pc = GetCurrentPinConfiguration();

                if (chiralityAgreementPhase)
                {
                    // If any neighbor has a different coin toss result, send beep
                    for (int i = 0; i < 6; i++)
                    {
                        if (!nbrs[i]) continue;

                        ChiralityAndCompassParticle nbr = GetNeighborAt(DirectionHelpers.Cardinal(i)) as ChiralityAndCompassParticle;
                        if (nbr.coinTossResult != coinTossResult.GetValue_After())
                        {
                            SetPlannedPinConfiguration(pc);
                            pc.SendBeepOnPartitionSet(0);
                            break;
                        }
                    }
                }
                else
                {
                    // Compass alignment phase: Send first beep on regional circuit if we have a matching neighbor
                    CompassAlignmentUpdateMergeBeep(1);
                }
            }
            else
            {
                // No regional circuit means that all neighbors are in other regions
                // Delay the merge decision until the round everyone else decides it
            }
        }

        // Proceed with round 6
        round.SetValue(6);
    }

    private void Activate6()
    {
        // Chirality agreement phase: Know if we have to change our chirality now
        if (chiralityAgreementPhase)
        {
            // Check if we have to merge into other region
            if (coinTossResult == CoinTossResult.TAILS)
            {
                bool mergeIntoOtherRegion = false;
                // Check for beep on regional circuit or check all neighbors if there is no regional circuit
                if (hasRegionalCircuit)
                {
                    PinConfiguration pc = GetCurrentPinConfiguration();
                    mergeIntoOtherRegion = pc.ReceivedBeepOnPartitionSet(0);
                }
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (!nbrs[i]) continue;

                        ChiralityAndCompassParticle nbr = GetNeighborAt(DirectionHelpers.Cardinal(i)) as ChiralityAndCompassParticle;
                        if (nbr.coinTossResult != CoinTossResult.TAILS)
                        {
                            mergeIntoOtherRegion = true;
                            break;
                        }
                    }
                }

                // Change chirality if necessary
                if (mergeIntoOtherRegion)
                {
                    //Debug.Log("MERGE");
                    reverseChirality.SetValue(!reverseChirality);
                    realChirality.SetValue(!realChirality);
                    SetMainColor(ColorData.Particle_Black);
                    isCandidate.SetValue(false);
                }
            }

            // Go back to round 0 to start next iteration
            round.SetValue(0);
            return;
        }
        else
        {
            // Part of phase where we send beeps for matching neighbor regions
            if (hasRegionalCircuit)
            {
                // Read result of beep for offset 1, beep if we have neighbor with offset 2 and coin toss result != TAILS
                CompassAlignmentUpdateMergeBeep(2);
            }
        }

        round.SetValue(7);
    }

    private void Activate7To9()
    {
        // Compass alignment phase
        // In these rounds, we only listen for beeps and send a beep if we have a matching neighbor
        // This is done to agree on a region we can merge into
        // Nothing needs to be done if we have no regional circuit
        if (hasRegionalCircuit && coinTossResult == CoinTossResult.TAILS)
        {
            CompassAlignmentUpdateMergeBeep(round - 4);
        }
        round.SetValue(round + 1);
    }

    private void Activate10()
    {
        // Compass alignment phase: Last round
        // In this round, we decide if and how to merge and start the next iteration
        // Only merge if our coin toss result is TAILS
        if (coinTossResult == CoinTossResult.TAILS)
        {
            if (hasRegionalCircuit)
            {
                // Read the last merge beep
                CompassAlignmentUpdateMergeBeep(6);
            }
            else
            {
                // Have no regional circuit: Decide if and how to merge now
                // Find the neighbor with the smallest offset
                int minOffset = 6;  // Real offsets are always < 6
                for (int origDir = 0; origDir < 6; origDir++)
                {
                    int offset = nbrOffsets[origDir];
                    if (!nbrs[origDir] || offset == -1 || offset >= minOffset) continue;
                    ChiralityAndCompassParticle nbr = GetNeighborAt(DirectionHelpers.Cardinal(origDir)) as ChiralityAndCompassParticle;
                    if (nbr.coinTossResult != CoinTossResult.TAILS)
                    {
                        minOffset = offset;
                    }
                }
                if (minOffset < 6)
                {
                    mergeOffset.SetValue(minOffset);
                }
            }

            // Perform merge if merge offset is set
            if (mergeOffset.GetValue_After() != -1)
            {
                int offset = mergeOffset.GetValue_After();
                // Rotate compass direction by the offset
                if (reverseChirality)
                {
                    compassOffset.SetValue(compassOffset.GetValue_After().Rotate60(offset));
                }
                else
                {
                    compassOffset.SetValue(compassOffset.GetValue_After().Rotate60(-offset));
                }

                // Also update value of real compass direction
                if (realChirality)
                {
                    realCompassDir.SetValue(realCompassDir.GetValue_After().Rotate60(-offset));
                }
                else
                {
                    realCompassDir.SetValue(realCompassDir.GetValue_After().Rotate60(offset));
                }

                // Withdraw candidacy if we merge
                isCandidate.SetValue(false);
            }
        }

        // Reset merge offset
        mergeOffset.SetValue(-1);

        // Start next iteration
        round.SetValue(0);
    }

    /// <summary>
    /// Translates the given original local direction
    /// into the corresponding direction in the new
    /// system according to the current compass offset
    /// and chirality.
    /// </summary>
    /// <param name="dir">The original local direction
    /// to be translated.</param>
    /// <returns>The new local direction that corresponds
    /// to the given original direction
    /// <paramref name="dir"/> with the current compass
    /// offset and chirality inversion applied.</returns>
    private Direction ToNewDir(Direction dir)
    {
        return DirectionHelpers.Cardinal(compassOffset.GetValue().DistanceTo(dir, reverseChirality) / 2);
    }

    private bool TossCoin()
    {
        bool result = Random.Range(0f, 1f) <= 0.5f;
        heads.SetValue(result);
        return result;
    }

    private void CompassAlignmentUpdateMergeBeep(int offset)
    {
        PinConfiguration pc = GetCurrentPinConfiguration();
        // Read value from previous round if offset is large enough and no merge offset was selected yet
        if (offset > 1 && mergeOffset.GetValue_After() == -1 && pc.ReceivedBeepOnPartitionSet(0))
        {
            mergeOffset.SetValue(offset - 1);
        }

        // Send beep if offset is small enough and we have a matching neighbor
        if (offset < 6)
        {
            for (int origDir = 0; origDir < 6; origDir++)
            {
                if (!nbrs[origDir] || nbrOffsets[origDir] != offset) continue;

                ChiralityAndCompassParticle nbr = GetNeighborAt(DirectionHelpers.Cardinal(origDir)) as ChiralityAndCompassParticle;
                if (nbr.coinTossResult != CoinTossResult.TAILS)
                {
                    // This neighbor has a matching coin toss result and offset
                    SetPlannedPinConfiguration(pc);
                    pc.SendBeepOnPartitionSet(0);
                }
            }
        }
    }

    // Only for visualization, uses global data
    private void SetColor()
    {
        PinConfiguration pc = GetPlannedPinConfiguration();
        if (pc is null)
        {
            pc = GetCurrentPinConfiguration();
            SetPlannedPinConfiguration(pc);
        }
        bool hasRegion = hasRegionalCircuit.GetValue_After();
        if (chiralityAgreementPhase.GetValue_After())
        {
            if (realChirality.GetValue_After())
            {
                if (isCandidate.GetValue_After())
                {
                    SetMainColor(chir1CandColor);
                    if (hasRegion)
                        pc.SetPartitionSetColor(0, chir1CircuitColor);
                }
                else
                {
                    SetMainColor(chir1NoCandColor);
                }
            }
            else
            {
                if (isCandidate.GetValue_After())
                {
                    SetMainColor(chir0CandColor);
                    if (hasRegion)
                        pc.SetPartitionSetColor(0, chir0CircuitColor);
                }
                else
                {
                    SetMainColor(chir0NoCandColor);
                }
            }
        }
        else
        {
            if (isCandidate.GetValue_After())
            {
                Direction i = realCompassDir.GetValue_After();
                SetMainColor(compCandColors[i.ToInt()]);
                if (hasRegion)
                    pc.SetPartitionSetColor(0, compCandColors[i.Opposite().ToInt()]);
            }
            else
            {
                SetMainColor(compNoCandColor[realCompassDir.GetValue_After().ToInt()]);
            }
        }
    }
}
