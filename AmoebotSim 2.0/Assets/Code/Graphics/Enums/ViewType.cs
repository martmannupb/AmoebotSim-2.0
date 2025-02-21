// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{
    /// <summary>
    /// The types of views of the simulation environment.
    /// Circular = Graph View (like in the first AmoebotSim),
    /// Hexagonal = Hexagonal Grid with Hexagonal Particles,
    /// HexagonalCirc = Hexagonal Grid with Rounded Particles
    /// </summary>
    public enum ViewType
    {
        Hexagonal,
        HexagonalCirc,
        Circular
    }

}