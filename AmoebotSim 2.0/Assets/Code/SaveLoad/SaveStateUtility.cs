using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Utility for saving and loading simulation states.
/// <para>
/// A simulation state comprises the entire history of all particles in
/// the system. It is stored in a <see cref="SimulationStateSaveData"/>
/// object, which can be serialized by Unity's <see cref="JsonUtility"/>.
/// 
/// </para>
/// </summary>
public static class SaveStateUtility
{
    public static string defaultSaveFile = Application.persistentDataPath + "/Amoebotsim_2_0_default_savedata.json";

    /// <summary>
    /// Converts the given simulation state to JSON format and writes it to
    /// a file.
    /// </summary>
    /// <param name="data">The serializable representation of the simulation state.</param>
    /// <param name="filename">The file in which to store the JSON string. Default value
    /// is <see cref="defaultSaveFile"/>.</param>
    /// <param name="prettyPrint">If <c>true</c>, the JSON text is formatted to be
    /// human-readable. Note that this significantly increases the file size.</param>
    /// <returns><c>true</c> if and only if the file was written successfully.</returns>
    public static bool Save(SimulationStateSaveData data, string filename = null, bool prettyPrint = false)
    {
        if (filename is null)
            filename = defaultSaveFile;
        string json = JsonUtility.ToJson(data, prettyPrint);
        try
        {
            File.WriteAllText(filename, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Error writing save data to file '" + filename + "': " + e);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Loads a simulation state from a save file.
    /// </summary>
    /// <param name="filename">The file from which to read the save data. Default
    /// value is <see cref="defaultSaveFile"/>.</param>
    /// <returns>The serializable object representation of the data stored in the
    /// given file. Can be <c>null</c> if the file could not be read or its content
    /// could not be parsed correctly.</returns>
    public static SimulationStateSaveData Load(string filename = null)
    {
        if (filename is null)
            filename = defaultSaveFile;
        string json;
        try
        {
            json = File.ReadAllText(filename);
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading save data from file '" + filename + "': " + e);
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SimulationStateSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing JSON data from file '" + filename + "': " + e);
        }

        return null;
    }
}
