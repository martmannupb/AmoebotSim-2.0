using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The system-side representation of a Particle.
/// </summary>
public class Particle : IParticleState
{
    // References _____
    public ParticleSystem system;
    public ParticleAlgorithm algorithm;

    // Graphics _____
    public IParticleGraphicsAdapter graphics;

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
    private List<ParticleAttribute> attributes = new List<ParticleAttribute>();
    
    // Messages
    private Queue<Message> messageQueue = new Queue<Message>();

    // Data used by system to coordinate movements
    public ParticleAction scheduledAction = null;
    public bool hasMoved = false;

    public Particle(ParticleSystem system, Vector2Int pos, int compassDir = 0, bool chirality = true)
    {
        this.system = system;

        // Start contracted
        this.pos_head = pos;
        this.pos_tail = pos;
        this.exp_isExpanded = false;
        this.exp_expansionDir = -1;

        this.comDir = compassDir;
        this.chirality = chirality;

        // Graphics
        // Add particle to the system and update the visuals of the particle
        graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
        graphics.AddParticle();
        graphics.Update();
    }

    /// <summary>
    /// This is the main activation method of the particle.
    /// It is implemented by the particle algorithm and should
    /// be called exactly once in each round.
    /// </summary>
    public void Activate()
    {
        algorithm.Activate();
    }


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

    public int GetGlobalExpansionDir()
    {
        return exp_isExpanded ? ParticleSystem_Utils.LocalToGlobalDir(exp_expansionDir, comDir, chirality) : -1;
    }


    /**
     * Messages
     */
    // TODO: Check if these are even needed (could directly call ParticleSystem methods like for the movements)
    // TODO: Implement these

    public void SendMessage(Message msg, int locDir, bool head = true)
    {
        system.SendParticleMessage(this, msg, locDir, head);
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
     * Particle action methods that are used by the system to change the
     * particle's state at the appropriate time. They are NOT part of the
     * ParticleAlgorithm's interface.
     */

    // TODO: Documentation

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

    // TODO: Check if we need to do anything else in these 3 methods
    public void Apply_PushHandover(int locDir)
    {
        Apply_Expand(locDir);
    }

    public void Apply_PullHandoverHead(int locDir)
    {
        Apply_ContractHead();
    }

    public void Apply_PullHandoverTail(int locDir)
    {
        Apply_ContractTail();
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
        attr.SetParticle(this);
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
