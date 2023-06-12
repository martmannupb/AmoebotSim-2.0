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
        /// <summary>
        /// Creates a new particle for the specified algorithm.
        /// </summary>
        /// <param name="system">The system to which the particle will belong.</param>
        /// <param name="algorithmId">The ID name of the algorithm that will control
        /// the particle's behavior.</param>
        /// <param name="position">The grid position of the particle's tail.</param>
        /// <param name="compassDir">The global direction representing the particle's
        /// compass, i.e., the direction it believes to be <see cref="Direction.E"/>.</param>
        /// <param name="chirality">If <c>true</c>, the particle has the same
        /// counter-clockwise positive rotation direction as the global coordinate
        /// system, otherwise it is inverted.</param>
        /// <param name="headDir">The global direction of the particle's head if it
        /// should be expanded.</param>
        /// <returns>A new particle with an attached algorithm instance.</returns>
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
            p.inConstructor = true;
            AlgorithmManager.Instance.Instantiate(algorithmId, p);
            p.inConstructor = false;
            p.isActive = false;
            p.InitWithAlgorithm();
            p.graphics.AddParticle(new ParticleMovementState(p.Head(), p.Tail(), p.IsExpanded(), p.GlobalHeadDirectionInt(), ParticleJointMovementState.None));
            p.graphics.UpdateReset();

            return p;
        }

        /// <summary>
        /// Creates a new particle for the specified algorithm using the
        /// given <see cref="InitializationParticle"/> as a template.
        /// </summary>
        /// <param name="system">The system to which the particle will belong.</param>
        /// <param name="algorithmId">The ID name of the algorithm that will control
        /// the particle's behavior.</param>
        /// <param name="ip">The <see cref="InitializationParticle"/> that
        /// should be used as a template to copy the position, expansion state,
        /// chirality, compass direction and algorithm parameters from.</param>
        /// <param name="initialize">If <c>true</c>, call the algorithm's
        /// <c>Init</c> method with the parameters stored in <paramref name="ip"/>.</param>
        /// <returns></returns>
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
    }

} // namespace AS2.Sim
