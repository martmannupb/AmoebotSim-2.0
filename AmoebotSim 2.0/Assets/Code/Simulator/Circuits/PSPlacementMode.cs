
namespace AS2
{

    /// <summary>
    /// Different modes of placing partition sets within a particle.
    /// </summary>
    public enum PSPlacementMode
    {
        /// <summary>
        /// Use the default placement that is set in the UI.
        /// </summary>
        NONE,
        /// <summary>
        /// Arrange the partition sets evenly on a straight line.
        /// </summary>
        LINE,
        /// <summary>
        /// Use a version of Lloyd's algorithm to place the partition
        /// sets on a circle to roughly match their pin positions
        /// without being too close to each other.
        /// </summary>
        LLOYD,
        /// <summary>
        /// Use polar coordinates to set every partition set's
        /// position manually.
        /// </summary>
        MANUAL
    }

} // namespace AS2
