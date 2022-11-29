using System;
using UnityEngine;

namespace Simulator
{

    [Serializable]
    public struct JointMovementInfo
    {
        public Vector2Int jmOffset;
        public Vector2Int movementOffset;
        public ActionType movementAction;

        public JointMovementInfo(Vector2Int jmOffset, Vector2Int movementOffset, ActionType movementAction)
        {
            this.jmOffset = jmOffset;
            this.movementOffset = movementOffset;
            this.movementAction = movementAction;
        }

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

    public class ValueHistoryJointMovement : ValueHistory<JointMovementInfo>
    {
        public ValueHistoryJointMovement(JointMovementInfo initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

        public ValueHistoryJointMovement(ValueHistorySaveData<JointMovementInfo> data) : base(data) { }
    }

} // namespace Simulator
