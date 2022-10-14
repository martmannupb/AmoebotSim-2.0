using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class LogUIHandler : MonoBehaviour
{

    // Singleton
    public static LogUIHandler instance;

    // UI
    public GameObject go_log;
    public GameObject go_elementParent;
    public GameObject go_expand;
    public ScrollRect scrollRect;

    // Colors
    public Color color_log;
    public Color color_debug;
    public Color color_error;
    public Color color_warning;

    // Data
    public List<GameObject> logElementList = new List<GameObject>();
    private int maxLogEntries = 30;
    private int logEntryID = 1;
    private int scrollToBottomInAmountOfFrames = 0;

    // Timestamp
    private float timestampLastInteraction = 0f;
    private float timeVisibleInitial = 5f;
    private float timeVisible = 10f;

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

    private void InitLog()
    {
        // Delete Dummies
        if(go_elementParent.transform.childCount > 0)
        {
            for (int i = go_elementParent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(go_elementParent.transform.GetChild(i).gameObject);
            }
        }

        AddLogEntry("Welcome to AmoebotSim 2.0!", EntryType.Log, false);
        //AddLogEntry("=============================================================================================", EntryType.Log, false);
        AddLogEntry("University of Paderborn, Theory of Distributed Systems (Prof. Dr. Christian Scheideler)", EntryType.Log, false);
        AddLogEntry("Created by Matthias Artmann [System + Functionality] and Tobias Maurer [Rendering + UI], project lead by Andreas Padalkin and Daniel Warner.", EntryType.Log, false);
        //AddLogEntry("=============================================================================================", EntryType.Log, false);
        AddLogEntry("                                                                                                                              ___/|", EntryType.Log, false);
        AddLogEntry("                                                                                                                              \\o.O|", EntryType.Log, false);
        AddLogEntry("                                                                                                                              (___)", EntryType.Log, false);
        AddLogEntry("                                                                                                                              U", EntryType.Log, false);
        AddLogEntry("", EntryType.Empty);
        AddLogEntry("Log  ========================================================================================", EntryType.Log, false);
        
        // Hide Log
        Hide();
        timeVisibleInitial = 0f;
        timestampLastInteraction = -100f;
    }

    public void Update()
    {
        // Scroll to Bottom
        if (scrollToBottomInAmountOfFrames == 0) ScrollToBottom();
        else scrollToBottomInAmountOfFrames--;

        // Hide Timer
        bool hide = timeVisibleInitial != 0f ? timestampLastInteraction + timeVisibleInitial < Time.timeSinceLevelLoad : timestampLastInteraction + timeVisible < Time.timeSinceLevelLoad;
        // Disable log after a number of seconds (use settings to adjust)
        if(hide)
        {
            // Hide
            Hide();
            if(timeVisibleInitial != 0f)
            {
                timeVisibleInitial = 0f;
                timestampLastInteraction = -100f;
            }
        }
        else
        {
            // Show
            Show();
        }
    }

    public void AddLogEntry(string text, EntryType type)
    {
        AddLogEntry(text, type, true);
    }

    protected void AddLogEntry(string text, EntryType type, bool showNumberOfEntry)
    {
        GameObject go = GameObject.Instantiate<GameObject>(UIDatabase.prefab_log_element, Vector3.zero, Quaternion.identity, go_elementParent.transform);
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
        timestampLastInteraction = Time.timeSinceLevelLoad;

        // Check if list is too long
        while (logElementList.Count > maxLogEntries)
        {
            Destroy(logElementList[0]);
            logElementList.RemoveAt(0);
        }

        // Scroll Down
        scrollToBottomInAmountOfFrames = 2;

    }

    public void Show()
    {
        go_log.SetActive(true);
        go_expand.SetActive(false);
    }

    public void Hide()
    {
        go_log.SetActive(false);
        go_expand.SetActive(true);
    }

    public void ScrollToBottom()
    {
        scrollRect.verticalNormalizedPosition = 0f;
        scrollToBottomInAmountOfFrames = 0;
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

    public void ButtonPressed_ExpandLog()
    {
        timestampLastInteraction = Time.timeSinceLevelLoad;
        Show();
    }

}
