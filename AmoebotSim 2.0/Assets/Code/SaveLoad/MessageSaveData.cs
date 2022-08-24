using System;
using System.Collections.Generic;

/// <summary>
/// Container for serializing arbitrary <see cref="Message"/> objects.
/// <para>
/// Because Unity's JSON deserializer requires the types of the objects
/// to parse, which are not known when reading from a save file, all
/// data stored in a <see cref="Message"/> is serialized by name, type
/// and a string representation of its value. With the name of the
/// <see cref="Message"/> subtype, reflection is then used to restore
/// the original object. See
/// <see cref="Message.CreateFromSaveData(MessageSaveData)"/> for
/// details.
/// </para>
/// </summary>
[Serializable]
public class MessageSaveData
{
    /// <summary>
    /// The full name of the <see cref="Message"/> subtype.
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
