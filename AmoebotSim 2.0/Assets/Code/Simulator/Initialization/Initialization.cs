
namespace AS2
{

    /// <summary>
    /// Contains constants for the initialization mode.
    /// </summary>
    public static class Initialization
    {
        /// <summary>
        /// Particle chirality settings including a random setting.
        /// </summary>
        public enum Chirality { Clockwise, CounterClockwise, Random }

        /// <summary>
        /// Particle compass directions including a random setting.
        /// </summary>
        public enum Compass { E = 0, NNE = 1, NNW = 2, W = 3, SSW = 4, SSE = 5, Random = -1 }
    }

} // namespace AS2
