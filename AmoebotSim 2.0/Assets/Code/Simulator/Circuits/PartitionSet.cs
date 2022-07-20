using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartitionSet : IPartitionSet
{
    public PinConfiguration pinConfig;
    public int id;
    public BitArray pins;
    private int numStoredPins;

    public PartitionSet(PinConfiguration pinConfig, int id, int size)
    {
        this.pinConfig = pinConfig;
        this.id = id;
        this.pins = new BitArray(size);

        numStoredPins = 0;
    }

    // TODO: Maybe the validity check can be removed (this should only be called when the pin really has to be added)
    /// <summary>
    /// Adds the pin with the given ID to this partition set
    /// without causing any further actions.
    /// <para>
    /// Used by <see cref="PinConfiguration"/> to handle pin movements.
    /// </para>
    /// </summary>
    /// <param name="pinId">The ID of the pin to be added.</param>
    public void AddPinBasic(int pinId)
    {
        if (!pins[pinId])
        {
            pins[pinId] = true;
            numStoredPins++;
        }
    }

    // TODO: Maybe the validity check can be removed (this should only be called when the pin really has to be removed)
    /// <summary>
    /// Removes the pin with the given ID from this partition set
    /// without causing any further actions.
    /// <para>
    /// Used by <see cref="PinConfiguration"/> to handle pin movements.
    /// </para>
    /// </summary>
    /// <param name="pinId">The ID of the pin to be removed.</param>
    public void RemovePinBasic(int pinId)
    {
        if (pins[pinId])
        {
            pins[pinId] = false;
            numStoredPins--;
        }
    }

    /// <summary>
    /// Clears the local data of this partition set.
    /// <para>
    /// Note that if this partition set contains any pins, these
    /// pins will keep their references to this partition set.
    /// </para>
    /// </summary>
    public void ClearInternal()
    {
        pins = new BitArray(pins.Count);
        numStoredPins = 0;
    }


    // TODO: May have to put these into compressed storage classes/structs instead
    /**
     * Comparison operators for comparing partition sets and pin configurations easily
     */

    public static bool operator==(PartitionSet p1, PartitionSet p2)
    {
        if (p1 is null && p2 is null)
        {
            return true;
        }
        else if (p1 is null || p2 is null || p1.id != p2.id || p1.numStoredPins != p2.numStoredPins || p1.pins.Count != p2.pins.Count)
        {
            return false;
        }
        for (int i = 0; i < p1.pins.Count; i++)
        {
            if (p1.pins[i] != p2.pins[i])
            {
                return false;
            }
        }
        return true;
    }

    public static bool operator!=(PartitionSet p1, PartitionSet p2)
    {
        if (p1 is null && p2 is null)
        {
            return false;
        }
        else if (p1 is null || p2 is null || p1.id != p2.id || p1.numStoredPins != p2.numStoredPins || p1.pins.Count != p2.pins.Count)
        {
            return true;
        }
        for (int i = 0; i < p1.pins.Count; i++)
        {
            if (p1.pins[i] != p2.pins[i])
            {
                return true;
            }
        }
        return false;
    }

    public override bool Equals(object obj)
    {
        PartitionSet other = obj as PartitionSet;
        if (other is null)
        {
            return false;
        }
        return this == other;
    }

    // TODO: Make sure this is correct if it is used
    public override int GetHashCode()
    {
        return HashCode.Combine(id, pins, numStoredPins);
    }


    /**
     * IPartitionSet: Developer API
     */

    public IPinConfiguration PinConfiguration
    {
        get { return pinConfig; }
    }

    public int Id
    {
        get { return id; }
    }

    public bool IsEmpty()
    {
        return numStoredPins == 0;
    }

    public int[] GetPinIds()
    {
        int[] pinIds = new int[numStoredPins];
        int j = 0;
        for (int i = 0; i < pins.Count; i++)
        {
            if (pins[i])
            {
                pinIds[j] = i;
                j++;
            }
        }
        return pinIds;
    }

    public IPin[] GetPins()
    {
        IPin[] ipins = new IPin[numStoredPins];
        int j = 0;
        for (int i = 0; i < pins.Count; i++)
        {
            if (pins[i])
            {
                ipins[j] = pinConfig.GetPin(i);
                j++;
            }
        }
        return ipins;
    }

    public bool ContainsPin(int pinId)
    {
        return pins[pinId];
    }

    public bool ContainsPin(IPin pin)
    {
        return pins[pin.Id];
    }

    public void AddPin(int idx)
    {
        if (!pins[idx])
        {
            Pin pin = pinConfig.GetPin(idx);
            if (pin.partitionSet != null)
            {
                pin.partitionSet.RemovePinBasic(idx);
            }
            AddPinBasic(idx);
            pin.partitionSet = this;
        }
    }

    public void AddPin(IPin pin)
    {
        AddPin(pin.Id);
    }

    public void AddPins(int[] pinIds)
    {
        foreach (int pinId in pinIds)
        {
            AddPin(pinId);
        }
    }

    public void AddPins(IPin[] pins)
    {
        foreach (IPin pin in pins)
        {
            AddPin(pin.Id);
        }
    }

    public void RemovePin(int idx)
    {
        if (pins[idx])
        {
            pinConfig.TryRemovePin(idx);
        }
    }

    public void RemovePin(IPin pin)
    {
        RemovePin(pin.Id);
    }

    public void RemovePins(int[] pinIds)
    {
        // First collect the pins that can be removed
        List<int> pinsToRemove = new List<int>();
        foreach (int pinId in pinIds)
        {
            if (pins[pinId])
            {
                pinsToRemove.Add(pinId);
            }
        }
        // Now let the pin configuration try to remove them
        // and put them into new partition sets
        if (pinsToRemove.Count > 0)
        {
            pinConfig.TryRemovePins(pinsToRemove.ToArray());
        }
    }

    public void RemovePins(IPin[] pins)
    {
        // First collect the pins that can be removed
        List<int> pinsToRemove = new List<int>();
        foreach (IPin pin in pins)
        {
            if (this.pins[pin.Id])
            {
                pinsToRemove.Add(pin.Id);
            }
        }
        // Now let the pin configuration try to remove them
        // and put them into new partition sets
        if (pinsToRemove.Count > 0)
        {
            pinConfig.TryRemovePins(pinsToRemove.ToArray());
        }
    }

    public void Merge(int otherId)
    {
        if (otherId == Id)
        {
            return;
        }
        PartitionSet other = pinConfig.GetPartitionSetWithId(otherId);
        for (int pinId = 0; pinId < pins.Count; pinId++)
        {
            if (other.pins[pinId])
            {
                other.RemovePinBasic(pinId);
                AddPinBasic(pinId);
                pinConfig.GetPin(pinId).partitionSet = this;
            }
        }
    }

    public void Merge(IPartitionSet other)
    {
        Merge(other.Id);
    }



    // <<<TEMPORARY, FOR DEBUGGING>>>
    public void Print()
    {
        string s = "Partition Set with " + numStoredPins + " pins:\n";
        for (int i = 0; i < pins.Count; i++)
        {
            if (pins[i])
            {
                s += i + " " + pinConfig.GetPin(i).Print() + "\n";
            }
        }
        s += "\n";
        Debug.Log(s);
    }
}
