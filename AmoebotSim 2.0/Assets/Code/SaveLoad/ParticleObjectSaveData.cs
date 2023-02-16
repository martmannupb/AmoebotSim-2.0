using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AS2
{

    [Serializable]
    public class ParticleObjectSaveData
    {
        public ValueHistorySaveData<Vector2Int> positionHistory;
        public Vector2Int[] occupiedRel;
    }

} // namespace AS2
