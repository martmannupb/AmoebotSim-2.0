using UnityEngine;

namespace Simulator
{

    /// <summary>
    /// Base class for exceptions thrown because a particle tried
    /// to perform an invalid operation or its algorithm code caused
    /// a problem.
    /// </summary>
    public class ParticleException : AmoebotSimException
    {
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

} // namespace Simulator
