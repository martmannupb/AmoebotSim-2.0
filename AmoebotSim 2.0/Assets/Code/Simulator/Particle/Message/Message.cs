using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
}
