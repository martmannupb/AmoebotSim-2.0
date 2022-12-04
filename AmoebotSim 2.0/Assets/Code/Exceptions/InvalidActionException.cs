using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Exception class for the case that a particle tries to
    /// perform an invalid action like calling a method on a
    /// neighbor particle or in the wrong phase, or scheduling
    /// an impossible movement.
    /// </summary>
    public class InvalidActionException : ParticleException
    {
        public InvalidActionException() { }

        public InvalidActionException(Particle p) : base(p) { }

        public InvalidActionException(string msg) : base(msg) { }

        public InvalidActionException(Particle p, string msg) : base(p, msg) { }
    }

} // namespace AS2.Sim
