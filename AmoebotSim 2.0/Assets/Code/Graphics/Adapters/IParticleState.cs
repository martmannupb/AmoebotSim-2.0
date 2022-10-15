using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IParticleState
{
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
    /// Tail position of the particle. Same as Head() if particle is not expanded.
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
    /// Returns a list of all <see cref="ParticleAttribute{T}"/>s of the particle.
    /// </summary>
    /// <returns>A list containing all attributes of the particle.</returns>
    List<IParticleAttribute> GetAttributes();

    /// <summary>
    /// Provides access to <see cref="ParticleAttribute{T}"/>s by their display names.
    /// </summary>
    /// <param name="attrName">The display of the attribute to return.</param>
    /// <returns>The particle's attribute with the given name <paramref name="attrName"/>
    /// if it exists, otherwise <c>null</c>.</returns>
    IParticleAttribute TryGetAttributeByName(string attrName);

    // Circuits and Partition Sets _________________________

    /**
     * List of methods we need:
     * - Access to list of partition sets
     * - 
     * 
     * What connections we need:
     * - Partition sets need to have a connection to the circuits
     * - Circuits should have a color set
     * - 
     **/

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
    
}
