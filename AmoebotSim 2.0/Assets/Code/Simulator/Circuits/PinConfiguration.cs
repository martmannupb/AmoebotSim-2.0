using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Use generic collections instead of (or in addition to) arrays?

/// <summary>
/// Developer API for pin configuration definition.
/// <para>
/// Represents a complete pin configuration for a fixed
/// expansion state. The object contains <see cref="PartitionSet"/>
/// and <see cref="Pin"/> instances which are connected such that
/// changes to one of the objects may cause changes in any of the
/// other objects. For example, adding a pin to a partition set
/// causes it to be removed from the partition set it was previously
/// contained in.
/// </para>
/// <para>
/// The position of a pin is uniquely defined by the edge it is
/// located on and its offset on the edge. The pin indices on each
/// edge are 0-based and increase in (local) counter-clockwise
/// direction.
/// </para>
/// <para>
/// Pins and partition sets have fixed IDs from <c>0</c>
/// to <c><see cref="NumPins"/> - 1</c>. The pin IDs are
/// defined as <c>edgeLabel * <see cref="PinsPerEdge"/> + edgeOffset</c>.
/// This means that the pins are numbered consecutively in
/// (local) counter-clockwise direction, starting at edge label <c>0</c>
/// and edge offset <c>0</c>.
/// </para>
/// </summary>
public abstract class PinConfiguration
{
    /// <summary>
    /// The head direction which encodes the expansion state of
    /// this pin configuration. If the state is contracted, this
    /// value is <c>-1</c>.
    /// <para>
    /// A pin configuration can only be applied if its expansion
    /// state matches the particle's expansion state at the end of
    /// the round.
    /// </para>
    /// </summary>
    public abstract int HeadDirection
    {
        get;
    }

    /// <summary>
    /// The number of pins per edge. This number is defined as
    /// a constant by the particle algorithm.
    /// </summary>
    public abstract int PinsPerEdge
    {
        get;
    }

    /// <summary>
    /// The total number of pins in the pin configuration.
    /// <para>
    /// For contracted particles, this number equals
    /// <c>6 * <see cref="PinsPerEdge"/></c>, and for expanded
    /// particles, it is <c>10 * <see cref="PinsPerEdge"/></c>.
    /// </para>
    /// </summary>
    public abstract int NumPins
    {
        get;
    }

    /// <summary>
    /// Returns the pin with the given offset at the given edge.
    /// </summary>
    /// <param name="direction">The direction of the edge containing the pin.
    /// Must be in the range <c>0,...,5</c>.</param>
    /// <param name="offset">The pin offset on the specified edge. Must be in
    /// the range <c>0,...,<see cref="PinsPerEdge"/>-1</c>.</param>
    /// <param name="head">If the expansion state of the pin configuration is
    /// expanded, use this flag to indicate whether the edge belongs to the
    /// particle's head or not.</param>
    /// <returns>The pin in the specified location.</returns>
    public abstract Pin GetPinAt(int direction, int offset, bool head = true);

    /// <summary>
    /// Returns all pins at the given edge.
    /// </summary>
    /// <param name="direction">The direction of the edge containing the pin.
    /// Must be in the range <c>0,...,5</c>.</param>
    /// <param name="head">If the expansion state of the pin configuration is
    /// expanded, use this flag to indicate whether the edge belongs to the
    /// particle's head or not.</param>
    /// <returns>An array of size <see cref="PinsPerEdge"/> containing the
    /// pins at the specified edge, ordered by their edge offsets.</returns>
    public abstract Pin[] GetPinsAtEdge(int direction, bool head = true);

    /// <summary>
    /// Returns the partition set with the given ID.
    /// <para>
    /// Note that the ID of a partition set does not provide any information
    /// on the partition set's content. It is merely used to identify and
    /// refer to the partition set and to provide more control over which
    /// partition sets contain which pins.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the partition set to get. Must be
    /// in the range <c>0.,,,.<see cref="NumPins"/>-1</c>.</param>
    /// <returns>The partition set with the given ID  <paramref name="index"/>.</returns>
    public abstract PartitionSet GetPartitionSet(int index);

    /// <summary>
    /// Returns all partition sets in the pin configuration.
    /// <para>
    /// Note that the number of partition sets is always equal to <see cref="NumPins"/>
    /// since this is the maximum number of non-empty partition sets. Use
    /// <see cref="GetNonEmptyPartitionSets"/> to get only the partition sets that
    /// contain at least one pin.
    /// </para>
    /// </summary>
    /// <returns>An array of size <see cref="NumPins"/> containing all partition
    /// sets in this pin configuration. The index of a partition set in this array is
    /// its ID. Some of the partition sets may be empty.</returns>
    public abstract PartitionSet[] GetPartitionSets();

    /// <summary>
    /// Returns all partition sets in the pin configuration that contain at
    /// least one pin.
    /// <para>
    /// See also <seealso cref="GetPartitionSets"/>.
    /// </para>
    /// </summary>
    /// <returns>An array containing the non-empty partition sets, ordered by their
    /// IDs. The number of returned partition sets is between <c>1</c> and
    /// <see cref="NumPins"/>.</returns>
    public abstract PartitionSet[] GetNonEmptyPartitionSets();

    /// <summary>
    /// Sets the pin configuration to the singleton pattern.
    /// <para>
    /// In the singleton pattern, every pin constitutes its own
    /// partition set of size <c>1</c>, i.e., there are no connections
    /// between any pins.
    /// </para>
    /// <para>
    /// Additionally, in this implementation, the pins are assigned to the
    /// partition sets such that their IDs match.
    /// </para>
    /// </summary>
    public abstract void SetToSingleton();

    /// <summary>
    /// Sets the pin configuration to the global pattern.
    /// <para>
    /// In the global pattern, all pins are contained in a single
    /// partition set. If all particles in a connected system have
    /// this pin configuration, they form a global circuit on all
    /// pins simultaneously.
    /// </para>
    /// </summary>
    /// <param name="partitionSetId">The ID of the partition set that
    /// should hold the pins.</param>
    public abstract void SetToGlobal(int partitionSetId = 0);

    /// <summary>
    /// Same as <see cref="SetToGlobal(int)"/>, but the partition set
    /// to hold the pins is specified directly instead of by its ID.
    /// </summary>
    /// <param name="partitionSet">The partition set that should hold
    /// all pins.</param>
    public abstract void SetToGlobal(PartitionSet partitionSet);

    /// <summary>
    /// Sets the specified partition set to contain all pins with
    /// the given edge offset <paramref name="offset"/>.
    /// <para>
    /// The partition set will thus contain exactly one pin for each
    /// edge. If it already contains any pins that do not have this
    /// offset, they are removed and put into their own singleton
    /// sets.
    /// </para>
    /// <para>
    /// See also <seealso cref="SetStarConfig(int, PartitionSet)"/>,
    /// <seealso cref="SetStarConfig(int, bool[], int)"/>.
    /// </para>
    /// </summary>
    /// <param name="offset">The edge offset of the pins to be
    /// collected in the star partition set.</param>
    /// <param name="partitionSetIndex">The ID of the partition set
    /// that should hold the star pattern.</param>
    public abstract void SetStarConfig(int offset, int partitionSetIndex);

    /// <summary>
    /// Same as <see cref="SetStarConfig(int, int)"/>, but the partition
    /// set is specified directly and not by its ID.
    /// </summary>
    /// <param name="offset">The edge offset of the pins to be
    /// collected in the star partition set.</param>
    /// <param name="partitionSet">The partition set that should hold
    /// the star pattern.</param>
    public abstract void SetStarConfig(int offset, PartitionSet partitionSet);

    /// <summary>
    /// Sets the specified partition set to contain all pins with
    /// the given edge offset <paramref name="offset"/>, where the
    /// offsets of certain edges are inverted as specified by the
    /// <paramref name="inverted"/> array.
    /// <para>
    /// This is an extension of the basic
    /// <see cref="SetStarConfig(int, int)"/> method.
    /// </para>
    /// </summary>
    /// <param name="offset">The edge offset of the pins to be
    /// collected in the star partition set.</param>
    /// <param name="inverted">Array specifying for which edges the
    /// offset should be inverted. For any edge with a <c>true</c> entry,
    /// the inverted offset is computed as <c><see cref="PinsPerEdge"/> -
    /// 1 - <paramref name="offset"/></c>. The array indices correspond to
    /// edge labels. The array must have length <c>6</c> for a contracted
    /// state and <c>10</c> for an expanded state.</param>
    /// <param name="partitionSetIndex">The ID of the partition set that
    /// should hold the star pattern.</param>
    public abstract void SetStarConfig(int offset, bool[] inverted, int partitionSetIndex);

    /// <summary>
    /// Same as <see cref="SetStarConfig(int, bool[], int)"/>, but the
    /// partition set is specified directly and not by its ID.
    /// </summary>
    /// <param name="offset">The edge offset of the pins to be
    /// collected in the star partition set.</param>
    /// <param name="inverted">Array specifying for which edges the
    /// offset should be inverted. For any edge with a <c>true</c> entry,
    /// the inverted offset is computed as <c><see cref="PinsPerEdge"/> -
    /// 1 - <paramref name="offset"/></c>. The array indices correspond to
    /// edge labels. The array must have length <c>6</c> for a contracted
    /// state and <c>10</c> for an expanded state.</param>
    /// <param name="partitionSet">The partition set that should hold
    /// the star pattern.</param>
    public abstract void SetStarConfig(int offset, bool[] inverted, PartitionSet partitionSet);

    /// <summary>
    /// Sets the specified partition set to contain exactly the given pins.
    /// <para>
    /// If the partition set already contains any pins that are not part of
    /// the given set, they are removed and put into their own singleton
    /// sets. If <paramref name="pinIds"/> is empty, it may be impossible
    /// to put every removed pin into a singleton set, in which case an
    /// exception is thrown.
    /// </para>
    /// <para>
    /// The <paramref name="pinIds"/> array will be sorted in ascending
    /// order.
    /// </para>
    /// </summary>
    /// <param name="pinIds">The IDs of the pins to be put into the
    /// specified partition set.</param>
    /// <param name="partitionSetIndex">The ID of the partition set that
    /// should hold the specified set of pins.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if <paramref name="pinIds"/> is empty and not all pins can
    /// be inserted into empty partition sets.
    /// </exception>
    public abstract void MakePartitionSet(int[] pinIds, int partitionSetIndex);

    /// <summary>
    /// Same as <see cref="MakePartitionSet(int[], int)"/>, but the pins
    /// are specified directly and not by their IDs. The array will not
    /// be sorted.
    /// </summary>
    /// <param name="pins">The pins to be put into the specified
    /// partition set.</param>
    /// <param name="partitionSetIndex">The ID of the partition set that
    /// should hold the given set of pins.</param>
    public abstract void MakePartitionSet(Pin[] pins, int partitionSetIndex);

    /// <summary>
    /// Same as <see cref="MakePartitionSet(int[], int)"/>, but the
    /// partition set is specified directly instead of by its ID.
    /// </summary>
    /// <param name="pinIds">The IDs of the pins to be put into the
    /// given partition set.</param>
    /// <param name="partitionSet">The partition set that should hold
    /// the specified set of pins.</param>
    public abstract void MakePartitionSet(int[] pinIds, PartitionSet partitionSet);

    /// <summary>
    /// Same as <see cref="MakePartitionSet(int[], int)"/>, but the pins
    /// and the partition set are specified directly and not by their IDs.
    /// The array of pins will not be sorted.
    /// </summary>
    /// <param name="pins">The pins to be put into the given
    /// partition set.</param>
    /// <param name="partitionSetIndex">The partition set that
    /// should hold the given set of pins.</param>
    public abstract void MakePartitionSet(Pin[] pins, PartitionSet partitionSet);

    /// <summary>
    /// Checks whether the specified partition set has received a beep in the
    /// last round, if this pin configuration is the current one.
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set to check.</param>
    /// <returns><c>true</c> if and only if the partition set with ID
    /// <paramref name="partitionSetIndex"/> has received a beep.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if this pin configuration is not the current one.
    /// </exception>
    public abstract bool ReceivedBeepOnPartitionSet(int partitionSetIndex);

    /// <summary>
    /// Sends a beep on the specified partition set, if this is the planned pin
    /// configuration.
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set on which
    /// to send the beep.</param>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if this pin configuration is not the planned one.
    /// </exception>
    public abstract void SendBeepOnPartitionSet(int partitionSetIndex);

    /// <summary>
    /// Checks whether the specified partition set hat received a message in
    /// the last round, if this pin configuration is the current one.
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set to check.</param>
    /// <returns><c>true</c> if and only if the partition set with ID
    /// <paramref name="partitionSetIndex"/> has received a message.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if this pin configuration is not the current one.
    /// </exception>
    public abstract bool ReceivedMessageOnPartitionSet(int partitionSetIndex);

    /// <summary>
    /// Returns the message received by the specified partition set, if it has
    /// received a message and this pin configuration is the current one.
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set to get the
    /// message from.</param>
    /// <returns>A <see cref="Message"/> instance received by the partition set
    /// with ID <paramref name="partitionSetIndex"/>, if it has received one,
    /// otherwise <c>null</c>.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if this pin configuration is not the current one.
    /// </exception>
    public abstract Message GetReceivedMessageOfPartitionSet(int partitionSetIndex);

    /// <summary>
    /// Sends the given message on the specified partition set, if this pin
    /// configuration is the planned one.
    /// <para>
    /// Note that a copy of the given <see cref="Message"/> instance
    /// <paramref name="msg"/> is sent. Altering the instance after calling
    /// this method has no effect on the sent message.
    /// </para>
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set on which
    /// to send the message.</param>
    /// <param name="msg">The message to be sent.</param>
    public abstract void SendMessageOnPartitionSet(int partitionSetIndex, Message msg);

    /// <summary>
    /// Overrides the specified partition set's color. The circuit to which
    /// the partition set belongs will be displayed in this color unless it
    /// has been overridden by another partition set already.
    /// <para>
    /// Overriding the color of partition sets is only possible for planned
    /// pin configurations. Planning a new pin configuration will reset all
    /// partition set colors.
    /// </para>
    /// <para>
    /// See also <seealso cref="ResetPartitionSetColor(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set whose
    /// color to override.</param>
    /// <param name="color">The color to set for the partition set.</param>
    public abstract void SetPartitionSetColor(int partitionSetIndex, Color color);

    /// <summary>
    /// Resets the specified partition set's color override.
    /// <para>
    /// See <see cref="SetPartitionSetColor(int, Color)"/>.
    /// </para>
    /// </summary>
    /// <param name="partitionSetIndex">The ID of the partition set whose
    /// color override to reset.</param>
    public abstract void ResetPartitionSetColor(int partitionSetIndex);
}
