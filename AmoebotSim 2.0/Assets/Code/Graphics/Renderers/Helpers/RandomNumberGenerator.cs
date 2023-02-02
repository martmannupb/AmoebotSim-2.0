using System;

public class RandomNumberGenerator
{
    private Random _random;
    private readonly int _seed;

    public RandomNumberGenerator(int seed)
    {
        _seed = seed;
        Reset();
    }

    public int Range(int min, int max)
    {
        return _random.Next(min, max);
    }

    public float Range(float min, float max)
    {
        return (float)(min + (_random.NextDouble() * (max - min)));
    }

    public void Reset()
    {
        _random = new Random(_seed);
    }
}