using System;
using UnityEngine;

namespace AS2.Sim
{

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

        public static bool operator ==(BondMovementInfo i1, BondMovementInfo i2)
        {
            return i1.start1 == i2.start1 && i1.end1 == i2.end1 && i1.start2 == i2.start2 && i1.end2 == i2.end2;
        }

        public static bool operator !=(BondMovementInfo i1, BondMovementInfo i2)
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
    public struct BondMovementInfoList
    {
        public BondMovementInfo[] bondMovements;

        public BondMovementInfoList(BondMovementInfo[] bondMovements)
        {
            this.bondMovements = bondMovements;
        }

        public static BondMovementInfoList Empty = new BondMovementInfoList(new BondMovementInfo[0]);

        public static bool operator ==(BondMovementInfoList l1, BondMovementInfoList l2)
        {
            if ((l1.bondMovements == null ^ l2.bondMovements == null)
                || l1.bondMovements != null && l1.bondMovements.Length != l2.bondMovements.Length)
                return false;
            if (l1.bondMovements != null)
            {
                for (int i = 0; i < l1.bondMovements.Length; i++)
                {
                    if (l1.bondMovements[i] != l2.bondMovements[i])
                        return false;
                }
            }
            return true;
        }

        public static bool operator !=(BondMovementInfoList l1, BondMovementInfoList l2)
        {
            return !(l1 == l2);
        }

        public override bool Equals(object obj)
        {
            return obj != null && this.GetType().Equals(obj.GetType()) && this == (BondMovementInfoList)obj;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(bondMovements);
        }
    }

    public class ValueHistoryBondInfo : ValueHistory<BondMovementInfoList>
    {
        public ValueHistoryBondInfo(BondMovementInfoList initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

        public ValueHistoryBondInfo(ValueHistorySaveData<BondMovementInfoList> data) : base(data) { }
    }

} // namespace AS2.Sim
