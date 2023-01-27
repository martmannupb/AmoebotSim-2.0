using System;

namespace AS2
{

    /// <summary>
    /// Serializable representation of the system state in
    /// Initialization Mode used for saving and loading.
    /// </summary>
    [Serializable]
    public class InitializationStateSaveData
    {
        /// <summary>
        /// The string ID of the currently selected algorithm.
        /// </summary>
        public string selectedAlgorithm;

        /// <summary>
        /// The current set of particles.
        /// </summary>
        public InitParticleSaveData[] particles;

        /// <summary>
        /// Description of the current UI state.
        /// </summary>
        public InitModeSaveData initModeSaveData;
    }

} // namespace AS2
