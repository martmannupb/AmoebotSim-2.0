# Model Reference: Messages

Theoretically, information that can be encoded in $k$ bits can be sent in $k$ rounds by reserving one round for transmitting each bit as a beep (technically, $2k$ rounds are needed because beeps can only be sent in the beep rounds).
It is possible to implement such a method by using a counter attribute and some other attributes to store the received information and the data that still has to be sent.
But because this is tedious to implement and slows down the simulation, and since sending more complex data is a rather common requirement, the simulator provides an abstraction that allows the transmission of constant-size data packages, called *Messages*, in a single round.

TODO

- Circuits can be used to send beeps and messages
- Beeps are the simplest form of communication, each beep is only one bit of information
- Messages can be used to send more information in a single round
	- They are an abstraction: According to the model, sending a message would take multiple rounds
	- Using messages avoids the overhead of coordinating multi-round communication, including the synchronization with particles that do not take part in the exchange
- Messages and beeps are handled independently
	- A circuit can transmit a beep and a message simultaneously
- What happens if there are multiple senders?
	- If multiple particles send a beep on the same circuit in the same round, every particle in the circuit simply receives a beep (no conflict)
	- What happens if multiple particles send multiple *different* messages?
	- There must be a total ordering of all possible messages
		- Only the message with the highest order (the "largest" message) is transmitted
		- The developer must define this total ordering!
- Implementation
	- New message types are defined as subclasses of the `Message` class
	- The subclass must meet several requirements:
		- It must have a parameterless default constructor
		- It must implement a `Copy()` method that returns a deep copy of the message
		- It must be serializable for the save/load feature to work
			- Easy to accomplish by using only simple data types and no data structures
		- It must implement the `Equals(Message other)` method that compares messages by their content
		- It must implement the `GreaterThan(Message other)` method that implements the total ordering
			- Both the `Equals` and `GreaterThan` methods must work for all message types that are used by the algorithm
	- It is generally a good idea to treat messages like structs that store a number of primitive values which belong together
		- Messages should never contain reference types
- Example: Message that stores an integer. The total ordering is given by the value of the stored integer
	```csharp
	public class IntMessage : Message
	{
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

	    public override Message Copy()
	    {
	        return new IntMessage(val);
	    }

	    public override bool Equals(Message other)
	    {
	        IntMessage otherMsg = other as IntMessage;
	        return otherMsg != null && val == otherMsg.val;
	    }

	    public override bool GreaterThan(Message other)
	    {
	        IntMessage otherMsg = other as IntMessage;
	        return otherMsg == null && val > otherMsg.val;
	    }
	}
	```
