using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinConfiguration : IPinConfiguration
{
    public Particle particle;
    private int pinsPerEdge;
    private int headDirection;
    private int numPins;

    public Pin[] pins;
    public PartitionSet[] partitionSets;

    public PinConfiguration(Particle particle, int pinsPerEdge, int headDirection = -1)
    {
        this.particle = particle;
        this.pinsPerEdge = pinsPerEdge;
        this.headDirection = headDirection;

        numPins = headDirection == -1 ? (6 * pinsPerEdge) : (10 * pinsPerEdge);

        partitionSets = new PartitionSet[numPins];
        pins = new Pin[numPins];

        // Initialize partition sets and pins
        // Default is singleton: Each pin is its own partition set (for now)
        if (headDirection == -1)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                for (int idx = 0; idx < pinsPerEdge; idx++)
                {
                    int id = direction * pinsPerEdge + idx;
                    PartitionSet ps = new PartitionSet(this, id, numPins);
                    Pin pin = new Pin(ps, id, direction, true, idx);
                    ps.AddPinBasic(id);
                    partitionSets[id] = ps;
                    pins[id] = pin;
                }
            }
        }
        else
        {
            for (int label = 0; label < 10; label++)
            {
                int direction = ParticleSystem_Utils.GetDirOfLabel(label, headDirection);
                bool isHead = ParticleSystem_Utils.IsHeadLabel(label, headDirection);
                for (int idx = 0; idx < pinsPerEdge; idx++)
                {
                    int id = label * pinsPerEdge + idx;
                    PartitionSet ps = new PartitionSet(this, id, numPins);
                    Pin pin = new Pin(ps, id, direction, isHead, idx);
                    ps.AddPinBasic(id);
                    partitionSets[id] = ps;
                    pins[id] = pin;
                }
            }
        }
    }

    /// <summary>
    /// Computes the ID of the pin on the specified edge with the
    /// given offset.
    /// <para>
    /// The formula for the pin ID is <c>label * pinsPerEdge +
    /// <paramref name="offset"/></c>, where <c>label</c> is computed
    /// using <paramref name="direction"/> and <paramref name="head"/>.
    /// </para>
    /// </summary>
    /// <param name="direction">The local direction of the edge.</param>
    /// <param name="offset">The edge offset of the pin.</param>
    /// <param name="head">If the pin configuration represents the
    /// expanded state, this flag indicates whether the edge belongs to
    /// the particle's head or not.</param>
    /// <returns>The ID of the pin in the location specified by an edge
    /// and an edge offset.</returns>
    public int GetPinId(int direction, int offset, bool head = true)
    {
        return ParticleSystem_Utils.GetLabelInDir(direction, headDirection, head) * pinsPerEdge + offset;
    }

    /// <summary>
    /// Returns the pin with the given ID.
    /// </summary>
    /// <param name="pinId">The ID of the pin to return.</param>
    /// <returns>The pin with ID <paramref name="pinId"/>.</returns>
    public Pin GetPin(int pinId)
    {
        return pins[pinId];
    }

    /// <summary>
    /// Returns the partition set with the given ID.
    /// <para>
    /// This is the system-side version of <see cref="GetPartitionSet(int)"/>,
    /// which is part of the algorithm developer API.
    /// </para>
    /// </summary>
    /// <param name="partitionSetId">The ID of the partition set to return.</param>
    /// <returns>The partition set with ID <paramref name="partitionSetId"/>.</returns>
    public PartitionSet GetPartitionSetWithId(int partitionSetId)
    {
        return partitionSets[partitionSetId];
    }

    /// <summary>
    /// Tries to remove the specified pin from its
    /// partition set and insert it into an empty
    /// partition set.
    /// <para>
    /// Throws a <see cref="System.InvalidOperationException"/> if
    /// no empty partition set can be found.
    /// </para>
    /// </summary>
    /// <param name="pinId">The ID of the pin to be
    /// removed from its partition set.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the pin is the only one in this partition set and
    /// no other partition set is empty.
    /// </exception>
    public void TryRemovePin(int pinId)
    {
        // Find empty partition set
        for (int i = 0; i < numPins; i++)
        {
            PartitionSet ps = partitionSets[i];
            if (ps.IsEmpty())
            {
                ps.AddPin(pinId);
                return;
            }
        }
        throw new System.InvalidOperationException("Pin with ID " + pinId + " cannot be removed from its partition set: No other partition set is empty.");
    }

    /// <summary>
    /// Tries to remove the specified pins from their
    /// partition set and insert them into empty
    /// partition sets.
    /// <para>
    /// It is assumed that the pins should all be removed
    /// from the same partition set.
    /// </para>
    /// <para>
    /// Throws a <see cref="System.InvalidOperationException"/> if at
    /// any point, no empty partition set can be found.
    /// </para>
    /// </summary>
    /// <param name="pinIds">The IDs of the pins to be removed from
    /// their partition set. Must all be from the same partition set.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if one of the pins is the last one in the partition set
    /// and no other partition set is empty.
    /// </exception>
    public void TryRemovePins(int[] pinIds)
    {
        // Try to find an empty partition set for each pin
        int i = 0;
        foreach (int pinId in pinIds)
        {
            bool foundSet = false;
            for (; i < numPins; i++)
            {
                PartitionSet ps = partitionSets[i];
                if (ps.IsEmpty())
                {
                    ps.AddPin(pinId);
                    foundSet = true;
                    break;
                }
            }
            if (!foundSet)
            {
                throw new System.InvalidOperationException("Pin with ID " + pinId + " cannot be removed from its partition set: No other partition set is empty.");
            }
        }
    }

    // TODO: May have to put these into compressed storage classes/structs instead
    /**
     * Comparison operators for comparing pin configurations easily
     */

    public static bool operator==(PinConfiguration pc1, PinConfiguration pc2)
    {
        if (pc1 is null && pc2 is null)
        {
            return true;
        }
        else if (pc1 is null || pc2 is null || pc1.headDirection != pc2.headDirection || pc1.pinsPerEdge != pc2.pinsPerEdge)
        {
            return false;
        }
        for (int i = 0; i < pc1.numPins; i++)
        {
            if (pc1.partitionSets[i] != pc2.partitionSets[i])
            {
                return false;
            }
        }
        return true;
    }

    public static bool operator!=(PinConfiguration pc1, PinConfiguration pc2)
    {
        if (pc1 is null && pc2 is null)
        {
            return false;
        }
        else if (pc1 is null || pc2 is null || pc1.headDirection != pc2.headDirection || pc1.pinsPerEdge != pc2.pinsPerEdge)
        {
            return true;
        }
        for (int i = 0; i < pc1.numPins; i++)
        {
            if (pc1.partitionSets[i] != pc2.partitionSets[i])
            {
                return true;
            }
        }
        return false;
    }

    public override bool Equals(object obj)
    {
        return obj is PinConfiguration other && this == other;
    }

    // TODO: Make sure this is correct if it is used
    public override int GetHashCode()
    {
        return System.HashCode.Combine(pinsPerEdge, headDirection, partitionSets);
    }


    /**
     * IPinConfiguration: Developer API
     */

    public int HeadDirection
    {
        get { return headDirection; }
    }

    public int PinsPerEdge
    {
        get { return pinsPerEdge; }
    }

    public int NumPins
    {
        get { return numPins; }
    }

    public IPin GetPinAt(int direction, int offset, bool head = true)
    {
        int pinId = GetPinId(direction, offset, head);
        return pins[pinId];
    }

    public IPin[] GetPinsAtEdge(int direction, bool head = true)
    {
        IPin[] ipins = new IPin[pinsPerEdge];
        int pinId = GetPinId(direction, 0, head);
        for (int i = 0; i < pinsPerEdge; i++)
        {
            ipins[i] = pins[pinId];
            pinId++;
        }
        return ipins;
    }

    public IPartitionSet GetPartitionSet(int index)
    {
        return partitionSets[index];
    }

    public IPartitionSet[] GetPartitionSets()
    {
        return partitionSets;
    }

    public IPartitionSet[] GetNonEmptyPartitionSets()
    {
        List<PartitionSet> nonEmptySets = new List<PartitionSet>();
        foreach (PartitionSet ps in partitionSets)
        {
            if (!ps.IsEmpty())
            {
                nonEmptySets.Add(ps);
            }
        }
        return nonEmptySets.ToArray();
    }

    public void SetToSingleton()
    {
        for (int id = 0; id < numPins; id++)
        {
            PartitionSet ps = partitionSets[id];
            Pin pin = pins[id];
            ps.ClearInternal();
            ps.AddPinBasic(id);
            pin.partitionSet = ps;
        }
    }

    public void SetToGlobal(int partitionSetId = 0)
    {
        PartitionSet psGlobal = partitionSets[partitionSetId];
        psGlobal.ClearInternal();
        for (int id = 0; id < numPins; id++)
        {
            PartitionSet ps = partitionSets[id];
            Pin pin = pins[id];
            if (id != partitionSetId)
            {
                ps.ClearInternal();
            }
            psGlobal.AddPinBasic(id);
            pin.partitionSet = psGlobal;
        }
    }

    public void SetToGlobal(IPartitionSet partitionSet)
    {
        SetToGlobal(partitionSet.Id);
    }

    public void SetStarConfig(int offset, int partitionSetIndex)
    {
        SetStarConfig(offset, new bool[headDirection == -1 ? 6 : 10], partitionSetIndex);
    }

    public void SetStarConfig(int offset, IPartitionSet partitionSet)
    {
        SetStarConfig(offset, new bool[headDirection == -1 ? 6 : 10], partitionSet.Id);
    }

    public void SetStarConfig(int offset, bool[] inverted, int partitionSetIndex)
    {
        // First add the correct pins, then remove the ones that have to be removed
        // This order avoids exceptions in all cases
        PartitionSet ps = partitionSets[partitionSetIndex];
        List<int> pinsToRemove = new List<int>();
        int numLabels = headDirection == -1 ? 6 : 10;
        for (int label = 0; label < numLabels; label++)
        {
            for (int os = 0; os < pinsPerEdge; os++)
            {
                int pinId = label * pinsPerEdge + os;
                if ((!inverted[label] && os == offset) || (inverted[label] && os == pinsPerEdge - 1 - offset))
                {
                    ps.AddPin(pinId);
                }
                else if (ps.ContainsPin(pinId))
                {
                    pinsToRemove.Add(pinId);
                }
            }
        }
        TryRemovePins(pinsToRemove.ToArray());
    }

    public void SetStarConfig(int offset, bool[] inverted, IPartitionSet partitionSet)
    {
        SetStarConfig(offset, inverted, partitionSet.Id);
    }

    public void MakePartitionSet(int[] pinIds, int partitionSetIndex)
    {
        // First add the correct pins, then remove the ones that have to be removed
        // This order avoids exceptions in all cases except the one where an error
        // is unavoidable
        PartitionSet ps = partitionSets[partitionSetIndex];
        List<int> pinsToRemove = new List<int>();
        System.Array.Sort(pinIds);
        int pinIdx = 0;
        int numLabels = headDirection == -1 ? 6 : 10;
        for (int label = 0; label < numLabels; label++)
        {
            for (int os = 0; os < pinsPerEdge; os++)
            {
                int pinId = label * pinsPerEdge + os;
                if (pinIdx < pinIds.Length && pinIds[pinIdx] == pinId)
                {
                    ps.AddPin(pinId);
                    pinIdx++;
                }
                else if (ps.ContainsPin(pinId))
                {
                    pinsToRemove.Add(pinId);
                }
            }
        }
        TryRemovePins(pinsToRemove.ToArray());
    }

    public void MakePartitionSet(IPin[] pins, int partitionSetIndex)
    {
        int[] pinIds = new int[pins.Length];
        for (int i = 0; i < pins.Length; i++)
        {
            pinIds[i] = pins[i].Id;
        }
        MakePartitionSet(pinIds, partitionSetIndex);
    }

    public void MakePartitionSet(int[] pinIds, IPartitionSet partitionSet)
    {
        MakePartitionSet(pinIds, partitionSet.Id);
    }

    public void MakePartitionSet(IPin[] pins, IPartitionSet partitionSet)
    {
        int[] pinIds = new int[pins.Length];
        for (int i = 0; i < pins.Length; i++)
        {
            pinIds[i] = pins[i].Id;
        }
        MakePartitionSet(pinIds, partitionSet.Id);
    }



    // <<<TEMPORARY, FOR DEBUGGING>>>
    public void Print()
    {
        Debug.Log("Pin Configuration for head direction " + headDirection + " with " + pinsPerEdge + " pins per edge and " + numPins + " pins:");
        for (int i = 0; i < numPins; i++)
        {
            Debug.Log("Partition set " + i + ":");
            partitionSets[i].Print();
        }
    }
}
