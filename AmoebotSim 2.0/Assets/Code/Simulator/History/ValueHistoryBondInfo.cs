// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Serializable representation of a single bond movement.
    /// Describes the two locations of a bond before and after
    /// a movement round. Also contains some graphical info.
    /// </summary>
    [Serializable]
    public struct BondMovementInfo
    {
        /// <summary>
        /// The bond's start location before the movement.
        /// </summary>
        public Vector2Int start1;
        /// <summary>
        /// The bond's end location before the movement.
        /// </summary>
        public Vector2Int end1;
        /// <summary>
        /// The bond's start location after the movement.
        /// </summary>
        public Vector2Int start2;
        /// <summary>
        /// The bond's end location after the movement.
        /// </summary>
        public Vector2Int end2;
        /// <summary>
        /// Whether the bond should be hidden.
        /// </summary>
        public bool hidden;

        public BondMovementInfo(Vector2Int start1, Vector2Int end1, Vector2Int start2, Vector2Int end2, bool hidden = false)
        {
            this.start1 = start1;
            this.end1 = end1;
            this.start2 = start2;
            this.end2 = end2;
            this.hidden = hidden;
        }

        /// <summary>
        /// A movement info in which all positions are <c>(0,0)</c>.
        /// </summary>
        public static BondMovementInfo Empty = new BondMovementInfo(Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero);

        public static bool operator ==(BondMovementInfo i1, BondMovementInfo i2)
        {
            return i1.start1 == i2.start1 && i1.end1 == i2.end1 && i1.start2 == i2.start2 && i1.end2 == i2.end2 && i1.hidden == i2.hidden;
        }

        public static bool operator !=(BondMovementInfo i1, BondMovementInfo i2)
        {
            return i1.start1 != i2.start1 || i1.end1 != i2.end1 || i1.start2 != i2.start2 || i1.end2 != i2.end2 || i1.hidden != i2.hidden;
        }

        public override bool Equals(object obj)
        {
            return obj != null && this.GetType().Equals(obj.GetType()) && this == (BondMovementInfo)obj;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(start1, end1, start2, end2, hidden);
        }
    }

    /// <summary>
    /// Serializable representation of a list of bond movements.
    /// </summary>
    [Serializable]
    public struct BondMovementInfoList
    {
        /// <summary>
        /// An array of <see cref="BondMovementInfo"/> structs.
        /// </summary>
        public BondMovementInfo[] bondMovements;

        public BondMovementInfoList(BondMovementInfo[] bondMovements)
        {
            this.bondMovements = bondMovements;
        }

        /// <summary>
        /// A movement info list which is empty.
        /// </summary>
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

    /// <summary>
    /// Implementation of <see cref="ValueHistory{T}"/> storing
    /// <see cref="BondMovementInfoList"/> structs.
    /// </summary>
    public class ValueHistoryBondInfo : ValueHistory<BondMovementInfoList>
    {
        public ValueHistoryBondInfo(BondMovementInfoList initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

        public ValueHistoryBondInfo(ValueHistorySaveData<BondMovementInfoList> data) : base(data) { }
    }

} // namespace AS2.Sim
