using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{
    /// <summary>
    /// The types of views of the simulation environment.
    /// Circular = Graph View (like in the first AmoebotSim), Hexagonal = Hexagonal Grid with Hexagonal Particles, HexagonalCirc = Hexagonal Grid with Rounded Particles
    /// </summary>
    public enum ViewType
    {
        Hexagonal,
        HexagonalCirc,
        Circular
    }

}