// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.



namespace AS2.Sim
{

    /// <summary>
    /// Abstract base class for all particle attributes.
    /// <para>
    /// Stores a reference to the <see cref="Particle"/> containing
    /// the attribute and the name of the attribute.
    /// </para>
    /// </summary>
    public abstract class ParticleAttributeBase
    {
        /// <summary>
        /// The <see cref="Particle"/> to which the attribute belongs.
        /// </summary>
        protected Particle particle;
        /// <summary>
        /// The unique name of the attribute.
        /// </summary>
        protected string name;

        public ParticleAttributeBase(Particle particle, string name)
        {
            this.particle = particle;
            this.name = name;
        }
    }

} // namespace AS2.Sim
