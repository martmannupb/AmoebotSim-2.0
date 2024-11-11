
namespace AS2.Sim
{

    /// <summary>
    /// Developer API for pins.
    /// <para>
    /// This is part of the pin configuration API, see
    /// <see cref="PinConfiguration"/>.
    /// </para>
    /// <para>
    /// A pin represents a connection point on the edge of a particle
    /// through which it can communicate with neighboring particles
    /// using beeps or entire messages. Each edge incident to a
    /// particle has the same number of pins and the pins are ordered
    /// according to each particle's chirality. This means that
    /// the same pin may have different IDs for the two particles.
    /// Pins are locally identified by IDs computed from the
    /// label of the edge to which they belong and their offset on
    /// that edge.
    /// </para>
    /// <para>
    /// Every pin is part of a pin configuration and as such, it
    /// belongs to a partition set at all times. Pins cannot be
    /// created or destroyed, they can only be moved from one
    /// partition set to another.
    /// </para>
    /// </summary>
    public abstract class Pin
    {
        /// <summary>
        /// The partition set to which this pin currently belongs.
        /// </summary>
        public abstract PartitionSet PartitionSet
        {
            get;
        }

        /// <summary>
        /// The ID of this pin.
        /// </summary>
        public abstract int Id
        {
            get;
        }

        /// <summary>
        /// The local direction of the edge on which this pin is located.
        /// </summary>
        public abstract Direction Direction
        {
            get;
        }

        /// <summary>
        /// The offset of this pin on its edge.
        /// <para>
        /// The pins on an edge are ordered according to the chirality of
        /// the particle. Their offsets range from <c>0</c> to
        /// <c><see cref="PinConfiguration.PinsPerEdge"/> - 1</c>.
        /// </para>
        /// </summary>
        public abstract int Offset
        {
            get;
        }

        /// <summary>
        /// Whether the edge this pin belongs to is on the particle's head.
        /// <para>
        /// See also <seealso cref="IsOnTail"/>.
        /// </para>
        /// </summary>
        public abstract bool IsOnHead
        {
            get;
        }

        /// <summary>
        /// Whether the edge this pin belongs to is on the particle's tail.
        /// <para>
        /// See also <seealso cref="IsOnHead"/>.
        /// </para>
        /// </summary>
        public abstract bool IsOnTail
        {
            get;
        }

        /// <summary>
        /// Checks whether this pin has received a beep in
        /// the last round, if the pin configuration it belongs to
        /// is the previous one.
        /// </summary>
        /// <returns><c>true</c> if and only if this pin has
        /// received a beep in the last round.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this pin does not belong to the previous
        /// pin configuration.
        /// </exception>
        public abstract bool ReceivedBeep();

        /// <summary>
        /// Sends a beep on this pin if the pin configuration
        /// it belongs to is the next one.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this pin does not belong to the next
        /// pin configuration.
        /// </exception>
        public abstract void SendBeep();

        /// <summary>
        /// Checks whether this pin has received a message
        /// in the last round, if the pin configuration it belongs to
        /// is the previous one.
        /// </summary>
        /// <returns><c>true</c> if and only if this pin has
        /// received a message in the last round.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this pin does not belong to the previous
        /// pin configuration.
        /// </exception>
        public abstract bool HasReceivedMessage();

        /// <summary>
        /// Returns the message this pin has received in the
        /// last round, if it has received one and it belongs to the
        /// previous pin configuration.
        /// </summary>
        /// <returns>The message received by this pin in the
        /// last round, if it exists, otherwise <c>null</c>.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this pin does not belong to the previous
        /// pin configuration.
        /// </exception>
        public abstract Message GetReceivedMessage();

        /// <summary>
        /// Sends a message on this pin if the pin configuration
        /// it belongs to is the next one.
        /// </summary>
        /// <para>
        /// Note that a copy of the given <see cref="Message"/> instance
        /// <paramref name="msg"/> is sent. Altering the instance after calling
        /// this method has no effect on the sent message.
        /// </para>
        /// <param name="msg">The message to be sent.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if this pin does not belong to the next
        /// pin configuration.
        /// </exception>
        public abstract void SendMessage(Message msg);
    }

} // namespace AS2.Sim
