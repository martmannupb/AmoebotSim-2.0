using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AS2
{

    /// <summary>
    /// Custom editor implementation for the <see cref="ConfigurationEditor"/>.
    /// </summary>
    [CustomEditor(typeof(ConfigurationEditor))]
    public class ConfigurationEditorBehavior : Editor
    {
        /// <summary>
        /// Reference to the editor script containing the config data.
        /// </summary>
        ConfigurationEditor editor;

        public void OnEnable()
        {
            // Get reference to the editor script and load the config file
            editor = ((MonoBehaviour)target).GetComponent<ConfigurationEditor>();
            LoadConfig();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            // Load button (loads the current config file)
            if (GUILayout.Button("Load Config"))
            {
                LoadConfig(); 
            }

            // Save button (saves the current configuration to file)
            if (GUILayout.Button("Save Config"))
            {
                SaveConfig();
            }

            // Reset button (resets to default values)
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetConfig();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Tries loading the configuration data from the simulator's
        /// configuration file.
        /// </summary>
        private void LoadConfig()
        {
            if (editor is null)
            {
                Debug.LogError("No ConfigurationEditor component found");
                return;
            }

            ConfigData data = Config.ReadConfigFile();
            if (data is not null)
                editor.configData = data;
        }

        /// <summary>
        /// Tries storing the current configuration data in the
        /// simulator's configuration file.
        /// </summary>
        private void SaveConfig()
        {
            if (editor is null)
            {
                Debug.LogError("No ConfigurationEditor component found");
                return;
            }

            Config.SaveConfig(editor.configData);
        }

        /// <summary>
        /// Resets the configuration data of the editor to
        /// its default settings.
        /// </summary>
        private void ResetConfig()
        {
            if (editor is null)
            {
                Debug.LogError("No ConfigurationEditor component found");
                return;
            }

            ConfigData data = ConfigData.GetDefault();
            editor.configData = data;
        }
    }

} // namespace AS2
