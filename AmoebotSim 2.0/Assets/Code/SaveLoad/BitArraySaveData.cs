using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Container for serializing BitArray objects.
/// </summary>
[Serializable]
public class BitArraySaveData
{
    public bool[] bits;

    public static BitArraySaveData FromBitArray(BitArray ba)
    {
        BitArraySaveData data = new BitArraySaveData();
        data.bits = new bool[ba.Count];
        ba.CopyTo(data.bits, 0);
        return data;
    }
}
