using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compressed representation of pin configuration data.
/// Stores all information stored in a <see cref="SysPinConfiguration"/>
/// instance except the particle reference and state flags.
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
    public int headDirection;

    // Only information required for pins is the partition set ID,
    // everything else is already defined by the index
    public int[] pinPartitionSets;

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
        if (obj == null || GetType() != obj.GetType())
            return false;
        PinConfigurationSaveData d = (PinConfigurationSaveData)obj;
        bool myArrayNull = pinPartitionSets == null;
        bool otherArrayNull = d.pinPartitionSets == null;
        if (headDirection != d.headDirection ||
            myArrayNull != otherArrayNull ||
            !myArrayNull && !otherArrayNull && pinPartitionSets.Length != d.pinPartitionSets.Length)
            return false;
        if (!myArrayNull && !otherArrayNull)
        {
            for (int i = 0; i < pinPartitionSets.Length; i++)
            {
                if (pinPartitionSets[i] != d.pinPartitionSets[i])
                    return false;
            }
        }
        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(headDirection, pinPartitionSets);
    }
}
