# Model Reference: Messages

In the [Reconfigurable Circuits](~/amoebot_model/circuits.md) extension of the Amoebot model, circuits can be used to send beeps.
A beep is a very simple form of communication because it only contains one bit of information.
Here, we discuss how more complex information can be sent via circuits.



## Message Basics

Theoretically, information that can be encoded in $m$ bits can be sent in $m$ rounds by reserving one round for transmitting each bit as a beep (technically, $2m$ rounds are needed because beeps can only be sent in the beep rounds, not the movement rounds).
It is possible to implement such a method by using a counter attribute and some other attributes to store the received information and the data that still has to be sent.
But because this is tedious to implement and slows down the simulation, and since sending more complex data is a rather common requirement, the simulator provides an abstraction that allows the transmission of constant-size data packages, called *Messages*, in a single round.
Messages do not only simplify the transmission of complex data, they also hide the coordination overhead that would be needed to organize communication over multiple rounds and to stay synchronized with particles that do not take part in the exchange.

Since it would be easy to extend any Message to contain one more bit of information, we treat Messages and beeps independently.
Thus, a circuit can transmit a Message and a beep simultaneously.

### Handling Conflicts

With Messages, a new problem arises that does not occur for beeps.
We need to define what happens when multiple different Messages are sent on the same circuit.
For single beeps, this is well-defined: If any particle on a circuit sends a beep, all particles on that circuit will receive a beep.
If Messages are interpreted as bit strings, one could define the resulting Message to be the logical OR of all competing Messages.
However, the result might not be a valid Message, or it could be an entirely different Message that was not sent by any particle individually.

Because the number of possible Messages is finite, we can use a different method:
By defining a total ordering of all possible Messages, the implicit bit encoding of the Messages can be changed such that it is always the "greatest" one of the sent Messages that comes through.
This way, it is guaranteed that the received Message is valid and well-defined.
The total ordering, however, must be defined by the algorithm developer.

![Message Ordering](~/images/message_ordering.png "Message Ordering")

The figure above illustrates how the total ordering works.
For the 3 possible Message types, $A$, $B$ and $C$, we define the total ordering as $A > B > C$.
If any particle sends the Message $A$, all particles on the circuit will receive Message $A$.
Otherwise, they will receive $B$ if any particle has sent $B$, or $C$ if nobody sent $B$ but somebody sent $C$.
Naturally, no Messages are received when none are sent.
This happens independently and simultaneously with the beep transmission.



## Implementation

To use the Messaging system, you first need to create a new Message type as a subclass of the [`Message`][1] class.
By putting this class into the namespace of the algorithm you are developing, you can choose the name of the new Message type freely (see [Algorithm Creation](~/user_guide/dev/creation.md)).
The [`Message`][1] subclass must meet several requirements:
- It must have a parameterless default constructor (`public MyMessage() {...}`).
- It must override the [`Copy`][2] method to return a deep copy of the Message.
- Its instances must be serializable for the save and load functionality to work properly.
	A class that contains only simple data types (`int`, `float`, `enum` etc.) and no reference types is always serializable.
- It must override the [`Equals(Message other)`][3] and [`GreaterThan(Message other)`][4] methods to define the total ordering.
	The [`Equals`][3] method compares Messages by *value* (i.e., two instances with the same content are equal).
	Both methods must work for all [`Message`][1] types used by the algorithm.

In general, it is a good idea to treat Messages like structs that simply store a collection of primitive values.
Messages should never contain reference types because this could lead to unintended behavior that is very difficult to debug.

The following is the code for a simple Message storing a single integer value.
The total ordering is defined so that Messages are simply compared by their integer values, with the greater value determining the greater Message.

```csharp
public class IntMessage : Message
{
    // The data contained in the Message
    public int val;

    public IntMessage(int v)
    {
        val = v;
    }

    // Required parameterless default constructor
    public IntMessage()
    {
        val = 0;
    }

    // Returns a deep copy of this Message
    public override Message Copy()
    {
        return new IntMessage(val);
    }

    // Compares this Message to other by value
    public override bool Equals(Message other)
    {
        IntMessage otherMsg = other as IntMessage;
        return otherMsg != null && val == otherMsg.val;
    }

    // Total ordering is defined by comparing the int value
    public override bool GreaterThan(Message other)
    {
        IntMessage otherMsg = other as IntMessage;
        return otherMsg != null && val > otherMsg.val;
    }
}
```

> [!NOTE]
> If you have multiple types of messages, the total ordering must include all of them.
> Thus, the [`GreaterThan`][4] method must account for the other message types as well.


[1]: xref:AS2.Sim.Message
[2]: xref:AS2.Sim.Message.Copy
[3]: xref:AS2.Sim.Message.Equals(AS2.Sim.Message)
[4]: xref:AS2.Sim.Message.GreaterThan(AS2.Sim.Message)
