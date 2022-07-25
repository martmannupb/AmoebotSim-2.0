using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Change visibility of members (also other API classes) when circuit computation is finished
// TODO: Update documentation if compressed objects are used for storage
/// <summary>
/// System-side implementation of the abstract base class
/// <see cref="PinConfiguration"/>, which declares the API
/// for the developer.
/// <para>
/// Implements comparison operators such that pin configurations can
/// be stored in a <see cref="ValueHistory{T}"/>.
/// </para>
/// </summary>
public class SysPinConfiguration : PinConfiguration
{
    public Particle particle;
    private int pinsPerEdge;
    private int headDirection;
    private int numPins;

    public SysPin[] pins;
    public SysPin[] pinsGlobal;
    public SysPartitionSet[] partitionSets;

    // State information for receiving and sending beeps and messages
    public bool isCurrent = false;  // If true, give access to received data
    public bool isPlanned = false;  // If true, allow sending data

    public SysPinConfiguration(Particle particle, int pinsPerEdge, int headDirection = -1)
    {
        this.particle = particle;
        this.pinsPerEdge = pinsPerEdge;
        this.headDirection = headDirection;

        numPins = headDirection == -1 ? (6 * pinsPerEdge) : (10 * pinsPerEdge);

        partitionSets = new SysPartitionSet[numPins];
        pins = new SysPin[numPins];
        pinsGlobal = new SysPin[numPins];

        int comDir = particle.comDir;
        bool chirality = particle.chirality;

        // Initialize partition sets and pins
        // Default is singleton: Each pin is its own partition set (for now)
        // Store each pin in its local position and its global position
        if (headDirection == -1)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                int globalDir = ParticleSystem_Utils.LocalToGlobalDir(direction, comDir, chirality);
                for (int idx = 0; idx < pinsPerEdge; idx++)
                {
                    int idxGlobal = chirality ? idx : pinsPerEdge - 1 - idx;
                    int id = direction * pinsPerEdge + idx;
                    int idGlobal = globalDir * pinsPerEdge + idxGlobal;
                    SysPartitionSet ps = new SysPartitionSet(this, id, numPins);
                    SysPin pin = new SysPin(ps, id, direction, globalDir, true, idx, idxGlobal);
                    ps.AddPinBasic(id);
                    partitionSets[id] = ps;
                    pins[id] = pin;
                    pinsGlobal[idGlobal] = pin;
                }
            }
        }
        else
        {
            for (int label = 0; label < 10; label++)
            {
                int direction = ParticleSystem_Utils.GetDirOfLabel(label, headDirection);
                int globalDir = ParticleSystem_Utils.LocalToGlobalDir(direction, comDir, chirality);
                bool isHead = ParticleSystem_Utils.IsHeadLabel(label, headDirection);
                int globalLabel = ParticleSystem_Utils.GetLabelInDir(globalDir, ParticleSystem_Utils.LocalToGlobalDir(headDirection, comDir, chirality), isHead);
                for (int idx = 0; idx < pinsPerEdge; idx++)
                {
                    int idxGlobal = chirality ? idx : pinsPerEdge - 1 - idx;
                    int id = label * pinsPerEdge + idx;
                    int idGlobal = globalLabel * pinsPerEdge + idxGlobal;
                    SysPartitionSet ps = new SysPartitionSet(this, id, numPins);
                    SysPin pin = new SysPin(ps, id, direction, globalLabel, isHead, idx, idxGlobal);
                    ps.AddPinBasic(id);
                    partitionSets[id] = ps;
                    pins[id] = pin;
                    pinsGlobal[idGlobal] = pin;
                }
            }
        }

        // FOR DEBUGGING

        // DEFAULT IS SINGLETON

        // TRY GLOBAL
        //SetToGlobal();

        // TRY STAR ON ALL PINS
        //for (int i = 0; i < pinsPerEdge; i++)
        //{
        //    SetStarConfig(i, i);
        //}
    }

    private void UpdateFlagsAfterChange()
    {
        isCurrent = false;
        isPlanned = false;
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
    public SysPin GetPin(int pinId)
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
    public SysPartitionSet GetPartitionSetWithId(int partitionSetId)
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
            SysPartitionSet ps = partitionSets[i];
            if (ps.IsEmpty())
            {
                ps.AddPin(pinId);
                UpdateFlagsAfterChange();
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
                SysPartitionSet ps = partitionSets[i];
                if (ps.IsEmpty())
                {
                    ps.AddPin(pinId);
                    foundSet = true;
                    UpdateFlagsAfterChange();
                    break;
                }
            }
            if (!foundSet)
            {
                throw new System.InvalidOperationException("Pin with ID " + pinId + " cannot be removed from its partition set: No other partition set is empty.");
            }
        }
    }

    // TODO: There is probably a much more efficient way to do this
    /// <summary>
    /// Creates a full copy of this pin configuration.
    /// <para>
    /// All <see cref="SysPin"/>s and <see cref="SysPartitionSet"/>s contained in
    /// this pin configuration are copied as well. It is not a deep copy
    /// because the reference to the containing <see cref="Particle"/> stays
    /// the same.
    /// </para>
    /// </summary>
    /// <returns>A copy of this pin configuration.</returns>
    public SysPinConfiguration Copy()
    {
        SysPinConfiguration copy = new SysPinConfiguration(particle, pinsPerEdge, headDirection);
        for (int i = 0; i < numPins; i++)
        {
            if (!partitionSets[i].IsEmpty())
            {
                copy.MakePartitionSet(partitionSets[i].GetPins(), i);
            }
        }
        copy.isCurrent = isCurrent;
        copy.isPlanned = isPlanned;
        return copy;
    }

    // TODO: May have to put these into compressed storage classes/structs instead
    /**
     * Comparison operators for comparing pin configurations easily
     */

    public static bool operator==(SysPinConfiguration pc1, SysPinConfiguration pc2)
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

    public static bool operator!=(SysPinConfiguration pc1, SysPinConfiguration pc2)
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
        return obj is SysPinConfiguration other && this == other;
    }

    // TODO: Make sure this is correct if it is used
    public override int GetHashCode()
    {
        return System.HashCode.Combine(pinsPerEdge, headDirection, partitionSets);
    }


    /**
     * PinConfiguration: Developer API
     */

    public override int HeadDirection
    {
        get { return headDirection; }
    }

    public override int PinsPerEdge
    {
        get { return pinsPerEdge; }
    }

    public override int NumPins
    {
        get { return numPins; }
    }

    public override Pin GetPinAt(int direction, int offset, bool head = true)
    {
        int pinId = GetPinId(direction, offset, head);
        return pins[pinId];
    }

    public override Pin[] GetPinsAtEdge(int direction, bool head = true)
    {
        Pin[] ipins = new Pin[pinsPerEdge];
        int pinId = GetPinId(direction, 0, head);
        for (int i = 0; i < pinsPerEdge; i++)
        {
            ipins[i] = pins[pinId];
            pinId++;
        }
        return ipins;
    }

    public override PartitionSet GetPartitionSet(int index)
    {
        return partitionSets[index];
    }

    public override PartitionSet[] GetPartitionSets()
    {
        return partitionSets;
    }

    public override PartitionSet[] GetNonEmptyPartitionSets()
    {
        List<SysPartitionSet> nonEmptySets = new List<SysPartitionSet>();
        foreach (SysPartitionSet ps in partitionSets)
        {
            if (!ps.IsEmpty())
            {
                nonEmptySets.Add(ps);
            }
        }
        return nonEmptySets.ToArray();
    }

    public override void SetToSingleton()
    {
        for (int id = 0; id < numPins; id++)
        {
            SysPartitionSet ps = partitionSets[id];
            SysPin pin = pins[id];
            ps.ClearInternal();
            ps.AddPinBasic(id);
            pin.partitionSet = ps;
        }
        UpdateFlagsAfterChange();
    }

    public override void SetToGlobal(int partitionSetId = 0)
    {
        SysPartitionSet psGlobal = partitionSets[partitionSetId];
        psGlobal.ClearInternal();
        for (int id = 0; id < numPins; id++)
        {
            SysPartitionSet ps = partitionSets[id];
            SysPin pin = pins[id];
            if (id != partitionSetId)
            {
                ps.ClearInternal();
            }
            psGlobal.AddPinBasic(id);
            pin.partitionSet = psGlobal;
        }
        UpdateFlagsAfterChange();
    }

    public override void SetToGlobal(PartitionSet partitionSet)
    {
        SetToGlobal(partitionSet.Id);
    }

    public override void SetStarConfig(int offset, int partitionSetIndex)
    {
        SetStarConfig(offset, new bool[headDirection == -1 ? 6 : 10], partitionSetIndex);
    }

    public override void SetStarConfig(int offset, PartitionSet partitionSet)
    {
        SetStarConfig(offset, new bool[headDirection == -1 ? 6 : 10], partitionSet.Id);
    }

    public override void SetStarConfig(int offset, bool[] inverted, int partitionSetIndex)
    {
        // First add the correct pins, then remove the ones that have to be removed
        // This order avoids exceptions in all cases
        SysPartitionSet ps = partitionSets[partitionSetIndex];
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
        UpdateFlagsAfterChange();
    }

    public override void SetStarConfig(int offset, bool[] inverted, PartitionSet partitionSet)
    {
        SetStarConfig(offset, inverted, partitionSet.Id);
    }

    public override void MakePartitionSet(int[] pinIds, int partitionSetIndex)
    {
        // First add the correct pins, then remove the ones that have to be removed
        // This order avoids exceptions in all cases except the one where an error
        // is unavoidable
        SysPartitionSet ps = partitionSets[partitionSetIndex];
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
        UpdateFlagsAfterChange();
    }

    public override void MakePartitionSet(Pin[] pins, int partitionSetIndex)
    {
        int[] pinIds = new int[pins.Length];
        for (int i = 0; i < pins.Length; i++)
        {
            pinIds[i] = pins[i].Id;
        }
        MakePartitionSet(pinIds, partitionSetIndex);
    }

    public override void MakePartitionSet(int[] pinIds, PartitionSet partitionSet)
    {
        MakePartitionSet(pinIds, partitionSet.Id);
    }

    public override void MakePartitionSet(Pin[] pins, PartitionSet partitionSet)
    {
        int[] pinIds = new int[pins.Length];
        for (int i = 0; i < pins.Length; i++)
        {
            pinIds[i] = pins[i].Id;
        }
        MakePartitionSet(pinIds, partitionSet.Id);
    }

    public override bool ReceivedBeepOnPartitionSet(int partitionSetIndex)
    {
        if (!isCurrent)
        {
            throw new InvalidOperationException("Cannot check for received beeps in non-current pin configuration.");
        }
        return particle.HasReceivedBeep(partitionSetIndex);
    }

    public override void SendBeepOnPartitionSet(int partitionSetIndex)
    {
        if (!isPlanned)
        {
            throw new InvalidOperationException("Cannot send beeps in non-planned pin configuration.");
        }
        // TODO: Mask these things better
        particle.PlanBeep(partitionSetIndex);
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
