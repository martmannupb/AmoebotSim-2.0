// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections.Generic;

namespace AS2.Sim
{

    /// <summary>
    /// Specialized value history that stores compressed pin configuration data.
    /// <para>
    /// Each pin configuration is only stored once to save memory. The history
    /// itself simply stores identifiers instead of whole pin configurations.
    /// </para>
    /// </summary>
    public class ValueHistoryPinConfiguration : ValueHistory<SysPinConfiguration>
    {
        /// <summary>
        /// Stores the actual history state and indices of pin configurations.
        /// </summary>
        private ValueHistory<int> idxHistory;
        /// <summary>
        /// Contains all pin configurations we have seen so far. Pin configurations
        /// are only stored once to save memory.
        /// </summary>
        private List<PinConfigurationSaveData> configs;

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
            PinConfigurationSaveData d = configs[idx];
            if (d is null)
                return null;
            return new SysPinConfiguration(d, p);
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
            PinConfigurationSaveData d = configs[idx];
            if (d is null)
                return null;
            return new SysPinConfiguration(d, p);
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


        /*
         * Saving and loading functionality.
         */

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <returns></returns>
        public override ValueHistorySaveData<SysPinConfiguration> GenerateSaveData()
        {
            return null;
        }

        /// <summary>
        /// Generates value history save data specifically for serialized
        /// pin configuration data. Use this instead of <see cref="GenerateSaveData"/>.
        /// </summary>
        /// <returns>A serializable object storing history data from which
        /// pin configurations can be restored.</returns>
        public PinConfigurationHistorySaveData GeneratePCSaveData()
        {
            PinConfigurationHistorySaveData data = new PinConfigurationHistorySaveData();

            data.idxHistory = idxHistory.GenerateSaveData();
            // Convert list of configurations to array and replace null entries
            // with special null instance to prevent JSON utility from
            // initializing the null entries on its own
            data.configs = configs.ToArray();
            for (int i = 0; i < data.configs.Length; i++)
                if (data.configs[i] is null)
                    data.configs[i] = PinConfigurationSaveData.NullInstance;

            return data;
        }

        /// <summary>
        /// Same as <see cref="ValueHistory{T}.ValueHistory(ValueHistorySaveData{T})"/> but
        /// specialized for pin configuration history data.
        /// </summary>
        /// <param name="data">The serializable history data from which to restore the
        /// <see cref="ValueHistoryPinConfiguration"/> instance.</param>
        public ValueHistoryPinConfiguration(PinConfigurationHistorySaveData data)
        {
            idxHistory = new ValueHistory<int>(data.idxHistory);
            configs = new List<PinConfigurationSaveData>(data.configs);
            // Turn null instances back into proper null entries
            for (int i = 0; i < configs.Count; i++)
                if (configs[i].isNull)
                    configs[i] = null;
        }
    }

} // namespace AS2.Sim
