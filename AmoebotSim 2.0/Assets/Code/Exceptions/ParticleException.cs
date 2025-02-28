// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Base class for exceptions thrown because a particle tried
    /// to perform an invalid operation or its algorithm code caused
    /// a problem.
    /// <para>
    /// Stores a reference to the particle that caused the exception.
    /// </para>
    /// </summary>
    public class ParticleException : AmoebotSimException
    {
        /// <summary>
        /// The particle that caused this exception.
        /// <para>
        /// Note that multiple particles may be involved and this
        /// is only the first particle in the processing of which
        /// the error was detected.
        /// </para>
        /// </summary>
        public Particle particle;

        public ParticleException() { }

        public ParticleException(Particle particle)
        {
            this.particle = particle;
        }

        public ParticleException(string msg) : base(msg) { }

        public ParticleException(Particle particle, string msg) : base(msg)
        {
            this.particle = particle;
        }
    }

} // namespace AS2.Sim
