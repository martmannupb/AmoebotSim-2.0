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
        /// An array containing the compressed pin configurations stored in the history.
        /// </summary>
        public PinConfigurationSaveData[] configs;
    }

} // namespace AS2
