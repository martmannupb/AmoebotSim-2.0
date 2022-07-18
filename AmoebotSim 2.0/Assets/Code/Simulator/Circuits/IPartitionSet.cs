using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPartitionSet
{
    IPin[] GetPins();

    bool ContainsPin(IPin pin);
}
