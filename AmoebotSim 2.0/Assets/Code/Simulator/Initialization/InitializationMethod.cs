using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Documentation
/// <summary>
/// Base class for algorithms that initialize a particle system.
/// Inherit from this class and implement a method called
/// <c>Generate</c> to create an initialization algorithm.
/// </summary>
public abstract class InitializationMethod
{
    private ParticleSystem system;

    public InitializationMethod(ParticleSystem system)
    {
        this.system = system;
    }

    public InitializationParticle AddParticle(Vector2Int position, Direction headDir = Direction.NONE,
        Initialization.Chirality chirality = Initialization.Chirality.Clockwise,
        Initialization.Compass compassDir = Initialization.Compass.E)
    {
        bool chir = true;
        if (chirality == Initialization.Chirality.CounterClockwise)
            chir = false;
        else if (chirality == Initialization.Chirality.Random)
            chir = Random.Range(0, 2) == 0;

        Direction comDir = compassDir == Initialization.Compass.Random ?
            DirectionHelpers.Cardinal(Random.Range(0, 6)) :
            DirectionHelpers.Cardinal((int)compassDir);

        return system.AddInitParticle(position, chir, comDir, headDir);
    }

    /// <summary>
    /// Tries to get the <see cref="InitializationParticle"/> at the given position.
    /// </summary>
    /// <param name="position">The grid position at which to look for the particle.</param>
    /// <param name="particle">The particle at the given position, if it exists,
    /// otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if and only if a particle was found at the given position.</returns>
    public bool TryGetParticleAt(Vector2Int position, out InitializationParticle particle)
    {
        return system.TryGetInitParticleAt(position, out particle);
    }
}
