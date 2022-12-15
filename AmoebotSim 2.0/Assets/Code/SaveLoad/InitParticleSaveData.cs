using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    [Serializable]
    public class InitParticleSaveData
    {
        public Vector2Int tailPos;
        public Direction expansionDir;
        public bool chirality;
        public Direction compassDir;

        // Attribute data, sorted by type
        public List<ParticleAttributeSaveData<bool>> boolAttributes;
        public List<ParticleAttributeSaveData<Direction>> dirAttributes;
        public List<ParticleAttributeSaveData<float>> floatAttributes;
        public List<ParticleAttributeSaveData<int>> intAttributes;
        public List<ParticleAttributeEnumSaveData> enumAttributes;
        public List<ParticleAttributePCSaveData> pcAttributes;
        public List<ParticleAttributeSaveData<string>> stringAttributes;
    }

} // namespace AS2
