using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.LeaderElection
{

    /// <summary>
    /// Implements the leader election algorithm from
    /// https://arxiv.org/abs/2105.05071v1.
    /// <para>
    /// Elects a leader among the particles connected by a
    /// circuit. The partition set identifying this circuit
    /// must be given as a parameter (i.e., the circuit must
    /// be created before the leader election can start).
    /// The same circuit must be used for the entire procedure.
    /// </para>
    /// <para>
    /// The sequence of calls should be as follows:<br/>
    /// <see cref="Init(int, int)"/> to initialize the subroutine.<br/>
    /// Then, in the beep activation: <see cref="ActivateReceive"/>
    /// with the current pin configuration so that beeps can be received,
    /// followed by <see cref="ActivateSend"/> with a pin configuration that
    /// allows sending beeps on the LE circuit.<br/>
    /// This separation allows you to change the pin configuration or
    /// pause the leader election procedure.<br/>
    /// The algorithm is already finished after the last
    /// <see cref="ActivateReceive"/> call.
    /// </para>
    /// <para>
    /// Phase 1 (2 Rounds):<br/>
    /// Round 0: Leader candidates toss a coin and send a beep if the result is HEADS.<br/>
    /// Round 1: Store received beep and send beep if the result was TAILS.<br/>
    /// Next round 0: If both HEADS and TAILS occurred, all candidates with TAILS
    /// withdraw their candidacy. Otherwise, we move on to phase 2.
    /// </para>
    /// <para>
    /// Phase 2 (4 Rounds):<br/>
    /// Rounds 0 and 1: Same as in phase 1 but we don't do anything if only one coin
    /// toss result occurred.<br/>
    /// Rounds 2 and 3: Same as rounds 0 and 1 but with separate candidacies and
    /// coin tosses. This step is just used to increase the duration for which the
    /// competition is executed. When only one coin toss result occurs, we either
    /// repeat phase 2 by restoring all candidacies or terminate. The number of
    /// repetitions is given as the parameter kappa.
    /// </para>
    /// <para>
    /// Note that this algorithm only determines a unique leader with high probability.
    /// It is possible that more than one particle remains a candidate when the
    /// algorithm terminates, especially for a small number of particles and small
    /// value for kappa.
    /// </para>
    /// </summary>
    public class SubLeaderElection : Subroutine
    {
        private ParticleAttribute<int> kappa;               // Number of repetitions
        private ParticleAttribute<int> partitionSetId;      // The ID of the partition set on which to perform the LE

        private ParticleAttribute<bool> isLeaderCandidate;  // Whether we are still a leader candidate
        private ParticleAttribute<bool> isPhase2Candidate;  // Whether we are a candidate in the second phase
        private ParticleAttribute<int> round;               // The current round number
        private ParticleAttribute<bool> firstPhase;         // Whether we are still in the first phase
        private ParticleAttribute<int> repetitions;         // Number of repetitions of the second phase (maximum is kappa)
        private ParticleAttribute<bool> heads;              // Last coin toss result
        private ParticleAttribute<bool> beepFromHeads;      // Flag storing whether we have received a beep sent in the HEADS round
        private ParticleAttribute<bool> beepFromTails;      // Flag storing whether we have received a beep sent in the TAILS round
        private ParticleAttribute<bool> finished;           // Whether the leader election has finished

        public SubLeaderElection(Particle p) : base(p)
        {
            kappa = algo.CreateAttributeInt(FindValidAttributeName("[LE] Kappa"), 3);
            partitionSetId = algo.CreateAttributeInt(FindValidAttributeName("[LE] Partition set"), -1);

            isLeaderCandidate = algo.CreateAttributeBool(FindValidAttributeName("[LE] Candidate"), true);
            isPhase2Candidate = algo.CreateAttributeBool(FindValidAttributeName("[LE] Candidate 2"), true);
            round = algo.CreateAttributeInt(FindValidAttributeName("[LE] Round"), -1);
            firstPhase = algo.CreateAttributeBool(FindValidAttributeName("[LE] First phase"), true);
            repetitions = algo.CreateAttributeInt(FindValidAttributeName("[LE] Repetitions"), 0);
            heads = algo.CreateAttributeBool(FindValidAttributeName("[LE] Heads"), false);
            beepFromHeads = algo.CreateAttributeBool(FindValidAttributeName("[LE] Beep Heads"), false);
            beepFromTails = algo.CreateAttributeBool(FindValidAttributeName("[LE] Beep Tails"), false);
            finished = algo.CreateAttributeBool(FindValidAttributeName("[LE] Finished"), false);
        }

        public void Init(int partitionSet, int kappa = 3)
        {
            this.kappa.SetValue(kappa);
            partitionSetId.SetValue(partitionSet);

            isLeaderCandidate.SetValue(true);
            isPhase2Candidate.SetValue(true);
            round.SetValue(-1);
            firstPhase.SetValue(true);
            repetitions.SetValue(0);
            heads.SetValue(false);
            beepFromHeads.SetValue(false);
            beepFromTails.SetValue(false);
            finished.SetValue(false);
        }

        public void ActivateReceive()
        {
            if (finished.GetCurrentValue() || round.GetCurrentValue() == -1)
                return;

            PinConfiguration pc = algo.GetCurrentPinConfiguration();
            bool receivedBeep = pc.ReceivedBeepOnPartitionSet(partitionSetId.GetCurrentValue());

            if (round.GetCurrentValue() % 2 == 0)
            {
                // Even round: Receive TAILS beep and decide what should be done next
                beepFromTails.SetValue(receivedBeep);

                // In round 0, we switch phases or terminate
                // If one of the two coin toss results did not occur: Terminate
                if (round.GetCurrentValue() == 0 && (!beepFromHeads.GetCurrentValue() || !beepFromTails.GetCurrentValue()))
                {
                    // Either start an iteration of phase 2 or terminate completely
                    if (firstPhase.GetCurrentValue() || repetitions.GetCurrentValue() < kappa.GetCurrentValue())
                    {
                        // We need to start an iteration of phase 2
                        firstPhase.SetValue(false);
                        repetitions.SetValue(repetitions.GetCurrentValue() + 1);
                        isPhase2Candidate.SetValue(true);

                        // Only proper candidates toss a coin here
                        if (isLeaderCandidate.GetCurrentValue())
                            TossCoin();
                    }
                    else
                    {
                        // This was the last iteration
                        finished.SetValue(true);
                        return;
                    }
                }
                // Not round 0 or we received both kinds of beeps
                else
                {
                    // Check if we have tossed TAILS and received a HEADS beep
                    if (!heads.GetCurrentValue() && beepFromHeads.GetCurrentValue())
                    {
                        // We have to withdraw our candidacy in this case
                        // Proper candidates withdraw in first phase or after round 0,1
                        if (firstPhase.GetCurrentValue() || round.GetCurrentValue() == 2)
                        {
                            if (isLeaderCandidate.GetCurrentValue())
                                isLeaderCandidate.SetValue(false);
                        }
                        else if (isPhase2Candidate.GetCurrentValue())
                            isPhase2Candidate.SetValue(false);
                    }
                    else
                    {
                        // Don't have to withdraw candidacy
                        // Toss a coin if we are a candidate in the correct round
                        if (round.GetCurrentValue() == 0 && isLeaderCandidate.GetCurrentValue()
                            || round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
                            TossCoin();
                    }
                }
            }
            else
            {
                // Odd round: Receive HEADS beep
                beepFromHeads.SetValue(receivedBeep);
            }
        }

        public void ActivateSend()
        {
            if (finished.GetCurrentValue())
                return;

            PinConfiguration pc = algo.GetPlannedPinConfiguration();
            if (pc == null)
                pc = algo.GetCurrentPCAsPlanned();

            if (round.GetCurrentValue() == -1)
            {
                // First round: Toss coin and send a beep
                if (TossCoin())
                {
                    pc.SendBeepOnPartitionSet(partitionSetId.GetCurrentValue());
                }

                // This is a stand-in for round 0, so we continue with round 1
                round.SetValue(1);
                return;
            }

            if (round.GetCurrentValue() % 2 == 0)
            {
                // Even round
                // If we are a candidate and have tossed HEADS, we send a beep
                if (heads.GetCurrentValue())
                {
                    if (round.GetCurrentValue() == 0 && isLeaderCandidate.GetCurrentValue()
                        || round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
                    {
                        pc.SendBeepOnPartitionSet(partitionSetId.GetCurrentValue());
                    }
                }

                // Also reset both beep flags
                beepFromHeads.SetValue(false);
                beepFromTails.SetValue(false);

                // Increment counter
                round.SetValue(round.GetCurrentValue() + 1);
            }
            else
            {
                // Odd round
                // If we are a candidate and have tossed TAILS, we send a beep
                if (!heads.GetCurrentValue())
                {
                    if (round.GetCurrentValue() == 1 && isLeaderCandidate.GetCurrentValue()
                        || round.GetCurrentValue() == 3 && isPhase2Candidate.GetCurrentValue())
                    {
                        pc.SendBeepOnPartitionSet(partitionSetId.GetCurrentValue());
                    }
                }

                // Increment counter
                if (firstPhase.GetCurrentValue() || round.GetCurrentValue() == 3)
                    round.SetValue(0);
                else
                    round.SetValue(2);
            }
        }

        private bool TossCoin()
        {
            bool result = Random.Range(0.0f, 1.0f) <= 0.5f;
            heads.SetValue(result);
            return result;
        }

        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        public bool IsCandidate()
        {
            return isLeaderCandidate.GetCurrentValue();
        }

        public bool IsLeader()
        {
            return IsFinished() && IsCandidate();
        }
    }

} // namespace AS2.Subroutines.LeaderElection
