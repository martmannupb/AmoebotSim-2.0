using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValueHistoryMessage : ValueHistory<Message>
{
    public ValueHistoryMessage(Message initialValue, int initialRound = 0) : base(initialValue, initialRound) { }

    // Compares two Messages using their specific comparison method
    // Also takes care of null values
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

    /**
     * Saving and loading functionality.
     */

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <returns></returns>
    public override ValueHistorySaveData<Message> GenerateSaveData()
    {
        return null;
    }

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
