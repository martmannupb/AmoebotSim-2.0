using System;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Serializable representation of the joint movement info
    /// describing a single particle's movement.
    /// </summary>
    [Serializable]
    public struct JointMovementInfo
    {
        /// <summary>
        /// The global movement of the particle's
        /// stationary part.
        /// </summary>
        public Vector2Int jmOffset;
        /// <summary>
        /// The local offset the particle's own movement
        /// (expansion or contraction) applies to neighbors
        /// bonded to its moving part.
        /// </summary>
        public Vector2Int movementOffset;
        /// <summary>
        /// The type of movement performed by the particle.
        /// </summary>
        public ActionType movementAction;

        public JointMovementInfo(Vector2Int jmOffset, Vector2Int movementOffset, ActionType movementAction)
        {
            this.jmOffset = jmOffset;
            this.movementOffset = movementOffset;
            this.movementAction = movementAction;
        }

        /// <summary>
        /// An empty record specifying no movement at all.
        /// </summary>
        public static JointMovementInfo Empty = new JointMovementInfo(Vector2Int.zero, Vector2Int.zero, ActionType.NULL);

        public static bool operator ==(JointMovementInfo i1, JointMovementInfo i2)
        {
            return i1.jmOffset == i2.jmOffset && i1.movementOffset == i2.movementOffset && i1.movementAction == i2.movementAction;
        }

        public static bool operator !=(JointMovementInfo i1, JointMovementInfo i2)
        {
            return !(i1 == i2);
        }

        public override bool Equals(object obj)
        {
            return obj != null && this.GetType().Equals(obj.GetType()) && this == (JointMovementInfo)obj;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(jmOffset, movementOffset, movementAction);
        }
    }

    /// <summary>
    /// Implementation of <see cref="ValueHistory{T}"/> storing
    /// <see cref="JointMovementInfo"/> structs.
    /// </summary>
    public class ValueHistoryJointMovement : ValueHistory<JointMovementInfo>
    {
        public ValueHistoryJointMovement(JointMovementInfo initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

        public ValueHistoryJointMovement(ValueHistorySaveData<JointMovementInfo> data) : base(data) { }
    }

} // namespace AS2.Sim
