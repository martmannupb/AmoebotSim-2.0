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
            _ => throw new System.ArgumentOutOfRangeException("globalDir", "Direction must be in set {0,1,2,3,4,5}.")
        };
    }

    /// <summary>
    /// Turns the given local direction into the corresponding global direction
    /// based on the given compass orientation.
    /// </summary>
    /// <param name="locDir">The local direction in <c>{0,1,2,3,4,5}</c>.</param>
    /// <param name="compassDir">The offset of the compass direction in <c>{0,1,2,3,4,5}</c>.</param>
    /// <returns>The global direction corresponding to <paramref name="locDir"/> offset by <paramref name="compassDir"/>.</returns>
    public static int LocalToGlobalDir(int locDir, int compassDir)
    {
        return (locDir + compassDir) % 6;
    }

    public static Vector2Int GetNeighborPosition(Particle p, int locDir, bool fromHead)
    {
        return GetNbrInDir(fromHead ? p.Head() : p.Tail(), LocalToGlobalDir(locDir, p.comDir));
    }
}
