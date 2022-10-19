using UnityEngine;

/// <summary>
/// Exception class for the case that an error occurred during the
/// simulation that cannot be directly attributed to a single particle.
/// </summary>
public class SimulationException : SimulatorException
{
    public SimulationException() { }

    public SimulationException(string msg) : base(msg) { }
}
