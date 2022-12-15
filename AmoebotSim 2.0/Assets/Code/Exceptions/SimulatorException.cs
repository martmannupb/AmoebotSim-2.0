using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Base class for exceptions thrown by the simulator due to
    /// a problem during the simulation or invalid data or usage.
    /// </summary>
    public class SimulatorException : AmoebotSimException
    {
        public SimulatorException() { }

        public SimulatorException(string msg) : base(msg) { }
    }

} // namespace AS2.Sim
