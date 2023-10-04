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
        /// The current set of objects.
        /// </summary>
        public ParticleObjectSaveData[] objects;

        /// <summary>
        /// The index of the current anchor particle or object.
        /// </summary>
        public int anchorIdx;

        /// <summary>
        /// Whether the current anchor is an object.
        /// </summary>
        public bool anchorIsObject;

        /// <summary>
        /// Description of the current UI state.
        /// </summary>
        public InitModeSaveData initModeSaveData;
    }

} // namespace AS2
