using System;
using System.Collections.Generic;

namespace AS2
{

    /// <summary>
    /// Container for serializing <see cref="AS2.Sim.ValueHistoryPinConfiguration"/> objects.
    /// </summary>
    [Serializable]
    public class PinConfigurationHistorySaveData
    {
        /// <summary>
        /// The serializable representation of the index history.
        /// </summary>
        public ValueHistorySaveData<int> idxHistory;
        /// <summary>
        /// A list containing the compressed pin configurations stored in the history.
        /// </summary>
        public List<PinConfigurationSaveData> configs;
    }

} // namespace AS2
