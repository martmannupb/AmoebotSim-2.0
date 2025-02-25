// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


﻿using System;
using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;
using System.IO;

namespace AS2
{

    /// <summary>
    /// Custom logging utility that displays log messages in
    /// Unity's debug log and in the simulator's log panel.
    /// Should be preferred over <c>UnityEngine.Debug</c>.
    /// </summary>
    public static class Log
    {

        private static int logID = 0;

        private static List<string> logHistory = new List<string>();

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The error message.</param>
        public static void Error(string text)
        {
            Error(text, false);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="text">The error message.</param>
        /// <param name="logOnlyInEditor">If <c>true</c>, only displays the
        /// message in Unity's debug log.</param>
        public static void Error(string text, bool logOnlyInEditor)
        {
            UnityEngine.Debug.LogError("[" + logID + "] " + text);
            logID++;
            if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Error);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="text">The debug message.</param>
        public static void Debug(string text)
        {
            Debug(text, false);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="text">The debug message.</param>
        /// <param name="logOnlyInEditor">If <c>true</c>, only displays the
        /// message in Unity's debug log.</param>
        public static void Debug(string text, bool logOnlyInEditor)
        {
            UnityEngine.Debug.Log("[" + logID + "] " + text);
            logID++;
            if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Debug);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="text">The warning message.</param>
        public static void Warning(string text)
        {
            Warning(text, false);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="text">The warning message.</param>
        /// <param name="logOnlyInEditor">If <c>true</c>, only displays the
        /// message in Unity's debug log.</param>
        public static void Warning(string text, bool logOnlyInEditor)
        {
            UnityEngine.Debug.Log("[" + logID + "] " + text);
            logID++;
            if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Warning);
        }

        /// <summary>
        /// Logs a simple message.
        /// </summary>
        /// <param name="text">The message.</param>
        public static void Entry(string text)
        {
            Entry(text, false);
        }

        /// <summary>
        /// Logs a simple message.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <param name="logOnlyInEditor">If <c>true</c>, only displays the
        /// message in Unity's debug log.</param>
        public static void Entry(string text, bool logOnlyInEditor)
        {
            UnityEngine.Debug.Log("[" + logID + "] " + text);
            logID++;
            if (LogUIHandler.instance != null && logOnlyInEditor == false) LogUIHandler.instance.AddLogEntry(text, LogUIHandler.EntryType.Log);
        }

        /// <summary>
        /// Adds the given text to the log's history.
        /// </summary>
        /// <param name="text">The text that should be added.</param>
        public static void AddToLogHistory(string text)
        {
            logHistory.Add("[" + System.DateTime.Now.ToString() + "] " + text);
        }

        /// <summary>
        /// Writes the log's history to the given file.
        /// </summary>
        /// <param name="path">The path to the file to which
        /// the log should be written.</param>
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
