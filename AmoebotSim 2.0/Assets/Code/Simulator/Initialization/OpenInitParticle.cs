using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialization of the <see cref="InitializationParticle"/>
/// class that provides direct access to some of its protected
/// members. Should only be used by the system because setting
/// these values directly can lead to inconsistent states.
/// </summary>
public class OpenInitParticle : InitializationParticle
{
    public OpenInitParticle(ParticleSystem system, Vector2Int position, bool chirality, Direction compassDir, Direction expansionDir = Direction.NONE)
        : base(system, position, chirality, compassDir, expansionDir) { }

    public Vector2Int TailPosDirect
    {
        get { return tailPos; }
        set { tailPos = value; }
    }

    public Vector2Int HeadPosDirect
    {
        get { return headPos; }
        set { headPos = value; }
    }

    public Direction ExpansionDirDirect
    {
        get { return expansionDir; }
        set { expansionDir = value; }
    }
}
