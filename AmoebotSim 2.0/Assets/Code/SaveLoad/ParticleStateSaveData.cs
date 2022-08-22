using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container for all data a <see cref="Particle"/> requires
/// to save and load its complete state.
/// </summary>
[Serializable]
public class ParticleStateSaveData
{
    // Global info
    public int comDir;
    public bool chirality;

    // Algorithm info
    public string algorithmType;

    // Positional info
    public ValueHistorySaveData<Vector2Int> tailPositionHistory;
    public ValueHistorySaveData<int> expansionDirHistory;

    // Attribute data, sorted by type
    public List<ParticleAttributeSaveData<bool>> boolAttributes;
    public List<ParticleAttributeSaveData<int>> dirAttributes;
    public List<ParticleAttributeSaveData<int>> intAttributes;
    public List<ParticleAttributeEnumSaveData> enumAttributes;
    public List<ParticleAttributePCSaveData> pcAttributes;

    // Circuit data
    public PinConfigurationHistorySaveData pinConfigurationHistory;
    public ValueHistorySaveData<BitArraySaveData> receivedBeepsHistory;
    public ValueHistorySaveData<MessageSaveData>[] receivedMessagesHistory;

    // Visualization data
    public ValueHistorySaveData<Color> mainColorHistory;
    public ValueHistorySaveData<bool> mainColorSetHistory;
    public ValueHistorySaveData<Color>[] partitionSetColorHistory;
    public ValueHistorySaveData<bool>[] partitionSetColorOverrideHistory;
}
