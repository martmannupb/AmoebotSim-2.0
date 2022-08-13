using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoundaryTestAlgo
{

    /// <summary>
    /// Implementation of the inner outer boundary test from
    /// https://arxiv.org/abs/2205.02610v1.
    /// <para>
    /// Common chirality and compass alignment are assumed. The particles
    /// determine whether or not they are part of a boundary, elect a leader
    /// on each boundary, and then test whether their boundary is an inner
    /// or the outer boundary.
    /// </para>
    /// <para>
    /// The phases are synchronized by periodic beeps on the global circuit
    /// by particles that have not yet finished their current phase. All
    /// particles terminate once the boundary test has finished on each
    /// boundary.
    /// </para>
    /// </summary>
    public class BoundaryTestParticle : ParticleAlgorithm
    {
        private ParticleAttribute<bool> firstActivation;    // Flag used to setup data on the very first activation (only used once)
        private ParticleAttribute<int> round;               // Round counter used to synchronize the particles in all phases
        private ParticleAttribute<int> numBoundaries;       // Number of boundaries the particle is a part of. Can be 0-3

        public BoundaryTestParticle(Particle p) : base(p)
        {
            firstActivation = CreateAttributeBool("First Activation", true);
            round = CreateAttributeInt("Round", 0);
            numBoundaries = CreateAttributeInt("# Boundaries", -1);
        }

        public override int PinsPerEdge => 4;

        public override void Activate()
        {
            if (firstActivation)
            {
                FirstActivation();
                firstActivation.SetValue(false);
                return;
            }
        }

        private void FirstActivation()
        {
            // Determine how many boundaries we are on and set up the initial pin configuration
        }
    }

} // namespace BoundaryTestAlgo
