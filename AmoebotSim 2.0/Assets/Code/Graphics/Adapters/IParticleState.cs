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
    int GlobalHeadDirection();

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
