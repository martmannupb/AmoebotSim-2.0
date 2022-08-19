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
    public bool expanded;   // This is actually not even needed since the particle in question knows if it is expanded or not

    // Only information required for pins is the partition set ID,
    // everything else is already defined by the index
    public int[] pinPartitionSets;
}
