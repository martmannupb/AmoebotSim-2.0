using System;

namespace AS2
{

    /// <summary>
    /// Container base for serializing particle attributes.
    /// </summary>
    [Serializable]
    public abstract class ParticleAttributeSaveDataBase
    {
        /// <summary>
        /// The display name given to the particle attribute. An attribute
        /// can only be loaded if its name is not changed.
        /// </summary>
        public string name;
    }

    /// <summary>
    /// Container for serializing the simplest kind of particle attributes,
    /// <see cref="AS2.Sim.ParticleAttributeWithHistory{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of values stored in the attribute history.</typeparam>
    [Serializable]
    public class ParticleAttributeSaveData<T> : ParticleAttributeSaveDataBase
    {
        /// <summary>
        /// The serializable history data of the attribute.
        /// </summary>
        public ValueHistorySaveData<T> history;
    }

    /// <summary>
    /// Specialized container for serializing particle attributes for enum values.
    /// </summary>
    [Serializable]
    public class ParticleAttributeEnumSaveData : ParticleAttributeSaveData<string>
    {
        /// <summary>
        /// The full type name of the stored enum type.
        /// </summary>
        public string enumType;
    }

    /// <summary>
    /// Specialized container for serializing particle attributes for pin configurations.
    /// </summary>
    [Serializable]
    public class ParticleAttributePCSaveData : ParticleAttributeSaveDataBase
    {
        /// <summary>
        /// The history of compressed pin configurations stored in the attribute.
        /// </summary>
        public PinConfigurationHistorySaveData history;
    }

} // namespace AS2
