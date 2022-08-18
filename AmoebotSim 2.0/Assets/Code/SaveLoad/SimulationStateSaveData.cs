using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container for all data a <see cref="ParticleSystem"/> requires
/// to save and load its complete state.
/// </summary>
[Serializable]
public class SimulationStateSaveData
{
    public int earliestRound;
    public int latestRound;

    public ParticleStateSaveData[] particles;
}
