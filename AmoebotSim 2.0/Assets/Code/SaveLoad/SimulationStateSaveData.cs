using System;

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
