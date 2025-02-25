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
    /// Exception class for the case that some operation of the simulator
    /// outside of the Amoebot simulation failed. This includes operations
    /// started in the wrong simulator state, save and load issues, and
    /// invalid input data.
    /// </summary>
    public class SimulatorStateException : SimulatorException
    {
        public SimulatorStateException() { }

        public SimulatorStateException(string msg) : base(msg) { }
    }

} // namespace AS2.Sim
