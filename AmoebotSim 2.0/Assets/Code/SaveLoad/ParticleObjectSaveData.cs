using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AS2
{

    [Serializable]
    public class ParticleObjectSaveData
    {
        public int identifier;
        public ValueHistorySaveData<Vector2Int> positionHistory;
        public Vector2Int[] occupiedRel;
        public Color color;
    }

} // namespace AS2
