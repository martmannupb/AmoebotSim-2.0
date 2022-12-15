using System.Collections;
using System.Collections.Generic;
using AS2.Algos.BoundaryTest;
using AS2.Algos.ChiralityCompass;
using AS2.Algos.ExpandedCircuitTest;
using AS2.Algos.JMTest;
using AS2.Algos.LeaderElection;
using AS2.Algos.LineFormation;

using AS2.Visuals;
using UnityEngine;

namespace AS2.Sim
{

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
        public static Particle CreateParticle(ParticleSystem system, string algorithmId, Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true, Direction headDir = Direction.NONE)
        {
            if (compassDir == Direction.NONE)
                compassDir = DirectionHelpers.Cardinal(0);
            if (headDir != Direction.NONE)
            {
                headDir = ParticleSystem_Utils.GlobalToLocalDir(headDir, compassDir, chirality);
            }
            Particle p = new Particle(system, position, compassDir, chirality, headDir);
            p.isActive = true;
            AlgorithmManager.Instance.Instantiate(algorithmId, p);
            p.isActive = false;
            p.InitWithAlgorithm();
            p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
            p.graphics.UpdateReset();

            return p;
        }

        public static Particle CreateParticle(ParticleSystem system, string algorithmId, InitializationParticle ip, bool initialize = false)
        {
            Particle p = CreateParticle(system, algorithmId, ip.Tail(), ip.CompassDir, ip.Chirality, ip.ExpansionDir);
            if (initialize)
            {
                p.isActive = true;
                AlgorithmManager.Instance.Initialize(algorithmId, p.algorithm, ip.GetParameterValues());
                p.isActive = false;
            }
            return p;
        }

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

        public static Particle CreateLineFormationParticleSync(ParticleSystem system, Vector2Int position, bool leader, Direction compassDir = Direction.NONE, bool chirality = true)
        {
            if (compassDir == Direction.NONE)
                compassDir = DirectionHelpers.Cardinal(0);
            Particle p = new Particle(system, position, compassDir, chirality);
            p.isActive = true;
            LineFormationParticleSync algo = new LineFormationParticleSync(p);
            algo.Init(leader);
            p.isActive = false;
            p.InitWithAlgorithm();
            p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
            p.graphics.UpdateReset();
            return p;
        }

        public static Particle CreateLeaderElectionParticle(ParticleSystem system, Vector2Int position, Direction compassDir = Direction.NONE, bool chirality = true)
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
            alg.Init(p.chirality, p.comDir);
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
            new BoundaryTestParticle(p);
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
            JMTestParticle algo = new JMTestParticle(p);
            algo.Init(mode, role);
            p.isActive = false;
            p.InitWithAlgorithm();
            p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
            p.graphics.UpdateReset();
            return p;
        }
    }

} // namespace AS2.Sim
