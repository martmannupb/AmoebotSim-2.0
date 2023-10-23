using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AS2
{

    public static class Config
    {
        private static ConfigData configData;

        public static ConfigData ConfigData
        {
            get
            {
                if (configData is null)
                    LoadConfigData();
                
                return configData;
            }
        }

        public static void LoadConfigData()
        {
            try
            {
                string json = File.ReadAllText(FilePaths.file_config);
                configData = JsonUtility.FromJson<ConfigData>(json);
            }
            catch (Exception e)
            {
                Log.Error("Error while reading config file. Using default values. " + e);
                configData = new ConfigData();
                configData.settingsMenu = new ConfigData.SettingsMenu();
                configData.additionalConfiguration = new ConfigData.AdditionalConfiguration();
            }
        }

        public static void SaveConfigData()
        {
            if (configData is null)
                LoadConfigData();

            try
            {
                string json = JsonUtility.ToJson(configData, true);
                File.WriteAllText(FilePaths.file_config, json);
            }
            catch (Exception e)
            {
                Log.Error("Error while writing config data to file: " + e);
                return;
            }
        }

        public static void ResetConfig()
        {
            configData = new ConfigData();
            configData.settingsMenu = new ConfigData.SettingsMenu();
            configData.additionalConfiguration = new ConfigData.AdditionalConfiguration();
        }
    }

} // namespace AS2
