using System;

/// <summary>
/// Simple wrapper around a random number generator that
/// can be reset easily.
/// </summary>
public class RandomNumberGenerator
{
    private Random _random;
    private readonly int _seed;

    public RandomNumberGenerator(int seed)
    {
        _seed = seed;
        Reset();
    }

    /// <summary>
    /// Computes a random integer between <paramref name="min"/>
    /// and <paramref name="max"/>.
    /// </summary>
    /// <param name="min">The minimal value to return.</param>
    /// <param name="max">The maximal value to return.</param>
    /// <returns>A random integer between <paramref name="min"/>
    /// and <paramref name="max"/>.</returns>
    public int Range(int min, int max)
    {
        return _random.Next(min, max);
    }

    /// <summary>
    /// Computes a random float between <paramref name="min"/>
    /// and <paramref name="max"/>.
    /// </summary>
    /// <param name="min">The minimal value to return.</param>
    /// <param name="max">The maximal value to return.</param>
    /// <returns>A random float between <paramref name="min"/>
    /// and <paramref name="max"/>.</returns>
    public float Range(float min, float max)
    {
        return (float)(min + (_random.NextDouble() * (max - min)));
    }

    /// <summary>
    /// Resets the random number generator's state using the
    /// seed it was constructed with.
    /// </summary>
    public void Reset()
    {
        _random = new Random(_seed);
    }
}
