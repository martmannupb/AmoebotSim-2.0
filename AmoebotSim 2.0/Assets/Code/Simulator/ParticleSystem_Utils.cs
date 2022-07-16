using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ParticleSystem_Utils
{
    /**
     * Label conversion matrices
     * 
     * Labels of contracted particles are the local directions.
     * 
     * For expanded particles, labels 0,1,2 always correspond to
     * directions 0,1,2. For head directions 0,1,2, label 0 belongs
     * to the head, and for directions 3,4,5, label 0 belongs to
     * the tail.
     */

    /// <summary>
    /// Conversion matrix for converting a local direction of an
    /// expanded particle into a label.
    /// <para>
    /// The entry at position <c>[exp,loc]</c> is the head label
    /// of an expanded particle with head direction <c>exp</c>
    /// corresponding to the local direction <c>loc</c>. To get
    /// the tail label for that direction, use <c>(exp + 3) % 6</c>
    /// as the first index.
    /// </para>
    /// </summary>
    private static readonly int[,] expandedLabels = new int[6,6]
    {
        // Head direction 0,...,5 for head labels or
        // 3,4,5,0,1,2 for tail labels
        { 0, 1, 2, -1, 8, 9 },
        { 0, 1, 2, 3, -1, 9 },
        { 0, 1, 2, 3, 4, -1 },
        { -1, 3, 4, 5, 6, 7 },
        { 8, -1, 4, 5, 6, 7 },
        { 8, 9, -1, 5, 6, 7 }
    };

    /// <summary>
    /// Conversion matrix for converting a label into a direction.
    /// <para>
    /// The entry at position <c>[exp,label]</c> is the direction
    /// of an expanded particle with head direction <c>exp</c>
    /// corresponding to label <c>label</c>.
    /// </para>
    /// <para>
    /// Note that the conversion for head directions <c>3,4,5</c>
    /// is the same as for head directions <c>0,1,2</c>, which is
    /// why the second half of the matrix is redundant.
    /// </para>
    /// </summary>
    private static readonly int[,] labelDirections = new int[,]
    {
        // Head direction 0,...,5
        { 0, 1, 2, 1, 2, 3, 4, 5, 4, 5 },
        { 0, 1, 2, 3, 2, 3, 4, 5, 0, 5 },
        { 0, 1, 2, 3, 4, 3, 4, 5, 0, 1 },
        // NOTE: The second half could be removed, but we would have
        // one additional operation when reading...
        { 0, 1, 2, 1, 2, 3, 4, 5, 4, 5 },
        { 0, 1, 2, 3, 2, 3, 4, 5, 0, 5 },
        { 0, 1, 2, 3, 4, 3, 4, 5, 0, 1 }
    };

    /// <summary>
    /// Bool matrix telling whether a given label is a head or
    /// tail label.
    /// <para>
    /// The entry at position <c>[exp,label]</c> is <c>true</c>
    /// if and only if the label <c>label</c> belongs to the
    /// head of an expanded particle with head direction <c>exp</c>.
    /// </para>
    /// <para>
    /// Note that the entries for head directions <c>3,4,5</c> are
    /// exactly the opposite of the entries for head directions
    /// <c>0,1,2</c>.
    /// </para>
    /// </summary>
    private static readonly bool[,] isHeadLabel = new bool[,]
    {
        // Head direction 0,...,5
        { true, true, true, false, false, false, false, false, true, true },
        { true, true, true, true, false, false, false, false, false, true },
        { true, true, true, true, true, false, false, false, false, false },
        // NOTE: The second half could be removed, but we would have
        // additional operations when reading...
        { false, false, false, true, true, true, true, true, false, false },
        { false, false, false, false, true, true, true, true, true, false },
        { false, false, false, false, false, true, true, true, true, true }
    };

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
