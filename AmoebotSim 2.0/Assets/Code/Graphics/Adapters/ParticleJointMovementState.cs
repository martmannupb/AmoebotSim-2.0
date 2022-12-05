using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Graphics
{

    public struct ParticleJointMovementState : IEquatable<ParticleJointMovementState>
    {

        // Variables
        public bool isJointMovement;
        public Vector2Int jointExpansionOffset;

        public static ParticleJointMovementState None = new ParticleJointMovementState(false, new Vector2Int(0, 0));

        public ParticleJointMovementState(bool isJointMovement, Vector2Int jointExpansionOffset)
        {
            this.isJointMovement = isJointMovement;
            this.jointExpansionOffset = jointExpansionOffset;
        }








        // Overrides

        public bool Equals(ParticleJointMovementState other)
        {
            return this.isJointMovement == other.isJointMovement && this.jointExpansionOffset == other.jointExpansionOffset;
        }


        public static bool operator ==(ParticleJointMovementState state1, ParticleJointMovementState state2)
        {
            return state1.isJointMovement == state2.isJointMovement && state1.jointExpansionOffset == state2.jointExpansionOffset;
        }

        public static bool operator !=(ParticleJointMovementState lhs, ParticleJointMovementState rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(ParticleJointMovementState) && this == (ParticleJointMovementState)obj;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isJointMovement, jointExpansionOffset);
        }

        public string Description()
        {
            return "JM: " + isJointMovement + "\nJEO:" + jointExpansionOffset;
        }
    }

} // namespace AS2.Graphics
