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

    }

} // namespace AS2
