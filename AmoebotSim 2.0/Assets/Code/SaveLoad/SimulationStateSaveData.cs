using System;

namespace AS2
{

    /// <summary>
    /// Container for all data a <see cref="AS2.Sim.ParticleSystem"/> requires
    /// to save and load its complete state.
    /// </summary>
    [Serializable]
    public class SimulationStateSaveData
    {
        public int earliestRound;
        public int latestRound;

        public int finishedRound;

        public ValueHistorySaveData<int> anchorIdxHistory;

        public ParticleStateSaveData[] particles;
    }

} // namespace AS2
