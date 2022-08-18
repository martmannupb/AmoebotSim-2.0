using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValueHistoryBitArray : ValueHistory<BitArray>
{
    public ValueHistoryBitArray(BitArray initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

    // Compares two BitArrays by value instead of just by reference
    protected override bool ValuesEqual(BitArray val1, BitArray val2)
    {
        if (val1 == val2)
        {
            return true;
        }
        else if (val1 == null || val2 == null || val1.Count != val2.Count)
        {
            return false;
        }
        else
        {
            for (int i = 0; i < val1.Count; i++)
            {
                if (val1[i] != val2[i])
                {
                    return false;
                }
            }
        }
        return true;
    }

    public new ValueHistorySaveData<BitArraySaveData> GenerateSaveData()
    {
        ValueHistorySaveData<BitArraySaveData> data = new ValueHistorySaveData<BitArraySaveData>();

        data.values = new List<BitArraySaveData>(values.Count);
        foreach (BitArray ba in values)
            data.values.Add(BitArraySaveData.FromBitArray(ba));
        data.rounds = rounds;
        data.lastRound = lastRound;

        return data;
    }
}
