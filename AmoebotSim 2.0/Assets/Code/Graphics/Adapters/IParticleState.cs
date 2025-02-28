// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using AS2.Sim;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Implemented by the particles. Contains helpful methods for
    /// the view to access standardized data from particles.
    /// </summary>
    public interface IParticleState
    {
        /// <summary>
        /// Returns the graphics adapter for the particle.
        /// </summary>
        /// <returns></returns>
        IParticleGraphicsAdapter GetGraphicsAdapter();

        // General Data _________________________

        /// <summary>
        /// True if the particle is expanded, false if not.
        /// </summary>
        /// <returns></returns>
        bool IsExpanded();

        /// <summary>
        /// The global direction of an expansion. Returns -1 if there is no expansion.
        /// </summary>
        /// <returns></returns>
        int GlobalHeadDirectionInt();

        /// <summary>
        /// Head position of the particle.
        /// </summary>
        /// <returns></returns>
        Vector2Int Head();

        /// <summary>
        /// Tail position of the particle. Same as <see cref="Head"/>
        /// if the particle is not expanded.
        /// </summary>
        /// <returns></returns>
        Vector2Int Tail();

        /// <summary>
        /// Returns the chirality of the particle. <c>true</c> means
        /// counter-clockwise and <c>false</c> means clockwise.
        /// </summary>
        /// <returns>The chirality of the particle.</returns>
        bool Chirality();

        /// <summary>
        /// Returns the global compass orientation of the particle. This is
        /// the global direction that the particle identifies as
        /// <see cref="Direction.E"/> in its local view.
        /// </summary>
        /// <returns>The global compass orientation of the particle.</returns>
        Direction CompassDir();

        /// <summary>
        /// Sets the particle's chirality to the given value. Only works if
        /// the particle is in a state that allows the chirality to be set.
        /// </summary>
        /// <param name="chirality">The new chirality. <c>true</c> means
        /// counter-clockwise, <c>false</c> means clockwise.</param>
        void SetChirality(bool chirality);

        /// <summary>
        /// Sets the particle's compass direction to the given value. Only works
        /// if the particle is in a state that allows the compass to be set.
        /// </summary>
        /// <param name="compassDir">The new compass direction, given as a
        /// global cardinal direction.</param>
        void SetCompassDir(Direction compassDir);

        /// <summary>
        /// Sets the particle's chirality to a random value. Only works if
        /// the particle is in a state that allows the chirality to be set.
        /// </summary>
        void SetChiralityRandom()
        {
            SetChirality(Random.Range(0, 2) == 0);
        }

        /// <summary>
        /// Sets the particle's compass direction to a random value. Only works
        /// if the particle is in a state that allows the compass to be set.
        /// </summary>
        void SetCompassDirRandom()
        {
            SetCompassDir(DirectionHelpers.Cardinal(Random.Range(0, 6)));
        }

        /// <summary>
        /// Returns a list of all <see cref="AS2.Sim.ParticleAttribute{T}"/>s of the particle.
        /// </summary>
        /// <returns>A list containing all attributes of the particle.</returns>
        List<IParticleAttribute> GetAttributes();

        /// <summary>
        /// Provides access to <see cref="AS2.Sim.ParticleAttribute{T}"/>s by their display names.
        /// </summary>
        /// <param name="attrName">The display of the attribute to return.</param>
        /// <returns>The particle's attribute with the given name <paramref name="attrName"/>
        /// if it exists, otherwise <c>null</c>.</returns>
        IParticleAttribute TryGetAttributeByName(string attrName);

        /// <summary>
        /// Checks if the particle is currently the anchor of the system. The anchor
        /// particle defines how the system moves during a joint movement by keeping
        /// its global position.
        /// </summary>
        /// <returns><c>true</c> if and only if this particle is the anchor.</returns>
        bool IsAnchor();

        /// <summary>
        /// Turns this particle into the anchor of the system. The anchor particle
        /// defines how the system moves during a joint movement by keeping its
        /// global position.
        /// </summary>
        /// <returns><c>true</c> if and only if the particle was successfully
        /// turned into the anchor.</returns>
        bool MakeAnchor();

        // Circuits and Partition Sets _________________________

        /// <summary>
        /// Returns the number of pins per side at the particle.
        /// </summary>
        /// <returns></returns>
        int GetCircuitPinsPerSide();

        // Visualization

        /// <summary>
        /// Method to get the particle color for the visualization.
        /// </summary>
        /// <returns></returns>
        Color GetParticleColor();

        /// <summary>
        /// Checks if the particle color has been overwritten.
        /// </summary>
        /// <returns><c>true</c> if and only if the particle color
        /// was overwritten by the particle algorithm.</returns>
        bool IsParticleColorSet();

        /// <summary>
        /// Gets the name of the algorithm running this particle's
        /// behavior.
        /// </summary>
        /// <returns>The unique display name of the algorithm
        /// running this particle.</returns>
        string AlgorithmName();

    }

}
