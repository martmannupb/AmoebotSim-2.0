using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A stripped down particle class that is only used for
/// system initialization. The data stored in this class
/// is used to instantiate the proper particles and the
/// associated algorithms when simulation mode is entered.
/// </summary>
public class InitializationParticle : IParticleState
{
    private Vector2Int tailPos;
    private Vector2Int headPos;
    private Direction expansionDir;
    private bool chirality;
    private Direction compassDir;
    public ParticleGraphicsAdapterImpl graphics;
    private ParticleSystem system;

    public InitializationParticle(ParticleSystem system, Vector2Int position, bool chirality, Direction compassDir, Direction expansionDir = Direction.NONE)
    {
        tailPos = position;
        this.chirality = chirality;
        this.compassDir = compassDir;
        this.expansionDir = expansionDir;
        if (expansionDir == Direction.NONE)
            headPos = tailPos;
        else
            headPos = ParticleSystem_Utils.GetNbrInDir(tailPos, expansionDir);
        this.system = system;

        // Add particle to the system and update the visuals of the particle
        graphics = new ParticleGraphicsAdapterImpl(this, system.renderSystem.rendererP);
    }

    public int GetCircuitPinsPerSide()
    {
        return 0;
    }

    public Color GetParticleColor()
    {
        return Color.gray;
    }

    public int GlobalHeadDirectionInt()
    {
        return expansionDir.ToInt();
    }

    public Vector2Int Head()
    {
        return headPos;
    }

    public bool IsExpanded()
    {
        return expansionDir != Direction.NONE;
    }

    public bool IsParticleColorSet()
    {
        return true;
    }

    public Vector2Int Tail()
    {
        return tailPos;
    }
}
