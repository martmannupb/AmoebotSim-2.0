// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// Compressed representation of pin configuration data.
    /// Stores all information stored in a <see cref="AS2.Sim.SysPinConfiguration"/>
    /// instance except the particle reference, particle-specific information
    /// and state flags.
    /// <para>
    /// This class should be used for storing pin configurations in
    /// attributes and writing them to save files. The class overloads the
    /// equality operator to compare instances by their content.
    /// </para>
    /// <para>
    /// Note: This way of representing the pin configuration data is
    /// highly compressed and does not contain all information necessary
    /// to construct a pin configuration. The data must be interpreted in
    /// the context of a particle's expansion direction, chirality and
    /// compass orientation in order to reconstruct the original pin
    /// configuration.
    /// </para>
    /// </summary>
    [Serializable]
    public class PinConfigurationSaveData
    {
        /// <summary>
        /// Flag indicating that this instance represents <c>null</c>.
        /// This is necessary because Unity's JSON utility will force
        /// null instances of serializable classes to be initialized.
        /// </summary>
        public bool isNull;

        /// <summary>
        /// The (local) head direction for which the pin configuration was created.
        /// </summary>
        public Direction headDirection;

        // Only information required for pins is the partition set ID,
        // everything else is already defined by the index
        /// <summary>
        /// The partition set IDs of the pins. The entry at index <c>i</c> is the
        /// ID of the partition set to which the pin with ID <c>i</c> belongs.
        /// <para>
        /// Note that pin and partition set IDs can be interpreted in different
        /// ways based on the chirality and compass orientation of the particle.
        /// </para>
        /// </summary>
        public int[] pinPartitionSets;

        // Visualization
        /// <summary>
        /// The placement mode of the pin configuration in the particle's head.
        /// </summary>
        public PSPlacementMode placementModeHead;
        /// <summary>
        /// The placement mode of the pin configuration in the particle's tail.
        /// </summary>
        public PSPlacementMode placementModeTail;
        /// <summary>
        /// The global angle of the line along which partition
        /// sets are placed in the particle's head.
        /// </summary>
        public float lineRotationHead;
        /// <summary>
        /// The global angle of the line along which partition
        /// sets are placed in the particle's tail.
        /// </summary>
        public float lineRotationTail;
        /// <summary>
        /// The colors of all partition sets.
        /// </summary>
        public Color[] partitionSetColors;
        /// <summary>
        /// Color override flags of all partition sets.
        /// </summary>
        public bool[] partitionSetColorOverrides;
        /// <summary>
        /// Head positions of all partition sets in polar coordinates.
        /// </summary>
        public Vector2[] partitionSetHeadPositions;
        /// <summary>
        /// Tail positions of all partition sets in polar coordinates.
        /// </summary>
        public Vector2[] partitionSetTailPositions;
        /// <summary>
        /// Draw handle flags of all partition sets.
        /// </summary>
        public bool[] partitionSetDrawHandleFlags;

        public PinConfigurationSaveData() { }

        private PinConfigurationSaveData(bool isNull)
        {
            this.isNull = isNull;
        }

        private static PinConfigurationSaveData nullInstance = new PinConfigurationSaveData(true);

        /// <summary>
        /// A special instance of this class representing <c>null</c>.
        /// </summary>
        public static PinConfigurationSaveData NullInstance
        {
            get { return nullInstance; }
        }

        // Comparison operators to easily compare compressed pin configuration data by value
        public static bool operator ==(PinConfigurationSaveData d1, PinConfigurationSaveData d2)
        {
            if (d1 is null)
                return d2 is null;

            return d1.Equals(d2);
        }

        public static bool operator !=(PinConfigurationSaveData d1, PinConfigurationSaveData d2)
        {
            return !(d1 == d2);
        }

        public override bool Equals(object obj)
        {
            // null and other types are not equal
            if (obj == null || GetType() != obj.GetType())
                return false;
            PinConfigurationSaveData d = (PinConfigurationSaveData)obj;
            // Compare null flag
            if (isNull != d.isNull)
                return false;
            // Compare pin assignments and head direction
            bool myArrayNull = pinPartitionSets == null;
            bool otherArrayNull = d.pinPartitionSets == null;
            if (headDirection != d.headDirection || // Head direction
                myArrayNull != otherArrayNull ||    // Null
                !myArrayNull && !otherArrayNull && pinPartitionSets.Length != d.pinPartitionSets.Length)    // Lengths
                return false;
            if (!myArrayNull && !otherArrayNull)
            {
                for (int i = 0; i < pinPartitionSets.Length; i++)
                {
                    if (pinPartitionSets[i] != d.pinPartitionSets[i])
                        return false;
                }
            }
            // Compare graphical data
            if (placementModeHead != d.placementModeHead || placementModeTail != d.placementModeTail || lineRotationHead != d.lineRotationHead || lineRotationTail != d.lineRotationTail)
                return false;
            // Colors
            myArrayNull = partitionSetColors == null;
            otherArrayNull = d.partitionSetColors == null;
            if (myArrayNull != otherArrayNull)
                return false;
            if (!myArrayNull)
            {
                for (int i = 0; i < partitionSetColors.Length; i++)
                {
                    if (partitionSetColors[i] != d.partitionSetColors[i])
                        return false;
                }
            }
            // Color overrides
            myArrayNull = partitionSetColorOverrides == null;
            otherArrayNull = d.partitionSetColorOverrides == null;
            if (myArrayNull != otherArrayNull)
                return false;
            if (!myArrayNull)
            {
                for (int i = 0; i < partitionSetColorOverrides.Length; i++)
                {
                    if (partitionSetColorOverrides[i] != d.partitionSetColorOverrides[i])
                        return false;
                }
            }
            // Head positions
            myArrayNull = partitionSetHeadPositions == null;
            otherArrayNull = d.partitionSetHeadPositions == null;
            if (myArrayNull != otherArrayNull)
                return false;
            if (!myArrayNull)
            {
                for (int i = 0; i < partitionSetHeadPositions.Length; i++)
                {
                    if (partitionSetHeadPositions[i] != d.partitionSetHeadPositions[i])
                        return false;
                }
            }
            // Tail positions
            myArrayNull = partitionSetTailPositions == null;
            otherArrayNull = d.partitionSetTailPositions == null;
            if (myArrayNull != otherArrayNull)
                return false;
            if (!myArrayNull)
            {
                for (int i = 0; i < partitionSetTailPositions.Length; i++)
                {
                    if (partitionSetTailPositions[i] != d.partitionSetTailPositions[i])
                        return false;
                }
            }
            // Handle draw flags
            myArrayNull = partitionSetDrawHandleFlags == null;
            otherArrayNull = d.partitionSetDrawHandleFlags == null;
            if (myArrayNull != otherArrayNull)
                return false;
            if (!myArrayNull)
            {
                for (int i = 0; i < partitionSetTailPositions.Length; i++)
                {
                    if (partitionSetDrawHandleFlags[i] != d.partitionSetDrawHandleFlags[i])
                        return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(headDirection, pinPartitionSets, placementModeHead, placementModeTail, lineRotationHead, lineRotationTail, HashCode.Combine(partitionSetColors, partitionSetColorOverrides, partitionSetHeadPositions, partitionSetTailPositions, partitionSetDrawHandleFlags, isNull));
        }
    }

} // namespace AS2
