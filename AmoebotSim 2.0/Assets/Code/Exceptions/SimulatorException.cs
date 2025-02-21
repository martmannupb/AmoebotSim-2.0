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
    /// Base class for exceptions thrown by the simulator due to
    /// a problem during the simulation or invalid data or usage.
    /// </summary>
    public class SimulatorException : AmoebotSimException
    {
        public SimulatorException() { }

        public SimulatorException(string msg) : base(msg) { }
    }

} // namespace AS2.Sim
