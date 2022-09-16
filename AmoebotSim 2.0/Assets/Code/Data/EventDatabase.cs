using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public static class EventDatabase
{

    public static Action<bool> event_sim_startedStopped; // true = started, false = stopped
    public static Action<bool> event_initializationUI_initModeOpenClose; // true = Open, false = Close

}
