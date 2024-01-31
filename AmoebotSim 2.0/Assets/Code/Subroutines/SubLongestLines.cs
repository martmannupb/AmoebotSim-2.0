using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.PASC;

namespace AS2.Subroutines.LongestLines
{

    /// <summary>
    /// Procedure for finding all longest lines in the amoebot
    /// system. If longest lines exist in multiple directions,
    /// only lines from one direction will be selected.
    /// <para>
    /// This is part of the shape containment algorithm suite, which uses
    /// 4 pins per edge.
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <list type="bullet">
    /// <item>
    ///     Initialize by calling <see cref="Init"/>.
    /// </item>
    /// <item>
    ///     Call <see cref="SetupPC(PinConfiguration)"/> followed by
    ///     <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/>
    ///     and <see cref="ActivateSend"/> to send the first beeps.
    /// </item>
    /// <item>
    ///     In the very next round, call <see cref="ActivateReceive"/> to receive
    ///     the sent beeps. This must be done every time <see cref="ActivateSend"/>
    ///     was called. You can wait arbitraryily long before calling <see cref="ActivateSend"/>
    ///     again. Before each call, you must call <see cref="SetupPC(PinConfiguration)"/>
    ///     and <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/>.
    /// </item>
    /// <item>
    ///     After each <see cref="ActivateReceive"/> call, check whether the procedure
    ///     has finished with <see cref="IsFinished"/>.
    /// </item>
    /// <item>
    ///     After the procedure is finished, use <see cref="IsOnMaxLine"/>,
    ///     <see cref="IsMSB"/>, <see cref="GetBit"/> and <see cref="GetMaxDir"/> to
    ///     read the resulting information.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    
    // Round 0:
    //  Send:
    //  - Place markers and MSBs at line starts
    //  - Setup PASC circuits in all 3 directions
    //      - Setup additional connections to forward bits and markers
    //  - Send PASC and marker beep

    // (Lines of length 0: Only do global circuits and wait for the first phase to complete)

    // Round 1:
    //  Receive:
    //  - Receive PASC beep
    //  - Amoebot holding the marker stores the received bit and removes its marker
    //      - If the bit is a 1, place the MSB at the marker and remove it from its previous position
    //  - Amoebot receiving the marker beep takes the marker
    //  Send:
    //  - Setup a global circuit
    //  - Send beep if we became passive this round

    // Round 2:
    //  Receive:
    //  - If no beep received on global circuit:
    //      - Place markers at line starts
    //      - Go to round 3
    //  Send:
    //  - Setup PASC, bit and marker circuits again
    //  - Send beeps
    //  - Go to round 1

    // Round 3:
    //  Receive (only entered after the first send):
    //  - If no beep was received on the global circuit:
    //      - Place marker at the MSB if we are still active
    //      - Go to round 4
    //  - If the line did not send a beep but received one on the global circuit: Retire
    //  - Marker gets moved forward
    //  Send:
    //  - Setup global circuit, line circuit and marker circuit
    //  - Marker sends beep on all circuits
    //  - Marker sends forwarding beep unless it is the MSB

    // Round 4:
    //  Receive (only entered after the first send):
    //  - If no beep on second global circuit:
    //      - Go to round 5
    //  - If no beep on line circuit but beep on global circuit: Retire (but continue moving the marker)
    //  - Marker moves to next position
    //  Send:
    //  - Setup 2 global circuits, line circuit and marker circuit
    //  - Marker sends beep if bit is 1
    //  - Send marker beep to predecessor
    //  - Marker sends beep on second global circuit

    // Round 5:
    //  Send:
    //  - Setup 3 global circuits
    //  - Longest lines beep on the circuit belonging to their direction
    //  Receive:
    //  - Only lines in the smallest direction remain, other ones retire
    //  - Terminate


    public class SubLongestLines : Subroutine
    {
        // We encode the state in a single integer
        // Bits 0-2 encode the round (0-5)
        // Bits 3-4 encode the direction of the max. length line (0-2)
        // Bits 5-7 are the retired flags for the 3 directions
        // Bits 8-10 are the marker flags
        // Bits 11-13 are the stored bit flags
        // Bits 14-16 are the MSB flags
        // Bits 17-19 are the length = 0 flags (length 0 lines retire immediately and wait for the procedure to finish)
        // Bits 20-22 are the predecessor existence flags (to avoid checking existence over and over again)
        // Bits 23-25 are the successor existence flags
        // Bit 26 is the finished flag
        //          26         25       24       23       22       21       20       19       18       17       16      15      14      13      12      11      10       9        8        7        6        5        43         210
        // xxxx x   x          x        x        x        x        x        x        x        x        x        x       x       x       x       x       x       x        x        x        x        x        x        xx         xxx
        //          Finished   Succ 3   Succ 2   Succ 1   Pred 3   Pred 2   Pred 1   Len0 3   Len0 2   Len0 1   MSB 3   MSB 2   MSB 1   Bit 3   Bit 2   Bit 1   Mark 3   Mark 2   Mark 1   Ret. 3   Ret. 2   Ret. 1   Max. Dir   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_round = 0;
        private const int bit_maxDir = 3;
        private const int bit_retired = 5;
        private const int bit_marker = 8;
        private const int bit_bit = 11;
        private const int bit_MSB = 14;
        private const int bit_len0 = 17;
        private const int bit_pred = 20;
        private const int bit_succ = 23;
        private const int bit_finished = 26;

        // Need 3 PASC subroutines
        SubPASC2[] pasc = new SubPASC2[3];

        public SubLongestLines(Particle p, SubPASC2 pasc1 = null, SubPASC2 pasc2 = null, SubPASC2 pasc3 = null) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Lines] State"), 0);
            SubPASC2[] pascGiven = new SubPASC2[] { pasc1, pasc2, pasc3 };
            for (int i = 0; i < 3; i++)
            {
                if (pascGiven[i] is null)
                {
                    pasc[i] = new SubPASC2(p);
                }
                else
                {
                    pasc[i] = pascGiven[i];
                }
            }
        }

        public void Init()
        {
            state.SetValue(0);

            // Find lines of length 0 and initialize PASC
            for (int i = 0; i < 3; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                bool pred = algo.HasNeighborAt(d.Opposite());
                bool succ = algo.HasNeighborAt(d);
                if (!pred && !succ)
                {
                    SetFlag(bit_len0, i, true);
                    SetFlag(bit_retired, i, true);
                }
                else
                {
                    // PASC partition set IDs are 4*i and 4*i + 1 for direction index i
                    pasc[i].Init(pred ? new List<Direction>() { d.Opposite() } : null, succ ? new List<Direction>() { d } : null, 0, 1, 4 * i, 4 * i + 1, !pred);
                }
                // Place marker and MSB
                if (!pred)
                {
                    SetFlag(bit_marker, i, true);
                    SetFlag(bit_MSB, i, true);
                }
                // Store predecessor and successor flags
                SetFlag(bit_pred, i, pred);
                SetFlag(bit_succ, i, succ);
            }
        }

        public void ActivateReceive()
        {
            if (Finished)
                return;

            int round = Round;
            PinConfiguration pc = algo.GetCurrentPinConfiguration();

            switch (round)
            {
                case 1:
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (!GetFlag(bit_len0, i))
                            {
                                pasc[i].ActivateReceive();
                                bool bit = pc.ReceivedBeepOnPartitionSet(4 * i + 2);
                                // Move MSB to marker if bit is 1
                                if (bit && GetFlag(bit_MSB, i))
                                {
                                    SetFlag(bit_MSB, i, false);
                                }
                                // Marker receives new bit and sets MSB
                                if (GetFlag(bit_marker, i))
                                {
                                    SetFlag(bit_bit, i, bit);
                                    if (bit)
                                        SetFlag(bit_MSB, i, true);
                                }
                            }

                            // Move marker forward
                            SetFlag(bit_marker, i, pc.GetPinAt(DirectionHelpers.Cardinal(i).Opposite(), 0).PartitionSet.ReceivedBeep());
                        }
                    }
                    break;
                case 2:
                    {
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // No beep on global circuit => Continue with next phase
                            // Place markers at line starts
                            for (int i = 0; i < 3; i++)
                            {
                                SetFlag(bit_marker, i, !GetFlag(bit_pred, i));
                            }

                            Round = 3;
                        }
                    }
                    break;
                case 3:
                    {
                        if (!pc.ReceivedBeepOnPartitionSet(0))
                        {
                            // No beep on global circuit: Place marker at MSB if we are still active (each line)
                            for (int i = 0; i < 3; i++)
                            {
                                SetFlag(bit_marker, i, !GetFlag(bit_retired, i) && GetFlag(bit_MSB, i));
                            }

                            Round = 4;
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                // Retire if the line did not send a beep but there was one on the global circuit
                                if (!pc.ReceivedBeepOnPartitionSet(4 * i + 2))
                                {
                                    SetFlag(bit_retired, i, true);
                                }
                                // Move marker forward
                                if (GetFlag(bit_marker, i))
                                    SetFlag(bit_marker, i, false);
                                else if (pc.GetPinAt(DirectionHelpers.Cardinal(i).Opposite(), 0).PartitionSet.ReceivedBeep())
                                    SetFlag(bit_marker, i, true);
                            }
                        }
                    }
                    break;
                case 4:
                    {
                        if (!pc.ReceivedBeepOnPartitionSet(1))
                        {
                            // No beep on second global circuit: Go to round 5
                            Round = 5;
                        }
                        else
                        {
                            // Retire if there was a beep on the global circuit but not on the line circuit
                            bool beepGlobal = pc.ReceivedBeepOnPartitionSet(0);
                            for (int i = 0; i < 3; i++)
                            {
                                if (!GetFlag(bit_retired, i))
                                {
                                    if (beepGlobal && !pc.ReceivedBeepOnPartitionSet(4 * i + 2))
                                    {
                                        SetFlag(bit_retired, i, true);
                                    }
                                }
                                // Move marker forward
                                if (GetFlag(bit_marker, i))
                                    SetFlag(bit_marker, i, false);
                                else if (pc.GetPinAt(DirectionHelpers.Cardinal(i), 3).PartitionSet.ReceivedBeep())
                                    SetFlag(bit_marker, i, true);
                            }
                        }
                    }
                    break;
                case 5:
                    {
                        // Beeps on global circuits determine which directions have longest lines
                        // Only lines in the first direction remain
                        int d = -1;
                        if (pc.ReceivedBeepOnPartitionSet(0))
                            d = 0;
                        else if (pc.ReceivedBeepOnPartitionSet(1))
                            d = 1;
                        else if (pc.ReceivedBeepOnPartitionSet(2))
                            d = 2;
                        MaxDir = d;
                        for (int i = 0; i < 3; i++)
                        {
                            if (i != d && !GetFlag(bit_retired, i))
                                SetFlag(bit_retired, i, true);
                        }
                        Finished = true;
                    }
                    break;
            }
        }

        public void SetupPC(PinConfiguration pc)
        {
            if (Finished)
                return;

            int round = Round;

            switch (round)
            {
                case 0:
                case 2:
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (!GetFlag(bit_len0, i))
                            {
                                SetupPASCCircuit(pc, i, !GetFlag(bit_succ, i));
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        pc.SetToGlobal(0);
                    }
                    break;
                case 3:
                case 4:
                    {
                        SetupGlobalAndLineCircuit(pc);
                    }
                    break;
                case 5:
                    {
                        // Setup 3 global circuits
                        bool[] inverted = new bool[] { false, false, false, true, true, true };
                        pc.SetStarConfig(0, inverted, 0);
                        pc.SetStarConfig(1, inverted, 1);
                        pc.SetStarConfig(2, inverted, 2);
                    }
                    break;
            }
        }

        public void ActivateSend()
        {
            if (Finished)
                return;

            int round = Round;

            switch(round)
            {
                case 0:
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (!GetFlag(bit_len0, i))
                            {
                                pasc[i].ActivateSend();
                                // Send marker beep
                                if (GetFlag(bit_marker, i))
                                {
                                    PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                    pc.SendBeepOnPartitionSet(pc.GetPinAt(DirectionHelpers.Cardinal(i), 3).PartitionSet.Id);
                                }
                            }
                        }
                        Round = round + 1;
                    }
                    break;
                case 1:
                    {
                        bool send = false;
                        for (int i = 0; i < 3; i++)
                        {
                            if (!GetFlag(bit_len0) && pasc[i].BecamePassive())
                            {
                                send = true;
                                break;
                            }
                        }
                        if (send)
                        {
                            PinConfiguration pc = algo.GetPlannedPinConfiguration();
                            pc.SendBeepOnPartitionSet(0);
                        }
                        Round = round + 1;
                    }
                    break;
                case 2:
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (!GetFlag(bit_len0, i))
                            {
                                pasc[i].ActivateSend();
                                // Send marker beep
                                if (GetFlag(bit_marker, i))
                                {
                                    PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                    pc.SendBeepOnPartitionSet(pc.GetPinAt(DirectionHelpers.Cardinal(i), 3).PartitionSet.Id);
                                }
                            }
                        }
                        Round = 1;
                    }
                    break;
                case 3:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Marker sends beep on all lines and on first global circuit
                        // Marker also sends forwarding beep unless it is also the MSB
                        bool sendGlobal = false;
                        for (int i = 0; i < 3; i++)
                        {
                            if (GetFlag(bit_marker, i))
                            {
                                sendGlobal = true;
                                pc.SendBeepOnPartitionSet(4 * i + 2);
                                if (!GetFlag(bit_MSB, i))
                                {
                                    pc.GetPinAt(DirectionHelpers.Cardinal(i), 3).PartitionSet.SendBeep();
                                }
                            }
                        }
                        if (sendGlobal)
                        {
                            pc.SendBeepOnPartitionSet(0);
                        }
                    }
                    break;
                case 4:
                    {
                        PinConfiguration pc = algo.GetPlannedPinConfiguration();
                        // Marker sends beep on first global circuit and line if bit is 1 (each line, only non-retired)
                        // Marker always sends beep on second global circuit
                        // Also send forwarding beep to predecessor
                        bool sendGlobal1 = false;
                        bool sendGlobal2 = false;
                        for (int i = 0; i < 3; i++)
                        {
                            if (GetFlag(bit_marker, i))
                            {
                                sendGlobal2 = true;
                                if (GetFlag(bit_bit, i) && !GetFlag(bit_retired, i))
                                {
                                    sendGlobal1 = true;
                                    pc.SendBeepOnPartitionSet(4 * i + 2);
                                }
                                pc.GetPinAt(DirectionHelpers.Cardinal(i).Opposite(), 0).PartitionSet.SendBeep();
                            }
                        }
                        if (sendGlobal1)
                            pc.SendBeepOnPartitionSet(0);
                        if (sendGlobal2)
                            pc.SendBeepOnPartitionSet(1);
                    }
                    break;
                case 5:
                    {
                        // Longest lines beep on circuit belonging to their direction
                        for (int i = 0; i < 3; i++)
                        {
                            if (!GetFlag(bit_retired, i))
                            {
                                PinConfiguration pc = algo.GetPlannedPinConfiguration();
                                pc.SendBeepOnPartitionSet(i);
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Checks whether the procedure is finished.
        /// </summary>
        /// <returns><c>true</c> if and only if the longest lines have been
        /// identified.</returns>
        public bool IsFinished()
        {
            return Finished;
        }

        /// <summary>
        /// Checks whether this amoebot is on a maximum length line.
        /// </summary>
        /// <returns><c>true</c> if and only if the procedure is finished
        /// and this amoebot is on a maximum line.</returns>
        public bool IsOnMaxLine()
        {
            if (!Finished)
                return false;

            return !GetFlag(bit_retired, MaxDir);
        }

        /// <summary>
        /// Checks which direction the maximum length lines are in.
        /// </summary>
        /// <returns>The direction of the maximum length lines, if
        /// the procedure has finished.</returns>
        public Direction GetMaxDir()
        {
            if (!Finished)
                return Direction.NONE;

            return DirectionHelpers.Cardinal(MaxDir);
        }

        /// <summary>
        /// Returns the bit stored at this counter position on a longest line.
        /// </summary>
        /// <returns><c>true</c> if and only if we are on a longest line after
        /// the procedure has finished and our counter bit is 1.</returns>
        public bool GetBit()
        {
            if (!Finished)
                return false;

            int maxDir = MaxDir;
            return !GetFlag(bit_retired, maxDir) && GetFlag(bit_bit, maxDir);
        }

        /// <summary>
        /// Checks whether this is the MSB of the length counter on a longest line.
        /// </summary>
        /// <returns><c>true</c> if and only if we are on a longest line after
        /// the procedure has finished and we are the counter's MSB.</returns>
        public bool IsMSB()
        {
            if (!Finished)
                return false;

            int maxDir = MaxDir;
            return !GetFlag(bit_retired, maxDir) && GetFlag(bit_MSB, maxDir);
        }

        /// <summary>
        /// Helper setting up a pin configuration to establish a PASC circuit along
        /// the given direction and a simple line connected to the PASC endpoint's
        /// secondary partition set.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="direction">The direction in which the PASC circuit runs.</param>
        /// <param name="isEndpoint">Whether this is the circuit's end point.</param>
        private void SetupPASCCircuit(PinConfiguration pc, int direction, bool isEndpoint)
        {
            pasc[direction].SetupPC(pc);
            Direction d = DirectionHelpers.Cardinal(direction);
            if (isEndpoint)
            {
                pc.GetPartitionSet(4 * direction + 1).AddPin(pc.GetPinAt(d.Opposite(), 1).Id);
                pc.SetPartitionSetPosition(4 * direction + 1, new Vector2(d.Opposite().ToInt() * 60f, 0.7f));
            }
            else
            {
                // Leader does not need to connect
                if (!pasc[direction].IsLeader())
                {
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 2).Id, pc.GetPinAt(d.Opposite(), 1).Id }, 4 * direction + 2);
                    pc.SetPartitionSetPosition(4 * direction + 2, new Vector2((d.Opposite().ToInt() - 0.15f) * 60, 0.6f));
                }
                else
                {
                    pc.MakePartitionSet(new int[] { pc.GetPinAt(d, 2).Id }, 4 * direction + 2);
                }
            }
        }

        /// <summary>
        /// Helper setting up two global circuits and one line circuit for each line.
        /// The two global circuits have IDs 0 and 1, the line circuits have IDs
        /// 4i + 2 for direction i.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        private void SetupGlobalAndLineCircuit(PinConfiguration pc)
        {
            // Two global circuits
            bool[] inverted = new bool[] { false, false, false, true, true, true };
            pc.SetStarConfig(0, inverted, 0);
            pc.SetStarConfig(1, inverted, 1);
            pc.SetPartitionSetPosition(0, new Vector2(4 * 60f, 0.7f));
            pc.SetPartitionSetPosition(1, new Vector2(5 * 60f, 0.7f));
            // Line circuits
            for (int i = 0; i < 3; i++)
            {
                Direction d = DirectionHelpers.Cardinal(i);
                bool pred = GetFlag(bit_pred, i);
                bool succ = GetFlag(bit_succ, i);
                int pinPred = pc.GetPinAt(d.Opposite(), 1).Id;
                int pinSucc = pc.GetPinAt(d, 2).Id;
                int pSet = 4 * i + 2;
                if (pred && succ)
                {
                    // Connect
                    pc.MakePartitionSet(new int[] { pinPred, pinSucc }, pSet);
                    pc.SetPartitionSetPosition(4 * i + 2, new Vector2((i + 0.25f) * 60, 0.6f));
                }
                else if (pred)
                {
                    pc.MakePartitionSet(new int[] { pinPred }, pSet);
                }
                else
                {
                    // Always set this up, even if we have no successor
                    pc.MakePartitionSet(new int[] { pinSucc }, pSet);
                }
            }
        }

        // State helpers

        #region State Helpers

        private int Round
        {
            get { return (state.GetCurrentValue() >> bit_round) & 7; }
            set { state.SetValue((state.GetCurrentValue() & ~(7 << bit_round)) | (value << bit_round)); }
        }

        private int MaxDir
        {
            get { return (state.GetCurrentValue() >> bit_maxDir) & 3; }
            set { state.SetValue((state.GetCurrentValue() & ~(3 << bit_maxDir)) | (value << bit_maxDir)); }
        }

        private bool Finished
        {
            get { return ((state.GetCurrentValue() >> bit_finished) & 1) > 0; }
            set { state.SetValue((state.GetCurrentValue() & ~(1 << bit_finished)) | ((value ? 1 : 0) << bit_finished)); }
        }

        private bool GetFlag(int flag, int index = 0)
        {
            return ((state.GetCurrentValue() >> (flag + index)) & 1) > 0;
        }

        private void SetFlag(int flag, int index, bool value)
        {
            int bit = flag + index;
            state.SetValue((state.GetCurrentValue() & ~(1 << bit)) | (value ? 1 : 0) << bit);
        }
        #endregion
    }

} // namespace AS2.Subroutines.LongestLines
