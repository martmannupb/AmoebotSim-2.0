using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pin
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
}
