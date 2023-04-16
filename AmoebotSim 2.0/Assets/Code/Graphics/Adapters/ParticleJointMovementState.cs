using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Contains the joint movement information for a single particle in a single round.
    /// </summary>
    public struct ParticleJointMovementState : IEquatable<ParticleJointMovementState>
    {

        // Variables
        /// <summary>
        /// Whether or not this is actually a joint movement.
        /// if <c>false</c>, the <see cref="jointMovementOffset"/>
        /// is invalid unless it is <c>(0, 0)</c>.
        /// </summary>
        public bool isJointMovement;
        /// <summary>
        /// The global offset by which the particle's position
        /// has shifted. This is relative to the particle's own
        /// non-moving part if it performs a movement itself.
        /// </summary>
        public Vector2Int jointMovementOffset;

        /// <summary>
        /// Constant neutral version of the struct, representing
        /// no joint movement.
        /// </summary>
        public static ParticleJointMovementState None = new ParticleJointMovementState(false, new Vector2Int(0, 0));

        public ParticleJointMovementState(bool isJointMovement, Vector2Int jointExpansionOffset)
        {
            this.isJointMovement = isJointMovement;
            this.jointMovementOffset = jointExpansionOffset;
        }


        // Overrides

        public bool Equals(ParticleJointMovementState other)
        {
            return this.isJointMovement == other.isJointMovement && this.jointMovementOffset == other.jointMovementOffset;
        }

        public static bool operator ==(ParticleJointMovementState state1, ParticleJointMovementState state2)
        {
            return state1.isJointMovement == state2.isJointMovement && state1.jointMovementOffset == state2.jointMovementOffset;
        }

        public static bool operator !=(ParticleJointMovementState lhs, ParticleJointMovementState rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(ParticleJointMovementState) && this == (ParticleJointMovementState)obj;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isJointMovement, jointMovementOffset);
        }

        public string Description()
        {
            return "JM: " + isJointMovement + "\nOffset:" + jointMovementOffset;
        }
    }

}