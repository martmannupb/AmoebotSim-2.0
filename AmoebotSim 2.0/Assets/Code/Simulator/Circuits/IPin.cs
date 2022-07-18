using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPin
{
    int Direction
    {
        get;
    }

    int Offset
    {
        get;
    }

    bool IsOnHead
    {
        get;
    }

    bool IsOnTail
    {
        get;
    }

    IPartitionSet GetPartitionSet();
}
