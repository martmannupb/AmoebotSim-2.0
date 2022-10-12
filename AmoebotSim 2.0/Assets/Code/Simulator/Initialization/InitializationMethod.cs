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

    public static string Name { get { return ""; } }

    public InitializationMethod(ParticleSystem system)
    {
        this.system = system;
    }

    public InitializationParticle AddParticle(Vector2Int position, Direction headDir = Direction.NONE,
        Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise,
        Initialization.Compass compassDir = Initialization.Compass.E)
    {
        bool chir = true;
        if (chirality == Initialization.Chirality.Clockwise)
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
    /// Returns an array of all currently placed particles. This is useful
    /// for cases where particle parameters have to be set after the
    /// particles were placed and where chirality and compass directions
    /// were chosen randomly.
    /// </summary>
    /// <returns>An array containing all current particles.</returns>
    public InitializationParticle[] GetParticles()
    {
        return system.GetInitParticles();
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

    /// <summary>
    /// Adds contracted particles in the shape of a parallelogram.
    /// The particles are added row by row, where
    /// <paramref name="length"/> determines how many particles are
    /// in a row and <paramref name="height"/> determines how many
    /// rows there are. Rows are offset to form an acute angle
    /// at the start location if <paramref name="acuteAngle"/> is
    /// <c>true</c>, i.e., the offset direction is the main direction
    /// rotated by 60° in counter-clockwise direction. Otherwise, the
    /// offset direction will be rotated by 120° and the angle becomes
    /// obtuse.
    /// </summary>
    /// <param name="startPos">The position at which the first particle
    /// will be placed.</param>
    /// <param name="mainDir">The direction in which rows grow.</param>
    /// <param name="length">The number of particles in each row.</param>
    /// <param name="acuteAngle">If <c>true</c>, the parallelogram's
    /// angle at the first particle is acute (60°), otherwise, it is
    /// obtuse (120°).</param>
    /// <param name="height">The number of rows.</param>
    /// <param name="chirality">The chirality setting for all particles.</param>
    /// <param name="compass">The compass setting for all particles.</param>
    public void PlaceParallelogram(Vector2Int startPos, Direction mainDir, int length, bool acuteAngle = true, int height = 1,
        Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compass = Initialization.Compass.E)
    {
        if (length < 1 || height < 1)
            return;

        if (!mainDir.IsCardinal())
        {
            Log.Warning("Cannot place parallelogram with main direction " + mainDir + ", must be a cardinal direction");
            return;
        }

        Direction secDir = acuteAngle ? mainDir.Rotate60(1) : mainDir.Rotate60(2);

        for (int h = 0; h < height; h++)
        {
            Vector2Int pos = startPos;

            for (int l = 0; l < length; l++)
            {
                AddParticle(pos, Direction.NONE, chirality, compass);
                pos = ParticleSystem_Utils.GetNbrInDir(pos, mainDir);
            }
            startPos = ParticleSystem_Utils.GetNbrInDir(startPos, secDir);
        }
    }

    /// <summary>
    /// Generates a system with a fixed number of particles using a
    /// randomized breadth-first-search algorithm that leaves out some
    /// positions to insert holes. It is guaranteed that the desired
    /// number of particles is placed because if all available locations
    /// are marked as holes, one of them is chosen at random to be
    /// replaced with a particle and keep the algorithm going.
    /// All particles are contracted.
    /// </summary>
    /// <param name="numParticles">The number of particles to place.</param>
    /// <param name="holeProb">The probability of a grid position becoming
    /// a hole when it is first encountered. Note that the final system might
    /// still have holes if this is set to <c>0</c>.</param>
    /// <param name="chirality">The chirality setting for all particles.</param>
    /// <param name="compassDir">The compass setting for all particles.</param>
    public void GenerateRandomWithHoles(int numParticles = 50, float holeProb = 0.3f,
        Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compassDir = Initialization.Compass.E)
    {
        if (numParticles < 1)
            return;

        int n = 1;
        // Always start by adding a particle at position (0, 0)
        List<Vector2Int> candidates = new List<Vector2Int>();
        Vector2Int node = new Vector2Int(0, 0);
        AddParticle(node, Direction.NONE, chirality, compassDir);

        for (int d = 0; d < 6; d++)
            candidates.Add(ParticleSystem_Utils.GetNbrInDir(node, DirectionHelpers.Cardinal(d)));

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();       // Occupied by particles
        HashSet<Vector2Int> excluded = new HashSet<Vector2Int>();       // Reserved for holes
        occupied.Add(node);

        int numExcludedChosen = 0;

        while (n < numParticles)
        {
            // Find next position
            Vector2Int newPos = Vector2Int.zero;
            bool choseExcluded = false;
            if (candidates.Count > 0)
            {
                int randIdx = Random.Range(0, candidates.Count);
                newPos = candidates[randIdx];
                candidates.RemoveAt(randIdx);
            }
            else
            {
                // Choose random excluded position
                int randIdx = Random.Range(0, excluded.Count);
                int i = 0;
                foreach (Vector2Int v in excluded)
                {
                    if (i == randIdx)
                    {
                        newPos = v;
                        break;
                    }
                    i++;
                }
                numExcludedChosen++;
                excluded.Remove(newPos);
                choseExcluded = true;
            }

            // Either use newPos to insert particle or to insert hole
            if (choseExcluded || Random.Range(0.0f, 1.0f) >= holeProb)
            {
                for (int d = 0; d < 6; d++)
                {
                    Vector2Int nbr = ParticleSystem_Utils.GetNbrInDir(newPos, DirectionHelpers.Cardinal(d));
                    if (!occupied.Contains(nbr) && !excluded.Contains(nbr) && !candidates.Contains(nbr))
                        candidates.Add(nbr);
                }

                AddParticle(newPos, Direction.NONE, chirality, compassDir);

                occupied.Add(newPos);
                n++;
            }
            else
            {
                excluded.Add(newPos);
            }
        }
        Log.Debug("Created system with " + n + " particles, had to choose " + numExcludedChosen + " excluded positions");
    }
}
