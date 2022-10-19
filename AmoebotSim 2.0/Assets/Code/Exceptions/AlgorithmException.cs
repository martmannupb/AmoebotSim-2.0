using UnityEngine;

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
