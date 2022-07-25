using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Represents a collection of partition sets that are connected
/// into a single circuit.
/// </summary>
public class Circuit
{
    public int id;
    public List<SysPartitionSet> partitionSets;
    public bool active;
    public bool hasBeep;

    public Circuit(int id)
    {
        this.id = id;
        partitionSets = new List<SysPartitionSet>();
        active = true;
        hasBeep = false;
    }

    /// <summary>
    /// Adds the given partition set to this circuit.
    /// <para>
    /// If the partition set has a planned beep, this information
    /// is stored in the circuit.
    /// </para>
    /// </summary>
    /// <param name="ps">The partition set to be added.</param>
    public void AddPartitionSet(SysPartitionSet ps)
    {
        partitionSets.Add(ps);
        ps.circuit = id;
        if (!hasBeep && ps.pinConfig.particle.HasPlannedBeep(ps.Id))
        {
            hasBeep = true;
        }
    }

    /// <summary>
    /// Merges this circuit with the given other circuit.
    /// <para>
    /// Nothing happens if the other circuit is the same
    /// as this one. Otherwise, the smaller circuit is
    /// merged into the bigger one and the smaller one is
    /// set to be inactive. If one of the circuits has a
    /// planned beep, the resulting circuit will also
    /// have a planned beep.
    /// </para>
    /// </summary>
    /// <param name="other">The circuit to merge this one
    /// with.</param>
    public void MergeWith(Circuit other)
    {
        if (other.id == id)
        {
            return;
        }
        if (partitionSets.Count >= other.partitionSets.Count)
        {
            MergeOther(other);
        }
        else
        {
            other.MergeOther(this);
        }
        bool beep = hasBeep || other.hasBeep;
        hasBeep = beep;
        other.hasBeep = beep;
    }

    /// <summary>
    /// Merges the given other circuit into this one.
    /// </summary>
    /// <param name="other">The circuit to be merged into
    /// this one. It will be set to inactive after the merge.</param>
    private void MergeOther(Circuit other)
    {
        foreach (SysPartitionSet ps in other.partitionSets)
        {
            ps.circuit = id;
        }
        partitionSets.AddRange(other.partitionSets);
        other.active = false;
    }


    public string Print()
    {
        string s = "Circuit with ID " + id + " and " + partitionSets.Count + " partition sets (" + (active ? "active" : "inactive") + "): " + (hasBeep ? "HAS BEEP" : "Has no beep");
        return s;
    }
}
