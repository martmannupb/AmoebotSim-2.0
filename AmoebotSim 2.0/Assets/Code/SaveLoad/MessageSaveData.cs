using System;
using System.Collections.Generic;

namespace AS2
{

    /// <summary>
    /// Container for serializing arbitrary <see cref="AS2.Sim.Message"/> objects.
    /// <para>
    /// Because Unity's JSON deserializer requires the types of the objects
    /// to parse, which are not known when reading from a save file, all
    /// data stored in a <see cref="AS2.Sim.Message"/> is serialized by name, type
    /// and a string representation of its value. With the name of the
    /// <see cref="AS2.Sim.Message"/> subtype, reflection is then used to restore
    /// the original object. See
    /// <see cref="AS2.Sim.Message.CreateFromSaveData(MessageSaveData)"/> for
    /// details.
    /// </para>
    /// </summary>
    [Serializable]
    public class MessageSaveData
    {
        /// <summary>
        /// The full name of the <see cref="AS2.Sim.Message"/> subtype.
        /// </summary>
        public string messageType;

        /// <summary>
        /// The names of the message's fields.
        /// </summary>
        public List<string> names;

        /// <summary>
        /// The types of the message's fields.
        /// </summary>
        public List<string> types;

        /// <summary>
        /// The values of the message's fields, represented as strings.
        /// </summary>
        public List<string> values;
    }

} // namespace AS2
