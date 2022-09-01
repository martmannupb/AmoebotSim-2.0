using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Constants for the 6 cardinal directions as well as the
/// 6 directions perpendicular to them. The directions are
/// numbered <c>0,...,11</c> with direction <see cref="Direction.E"/>
/// being <c>0</c> and direction values increasing in
/// counter-clockwise direction.
/// <para>
/// The <see cref="Direction.NONE"/> value represents no
/// direction. If it is used in any calculation, the result
/// is undefined.
/// </para>
/// </summary>
public enum Direction
{
    NONE = -1,
    E = 0,
    ENE = 1,
    NNE = 2,
    N = 3,
    NNW = 4,
    WNW = 5,
    W = 6,
    WSW = 7,
    SSW = 8,
    S = 9,
    SSE = 10,
    ESE = 11
}


/// <summary>
/// Defines extension methods for the <see cref="Direction"/> enum.
/// </summary>
static class DirectionExtensions
{
    /// <summary>
    /// Returns the direction directly opposite to this direction.
    /// <para>
    /// If <paramref name="d"/> is (not) a cardinal direction, then
    /// the result will also (not) be a cardinal direction.
    /// </para>
    /// </summary>
    /// <param name="d">The current direction.</param>
    /// <returns>The direction directly opposite of <paramref name="d"/>.</returns>
    public static Direction Opposite(this Direction d)
    {
        return (Direction)(((int)d + 6) % 12);
    }

    /// <summary>
    /// Returns the given direction rotated by 60 degrees times the
    /// given amount in clockwise direction. Negative values rotate
    /// in counter-clockwise direction.
    /// <para>
    /// If <paramref name="d"/> is (not) a cardinal direction, then
    /// the result will also (not) be a cardinal direction.
    /// </para>
    /// </summary>
    /// <param name="d">The current direction.</param>
    /// <param name="amount">The number of times to rotate by 60
    /// degrees in clockwise direction.</param>
    /// <returns>Direction <paramref name="d"/> rotated by 60
    /// degrees times <paramref name="amount"/> in clockwise
    /// direction.</returns>
    public static Direction Rotate60(this Direction d, int amount)
    {
        if (amount < 0)
            amount = 5 * Mathf.Abs(amount);
        return (Direction)(((int)d + 2 * amount) % 12);
    }

    /// <summary>
    /// Returns the given direction rotated by 30 degrees times the
    /// given amount in clockwise direction. Negative values rotate
    /// in counter-clockwise direction.
    /// <para>
    /// If <paramref name="d"/> is (not) a cardinal direction, then
    /// the result will also (not) be a cardinal direction if and
    /// only if the given <paramref name="amount"/> is even. If
    /// <paramref name="amount"/> is odd, one of <paramref name="d"/>
    /// and the result will be a cardinal direction and the other
    /// one will not be a cardinal direction.
    /// </para>
    /// </summary>
    /// <param name="d">The current direction.</param>
    /// <param name="amount">The number of times to rotate by 30
    /// degrees in clockwise direction.</param>
    /// <returns>Direction <paramref name="d"/> rotated by 30
    /// degrees times <paramref name="amount"/> in clockwise
    /// direction.</returns>
    public static Direction Rotate30(this Direction d, int amount)
    {
        if (amount < 0)
            amount = 11 * Mathf.Abs(amount);
        return (Direction)(((int)d + amount) % 12);
    }

    /// <summary>
    /// Returns the number of 30 degree rotations needed to make
    /// direction <paramref name="d"/> match direction <paramref name="target"/>.
    /// </summary>
    /// <param name="d">The direction to be rotated.</param>
    /// <param name="target">The target direction to be matched.</param>
    /// <param name="clockwise">If <c>true</c>, rotate in clockwise direction
    /// instead of counter-clockwise.</param>
    /// <returns>The number of counter-clockwise or clockwise 30 degree rotations
    /// necessary to align <paramref name="d"/> with <paramref name="target"/>.</returns>
    public static int DistanceTo(this Direction d, Direction target, bool clockwise = false)
    {
        int dInt = (int)d;
        int tInt = (int)target;
        if (tInt < dInt)
            tInt += 12;
        int dist = tInt - dInt;
        if (clockwise)
            dist = (12 - dist) % 12;
        return dist;
    }
}
