// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AS2.UI
{

    /// <summary>
    /// Controls the log panel.
    /// </summary>
    public class LogUIHandler : MonoBehaviour
    {

        // Singleton
        public static LogUIHandler instance;

        // UI
        public GameObject go_log;
        public GameObject go_elementParent;
        public GameObject go_expand;
        public ScrollRect scrollRect;
        public Scrollbar scrollBar;

        // Colors
        public Color color_log;
        public Color color_debug;
        public Color color_error;
        public Color color_warning;

        // Data
        public List<GameObject> logElementList = new List<GameObject>();
        private int maxLogEntries = 100;
        private int logEntryID = 1;
        private int scrollToBottomInAmountOfFrames = 0;

        // Timestamp for closing log automatically after a few seconds
        // (only if log was opened by a log message, not manually)
        private float timestampLastInteraction = 0f;
        private float timeVisibleInitial = 5f;
        private float timeVisible = 10f;
        private bool keepVisible = false;

        public enum EntryType
        {
            Log, Debug, Error, Warning, Empty
        }

        public void Start()
        {
            instance = this;
            InitLog();

            // Test
            //for (int i = 0; i < 1; i++)
            //{
            //    AddLogEntry("Test", EntryType.Debug);
            //    AddLogEntry("Test", EntryType.Log);
            //    AddLogEntry("Test", EntryType.Error);
            //    AddLogEntry("Test", EntryType.Warning);
            //    AddLogEntry("Test", EntryType.Empty);
            //}
        }

        /// <summary>
        /// Initializes the log and writes some welcome message.
        /// </summary>
        private void InitLog()
        {
            // Delete Dummies
            if (go_elementParent.transform.childCount > 0)
            {
                for (int i = go_elementParent.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(go_elementParent.transform.GetChild(i).gameObject);
                }
            }

            AddWelcomeMessage();

            // Test
            //AddLogEntry("This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test." +
            //    "This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test." +
            //    "This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test.This is a Test." +
            //    "\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test.\nThis is a Test." +
            //    "\nThis is a Test.\nThis is a Test.\nThis is a Test.\n", EntryType.Log, true);

            // Hide Log
            Hide();
        }

        /// <summary>
        /// Adds the log entries making up the welcome message so that the log is not empty.
        /// </summary>
        private void AddWelcomeMessage()
        {
            AddLogEntry("Welcome to AmoebotSim 2.0!", EntryType.Log, false, true);
            //AddLogEntry("=============================================================================================", EntryType.Log, false);
            AddLogEntry("University of Paderborn, Theory of Distributed Systems (Prof. Dr. Christian Scheideler)", EntryType.Log, false, true);
            AddLogEntry("Created by Matthias Artmann [System + Functionality] and Tobias Maurer [Rendering + UI], project lead by Andreas Padalkin and Daniel Warner.", EntryType.Log, false, true);
            //AddLogEntry("=============================================================================================", EntryType.Log, false);
            AddLogEntry("                                                                                                                              ___/|", EntryType.Log, false, true);
            AddLogEntry("                                                                                                                              \\o.O|", EntryType.Log, false, true);
            AddLogEntry("                                                                                                                              (___)", EntryType.Log, false, true);
            AddLogEntry("                                                                                                                              U", EntryType.Log, false, true);
            AddLogEntry("", EntryType.Empty, false, true);
            AddLogEntry("Log  ========================================================================================", EntryType.Log, false, true);
        }

        /// <summary>
        /// Update loop for the log UI, called each frame.
        /// </summary>
        public void Update()
        {
            // Scroll to Bottom
            if (scrollToBottomInAmountOfFrames == 0)
            {
                ScrollToBottom();
                scrollToBottomInAmountOfFrames--;
            }
            else if (scrollToBottomInAmountOfFrames >= 0)
                scrollToBottomInAmountOfFrames--;

            // Hide Timer (if log is visible but should disappear after a few seconds)
            if (go_log.activeSelf && !keepVisible)
            {
                // Disable log after a number of seconds (use settings to adjust)
                if (timeVisibleInitial != 0f ? timestampLastInteraction + timeVisibleInitial < Time.timeSinceLevelLoad : timestampLastInteraction + timeVisible < Time.timeSinceLevelLoad)
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// Adds a log entry to the log.
        /// </summary>
        /// <param name="text">The text to log.</param>
        /// <param name="type">The type of the log entry.</param>
        public void AddLogEntry(string text, EntryType type)
        {
            AddLogEntry(text, type, true);
        }

        /// <summary>
        /// Adds a log entry to the log.
        /// </summary>
        /// <param name="text">The text to log.</param>
        /// <param name="type">The type of the log entry.</param>
        /// <param name="showNumberOfEntry">True if the number of the entry should be shown.</param>
        /// <param name="useOldPrefab">True if the old prefab which does not support multiline should be used. Recommendation: Keep this at false.</param>
        protected void AddLogEntry(string text, EntryType type, bool showNumberOfEntry, bool useOldPrefab = false)
        {
            GameObject go;
            if (useOldPrefab) go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_log_element, Vector3.zero, Quaternion.identity, go_elementParent.transform);
            else go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_log_elementLarge, Vector3.zero, Quaternion.identity, go_elementParent.transform);
            logElementList.Add(go);

            TextMeshProUGUI tmp_header = GetLogElementTMPHeader(go);
            TextMeshProUGUI tmp_text = GetLogElementTMPText(go);
            string preText = showNumberOfEntry && type != EntryType.Empty ? "[" + (logEntryID++) + "] " : "";
            switch (type)
            {
                case EntryType.Log:
                    tmp_header.text = preText;
                    tmp_header.color = color_log;
                    break;
                case EntryType.Debug:
                    tmp_header.text = preText + "Debug:";
                    tmp_header.color = color_debug;
                    break;
                case EntryType.Error:
                    tmp_header.text = preText + "Error:";
                    tmp_header.color = color_error;
                    break;
                case EntryType.Warning:
                    tmp_header.text = preText + "Warning:";
                    tmp_header.color = color_warning;
                    break;
                case EntryType.Empty:
                    tmp_header.text = "";
                    break;

                default:
                    break;
            }
            tmp_text.text = text;
            if (type == EntryType.Empty)
            {
                tmp_text.text = "";
                //go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            }
            Log.AddToLogHistory(text);
            if (go.GetComponent<SizeFitter>() != null) go.GetComponent<SizeFitter>().ResizeCentralizedInFrameAmount(1);

            // Check if list is too long
            while (logElementList.Count > maxLogEntries)
            {
                Destroy(logElementList[0]);
                logElementList.RemoveAt(0);
            }

            // Show entry if log level is warning or higher
            if (type == EntryType.Warning || type == EntryType.Error)
            {
                timestampLastInteraction = Time.timeSinceLevelLoad;
                Show(true, this.keepVisible);
            }

            // Scroll to bottom any time an entry is added
            if (scrollToBottomInAmountOfFrames < 0)
                scrollToBottomInAmountOfFrames = 3;
        }

        /// <summary>
        /// Shows the log.
        /// </summary>
        /// <param name="scrollDown">True if the log should scroll down.</param>
        /// <param name="keepVisible">True if the log should be kept visible, false if it should disappear automatically after some time.</param>
        public void Show(bool scrollDown, bool keepVisible)
        {
            this.keepVisible = keepVisible;
            go_log.SetActive(true);
            go_expand.SetActive(false);
            // Scroll Down
            if (scrollDown) scrollToBottomInAmountOfFrames = 3;
        }

        /// <summary>
        /// Hides the log.
        /// </summary>
        public void Hide()
        {
            go_log.SetActive(false);
            go_expand.SetActive(true);
            timeVisibleInitial = 0f;
            timestampLastInteraction = -100f;
            keepVisible = false;
        }

        /// <summary>
        /// Scrolls to the bottom of the log.
        /// We call this method after some very small number of frames because Unity needs to update some internal UI stuff before the sizes of the UI elements are in the system.
        /// </summary>
        public void ScrollToBottom()
        {
            scrollRect.verticalNormalizedPosition = 0f;
            scrollToBottomInAmountOfFrames = 0;
            // Move the scroll bar all the way to the top and back to the bottom to force update (necessary after deleting the log)
            scrollBar.value = 1;
            scrollBar.value = 0;
        }

        /// <summary>
        /// Returns the TMP UGUI for the header.
        /// </summary>
        /// <param name="logElement"></param>
        /// <returns></returns>
        private TextMeshProUGUI GetLogElementTMPHeader(GameObject logElement)
        {
            // Return the first found TMP UGUI
            return GetLogElementTextMeshProUGUI(logElement, 1);
        }

        /// <summary>
        /// Returns the TMP UGUI for the text.
        /// </summary>
        /// <param name="logElement"></param>
        /// <returns></returns>
        private TextMeshProUGUI GetLogElementTMPText(GameObject logElement)
        {
            // Return the second found TMP UGUI
            return GetLogElementTextMeshProUGUI(logElement, 2);
        }

        /// <summary>
        /// Returns the first/second/third/etc... found TMP UGUI in the child elements of the log element. Index starting at 1.
        /// </summary>
        /// <param name="logElement"></param>
        /// <param name="firstSecondEtc"></param>
        /// <returns></returns>
        private TextMeshProUGUI GetLogElementTextMeshProUGUI(GameObject logElement, int firstSecondEtc)
        {
            int found = 0;
            for (int i = 0; i < logElement.transform.childCount; i++)
            {
                TextMeshProUGUI tmp = logElement.transform.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    // Found a TMP
                    found++;
                    if (found == firstSecondEtc) return tmp;
                }
            }
            return null;
        }

        /// <summary>
        /// The user pressed the expand log button. Show the log panel.
        /// </summary>
        public void ButtonPressed_ExpandLog()
        {
            timestampLastInteraction = Time.timeSinceLevelLoad;
            Show(true, true);
            // Prevent sticky tooltip
            TooltipHandler.Instance.Close();
        }

        /// <summary>
        /// The user pressed the collapse log button. Hide the log panel.
        /// </summary>
        public void ButtonPressed_CollapseLog()
        {
            Hide();
            // Prevent sticky tooltip
            TooltipHandler.Instance.Close();
        }

        /// <summary>
        /// The user pressed the clear log button. Delete all log entries.
        /// </summary>
        public void ButtonPressed_ClearLog()
        {
            foreach (GameObject go in logElementList)
            {
                Destroy(go);
            }
            logElementList.Clear();
            Log.ClearLogHistory();
            logEntryID = 1;
            // Do not reset Unity log ID because the Unity log is not cleared by this
            AddWelcomeMessage();
            // Then scroll to bottom after short delay
            if (scrollToBottomInAmountOfFrames < 0)
                scrollToBottomInAmountOfFrames = 3;
        }
    }

}
