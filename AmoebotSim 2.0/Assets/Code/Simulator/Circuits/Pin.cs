using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pin : IPin
{
    public PartitionSet partitionSet;
    public int id;
    public int localDir;
    public bool head;
    public int edgeOffset;

    public Pin(PartitionSet partitionSet, int id, int localDir, bool head, int edgeOffset)
    {
        this.partitionSet = partitionSet;
        this.id = id;
        this.localDir = localDir;
        this.head = head;
        this.edgeOffset = edgeOffset;
    }


    /**
     * IPin: Developer API
     */

    public IPartitionSet PartitionSet
    {
        get { return partitionSet; }
    }

    public int Id
    {
        get { return id; }
    }

    public int Direction
    {
        get { return localDir; }
    }

    public int Offset
    {
        get { return edgeOffset; }
    }

    public bool IsOnHead
    {
        get { return head; }
    }

    public bool IsOnTail
    {
        get { return !head; }
    }
}
