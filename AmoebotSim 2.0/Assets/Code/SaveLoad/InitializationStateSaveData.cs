using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    [Serializable]
    public class InitializationStateSaveData
    {
        public string selectedAlgorithm;

        public InitParticleSaveData[] particles;

        public InitModeSaveData initModeSaveData;
    }

} // namespace AS2
