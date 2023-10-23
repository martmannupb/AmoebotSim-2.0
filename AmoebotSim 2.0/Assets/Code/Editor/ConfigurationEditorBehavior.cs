using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AS2
{

    [CustomEditor(typeof(ConfigurationEditor))]
    public class ConfigurationEditorBehavior : Editor
    {
        public void OnEnable()
        {
            LoadConfig();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Load button (loads the current config file)
            if (GUILayout.Button("Load Config"))
            {
                LoadConfig(); 
            }

            if (GUILayout.Button("Save Config"))
            {
                SaveConfig();
            }
        }

        private void LoadConfig()
        {
            if (ConfigurationEditor.Instance is null)
                return;

            ConfigData data = ConfigurationEditor.Instance.configData;
            try
            {
                string json = File.ReadAllText(FilePaths.file_config);
                data = JsonUtility.FromJson<ConfigData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Error while loading config file (" + FilePaths.file_config + "): " + e);
            }
            ConfigurationEditor.Instance.configData = data;
        }

        private void SaveConfig()
        {
            if (ConfigurationEditor.Instance is null)
                return;

            string json;
            try
            {
                json = JsonUtility.ToJson(ConfigurationEditor.Instance.configData, true);
                File.WriteAllText(FilePaths.file_config, json);
            }
            catch (Exception e)
            {
                Debug.LogError("Error while writing config file (" + FilePaths.file_config + "): " + e);
            }
        }
    }

} // namespace AS2
