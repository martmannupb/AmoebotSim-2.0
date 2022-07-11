using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IParticleState
{
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

}
