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
}
