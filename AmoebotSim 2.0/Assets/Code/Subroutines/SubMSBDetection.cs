using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Implements a simple procedure to find the MSB of a binary counter.
    /// <para>
    /// Determines the highest-value 1-bit of a binary counter, if there is
    /// one, and identifies the chain's start as the MSB otherwise.
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
    ///     Initialize using the <see cref="Init(bool, Direction, Direction)"/> method.
    ///     You must pass the bit <c>a</c> and the two chain directions.
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
    ///     Call <see cref="IsFinished"/> after <see cref="ActivateReceive"/> to check whether the
    ///     procedure is finished. The result is thereafter available through <see cref="IsMSB"/>
    ///     for each amoebot on the chain.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    // Init:
    //  - Give the bit of a
    //  - Give the predecessor and successor direction
    // Round 0:
    //  Send:
    //  - Establish chain circuit 1, splitting at unequal bits
    //  - End of the chain sends beep to predecessor unless it is the start or has a 1-bit
    // Round 1:
    //  Receive:
    //  - Some amoebot receives the beep and identifies as MSB or the chain end becomes the MSB
    public class SubMSBDetection : Subroutine
    {

        // This int represents the state of this amoebot
        // Since the standard int type is a 32-bit signed int, we use the
        // 32 bits to encode the entire state:
        // The lowest bit represents the round counter (possible values 0, 1)
        // Bit 1 stores the bit of a
        // Bits 2-4 store the direction of the predecessor (0-5 directions and 6 means no predecessor)
        // Bits 5-7 store the direction of the successor
        // Bit 8 is the termination flag
        // Bit 9 is the MSB flag
        //                              9     8       765         432         1   0
        // xxxx xxxx xxxx xxxx xxxx xx  x     x       xxx         xxx         x   x
        //                              MSB   Term.   Succ. dir   Pred. dir   a   Round
        ParticleAttribute<int> state;

        // Bit index constants
        private const int bit_A = 1;
        private const int bit_Finished = 8;
        private const int bit_MSB = 9;

        public SubMSBDetection(Particle p, ParticleAttribute<int> stateAttr = null) : base(p)
        {
            if (stateAttr is null)
                state = algo.CreateAttributeInt(FindValidAttributeName("[MSB] State"), 0);
            else
                state = stateAttr;
        }

        /// <summary>
        /// Initializes the subroutine. Must be called by each
        /// amoebot on the chain that stores <c>a</c> and <c>b</c>.
        /// </summary>
        /// <param name="a">This amoebot's bit of <c>a</c>.</param>
        /// <param name="predDir">The direction of the predecessor. Should be <see cref="Direction.NONE"/>
        /// only at the start of the chain.</param>
        /// <param name="succDir">The direction of the successor. Should be <see cref="Direction.NONE"/>
        /// only at the end of the chain.</param>
        public void Init(bool a, Direction predDir, Direction succDir)
        {
            // Encode the starting information in the state
            state.SetValue(
                0 |                     // Round
                (a ? 2 : 0) |           // Bit of a
                (predDir != Direction.NONE ? (predDir.ToInt() << 2) : (6 << 2)) |   // Predecessor and successor direction
                (succDir != Direction.NONE ? (succDir.ToInt() << 5) : (6 << 5))
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
            if (round == 1)
            {
                // Receive bit from successor or become MSB otherwise
                bool beep = succDir != Direction.NONE && pc.GetPinAt(succDir, algo.PinsPerEdge - 1).PartitionSet.ReceivedBeep();
                bool bit1 = GetStateBit(bit_A);
                SetStateBit(bit_MSB, bit1 && (beep || succDir == Direction.NONE) || !bit1 && beep && predDir == Direction.NONE);
                SetStateBit(bit_Finished, true);
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
                // Establish MSB circuit 1: Split for 1-bits
                BinOpUtils.MakeChainCircuit(pc, predDir, succDir, 0, algo.PinsPerEdge, !GetStateBit(bit_A));
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
            if (round == 0)
            {
                // Chain end sends beep to predecessor unless it has a 1-bit or is the start
                if (succDir == Direction.NONE)
                {
                    if (!GetStateBit(bit_A) && predDir != Direction.NONE)
                    {
                        pc.GetPinAt(predDir, 0).PartitionSet.SendBeep();
                    }
                }
                SetRound(1);
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
            return state.GetCurrentValue() & 1;
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
        /// Helper for reading the predecessor direction from the state integer.
        /// </summary>
        /// <returns>The direction of the chain predecessor.</returns>
        private Direction PredDir()
        {
            int d = (state.GetCurrentValue() >> 2) & 7;
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
            int d = (state.GetCurrentValue() >> 5) & 7;
            if (d == 6)
                return Direction.NONE;
            else
                return DirectionHelpers.Cardinal(d);
        }

        /// <summary>
        /// Checks whether the MSB procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>.
        /// </summary>
        /// <returns><c>true</c> if and only if the MSB procedure
        /// has finished.</returns>
        public bool IsFinished()
        {
            return GetStateBit(bit_Finished);
        }

        /// <summary>
        /// Checks whether this amoebot is the MSB.
        /// Should only be called once the procedure has finished.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot stores
        /// the highest-value 1-bit or it is the start and the stored
        /// number is 0.</returns>
        public bool IsMSB()
        {
            return GetStateBit(bit_MSB);
        }

        /// <summary>
        /// Helper for setting the round counter.
        /// </summary>
        /// <param name="round">The new value of the round counter.</param>
        private void SetRound(int round)
        {
            state.SetValue((state.GetCurrentValue() & ~1 | round));
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
