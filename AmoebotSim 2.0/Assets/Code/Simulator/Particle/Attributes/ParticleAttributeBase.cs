
namespace Simulator
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
        protected Particle particle;
        protected string name;

        public ParticleAttributeBase(Particle particle, string name)
        {
            this.particle = particle;
            this.name = name;
        }
    }

} // namespace Simulator
