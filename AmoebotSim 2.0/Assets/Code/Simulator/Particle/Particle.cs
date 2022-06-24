using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Particle
{

    // References _____
    private ParticleSystem system;

    // Data _____
    // General
    public int comDir;
    public bool chirality;

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


    public Particle(ParticleSystem system, int x = 0, int y = 0)
    {
        this.system = system;

        // Start contracted
        this.pos_head = new Vector2Int(x, y);
        this.pos_tail = new Vector2Int(x, y);
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


    /**
     * System information retrieval
     */

    // TODO: Documentation
    // TODO: Decide on uniform interface for specifying ports (through labels or direction + head/tail (or both))
    public bool HasNeighborAt(int locDir, bool head = true)
    {
        throw new System.NotImplementedException();
    }

    public Particle GetNeighborAt(int locDir, bool head = true)
    {
        throw new System.NotImplementedException();
    }


    /**
     * Particle actions
     */

    public void Expand(int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void ContractHead()
    {
        throw new System.NotImplementedException();
    }

    public void ContractTail()
    {
        throw new System.NotImplementedException();
    }

    public void PushHandover(int locDir)
    {
        throw new System.NotImplementedException();
    }

    public void PullHandoverHead()
    {
        throw new System.NotImplementedException();
    }

    public void PullHandoverTail()
    {
        throw new System.NotImplementedException();
    }

    public void SendMessage(Message msg, int locDir, bool head = true)
    {
        throw new System.NotImplementedException();
    }
}
