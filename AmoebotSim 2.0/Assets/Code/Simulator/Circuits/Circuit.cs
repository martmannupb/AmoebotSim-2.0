using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Documentation
public class Circuit
{
    public int id;
    public List<SysPartitionSet> partitionSets;
    public bool active;

    public Circuit(int id)
    {
        this.id = id;
        partitionSets = new List<SysPartitionSet>();
        active = true;
    }

    public void AddPartitionSet(SysPartitionSet ps)
    {
        partitionSets.Add(ps);
        ps.circuit = id;
    }

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
    }

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
        string s = "Circuit with ID " + id + " and " + partitionSets.Count + " partition sets (" + (active ? "active" : "inactive") + ")";
        return s;
    }
}
