using System;
using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;
using System.IO;

namespace AS2
{


    public static class Log
    {

        private static int logID = 0;

        private static List<string> logHistory = new List<string>();

        public static void Error(string text)
        {
            Error(text, false);
        }

        public static void Error(string text, bool logOnlyInEditor)
        {
            UnityEngine.Debug.LogError("[" + logID + "] " + text);
            logID++;
            if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Error);
        }

        public static void Debug(string text)
        {
            Debug(text, false);
        }

        public static void Debug(string text, bool logOnlyInEditor)
        {
            UnityEngine.Debug.Log("[" + logID + "] " + text);
            logID++;
            if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Debug);
        }

        public static void Warning(string text)
        {
            Warning(text, false);
        }

        public static void Warning(string text, bool logOnlyInEditor)
        {
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

        public static void AddToLogHistory(string text)
        {
            logHistory.Add("[" + System.DateTime.Now.ToString() + "] " + text);
        }

        public static void SaveLogToFile(string path)
        {
            // Print logHistory to file at path
            string text = "AmoebotSim Log =========================\n\n";
            foreach (var line in logHistory)
            {
                text += line + "\n";
            }
            File.WriteAllText(path, text);
        }

    }

} // namespace AS2
