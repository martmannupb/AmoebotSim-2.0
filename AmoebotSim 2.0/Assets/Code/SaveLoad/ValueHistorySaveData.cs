using System;
using System.Collections.Generic;

/// <summary>
/// Container for saving and loading <see cref="ValueHistory{T}"/>
/// objects of serializable types.
/// </summary>
/// <typeparam name="T">The type of data stored in the history.</typeparam>
[Serializable]
public class ValueHistorySaveData<T>
{
    public List<T> values;
    public List<int> rounds;
    public int lastRound;
}
