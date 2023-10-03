using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.LeaderElectionSync
{

    /// <summary>
    /// Implements a variation of the leader election algorithm from
    /// https://arxiv.org/abs/2105.05071v1.
    /// <para>
    /// Elects a leader among the particles connected by a
    /// circuit, using a second circuit for synchronization.
    /// The partition sets identifying both circuits
    /// must be given as parameters and the circuits must be
    /// established before the corresponding rounds.
    /// The same two circuits must be used for the entire procedure.
    /// It is possible to run multiple synchronized instances of this
    /// procedure by establishing multiple different election circuits
    /// but using the same synchronization circuit. The more particles
    /// participate in the synchronization and the second phase of the
    /// algorithm, the lower the chances of electing multiple leaders
    /// will be (but at the cost of a slightly longer runtime).
    /// </para>
    /// <para>
    /// The sequence of calls should be as follows:<br/>
    /// <see cref="Init(int, int, bool, int, bool)"/> to initialize the subroutine.<br/>
    /// In the beep activation: Call <see cref="ActivateReceive"/>, then call
    /// <see cref="NeedSyncCircuit"/> to check whether the synchronization circuit
    /// has to be established, then call <see cref="ActivateSend"/> with the
    /// correct circuit.<br/>
    /// You do not have to call <see cref="NeedSyncCircuit"/> and <see cref="ActivateSend"/>
    /// in the same activation as <see cref="ActivateReceive"/>, allowing you to pause
    /// the leader election as long as necessary to do anything else inbetween. The
    /// procedure will work as long as the correct circuits are established before
    /// the <see cref="ActivateSend"/> calls and the corresponding beeps are received
    /// in the very next round by calling <see cref="ActivateReceive"/>.<br/>
    /// Generally, the synchronization circuit must be established before calling
    /// <see cref="ActivateSend"/> in the last one or two rounds of each phase, i.e.,
    /// in round 1 of phase 1 and rounds 1 and 2 of phase 2.<br/>
    /// Checking whether the algorithm is finished is best done after the
    /// <see cref="ActivateReceive"/> call.
    /// </para>
    /// <para>
    /// Phase 1 (2 Rounds):<br/>
    /// Round 0: Listen for continuation beep on synchronization circuit,
    /// move on to next phase if no beep is received.<br/>
    /// Leader candidates toss a coin and send a beep on the
    /// election circuit if the result is HEADS.<br/>
    /// Round 1: Receive the HEADS beep and withdraw candidacy if coin toss
    /// result was TAILS.<br/>
    /// Send a beep on the synchronization circuit if this was the case.
    /// </para>
    /// <para>
    /// Phase 2 (3 Rounds):<br/>
    /// Round 0: Listen for continuation beep on synchronization circuit,
    /// start next iteration or terminate if no beep is received.<br/>
    /// Leader candidates toss a coin and send a beep on the election circuit
    /// if the result is HEADS.<br/>
    /// Round 1: Receive the HEADS beep and withdraw candidacy if coin toss
    /// result was TAILS.<br/>
    /// Phase 2 candidates toss coin and send beep on the synchronization
    /// circuit if the result is HEADS.<br/>
    /// Round 2: Phase 2 candidates receive the HEADS beep and withdraw
    /// candidacy if the coin toss result was TAILS.<br/>
    /// Send a beep on the synchronization circuit if this was the case.<br/>
    /// This phase is just used to increase the duration for which the
    /// competition is executed. The number of repetitions is given as the
    /// parameter kappa.
    /// </para>
    /// <para>
    /// Note that this algorithm only determines a unique leader with high probability.
    /// It is possible that more than one particle remains a candidate when the
    /// algorithm terminates, especially for a small number of particles and small
    /// value for kappa. However, if the synchronization circuit is large, this version
    /// of the leader election algorithm has better chances of success than the basic
    /// version, for the price of taking a little more time.
    /// </para>
    /// </summary>
    public class SubLeaderElectionSync : Subroutine
    {
        private ParticleAttribute<int> kappa;               // Number of repetitions
        private ParticleAttribute<int> partitionSetElect;   // The ID of the partition set on which to perform the LE
        private ParticleAttribute<int> partitionSetSync;    // The ID of the partition set on which to perform the synchronization
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

        public SubLeaderElectionSync(Particle p) : base(p)
        {
            kappa = algo.CreateAttributeInt(FindValidAttributeName("[LE] Kappa"), 3);
            partitionSetElect = algo.CreateAttributeInt(FindValidAttributeName("[LE] Partition set election"), -1);
            partitionSetSync = algo.CreateAttributeInt(FindValidAttributeName("[LE] Partition set sync"), -1);
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
        /// <param name="partitionSetElection">The index of the partition set used
        /// to run the leader election. This should always identify the same
        /// circuit throughout the procedure. There may be multiple different election
        /// circuits used in parallel by different sets of participants.</param>
        /// <param name="partitionSetSynchronization">The index of the partition set
        /// used to run the synchronization and secondary competition. This should
        /// always identify the same circuit throughout the procedure. Multiple sets
        /// of participants can share the same synchronization circuit to benefit from
        /// a higher success probability. You can increase the number of participants
        /// by letting non-candidate particles participate using the
        /// <paramref name="startAsCandidate"/> parameter.</param>
        /// <param name="controlColor">Whether the subroutine should set
        /// the particle color according to the leader election.</param>
        /// <param name="kappa">The number of repetitions of the second
        /// phase. The more repetitions are made, the lower the chances
        /// of electing multiple leaders become.</param>
        /// <param name="startAsCandidate">Whether this participant should
        /// start the leader election as a candidate. Useful for restricting
        /// the set of candidates beforehand. Note that it is possible that
        /// no leader is elected in case there are no candidates to start with.</param>
        public void Init(int partitionSetElection, int partitionSetSynchronization, bool controlColor = false, int kappa = 3, bool startAsCandidate = true)
        {
            partitionSetElect.SetValue(partitionSetElection);
            partitionSetSync.SetValue(partitionSetSynchronization);
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
        /// call. Check <see cref="NeedSyncCircuit"/> whether the correct pin configuration
        /// is the synchronization circuit.
        /// </summary>
        public void ActivateReceive()
        {
            if (finished.GetCurrentValue() || round.GetCurrentValue() == -1)
                return;

            PinConfiguration pc = algo.GetCurrentPinConfiguration();

            if (round.GetCurrentValue() == 0)
            {
                // Check if we have received a synchronization beep
                bool receivedBeep = pc.ReceivedBeepOnPartitionSet(partitionSetSync.GetCurrentValue());

                // Move on to the next phase if no beep was received
                if (!receivedBeep)
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
                // Received continuation beep, have to start another iteration
                else
                {
                    // Toss a coin if we are a candidate
                    if (isLeaderCandidate.GetCurrentValue())
                        TossCoin();
                }
            }
            //else if (round.GetCurrentValue() % 2 == 0)
            //{
            //    // Other even round (2 or 4)
            //    beepFromTails.SetValue(pc.ReceivedBeepOnPartitionSet(partitionSetElect.GetCurrentValue()));

            //    // Check if we have tossed TAILS and received a HEADS beep
            //    if (!heads.GetCurrentValue() && beepFromHeads.GetCurrentValue())
            //    {
            //        // We have to withdraw our candidacy in this case
            //        // Proper candidates withdraw after round 0,1
            //        if (round.GetCurrentValue() == 2)
            //        {
            //            if (isLeaderCandidate.GetCurrentValue())
            //                isLeaderCandidate.SetValue(false);
            //        }
            //        // Phase 2 candidates withdraw in round 4
            //        else if (isPhase2Candidate.GetCurrentValue())
            //            isPhase2Candidate.SetValue(false);
            //    }

            //    // Toss a coin if we are a candidate in the correct phase and round
            //    if (!firstPhase.GetCurrentValue() && round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
            //        TossCoin();
            //}
            else
            {
                // Rounds 1 and 2: Receive HEADS beep
                beepFromHeads.SetValue(pc.ReceivedBeepOnPartitionSet(partitionSetElect.GetCurrentValue()));
            }






            //if (round.GetCurrentValue() % 2 == 0)
            //{
            //    // Even round: Receive TAILS beep and decide what should be done next
            //    beepFromTails.SetValue(receivedBeep);

            //    // In round 0, we switch phases or terminate
            //    // If one of the two coin toss results did not occur: Terminate
            //    if (round.GetCurrentValue() == 0 && (!beepFromHeads.GetCurrentValue() || !beepFromTails.GetCurrentValue()))
            //    {
            //        // Either start an iteration of phase 2 or terminate completely
            //        if (firstPhase.GetCurrentValue() && kappa.GetCurrentValue() > 0 || repetitions.GetCurrentValue() < kappa.GetCurrentValue())
            //        {
            //            // We need to start an iteration of phase 2
            //            firstPhase.SetValue(false);
            //            repetitions.SetValue(repetitions.GetCurrentValue() + 1);
            //            isPhase2Candidate.SetValue(true);

            //            // Only proper candidates toss a coin here
            //            if (isLeaderCandidate.GetCurrentValue())
            //                TossCoin();
            //        }
            //        else
            //        {
            //            // This was the last iteration
            //            finished.SetValue(true);
            //            if (controlColor.GetCurrentValue())
            //                UpdateColor();
            //            return;
            //        }
            //    }
            //    // Not round 0 or we received both kinds of beeps
            //    else
            //    {
            //        // Check if we have tossed TAILS and received a HEADS beep
            //        if (!heads.GetCurrentValue() && beepFromHeads.GetCurrentValue())
            //        {
            //            // We have to withdraw our candidacy in this case
            //            // Proper candidates withdraw in first phase or after round 0,1
            //            if (firstPhase.GetCurrentValue() || round.GetCurrentValue() == 2)
            //            {
            //                if (isLeaderCandidate.GetCurrentValue())
            //                    isLeaderCandidate.SetValue(false);
            //            }
            //            else if (isPhase2Candidate.GetCurrentValue())
            //                isPhase2Candidate.SetValue(false);
            //        }
                    
            //        // Toss a coin if we are a candidate in the correct round
            //        if (round.GetCurrentValue() == 0 && isLeaderCandidate.GetCurrentValue()
            //            || round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
            //            TossCoin();
            //    }
            //}
            //else
            //{
            //    // Odd round: Receive HEADS beep
            //    beepFromHeads.SetValue(receivedBeep);
            //}

            if (controlColor.GetCurrentValue())
                UpdateColor();
        }

        /// <summary>
        /// The second half of the beep activation. Must be
        /// called only when the correct pin configuration has
        /// been established. Call <see cref="NeedSyncCircuit"/> to check
        /// which pin configuration is required. The beeps sent by this method
        /// must be received by the next call of <see cref="ActivateReceive"/>.
        /// </summary>
        public void ActivateSend()
        {
            if (finished.GetCurrentValue())
                return;

            PinConfiguration pc = algo.GetPlannedPinConfiguration();
            if (pc == null)
                pc = algo.GetCurrentPCAsPlanned();

            if (round.GetCurrentValue() == -1)
            {
                // First round: Candidates toss coin and send a beep on the election circuit
                if (isLeaderCandidate.GetCurrentValue() && TossCoin())
                    pc.SendBeepOnPartitionSet(partitionSetElect.GetCurrentValue());

                // This is a stand-in for round 0, so we continue with round 1
                round.SetValue(1);
                if (controlColor.GetCurrentValue())
                    UpdateColor();
                return;
            }

            if (round.GetCurrentValue() == 0)
            {
                // If we are a candidate and have tossed HEADS, we send a beep
                if (heads.GetCurrentValue() && isLeaderCandidate.GetCurrentValue())
                    pc.SendBeepOnPartitionSet(partitionSetElect.GetCurrentValue());

                //// If we have received both results and are in the first phase, we send a
                //// continuation beep in round 2
                //if (firstPhase.GetCurrentValue() && round.GetCurrentValue() == 2 && beepFromHeads.GetCurrentValue() && beepFromTails.GetCurrentValue())
                //    pc.SendBeepOnPartitionSet(partitionSetSync.GetCurrentValue());

                // Also reset HEADS beep flag
                beepFromHeads.SetValue(false);

                // Increment counter
                round.SetValue(round.GetCurrentValue() + 1);
            }
            //else if (round.GetCurrentValue() == 4)
            //{
            //    // If we have received both a HEADS and a TAILS beep,
            //    // we send a beep on the synchronization circuit
            //    if (beepFromHeads.GetCurrentValue() && beepFromTails.GetCurrentValue())
            //        pc.SendBeepOnPartitionSet(partitionSetSync.GetCurrentValue());

            //    // Reset both beep flags
            //    beepFromHeads.SetValue(false);
            //    beepFromTails.SetValue(false);

            //    // Reset counter
            //    round.SetValue(0);
            //}
            else
            {
                // Rounds 1 and 2
                // Check if we have tossed TAILS and received a HEADS beep
                if (!heads.GetCurrentValue() && beepFromHeads.GetCurrentValue())
                {
                    // We have to withdraw our candidacy in this case
                    // Proper candidates withdraw in round 1, phase 2 candidates in round 2
                    if (round.GetCurrentValue() == 1 && isLeaderCandidate.GetCurrentValue())
                    {
                        isLeaderCandidate.SetValue(false);

                        // Send a beep on the synchronization circuit
                        // if we are still in the first phase
                        if (firstPhase.GetCurrentValue())
                            pc.SendBeepOnPartitionSet(partitionSetSync.GetCurrentValue());
                    }
                    else if (round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
                    {
                        isPhase2Candidate.SetValue(false);

                        // Send a beep on the synchronization circuit
                        pc.SendBeepOnPartitionSet(partitionSetSync.GetCurrentValue());
                    }
                }

                // Reset head beep
                beepFromHeads.SetValue(false);

                // In phase 2, candidates toss coins in round 1 and send beep on election circuit
                // if the result was HEADS
                if (!firstPhase.GetCurrentValue() && round.GetCurrentValue() == 1 && isPhase2Candidate.GetCurrentValue())
                {
                    if (TossCoin())
                        pc.SendBeepOnPartitionSet(partitionSetElect.GetCurrentValue());
                }

                // Increment counter
                if (firstPhase.GetCurrentValue())
                    round.SetValue(0);
                else
                    round.SetValue((round.GetCurrentValue() + 1) % 3);
            }







            //if (round.GetCurrentValue() % 2 == 0)
            //{
            //    // Even round
            //    // If we are a candidate and have tossed HEADS, we send a beep
            //    if (heads.GetCurrentValue())
            //    {
            //        if (round.GetCurrentValue() == 0 && isLeaderCandidate.GetCurrentValue()
            //            || round.GetCurrentValue() == 2 && isPhase2Candidate.GetCurrentValue())
            //        {
            //            pc.SendBeepOnPartitionSet(partitionSetElect.GetCurrentValue());
            //        }
            //    }

            //    // Also reset both beep flags
            //    beepFromHeads.SetValue(false);
            //    beepFromTails.SetValue(false);

            //    // Increment counter
            //    round.SetValue(round.GetCurrentValue() + 1);
            //}
            //else
            //{
            //    // Odd round
            //    // If we are a candidate and have tossed TAILS, we send a beep
            //    if (!heads.GetCurrentValue())
            //    {
            //        if (round.GetCurrentValue() == 1 && isLeaderCandidate.GetCurrentValue()
            //            || round.GetCurrentValue() == 3 && isPhase2Candidate.GetCurrentValue())
            //        {
            //            pc.SendBeepOnPartitionSet(partitionSetElect.GetCurrentValue());
            //        }
            //    }

            //    // Increment counter
            //    if (firstPhase.GetCurrentValue() || round.GetCurrentValue() == 3)
            //        round.SetValue(0);
            //    else
            //        round.SetValue(2);
            //}

            if (controlColor.GetCurrentValue())
                UpdateColor();
        }

        public bool NeedSyncCircuit()
        {
            return round.GetCurrentValue() > 0;
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

} // namespace AS2.Subroutines.LeaderElectionSync
