using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Developer API for partition sets.
    /// <para>
    /// This is part of the pin configuration API, see
    /// <see cref="AS2.Sim.PinConfiguration"/>.
    /// </para>
    /// <para>
    /// A partition set is a set of connected pins within
    /// a pin configuration. It belongs to an object
    /// implementing the <see cref="AS2.Sim.PinConfiguration"/>
    /// interface and may be changed through methods of
    /// this API or through the pin configuration itself.
    /// <br/>
    /// See also <seealso cref="Pin"/>.
    /// </para>
    /// </summary>
    public abstract class PartitionSet
    {
        /// <summary>
        /// The <see cref="AS2.Sim.PinConfiguration"/> to which this
        /// partition set belongs.
        /// </summary>
        public abstract PinConfiguration PinConfiguration
        {
            get;
        }

        /// <summary>
        /// The ID of this partition set.
        /// <para>
        /// Partition set IDs range from <c>0</c> to
        /// <c><see cref="PinConfiguration.NumPins"/> - 1</c>.
        /// </para>
        /// </summary>
        public abstract int Id
        {
            get;
        }

        /// <summary>
        /// Checks if this partition set is empty.
        /// </summary>
        /// <returns><c>true</c> if and only if this partition
        /// set does not contain any pins.</returns>
        public abstract bool IsEmpty();

        /// <summary>
        /// Returns the IDs of all pins contained in this
        /// partition set.
        /// <para>
        /// See also <seealso cref="GetPins"/>.
        /// </para>
        /// </summary>
        /// <returns>An array containing the IDs of all pins
        /// contained in this partition set, in ascending
        /// order.</returns>
        public abstract int[] GetPinIds();

        /// <summary>
        /// Returns all pins contained in this partition set.
        /// <para>
        /// See also <seealso cref="GetPinIds"/>.
        /// </para>
        /// </summary>
        /// <returns>An array containing all pins contained
        /// in this partition set, ordered by their IDs.</returns>
        public abstract Pin[] GetPins();

        /// <summary>
        /// Checks if the specified pin is contained in this
        /// partition set.
        /// <para>
        /// See also <seealso cref="ContainsPin(Pin)"/>.
        /// </para>
        /// </summary>
        /// <param name="pinId">The ID of the pin to be checked.</param>
        /// <returns><c>true</c> if and only if this partition set
        /// contains a pin with ID <paramref name="pinId"/>.</returns>
        public abstract bool ContainsPin(int pinId);

        /// <summary>
        /// Checks if the given pin is contained in this
        /// partition set.
        /// <para>
        /// See also <seealso cref="ContainsPin(int)"/>.
        /// </para>
        /// </summary>
        /// <param name="pin">The pin to be checked.</param>
        /// <returns><c>true</c> if and only if this partition set
        /// contains a pin with the same ID as the givne pin.</returns>
        public abstract bool ContainsPin(Pin pin);

        // TODO: Add bool return value to indicate what happened?

        /// <summary>
        /// Adds the specified pin to this partition set.
        /// <para>
        /// If the pin is not already contained in this set, it is
        /// removed from the set it is currently contained in.
        /// </para>
        /// <para>
        /// See also <seealso cref="AddPin(Pin)"/>.
        /// </para>
        /// </summary>
        /// <param name="pinId">The ID of the pin to be added.</param>
        public abstract void AddPin(int pinId);

        /// <summary>
        /// Adds the given pin to this partition set.
        /// <para>
        /// If the pin is not already contained in this set, it is
        /// removed from the set it is currently contained in.
        /// </para>
        /// <para>
        /// See also <seealso cref="AddPin(int)"/>.
        /// </para>
        /// </summary>
        /// <param name="pin">The pin to be added.</param>
        public abstract void AddPin(Pin pin);

        /// <summary>
        /// Adds the specified pins to this partition set.
        /// <para>
        /// Has the same effect as calling <see cref="AddPin(int)"/>
        /// multiple times.
        /// </para>
        /// <para>
        /// See also <seealso cref="AddPins(Pin[])"/>.
        /// </para>
        /// </summary>
        /// <param name="pinIds">An array containing the IDs of
        /// the pins to be added.</param>
        public abstract void AddPins(int[] pinIds);

        /// <summary>
        /// Adds the given pins to this partition set.
        /// <para>
        /// Has the same effect as calling <see cref="AddPin(Pin)"/>
        /// multiple times.
        /// </para>
        /// <para>
        /// See also <seealso cref="AddPins(int[])"/>.
        /// </para>
        /// </summary>
        /// <param name="pins">An array containing the pins
        /// to be added.</param>
        public abstract void AddPins(Pin[] pins);

        /// <summary>
        /// Tries to remove the specified pin from this partition set.
        /// <para>
        /// If the pin is currently contained in this partition
        /// set and some other partition set is currently empty, the
        /// pin is moved to that empty partition set. If no empty
        /// partition set can be found, an exception will be thrown.
        /// </para>
        /// <para>
        /// See also <seealso cref="RemovePin(Pin)"/>.
        /// </para>
        /// </summary>
        /// <param name="pinId">The ID of the pin to be removed.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the pin is the only one in this partition set and
        /// no other partition set is empty.
        /// </exception>
        public abstract void RemovePin(int pinId);

        /// <summary>
        /// Tries to remove the specified pin from this partition set.
        /// <para>
        /// If the pin is currently contained in this partition
        /// set and some other partition set is currently empty, the
        /// pin is moved to that empty partition set. If no empty
        /// partition set can be found, an exception will be thrown.
        /// </para>
        /// <para>
        /// See also <seealso cref="RemovePin(int)"/>.
        /// </para>
        /// </summary>
        /// <param name="pin">The pin to be removed.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the pin is the only one in this partition set and
        /// no other partition set is empty.
        /// </exception>
        public abstract void RemovePin(Pin pin);

        /// <summary>
        /// Tries to remove the specified pins from this partition set.
        /// <para>
        /// Has the same effect as calling <see cref="RemovePin(int)"/>
        /// multiple times.
        /// </para>
        /// <para>
        /// See also <seealso cref="RemovePins(Pin[])"/>.
        /// </para>
        /// </summary>
        /// <param name="pinIds">An array containing the IDs of
        /// the pins to be removed.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if a pin is the last one in this partition set and
        /// no other partition set is empty.
        /// </exception>
        public abstract void RemovePins(int[] pinIds);

        /// <summary>
        /// Tries to remove the given pins from this partition set.
        /// <para>
        /// Has the same effect as calling <see cref="RemovePin(Pin)"/>
        /// multiple times.
        /// </para>
        /// <para>
        /// See also <seealso cref="RemovePins(int[])"/>.
        /// </para>
        /// </summary>
        /// <param name="pins">An array containing the pins to
        /// be removed.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if a pin is the last one in this partition set and
        /// no other partition set is empty.
        /// </exception>
        public abstract void RemovePins(Pin[] pins);

        /// <summary>
        /// Merges the specified partition set into this one.
        /// </summary>
        /// <param name="otherId">The ID of the partition set whose
        /// pins should be moved to this set.</param>
        public abstract void Merge(int otherId);

        /// <summary>
        /// Merges the given partition set into this one.
        /// </summary>
        /// <param name="other">The partition set whose pins should
        /// be moved to this set.</param>
        public abstract void Merge(PartitionSet other);

        /// <summary>
        /// Checks whether this partition set has received a beep in
        /// the last round, if the pin configuration it belongs to
        /// is the current one.
        /// </summary>
        /// <returns><c>true</c> if and only if this partition set has
        /// received a beep in the last round.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this partition set does not belong to the current
        /// pin configuration.
        /// </exception>
        public abstract bool ReceivedBeep();

        /// <summary>
        /// Sends a beep on this partition set if the pin configuration
        /// it belongs to is the planned one.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this partition set does not belong to the planned
        /// pin configuration.
        /// </exception>
        public abstract void SendBeep();

        /// <summary>
        /// Checks whether this partition set has received a message
        /// in the last round, if the pin configuration it belongs to
        /// is the current one.
        /// </summary>
        /// <returns><c>true</c> if and only if this partition set has
        /// received a message in the last round.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this partition set does not belong to the current
        /// pin configuration.
        /// </exception>
        public abstract bool HasReceivedMessage();

        /// <summary>
        /// Returns the message this partition set has received in the
        /// last round, if it has received one and it belongs to the
        /// current pin configuration.
        /// </summary>
        /// <returns>The message received by this partition set in the
        /// last round, if it exists, otherwise <c>null</c>.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this partition set does not belong to the current
        /// pin configuration.
        /// </exception>
        public abstract Message GetReceivedMessage();

        /// <summary>
        /// Sends a message on this partition set if the pin configuration
        /// it belongs to is the planned one.
        /// </summary>
        /// <para>
        /// Note that a copy of the given <see cref="Message"/> instance
        /// <paramref name="msg"/> is sent. Altering the instance after calling
        /// this method has no effect on the sent message.
        /// </para>
        /// <param name="msg">The message to be sent.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this partition set does not belong to the planned
        /// pin configuration.
        /// </exception>
        public abstract void SendMessage(Message msg);

        /// <summary>
        /// Overrides the color of this partition set.
        /// <para>
        /// See <see cref="PinConfiguration.SetPartitionSetColor(int, Color)"/>.
        /// </para>
        /// </summary>
        /// <param name="color">The color in which to display this
        /// partition set.</param>
        public abstract void SetColor(Color color);

        /// <summary>
        /// Resets the color override of this partition set.
        /// <para>
        /// See <see cref="PinConfiguration.ResetPartitionSetColor(int)"/>.
        /// </para>
        /// </summary>
        public abstract void ResetColor();

        /// <summary>
        /// Sets the position of this partition set to the given polar
        /// coordinates. Only affects the position if the partition set
        /// contains at least two pins. Also sets the pin configuration's
        /// placement mode to <see cref="PSPlacementMode.MANUAL"/>.
        /// </summary>
        /// <param name="polarCoords">The polar coordinates
        /// <c>(angleDeg, distance)</c> of the partition set relative to
        /// the center of the particle. Angle <c>0</c> points in local
        /// <see cref="Direction.E"/> direction and angles increase in
        /// local counter-clockwise direction. Distance <c>1</c> places
        /// the partition set on the edge of the particle.</param>
        /// <param name="head">Determines whether the partition set in
        /// the particle's head or tail should be placed. For
        /// contracted particles, this should be <c>true</c>.</param>
        public abstract void SetPosition(Vector2 polarCoords, bool head = true);
    }

} // namespace AS2.Sim
