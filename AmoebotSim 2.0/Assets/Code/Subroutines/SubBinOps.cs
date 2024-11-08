using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinaryOps
{
    /// <summary>
    /// Wrapper for several operations on a chain storing one or two binary counters.
    /// <para>
    /// Implements MSB detection, comparison, addition, subtraction, multiplication
    /// and division with remainder. Only one of these operations can be performed
    /// at a time. This wrapper is implemented such that all subroutines share their
    /// state attribute, making it very memory efficient.
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
    ///     <c>b</c> (if an operation for two counters is required).
    /// </item>
    /// <item>
    ///     Initialize using the <see cref="Init(SubBinOps.Mode, bool, Direction, Direction, bool, bool)"/> method.
    ///     You must pass the mode of operation, the bit <c>a</c> and the two chain directions, plus the bit <c>b</c>
    ///     and the marked MSB of <c>a</c> for some operations.
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
    /// 
    /// 
    /// <item>
    ///     Call <see cref="IsFinished"/> after <see cref="ActivateReceive"/> to check whether the
    ///     procedure is finished. The result is thereafter available through one of the interface
    ///     methods, such as <see cref="ResultBit"/>.
    ///     If the operation has an optional overflow check (addition/subtraction) that you want
    ///     to use, call <see cref="IsFinishedOverflow"/> instead.
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    public class SubBinOps : Subroutine
    {
        public enum Mode
        {
            MSB, COMP, ADD, SUB, MULT, DIV
        }

        // This int represents the state of the selected subroutine
        ParticleAttribute<int> state;
        // The currently selected routine
        ParticleAttribute<Mode> mode;

        // Subroutines
        SubMSBDetection msb;
        SubComparison comp;
        SubAddition add;
        SubSubtraction sub;
        SubMultiplication mult;
        SubDivision div;

        public SubBinOps(Particle p) : base(p)
        {
            state = algo.CreateAttributeInt(FindValidAttributeName("[BinOps] State"), 0);
            mode = algo.CreateAttributeEnum<Mode>(FindValidAttributeName("[BinOps] Mode"), Mode.MSB);

            msb = new SubMSBDetection(p, state);
            comp = new SubComparison(p, state);
            add = new SubAddition(p, state);
            sub = new SubSubtraction(p, state);
            mult = new SubMultiplication(p, state);
            div = new SubDivision(p, state);
        }

        /// <summary>
        /// Initializes the subroutine. Must be called by each
        /// amoebot on the chain.
        /// </summary>
        /// <param name="mode">The operation to be initialized.</param>
        /// <param name="a">This amoebot's bit of <c>a</c>.</param>
        /// <param name="predDir">The direction of the predecessor. Should be <see cref="Direction.NONE"/>
        /// only at the start of the chain.</param>
        /// <param name="succDir">The direction of the successor. Should be <see cref="Direction.NONE"/>
        /// only at the end of the chain.</param>
        /// <param name="b">The amoebot's bit of <c>b</c> if a second binary number is involved.</param>
        /// <param name="msbA">Whether this amoebot stores the MSB of <c>a</c> (required for multiplication
        /// and division).</param>
        public void Init(Mode mode, bool a, Direction predDir, Direction succDir, bool b = false, bool msbA = false)
        {
            this.mode.SetValue(mode);

            if (mode == Mode.MSB)
                msb.Init(a, predDir, succDir);
            else if (mode == Mode.COMP)
                comp.Init(a, b, predDir, succDir);
            else if (mode == Mode.ADD)
                add.Init(a, b, predDir, succDir);
            else if (mode == Mode.SUB)
                sub.Init(a, b, predDir, succDir);
            else if (mode == Mode.MULT)
                mult.Init(a, b, msbA, predDir, succDir);
            else if (mode == Mode.DIV)
                div.Init(a, b, msbA, predDir, succDir);
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to receive the
        /// beeps sent in the last round. Should always be called before
        /// <see cref="SetupPinConfig(PinConfiguration)"/> and <see cref="ActivateSend"/>,
        /// except in the very first activation, where it should not be called.
        /// </summary>
        public void ActivateReceive()
        {
            Mode m = mode.GetCurrentValue();
            if (m == Mode.MSB)
                msb.ActivateReceive();
            else if (m == Mode.COMP)
                comp.ActivateReceive();
            else if (m == Mode.ADD)
                add.ActivateReceive();
            else if (m == Mode.SUB)
                sub.ActivateReceive();
            else if (m == Mode.MULT)
                mult.ActivateReceive();
            else if (m == Mode.DIV)
                div.ActivateReceive();
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
            Mode m = mode.GetCurrentValue();
            if (m == Mode.MSB)
                msb.SetupPinConfig(pc);
            else if (m == Mode.COMP)
                comp.SetupPinConfig(pc);
            else if (m == Mode.ADD)
                add.SetupPinConfig(pc);
            else if (m == Mode.SUB)
                sub.SetupPinConfig(pc);
            else if (m == Mode.MULT)
                mult.SetupPinConfig(pc);
            else if (m == Mode.DIV)
                div.SetupPinConfig(pc);
        }

        /// <summary>
        /// Activation during <see cref="ParticleAlgorithm.ActivateBeep"/> to send the
        /// beeps required for this step. Must be called after <see cref="ActivateReceive"/>
        /// and <see cref="SetupPinConfig(PinConfiguration)"/> and after the pin configuration
        /// has been planned.
        /// </summary>
        public void ActivateSend()
        {
            Mode m = mode.GetCurrentValue();
            if (m == Mode.MSB)
                msb.ActivateSend();
            else if (m == Mode.COMP)
                comp.ActivateSend();
            else if (m == Mode.ADD)
                add.ActivateSend();
            else if (m == Mode.SUB)
                sub.ActivateSend();
            else if (m == Mode.MULT)
                mult.ActivateSend();
            else if (m == Mode.DIV)
                div.ActivateSend();
        }

        /// <summary>
        /// Checks whether the current procedure is finished. Should be called after
        /// <see cref="ActivateReceive"/>. Note that for addition and subtraction,
        /// this becomes <c>true</c> before the overflow has been determined. Call
        /// <see cref="IsFinishedOverflow"/> to check whether the procedure has
        /// finished completely.
        /// </summary>
        /// <returns><c>true</c> if and only if the computation procedure
        /// has finished.</returns>
        public bool IsFinished()
        {
            Mode m = mode.GetCurrentValue();
            if (m == Mode.MSB)
                return msb.IsFinished();
            else if (m == Mode.COMP)
                return comp.IsFinished();
            else if (m == Mode.ADD)
                return add.IsFinishedAdd();
            else if (m == Mode.SUB)
                return sub.IsFinishedSub();
            else if (m == Mode.MULT)
                return mult.IsFinished();
            else if (m == Mode.DIV)
                return div.IsFinished();
            return true;
        }

        /// <summary>
        /// Checks whether the current addition or subtraction procedure has
        /// determined its overflow flag. This is similar to
        /// <see cref="IsFinished"/> but it will become <c>true</c> later.
        /// </summary>
        /// <returns><c>true</c> if and only if the current procedure
        /// is addition or subtraction and it has already computed its overflow
        /// result.</returns>
        public bool IsFinishedOverflow()
        {
            Mode m = mode.GetCurrentValue();
            if (m == Mode.ADD)
                return add.IsFinishedOverflow();
            else if (m == Mode.SUB)
                return sub.IsFinishedOverflow();
            return false;
        }

        /// <summary>
        /// Returns the resulting bit for mathematical operations.
        /// For division, this returns the bit of the quotient.
        /// Should only be called after <see cref="IsFinished"/>
        /// returns <c>true</c>.
        /// </summary>
        /// <returns>This amoebot's bit of the resulting number.</returns>
        public bool ResultBit()
        {
            Mode m = mode.GetCurrentValue();
            if (m == Mode.ADD)
                return add.Bit_C();
            else if (m == Mode.SUB)
                return sub.Bit_C();
            else if (m == Mode.MULT)
                return mult.Bit_C();
            else if (m == Mode.DIV)
                return div.Bit_C();
            return false;
        }

        /// <summary>
        /// Returns the remainder bit resulting from division.
        /// Should only be called after <see cref="IsFinished"/>
        /// returns <c>true</c>.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot's
        /// bit of the remainder is 1.</returns>
        public bool RemainderBit()
        {
            if (mode.GetCurrentValue() == Mode.DIV)
                return div.Bit_A();
            return false;
        }

        /// <summary>
        /// Checks whether the operation created an overflow
        /// (only works for addition, subtraction and multiplication).
        /// Should only be called after <see cref="IsFinished"/>
        /// returns <c>true</c>.
        /// </summary>
        /// <returns></returns>
        public bool HaveOverflow()
        {
            Mode m = mode.GetCurrentValue();
            if (m == Mode.ADD)
                return add.HaveOverflow();
            else if (m == Mode.SUB)
                return sub.HaveOverflow();
            else if (m == Mode.MULT)
                return mult.HaveOverflow();
            return false;
        }

        /// <summary>
        /// Returns the result of the comparison procedure.
        /// Should only be called after <see cref="IsFinished"/>
        /// returns <c>true</c>.
        /// </summary>
        /// <returns>The result of comparing <c>a</c> to <c>b</c>.</returns>
        public SubComparison.ComparisonResult CompResult()
        {
            if (mode.GetCurrentValue() == Mode.COMP)
                return comp.Result();
            return SubComparison.ComparisonResult.NONE;
        }

        /// <summary>
        /// Checks whether this amoebot is the MSB.
        /// Should only be called after <see cref="IsFinished"/>
        /// returns <c>true</c>.
        /// </summary>
        /// <returns><c>true</c> if and only if this amoebot stores
        /// the highest-value 1-bit of <c>a</c> or it is the start and
        /// the stored number is 0.</returns>
        public bool IsMSB()
        {
            return mode.GetCurrentValue() == Mode.MSB && msb.IsFinished() && msb.IsMSB();
        }
    }

} // namespace AS2.Subroutines.BinaryOps
