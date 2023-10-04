using System;
using AS2.Sim;

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
        /// Indicates whether the simulation is in a collision state or not.
        /// </summary>
        public bool inCollisionState;


        public CollisionChecker.DebugLine[] collisionDebugLines;

        /// <summary>
        /// The history of the anchor particle indices.
        /// </summary>
        public ValueHistorySaveData<int> anchorIdxHistory;

        /// <summary>
        /// The history of the flag telling whether the anchor
        /// index refers to an object.
        /// </summary>
        public ValueHistorySaveData<bool> anchorIsObjectHistory;

        /// <summary>
        /// The particles in the system (includes their state histories).
        /// </summary>
        public ParticleStateSaveData[] particles;

        /// <summary>
        /// The objects in the system (includes their state histories).
        /// </summary>
        public ParticleObjectSaveData[] objects;
    }

} // namespace AS2
