using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PinConfigurationHistorySaveData
{
    public ValueHistorySaveData<int> idxHistory;
    public List<PinConfigurationSaveData> configs;
}
