using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ParticleSystem_Utils
{
    /// <summary>
    /// Computes the neighbor of a grid node position in the given direction and distance.
    /// </summary>
    /// <param name="pos">The original grid node position.</param>
    /// <param name="globalDir">The global direction in which the neighbor lies.
    /// <c>0</c> means "right" and the direction increases counter-clockwise.
    /// Allowed values are <p>{0,1,2,3,4,5}</p>.</param>
    /// <param name="distance">The number of steps to reach the neighbor from the original position.</param>
    /// <returns>The node that lies <paramref name="distance"/> steps in direction <paramref name="globalDir"/>
    /// from node <paramref name="pos"/>.</returns>
    public static Vector2Int GetNbrInDir(Vector2Int pos, int globalDir, int distance = 1)
    {
        return globalDir switch
        {
            0 => new Vector2Int(pos.x + distance,   pos.y),
            1 => new Vector2Int(pos.x,              pos.y + distance),
            2 => new Vector2Int(pos.x - distance,   pos.y + distance),
            3 => new Vector2Int(pos.x - distance,   pos.y),
            4 => new Vector2Int(pos.x,              pos.y - distance),
            5 => new Vector2Int(pos.x + distance,   pos.y - distance),
            _ => throw new System.ArgumentOutOfRangeException()
        };
    }

    public static Vector2Int GetNeighborPosition(Particle p, int locDir, bool isHead)
    {
        throw new System.NotImplementedException();
    }
}
