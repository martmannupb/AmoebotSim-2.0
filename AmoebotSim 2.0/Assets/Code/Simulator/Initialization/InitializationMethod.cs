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

    public static string Name { get { return ""; } }

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

    /// <summary>
    /// Returns the current number of generic parameters.
    /// </summary>
    /// <returns>The number of generic parameters that each
    /// particle currently has.</returns>
    public int NumGenericParameters()
    {
        return system.NumGenericParameters;
    }

    /// <summary>
    /// Adds a new generic parameter to all particles.
    /// </summary>
    /// <param name="initialValue">The initial value
    /// of the new parameter.</param>
    public void AddGenericParameter(int initialValue = 0)
    {
        system.AddGenericParameter(initialValue);
    }

    /// <summary>
    /// Removes the specified generic parameter from
    /// all particles.
    /// </summary>
    /// <param name="index">The index of the generic
    /// parameter to be removed.</param>
    public void RemoveGenericParameter(int index)
    {
        system.RemoveGenericParameter(index);
    }

    /// <summary>
    /// Assigns values to the generic parameters of a fixed number of particles.
    /// The values of the other particles can be reset to 0 if desired. Each
    /// chosen particle's value is set to a random value between
    /// <paramref name="minVal"/> and <paramref name="maxVal"/>, inclusively.
    /// </summary>
    /// <param name="paramIdx">The index of the generic parameter to set.</param>
    /// <param name="minVal">The minimum value to be set.</param>
    /// <param name="maxVal">The maximum (inclusive) value to be set. Set to
    /// the same value as <paramref name="minVal"/> to fix the value.</param>
    /// <param name="numParticles">The number of particles to get assigned
    /// a new value.</param>
    /// <param name="reset">Flag indicating whether the parameter values should
    /// be reset to 0 before assigning the new values.</param>
    public void SetGenericParameterFixed(int paramIdx, int minVal, int maxVal, int numParticles, bool reset = true)
    {
        system.SetGenericParameterFixed(paramIdx, minVal, maxVal, numParticles, reset);
    }

    /// <summary>
    /// Assigns values to the generic parameters of a fixed fraction of particles.
    /// The values of the other particles can be reset to 0 if desired. Each
    /// chosen particle's value is set to a random value between
    /// <paramref name="minVal"/> and <paramref name="maxVal"/>, inclusively.
    /// </summary>
    /// <param name="paramIdx">The index of the generic parameter to set.</param>
    /// <param name="minVal">The minimum value to be set.</param>
    /// <param name="maxVal">The maximum (inclusive) value to be set. Set to
    /// the same value as <paramref name="minVal"/> to fix the value.</param>
    /// <param name="fraction">The fraction of particles to receive a new value.</param>
    /// <param name="reset">Flag indicating whether the parameter values should
    /// be reset to 0 before assigning the new values.</param>
    public void SetGenericParameterFraction(int paramIdx, int minVal, int maxVal, float fraction, bool reset = true)
    {
        system.SetGenericParameterFraction(paramIdx, minVal, maxVal, fraction, reset);
    }

    /// <summary>
    /// Assigns values to the generic parameters of randomly chosen particles.
    /// The values of the other particles can be reset to 0 if desired. Each
    /// chosen particle's value is set to a random value between
    /// <paramref name="minVal"/> and <paramref name="maxVal"/>, inclusively.
    /// </summary>
    /// <param name="paramIdx">The index of the generic parameter to set.</param>
    /// <param name="minVal">The minimum value to be set.</param>
    /// <param name="maxVal">The maximum (inclusive) value to be set. Set to
    /// the same value as <paramref name="minVal"/> to fix the value.</param>
    /// <param name="prob">The probability with which each particle is chosen to
    /// get a new value.</param>
    /// <param name="reset">Flag indicating whether the parameter values should
    /// be reset to 0 before assigning the new values.</param>
    public void SetGenericParameterProb(int paramIdx, int minVal, int maxVal, float prob, bool reset = true)
    {
        system.SetGenericParameterProb(paramIdx, minVal, maxVal, prob, reset);
    }
}
