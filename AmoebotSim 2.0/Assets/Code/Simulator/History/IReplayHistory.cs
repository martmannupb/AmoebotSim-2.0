using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface defining methods for objects that store a history
/// and that can be reset to a previous point in time.
/// <para>
/// Time is simply measured in rounds. The round number equals
/// the number of simulation rounds executed until that point
/// in time.
/// </para>
/// <para>
/// By default, the object's state evolves with the rounds that
/// are simulated. If the tracking marker is reset to a particular
/// round, the object reverts to its previous state recorded for
/// that round. In the tracking state, the object cannot be part
/// of the simulation anymore, but the marker can be used to step
/// through the recorded rounds or jump to any valid round. As
/// soon as the state tracking is reactivated, the marker will
/// follow the latest recorded state and the object will evolve
/// with the simulation again.
/// </para>
/// </summary>
public interface IReplayHistory
{
    /// <summary>
    /// Returns the first round for which a state has been recorded.
    /// <para>
    /// The object cannot be reset to a time before this round.
    /// </para>
    /// </summary>
    /// <returns>The first round for which a state has been recorded.</returns>
    int GetFirstRecordedRound();

    /// <summary>
    /// Checks whether the object is currently tracking the latest
    /// recorded state.
    /// <para>
    /// If the object is not tracking, it may not be usable in the regular
    /// way until the tracking is continued.
    /// </para>
    /// </summary>
    /// <returns>Whether the marker currently tracks the latest recorded state.</returns>
    bool IsTracking();

    /// <summary>
    /// Sets the tracking marker to the specified round, restores the
    /// state recorded for that point in time, and stops the marker
    /// from tracking the latest round.
    /// <para>
    /// The target round must not be earlier than the round returned by
    /// <see cref="GetFirstRecordedRound"/>.
    /// </para>
    /// </summary>
    /// <param name="round">The round to which the tracking marker
    /// should be set.</param>
    void SetMarkerToRound(int round);

    /// <summary>
    /// Moves the marker one round back and restores the object's recorded
    /// state for that round. Also stops it from tracking the last round if
    /// it was still tracking.
    /// <para>
    /// Must not be called when the marker is already at the first recorded
    /// round.
    /// </para>
    /// </summary>
    void StepBack();

    /// <summary>
    /// Moves the marker one round forward and restores the object's
    /// recorded state for that round. Also stops it from tracking the last
    /// round if it was still tracking.
    /// <para>
    /// May be ineffective if the object has a hard limit on the last
    /// recorded round.
    /// </para>
    /// </summary>
    void StepForward();

    /// <summary>
    /// Returns the round that is currently marked.
    /// <para>
    /// If the marker is tracking the object's state, this round will
    /// increase every time a new value is recorded.
    /// </para>
    /// </summary>
    /// <returns>The round that is currently marked.</returns>
    int GetMarkedRound();

    /// <summary>
    /// Resets the marker to track the latest recorded state and
    /// to continue evolving as the simulation progresses.
    /// </summary>
    void ContinueTracking();

    /// <summary>
    /// Deletes all recorded states after the currently marked round.
    /// </summary>
    void CutOffAtMarker();

    /// <summary>
    /// Shifts all records as well as the marker by the specified
    /// amount of rounds.
    /// </summary>
    /// <param name="amount">The number of rounds to add to each entry.
    /// May be negative.</param>
    void ShiftTimescale(int amount);
}
