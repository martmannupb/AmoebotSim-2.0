using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartitionSet
{
    private PinConfiguration pinConfig;
    private int id;
    private BitArray pins;

    public PartitionSet(PinConfiguration pinConfig, int id, int size)
    {
        this.pinConfig = pinConfig;
        this.id = id;
        this.pins = new BitArray(size);
    }

    public void AddPin(int idx)
    {
        pins[idx] = true;
    }

    public void RemovePin(int idx)
    {
        pins[idx] = false;
    }

    public bool HasPin(int idx)
    {
        return pins[idx];
    }
}
