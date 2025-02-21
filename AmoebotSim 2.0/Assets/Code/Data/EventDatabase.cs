// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace AS2
{

    public static class EventDatabase
    {

        public static Action<bool> event_sim_startedStopped; // true = started, false = stopped
        public static Action<bool> event_initializationUI_initModeOpenClose; // true = Open, false = Close
        public static Action<bool> event_particleUI_particlePanelOpenClose; // true = Open, false = Close
        public static Action<bool> event_objectUI_objectPanelOpenClose;     // true = Open, false = Close

    }

} // namespace AS2
