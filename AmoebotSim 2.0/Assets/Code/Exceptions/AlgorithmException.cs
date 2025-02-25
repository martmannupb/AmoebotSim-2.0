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
    /// Exception class for the case that an error occurred in
    /// the algorithm code of a particle. This covers all errors
    /// that cannot be recognized by the simulator because the error
    /// occurred directly in the algorithm code.
    /// </summary>
    public class AlgorithmException : ParticleException
    {
        public AlgorithmException() { }

        public AlgorithmException(Particle p) : base(p) { }

        public AlgorithmException(string msg) : base(msg) { }

        public AlgorithmException(Particle p, string msg) : base(p, msg) { }
    }

} // namespace AS2.Sim
