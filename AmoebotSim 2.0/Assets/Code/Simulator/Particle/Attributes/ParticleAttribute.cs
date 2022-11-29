
namespace Simulator
{

    /// <summary>
    /// Representation of an attribute that is part of a particle's state.
    /// <para>
    /// <see cref="ParticleAlgorithm"/> subclasses should use instances of
    /// <see cref="ParticleAttribute{T}"/> to represent their state
    /// variables. Only these attributes are displayed and editable in the
    /// simulation UI and recorded in a simulation history. They also provide
    /// correct read and write behavior in synchronous rounds, i.e., a
    /// <see cref="ParticleAttribute{T}"/> provides its most recently written
    /// value only to the particle it belongs to, and it always returns the
    /// value from the previous round to other particles.
    /// </para>
    /// <para>
    /// <see cref="ParticleAttribute{T}"/>s are created using the factory
    /// methods provided in the <see cref="ParticleAlgorithm"/> base class
    /// (see <see cref="ParticleAlgorithm.CreateAttributeInt(string, int)"/> etc.).
    /// For example, an integer attribute is declared and initialized with the
    /// value <c>3</c> as follows:
    /// <code>
    /// public class MyAlgorithm : ParticleAlgorithm {
    ///     public ParticleAttribute<![CDATA[<int>]]> myAttr;
    ///     public MyAlgorithm(Particle p) : base(p) {
    ///         myAttr = CreateAttributeInt("Fancy display name", 3);
    ///     }
    /// }
    /// </code>
    /// Display names of attributes must be unique because they are used to
    /// identify attributes when saving and loading simulation states.
    /// </para>
    /// <para>
    /// Note the difference between the <see cref="GetValue"/> and
    /// <see cref="GetValue_After"/> methods. Reading a <see cref="ParticleAttribute{T}"/>
    /// like a variable of type <typeparamref name="T"/> will return the same value as
    /// <see cref="GetValue"/>. Depending on the desired semantics, it may be helpful to
    /// wrap attributes in properties when writing a particle algorithm.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of values the attribute stores.</typeparam>
    public abstract class ParticleAttribute<T> : ParticleAttributeBase
    {
        public ParticleAttribute(Particle particle, string name) : base(particle, name) { }

        /// <summary>
        /// Returns the attribute's value from the snapshot taken at the
        /// beginning of the current round.
        /// <para>
        /// The return value is not changed by assigning new values to this attribute!
        /// To get the latest assigned value, use <see cref="GetValue_After"/>.
        /// </para>
        /// </summary>
        /// <returns>The attribute value at the beginning of the current round.</returns>
        public abstract T GetValue();

        /// <summary>
        /// Returns the latest value of this attribute.
        /// <para>
        /// The return value changes based on value assignments made within this
        /// activation. Use <see cref="GetValue"/> to get the attribute's value at
        /// the beginning of the current round.
        /// </para>
        /// </summary>
        /// <returns>The latest value of this attribute.</returns>
        public abstract T GetValue_After();

        /// <summary>
        /// Assigns the given value to this attribute.
        /// <para>
        /// Note that this value will only be visible to other particles in the
        /// next round.
        /// </para>
        /// </summary>
        /// <param name="value">The new value assigned to this attribute.</param>
        public abstract void SetValue(T value);

        public static implicit operator T(ParticleAttribute<T> attr) => attr.GetValue();
    }

} // namespace Simulator
