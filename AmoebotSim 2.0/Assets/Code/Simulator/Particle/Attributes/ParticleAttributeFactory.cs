// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.



namespace AS2.Sim
{

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
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
            ParticleAttribute_Int attr = new ParticleAttribute_Int(p, name, initialValue);
            p.AddAttribute(attr);
            return attr;
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute_Float"/> and adds it to the given
        /// <see cref="Particle"/>'s list of attributes.
        /// </summary>
        /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
        /// <param name="name">The display name of the attribute.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>A newly initialized <see cref="ParticleAttribute_Float"/>.</returns>
        public static ParticleAttribute_Float CreateParticleAttributeFloat(Particle p, string name, float initialValue)
        {
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
            ParticleAttribute_Float attr = new ParticleAttribute_Float(p, name, initialValue);
            p.AddAttribute(attr);
            return attr;
        }

        /// <summary>
        /// Creates a new <see cref="ParticleAttribute_String"/> and adds it to the given
        /// <see cref="Particle"/>'s list of attributes.
        /// </summary>
        /// <param name="p">The <see cref="Particle"/> to which the attribute should belong.</param>
        /// <param name="name">The display name of the attribute.</param>
        /// <param name="initialValue">The initial attribute value.</param>
        /// <returns>A newly initialized <see cref="ParticleAttribute_String"/>.</returns>
        public static ParticleAttribute_String CreateParticleAttributeString(Particle p, string name, string initialValue)
        {
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
            ParticleAttribute_String attr = new ParticleAttribute_String(p, name, initialValue);
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
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
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
        public static ParticleAttribute_Direction CreateParticleAttributeDirection(Particle p, string name, Direction initialValue)
        {
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
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
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
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
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
            ParticleAttribute_PinConfiguration attr = new ParticleAttribute_PinConfiguration(p, name, initialValue);
            p.AddAttribute(attr);
            return attr;
        }

        /// <summary>
        /// Creates a particle attribute matching the desired type if a corresponding
        /// attribute type exists. The attribute is not added to the given particle's
        /// list of attributes.
        /// </summary>
        /// <param name="p">The particle holding the attribute.</param>
        /// <param name="type">The data type stored by the attribute.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="initialValue">The initial value of the attribute. If <c>null</c>,
        /// an appropriate default value is selected.</param>
        /// <returns>A new attribute instance if the type could be matched,
        /// otherwise <c>null</c>.</returns>
        public static IParticleAttribute CreateParticleAttribute(Particle p, System.Type type, string name, object initialValue)
        {
            CheckParticleInConstruction(p);
            CheckAttributeName(name);
            if (type == typeof(bool))
            {
                return new ParticleAttribute_Bool(p, name, initialValue != null ? (bool)initialValue : false);
            }
            else if (type == typeof(Direction))
            {
                return new ParticleAttribute_Direction(p, name, initialValue != null ? (Direction)initialValue : Direction.NONE);
            }
            else if (type == typeof(float))
            {
                return new ParticleAttribute_Float(p, name, initialValue != null ? (float)initialValue : 0f);
            }
            else if (type == typeof(int))
            {
                return new ParticleAttribute_Int(p, name, initialValue != null ? (int)initialValue : 0);
            }
            else if (type == typeof(string))
            {
                return new ParticleAttribute_String(p, name, initialValue != null ? (string)initialValue : "");
            }
            else if (type.IsEnum)
            {
                try
                {
                    System.Type enumAttrType = typeof(ParticleAttribute_Enum<>);
                    enumAttrType = enumAttrType.MakeGenericType(new System.Type[] { type });
                    System.Reflection.ConstructorInfo ctor = enumAttrType.GetConstructor(new System.Type[] { typeof(Particle), typeof(string), type });
                    IParticleAttribute attr = (IParticleAttribute)ctor.Invoke(new object[] { p, name, initialValue });
                    return attr;
                }
                catch (System.Exception e)
                {
                    Log.Error("Error while trying to instantiate enum attribute for enum type '" + type + "':\n" + e);
                }
            }

            Log.Error("Cannot create attribute: Unsupported type '" + type + "'.");
            return null;
        }

        /// <summary>
        /// Checks whether the given particle is currently in construction or
        /// <c>null</c> and throws an exception otherwise. This is done to
        /// prevent attributes from being created outside of the constructor.
        /// </summary>
        /// <param name="p">The particle to check.</param>
        private static void CheckParticleInConstruction(Particle p)
        {
            if (p != null && !p.inConstructor)
            {
                throw new SimulatorStateException("Particle attributes can only be created in the constructor.");
            }
        }

        /// <summary>
        /// Checks whether the given attribute name is valid and
        /// throws an exception if it is not.
        /// <para>
        /// Invalid attribute names are <c>"Chirality"</c> and
        /// <c>"Compass Dir"</c> because these names are used by the
        /// particle panel to represent a particle's chirality and
        /// compass direction, which are not actually attributes.
        /// </para>
        /// </summary>
        /// <param name="name">The attribute names to check.</param>
        private static void CheckAttributeName(string name)
        {
            if (name.Equals("Chirality") || name.Equals("Compass Dir"))
            {
                throw new SimulatorStateException("Particle attributes cannot have the name '" + name + "'");
            }
        }
    }

} // namespace AS2.Sim
