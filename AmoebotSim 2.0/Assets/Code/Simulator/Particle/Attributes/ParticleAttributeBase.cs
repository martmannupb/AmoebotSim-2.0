
namespace AS2.Sim
{

    /// <summary>
    /// Abstract base class for all particle attributes.
    /// <para>
    /// Stores a reference to the <see cref="Particle"/> containing
    /// the attribute and the name of the attribute.
    /// </para>
    /// </summary>
    public abstract class ParticleAttributeBase
    {
        /// <summary>
        /// The <see cref="Particle"/> to which the attribute belongs.
        /// </summary>
        protected Particle particle;
        /// <summary>
        /// The unique name of the attribute.
        /// </summary>
        protected string name;

        public ParticleAttributeBase(Particle particle, string name)
        {
            this.particle = particle;
            this.name = name;
        }
    }

} // namespace AS2.Sim
