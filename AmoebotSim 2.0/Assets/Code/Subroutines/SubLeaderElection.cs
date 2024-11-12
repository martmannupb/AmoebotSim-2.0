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
    /// <see cref="Init(int, bool, int, bool)"/> to initialize the subroutine.<br/>
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
        private ParticleAttribute<bool> controlColor;       // Whether we control the particle's color

        private ParticleAttribute<bool> isLeaderCandidate;  // Whether we are still a leader candidate
        private ParticleAttribute<bool> isPhase2Candidate;  // Whether we are a candidate in the second phase
        private ParticleAttribute<int> round;               // The current round number
        private ParticleAttribute<bool> firstPhase;         // Whether we are still in the first phase
        private ParticleAttribute<int> repetitions;         // Number of repetitions of the second phase (maximum is kappa)
        private ParticleAttribute<bool> heads;              // Last coin toss result
        private ParticleAttribute<bool> beepFromHeads;      // Flag storing whether we have received a beep sent in the HEADS round
        private ParticleAttribute<bool> beepFromTails;      // Flag storing whether we have received a beep sent in the TAILS round
        private ParticleAttribute<bool> finished;           // Whether the leader election has finished

        /// <summary>
        /// Color for particles that are still active leader candidates.
        /// </summary>
        public static readonly Color candidateColor = ColorData.Particle_Green;
        /// <summary>
        /// Color for particles that are no leader candidate but still
        /// an active candidate in phase 2.
        /// </summary>
        public static readonly Color activeColor = ColorData.Particle_Blue;
        /// <summary>
        /// Color for particles that are no candidates but still not finished.
        /// </summary>
        public static readonly Color passiveColor = ColorData.Particle_BlueDark;
        /// <summary>
        /// Color for particles that are finished and no leader candidate.
        /// </summary>
        public static readonly Color retiredColor = ColorData.Particle_Black;

        public SubLeaderElection(Particle p) : base(p)
        {
            kappa = algo.CreateAttributeInt(FindValidAttributeName("[LE] Kappa"), 3);
            partitionSetId = algo.CreateAttributeInt(FindValidAttributeName("[LE] Partition set"), -1);
            controlColor = algo.CreateAttributeBool(FindValidAttributeName("[LE] Control color"), false);

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

        /// <summary>
        /// Initializes the subroutine for a new leader election.
        /// </summary>
        /// <param name="partitionSet">The index of the partition set used
        /// to run the leader election. This should always identify the same
        /// circuit throughout the procedure.</param>
        /// <param name="controlColor">Whether the subroutine should set
        /// the particle color according to the leader election.</param>
        /// <param name="kappa">The number of repetitions of the second
        /// phase. The more repetitions are made, the lower the chances
        /// of electing multiple leaders become.</param>
        /// <param name="startAsCandidate">Whether this participant should
        /// start the leader election as a candidate. Useful for restricting
        /// the set of candidates beforehand. Note that it is possible that
        /// no leader is elected in case there are no candidates to start with.</param>
        public void Init(int partitionSet, bool controlColor = false, int kappa = 3, bool startAsCandidate = true)
        {
            partitionSetId.SetValue(partitionSet);
            this.controlColor.SetValue(controlColor);
            this.kappa.SetValue(kappa);

            isLeaderCandidate.SetValue(startAsCandidate);
            isPhase2Candidate.SetValue(true);
            round.SetValue(-1);
            firstPhase.SetValue(true);
            repetitions.SetValue(0);
            heads.SetValue(false);
            beepFromHeads.SetValue(false);
            beepFromTails.SetValue(false);
            finished.SetValue(false);
        }

        /// <summary>
        /// The first half of the beep activation. Must be called
        /// while the beeps of the previous <see cref="ActivateSend"/> call
        /// can still be read on the current pin configuration (i.e., the pin
        /// configuration must not be changed before this method is called).
        /// It may however be changed after this call, as long as the correct
        /// pin configuration is planned again before the next <see cref="ActivateSend"/>
        /// call.
        /// </summary>
        public void ActivateReceive()
        {
            if (finished.GetCurrentValue() || round.GetCurrentValue() == -1)
                return;

            bool receivedBeep = algo.ReceivedBeepOnPartitionSet(partitionSetId.GetCurrentValue());

            if (round.GetCurrentValue() % 2 == 0)
            {
                // Even round: Receive TAILS beep and decide what should be done next
                beepFromTails.SetValue(receivedBeep);

                // In round 0, we switch phases or terminate
                // If one of the two coin toss results did not occur: Terminate
                if (round.GetCurrentValue() == 0 && (!beepFromHeads.GetCurrentValue() || !beepFromTails.GetCurrentValue()))
                {
                    // Either start an iteration of phase 2 or terminate completely
                    if (firstPhase.GetCurrentValue() && kappa.GetCurrentValue() > 0 || repetitions.GetCurrentValue() < kappa.GetCurrentValue())
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
                        if (controlColor.GetCurrentValue())
                            UpdateColor();
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
                    
                    // Toss a coin if we are a candidate in the correct round
                    if (round.GetCurrentValue() == 0 && isLeaderCandidate.GetCurrentValue()
                        || round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
                        TossCoin();
                }
            }
            else
            {
                // Odd round: Receive HEADS beep
                beepFromHeads.SetValue(receivedBeep);
            }

            if (controlColor.GetCurrentValue())
                UpdateColor();
        }

        /// <summary>
        /// The second half of the beep activation. Must be
        /// called when the correct pin configuration has
        /// been established. The beeps sent by this method
        /// must be received by the next call of <see cref="ActivateReceive"/>.
        /// </summary>
        public void ActivateSend()
        {
            if (finished.GetCurrentValue())
                return;

            if (round.GetCurrentValue() == -1)
            {
                // First round: Candidates toss coin and send a beep
                if (isLeaderCandidate.GetCurrentValue() && TossCoin())
                {
                    algo.SendBeepOnPartitionSet(partitionSetId.GetCurrentValue());
                }

                // This is a stand-in for round 0, so we continue with round 1
                round.SetValue(1);
                if (controlColor.GetCurrentValue())
                    UpdateColor();
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
                        algo.SendBeepOnPartitionSet(partitionSetId.GetCurrentValue());
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
                        algo.SendBeepOnPartitionSet(partitionSetId.GetCurrentValue());
                    }
                }

                // Increment counter
                if (firstPhase.GetCurrentValue() || round.GetCurrentValue() == 3)
                    round.SetValue(0);
                else
                    round.SetValue(2);
            }

            if (controlColor.GetCurrentValue())
                UpdateColor();
        }

        /// <summary>
        /// Shorthand for calling <see cref="ActivateReceive"/>
        /// and then <see cref="ActivateSend"/> for when the pin
        /// configuration should not be changed inbetween.
        /// </summary>
        public override void ActivateBeep()
        {
            ActivateReceive();
            ActivateSend();
        }

        /// <summary>
        /// Tosses a fair coin and returns the result.
        /// Also stores the result in the <see cref="heads"/>
        /// attribute.
        /// </summary>
        /// <returns><c>true</c> if the result was HEADS (probability
        /// of 1/2), <c>false</c> otherwise.</returns>
        private bool TossCoin()
        {
            bool result = Random.Range(0.0f, 1.0f) <= 0.5f;
            heads.SetValue(result);
            return result;
        }

        /// <summary>
        /// Updates the particle's color based on the current
        /// leader election state.
        /// </summary>
        private void UpdateColor()
        {
            if (isLeaderCandidate.GetCurrentValue())
                algo.SetMainColor(candidateColor);
            else
            {
                if (finished.GetCurrentValue())
                    algo.SetMainColor(retiredColor);
                else if (!firstPhase.GetCurrentValue() && isPhase2Candidate.GetCurrentValue())
                    algo.SetMainColor(activeColor);
                else
                    algo.SetMainColor(passiveColor);
            }
        }

        /// <summary>
        /// Checks whether the leader election procedure has finished.
        /// Once this returns <c>true</c>, no activation method calls
        /// will change the state of this subroutine.
        /// </summary>
        /// <returns><c>true</c> if and only if the leader election
        /// procedure has terminated.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether the LE participant represented by this
        /// subroutine is still a leader candidate.
        /// </summary>
        /// <returns><c>true</c> if and only if this participant
        /// still has a chance of becoming the leader.</returns>
        public bool IsCandidate()
        {
            return !IsFinished() && isLeaderCandidate.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether the LE participant represented by this
        /// subroutine is currently a phase 2 candidate. Phase 2
        /// candidate are independent of leader candidates and are
        /// reinstated in each iteration of the second phase.
        /// </summary>
        /// <returns><c>true</c> if and only if this participant
        /// is currently a phase 2 candidate and is not still
        /// running phase 1.</returns>
        public bool IsPhase2Candidate()
        {
            return !IsFinished() && !firstPhase.GetCurrentValue() && isPhase2Candidate.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this LE participant has been elected
        /// as a leader. Note that for small sets of particles
        /// and a low value of kappa, it is possible that
        /// multiple participants are elected as leaders.
        /// </summary>
        /// <returns><c>true</c> if and only if the leader election
        /// procedure has terminated and this participant
        /// is still marked as a leader candidate.</returns>
        public bool IsLeader()
        {
            return IsFinished() && isLeaderCandidate.GetCurrentValue();
        }

        /// <summary>
        /// Gets the current round counter of the participant.
        /// See the class documentation for an overview of what
        /// happens during each round.
        /// </summary>
        /// <returns>The current value of the round counter.</returns>
        public int GetRound()
        {
            return round.GetCurrentValue();
        }
    }

} // namespace AS2.Subroutines.LeaderElection
