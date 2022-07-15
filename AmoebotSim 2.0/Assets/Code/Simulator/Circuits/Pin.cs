using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pin
{
    private PartitionSet partitionSet;
    private int id;
    private int localDir;
    private bool head;
    private int edgeOffset;

    public Pin(PartitionSet partitionSet, int id, int localDir, bool head, int edgeOffset)
    {
        this.partitionSet = partitionSet;
        this.id = id;
        this.localDir = localDir;
        this.head = head;
        this.edgeOffset = edgeOffset;
    }
}
