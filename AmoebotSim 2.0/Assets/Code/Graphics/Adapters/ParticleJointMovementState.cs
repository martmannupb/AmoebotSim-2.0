using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ParticleJointMovementState : IEquatable<ParticleJointMovementState>
{

    // Variables
    public bool isJointMovement;
    public Vector2Int jointExpansionOffset;
    public int contractionDir;

    public static ParticleJointMovementState None = new ParticleJointMovementState(false, new Vector2Int(0, 0), -1);

    public ParticleJointMovementState(bool isJointMovement, Vector2Int jointExpansionOffset, int contractionDir = -1)
    {
        this.isJointMovement = isJointMovement;
        this.jointExpansionOffset = jointExpansionOffset;
        this.contractionDir = contractionDir;
    }








    // Overrides

    public bool Equals(ParticleJointMovementState other)
    {
        return this.isJointMovement == other.isJointMovement && this.jointExpansionOffset == other.jointExpansionOffset && this.contractionDir == other.contractionDir;
    }


    public static bool operator ==(ParticleJointMovementState state1, ParticleJointMovementState state2)
    {
        return state1.isJointMovement == state2.isJointMovement && state1.jointExpansionOffset == state2.jointExpansionOffset && state1.contractionDir == state2.contractionDir;
    }

    public static bool operator !=(ParticleJointMovementState lhs, ParticleJointMovementState rhs) => !(lhs == rhs);
    
    public override bool Equals(object obj)
    {
        return obj.GetType() == typeof(ParticleJointMovementState) && this == (ParticleJointMovementState)obj;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(isJointMovement, jointExpansionOffset, contractionDir);
    }

    public string Description()
    {
        return "JM: " + isJointMovement + "\nJEO:" + jointExpansionOffset + "\nCD: " + contractionDir;
    }
}


