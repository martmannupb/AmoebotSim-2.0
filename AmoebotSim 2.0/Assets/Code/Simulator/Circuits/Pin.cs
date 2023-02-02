
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
    }

} // namespace AS2.Sim
