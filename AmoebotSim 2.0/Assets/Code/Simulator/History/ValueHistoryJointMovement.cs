using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BondMovementInfo
{
    public Vector2Int start1;
    public Vector2Int end1;
    public Vector2Int start2;
    public Vector2Int end2;

    public BondMovementInfo(Vector2Int start1, Vector2Int end1, Vector2Int start2, Vector2Int end2)
    {
        this.start1 = start1;
        this.end1 = end1;
        this.start2 = start2;
        this.end2 = end2;
    }

    public static BondMovementInfo Empty = new BondMovementInfo(Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero);

    public static bool operator==(BondMovementInfo i1, BondMovementInfo i2)
    {
        return i1.start1 == i2.start1 && i1.end1 == i2.end1 && i1.start2 == i2.start2 && i1.end2 == i2.end2;
    }

    public static bool operator!=(BondMovementInfo i1, BondMovementInfo i2)
    {
        return i1.start1 != i2.start1 || i1.end1 != i2.end1 || i1.start2 != i2.start2 || i1.end2 != i2.end2;
    }

    public override bool Equals(object obj)
    {
        return obj != null && this.GetType().Equals(obj.GetType()) && this == (BondMovementInfo)obj;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(start1, end1, start2, end2);
    }
}

[Serializable]
public struct JointMovementInfo
{
    public BondMovementInfo[] bondMovements;
    public Vector2Int jmOffset;
    public Vector2Int movementOffset;
    public ActionType movementAction;

    public JointMovementInfo(BondMovementInfo[] bondMovements, Vector2Int jmOffset, Vector2Int movementOffset, ActionType movementAction)
    {
        this.bondMovements = bondMovements;
        this.jmOffset = jmOffset;
        this.movementOffset = movementOffset;
        this.movementAction = movementAction;
    }

    public static JointMovementInfo Empty = new JointMovementInfo(new BondMovementInfo[0], Vector2Int.zero, Vector2Int.zero, ActionType.NULL);

    public static bool operator==(JointMovementInfo i1, JointMovementInfo i2)
    {
        if (i1.jmOffset != i2.jmOffset || i1.movementOffset != i2.movementOffset || i1.movementAction != i2.movementAction
            || (i1.bondMovements == null ^ i2.bondMovements == null)
            || i1.bondMovements != null && i1.bondMovements.Length != i2.bondMovements.Length)
            return false;
        if (i1.bondMovements != null)
        {
            for (int i = 0; i < i1.bondMovements.Length; i++)
            {
                if (i1.bondMovements[i] != i2.bondMovements[i])
                    return false;
            }
        }
        return true;
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
        return HashCode.Combine(bondMovements, jmOffset, movementOffset, movementAction);
    }
}

public class ValueHistoryJointMovement : ValueHistory<JointMovementInfo>
{
    public ValueHistoryJointMovement(JointMovementInfo initialValue, int initialRound = 0) : base(initialValue, initialRound) { }
}
