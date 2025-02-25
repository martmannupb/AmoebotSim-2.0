// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Serializable representation of an
    /// <see cref="Sim.InitializationParticle"/>'s state.
    /// <para>
    /// Attributes are stored separately by type.
    /// </para>
    /// </summary>
    [Serializable]
    public class InitParticleSaveData
    {
        /// <summary>
        /// The grid position of the particle's tail.
        /// </summary>
        public Vector2Int tailPos;
        /// <summary>
        /// The global head direction of the particle.
        /// </summary>
        public Direction expansionDir;
        /// <summary>
        /// The chirality of the particle.
        /// </summary>
        public bool chirality;
        /// <summary>
        /// The compass direction of the particle.
        /// </summary>
        public Direction compassDir;

        // Attribute data, sorted by type
        public List<ParticleAttributeSaveData<bool>> boolAttributes;
        public List<ParticleAttributeSaveData<Direction>> dirAttributes;
        public List<ParticleAttributeSaveData<float>> floatAttributes;
        public List<ParticleAttributeSaveData<int>> intAttributes;
        public List<ParticleAttributeEnumSaveData> enumAttributes;
        public List<ParticleAttributePCSaveData> pcAttributes;
        public List<ParticleAttributeSaveData<string>> stringAttributes;
    }

} // namespace AS2
