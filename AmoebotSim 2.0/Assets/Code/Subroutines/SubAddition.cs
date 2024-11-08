using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Implements binary addition for two numbers a, b
    /// stored in the same chain.
    /// <para>
    /// Computes a new binary number <c>c := a + b</c> and optionally determines
    /// whether the chain length was insufficient to compute all bits of <c>c</c>.
    /// </para>
    /// <para>
    /// This procedure requires at least 1 pin and it always uses the
    /// "outermost / leftmost" pin when traversing the chain. If an amoebot
    /// occurs on the chain multiple times, its predecessor and successor directions
    /// must be different for all occurrences.
    /// </para>
    /// <para>
    /// <b>Usage</b>:
    /// <list type="bullet">
    /// <item>
    ///     Establish a chain of amoebots such that each amoebot knows its predecessor and successor
    ///     (except the start and end amoebots). Each amoebot should store a bit <c>a</c> and a bit
    ///     <c>b</c>.
    /// </item>
    /// <item>
    ///     Initialize using the <see cref="Init(bool, bool, Direction, Direction)"/> method.
    ///     You must pass the bits <c>a</c> and <c>b</c> and the two chain directions.
    ///     The chain start should have no predecessor and the end should have no successor.
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
    ///     Call <see cref="IsFinishedAdd"/> after <see cref="ActivateReceive"/> to check whether the
    ///     addition is finished. Running another iteration after this, until <see cref="IsFinishedOverflow"/>
    ///     returns <c>true</c>, will make the overflow result available.
    /// </item>
    /// <item>
    ///     The addition result <c>c</c> is thereafter available through <see cref="Bit_C"/>
    ///     for each amoebot on the chain.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    // Init:
    //  - Give the bits of a and b
    //  - Give the predecessor and successor direction
    // Round 0:
    //  Send:
    //  - Establish addition chain circuit 1: Split where bits are equal
    //  - Amoebots with bits 1, 1 send beep to successor
    // Round 1:
    //  Receive:
    //  - Receive carry bit and compute addition result as a ^ b ^ carry
    //  Send:
    //  - Setup full chain circuit 1
    //  - Amoebot at the end of the chain beeps if it had to forward a carry to non-existent successor
    //      - It can determine this by its own bits a, b, c
    // Round 2:
    //  Receive:
    //  - Receive overflow bit on first circuit
    public class SubAddition : Subroutine
    {

        // This int represents the state of this amoebot
        // Since the standard int type is a 32-bit signed int, we use the
        // 32 bits to encode the entire state:
        // The lowest 2 bits represent the round counter (possible values 0, 1, 2)
        // Bits 2, 3, 4 store the bits of a, b and c
        // Bits 5-7 store the direction of the predecessor (0-5 directions and 6 means no predecessor)
        // Bits 8-10 store the direction of the successor
        // Bits 11 and 12 are the termination flags for the addition and the overflow check
        // Bit 13 stores the overflow result
        //                         13         12              11        1098         765         432   10
        // xxxx xxxx xxxx xxxx xx  x          x               x          xxx         xxx         xxx   xx
        //                         Overflow   Overflow done   Add done   Succ. dir   Pred. dir   cba   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_A = 2;
        private const int bit_B = 3;
        private const int bit_C = 4;
        private const int bit_FinishedAdd = 11;
        private const int bit_FinishedOverflow = 12;
        private const int bit_Overflow = 13;

        public SubAddition(Particle p, ParticleAttribute<int> stateAttr = null) : base(p)
        {
            if (stateAttr is null)
                state = algo.CreateAttributeInt(FindValidAttributeName("[Add] State"), 0);
            else
                state = stateAttr;
        }

        /// <summary>
        /// Initializes the subroutine. Must be called by each
        /// amoebot on the chain that stores <c>a</c> and <c>b</c>.
        /// </summary>
        /// <param name="a">This amoebot's bit of <c>a</c>.</param>
        /// <param name="b">This amoebot's bit of <c>b</c>.</param>
        /// <param name="predDir">The direction of the predecessor. Should be <see cref="Direction.NONE"/>
        /// only at the start of the chain.</param>
        /// <param name="succDir">The direction of the successor. Should be <see cref="Direction.NONE"/>
        /// only at the end of the chain.</param>
        public void Init(bool a, bool b, Direction predDir, Direction succDir)
        {
            // Encode the starting information in the state
            state.SetValue(
                0 |                     // Round
                (a ? 4 : 0) |           // Bits of a, b and c (c is initially 0)
                (b ? 8 : 0) |
                (predDir != Direction.NONE ? (predDir.ToInt() << 5) : (6 << 5)) |   // Predecessor and successor direction
                (succDir != Direction.NONE ? (succDir.ToInt() << 8) : (6 << 8))
                );
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
            Direction predDir = PredDir();
            Direction succDir = SuccDir();
            int pSet1 = BinOpUtils.GetChainPSetID(pc, predDir, succDir, 0, algo.PinsPerEdge);
            if (round == 1)
            {
                // Receive carry and compute result bit
                bool carry = predDir != Direction.NONE && pc.GetPinAt(predDir, 0).PartitionSet.ReceivedBeep();
                SetStateBit(bit_C, GetStateBit(bit_A) ^ GetStateBit(bit_B) ^ carry);
                SetStateBit(bit_FinishedAdd, true);
            }
            else if (round == 2)
            {
                // Receive overflow result
                if (pSet1 != -1 && pc.ReceivedBeepOnPartitionSet(pSet1))
                {
                    SetStateBit(bit_Overflow, true);
                }
                SetStateBit(bit_FinishedOverflow, true);
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
                // Establish addition circuit 1: Split for equal bits
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, GetStateBit(bit_A) ^ GetStateBit(bit_B));
            }
            else if (round == 1)
            {
                // Full chain circuit 1
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, true);
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
            PinConfiguration pc = GetPlannedPC();
            Direction succDir = SuccDir();
            if (round == 0)
            {
                // Send beep to successor if both bits are 1
                if (GetStateBit(bit_A) && GetStateBit(bit_B) && succDir != Direction.NONE)
                {
                    pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.SendBeep();
                }
                SetRound(1);
            }
            else if (round == 1)
            {
                // End of the chain sends beep if it has sent a carry
                if (succDir == Direction.NONE)
                {
                    // Our bits and addition result tell us what happened
                    bool a = GetStateBit(bit_A);
                    bool b = GetStateBit(bit_B);
                    bool c = GetStateBit(bit_C);
                    // Both bits are 1 (we sent the carry) or a != b and c was flipped by carry
                    if (a && b || ((a ^ b) && !c))
                    {
                        SetStateBit(bit_Overflow, true);
                        int pSet1 = BinOpUtils.GetChainPSetID(pc, PredDir(), succDir, 0, algo.PinsPerEdge);
                        if (pSet1 != -1)
                        {
                            pc.SendBeepOnPartitionSet(pSet1);
                        }
                    }
                }
                SetRound(2);
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
        /// This amoebot's bit of <c>b</c>.
        /// </summary>
        /// <returns>Whether this amoebot's bit of <c>b</c> is equal to <c>1</c>.</returns>
        public bool Bit_B()
        {
            return GetStateBit(bit_B);
        }

        /// <summary>
        /// This amoebot's bit of <c>c</c>.
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
            int d = (state.GetCurrentValue() >> 5) & 7;
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
            int d = (state.GetCurrentValue() >> 8) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        /// <summary>
        /// Checks whether the addition procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>. If an overflow should be detected, another
        /// iteration has to be run.
        /// </summary>
        /// <returns><c>true</c> if and only if the addition procedure
        /// has finished.</returns>
        public bool IsFinishedAdd()
        {
            return GetStateBit(bit_FinishedAdd);
        }

        /// <summary>
        /// Checks whether the procedure including overflow detection is finished.
        /// Should be called after <see cref="ActivateReceive"/>.
        /// </summary>
        /// <returns><c>true</c> if and only if the overflow detection procedure
        /// has finished.</returns>
        public bool IsFinishedOverflow()
        {
            return GetStateBit(bit_FinishedOverflow);
        }

        /// <summary>
        /// Checks whether an overflow occurred during the addition.
        /// Should only be called once the procedure has finished completely.
        /// </summary>
        /// <returns><c>true</c> if and only if a carry bit was sent beyond the
        /// end of the chain.</returns>
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
