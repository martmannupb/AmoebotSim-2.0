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
    /// Specialized value history that stores <see cref="Message"/> data and
    /// compares messages using their custom equality check.
    /// </summary>
    public class ValueHistoryMessage : ValueHistory<Message>
    {
        public ValueHistoryMessage(Message initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

        /// <summary>
        /// Compares two messages using their specific comparison
        /// method. <c>null</c> values can be handled.
        /// </summary>
        /// <param name="val1">The first message to compare.</param>
        /// <param name="val2">The second message to compare.</param>
        /// <returns><c>true</c> if and only if </returns>
        protected override bool ValuesEqual(Message val1, Message val2)
        {
            if (val1 == val2)
            {
                return true;
            }
            else if (val1 == null || val2 == null)
            {
                return false;
            }
            return val1.Equals(val2);
        }

        /*
         * Saving and loading functionality.
         */

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <returns><c>null</c></returns>
        public override ValueHistorySaveData<Message> GenerateSaveData()
        {
            return null;
        }

        /// <summary>
        /// Generates value history save data specifically for serialized
        /// <see cref="Message"/> data. Use this instead of <see cref="GenerateSaveData"/>.
        /// </summary>
        /// <returns>A serializable object storing history data from which
        /// <see cref="Message"/>s can be restored.</returns>
        public ValueHistorySaveData<MessageSaveData> GenerateMessageSaveData()
        {
            ValueHistorySaveData<MessageSaveData> data = new ValueHistorySaveData<MessageSaveData>();

            data.values = new List<MessageSaveData>(values.Count);
            foreach (Message m in values)
            {
                data.values.Add(m is null ? null : m.GenerateSaveData());
            }

            data.rounds = rounds;
            data.lastRound = lastRound;

            return data;
        }

        /// <summary>
        /// Same as <see cref="ValueHistory{T}.ValueHistory(ValueHistorySaveData{T})"/> but
        /// specialized for <see cref="Message"/> history data.
        /// </summary>
        /// <param name="data">The serializable history data from which to restore the
        /// <see cref="ValueHistoryMessage"/> instance.</param>
        public ValueHistoryMessage(ValueHistorySaveData<MessageSaveData> data)
        {
            values = new List<Message>(data.values.Count);
            foreach (MessageSaveData mData in data.values)
            {
                values.Add(Message.CreateFromSaveData(mData));
            }
            rounds = data.rounds;
            lastRound = data.lastRound;

            // Start in tracking state
            markedRound = lastRound;
            markedIndex = rounds.Count - 1;
            isTracking = true;
        }
    }

} // namespace AS2.Sim
