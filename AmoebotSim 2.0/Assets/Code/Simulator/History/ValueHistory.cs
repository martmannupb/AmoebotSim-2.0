using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

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
        /// <summary>
        /// The history of values. If two consecutively recorded
        /// values are equal, the second value is not stored.
        /// </summary>
        protected List<T> values;
        /// <summary>
        /// The history of rounds in which the recorded value
        /// has changed.
        /// </summary>
        protected List<int> rounds;
        /// <summary>
        /// The last round for which we have a recorded value.
        /// </summary>
        protected int lastRound;

        /// <summary>
        /// The currently marked round.
        /// </summary>
        protected int markedRound;
        /// <summary>
        /// The record index corresponding to the currently marked round.
        /// Useful for fast lookup.
        /// </summary>
        protected int markedIndex;
        /// <summary>
        /// Indicates whether the marked round is currently tracking
        /// the last round.
        /// </summary>
        protected bool isTracking;

        /// <summary>
        /// Creates a new history for a variable of type <typeparamref name="T"/>.
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

        // Empty constructor to allow subclasses specifying their constructors freely
        protected ValueHistory() { }

        // TODO: Check if the default comparison can be done better
        /// <summary>
        /// Equality comparison method for values of the parameter type
        /// <typeparamref name="T"/>. Can be overridden by inheriting
        /// classes to define specialized behavior for specific types.
        /// <para>
        /// Default implementation uses
        /// <see cref="EqualityComparer{T}.Default"/> for comparison.
        /// </para>
        /// </summary>
        /// <param name="val1">The first value to compare.</param>
        /// <param name="val2">The second value to compare.</param>
        /// <returns><c>true</c> if and only if <paramref name="val1"/>
        /// and <paramref name="val2"/> are equal in the sense that it
        /// is not necessary to create a new history entry.</returns>
        protected virtual bool ValuesEqual(T val1, T val2)
        {
            return EqualityComparer<T>.Default.Equals(val1, val2);
        }

        /// <summary>
        /// Returns the first round in which a value was recorded. This is the
        /// round in which the history of this variable begins; its value is
        /// undefined in all previous rounds.
        /// <para>See also <seealso cref="GetLastRecordedRound"/>.</para>
        /// </summary>
        /// <returns>The first round with a recorded value.</returns>
        public virtual int GetFirstRecordedRound()
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
        public virtual int GetLastRecordedRound()
        {
            return lastRound;
        }

        /// <summary>
        /// Returns the value the variable had in the specified round <paramref name="round"/>.
        /// </summary>
        /// <param name="round">The round for which to query. Must be at least
        /// <see cref="GetFirstRecordedRound"/>.</param>
        /// <returns>The value of the represented variable in round <paramref name="round"/>.</returns>
        public virtual T GetValueInRound(int round)
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
        public virtual void RecordValueInRound(T value, int round)
        {
            if (round < lastRound)
            {
                throw new System.ArgumentOutOfRangeException("Cannot record value for round earlier than last round.");
            }

            // New round is later than or equal to current round
            lastRound = round;
            // Check if we actually need a new entry or if we can just update the last recorded one
            if (round == rounds[rounds.Count - 1])
            {
                values[values.Count - 1] = value;
            }
            else if (!ValuesEqual(value, values[values.Count - 1]))
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

        /// <summary>
        /// Checks whether the round marker is currently
        /// tracking the latest round.
        /// </summary>
        /// <returns><c>true</c> if and only if the marker
        /// is tracking the latest round.</returns>
        public virtual bool IsTracking()
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
        public virtual void SetMarkerToRound(int round)
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
        public virtual void ContinueTracking()
        {
            isTracking = true;
            markedRound = rounds[rounds.Count - 1];
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
        public virtual void StepBack()
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
        public virtual void StepForward()
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
        public virtual int GetMarkedRound()
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
        public virtual T GetMarkedValue()
        {
            return values[markedIndex];
        }

        /// <summary>
        /// Like <see cref="RecordValueInRound(T, int)"/> but the recorded round is the
        /// round that is currently marked.
        /// </summary>
        /// <param name="value">The value to record in the currently marked round.</param>
        public virtual void RecordValueAtMarker(T value)
        {
            if (markedRound < lastRound)
            {
                throw new System.InvalidOperationException("Cannot record value when marker is in round earlier than last round.");
            }

            if (markedRound == rounds[rounds.Count - 1])
            {
                // Only update value of last round
                values[values.Count - 1] = value;
            }
            else
            {
                lastRound = markedRound;
                // Only record new entry if value is different
                if (!ValuesEqual(value, values[values.Count - 1]))
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
        public virtual void ShiftTimescale(int amount)
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
        public virtual void CutOffAfterRound(int round)
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
        public virtual void CutOffAtMarker()
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
            else if (round >= rounds[rounds.Count - 1])
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


        /*
         * Saving and loading functionality.
         */

        /// <summary>
        /// Creates a serializable object storing the history data so
        /// that it can be saved to and loaded from a file.
        /// </summary>
        /// <returns>A serializable representation of the history data.</returns>
        public virtual ValueHistorySaveData<T> GenerateSaveData()
        {
            ValueHistorySaveData<T> data = new ValueHistorySaveData<T>();

            data.values = values;
            data.rounds = rounds;
            data.lastRound = lastRound;

            return data;
        }

        /// <summary>
        /// Same as <see cref="GenerateSaveData"/>, but all values are stored as strings.
        /// </summary>
        /// <returns>A serializable representation of the history data, where
        /// the stored values are represented as strings.</returns>
        public ValueHistorySaveData<string> GenerateSaveDataString()
        {
            ValueHistorySaveData<string> data = new ValueHistorySaveData<string>();

            data.values = new List<string>(values.Count);
            foreach (T val in values)
                data.values.Add(val.ToString());
            data.rounds = rounds;
            data.lastRound = lastRound;

            return data;
        }

        /// <summary>
        /// Instantiates a <see cref="ValueHistory{T}"/> from the given save data.
        /// <para>
        /// The instance will start in the tracking state, following the latest recorded round.
        /// </para>
        /// </summary>
        /// <param name="data">A serializable object storing history data.</param>
        public ValueHistory(ValueHistorySaveData<T> data)
        {
            values = data.values;
            rounds = data.rounds;
            lastRound = data.lastRound;

            // Start in tracking state
            markedRound = lastRound;
            markedIndex = rounds.Count - 1;
            isTracking = true;
        }
    }

} // namespace AS2.Sim
