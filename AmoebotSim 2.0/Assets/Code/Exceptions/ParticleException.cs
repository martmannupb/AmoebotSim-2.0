using UnityEngine;

/// <summary>
/// Base class for exceptions thrown because a particle tried
/// to perform an invalid operation or its algorithm code caused
/// a problem.
/// </summary>
public class ParticleException : AmoebotSimException
{
    public ParticleException() { }

    public ParticleException(string msg) : base(msg) { }
}
