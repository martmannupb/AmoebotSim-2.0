using System;
using UnityEngine;

namespace Simulator
{

    /// <summary>
    /// Base class for exceptions thrown due to unintended
    /// behavior of the simulator.
    /// </summary>
    public class AmoebotSimException : Exception
    {
        public AmoebotSimException() { }

        public AmoebotSimException(string msg) : base(msg) { }
    }

} // namespace Simulator
