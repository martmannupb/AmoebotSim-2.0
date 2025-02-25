// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


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
        public ValueHistorySaveData<Color> colorHistory;
        public ValueHistorySaveData<Vector2Int> jmOffsetHistory;
        public Vector2Int[] occupiedRel;
    }

} // namespace AS2
