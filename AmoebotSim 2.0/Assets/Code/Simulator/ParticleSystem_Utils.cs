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
        switch (globalDir)
        {
            case 0:
                return new Vector2Int(pos.x + distance, pos.y);
            case 1:
                return new Vector2Int(pos.x, pos.y + distance);
            case 2:
                return new Vector2Int(pos.x - distance, pos.y + distance);
            case 3:
                return new Vector2Int(pos.x - distance, pos.y);
            case 4:
                return new Vector2Int(pos.x, pos.y - distance);
            case 5:
                return new Vector2Int(pos.x + distance, pos.y - distance);
            default:
                throw new System.ArgumentOutOfRangeException("globalDir", "Direction must be in set {0,1,2,3,4,5}.");
        }
    }

    /// <summary>
    /// Turns the given local direction into the corresponding global direction
    /// based on the given compass orientation and chirality.
    /// </summary>
    /// <param name="locDir">The local direction in <c>{0,1,2,3,4,5}</c>.</param>
    /// <param name="compassDir">The offset of the compass direction in <c>{0,1,2,3,4,5}</c>.</param>
    /// <param name="chirality">The direction in which rotation is applied. <c>true</c> means
    /// counter-clockwise is positive rotation and <c>false</c> means clockwise.</param>
    /// <returns>The global direction corresponding to <paramref name="locDir"/> offset by <paramref name="compassDir"/>.</returns>
    public static int LocalToGlobalDir(int locDir, int compassDir, bool chirality)
    {
        return chirality ? (compassDir + locDir) % 6 : (compassDir - locDir + 6) % 6;
    }

    /// <summary>
    /// Turns the given global direction into the corresponding local direction
    /// based on the given compass orientation and chirality.
    /// </summary>
    /// <param name="globalDir">The global direction in <c>{0,1,2,3,4,5}</c>.</param>
    /// <param name="compassDir">The offset of the compass direction in <c>{0,1,2,3,4,5}</c> (independent of chirality).</param>
    /// <param name="chirality">The direction in which rotation is applied. <c>true</c> means
    /// counter-clockwise is positive rotation and <c>false</c> means clockwise.</param>
    /// <returns>The local direction corresponding to <paramref name="globalDir"/> offset by <paramref name="compassDir"/>.</returns>
    public static int GlobalToLocalDir(int globalDir, int compassDir, bool chirality)
    {
        return chirality ? (globalDir - compassDir + 6) % 6 : (compassDir - globalDir + 6) % 6;
    }

    /// <summary>
    /// Determines the grid node neighboring the given particle in the
    /// indicated direction.
    /// </summary>
    /// <param name="p">The Particle whose neighbor node to find.</param>
    /// <param name="locDir">The local direction of the Particle <paramref name="p"/>
    /// indicating in which direction to look.</param>
    /// <param name="fromHead">If <c>true</c>, use the Particle's head as reference,
    /// otherwise use the Particle's tail.</param>
    /// <returns>The grid node in direction <paramref name="locDir"/> relative to
    /// Particle <paramref name="p"/>'s head or tail, depending on <paramref name="fromHead"/>.</returns>
    public static Vector2Int GetNeighborPosition(Particle p, int locDir, bool fromHead)
    {
        return GetNbrInDir(fromHead ? p.Head() : p.Tail(), LocalToGlobalDir(locDir, p.comDir, p.chirality));
    }
}
