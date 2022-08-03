using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Maybe some ParticleAlgorithm implementations may specify parameters?
// TODO: Find way to prevent developers from having to "register" their algorithms here

/// <summary>
/// Factory for instantiating <see cref="Particle"/>s with
/// specific <see cref="ParticleAlgorithm"/> implementations
/// attached to them.
/// <para>
/// To be called by <see cref="ParticleSystem"/> to initialize
/// the simulation.
/// </para>
/// </summary>
public class ParticleFactory
{
    public static Particle CreateExampleParticle(ParticleSystem system, Vector2Int position, int compassDir = 0, bool chirality = true)
    {
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new ExampleParticle(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        return p;
    }

    public static Particle CreateLineFormationParticleSeq(ParticleSystem system, Vector2Int position, int compassDir = 0, bool chirality = true)
    {
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new LineFormationParticleSeq(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        return p;
    }

    public static Particle CreateLineFormationParticleSync(ParticleSystem system, Vector2Int position, int compassDir = 0, bool chirality = true)
    {
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new LineFormationParticleSync(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        return p;
    }

    public static Particle CreateLeaderElectionParticle(ParticleSystem system , Vector2Int position, int compassDir = 0, bool chirality = true)
    {
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new LeaderElectionParticle(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        return p;
    }

    public static Particle CreateChiralityAndCompassParticle(ParticleSystem system, Vector2Int position, int compassDir = 0, bool chirality = true)
    {
        Particle p = new Particle(system, position, Random.Range(0, 6), Random.Range(0f, 1f) <= 0.5f);
        p.isActive = true;
        ChiralityAndCompassParticle alg = new ChiralityAndCompassParticle(p);
        alg.realChirality.SetValue(p.chirality);
        alg.realCompassDir.SetValue(p.comDir);
        p.InitWithAlgorithm();
        p.isActive = false;
        return p;
    }
}
