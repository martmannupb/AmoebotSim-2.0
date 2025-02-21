// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.



namespace AS2
{

    /// <summary>
    /// Standard initialization method that generates
    /// a connected system of contracted particles with
    /// random holes.
    /// See <see cref="InitializationMethod.GenerateRandomWithHoles(int, float, Initialization.Chirality, Initialization.Compass)"/>.
    /// </summary>
    public class InitRandomWithHoles : InitializationMethod
    {
        public InitRandomWithHoles(AS2.Sim.ParticleSystem system) : base(system)
        {

        }

        public void Generate(int numParticles = 50, float holeProb = 0.3f, Initialization.Chirality chirality = Initialization.Chirality.CounterClockwise, Initialization.Compass compassDir = Initialization.Compass.E)
        {
            GenerateRandomWithHoles(numParticles, holeProb, chirality, compassDir);
        }
    }

} // namespace AS2
