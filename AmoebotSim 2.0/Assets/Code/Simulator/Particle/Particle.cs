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


    public Particle(ParticleSystem system)
    {
        this.system = system;
    }

    public abstract void Activate();

    public bool IsExpanded()
    {
        return exp_isExpanded;
    }

}
