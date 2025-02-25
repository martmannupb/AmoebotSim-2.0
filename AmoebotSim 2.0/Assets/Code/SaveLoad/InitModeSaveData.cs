// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;

namespace AS2
{

    /// <summary>
    /// Serializable representation of UI data in
    /// Initialization Mode.
    /// </summary>
    [Serializable]
    public class InitModeSaveData
    {
        /// <summary>
        /// Name of the selected algorithm.
        /// </summary>
        public string algString;
        /// <summary>
        /// Name of the generation method associated with the
        /// selected algorithm.
        /// </summary>
        public string genAlgString;
        /// <summary>
        /// Parameter values of the generation method.
        /// </summary>
        public string[] genAlg_parameters;
    }

} // namespace AS2
