using System;
using System.Collections;

namespace AS2
{


    /// <summary>
    /// Container for serializing BitArray objects.
    /// <para>
    /// Bits of the given <see cref="BitArray"/> are stored
    /// in a <c>bool[]</c>.
    /// </para>
    /// </summary>
    [Serializable]
    public class BitArraySaveData
    {
        /// <summary>
        /// The <see cref="BitArray"/>'s bit values.
        /// </summary>
        public bool[] bits;

        /// <summary>
        /// Creates a serializable representation of the given
        /// <see cref="BitArray"/>. Note that <c>null</c> values
        /// are handled like <see cref="BitArray"/>s of size 0 since the
        /// JSON format does not allow null values.
        /// </summary>
        /// <param name="ba">The <see cref="BitArray"/> to be stored.</param>
        /// <returns>A <see cref="BitArraySaveData"/> object storing the
        /// bits of <paramref name="ba"/> or an empty instance if
        /// <paramref name="ba"/> is <c>null</c>.</returns>
        public static BitArraySaveData FromBitArray(BitArray ba)
        {
            BitArraySaveData data = new BitArraySaveData();
            if (ba is null)
            {
                data.bits = new bool[0];
                return data;
            }
            data.bits = new bool[ba.Count];
            ba.CopyTo(data.bits, 0);
            return data;
        }

        /// <summary>
        /// Recovers a <see cref="BitArray"/> instance from the data
        /// stored in this object. Note that if the original
        /// <see cref="BitArray"/> this object was created from was
        /// <c>null</c>, this will return a <see cref="BitArray"/>
        /// instance of length 0.
        /// </summary>
        /// <returns>A <see cref="BitArray"/> with the bit values
        /// stored in this object.</returns>
        public BitArray ToBitArray()
        {
            return new BitArray(bits);
        }
    }

} // namespace AS2
