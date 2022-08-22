using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Utility for saving and loading simulation states.
/// </summary>
public static class SaveStateUtility
{
    public static string saveFile = Application.persistentDataPath + "/savedata.json";

    public static bool Save(SimulationStateSaveData data)
    {
        string json = JsonUtility.ToJson(data, false);
        File.WriteAllText(saveFile, json);
        return true;
    }

    public static SimulationStateSaveData Load()
    {
        string json = System.IO.File.ReadAllText(saveFile);
        SimulationStateSaveData data = JsonUtility.FromJson<SimulationStateSaveData>(json);

        //foreach (ParticleStateSaveData d in data.particles)
        //{
        //    string s = "Values:\n";
        //    foreach (Vector2Int v in d.tailPositionHistory.values)
        //        s += v + "\n";
        //    s += "Rounds:\n";
        //    foreach (int r in d.tailPositionHistory.rounds)
        //        s += r + "\n";
        //    Debug.Log(s);
        //}

        return data;
    }
}
