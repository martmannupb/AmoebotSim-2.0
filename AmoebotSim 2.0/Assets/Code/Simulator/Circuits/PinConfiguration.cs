// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Developer API for pin configuration definition.
    /// <para>
    /// Represents a complete pin configuration for a fixed
    /// expansion state. The object contains <see cref="AS2.Sim.PartitionSet"/>
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
        /// The local head direction which encodes the expansion state
        /// of this pin configuration. If the state is contracted, this
        /// value is <see cref="Direction.NONE"/>.
        /// <para>
        /// A pin configuration can only be applied if its expansion
        /// state matches the particle's expansion state at the end of
        /// the round.
        /// </para>
        /// </summary>
        public abstract Direction HeadDirection
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
        /// Helper to compute the ID of the pin on the specified edge with the
        /// given offset.
        /// <para>
        /// The formula for the pin ID is <c>label * <paramref name="pinsPerEdge"/> +
        /// <paramref name="offset"/></c>, where <c>label</c> is computed
        /// using <paramref name="direction"/>, <paramref name="headDirection"/> and
        /// <paramref name="head"/>.
        /// </para>
        /// </summary>
        /// <param name="direction">The local direction of the edge.</param>
        /// <param name="offset">The edge offset of the pin.</param>
        /// <param name="head">If the pin configuration represents the
        /// expanded state, this flag indicates whether the edge belongs to
        /// the particle's head or not.</param>
        /// <returns>The ID of the pin in the location specified by an edge
        /// and an edge offset.</returns>
        public static int GetPinId(Direction direction, int offset, int pinsPerEdge, Direction headDirection = Direction.NONE, bool head = true)
        {
            return ParticleSystem_Utils.GetLabelInDir(direction, headDirection, head) * pinsPerEdge + offset;
        }

        /// <summary>
        /// Returns the pin with the given offset at the given edge.
        /// </summary>
        /// <param name="direction">The direction of the edge containing the pin.</param>
        /// <param name="offset">The pin offset on the specified edge. Must be in
        /// the range <c>0,...,<see cref="PinsPerEdge"/>-1</c>.</param>
        /// <param name="head">If the expansion state of the pin configuration is
        /// expanded, use this flag to indicate whether the edge belongs to the
        /// particle's head or not.</param>
        /// <returns>The pin in the specified location.</returns>
        public abstract Pin GetPinAt(Direction direction, int offset, bool head = true);

        /// <summary>
        /// Returns all pins at the given edge.
        /// </summary>
        /// <param name="direction">The direction of the edge containing the pins.</param>
        /// <param name="head">If the expansion state of the pin configuration is
        /// expanded, use this flag to indicate whether the edge belongs to the
        /// particle's head or not.</param>
        /// <returns>An array of size <see cref="PinsPerEdge"/> containing the
        /// pins at the specified edge, ordered by their edge offsets.</returns>
        public abstract Pin[] GetPinsAtEdge(Direction direction, bool head = true);

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
        /// Overrides the specified partition set's color. The circuit to which
        /// the partition set belongs will be displayed in this color unless it
        /// has been overridden by another partition set already.
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

        /// <summary>
        /// Resets the color overrides of all partition sets.
        /// </summary>
        public abstract void ResetAllPartitionSetColors();

        /// <summary>
        /// Sets the specified partition set's position in polar coordinates and
        /// sets the positioning mode of the pin configuration to
        /// <see cref="PSPlacementMode.MANUAL"/>.
        /// </summary>
        /// <param name="partitionSetIndex">The ID of the partition set whose
        /// position to set.</param>
        /// <param name="polarCoords">The polar coordinates of the partition
        /// set's position inside of the particle as <c>(angle, distance)</c>.
        /// An angle of <c>0</c> points in local direction
        /// <see cref="Direction.E"/> and angles increase in the local
        /// counter-clockwise direction. Distance <c>0</c> is the center
        /// of the particle (part) and distance <c>1</c> is on the edge
        /// of the particle.
        /// </param>
        /// <param name="head">Indicates whether the position in the
        /// particle's head or tail should be set. For contracted particles,
        /// this should be <c>true</c>.</param>
        public abstract void SetPartitionSetPosition(int partitionSetIndex, Vector2 polarCoords, bool head = true);

        /// <summary>
        /// Sets the partition set placement mode to the given value.
        /// </summary>
        /// <param name="mode">The new partition set placement mode.</param>
        /// <param name="head">Indicates whether the placement mode for
        /// the particle's head or tail will be set.</param>
        public abstract void SetPSPlacementMode(PSPlacementMode mode, bool head = true);

        /// <summary>
        /// Sets the rotation of the line on which the partition sets are
        /// arranged in <see cref="PSPlacementMode.LINE"/> mode.
        /// Multiples of 30 or 60 degrees will usually look best.
        /// The placement mode is changed automatically.
        /// </summary>
        /// <param name="angle">The new angle of the line in degrees.
        /// <c>0</c> means vertical (perpendicular to the local
        /// <see cref="Direction.E"/> direction) and increasing angles
        /// rotate the line in local counter-clockwise direction.</param>
        /// <param name="head">Indicates whether the rotation for the
        /// particle's head or tail part should be set. Must be
        /// <c>true</c> for contracted particles.</param>
        public abstract void SetLineRotation(float angle, bool head = true);

        /// <summary>
        /// Resets the positions of the partition sets and sets the
        /// placement mode to <see cref="PSPlacementMode.NONE"/> in the
        /// particle's head or tail.
        /// </summary>
        /// <param name="head">Indicates whether the partition sets
        /// should be reset in the particle's head or tail.</param>
        public abstract void ResetPartitionSetPlacement(bool head = true);

        /// <summary>
        /// Specifies whether the given partition set should always be drawn
        /// with a handle, even if it is a singleton set (but not if it
        /// is empty).
        /// </summary>
        /// <param name="partitionSetIndex">The ID of the partition set
        /// whose handle draw setting to override.</param>
        /// <param name="drawHandle">Whether the handle should always
        /// be drawn.</param>
        public abstract void SetPartitionSetDrawHandle(int partitionSetIndex, bool drawHandle);

        /// <summary>
        /// Resets the handle draw flags of all partition sets in the given
        /// part of the particle to <c>false</c>.
        /// </summary>
        /// <param name="head">Whether the flag should be reset in the
        /// particle's head or tail.</param>
        public abstract void ResetPartitionSetDrawHandle(bool head = true);


        #region Deprecated methods

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
        [Obsolete("This method is part of the old pin configuration system and does not work anymore. To receive beeps and messages, call the methods directly in the algorithm code or use GetCurrPinConfiguration.")]
        public bool ReceivedBeepOnPartitionSet(int partitionSetIndex) { return false; }

        /// <summary>
        /// Sends a beep on the specified partition set, if this is the planned pin
        /// configuration.
        /// </summary>
        /// <param name="partitionSetIndex">The ID of the partition set on which
        /// to send the beep.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this pin configuration is not the planned one.
        /// </exception>
        [Obsolete("This method is part of the old pin configuration system and does not work anymore. To send beeps and messages, call the methods directly in the algorithm code or use GetNextPinConfiguration.")]
        public void SendBeepOnPartitionSet(int partitionSetIndex) { }

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
        [Obsolete("This method is part of the old pin configuration system and does not work anymore. To receive beeps and messages, call the methods directly in the algorithm code or use GetCurrPinConfiguration.")]
        public bool ReceivedMessageOnPartitionSet(int partitionSetIndex) { return false; }

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
        [Obsolete("This method is part of the old pin configuration system and does not work anymore. To receive beeps and messages, call the methods directly in the algorithm code or use GetCurrPinConfiguration.")]
        public Message GetReceivedMessageOfPartitionSet(int partitionSetIndex) { return null; }

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
        [Obsolete("This method is part of the old pin configuration system and does not work anymore. To send beeps and messages, call the methods directly in the algorithm code or use GetNextPinConfiguration.")]
        public void SendMessageOnPartitionSet(int partitionSetIndex, Message msg) { }

        #endregion
    }

} // namespace AS2.Sim
