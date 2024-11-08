using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Implements binary division for two numbers a, b
    /// stored in the same chain.
    /// <para>
    /// Computes two new binary numbers <c>c := a / b</c> and <c>d := a mod b</c>. It is
    /// required that <c>a >= b</c> and <c>b > 0</c> holds when the subroutine starts.
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
    ///     Initialize using the <see cref="Init(bool, bool, bool, Direction, Direction)"/> method.
    ///     You must pass the bits <c>a</c> and <c>b</c>, the marked MSB of <c>a</c>
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
    ///     division is finished.
    /// </item>
    /// <item>
    ///     You can read the quotient bit <c>c</c> using <see cref="Bit_C"/> and the remainder bit
    ///     <c>a</c> using <see cref="Bit_A"/>. After each iteration, you can also read the current
    ///     bit <c>a</c> and the shifted bit <c>b</c> using <see cref="Bit_A"/> and <see cref="Bit_C"/>.
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
    //
    // Phase 1: Align the MSBs of the two numbers by shifting b
    // Round 0:
    //  Send:
    //  - Setup full chain circuit 1 and neighbor circuit 2
    //  - If MSB of a holds a 1-bit for b, send beep on circuit 1
    //  - Transmit bit of b to successor on circuit 2
    //  - Go to round 1
    //
    // Round 1:
    //  Receive:
    //  - If beep on circuit 1 is received: Go to next phase (go to round 3)
    //  - Otherwise: Receive b bit from predecessor
    //  Send:
    //  - Setup neighbor circuit 2
    //  - Send start token to successor
    //  - Go to round 2
    //
    // Round 2:
    //  Receive:
    //  - Receive start token from predecessor
    //  - Go back to round 0
    //
    // Round 3:
    //  Send:
    //  - Setup full chain circuit 1
    //  - Amoebot with token sends beep
    //  - Go to round 4
    //
    //
    // Phase 2: Compute quotient and remainder
    // Round 4:
    //  Receive:
    //  - If beep on circuit 1:
    //      - Proceed with next iteration
    //  - Otherwise:
    //      - Terminate here
    //  Send:
    //  - Setup comparison circuit 1: Equal bits connect, unequal bits do not connect
    //  - MSB of a sends beep to predecessor unless its own bits are unequal
    //
    // Round 5:
    //  Receive:
    //  - First amoebot with unequal bits will receive beep from successor
    //      - Special case: Start amoebot receives beep or MSB of a has unequal bits
    //  - Some amoebot now knows the comparison result (=, < or >) and stores it
    //  Send:
    //  - Setup full chain circuit 1 and beep if comparison result is a >= b
    //  - Setup subtraction circuit 2: Amoebots with a != b split and other amoebots connect
    //      - Beep for successor if a = 0 and b = 1
    //
    // Round 6:
    //  Receive:
    //  - If beep on circuit 1: Have to perform subtraction
    //      - Update bit of a to a XOR b XOR carry
    //      - Marker on c records a 1
    //  - No beep: Marker on c records a 0
    //  Send:
    //  - Setup neighbor circuits 1 and 2
    //  - Send token to predecessor on circuit 1
    //  - Send b bit to predecessor on circuit 2
    //
    // Round 7:
    //  Receive:
    //  - Receive token on circuit 1
    //  - Receive b bit on circuit 2
    //  Send:
    //  - Setup full chain circuit 1
    //  - Amoebot with token sends beep if we have to continue
    public class SubDivision : Subroutine
    {
        // This int represents the state of this amoebot
        // Since the standard int type is a 32-bit signed int, we use the
        // 32 bits to encode the entire state:
        // The lowest 3 bits represent the round counter (possible values 0-7)
        // Bits 3, 4, 5 store the current bits of a, b and c (result) stored in this amoebot
        // Bit 6 is the flag for the token in a
        // Bit 7 is the MSB flag for a (it does not move during the procedure, even though a gets smaller)
        // Bits 8-10 store the direction of the predecessor (0-5 directions and 6 means no predecessor)
        // Bits 11-13 store the direction of the successor
        // Bit 14 is the termination flag
        // Bit 15 remembers whether we have to perform a subtraction / whether a >= b
        //                      15    14      1311        108         7       6       543   210
        // xxxx xxxx xxxx xxxx  x     x       xxx         xxx         x       x       xxx   xxx
        //                      Sub   Term.   Succ. dir   Pred. dir   MSB a   Token   cba   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_A = 3;
        private const int bit_B = 4;
        private const int bit_C = 5;
        private const int bit_Token = 6;
        private const int bit_MSB_A = 7;
        private const int bit_Finished = 14;
        private const int bit_Sub = 15;

        public SubDivision(Particle p, ParticleAttribute<int> stateAttr = null) : base(p)
        {
            if (stateAttr is null)
                state = algo.CreateAttributeInt(FindValidAttributeName("[Div] State"), 0);
            else
                state = stateAttr;
        }

        /// <summary>
        /// Initializes the subroutine. Must be called by each
        /// amoebot on the chain that stores <c>a</c> and <c>b</c>.
        /// </summary>
        /// <param name="a">This amoebot's bit of <c>a</c>.</param>
        /// <param name="b">This amoebot's bit of <c>b</c>.</param>
        /// <param name="msbA">Whether this amoebot is the highest-value 1-bit of <c>a</c>.</param>
        /// <param name="predDir">The direction of the predecessor. Should be <see cref="Direction.NONE"/>
        /// only at the start of the chain.</param>
        /// <param name="succDir">The direction of the successor. Should be <see cref="Direction.NONE"/>
        /// only at the end of the chain.</param>
        public void Init(bool a, bool b, bool msbA, Direction predDir, Direction succDir)
        {
            // Encode the starting information in the state
            state.SetValue(
                0 |                     // Round
                (a ? 8 : 0) |           // Bits of a, b and c (c is 0 initially)
                (b ? 16 : 0) |
                (predDir == Direction.NONE ? 64 : 0) |  // Token
                (msbA ? 128 : 0) |                      // MSB of a
                (predDir != Direction.NONE ? (predDir.ToInt() << 8) : (6 << 8)) |   // Predecessor and successor direction
                (succDir != Direction.NONE ? (succDir.ToInt() << 11) : (6 << 11)));
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
                // No beep on circuit 1 means that we have to shift b and the token, staying in phase 1
                if (!pc.ReceivedBeepOnPartitionSet(pSet1))
                {
                    SetStateBit(bit_B, predDir != Direction.NONE && pc.GetPinAt(predDir, 1).PartitionSet.ReceivedBeep());
                }
                // Beep means we move on to the next phase
                else
                {
                    SetRound(3);
                }
            }
            else if (round == 2)
            {
                // Receive start token from predecessor on circuit 2
                if (predDir != Direction.NONE && pc.GetPinAt(predDir, 1).PartitionSet.ReceivedBeep())
                {
                    SetStateBit(bit_Token, true);
                }

                // Go for another shift
                SetRound(0);
            }
            else if (round == 4)
            {
                // Check for beep on full circuit 1
                // No beep: Terminate
                if (!pc.ReceivedBeepOnPartitionSet(pSet1))
                {
                    SetStateBit(bit_Finished, true);
                }
            }
            else if (round == 5)
            {
                // If our bits are different: We decide comparison result if we received a beep
                if (GetStateBit(bit_A) != GetStateBit(bit_B))
                {
                    if (succDir != Direction.NONE && pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                    {
                        SetStateBit(bit_Sub, GetStateBit(bit_A));
                    }
                }
                // Special case: Bits are equal, we are the start amoebot and receive the beep
                // => a = b, we do have to subtract
                else
                {
                    if (predDir == Direction.NONE && succDir != Direction.NONE && pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                    {
                        SetStateBit(bit_Sub, true);
                    }
                }
            }
            else if (round == 6)
            {
                // If beep on circuit 1: Update a by subtraction
                if (pc.ReceivedBeepOnPartitionSet(pSet1))
                {
                    bool carry = predDir != Direction.NONE && pc.GetPinAt(predDir, 1).PartitionSet.ReceivedBeep();
                    SetStateBit(bit_A, GetStateBit(bit_A) ^ GetStateBit(bit_B) ^ carry);
                    if (GetStateBit(bit_Token))
                    {
                        SetStateBit(bit_C, true);
                    }
                }
                else
                {
                    if (GetStateBit(bit_Token))
                    {
                        SetStateBit(bit_C, false);
                    }
                }
            }
            else if (round == 7)
            {
                // Receive token on circuit 1
                if (succDir != Direction.NONE && pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep())
                {
                    SetStateBit(bit_Token, true);
                }

                // Receive b bit on circuit 2
                SetStateBit(bit_B, succDir != Direction.NONE && pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.ReceivedBeep());
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
                // Full chain circuit 1
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, true);
                // Neighbor circuit 2
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 1, algo.PinsPerEdge, false);
            }
            else if (round == 1)
            {
                // Neighbor circuit 2
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 1, algo.PinsPerEdge, false);
            }
            else if (round == 3)
            {
                // Full chain circuit 1
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, true);
            }
            else if (round == 4)
            {
                // Comparison circuit 1: Connect equal bits
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, GetStateBit(bit_A) == GetStateBit(bit_B));
            }
            else if (round == 5)
            {
                // Full chain circuit 1
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, true);
                // Subtraction circuit 2: Connect equal bits
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 1, algo.PinsPerEdge, GetStateBit(bit_A) == GetStateBit(bit_B));
            }
            else if (round == 6)
            {
                // Neighbor circuits 1 and 2
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, false);
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 1, algo.PinsPerEdge, false);
            }
            else if (round == 7)
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
            Direction predDir = PredDir();
            Direction succDir = SuccDir();
            int pSet1 = BinOpUtils.GetChainPSetID(pc, predDir, succDir, 0, algo.PinsPerEdge);
            if (round == 0)
            {
                // Only do something if our b bit is 1
                if (GetStateBit(bit_B))
                {
                    // If we are the MSB of a: Beep on circuit 1
                    if (GetStateBit(bit_MSB_A))
                    {
                        pc.SendBeepOnPartitionSet(pSet1);
                    }

                    // Send b bit to successor
                    if (succDir != Direction.NONE)
                    {
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                    }
                }
                SetRound(1);
            }
            else if (round == 1)
            {
                // Amoebot with token sends beep to successor
                if (GetStateBit(bit_Token))
                {
                    if (succDir != Direction.NONE)
                    {
                        pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                    }
                    SetStateBit(bit_Token, false);
                }
                SetRound(2);
            }
            else if (round == 3)
            {
                // Amoebot with token sends beep
                if (GetStateBit(bit_Token))
                {
                    pc.SendBeepOnPartitionSet(pSet1);
                }
                SetRound(4);
            }
            else if (round == 4)
            {
                // MSB of a sends beep to predecessor unless its own bits are unequal
                if (GetStateBit(bit_MSB_A))
                {
                    if (GetStateBit(bit_A) != GetStateBit(bit_B))
                    {
                        // Decide comparison result here already
                        SetStateBit(bit_Sub, GetStateBit(bit_A));
                    }
                    else
                    {
                        // Send beep to predecessor
                        if (predDir != Direction.NONE)
                        {
                            pc.GetPinAt(predDir, 0).PartitionSet.SendBeep();
                        }
                    }
                }
                SetRound(5);
            }
            else if (round == 5)
            {
                // Beep on circuit 1 if comparison result implies we have to subtract
                if (GetStateBit(bit_Sub))
                {
                    pc.SendBeepOnPartitionSet(pSet1);
                    // Reset this flag
                    SetStateBit(bit_Sub, false);
                }

                // Beep for successor on circuit 2 if a = 0 and b = 1
                if (succDir != Direction.NONE && !GetStateBit(bit_A) && GetStateBit(bit_B))
                {
                    pc.GetPinAt(succDir, algo.PinsPerEdge - 2).PartitionSet.SendBeep();
                }
                SetRound(6);
            }
            else if (round == 6)
            {
                // Send token to predecessor on circuit 1
                if (GetStateBit(bit_Token))
                {
                    SetStateBit(bit_Token, false);
                    if (predDir != Direction.NONE)
                    {
                        pc.GetPinAt(predDir, 0).PartitionSet.SendBeep();
                    }
                }

                // Send b bit to predecessor on circuit 2
                if (GetStateBit(bit_B) && predDir != Direction.NONE)
                {
                    pc.GetPinAt(predDir, 1).PartitionSet.SendBeep();
                }
                SetRound(7);
            }
            else if (round == 7)
            {
                // Amoebot with token sends beep on circuit 1 if we have to continue
                if (GetStateBit(bit_Token))
                {
                    pc.SendBeepOnPartitionSet(pSet1);
                }
                SetRound(4);
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
            return state.GetCurrentValue() & 7;
        }

        /// <summary>
        /// This amoebot's bit of <c>a</c>. During the procedure,
        /// <c>a</c> will be reduced until it contains the remainder of dividing
        /// the initial <c>a</c> by <c>b</c>.
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
        /// This amoebot's bit of <c>c</c>, the result of dividing <c>a</c> by <c>b</c>.
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
            int d = (state.GetCurrentValue() >> 8) & 7;
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
            int d = (state.GetCurrentValue() >> 11) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        /// <summary>
        /// Checks whether the procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>.
        /// </summary>
        /// <returns><c>true</c> if and only if the division procedure
        /// has finished.</returns>
        public bool IsFinished()
        {
            return GetStateBit(bit_Finished);
        }

        /// <summary>
        /// Helper for setting the round counter.
        /// </summary>
        /// <param name="round">The new value of the round counter.</param>
        private void SetRound(int round)
        {
            state.SetValue((state.GetCurrentValue() & ~7 | round));
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
