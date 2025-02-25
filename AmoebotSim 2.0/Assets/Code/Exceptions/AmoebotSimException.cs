// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Base class for exceptions thrown due to unintended
    /// behavior of the simulator.
    /// </summary>
    public class AmoebotSimException : Exception
    {
        public AmoebotSimException() { }

        public AmoebotSimException(string msg) : base(msg) { }
    }

} // namespace AS2.Sim
