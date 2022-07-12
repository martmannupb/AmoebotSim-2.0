using System;
using UnityEngine;
using System.Collections;


public static class Log {

    private static int logID = 0;

    public static void Error(string text) {
        UnityEngine.Debug.LogError("[" + logID + "] " + text);
        logID++;
    }

    public static void Debug(string text) {
        UnityEngine.Debug.Log("[" + logID + "] " + text);
        logID++;
    }

    public static void Warning(string text) {
        UnityEngine.Debug.Log("[" + logID + "] " + text);
        logID++;
    }

}