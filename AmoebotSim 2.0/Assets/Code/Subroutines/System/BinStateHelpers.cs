using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.BinStateHelpers
{

    /// <summary>
    /// Lightweight attribute wrapper representing a value that is stored
    /// in a single integer attribute, encoded in binary using a subset of bits.
    /// A standard <c>int</c> has 32 bits, which can be used to encode multiple
    /// different values, especially since amoebot memory is limited and will
    /// never use integer values this large.
    /// </summary>
    /// <typeparam name="T">The type of value represented by the attribute.</typeparam>
    public abstract class BinAttribute<T>
    {
        public readonly ParticleAttribute<int> attr;
        public readonly int idx;

        /// <summary>
        /// Creates a new attribute wrapper referencing the
        /// given state attribute int.
        /// </summary>
        /// <param name="attr">The attribute storing the integer that contains
        /// this value as a subset of bits.</param>
        /// <param name="idx">The bit index at which the encoding of this
        /// value starts.</param>
        public BinAttribute(ParticleAttribute<int> attr, int idx)
        {
            this.attr = attr;
            this.idx = idx;
        }

        /// <summary>
        /// Returns the value from the beginning of the current round.
        /// </summary>
        /// <returns>The value decoded from the int's snapshot value.</returns>
        public abstract T GetValue();

        /// <summary>
        /// Returns the latest value.
        /// </summary>
        /// <returns>The value decoded from the int's latest value.</returns>
        public abstract T GetCurrentValue();

        /// <summary>
        /// Writes a new value to the state integer.
        /// </summary>
        /// <param name="value">The new value to be written.</param>
        public abstract void SetValue(T value);
    }

    /// <summary>
    /// Binary encoded attribute for bool values. Each bool
    /// is represented by a single bit.
    /// </summary>
    public class BinAttributeBool : BinAttribute<bool>
    {
        /// <inheritdoc/>
        public BinAttributeBool(ParticleAttribute<int> attr, int idx) : base(attr, idx) { }

        /// <inheritdoc/>
        public override bool GetValue()
        {
            return ((attr.GetValue() >> idx) & 1) > 0;
        }

        /// <inheritdoc/>
        public override bool GetCurrentValue()
        {
            return ((attr.GetCurrentValue() >> idx) & 1) > 0;
        }

        /// <inheritdoc/>
        public override void SetValue(bool value)
        {
            attr.SetValue((attr.GetCurrentValue() & ~(1 << idx)) | ((value ? 1 : 0) << idx));
        }
    }

    /// <summary>
    /// Binary encoded attribute for <see cref="Direction"/> values.
    /// A direction is encoded in 3 bits and the bits <c>000</c> represent
    /// <see cref="Direction.NONE"/>. Only cardinal directions are encoded.
    /// </summary>
    public class BinAttributeDirection : BinAttribute<Direction>
    {
        protected const int bit_mask = 7;

        /// <inheritdoc/>
        public BinAttributeDirection(ParticleAttribute<int> attr, int idx) : base(attr, idx) { }

        /// <inheritdoc/>
        public override Direction GetValue()
        {
            return DirectionHelpers.Cardinal(((attr.GetValue() >> idx) & bit_mask) - 1);
        }

        /// <inheritdoc/>
        public override Direction GetCurrentValue()
        {
            return DirectionHelpers.Cardinal(((attr.GetCurrentValue() >> idx) & bit_mask) - 1);
        }

        /// <inheritdoc/>
        public override void SetValue(Direction dir)
        {
            attr.SetValue((attr.GetCurrentValue() & ~(bit_mask << idx)) | ((dir.ToInt() + 1) << idx));
        }
    }

    /// <summary>
    /// A binary encoded attribute for integers using less space
    /// than a full int.
    /// </summary>
    public class BinAttributeInt : BinAttribute<int>
    {
        protected int bit_mask;

        /// <summary>
        /// <inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)"/>
        /// </summary>
        /// <param name="attr"><inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)" path="/param[@name='attr']"/></param>
        /// <param name="idx"><inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)" path="/param[@name='idx']"/></param>
        /// <param name="width">The number of bits occupied by the integer.</param>
        public BinAttributeInt(ParticleAttribute<int> attr, int idx, int width) : base(attr, idx)
        {
            int b = 1;
            bit_mask = 0;
            for (int i = 0; i < width; i++)
            {
                bit_mask |= b;
                b <<= 1;
            }
        }

        /// <inheritdoc/>
        public override int GetValue()
        {
            return (attr.GetValue() >> idx) & bit_mask;
        }

        /// <inheritdoc/>
        public override int GetCurrentValue()
        {
            return (attr.GetCurrentValue() >> idx) & bit_mask;
        }

        /// <inheritdoc/>
        public override void SetValue(int value)
        {
            attr.SetValue((attr.GetCurrentValue() & ~(bit_mask << idx)) | (value << idx));
        }
    }

    /// <summary>
    /// A binary encoded attribute for arbitrary enums. Make sure to
    /// provide enough bits to encode the enum as an integer. Note that
    /// the enum value represented by <c>0</c> will be the default value.
    /// </summary>
    public class BinAttributeEnum<T> : BinAttribute<T> where T : System.Enum
    {
        protected int bit_mask;

        /// <summary>
        /// <inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)"/>
        /// </summary>
        /// <param name="attr"><inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)" path="/param[@name='attr']"/></param>
        /// <param name="idx"><inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)" path="/param[@name='idx']"/></param>
        /// <param name="width">The number of bits required to encode the enum value.</param>
        public BinAttributeEnum(ParticleAttribute<int> attr, int idx, int width) : base(attr, idx)
        {
            int b = 1;
            bit_mask = 0;
            for (int i = 0; i < width; i++)
            {
                bit_mask |= b;
                b <<= 1;
            }
        }

        /// <inheritdoc/>
        public override T GetValue()
        {
            int value = (attr.GetValue() >> idx) & bit_mask;
            return (T)System.Enum.ToObject(typeof(T), value);
        }

        /// <inheritdoc/>
        public override T GetCurrentValue()
        {
            int value = (attr.GetCurrentValue() >> idx) & bit_mask;
            return (T)System.Enum.ToObject(typeof(T), value);
        }

        /// <inheritdoc/>
        public override void SetValue(T value)
        {
            attr.SetValue((attr.GetCurrentValue() & ~(bit_mask << idx)) | (System.Convert.ToInt32(value) << idx));
        }
    }

    /// <summary>
    /// Binary encoded attribute for arrays of up to 32 bool values.
    /// Each bool is represented by a single bit. If the given start index
    /// is greater than 0, the maximum number of bits is reduced
    /// </summary>
    public class BinAttributeBitField : BinAttribute<bool>
    {
        public readonly int length;
        protected int bit_mask;

        /// <summary>
        /// <inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)"/>
        /// </summary>
        /// <param name="attr"><inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)" path="/param[@name='attr']"/></param>
        /// <param name="idx"><inheritdoc cref="BinAttribute{T}.BinAttribute(ParticleAttribute{int}, int)" path="/param[@name='idx']"/></param>
        /// <param name="length">The number of bits in the array.</param>
        public BinAttributeBitField(ParticleAttribute<int> attr, int idx, int length) : base(attr, idx)
        {
            this.length = length;
            int b = 1;
            bit_mask = 0;
            for (int i = 0; i < length; i++)
            {
                bit_mask |= b;
                b <<= 1;
            }
        }

        /// <summary>
        /// Returns the value from the beginning of the
        /// current round.
        /// </summary>
        /// <param name="index">The index of the bit to return.</param>
        /// <returns>The bit value at index <paramref name="index"/>
        /// decoded from the int's snapshot value.</returns>
        public bool GetValue(int index)
        {
            return ((attr.GetValue() >> (idx + index)) & 1) > 0;
        }

        /// <summary>
        /// Returns the latest value.
        /// </summary>
        /// <returns>The value at index <paramref name="index"/>
        /// decoded from the int's latest value.</returns>
        /// <param name="index">The index of the bit to return.</param>
        public bool GetCurrentValue(int index)
        {
            return ((attr.GetCurrentValue() >> (idx + index)) & 1) > 0;
        }

        /// <summary>
        /// Writes a new value to the state integer.
        /// </summary>
        /// <param name="index">The index of the bit to be written.</param>
        /// <param name="value">The new value to be written.</param>
        public void SetValue(int index, bool value)
        {
            attr.SetValue((attr.GetCurrentValue() & ~(1 << (idx + index))) | ((value ? 1 : 0) << (idx + index)));
        }

        /// <summary>
        /// Returns the logical OR of all stored bits for the value
        /// at the beginning of the round.
        /// </summary>
        /// <returns><c>true</c> if and only if any of the stored bits
        /// are <c>true</c> at the beginning of the round.</returns>
        public bool GetOr()
        {
            return ((attr.GetValue() >> idx) & bit_mask) > 0;
        }

        /// <summary>
        /// Returns the logical OR of all latest stored bits.
        /// </summary>
        /// <returns><c>true</c> if and only if any of the latest
        /// stored bits are <c>true</c>.</returns>
        public bool GetCurrentOr()
        {
            return ((attr.GetCurrentValue() >> idx) & bit_mask) > 0;
        }

        /// <summary>
        /// <inheritdoc/>
        /// Uses the default index 0.
        /// </summary>
        public override bool GetValue()
        {
            return GetValue(0);
        }

        /// <summary>
        /// <inheritdoc/>
        /// Uses the default index 0.
        /// </summary>
        public override bool GetCurrentValue()
        {
            return GetCurrentValue(0);
        }

        /// <summary>
        /// <inheritdoc/>
        /// Uses the default index 0.
        /// </summary>
        /// <param name="value"><inheritdoc/></param>
        public override void SetValue(bool value)
        {
            SetValue(0, value);
        }
    }

} // namespace AS2.Subroutines.BinStateHelpers
