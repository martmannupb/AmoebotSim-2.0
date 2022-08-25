
/// <summary>
/// Factory class for creating and initializing <see cref="ParticleAttribute{T}"/> subclass instances.
/// </summary>
public static class ParticleAttributeFactory
{
    /// <summary>
    /// Creates a new <see cref="ParticleAttribute_Int"/> and adds it to the given
    /// <see cref="Particle"/>'s list of attributes.
    /// </summary>
    /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
    /// <param name="name">The display name of the attribute.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>A newly initialized <see cref="ParticleAttribute_Int"/>.</returns>
    public static ParticleAttribute_Int CreateParticleAttributeInt(Particle p, string name, int initialValue)
    {
        ParticleAttribute_Int attr = new ParticleAttribute_Int(p, name, initialValue);
        p.AddAttribute(attr);
        return attr;
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute_Bool"/> and adds it to the given
    /// <see cref="Particle"/>'s list of attributes.
    /// </summary>
    /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
    /// <param name="name">The display name of the attribute.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>A newly initialized <see cref="ParticleAttribute_Bool"/>.</returns>
    public static ParticleAttribute_Bool CreateParticleAttributeBool(Particle p, string name, bool initialValue)
    {
        ParticleAttribute_Bool attr = new ParticleAttribute_Bool(p, name, initialValue);
        p.AddAttribute(attr);
        return attr;
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute_Direction"/> and adds it to the given
    /// <see cref="Particle"/>'s list of attributes.
    /// </summary>
    /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
    /// <param name="name">The display name of the attribute.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>A newly initialized <see cref="ParticleAttribute_Direction"/>.</returns>
    public static ParticleAttribute_Direction CreateParticleAttributeDirection(Particle p, string name, int initialValue)
    {
        ParticleAttribute_Direction attr = new ParticleAttribute_Direction(p, name, initialValue);
        p.AddAttribute(attr);
        return attr;
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute_Enum{T}"/> and adds it to the given
    /// <see cref="Particle"/>'s list of attributes.
    /// </summary>
    /// <typeparam name="EnumT">The enum type represented by the attribute.</typeparam>
    /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
    /// <param name="name">The display name of the attribute.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>A newly initialized <see cref="ParticleAttribute_Enum{T}"/>.</returns>
    public static ParticleAttribute_Enum<EnumT> CreateParticleAttributeEnum<EnumT>(Particle p, string name, EnumT initialValue) where EnumT : System.Enum
    {
        ParticleAttribute_Enum<EnumT> attr = new ParticleAttribute_Enum<EnumT>(p, name, initialValue);
        p.AddAttribute(attr);
        return attr;
    }

    /// <summary>
    /// Creates a new <see cref="ParticleAttribute_PinConfiguration"/> and adds it to
    /// the given <see cref="Particle"/>'s list of attributes.
    /// <para>
    /// Note the usage remarks of the <see cref="ParticleAttribute_PinConfiguration"/>
    /// class.
    /// </para>
    /// </summary>
    /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
    /// <param name="name">The display name of the attribute.</param>
    /// <param name="initialValue">The initial attribute value.</param>
    /// <returns>A newly initialized <see cref="ParticleAttribute_PinConfiguration"/>.</returns>
    public static ParticleAttribute_PinConfiguration CreateParticleAttributePinConfiguration(Particle p, string name, PinConfiguration initialValue)
    {
        ParticleAttribute_PinConfiguration attr = new ParticleAttribute_PinConfiguration(p, name, initialValue);
        p.AddAttribute(attr);
        return attr;
    }
}
