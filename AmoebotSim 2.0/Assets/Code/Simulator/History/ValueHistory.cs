using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a variable of type <typeparamref name="T"/> and its
/// entire value history starting at some round.
/// <para>
/// The history can be extended by adding records for later rounds or
/// updating the value for the currently last round, but it cannot be
/// changed for any round before the last round with a record unless
/// it is cut off.
/// </para>
/// <para>
/// The value in a round before the first recorded round is not defined.
/// </para>
/// <para>
/// The history also provides a marker mechanism that allows the value
/// history to be traversed in sequence. The marker starts at the initial
/// round and tracks the latest round until it is set to some round
/// using <see cref="SetMarkerToRound(int)"/> or moved by one step using
/// <see cref="StepBack"/> or <see cref="StepForward"/>. It can be reset
/// to track the last round using <see cref="ContinueTracking"/>.
/// </para>
/// </summary>
/// <typeparam name="T">The type of the represented variable. Must
/// implement an equality check.</typeparam>
public class ValueHistory<T> : IReplayHistory
{
    private List<T> values;     // Stores the different values
    private List<int> rounds;   // Stores the rounds in which values were changed
    private int lastRound;      // Stores the last round for which we have a certain recorded value

    private int markedRound;    // The currently marked round
    private int markedIndex;    // The index corresponding to the currently marked round for fast lookup
    private bool isTracking;    // Is true while the marked round is tracking the last round

    /// <summary>
    /// Creates a new history record for a variable of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="initialValue">The first recorded value of the variable.</param>
    /// <param name="initialRound">The round in which this value is recorded.</param>
    public ValueHistory(T initialValue, int initialRound = 0)
    {
        values = new List<T>();
        rounds = new List<int>();
        values.Add(initialValue);
        rounds.Add(initialRound);
        lastRound = initialRound;

        markedRound = initialRound;
        markedIndex = 0;
        isTracking = true;
    }

    /// <summary>
    /// Returns the first round in which a value was recorded. This is the
    /// round in which the history of this variable begins; its value is
    /// undefined in all previous rounds.
    /// <para>See also <seealso cref="GetLastRecordedRound"/>.</para>
    /// </summary>
    /// <returns>The first round with a recorded value.</returns>
    public int GetFirstRecordedRound()
    {
        return rounds[0];
    }

    /// <summary>
    /// Returns the last round in which a value was recorded. The history
    /// may only be updated for rounds greater than or equal to this round.
    /// Queries for rounds after this one will return the value that was
    /// recorded for this round.
    /// <para>See also <seealso cref="GetFirstRecordedRound"/>.</para>
    /// </summary>
    /// <returns>The last round with a recorded value.</returns>
    public int GetLastRecordedRound()
    {
        return lastRound;
    }

    /// <summary>
    /// Returns the value the variable had in the specified round <paramref name="round"/>.
    /// </summary>
    /// <param name="round">The round for which to query. Must be at least
    /// <see cref="GetFirstRecordedRound"/>.</param>
    /// <returns>The value of the represented variable in round <paramref name="round"/>.</returns>
    public T GetValueInRound(int round)
    {
        return values[GetIndexOfRound(round)];
    }

    /// <summary>
    /// Adds a value for the specified round to the variable record.
    /// <para>
    /// The value is assumed to remain unchanged between the previously
    /// last round and <paramref name="round"/>.
    /// </para>
    /// </summary>
    /// <param name="value">The value to be recorded.</param>
    /// <param name="round">The round for which the value should be recorded.
    /// Must be greater than or equal to the last recorded round.</param>
    public void RecordValueInRound(T value, int round)
    {
        if (round < lastRound)
        {
            throw new System.ArgumentOutOfRangeException("Cannot record value for round earlier than last round.");
        }

        // New round is later than or equal to current round
        lastRound = round;
        // Check if we actually need a new entry or if we can just update the last recorded one
        if (round == rounds[^1])
        {
            values[^1] = value;
        }
        // TODO: Check if this comparison can be done better
        else if (!EqualityComparer<T>.Default.Equals(value, values[^1]))
        {
            values.Add(value);
            rounds.Add(round);
        }

        // Update tracking
        if (isTracking)
        {
            markedRound = round;
            markedIndex = rounds.Count - 1;
        }
    }

    public bool IsTracking()
    {
        return isTracking;
    }

    /// <summary>
    /// Sets the marker to the specified round and stops it from
    /// tracking the latest round.
    /// </summary>
    /// <param name="round">The round to which the marker should be set. Must not
    /// be smaller than the first recorded round.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown if <paramref name="round"/> is less than the first recorded round.
    /// </exception>
    public void SetMarkerToRound(int round)
    {
        if (round < rounds[0])
        {
            throw new System.ArgumentOutOfRangeException("Cannot place marker in round " + round + ", which is earlier than the first round " + rounds[0]);
        }
        isTracking = false;
        markedRound = round;
        markedIndex = GetIndexOfRound(round);
    }

    /// <summary>
    /// Resets the marker to continue tracking the last recorded round.
    /// </summary>
    public void ContinueTracking()
    {
        isTracking = true;
        markedRound = rounds[^1];
        markedIndex = rounds.Count - 1;
    }

    /// <summary>
    /// Moves the marker one round back and stops it from tracking the last round.
    /// <para>Must not be called when the marker is already at the first recorded
    /// round.</para>
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the marker is already at the first recorded round.
    /// </exception>
    public void StepBack()
    {
        if (markedRound == rounds[0])
        {
            throw new System.InvalidOperationException("Cannot move marker before the first round " + rounds[0]);
        }
        if (markedRound == rounds[markedIndex])
        {
            markedIndex--;
        }
        markedRound--;
        isTracking = false;
    }

    /// <summary>
    /// Moves the marker one round forward and stops it from tracking the last round.
    /// </summary>
    public void StepForward()
    {
        markedRound++;
        if (markedIndex < rounds.Count - 1 && markedRound == rounds[markedIndex + 1])
        {
            markedIndex++;
        }
        isTracking = false;
    }

    /// <summary>
    /// Returns the round that is currently marked.
    /// <para>
    /// See <see cref="SetMarkerToRound(int)"/>,
    /// <see cref="StepBack"/>, <see cref="StepForward"/>.
    /// </para>
    /// </summary>
    /// <returns>The currently marked round.</returns>
    public int GetMarkedRound()
    {
        return markedRound;
    }

    /// <summary>
    /// Returns the value recorded for the currently marked round.
    /// <para>
    /// Note that the returned value may still change if the marked round
    /// is greater than or equal to the last recorded round and the
    /// history is updated with new values.
    /// </para>
    /// </summary>
    /// <returns>The recorded or predicted value for the currently marked round.</returns>
    public T GetMarkedValue()
    {
        return values[markedIndex];
    }

    /// <summary>
    /// Like <see cref="RecordValueInRound(T, int)"/> but the recorded round is the
    /// round that is currently marked.
    /// </summary>
    /// <param name="value">The value to record in the currently marked round.</param>
    public void RecordValueAtMarker(T value)
    {
        if (markedRound < lastRound)
        {
            throw new System.InvalidOperationException("Cannot record value when marker is in round earlier than last round.");
        }

        if (markedRound == rounds[^1])
        {
            // Only update value of last round
            values[^1] = value;
        }
        else
        {
            lastRound = markedRound;
            // Only record new entry if value is different
            if (!EqualityComparer<T>.Default.Equals(value, values[^1]))
            {
                values.Add(value);
                rounds.Add(markedRound);
            }
        }
    }

    /// <summary>
    /// Shifts all records as well as the marker by the specified
    /// amount of rounds.
    /// </summary>
    /// <param name="amount">The number of rounds to add to each entry.
    /// May be negative.</param>
    public void ShiftTimescale(int amount)
    {
        for (int i = 0; i < rounds.Count; i++)
        {
            rounds[i] += amount;
        }
        lastRound += amount;
        markedRound += amount;
    }

    /// <summary>
    /// Deletes all records after the specified round, turning this
    /// round into the last round if it is earlier than the current
    /// last round.
    /// </summary>
    /// <param name="round">The round after which all records should be removed.</param>
    public void CutOffAfterRound(int round)
    {
        int idx = GetIndexOfRound(round);
        // Cut off list elements after index
        if (idx < rounds.Count - 1)
        {
            values.RemoveRange(idx + 1, values.Count - idx - 1);
            rounds.RemoveRange(idx + 1, rounds.Count - idx - 1);
        }
        if (round < lastRound)
        {
            lastRound = round;
        }

        if (isTracking)
        {
            markedRound = lastRound;
        }
        markedIndex = GetIndexOfRound(markedRound);
    }

    /// <summary>
    /// Deletes all records after the currently marked round.
    /// If the marked round is earlier than the current
    /// last round, the marked round becomes the new last round.
    /// </summary>
    public void CutOffAtMarker()
    {
        if (markedIndex < rounds.Count)
        {
            values.RemoveRange(markedIndex + 1, values.Count - markedIndex - 1);
            rounds.RemoveRange(markedIndex + 1, rounds.Count - markedIndex - 1);
        }
        if (markedRound < lastRound)
        {
            lastRound = markedRound;
        }
    }

    /// <summary>
    /// Finds the list index of the record that specifies the value in
    /// the given round. The round of that record is at most <paramref name="round"/>
    /// and the next recorded round is greater than <paramref name="round"/>, if it
    /// exists.
    /// </summary>
    /// <param name="round">The round for which to find the corresponding index.
    /// Must be at least the first recorded round.</param>
    /// <returns>The list index of the last record before <paramref name="round"/>.</returns>
    private int GetIndexOfRound(int round)
    {
        if (round < rounds[0])
        {
            throw new System.ArgumentOutOfRangeException("Cannot find entry for round " + round + " as first recorded round is " + rounds[0]);
        }
        else if (round >= rounds[^1])
        {
            return rounds.Count - 1;
        }

        // Do a binary search to find the correct index
        // Element at left will always be <= round, element at right will always be > round
        int left = 0;
        int right = rounds.Count - 1;
        int middle;
        while (right > left + 1)
        {
            middle = (left + right) / 2;
            if (rounds[middle] > round)
            {
                right = middle;
            }
            else
            {
                left = middle;
            }
        }
        return left;
    }

    /// <summary>
    /// Implicit conversion that returns the currently marked value
    /// </summary>
    /// <param name="history">The history whose marked value to return.</param>
    public static implicit operator T(ValueHistory<T> history) => history.values[history.markedIndex];

    public void Print()
    {
        string s = "History (rounds " + rounds[0] + " - " + lastRound + ") with " + rounds.Count + " records:\n";
        foreach (int r in rounds)
        {
            s += r + " ";
        }
        s += "\n";
        foreach (T v in values)
        {
            s += v + " ";
        }
        Debug.Log(s);
    }
}
