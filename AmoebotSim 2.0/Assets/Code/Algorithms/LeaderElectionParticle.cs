using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of the leader election algorithm presented in
/// https://arxiv.org/abs/2105.05071v1.
/// <para>
/// In the first phase, the algorithm alternates between rounds
/// 0 and 1 in each iteration. In round 0, particles toss coins
/// and beep if they have tossed HEADS, and in round 1, they
/// listen for the beep from round 0 and beep if they have tossed
/// TAILS. In the next round 0, they listen for these beeps and
/// decide whether to repeat the process or initiate phase 2.
/// </para>
/// <para>
/// The second phase works analogously but with 4 rounds 0-3
/// instead of 2 rounds per iteration.
/// </para>
/// </summary>
public class LeaderElectionParticle : ParticleAlgorithm
{
    private static readonly int kappa = 3;              // Number of repetitions of the second phase (one iteration is always executed)

    private ParticleAttribute<bool> firstActivation;    // Flag used to set initial pin configuration to global circuit (only used once)
    private ParticleAttribute<bool> isCandidate;        // Flag that is true until we decide not to be a candidate anymore
    private ParticleAttribute<bool> phase2Candidate;    // Candidate flag for second phase
    private ParticleAttribute<bool> firstPhase;         // Flag that is true until the second phase starts
    private ParticleAttribute<int> round;               // Round counter used to synchronize the particles in both phases
    private ParticleAttribute<int> phase2Count;         // Counter for kappa to check when the algorithm should finally terminate
    private ParticleAttribute<bool> heads;              // Flag storing the result of the last coin toss
    private ParticleAttribute<bool> beepFromHeads;      // Flag storing whether we have received a beep sent in the HEADS round
    private ParticleAttribute<bool> terminated;         // Final termination flag

    public LeaderElectionParticle(Particle p) : base(p)
    {
        SetMainColor(ColorData.Particle_Green);
        firstActivation = CreateAttributeBool("First activation", true);
        isCandidate = CreateAttributeBool("Is candidate", true);
        phase2Candidate = CreateAttributeBool("Phase 2 Candidate", false);
        firstPhase = CreateAttributeBool("First phase", true);
        round = CreateAttributeInt("Round", 0);
        phase2Count = CreateAttributeInt("Phase 2 count", 0);
        heads = CreateAttributeBool("Heads", false);
        beepFromHeads = CreateAttributeBool("Beep from round 0", false);
        terminated = CreateAttributeBool("Terminated", false);
    }

    // Algorithm only requires one pin
    public override int PinsPerEdge => 1;

    public static new string Name => "Leader Election";

    public static new Initialization.Chirality Chirality => Initialization.Chirality.Random;
    public static new Initialization.Compass Compass => Initialization.Compass.Random;

    public static new string GenerationMethod => LeaderElectionInitializer.Name;

    public override bool IsFinished()
    {
        return terminated;
    }

    public override void ActivateMove()
    {

    }

    public override void ActivateBeep()
    {
        if (terminated)
        {
            return;
        }

        // Set global circuit and start with phase 1 in the first round
        if (firstActivation)
        {
            PinConfiguration pc = GetCurrentPinConfiguration();
            pc.SetToGlobal(0);  // Make partition set 0 hold all pins
            SetPlannedPinConfiguration(pc);
            pc.SetPartitionSetColor(0, new Color(0.75f, 0.75f, 0.75f));
            firstActivation.SetValue(false);

            // Toss a coin and start first phase
            // Beep if we have tossed heads
            if (TossCoin())
            {
                pc.SendBeepOnPartitionSet(0);
            }
            round.SetValue(1);
            return;
        }

        bool receivedBeep = GetCurrentPinConfiguration().ReceivedBeepOnPartitionSet(0);

        // Even round: Decision and start of next iteration
        if (round % 2 == 0)
        {
            // Increment round counter (can still read previous value)
            round.SetValue(round + 1);

            // Reset flag (can still read previous value)
            beepFromHeads.SetValue(false);

            bool coinToss = true;   // Indicates whether or not the particle should update its candidate state and perform a coin toss

            // Terminate if no particle beeped in one of the rounds
            if (!beepFromHeads || !receivedBeep)
            {
                // Round 0 is always the decision point for new iterations
                // It is also the point where we transition to the second phase
                if (round == 0)
                {
                    // Start next iteration of second phase
                    if (firstPhase || phase2Count < kappa)
                    {
                        firstPhase.SetValue(false);
                        phase2Count.SetValue(phase2Count + 1);
                        // Everyone becomes a candidate in phase 2 again
                        phase2Candidate.SetValue(true);
                        if (!isCandidate)
                        {
                            SetMainColor(ColorData.Particle_Blue);
                        }
                        // Proper candidates toss coin and send beep on HEADS
                        if (isCandidate)
                        {
                            if (TossCoin())
                            {
                                SendBeep();
                            }
                        }
                    }
                    // Terminate algorithm finally
                    else
                    {
                        if (isCandidate)
                        {
                            SetMainColor(ColorData.Particle_Purple);
                        }
                        else
                        {
                            SetMainColor(ColorData.Particle_Black);
                        }
                        terminated.SetValue(true);
                        return;
                    }
                    coinToss = false;
                }
            }

            if (coinToss)
            {
                // No or irrelevant termination: Revoke candidacy if necessary
                if (!heads && beepFromHeads) {
                    if (firstPhase || round == 2)
                    {
                        if (isCandidate)
                        {
                            isCandidate.SetValue(false);
                        }
                    }
                    else if (phase2Candidate)
                    {
                        phase2Candidate.SetValue(false);
                    }
                }

                // Then toss new coin and send beep if appropriate
                if (round == 0)
                {
                    if (isCandidate.GetValue_After() && TossCoin())
                    {
                        SendBeep();
                    }
                }
                else
                {
                    if (phase2Candidate.GetValue_After() && TossCoin())
                    {
                        SendBeep();
                    }
                }
            }
        }
        // Odd round: Receiving HEADS beep and sending TAILS beep
        else
        {
            beepFromHeads.SetValue(receivedBeep);

            // Send a beep if we have tossed TAILS and we are supposed to send a beep
            // in this phase and round
            if (!heads &&
                (firstPhase && isCandidate
                || !firstPhase && isCandidate && round == 1
                || !firstPhase && phase2Candidate && round == 3))
            {
                SendBeep();
            }

            // Increment round counter
            if (firstPhase || round == 3)
            {
                round.SetValue(0);
            }
            else
            {
                round.SetValue(2);
            }
        }

        // Set color
        if (isCandidate.GetValue_After())
        {
            SetMainColor(ColorData.Particle_Green);
        }
        else if (phase2Candidate.GetValue_After())
        {
            SetMainColor(ColorData.Particle_Blue);
        }
        else
        {
            SetMainColor(ColorData.Particle_Black);
        }
    }

    private bool TossCoin()
    {
        bool result = Random.Range(0.0f, 1.0f) <= 0.5f;
        heads.SetValue(result);
        return result;
    }

    private void SendBeep()
    {
        PinConfiguration pc = GetCurrentPinConfiguration();
        SetPlannedPinConfiguration(pc);
        pc.SendBeepOnPartitionSet(0);
    }
}

public class LeaderElectionInitializer : InitializationMethod
{
    public LeaderElectionInitializer(ParticleSystem system) : base(system) { }

    public static new string Name => "Leader Election";

    public void Generate(int numParticles = 50, float holeProb = 0.3f)
    {
        GenerateRandomWithHoles(numParticles, holeProb, Initialization.Chirality.Random, Initialization.Compass.Random);
    }
}
