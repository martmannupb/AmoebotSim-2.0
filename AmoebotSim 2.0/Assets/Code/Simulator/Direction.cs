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
/// <para>
/// See <see cref="DirectionHelpers"/> for additional helper functions.
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
/// Defines extension methods and helpers for the <see cref="Direction"/> enum.
/// </summary>
static class DirectionHelpers
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
        if (d == Direction.NONE) return Direction.NONE;
        return (Direction)(((int)d + 6) % 12);
    }

    /// <summary>
    /// Returns the given direction rotated by 60 degrees times the
    /// given amount in counter-clockwise direction. Negative values
    /// rotate in clockwise direction.
    /// <para>
    /// If <paramref name="d"/> is (not) a cardinal direction, then
    /// the result will also (not) be a cardinal direction.
    /// </para>
    /// </summary>
    /// <param name="d">The current direction.</param>
    /// <param name="amount">The number of times to rotate by 60
    /// degrees in clockwise direction.</param>
    /// <returns>Direction <paramref name="d"/> rotated by 60
    /// degrees times <paramref name="amount"/> in
    /// counter-clockwise direction.</returns>
    public static Direction Rotate60(this Direction d, int amount)
    {
        if (d == Direction.NONE) return Direction.NONE;
        if (amount < 0)
            amount = 5 * Mathf.Abs(amount);
        return (Direction)(((int)d + 2 * amount) % 12);
    }

    /// <summary>
    /// Returns the given direction rotated by 30 degrees times the
    /// given amount in counter-clockwise direction. Negative values
    /// rotate in clockwise direction.
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
    /// degrees times <paramref name="amount"/> in
    /// counter-clockwise direction.</returns>
    public static Direction Rotate30(this Direction d, int amount)
    {
        if (d == Direction.NONE) return Direction.NONE;
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
        if (d == Direction.NONE || target == Direction.NONE) return -1;
        int dInt = (int)d;
        int tInt = (int)target;
        if (tInt < dInt)
            tInt += 12;
        int dist = tInt - dInt;
        if (clockwise)
            dist = (12 - dist) % 12;
        return dist;
    }

    /// <summary>
    /// Adds this direction value to the given direction. The current direction
    /// is interpreted as an amount of rotation relative to the
    /// <see cref="Direction.E"/> direction that is to be added to the given
    /// other direction. This can be used to convert local directions to global
    /// directions.
    /// </summary>
    /// <param name="d">The current direction to be interpreted as an amount
    /// of rotation.</param>
    /// <param name="other">The offset direction to which the first direction
    /// should be added.</param>
    /// <param name="clockwise">If <c>true</c>, interpret the first direction
    /// as clockwise rotation instead of counter-clockwise.</param>
    /// <returns></returns>
    public static Direction AddTo(this Direction d, Direction other, bool clockwise = false)
    {
        if (d == Direction.NONE || other == Direction.NONE) return Direction.NONE;
        return (Direction)(clockwise ? (((int)other + 12 - (int)d) % 12) : (((int)other + (int)d) % 12));
    }

    /// <summary>
    /// Subtracts the given direction from this global direction. The current
    /// direction is interpreted as the result of adding a rotation <c>x</c> to
    /// the given other direction, where <c>x</c> can be clockwise or counter-
    /// clockwise rotation. This method finds and returns <c>x</c>. It can be
    /// used to convert global directions to local directions.
    /// </summary>
    /// <param name="d">The current global direction.</param>
    /// <param name="other">The global direction used as offset.</param>
    /// <param name="clockwise">If <c>true</c>, the resulting direction is
    /// interpreted as clockwise rotation instead of counter-clockwise.</param>
    /// <returns>A direction indicating the amoutn of rotation that must be added
    /// to <paramref name="other"/> to get <paramref name="d"/>.</returns>
    public static Direction Subtract(this Direction d, Direction other, bool clockwise = false)
    {
        if (d == Direction.NONE || other == Direction.NONE) return Direction.NONE;
        return (Direction)(clockwise ? (((int)other + 12 - (int)d) % 12) : (((int)d + 12 - (int)other) % 12));
    }

    /// <summary>
    /// Maps this direction to its integer representation. Every cardinal
    /// direction is mapped to the same value as the secondary direction
    /// obtained by a 30 degree counter-clockwise rotation. Thus, there
    /// are 6 pairs of directions, which are mapped to the integers
    /// <c>0,...,5</c>. The integer values increase with counter-
    /// clockwise rotation. The special direction <see cref="Direction.NONE"/>
    /// is represented by <c>-1</c>.
    /// </summary>
    /// <param name="d">The direction to be mapped to an integer.</param>
    /// <returns>The integer representing the given cardinal or secondary
    /// direction.</returns>
    public static int ToInt(this Direction d)
    {
        return d == Direction.NONE ? -1 : ((int)d / 2);
    }

    /// <summary>
    /// Checks if this is a cardinal direction.
    /// <para>See also <seealso cref="IsSecondary(Direction)"/>.</para>
    /// </summary>
    /// <param name="d">The direction to check.</param>
    /// <returns><c>true</c> if and only if this is a cardinal direction.</returns>
    public static bool IsCardinal(this Direction d)
    {
        return d != Direction.NONE && (((int)d) % 2) == 0;
    }

    /// <summary>
    /// Checks if this is a secondary direction.
    /// <para>See also <seealso cref="IsCardinal(Direction)"/>.</para>
    /// </summary>
    /// <param name="d">The direction to check.</param>
    /// <returns><c>true</c> if and only if this is a secondary direction.</returns>
    public static bool IsSecondary(this Direction d)
    {
        return d != Direction.NONE && !d.IsCardinal();
    }

    /// <summary>
    /// Returns the cardinal direction corresponding to the given integer.
    /// The cardinal directions are numbered <c>0,...,5</c>, with <c>0</c>
    /// being <see cref="Direction.E"/> and values increasing counter-
    /// clockwise.
    /// </summary>
    /// <param name="d">The integer representing a cardinal direction.
    /// If negative, the result will be <see cref="Direction.NONE"/>.</param>
    /// <returns>The cardinal direction identified by the integer <paramref name="d"/>.</returns>
    public static Direction Cardinal(int d)
    {
        return d < 0 ? Direction.NONE : (Direction)((d % 6) * 2);
    }

    /// <summary>
    /// Returns the secondary direction corresponding to the given integer.
    /// The secondary direction for integer <c>i</c> is obtained by rotating
    /// the cardinal direction with index <c>i</c> counter-clockwise by 30
    /// degrees.
    /// </summary>
    /// <param name="d">The integer representing a cardinal direction.
    /// If negative, the result will be <see cref="Direction.NONE"/>.</param>
    /// <returns>The secondary direction corresponding to the cardinal
    /// direction that is identified by the integer <paramref name="d"/>.</returns>
    public static Direction Secondary(int d)
    {
        return d < 0 ? Direction.NONE : (Direction)((d % 6) * 2 + 1);
    }
}
