using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains constants for the initialization mode.
/// </summary>
public static class Initialization
{
    public static readonly int NumGenericParams = 8;

    public enum Chirality { Clockwise, CounterClockwise, Random }

    public enum Compass { E = 0, NNE = 1, NNW = 2, W = 3, SSW = 4, SSE = 5, Random = -1 }
}
