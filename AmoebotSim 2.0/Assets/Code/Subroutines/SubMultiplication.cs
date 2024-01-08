using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Implements binary multiplication for two numbers a, b
    /// stored in the same chain.
    /// <para>
    /// Computes a new binary number <c>c := a * b</c>. If the binary representation of
    /// <c>c</c> does not fit into the chain, only the lowermost bits are computed and an
    /// overflow is detected.
    /// </para>
    /// <para>
    /// This procedure requires at least 2 pins and it always uses the 2
    /// "outermost / leftmost" pins when traversing the chain. If an amoebot
    /// occurs on the chain multiple times, its predecessor and successor directions
    /// must be different for all occurrences.
    /// </para>
    /// <para>
    /// <b>Usage</b>:
    /// <list type="bullet">
    /// <item>
    ///     Establish a chain of amoebots such that each amoebot knows its predecessor and successor
    ///     (except the start and end amoebots). Each amoebot should store a bit <c>a</c> and a bit
    ///     <c>b</c>. The highest-value 1-bit of <c>a</c> must be marked.
    /// </item>
    /// <item>
    ///     Initialize using the <see cref="Init(bool, bool, bool, bool, Direction, Direction)"/> method.
    ///     You must pass the bits <c>a</c> and <c>b</c>, the start of the chain, the marked MSB of <c>a</c>
    ///     and the two directions. The start should have no predecessor and the end should have no successor.
    /// </item>
    /// <item>
    ///     Create a pin configuration and call <see cref="SetupPinConfig(PinConfiguration)"/>, then
    ///     call <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/> to commit the
    ///     pin configuration changes.
    /// </item>
    /// <item>
    ///     Call <see cref="ActivateSend"/> in the same round to start the procedure.
    /// </item>
    /// <item>
    ///     After this, call <see cref="ActivateReceive"/>, <see cref="SetupPinConfig(PinConfiguration)"/>,
    ///     <see cref="ParticleAlgorithm.SetPlannedPinConfiguration(PinConfiguration)"/> and
    ///     <see cref="ActivateSend"/> in this order in every round.
    /// </item>
    /// <item>
    ///     The procedure can be paused after each <see cref="ActivateReceive"/> call and resumed by
    ///     continuing with <see cref="SetupPinConfig(PinConfiguration)"/> in some future round.
    /// </item>
    /// <item>
    ///     Call <see cref="IsFinished"/> after <see cref="ActivateReceive"/> to check whether the
    ///     multiplication is finished.
    /// </item>
    /// <item>
    ///     You can read the result bit <c>c</c> using <see cref="Bit_C"/>. After each iteration,
    ///     you can also read the bit <c>a</c> and the shifted bit <c>b</c> using
    ///     <see cref="Bit_A"/> and <see cref="Bit_C"/>.
    /// </item>
    /// <item>
    ///     If an overflow has occurred, <see cref="HaveOverflow"/> will return <c>true</c> after
    ///     the procedure has finished.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    // Init:
    //  - Give the bits of a and b
    //  - Set the result bit to 0
    //  - Give the chain's starting point the token
    //  - Mark the MSB of a
    //  - Give the predecessor and successor direction
    // Round 0:
    //  Receive (ONLY AFTER FIRST ITERATION!):
    //  - Receive token beep, update token
    //  Send:
    //  - Establish two full chain circuits
    //  - Beep on the second circuit if we perform another iteration
    //  - Transmit bit of a on the first circuit (only necessary if another iteration should be done)
    // Round 1:
    //  Receive:
    //  - If no beep on circuit 2: Finished
    //  - Otherwise:
    //      - Remember that we have to add in this iteration
    //  Send:
    //  - Setup chain circuit 1 and beep for carry bits
    //  - Setup neighbor circuit 2 and send bits of b for shift
    // Round 2:
    //  Receive:
    //  - Receive carry bits and update sum bits (XOR of b, c and carry)
    //  - Reset add flag
    //  - Receive b bits to shift b
    //  Send:
    //  - Setup neighbor connection circuit 1
    //  - Token amoebot sends beep to successor
    //  - Setup full chain circuit 2
    //      - Send beep if overflow was detected
    public class SubMultiplication : Subroutine
    {
        // This int represents the state of this amoebot
        // Since the standard int type is a 32-bit signed int, we use the
        // 32 bits to encode the entire state:
        // The lowest 2 bits represent the round counter (possible values 0, 1, 2)
        // Bits 2, 3, 4 store the current bits of a, b and c (result) stored in this amoebot
        // Bit 5 is the flag for the token in a
        // Bit 6 is the MSB flag for a
        // Bits 7-9 store the direction of the predecessor (0-5 directions and 6 means no predecessor)
        // Bits 10-12 store the direction of the successor
        // Bit 13 is the termination flag
        // Bit 14 remembers whether the current bit of a is 1 (whether we have to add)
        // Bit 15 indicates whether an overflow has occurred
        // Bit 16 remembers whether we sent a 1-bit of b to a non-existent successor
        // (causes overflow if the multiplication is not complete in the next round)
        //                     16            15         14    13      1210        987         6        5       432   10
        // xxxx xxxx xxxx xxx  x             x          x     x       xxx         xxx         x        x       xxx   xx
        //                     Shift b err   Overflow   Add   Term.   Succ. dir   Pred. dir   MSB a   Token   cba   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_A = 2;
        private const int bit_B = 3;
        private const int bit_C = 4;
        private const int bit_Token = 5;
        private const int bit_MSB_A = 6;
        private const int bit_Finished = 13;
        private const int bit_Add = 14;
        private const int bit_Overflow = 15;
        private const int bit_ShiftError = 16;

        public SubMultiplication(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[Mult] State"), 0);
        }

        /// <summary>
        /// Initializes the subroutine. Must be called by each
        /// amoebot on the chain that stores <c>a</c> and <c>b</c>.
        /// </summary>
        /// <param name="a">This amoebot's bit of <c>a</c>.</param>
        /// <param name="b">This amoebot's bit of <c>b</c>.</param>
        /// <param name="start">Whether this amoebot is the start of <c>a</c>.</param>
        /// <param name="msbA">Whether this amoebot is the highest-value 1-bit of <c>a</c>.</param>
        /// <param name="predDir">The direction of the predecessor. Should be <see cref="Direction.NONE"/>
        /// only at the start of the chain.</param>
        /// <param name="succDir">The direction of the successor. Should be <see cref="Direction.NONE"/>
        /// only at the end of the chain.</param>
        public void Init(bool a, bool b, bool start, bool msbA, Direction predDir, Direction succDir)
        {
            // Encode the starting information in the state
            state.SetValue(
                0 |                     // Round
                (a ? 4 : 0) |           // Bits of a, b and c (c is 0 initially)
                (b ? 8 : 0) |
                (start ? 32 : 0) |      // Token
                (msbA ? 64 : 0) |       // MSB of a
                (predDir != Direction.NONE ? (predDir.ToInt() << 7) : (6 << 7)) |   // Predecessor and successor direction
                (succDir != Direction.NONE ? (succDir.ToInt() << 10) : (6 << 10)));
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to receive the
        /// beeps sent in the last round. Should always be called before
        /// <see cref="SetupPinConfig(PinConfiguration)"/> and <see cref="ActivateSend"/>,
        /// except in the very first activation, where it should not be called.
        /// </summary>
        public void ActivateReceive()
        {
            int round = Round();
            PinConfiguration pc = algo.GetCurrentPinConfiguration();
            if (round == 0)
            {
                // Receive token beep
                Direction predDir = PredDir();
                if (predDir != Direction.NONE && pc.GetPinAt(predDir, 0).PartitionSet.ReceivedBeep())
                {
                    SetStateBit(bit_Token, true);
                }

                // Receive overflow beep
                GetPsetIds(pc, out int pSet1, out int pSet2);
                if (pc.ReceivedBeepOnPartitionSet(pSet2))
                {
                    SetStateBit(bit_Overflow, true);
                }
            }
            else if (round == 1)
            {
                // Check if there was a beep on circuit 2
                GetPsetIds(pc, out int pSet1, out int pSet2);
                if (pc.ReceivedBeepOnPartitionSet(pSet2))
                {
                    // Start the iteration
                    // Check if we have to add
                    if (pc.ReceivedBeepOnPartitionSet(pSet1))
                    {
                        SetStateBit(bit_Add, true);

                        // Check if we have to report an overflow
                        if (GetStateBit(bit_ShiftError))
                        {
                            SetStateBit(bit_Overflow, true);
                        }
                    }
                }
                else
                {
                    // Terminate
                    SetStateBit(bit_Finished, true);
                }
            }
            else if (round == 2)
            {
                // Receive carry bit and bit of b
                bool beepCarry = false;
                bool beepB = false;
                Direction predDir = PredDir();
                if (predDir != Direction.NONE)
                {
                    beepCarry = pc.GetPinAt(predDir, 0).PartitionSet.ReceivedBeep();
                    beepB = pc.GetPinAt(predDir, 1).PartitionSet.ReceivedBeep();

                    if (beepCarry && GetStateBit(bit_B) ^ GetStateBit(bit_C) && SuccDir() == Direction.NONE)
                    {
                        // We received a carry bit and it should be forwarded but we have no successor: Overflow
                        SetStateBit(bit_Overflow, true);
                    }
                }

                // Update sum bit
                if (GetStateBit(bit_Add))
                {
                    SetStateBit(bit_C, GetStateBit(bit_B) ^ GetStateBit(bit_C) ^ beepCarry);
                }

                // Update bit of shifted b
                SetStateBit(bit_B, beepB);

                // Reset add flag
                SetStateBit(bit_Add, false);
            }
        }

        /// <summary>
        /// Sets up the required circuits for the next step in the given
        /// pin configuration. This must be called after <see cref="ActivateReceive"/>
        /// and before <see cref="ActivateSend"/>. The given pin configuration
        /// will not be planned by this method.
        /// </summary>
        /// <param name="pc">The pin configuration to set up. Partition set IDs will
        /// always equal one of the IDs of the contained pins.</param>
        public void SetupPinConfig(PinConfiguration pc)
        {
            int round = Round();
            Direction predDir = PredDir();
            Direction succDir = SuccDir();
            if (round == 0)
            {
                // Establish two full chain circuits
                MakePartitionSets(pc, predDir, succDir, 0, true);
                MakePartitionSets(pc, predDir, succDir, 1, true);
            }
            else if (round == 1)
            {
                // Circuit 1 is chain circuit for carry bits
                // Connect iff the two bits of a and b are different
                MakePartitionSets(pc, predDir, succDir, 0, GetStateBit(bit_B) ^ GetStateBit(bit_C));

                // Circuit 2 is singleton to transmit bits of b
                MakePartitionSets(pc, predDir, succDir, 1, false);
            }
            else if (round == 2)
            {
                // Circuit 1 becomes neighbor circuit (singleton)
                MakePartitionSets(pc, predDir, succDir, 0, false);
                // Circuit 2 becomes full chain for overflow signal
                MakePartitionSets(pc, predDir, succDir, 1, true);
            }
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to send the
        /// beeps required for this step. Must be called after <see cref="ActivateReceive"/>
        /// and <see cref="SetupPinConfig(PinConfiguration)"/> and after the pin configuration
        /// has been planned.
        /// </summary>
        public void ActivateSend()
        {
            int round = Round();
            if (round == 0)
            {
                // If we have the token: Beep on second circuit and send a's bit on first circuit
                if (GetStateBit(bit_Token))
                {
                    PinConfiguration pc = GetPlannedPC();

                    GetPsetIds(pc, out int pSet1, out int pSet2);
                    pc.SendBeepOnPartitionSet(pSet2);
                    if (GetStateBit(bit_A))
                        pc.SendBeepOnPartitionSet(pSet1);
                }
                SetRound(1);
            }
            else if (round == 1)
            {
                PinConfiguration pc = GetPlannedPC();
                Direction succDir = SuccDir();
                if (succDir != Direction.NONE)
                {
                    // Beep for carry bit on circuit 1
                    if (GetStateBit(bit_Add) && GetStateBit(bit_B) && GetStateBit(bit_C))
                    {
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.SendBeep();
                    }

                    // Transmit bit of b
                    if (GetStateBit(bit_B))
                    {
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                    }
                }
                else
                {
                    if (GetStateBit(bit_B))
                    {
                        // We would have to transmit a 1 to a non-existent successor => Prepare overflow
                        SetStateBit(bit_ShiftError, true);

                        // If we send a carry bit to a non-existent successor, we immediately have an overflow
                        if (GetStateBit(bit_C))
                        {
                            SetStateBit(bit_Overflow, true);
                        }
                    }
                }
                SetRound(2);
            }
            else if (round == 2)
            {
                // Amoebot with the token forwards it to the successor, unless it is the MSB
                if (GetStateBit(bit_Token))
                {
                    SetStateBit(bit_Token, false);

                    if (!GetStateBit(bit_MSB_A))
                    {
                        PinConfiguration pc = GetPlannedPC();
                        Direction succDir = SuccDir();
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.SendBeep();
                    }
                }

                // Overflow signal sent on circuit 2
                if (GetStateBit(bit_Overflow))
                {
                    PinConfiguration pc = GetPlannedPC();
                    GetPsetIds(pc, out int pSet1, out int pSet2);
                    pc.SendBeepOnPartitionSet(pSet2);
                }
                SetRound(0);
            }
        }

        /// <summary>
        /// Helper for reading a single bit from the state integer.
        /// </summary>
        /// <param name="bit">The position of the bit.</param>
        /// <returns>The value of the state bit at position <paramref name="bit"/>.</returns>
        private bool GetStateBit(int bit)
        {
            return (state.GetCurrentValue() & (1 << bit)) != 0;
        }

        /// <summary>
        /// Helper for setting a single bit from the state integer.
        /// </summary>
        /// <param name="bit">The position of the bit.</param>
        /// <param name="value">The new value of the bit.</param>
        private void SetStateBit(int bit, bool value)
        {
            state.SetValue(value ? state.GetCurrentValue() | (1 << bit) : state.GetCurrentValue() & ~(1 << bit));
        }

        /// <summary>
        /// Helper for reading the round number from the state integer.
        /// </summary>
        /// <returns>The current round number.</returns>
        private int Round()
        {
            return state.GetCurrentValue() & 3;
        }

        /// <summary>
        /// This amoebot's bit of <c>a</c>.
        /// </summary>
        /// <returns>Whether this amoebot's bit of <c>a</c> is equal to <c>1</c>.</returns>
        public bool Bit_A()
        {
            return GetStateBit(bit_A);
        }

        /// <summary>
        /// This amoebot's bit of <c>b</c>. Note that <c>b</c> will be shifted along the
        /// chain during the procedure, one step per iteration.
        /// </summary>
        /// <returns>Whether this amoebot's bit of <c>b</c> is equal to <c>1</c>.</returns>
        public bool Bit_B()
        {
            return GetStateBit(bit_B);
        }

        /// <summary>
        /// This amoebot's bit of <c>c</c>, the result of multiplying <c>a</c> and <c>b</c>.
        /// </summary>
        /// <returns>Whether this amoebot's bit of <c>c</c> is equal to <c>1</c>.</returns>
        public bool Bit_C()
        {
            return GetStateBit(bit_C);
        }

        /// <summary>
        /// Helper for reading the predecessor direction from the state integer.
        /// </summary>
        /// <returns>The direction of the chain predecessor.</returns>
        private Direction PredDir()
        {
            int d = (state.GetCurrentValue() >> 7) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        /// <summary>
        /// Helper for reading the successor direction from the state integer.
        /// </summary>
        /// <returns>The direction of the chain successor.</returns>
        private Direction SuccDir()
        {
            int d = (state.GetCurrentValue() >> 10) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        /// <summary>
        /// Checks whether the procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>.
        /// </summary>
        /// <returns><c>true</c> if and only if the multiplication procedure
        /// has finished.</returns>
        public bool IsFinished()
        {
            return GetStateBit(bit_Finished);
        }

        /// <summary>
        /// Checks whether an overflow occurred during the multiplication.
        /// Should only be called once the procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if a 1-bit of <c>b</c> was shifted
        /// or a carry bit was sent beyond the end of the chain.</returns>
        public bool HaveOverflow()
        {
            return GetStateBit(bit_Overflow);
        }

        /// <summary>
        /// Helper for setting the round counter.
        /// </summary>
        /// <param name="round">The new value of the round counter.</param>
        private void SetRound(int round)
        {
            state.SetValue((state.GetCurrentValue() & ~3 | round));
        }

        /// <summary>
        /// Helper for getting the partition set IDs of the connected chain circuits.
        /// This is useful at the start and end of the chain, where one of the two
        /// ends is not connected, so we cannot use the pin to find the partition set.
        /// </summary>
        /// <param name="pc">The pin configuration from which to get the partition set IDs.</param>
        /// <param name="pSet1">The ID of the outer circuit's partition set.</param>
        /// <param name="pSet2">The ID of the inner circuit's partition set.</param>
        private void GetPsetIds(PinConfiguration pc, out int pSet1, out int pSet2)
        {
            Direction succDir = SuccDir();
            Direction predDir = PredDir();

            if (succDir != Direction.NONE)
            {
                pSet1 = pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.Id;
                pSet2 = pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.Id;
            }
            else
            {
                pSet1 = pc.GetPinAt(predDir, 0).PartitionSet.Id;
                pSet2 = pc.GetPinAt(predDir, 1).PartitionSet.Id;
            }
        }

        /// <summary>
        /// Sets up the outer or the inner circuit by connecting or
        /// disconnecting the predecessor from the successor.
        /// </summary>
        /// <param name="pc">The pin configuration to be modified.</param>
        /// <param name="predDir">The direction of the chain predecessor.</param>
        /// <param name="succDir">The direction of the chain successor.</param>
        /// <param name="offset">The pin distance from the outer circuit (only 0 and 1 are used).</param>
        /// <param name="connected">Whether the pins of the predecessor and successor should be
        /// connected. There will be no connection if the predecessor or successor direction
        /// is <see cref="Direction.NONE"/>.</param>
        private void MakePartitionSets(PinConfiguration pc, Direction predDir, Direction succDir, int offset, bool connected)
        {
            if (connected)
            {
                List<int> pins = new List<int>();
                if (predDir != Direction.NONE)
                    pins.Add(pc.GetPinAt(predDir, offset).Id);
                if (succDir != Direction.NONE)
                    pins.Add(pc.GetPinAt(succDir, algo.PinsPerEdge - 1 - offset).Id);
                pc.MakePartitionSet(pins.ToArray(), pins[0]);
            }
            else
            {
                if (predDir != Direction.NONE)
                {
                    int id1 = pc.GetPinAt(predDir, offset).Id;
                    pc.MakePartitionSet(new int[] { id1 }, id1);
                }
                if (succDir != Direction.NONE)
                {
                    int id2 = pc.GetPinAt(succDir, algo.PinsPerEdge - 1 - offset).Id;
                    pc.MakePartitionSet(new int[] { id2 }, id2);
                }
            }
        }

        /// <summary>
        /// Wrapper for getting the planned pin configuration and
        /// throwing an exception if there is none.
        /// </summary>
        /// <returns>The currently planned pin configuration.</returns>
        private PinConfiguration GetPlannedPC()
        {
            PinConfiguration pc = algo.GetPlannedPinConfiguration();
            if (pc is null)
            {
                throw new InvalidActionException(particle, "Amoebot has no planned pin configuration");
            }
            return pc;
        }
    }

} // namespace AS2.Subroutines.BinaryOps
