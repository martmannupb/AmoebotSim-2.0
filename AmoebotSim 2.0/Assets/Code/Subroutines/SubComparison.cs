// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Implements binary comparison for two numbers a, b
    /// stored in the same chain.
    /// <para>
    /// Determines whether a <![CDATA[>]]> b, a <![CDATA[<]]> b
    /// or a = b and makes the result available to all
    /// amoebots on the chain.
    /// </para>
    /// <para>
    /// This procedure requires at least 2 pins and it always uses the
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
    ///     <c>b</c>.
    /// </item>
    /// <item>
    ///     Initialize using the <see cref="Init(bool, bool, Direction, Direction)"/> method.
    ///     You must pass the bits <c>a</c> and <c>b</c> and the two chain directions.
    ///     The chain start should have no predecessor and the end should have no successor.
    /// </item>
    /// <item>
    ///     Call <see cref="SetupPinConfig(PinConfiguration)"/> to modify the pin configuration.
    /// </item>
    /// <item>
    ///     Call <see cref="ActivateSend"/> in the same round to start the procedure.
    /// </item>
    /// <item>
    ///     After this, call <see cref="ActivateReceive"/>, <see cref="SetupPinConfig(PinConfiguration)"/>,
    ///     and <see cref="ActivateSend"/> in this order in every round.
    /// </item>
    /// <item>
    ///     The procedure can be paused after each <see cref="ActivateReceive"/> call and resumed by
    ///     continuing with <see cref="SetupPinConfig(PinConfiguration)"/> in some future round.
    /// </item>
    /// <item>
    ///     Call <see cref="IsFinished"/> after <see cref="ActivateReceive"/> to check whether the
    ///     comparison is finished.
    /// </item>
    /// <item>
    ///     The comparison result is thereafter available through <see cref="Result"/> for each
    ///     amoebot on the chain.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    // Init:
    //  - Give the bits of a and b
    //  - Give the predecessor and successor direction
    // Round 0:
    //  Send:
    //  - Establish chain circuit split at amoebots with unequal bits
    //  - Last amoebot on the counter beeps unless its bits are unequal
    // Round 1:
    //  Receive:
    //  - If we have unequal bits and receive a beep: Determine comparison result by the stored bits
    //  - If we are the chain start and we receive the beep (equal bits): Set comparison result to equal
    //  - If we are the chain end and we have unequal bits: Determine the comparison result
    //  Send:
    //  - Setup full chain circuits 1 and 2
    //  - Amoebot with comparison result sends beep pattern encoding the result
    // Round 2:
    //  Receive:
    //  - Receive comparison result on the two circuits
    public class SubComparison : Subroutine
    {
        public enum ComparisonResult
        {
            NONE = 0,
            EQUAL = 1,
            GREATER = 2,
            LESS = 3
        }

        // This int represents the state of this amoebot
        // Since the standard int type is a 32-bit signed int, we use the
        // 32 bits to encode the entire state:
        // The lowest 2 bits represent the round counter (possible values 0, 1, 2)
        // Bits 2, 3 store the bits of a and b
        // Bits 4-6 store the direction of the predecessor (0-5 directions and 6 means no predecessor)
        // Bits 7-9 store the direction of the successor
        // Bit 10 is the termination flag
        // Bits 11, 12 store the comparison result (<, >, =, NONE)
        //                         1211      10      987         654         32   10
        // xxxx xxxx xxxx xxxx xxx  xx       x       xxx         xxx         xx   xx
        //                          Result   Term.   Succ. dir   Pred. dir   ba   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_A = 2;
        private const int bit_B = 3;
        private const int bit_Finished = 10;

        public SubComparison(Particle p, ParticleAttribute<int> stateAttr = null) : base(p)
        {
            if (stateAttr is null)
                state = algo.CreateAttributeInt(FindValidAttributeName("[Comp] State"), 0);
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
                (a ? 4 : 0) |           // Bits of a and b
                (b ? 8 : 0) |
                (predDir != Direction.NONE ? (predDir.ToInt() << 4) : (6 << 4)) |   // Predecessor and successor direction
                (succDir != Direction.NONE ? (succDir.ToInt() << 7) : (6 << 7)) |
                ((int)ComparisonResult.NONE << 11)  // Initial comparison result
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
            PinConfiguration pc = algo.GetCurrPinConfiguration();
            Direction predDir = PredDir();
            Direction succDir = SuccDir();
            int pSet1 = BinOpUtils.GetChainPSetID(pc, predDir, succDir, 0, algo.PinsPerEdge);
            int pSet2 = BinOpUtils.GetChainPSetID(pc, predDir, succDir, 1, algo.PinsPerEdge);
            if (round == 1)
            {
                // Determine comparison result if we can
                bool a = GetStateBit(bit_A);
                bool b = GetStateBit(bit_B);
                if (a ^ b)
                {
                    if (succDir == Direction.NONE)
                    {
                        // We determine the result (end of the chain)
                        SetResult(a ? ComparisonResult.GREATER : ComparisonResult.LESS);
                    }
                    else
                    {
                        // We only determine the result if we received the beep
                        if (algo.ReceivedBeepOnPartitionSet(pSet1))
                        {
                            SetResult(a ? ComparisonResult.GREATER : ComparisonResult.LESS);
                        }
                    }
                }
                else
                {
                    // We determine the result only if we are the start of the chain
                    if (predDir == Direction.NONE && (succDir == Direction.NONE || algo.ReceivedBeepOnPartitionSet(pSet1)))
                    {
                        SetResult(ComparisonResult.EQUAL);
                    }
                }
            }
            else if (round == 2)
            {
                // Receive comparison result from circuits
                bool c1 = algo.ReceivedBeepOnPartitionSet(pSet1);
                bool c2 = algo.ReceivedBeepOnPartitionSet(pSet2);
                // The last case should never happen
                ComparisonResult result = c1 && c2 ? ComparisonResult.EQUAL : (c1 ? ComparisonResult.GREATER : c2 ? ComparisonResult.LESS : ComparisonResult.NONE);
                SetResult(result);
                SetStateBit(bit_Finished, true);
            }
        }

        /// <summary>
        /// Sets up the required circuits for the next step in the given
        /// pin configuration. This must be called after <see cref="ActivateReceive"/>
        /// and before <see cref="ActivateSend"/>.
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
                // Establish comparison circuit 1: Split for unequal bits
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, GetStateBit(bit_A) == GetStateBit(bit_B));
            }
            else if (round == 1)
            {
                // Full chain circuits 1 and 2
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, true);
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 1, algo.PinsPerEdge, true);
            }
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to send the
        /// beeps required for this step. Must be called after <see cref="ActivateReceive"/>
        /// and <see cref="SetupPinConfig(PinConfiguration)"/>.
        /// </summary>
        public void ActivateSend()
        {
            int round = Round();
            PinConfiguration pc = algo.GetNextPinConfiguration();
            if (round == 0)
            {
                // Chain end sends beep to predecessor if bits are equal
                Direction predDir = PredDir();
                if (SuccDir() == Direction.NONE && predDir != Direction.NONE && GetStateBit(bit_A) == GetStateBit(bit_B))
                {
                    pc.GetPinAt(predDir, 0).SendBeep();
                }
                SetRound(1);
            }
            else if (round == 1)
            {
                // If we know the result, we send the beep
                ComparisonResult result = Result();
                if (result != ComparisonResult.NONE)
                {
                    Direction predDir = PredDir();
                    Direction succDir = SuccDir();
                    int pSet1 = BinOpUtils.GetChainPSetID(pc, predDir, succDir, 0, algo.PinsPerEdge);
                    int pSet2 = BinOpUtils.GetChainPSetID(pc, predDir, succDir, 1, algo.PinsPerEdge);
                    // a is greater: Circuit 1
                    // b is greater: Circuit 2
                    // a = b: Both circuits
                    if (result == ComparisonResult.GREATER || result == ComparisonResult.EQUAL)
                        algo.SendBeepOnPartitionSet(pSet1);
                    if (result == ComparisonResult.LESS || result == ComparisonResult.EQUAL)
                        algo.SendBeepOnPartitionSet(pSet2);
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
        /// Helper for reading the predecessor direction from the state integer.
        /// </summary>
        /// <returns>The direction of the chain predecessor.</returns>
        private Direction PredDir()
        {
            int d = (state.GetCurrentValue() >> 4) & 7;
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
            int d = (state.GetCurrentValue() >> 7) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        /// <summary>
        /// Checks whether the procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>.
        /// </summary>
        /// <returns><c>true</c> if and only if the comparison procedure
        /// has finished.</returns>
        public bool IsFinished()
        {
            return GetStateBit(bit_Finished);
        }

        /// <summary>
        /// Returns the result of the comparison after the procedure
        /// has finished.
        /// </summary>
        /// <returns>The result of comparing <c>a</c> to <c>b</c>.</returns>
        public ComparisonResult Result()
        {
            int r = (state.GetCurrentValue() >> 11) & 3;
            return (ComparisonResult)r;
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
        /// Helper for setting the comparison result.
        /// </summary>
        /// <param name="result">The new value of the result.</param>
        private void SetResult(ComparisonResult result)
        {
            int r = (int)result;
            state.SetValue(state.GetCurrentValue() & ~(3 << 11) | (r << 11));
        }
    }

} // namespace AS2.Subroutines.BinaryOps
