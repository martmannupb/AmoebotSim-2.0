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
        /// <summary>
        /// The first round of the simulation (usually 0).
        /// </summary>
        public int earliestRound;
        /// <summary>
        /// The last round of the simulation.
        /// </summary>
        public int latestRound;

        /// <summary>
        /// The round in which the simulation finished.
        /// </summary>
        public int finishedRound;

        /// <summary>
        /// The history of the anchor particle indices.
        /// </summary>
        public ValueHistorySaveData<int> anchorIdxHistory;

        /// <summary>
        /// The particles in the system (includes their state histories).
        /// </summary>
        public ParticleStateSaveData[] particles;
    }

} // namespace AS2
