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
    /// Container for saving and loading <see cref="AS2.Sim.ValueHistory{T}"/>
    /// objects of serializable types.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the history.</typeparam>
    [Serializable]
    public class ValueHistorySaveData<T>
    {
        /// <summary>
        /// The list of all different values stored in the history.
        /// </summary>
        public List<T> values;
        /// <summary>
        /// The list of rounds in which the values have changed.
        /// </summary>
        public List<int> rounds;
        /// <summary>
        /// The last recorded round.
        /// </summary>
        public int lastRound;
    }

} // namespace AS2
