using System;
using UnityEngine;
using System.Collections;


public static class Log {

    private static int logID = 0;

    public static void Error(string text)
    {
        Error(text, false);
    }

    public static void Error(string text, bool logOnlyInEditor) {
        UnityEngine.Debug.LogError("[" + logID + "] " + text);
        logID++;
        if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Error);
    }

    public static void Debug(string text)
    {
        Debug(text, false);
    }

    public static void Debug(string text, bool logOnlyInEditor) {
        UnityEngine.Debug.Log("[" + logID + "] " + text);
        logID++;
        if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Debug);
    }

    public static void Warning(string text)
    {
        Warning(text, false);
    }

    public static void Warning(string text, bool logOnlyInEditor) {
        UnityEngine.Debug.Log("[" + logID + "] " + text);
        logID++;
        if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Warning);
    }

    public static void Entry(string text)
    {
        Entry(text, false);
    }

    public static void Entry(string text, bool logOnlyInEditor)
    {
        UnityEngine.Debug.Log("[" + logID + "] " + text);
        logID++;
        if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Log);
    }

}