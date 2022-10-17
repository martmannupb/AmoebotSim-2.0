//using System.Collections;
//using System.Collections.Generic;

///// <summary>
///// Specialized value history that stores <see cref="BitArray"/> data and
///// compares bit arrays by their bit values.
///// </summary>
//public class ValueHistoryBitArray : ValueHistory<BitArray>
//{
//    public ValueHistoryBitArray(BitArray initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

//    // Compares two BitArrays by value instead of just by reference
//    protected override bool ValuesEqual(BitArray val1, BitArray val2)
//    {
//        if (val1 == val2)
//        {
//            return true;
//        }
//        else if (val1 == null || val2 == null || val1.Count != val2.Count)
//        {
//            return false;
//        }
//        else
//        {
//            for (int i = 0; i < val1.Count; i++)
//            {
//                if (val1[i] != val2[i])
//                {
//                    return false;
//                }
//            }
//        }
//        return true;
//    }

//    /**
//     * Saving and loading functionality.
//     */

//    /// <summary>
//    /// Overrides <see cref="ValueHistory{T}.GenerateSaveData"/>, returning
//    /// a serializable value history of <see cref="BitArraySaveData"/>.
//    /// </summary>
//    /// <returns>A serializable representation of the bit array history data.</returns>
//    public new ValueHistorySaveData<BitArraySaveData> GenerateSaveData()
//    {
//        ValueHistorySaveData<BitArraySaveData> data = new ValueHistorySaveData<BitArraySaveData>();

//        data.values = new List<BitArraySaveData>(values.Count);
//        foreach (BitArray ba in values)
//            data.values.Add(BitArraySaveData.FromBitArray(ba));
//        data.rounds = rounds;
//        data.lastRound = lastRound;

//        return data;
//    }

//    /// <summary>
//    /// Same as <see cref="ValueHistory{T}.ValueHistory(ValueHistorySaveData{T})"/> but
//    /// specialized for <see cref="BitArraySaveData"/> history data.
//    /// </summary>
//    /// <param name="data">The serializable history data from which to restore the
//    /// <see cref="ValueHistoryBitArray"/> instance.</param>
//    public ValueHistoryBitArray(ValueHistorySaveData<BitArraySaveData> data)
//    {
//        values = new List<BitArray>(data.values.Count);
//        foreach (BitArraySaveData bData in data.values)
//        {
//            values.Add(bData.ToBitArray());
//        }
//        rounds = data.rounds;
//        lastRound = data.lastRound;

//        // Start in tracking state
//        markedRound = lastRound;
//        markedIndex = rounds.Count - 1;
//        isTracking = true;
//    }
//}
