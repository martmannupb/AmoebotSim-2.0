using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IParticleState
{
    /// <summary>
    /// True if the particle is expanded, false if not.
    /// </summary>
    /// <returns></returns>
    public bool IsExpanded();

    /// <summary>
    /// The global direction of an expansion. Returns -1 if there is no expansion.
    /// </summary>
    /// <returns></returns>
    public int GetGlobalExpansionDir();

    /// <summary>
    /// Head position of the particle.
    /// </summary>
    /// <returns></returns>
    public Vector2Int Head();

    /// <summary>
    /// Tail position of the particle. Same as Head() if particle is not expanded.
    /// </summary>
    /// <returns></returns>
    public Vector2Int Tail();

}
