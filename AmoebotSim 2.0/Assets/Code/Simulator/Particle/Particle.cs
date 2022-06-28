using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The abstract base class for particles in the Amoebot model.
/// <para>
/// Every algorithm that should run in the simulation must be implemented
/// as a subclass of the <see cref="Particle"/> class through its <see cref="Activate"/>
/// method.
/// Particle attributes that represent a part of the particle's state must be implemented
/// using the <see cref="ParticleAttribute"/> subclasses.
/// </para>
/// <example>
/// Example for attribute initialization in subclass:
/// <code>
/// public class MyParticle : Particle {
///     ParticleAttribute_Int myIntAttr;
///     public MyParticle(ParticleSystem system, int x = 0, int y = 0) : base(system, x, y) {
///         myIntAttr = new ParticleAttribute_Int(this, "Fancy display name for myIntAttr", 42);
///     }
/// }
/// </code>
/// </example>
/// </summary>
public abstract class Particle
{

    // References _____
    private ParticleSystem system;

    // Data _____
    // General
    public readonly int comDir;
    public readonly bool chirality;

    // State _____
    public Vector2Int pos_head;
    public Vector2Int pos_tail;
    // Expansion
    public bool exp_isExpanded;
    public int exp_expansionDir;
    // Attributes
    public List<ParticleAttribute> attributes = new List<ParticleAttribute>();
    // Messages
    public Queue<Message> messageQueue = new Queue<Message>();

    // Data used by system to coordinate movements
    public ParticleAction scheduledAction = null;


    public Particle(ParticleSystem system, Vector2Int pos)
    {
        this.system = system;

        // Start contracted
        this.pos_head = pos;
        this.pos_tail = pos;
        this.exp_isExpanded = false;
        this.exp_expansionDir = -1;

        // TODO: Add these as parameters (?)
        this.comDir = 0;
        this.chirality = true;
    }

    /// <summary>
    /// This is the main activation method of the particle.
    /// It is called exactly once in each round and should contain
    /// the particle algorithm code.
    /// </summary>
    public abstract void Activate();


    /**
     * State information retrieval
     */

    /// <summary>
    /// Returns the grid node on which the particle's head is currently positioned.
    /// When a particle expands, its part on the node it expands to becomes the
    /// particle's head. For contracted particles, the head and tail are on the same node.
    /// <para>See <see cref="Tail"/></para>
    /// </summary>
    /// <returns>The node on which the particle's head is currently positioned.</returns>
    public Vector2Int Head()
    {
        return pos_head;
    }

    /// <summary>
    /// Returns the grid node on which the particle's tail is currently positioned.
    /// When a particle expands, its part on the node it originally occupies becomes
    /// the particle's tail. For contracted particles, the head and tail are on the same node.
    /// <para>See <see cref="Head"/></para>
    /// </summary>
    /// <returns>The node on which the particle's tail is currently positioned.</returns>
    public Vector2Int Tail()
    {
        return pos_tail;
    }

    /// <summary>
    /// Checks if the particle is currently expanded, i.e., occupies 2 neighboring
    /// grid nodes simultaneously.
    /// <para>See <see cref="IsContracted"/></para>
    /// </summary>
    /// <returns><c>true</c> if and only if the particle is expanded.</returns>
    public bool IsExpanded()
    {
        return exp_isExpanded;
    }

    /// <summary>
    /// Checks if the particle is currently contracted, i.e., occupies exactly one
    /// node of the grid.
    /// <para>See <see cref="IsExpanded"/></para>
    /// </summary>
    /// <returns><c>true</c> if and only if the particle is contracted.</returns>
    public bool IsContracted()
    {
        return !exp_isExpanded;
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

    // TODO: Documentation
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
        return system.HasNeighborAt(this, locDir, fromHead);
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
    public Particle GetNeighborAt(int locDir, bool fromHead = true)
    {
        return system.GetNeighborAt(this, locDir, fromHead);
    }


    /**
     * Particle actions defining the API.
     * These methods should be called from the Activate() method.
     */

    public void Expand(int locDir)
    {
        system.ExpandParticle(this, locDir);
    }

    public void ContractHead()
    {
        system.ContractParticleHead(this);
    }

    public void ContractTail()
    {
        system.ContractParticleTail(this);
    }

    public void PushHandover(int locDir)
    {
        system.PerformPushHandover(this, locDir);
    }

    public void PullHandoverHead(int locDir)
    {
        system.PerformPullHandoverHead(this, locDir);
    }

    public void PullHandoverTail(int locDir)
    {
        system.PerformPullHandoverTail(this, locDir);
    }

    public void SendMessage(Message msg, int locDir, bool head = true)
    {
        system.SendParticleMessage(this, msg, locDir, head);
    }


    /**
     * System state change functions that are NOT part of the API.
     * These methods are called by the simulation framework to update
     * the particle state at the appropriate time. They should not be
     * called in the Activate method.
     */

    public void Apply_Expand(int locDir)
    {
        exp_isExpanded = true;
        exp_expansionDir = locDir;
        pos_head = ParticleSystem_Utils.GetNbrInDir(pos_head, ParticleSystem_Utils.LocalToGlobalDir(locDir, comDir, chirality));
    }

    public void Apply_ContractHead()
    {
        exp_isExpanded = false;
        exp_expansionDir = -1;
        pos_tail = pos_head;
    }

    public void Apply_ContractTail()
    {
        exp_isExpanded = false;
        exp_expansionDir = -1;
        pos_head = pos_tail;
    }

    public void Apply_PushHandover(int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void Apply_PullHandoverHead(int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void Apply_PullHandoverTail(int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void Apply_SendMessage(Message msg, int locDir, bool head = true)
    {
        throw new System.NotImplementedException();
    }


    /**
     * Attribute handling
     */

    /// <summary>
    /// Adds the given <see cref="ParticleAttribute"/> to the particle's list of
    /// attributes. All attributes on this list will be displayed and editable in
    /// the simulation UI. This function is called by the
    /// <see cref="ParticleAttribute"/> constructor when it is created by this particle.
    /// </summary>
    /// <param name="attr">The attribute to add to this particle's attribute list.</param>
    public void AddAttribute(ParticleAttribute attr)
    {
        attributes.Add(attr);
    }

    /// <summary>
    /// Gets this particle's list of <see cref="ParticleAttribute"/>s. These
    /// attributes are supposed to be shown and edited in the simulation UI.
    /// </summary>
    /// <returns>The list of <see cref="ParticleAttribute"/>s belonging to this particle.</returns>
    public List<ParticleAttribute> GetAttributes()
    {
        return attributes;
    }
}
