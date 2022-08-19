using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specialized value history that stores compressed pin configuration data.
/// </summary>
public class ValueHistoryPinConfiguration : ValueHistory<SysPinConfiguration>
{
    private ValueHistory<int> idxHistory;           // This history stores the actual history state and the indices
    private List<PinConfigurationSaveData> configs; // This list contains all pin configurations we have seen so far

    public ValueHistoryPinConfiguration(SysPinConfiguration initialValue, int initialRound = 0) : base(null, 0)
    {
        idxHistory = new ValueHistory<int>(0, initialRound);
        configs = new List<PinConfigurationSaveData>();
        if (initialValue != null)
            configs.Add(initialValue.GenerateSaveData());
        else
            configs.Add(null);
    }

    // Override all ValueHistory methods
    
    public override int GetFirstRecordedRound()
    {
        return idxHistory.GetFirstRecordedRound();
    }

    public override int GetLastRecordedRound()
    {
        return idxHistory.GetLastRecordedRound();
    }

    /// <summary>
    /// Not supported, use <see cref="GetValueInRound(int, Particle)"/> instead.
    /// <para>
    /// A <see cref="Particle"/> is required to reconstruct a
    /// <see cref="SysPinConfiguration"/> from only its
    /// <see cref="PinConfigurationSaveData"/>.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public override SysPinConfiguration GetValueInRound(int round)
    {
        throw new System.InvalidOperationException();
    }

    /// <summary>
    /// Replaces <see cref="ValueHistory{T}.GetValueInRound(int)"/>.
    /// <para>
    /// A <see cref="Particle"/> is required to reconstruct a
    /// <see cref="SysPinConfiguration"/> from its
    /// <see cref="PinConfigurationSaveData"/>.
    /// </para>
    /// </summary>
    /// <param name="round">The round from which to get the pin configuration.</param>
    /// <param name="p">The particle used to reconstruct
    /// the pin configuration. Must be in a matching expansion state.</param>
    /// <returns>A pin configuration reconstructed from the configuration data
    /// recorded for round <paramref name="round"/>, using the particle
    /// <paramref name="p"/>.</returns>
    public SysPinConfiguration GetValueInRound(int round, Particle p)
    {
        int idx = idxHistory.GetValueInRound(round);
        return new SysPinConfiguration(configs[idx], p);
    }

    public override void RecordValueInRound(SysPinConfiguration value, int round)
    {
        PinConfigurationSaveData d;
        if (value == null)
        {
            d = null;
        }
        else
        {
            d = value.GenerateSaveData();
        }
        int idx = FindOrAddConfig(d);
        idxHistory.RecordValueInRound(idx, round);
    }

    public override bool IsTracking()
    {
        return idxHistory.IsTracking();
    }

    public override void SetMarkerToRound(int round)
    {
        idxHistory.SetMarkerToRound(round);
    }

    public override void ContinueTracking()
    {
        idxHistory.ContinueTracking();
    }

    public override void StepBack()
    {
        idxHistory.StepBack();
    }

    public override void StepForward()
    {
        idxHistory.StepForward();
    }

    public override int GetMarkedRound()
    {
        return idxHistory.GetMarkedRound();
    }

    /// <summary>
    /// Not supported, use <see cref="GetMarkedValue(Particle)"/> instead.
    /// <para>
    /// A <see cref="Particle"/> is required to reconstruct a
    /// <see cref="SysPinConfiguration"/> from only its
    /// <see cref="PinConfigurationSaveData"/>.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public override SysPinConfiguration GetMarkedValue()
    {
        throw new System.InvalidOperationException();
    }

    /// <summary>
    /// Replaces <see cref="ValueHistory{T}.GetMarkedValue"/>.
    /// <para>
    /// A <see cref="Particle"/> is required to reconstruct a
    /// <see cref="SysPinConfiguration"/> from its
    /// <see cref="PinConfigurationSaveData"/>.
    /// </para>
    /// </summary>
    /// <param name="p">The particle used to reconstruct
    /// the pin configuration. Must be in a matching expansion state.</param>
    /// <returns>A pin configuration reconstructed from the currently
    /// marked configuration data using the particle <paramref name="p"/>.</returns>
    public SysPinConfiguration GetMarkedValue(Particle p)
    {
        int idx = idxHistory.GetMarkedValue();
        return new SysPinConfiguration(configs[idx], p);
    }

    public override void RecordValueAtMarker(SysPinConfiguration value)
    {
        PinConfigurationSaveData d;
        if (value == null)
        {
            d = null;
        }
        else
        {
            d = value.GenerateSaveData();
        }
        int idx = FindOrAddConfig(d);
        idxHistory.RecordValueAtMarker(idx);
    }

    public override void ShiftTimescale(int amount)
    {
        idxHistory.ShiftTimescale(amount);
    }

    public override void CutOffAfterRound(int round)
    {
        idxHistory.CutOffAfterRound(round);
    }

    public override void CutOffAtMarker()
    {
        idxHistory.CutOffAtMarker();
    }

    /// <summary>
    /// Implicit conversion that returns the currently marked value
    /// </summary>
    /// <param name="history">The history whose marked value to return.</param>
    public static implicit operator SysPinConfiguration(ValueHistoryPinConfiguration history) => history.GetMarkedValue();

    /// <summary>
    /// Helper to find a given pin configuration in the list of encountered
    /// configurations or insert it and then return its index.
    /// </summary>
    /// <param name="d">The compressed version of the pin configuration to find
    /// or insert.</param>
    /// <returns>The index of <paramref name="d"/> in the list of encountered
    /// pin configurations.</returns>
    private int FindOrAddConfig(PinConfigurationSaveData d)
    {
        for (int i = 0; i < configs.Count; i++)
        {
            if (configs[i] == d)
                return i;
        }
        configs.Add(d);
        return configs.Count - 1;
    }


    /**
     * Saving and loading functionality.
     */

    /// <summary>
    /// Not supported
    /// </summary>
    /// <returns></returns>
    public override ValueHistorySaveData<SysPinConfiguration> GenerateSaveData()
    {
        return null;
    }

    public PinConfigurationHistorySaveData GeneratePCSaveData()
    {
        PinConfigurationHistorySaveData data = new PinConfigurationHistorySaveData();

        data.idxHistory = idxHistory.GenerateSaveData();
        data.configs = configs;

        return data;
    }
}
