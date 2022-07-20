using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// System-side implementation of the abstract base class
/// <see cref="Pin"/>, which declares the API
/// for the developer.
/// </summary>
public class SysPin : Pin
{
    public SysPartitionSet partitionSet;
    public int id;
    public int localDir;
    public bool head;
    public int edgeOffset;

    public SysPin(SysPartitionSet partitionSet, int id, int localDir, bool head, int edgeOffset)
    {
        this.partitionSet = partitionSet;
        this.id = id;
        this.localDir = localDir;
        this.head = head;
        this.edgeOffset = edgeOffset;
    }


    /**
     * Pin: Developer API
     */

    public override PartitionSet PartitionSet
    {
        get { return partitionSet; }
    }

    public override int Id
    {
        get { return id; }
    }

    public override int Direction
    {
        get { return localDir; }
    }

    public override int Offset
    {
        get { return edgeOffset; }
    }

    public override bool IsOnHead
    {
        get { return head; }
    }

    public override bool IsOnTail
    {
        get { return !head; }
    }




    // <<<TEMPORARY, FOR DEBUGGING>>>
    public string Print()
    {
        return "Pin with ID " + id + ": Direction " + localDir + ", Offset: " + edgeOffset + ", On Head: " + head;
    }
}
