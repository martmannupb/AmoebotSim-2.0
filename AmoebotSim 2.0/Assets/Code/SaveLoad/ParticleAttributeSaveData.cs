using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class ParticleAttributeSaveDataBase
{
    public string name;
    public int idx;         // Index in the particle's list of attributes
}

[Serializable]
public class ParticleAttributeSaveData<T> : ParticleAttributeSaveDataBase
{
    public ValueHistorySaveData<T> history;
}

[Serializable]
public class ParticleAttributeEnumSaveData : ParticleAttributeSaveData<string>
{
    public string enumType;
}

[Serializable]
public class ParticleAttributePCSaveData : ParticleAttributeSaveDataBase
{
    public PinConfigurationHistorySaveData history;
}
