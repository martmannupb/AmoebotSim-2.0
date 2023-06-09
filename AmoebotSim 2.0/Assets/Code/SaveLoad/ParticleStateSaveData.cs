using System;
using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Container for all data a <see cref="AS2.Sim.Particle"/> requires
    /// to save and load its complete state.
    /// </summary>
    [Serializable]
    public class ParticleStateSaveData
    {
        // Global info
        /// <summary>
        /// The particle's compass direction.
        /// </summary>
        public Direction comDir;
        /// <summary>
        /// The particle's chirality.
        /// </summary>
        public bool chirality;

        // Algorithm info
        /// <summary>
        /// The full type name of the algorithm attached to the particle.
        /// <para>
        /// This type name is used to find the correct algorithm type by
        /// reflection when the particle state is loaded.
        /// </para>
        /// </summary>
        public string algorithmType;

        // Positional info
        /// <summary>
        /// History of tail positions.
        /// </summary>
        public ValueHistorySaveData<Vector2Int> tailPositionHistory;
        /// <summary>
        /// History of head directions.
        /// </summary>
        public ValueHistorySaveData<Direction> expansionDirHistory;

        // Attribute data, sorted by type
        public List<ParticleAttributeSaveData<bool>> boolAttributes;
        public List<ParticleAttributeSaveData<Direction>> dirAttributes;
        public List<ParticleAttributeSaveData<float>> floatAttributes;
        public List<ParticleAttributeSaveData<int>> intAttributes;
        public List<ParticleAttributeEnumSaveData> enumAttributes;
        public List<ParticleAttributePCSaveData> pcAttributes;
        public List<ParticleAttributeSaveData<string>> stringAttributes;

        // Bond data
        public ValueHistorySaveData<int> activeBondHistory;
        public ValueHistorySaveData<int> markedBondHistory;
        public ValueHistorySaveData<int> visibleBondHistory;

        // Circuit data
        public PinConfigurationHistorySaveData pinConfigurationHistory;
        public ValueHistorySaveData<bool>[] receivedBeepsHistory;
        public ValueHistorySaveData<MessageSaveData>[] receivedMessagesHistory;
        public ValueHistorySaveData<bool>[] plannedBeepsHistory;
        public ValueHistorySaveData<MessageSaveData>[] plannedMessagesHistory;

        // Visualization data
        public ValueHistorySaveData<Color> mainColorHistory;
        public ValueHistorySaveData<bool> mainColorSetHistory;
        public ValueHistorySaveData<JointMovementInfo> jointMovementHistory;
        public ValueHistorySaveData<BondMovementInfoList> bondMovementHistory;
    }

} // namespace AS2
