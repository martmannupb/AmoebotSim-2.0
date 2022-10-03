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
    public static Particle CreateExpandedTestParticle(ParticleSystem system, Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true)
    {
        if (compassDir == Direction.NONE)
            compassDir = DirectionHelpers.Cardinal(0);
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new ExpandedCircuitTestParticle(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        p.graphics.UpdateReset();
        return p;
    }

    public static Particle CreateLineFormationParticleSync(ParticleSystem system, Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true)
    {
        if (compassDir == Direction.NONE)
            compassDir = DirectionHelpers.Cardinal(0);
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new LineFormationParticleSync(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        p.graphics.UpdateReset();
        return p;
    }

    public static Particle CreateLeaderElectionParticle(ParticleSystem system , Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true)
    {
        if (compassDir == Direction.NONE)
            compassDir = DirectionHelpers.Cardinal(0);
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new LeaderElectionParticle(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        p.graphics.UpdateReset();
        return p;
    }

    public static Particle CreateChiralityAndCompassParticle(ParticleSystem system, Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true)
    {
        Particle p = new Particle(system, position, DirectionHelpers.Cardinal(Random.Range(0, 6)), Random.Range(0f, 1f) <= 0.5f);
        p.isActive = true;
        ChiralityAndCompassParticle alg = new ChiralityAndCompassParticle(p);
        alg.realChirality.SetValue(p.chirality);
        alg.realCompassDir.SetValue(p.comDir);
        p.InitWithAlgorithm();
        p.isActive = false;
        p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        p.graphics.UpdateReset();
        return p;
    }

    public static Particle CreateBoundaryTestParticle(ParticleSystem system, Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true)
    {
        if (compassDir == Direction.NONE)
            compassDir = DirectionHelpers.Cardinal(0);
        Particle p = new Particle(system, position, compassDir, chirality);
        p.isActive = true;
        new BoundaryTestAlgo.BoundaryTestParticle(p);
        p.isActive = false;
        p.InitWithAlgorithm();
        p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        p.graphics.UpdateReset();
        return p;
    }

    public static Particle CreateJMTestParticle(ParticleSystem system, Vector2Int position, int mode, int role, Direction compassDir = Direction.NONE, bool chirality = true, Direction initialHeadDir = Direction.NONE)
    {
        if (compassDir == Direction.NONE)
            compassDir = DirectionHelpers.Cardinal(0);
        Particle p = new Particle(system, position, compassDir, chirality, initialHeadDir);
        p.isActive = true;
        new JMTestParticle(p, mode, role);
        p.isActive = false;
        p.InitWithAlgorithm();
        p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
        p.graphics.UpdateReset();
        return p;
    }
}
