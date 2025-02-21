// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


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
