// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Exception class for the case that an error occurred during the
    /// simulation that cannot be directly attributed to a single particle.
    /// </summary>
    public class SimulationException : SimulatorException
    {
        public SimulationException() { }

        public SimulationException(string msg) : base(msg) { }
    }

} // namespace AS2.Sim
