using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// The static config utility for accessing the configuration
    /// data at runtime.
    /// </summary>
    public static class Config
    {
        private static ConfigData configData;

        /// <summary>
        /// The currently loaded configuration data. May be
        /// modified so that the updated settings can be stored.
        /// </summary>
        public static ConfigData ConfigData
        {
            get
            {
                if (configData is null)
                    LoadConfigData();
                
                return configData;
            }
        }

        /// <summary>
        /// Loads the content of the configuration file into this
        /// object's configuration data. Can be used to reload the
        /// configuration in case the file contents have changed.
        /// </summary>
        public static void LoadConfigData()
        {
            ConfigData data = ReadConfigFile();
            if (data is null)
            {
                Debug.LogError("Using default config");
                configData = ConfigData.GetDefault();
            }
            else
                configData = data;
        }

        /// <summary>
        /// Reads the configuration file's content into a
        /// <see cref="ConfigData"/> object and returns it.
        /// </summary>
        /// <returns>The configuration data from the simulator's
        /// configuration file. May be <c>null</c> if the file
        /// could not be read.</returns>
        public static ConfigData ReadConfigFile()
        {
            ConfigData data = null;
            try
            {
                string json = File.ReadAllText(FilePaths.file_config);
                data = JsonUtility.FromJson<ConfigData>(json);
            }
            catch (Exception e)
            {
                Log.Error("Error while reading config file (" + FilePaths.file_config + "): " + e);
            }
            return data;
        }

        /// <summary>
        /// Stores the current configuration data in the
        /// simulator's configuration file.
        /// </summary>
        public static void SaveConfigData()
        {
            if (configData is null)
                LoadConfigData();

            SaveConfig(configData);
        }

        /// <summary>
        /// Stores the given configuration data in the
        /// simulator's configuration file.
        /// </summary>
        /// <param name="data">The configuration data
        /// to be stored.</param>
        public static void SaveConfig(ConfigData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePaths.file_config, json);
            }
            catch (Exception e)
            {
                Log.Error("Error while writing config data to file: " + e);
            }
        }

        /// <summary>
        /// Resets the configuration data to its default settings.
        /// </summary>
        public static void ResetConfig()
        {
            configData = ConfigData.GetDefault();
        }
    }

} // namespace AS2
