using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AS2.Sim
{

    // TODO: Could also define priorities directly using a Priority property

    /// <summary>
    /// Abstract base class for messages to be sent via the
    /// circuit system.
    /// <para>
    /// To use the messaging system, message types must be
    /// created as classes that inherit from this base class.
    /// Instances of the concrete message types can then be
    /// sent and received through partition sets, just like
    /// regular beeps.
    /// </para>
    /// <para>
    /// Messages use a priority system to solve conflicts:
    /// If multiple particles send different messages on a
    /// circuit in the same round, only the message with the
    /// highest priority is received by the particles on the
    /// circuit. Thus, the <see cref="GreaterThan(Message)"/>
    /// and <see cref="Equals(Message)"/> methods must be
    /// implemented such that they define a strict total
    /// order on the set of all possible messages.
    /// </para>
    /// <para>
    /// To ensure that messages can be saved and loaded correctly,
    /// every subclass must provide a parameterless default
    /// constructor and may only have simple members of
    /// primitive types like <c>int</c>, <c>bool</c>,
    /// <c>string</c>, or enums. In particular, no reference
    /// types or data structures like lists or arrays are
    /// supported as members.
    /// </para>
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        /// Creates a deep copy of this message.
        /// <para>
        /// The main purpose of this method is to prevent particles
        /// from sharing instances of messages due to reference
        /// semantics. Changing data in a message a particle has
        /// received (for example to forward the altered message
        /// later) can cause errors if this method does not create
        /// a deep copy of the original message.
        /// </para>
        /// </summary>
        /// <returns>A deep copy of this message.</returns>
        public abstract Message Copy();

        /// <summary>
        /// Checks whether this message is equivalent to the given
        /// other message.
        /// <para>
        /// This method should only return <c>true</c> if the two
        /// compared messages are equivalent in the sense that any
        /// particle receiving one of the messages will behave
        /// exactly as if it had received the other message.
        /// In other words, the messages have to be equivalent
        /// with respect to the total order defined on all
        /// possible messages.
        /// </para>
        /// <para>
        /// Note that this method and <see cref="GreaterThan(Message)"/>
        /// must not both return <c>true</c> when comparing the
        /// same instances.
        /// </para>
        /// </summary>
        /// <param name="other">The message that should be
        /// compared to this one.</param>
        /// <returns><c>true</c> if and only if the two messages
        /// are equivalent with respect to the total ordering of
        /// all messages.</returns>
        public abstract bool Equals(Message other);

        /// <summary>
        /// Checks whether this message has a higher priority
        /// than the given other message.
        /// <para>
        /// This is the main comparison operator defining the
        /// total order on the set of all possible messages.
        /// If the order is not strict, the message that is
        /// received in the case that multiple messages are
        /// sent on the same circuit in the same round may
        /// not be well-defined.
        /// </para>
        /// <para>
        /// Note that this method and <see cref="Equals(Message)"/>
        /// must not both return <c>true</c> when comparing the
        /// same instances.
        /// </para>
        /// </summary>
        /// <param name="other">The message that should be
        /// compared to this one.</param>
        /// <returns><c>true</c> if and only if this message
        /// has a strictly higher priority than the given
        /// <paramref name="other"/> message.</returns>
        public abstract bool GreaterThan(Message other);

        /**
         * Saving and loading functionality.
         */

        /// <summary>
        /// Stores this instance's member variables in a serializable object.
        /// <para>
        /// Uses reflection to find the members' names, types and values and
        /// stores the non-null values by their string representation.
        /// </para>
        /// </summary>
        /// <returns>A <see cref="MessageSaveData"/> instance storing the
        /// names, types and values of this message as strings.</returns>
        public MessageSaveData GenerateSaveData()
        {
            MessageSaveData data = new MessageSaveData();

            data.messageType = this.GetType().FullName;
            data.names = new List<string>();
            data.types = new List<string>();
            data.values = new List<string>();

            foreach (FieldInfo field in this.GetType().GetFields())
            {
                data.names.Add(field.Name);
                data.types.Add(field.FieldType.FullName);
                object val = field.GetValue(this);
                data.values.Add(val is null ? null : val.ToString());
            }

            return data;
        }

        /// <summary>
        /// Reconstructs a <see cref="Message"/> subclass instance from the given
        /// save data. Fields not stored in the save data will be left at the values set in
        /// the default constructor and fields stored in the save data that are not defined
        /// by the subclass will produce error messages.
        /// </summary>
        /// <param name="data">The serializable data storing a message's type and members.</param>
        /// <returns>An instance of a <see cref="Message"/> subclass with the member
        /// values stored in <paramref name="data"/>. Will be <c>null</c> if the message subtype
        /// cannot be determined or instantiated with a default constructor.</returns>
        public static Message CreateFromSaveData(MessageSaveData data)
        {
            // Null input or empty message type means null
            if (data is null || data.messageType == "")
                return null;

            // Try getting the Message type
            Type msgType = Type.GetType(data.messageType);
            if (msgType is null)
            {
                Debug.LogError("Error: Unknown Message type '" + data.messageType + "'");
                return null;
            }
            if (!msgType.IsSubclassOf(typeof(Message)))
            {
                Debug.LogError("Error: Type '" + data.messageType + "' is not a subclass of " + typeof(Message));
                return null;
            }

            // Try getting the default constructor
            ConstructorInfo ctor = msgType.GetConstructor(new Type[] { });
            if (ctor is null)
            {
                Debug.LogError("Error: Message type '" + data.messageType + "' does not have a default constructor");
                return null;
            }

            Message msg = ctor.Invoke(new object[] { }) as Message;
            for (int i = 0; i < data.names.Count; i++)
            {
                string name = data.names[i];
                string typeStr = data.types[i];
                string valStr = data.values[i];

                Type fieldType = Type.GetType(typeStr);
                if (fieldType is null)
                {
                    Debug.LogError("Error: Message field '" + name + "' has unknown type '" + typeStr + "'");
                    continue;
                }

                FieldInfo field = msg.GetType().GetField(name);
                if (field is null)
                {
                    Debug.LogError("Error: Message field '" + name + "' of type " + fieldType + " does not exist in " + msgType);
                    continue;
                }

                try
                {
                    if (fieldType.IsEnum)
                    {
                        MethodInfo parseMethod = typeof(Enum).GetMethod("Parse", new Type[] { typeof(string) });
                        MethodInfo parseMethodGen = parseMethod.MakeGenericMethod(fieldType);
                        field.SetValue(msg, parseMethodGen.Invoke(null, new object[] { valStr }));
                    }
                    else
                    {
                        field.SetValue(msg, Convert.ChangeType(valStr, fieldType));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error: Could not parse value '" + valStr + "' of field '" + name + "' in Message subtype " + msgType + "; Exception: " + e);
                }
            }
            return msg;
        }
    }

} // namespace AS2.Sim
