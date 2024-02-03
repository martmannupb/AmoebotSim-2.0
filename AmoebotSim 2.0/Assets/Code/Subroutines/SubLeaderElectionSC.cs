using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.BinStateHelpers;

namespace AS2.Subroutines.LeaderElectionSC
{

    /// <summary>
    /// Fast leader election subroutine for shape containment
    /// algorithms. Uses all 4 pins to establish 4 global circuits
    /// and transmit all coin toss results in a single round instead
    /// of 4. Also skips the first phase because it is unnecessary.
    /// <para>
    /// See <see cref="AS2.Subroutines.LeaderElection.SubLeaderElection"/>
    /// for a more detailed description.
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <list type="bullet">
    /// <item>
    ///     Initialize by calling <see cref="Init(bool, bool, int)"/>. At least
    ///     one amoebot in the system should be a candidate.
    /// </item>
    /// <item>
    ///     Setup a pin configuration using <see cref="SetupPC(PinConfiguration)"/>.
    ///     This only has to be called once and the same pin configuration can be used
    ///     for the whole duration of the leader election.
    /// </item>
    /// <item>
    ///     Call <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/>
    ///     and <see cref="ActivateSend"/> to send the beeps and then call
    ///     <see cref="ActivateReceive"/> in the next round to receive the beeps. Then repeat
    ///     this until the procedure is finished.
    /// </item>
    /// <item>
    ///     You can check whether the procedure has finished using <see cref="IsFinished"/>
    ///     and find the elected leader using <see cref="IsLeader"/>.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    public class SubLeaderElectionSC : Subroutine
    {
        // State:
        //                21     14  13     6   5            4        3        2          1         0
        // xxxx xxxx xx   xxxxxxxx   xxxxxxxx   x            x        x        x          x         x
        //                Kappa      Counter    Ctrl color   HEAD 2   HEAD 1   Finished   Cand. 2   Cand. 1
        ParticleAttribute<int> state;

        BinAttributeBool cand1;             // Whether we are a main candidate
        BinAttributeBool cand2;             // Whether we are a helper candidate (every participant starts as one)
        BinAttributeBool finished;          // Whether we are finished
        BinAttributeBool head1;             // Last main coin toss result
        BinAttributeBool head2;             // Last helper coin toss result
        BinAttributeBool controlColor;      // Whether we should control the amoebot color
        BinAttributeInt counter;            // Current number of repetitions completed
        BinAttributeInt kappa;              // The goal number of repetitions

        // Colors
        private static readonly Color candidateColor = ColorData.Particle_Green;
        private static readonly Color helperColor = ColorData.Particle_Blue;
        private static readonly Color retiredColor = ColorData.Particle_BlueDark;
        private static readonly Color finishedColor = ColorData.Particle_Black;

        public SubLeaderElectionSC(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[LE] State"), 0);

            cand1 = new BinAttributeBool(state, 0);
            cand2 = new BinAttributeBool(state, 1);
            finished = new BinAttributeBool(state, 2);
            head1 = new BinAttributeBool(state, 3);
            head2 = new BinAttributeBool(state, 4);
            controlColor = new BinAttributeBool(state, 5);
            counter = new BinAttributeInt(state, 6, 8);
            kappa = new BinAttributeInt(state, 14, 8);
        }

        /// <summary>
        /// Initializes the leader election.
        /// </summary>
        /// <param name="isCandidate">Whether this amoebot is a leader
        /// candidate. There should be at least one candidate in the system.</param>
        /// <param name="controlColor">Whether the subroutine should control the
        /// amoebot color to indicate its progress.</param>
        /// <param name="kappa">The number of repetitions to complete.</param>
        public void Init(bool isCandidate, bool controlColor = false, int kappa = 3)
        {
            state.SetValue(0);
            cand1.SetValue(isCandidate);
            cand2.SetValue(true);
            this.controlColor.SetValue(controlColor);
            this.kappa.SetValue(kappa);
            if (controlColor)
            {
                if (isCandidate)
                    algo.SetMainColor(candidateColor);
                else
                    algo.SetMainColor(helperColor);
            }
        }

        /// <summary>
        /// First half of the subroutine activation. Receives beeps
        /// sent by <see cref="ActivateSend"/> in the last round.
        /// </summary>
        public void ActivateReceive()
        {
            PinConfiguration pc = algo.GetCurrentPinConfiguration();

            bool h1 = pc.ReceivedBeepOnPartitionSet(0);
            bool t1 = pc.ReceivedBeepOnPartitionSet(1);
            bool h2 = pc.ReceivedBeepOnPartitionSet(2);
            bool t2 = pc.ReceivedBeepOnPartitionSet(3);

            // Problem: Nobody sent a beep -> Terminate and send error log
            if (!h1 && !t1 || !h2 && !t2)
            {
                finished.SetValue(true);
                if (controlColor.GetValue())
                    algo.SetMainColor(ColorData.Particle_Red);
                cand1.SetValue(false);
                cand2.SetValue(false);
                Log.Warning("Leader election error: No candidates left.");
                return;
            }

            // Candidates withdraw candidacy
            if (h1 && t1 && cand1.GetValue() && !head1.GetValue())
                cand1.SetValue(false);
            if (h2 && t2 && cand2.GetValue() && !head2.GetValue())
                cand2.SetValue(false);

            // End iteration if both types of candidates had only one type of coin toss result
            if ((h1 ^ t1) && (h2 ^ t2))
            {
                int numIterations = counter.GetValue() + 1;
                counter.SetValue(numIterations);
                if (numIterations >= kappa.GetValue())
                {
                    // Termiante
                    finished.SetValue(true);
                }
                else
                {
                    // Start next iteration
                    cand2.SetValue(true);
                }
            }

            // Set color if desired
            if (controlColor.GetValue())
            {
                if (cand1.GetCurrentValue())
                    algo.SetMainColor(candidateColor);
                else if (finished.GetCurrentValue())
                    algo.SetMainColor(finishedColor);
                else if (cand2.GetCurrentValue())
                    algo.SetMainColor(helperColor);
                else
                    algo.SetMainColor(retiredColor);
            }
        }

        /// <summary>
        /// Sets up the pin configuration required for the next
        /// <see cref="ActivateSend"/> call. Creates 4 global circuits
        /// using all 4 pins and partition set IDs 0,...,3.
        /// <i>This is not necessary if the pin configuration was not
        /// changed since the first call of this method.</i>
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        public void SetupPC(PinConfiguration pc)
        {
            // Setup 4 global circuits
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            for (int i = 0; i < 4; i++)
                pc.SetStarConfig(i, inverted, i);
        }

        /// <summary>
        /// Second half of the subroutine activation. Sends beeps to
        /// be received by <see cref="ActivateReceive"/> in the next
        /// round. The required pin configuration must have been set
        /// up by <see cref="SetupPC(PinConfiguration)"/> in some
        /// previous round and it must already be planned in this round.
        /// </summary>
        public void ActivateSend()
        {
            bool c1 = cand1.GetCurrentValue();
            bool c2 = cand2.GetCurrentValue();
            if (!c1 && !c2)
                return;

            PinConfiguration pc = algo.GetPlannedPinConfiguration();

            // Toss coin(s) and send result on global circuit(s)
            if (c1)
            {
                bool head = Random.Range(0f, 1f) < 0.5f;
                if (head)
                    pc.SendBeepOnPartitionSet(0);
                else
                    pc.SendBeepOnPartitionSet(1);
                head1.SetValue(head);
            }

            if (c2)
            {
                bool head = Random.Range(0f, 1f) < 0.5f;
                if (head)
                    pc.SendBeepOnPartitionSet(2);
                else
                    pc.SendBeepOnPartitionSet(3);
                head2.SetValue(head);
            }
        }

        /// <summary>
        /// Checks whether the subroutine is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the
        /// subroutine has finished.</returns>
        public bool IsFinished()
        {
            return finished.GetCurrentValue();
        }

        /// <summary>
        /// Checks whether this amoebot is the elected leader.
        /// </summary>
        /// <returns><c>true</c> if and only if the subroutine
        /// has finished and this amoebot is the leader.</returns>
        public bool IsLeader()
        {
            return IsFinished() && cand1.GetCurrentValue();
        }
    }

} // namespace AS2.Subroutines.LeaderElectionSC
