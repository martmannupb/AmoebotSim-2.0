using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The abstract base class for particle algorithms in the Amoebot model.
/// <para>
/// Every algorithm that should run in the simulation must be implemented
/// as a subclass of the <see cref="ParticleAlgorithm"/> class through its <see cref="Activate"/>
/// method.
/// </para>
/// <para>
/// The subclass constructor must call the base class constructor as follows:
/// <code>
/// public MyClass(Particle p) : base(p) { ... }
/// </code>
/// </para>
/// <para>
/// Particle attributes that represent a part of the particle's state must be implemented
/// using the <see cref="ParticleAttribute"/> subclasses.
/// <para/>
/// <example>
/// Example for attribute initialization in subclass:
/// <code>
/// public class MyParticle : ParticleAlgorithm {
///     ParticleAttribute_Int myIntAttr;
///     public MyParticle(Particle p) : base(p) {
///         myIntAttr = new ParticleAttribute_Int(this, "Fancy display name for myIntAttr", 42);
///     }
/// }
/// </code>
/// </example>
/// </para>
/// </summary>
public abstract class ParticleAlgorithm
{
    // Reference to the particle's system representation
    private Particle particle;

    public ParticleAlgorithm(Particle particle)
    {
        this.particle = particle;
        particle.algorithm = this;
    }

    /// <summary>
    /// This is the main activation method of the particle.
    /// It is called exactly once in each round and should contain
    /// the particle algorithm code.
    /// </summary>
    public abstract void Activate();


    /**
     * Attribute handling. Should not be called by the particle algorithm implementation.
     */

    public void AddAttribute(ParticleAttribute attr)
    {
        particle.AddAttribute(attr);
    }


    /** ====================================================================================================
     * Particle actions defining the API.
     * These methods should be called from the Activate() method.
     * ====================================================================================================
     */

    /**
     * State information retrieval
     */

    /// <summary>
    /// Checks if the particle is currently expanded, i.e., occupies 2 neighboring
    /// grid nodes simultaneously.
    /// <para>See <see cref="IsContracted"/></para>
    /// </summary>
    /// <returns><c>true</c> if and only if the particle is expanded.</returns>
    public bool IsExpanded()
    {
        return particle.exp_isExpanded;
    }

    /// <summary>
    /// Checks if the particle is currently contracted, i.e., occupies exactly one
    /// node of the grid.
    /// <para>See <see cref="IsExpanded"/></para>
    /// </summary>
    /// <returns><c>true</c> if and only if the particle is contracted.</returns>
    public bool IsContracted()
    {
        return !particle.exp_isExpanded;
    }


    /**
     * Messages
     */
    // TODO: Implement these

    public void SendMessage(Message msg, int locDir, bool head = true)
    {
        particle.system.SendParticleMessage(particle, msg, locDir, head);
    }

    public bool HasMsgOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public T GetMsgOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public List<T> GetMsgsOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public T PopMsgOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }

    public List<T> PopMsgsOfType<T>() where T : Message
    {
        throw new System.NotImplementedException();
    }


    /**
     * System information retrieval
     */

    // TODO: Decide on uniform interface for specifying ports (through labels or direction + head/tail (or both))
    // TODO: Add many more helper functions (like firstNbrWithProperty etc.)

    /// <summary>
    /// Checks if this particle has a neighboring particle in the given local direction.
    /// For expanded particles, there are two different nodes in the same local direction,
    /// one seen from the particle's head and one seen from its tail.
    /// <para>See also <see cref="GetNeighborAt(int, bool)"/></para>
    /// </summary>
    /// <param name="locDir">The local direction in which to search for a neighbor particle.</param>
    /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
    /// the particle's tail (only relevant if this particle is expanded).</param>
    /// <returns><c>true</c> if and only if there is a different particle in the specified position.</returns>
    public bool HasNeighborAt(int locDir, bool fromHead = true)
    {
        return particle.system.HasNeighborAt(particle, locDir, fromHead);
    }

    // TODO: What to do if there is no neighbor? Check beforehand, throw exception?
    /// <summary>
    /// Gets this particle's neighbor in the given local direction. The position to
    /// check is determined in the same way as in <see cref="HasNeighborAt(int, bool)"/>.
    /// </summary>
    /// <param name="locDir">The local direction from which to get the neighbor particle.</param>
    /// <param name="fromHead">If <c>true</c>, look from the particle's head, otherwise look from
    /// the particle's tail (only relevant if this particle is expanded).</param>
    /// <returns>The neighboring particle in the specified position.</returns>
    public ParticleAlgorithm GetNeighborAt(int locDir, bool fromHead = true)
    {
        return particle.system.GetNeighborAt(particle, locDir, fromHead).algorithm;
    }


    /**
     * Movement actions
     */

    public void Expand(int locDir)
    {
        particle.system.ExpandParticle(particle, locDir);
    }

    public void ContractHead()
    {
        particle.system.ContractParticleHead(particle);
    }

    public void ContractTail()
    {
        particle.system.ContractParticleTail(particle);
    }

    public void PushHandover(int locDir)
    {
        particle.system.PerformPushHandover(particle, locDir);
    }

    public void PullHandoverHead(int locDir)
    {
        particle.system.PerformPullHandoverHead(particle, locDir);
    }

    public void PullHandoverTail(int locDir)
    {
        particle.system.PerformPullHandoverTail(particle, locDir);
    }
}
