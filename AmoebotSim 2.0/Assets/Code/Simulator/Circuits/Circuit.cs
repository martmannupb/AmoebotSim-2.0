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
    public Message message;

    public Circuit(int id)
    {
        this.id = id;
        partitionSets = new List<SysPartitionSet>();
        active = true;
        hasBeep = false;
        message = null;
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

        if (message == null)
        {
            if (ps.pinConfig.particle.HasPlannedMessage(ps.Id))
            {
                message = ps.pinConfig.particle.GetPlannedMessage(ps.Id);
            }
        }
        else if (ps.pinConfig.particle.HasPlannedMessage(ps.Id))
        {
            Message msg = ps.pinConfig.particle.GetPlannedMessage(ps.Id);
            if (msg.GreaterThan(message))
            {
                message = msg;
            }
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

        Message newMsg = null;
        if (message == null)
        {
            newMsg = other.message;
        }
        else if (other.message == null)
        {
            newMsg = message;
        }
        else
        {
            newMsg = other.message.GreaterThan(message) ? other.message : message;
        }
        message = newMsg;
        other.message = newMsg;
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
        string s = "Circuit with ID " + id + " and " + partitionSets.Count + " partition sets (" + (active ? "active" : "inactive") + "): " + (hasBeep ? "HAS BEEP" : "Has no beep") + ", " + (message != null ? "HAS MESSAGE" : "Has no message");
        return s;
    }
}
