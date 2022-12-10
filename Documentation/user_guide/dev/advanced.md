# Algorithm Development: Advanced Features

This page explains features that were not demonstrated in the [Implementation Walkthrough](demo.md).
We will use the demo algorithm implemented in the walkthrough as a basis and extend it with new functionality.
The main topics that will be discussed here are:
- Printing log messages
- Using the message system
- Releasing bonds to allow specific movements


## Printing Log Messages

When developing more complex algorithms, it can be very helpful to print log messages indicating that a certain condition has been met or some event has occurred, or to view data that is not displayed in the UI.
In the simulation environment, there are two ways of printing log messages: The simulator's [`Log`][1] and the Unity Editor log system.

![Simulator Log](~/images/adv_sim_log.png "The simulator log")

The simulator [`Log`][1] provides four different log levels: *Debug*, *Entry*, *Warning* and *Error*.
They are printed using the [`Log.Debug`][2], [`Log.Entry`][3], [`Log.Warning`][4] and [`Log.Error`][5] methods.
Simulator log messages are displayed directly in the UI, at the bottom of the main viewing area.
The entire log can also be exported to a text file so that it can be viewed or compared to other logs later.
All simulator log messages are additionally sent to Unity's log system so they can be seen in the Editor as well.

![Unity Log](~/images/adv_unity_log.png "The Unity Editor log")

Unity's log provides *Log*, *Warning* and *Error* messages, which are printed with the `Debug.Log`, `Debug.LogWarning` and `Debug.LogError` methods.
They are displayed in the Editor's Console window (usually displayed in the same location as the Project window).
This log system has a search bar, buttons for filtering log entries by type, and a "collapse" function that groups consecutive entries with the same content into single entries.

We can add log messages to our demo algorithm to check if everything works as intended:
```csharp
public override void ActivateMove()
{
    PinConfiguration pc = GetCurrentPinConfiguration();
    if (pc.ReceivedBeepOnPartitionSet(0))
    {
        // Received a beep => Perform movement
        Debug.Log("Received beep");
        if (IsContracted())  // Expand East if contracted
            Expand(Direction.E);
        else                 // Contract into tail if expanded
            ContractTail();
    }
}

public override void ActivateBeep()
{
    PinConfiguration pc = GetCurrentPinConfiguration(); // Get a PinConfiguration instance
    pc.SetToGlobal(0);                                  // Collect all pins in partition set 0
    SetPlannedPinConfiguration(pc);                     // Commit to use this pin configuration

    if (isLeader)  // Only the leader should run this code
    {
        if (Random.Range(0.0f, 1.0f) < 0.5f)
        {
            // Decided to move => Send a beep on the global circuit
            Log.Debug("Leader decided to move");
            pc.SendBeepOnPartitionSet(0);
        }
    }
}
```
Now, whenever the leader decides to perform a movement, the message "Leader decided to move" will be logged and displayed in the UI, and "Received beep" will be printed to the Console by every particle receiving a beep on the global circuit.
Note that the "Received beep" message will be logged once for each particle, so it makes sense to use Unity's log here so that these entries can be collapsed.
Adding log messages is especially useful for implementing an algorithm incrementally (print a log message instead of performing the next step) or finding the cause of unintended behavior (trace down the section of code that is executed).


## Using the Message System

The communication in our demo algorithm is very simple because the information that has to be transferred is binary: Either a movement has to be performed or not.
However, more complex algorithms may require more complex data to be sent via circuits.
As explained on the [Message reference page](~/model_ref/messages.md), it is possible to transmit data bit by bit over multiple rounds, but the simulator provides an abstraction from this method in the form of *Messages*.
A Message is a constant-size data package that can be sent in a single round like a beep.
Custom Message types can be implemented as subclasses of the [`Message`][6] class, but they have to meet certain requirements to function properly.
Please refer to the [reference page](~/model_ref/messages.md) for more details.

We will demonstrate how custom Messages are used by adding new movement directions to our demo algorithm:
The particles should be able to expand to the North-North East and South-South East directions in addition to the East direction.
The leader will pick one of the available directions at random if it decides that a movement should be performed.
To accomplish this, we must solve two problems:
- Telling all particles which direction was chosen
- Ensuring that the movements are performed correctly

To solve the first problem, we will send the chosen movement direction using a custom Message type.
The second problem will be solved in the next section.

### Defining a Custom Message Type

Custom Messages are defined as subclasses of the [`Message`][6] class.
We start by defining our new class in the demo algorithm file, above the algorithm class, and giving it a member to store the direction as well as a default constructor without parameters:
```csharp
public class DemoDirectionMsg : Message
{
    public Direction dir;

    public DemoDirectionMsg()
    {
        dir = Direction.E;
    }
}
```
The parameterless default constructor is required for the class to work correctly and all members have to be *serializable*, ideally simple data types.
Since the [`Direction`][7] type is an `enum`, members of this type are allowed.

We can add another constructor for convenience:
```csharp
public DemoDirectionMsg(Direction d)
{
    dir = d;
}
```

To complete the Message type, we need to override three methods of the base class: [`Copy`][8], [`Equals`][9] and [`GreaterThan`][10].
The [`Copy`][8] method is straightforward: It must create a deep copy of the Message and return it.
```csharp
public override Message Copy()
{
    return new DemoDirectionMsg(dir);
}
``` 

The [`Equals`][9] method compares the Message to another instance by value.
If the other Message does not have the same type or is `null`, it must return `false`.
```csharp
public override bool Equals(Message other)
{
    DemoDirectionMsg m = other as DemoDirectionMsg;
    return m != null && m.dir == dir;
}
```
We use the `as` operator to typecast the parameter `other` into a `DemoDirectionMsg`, if possible, and `null` otherwise.
This way, the method will return `false` if `other` has the wrong type or is `null`.

The [`GreaterThan`][10] method defines an ordering of all Messages that is used to determine which Message is prioritized when multiple different Messages are sent on the same circuit in the same round.
In such a case, the "greatest" message is the only one that will be delivered.
To define this total ordering in our case, we define our Message type to be greater than any other type and use the integer representation of [`Direction`][7] values to compare messages of the same type:
```csharp
public override bool GreaterThan(Message other)
{
    DemoDirectionMsg m = other as DemoDirectionMsg;
    return m == null || dir.ToInt() > m.dir.ToInt();
}
```

The custom Message type is now finished, here is the final code:
```csharp
public class DemoDirectionMsg : Message
{
    public Direction dir;

    public DemoDirectionMsg()
    {
        dir = Direction.E;
    }

    public DemoDirectionMsg(Direction d)
    {
        dir = d;
    }

    public override Message Copy()
    {
        return new DemoDirectionMsg(dir);
    }

    public override bool Equals(Message other)
    {
        DemoDirectionMsg m = other as DemoDirectionMsg;
        return m != null && m.dir == dir;
    }

    public override bool GreaterThan(Message other)
    {
        DemoDirectionMsg m = other as DemoDirectionMsg;
        return m == null || dir.ToInt() > m.dir.ToInt();
    }
}
```

### Sending and Receiving Messages

Next, we want the leader to send a Message with the chosen movement direction on the global circuit.
We first have to make the leader choose a direction.
For this, we can define a constant array of possible directions from which the leader can choose:
```csharp
public class DemoParticle : ParticleAlgorithm
{
    static readonly Direction[] movementDirs = new Direction[] { Direction.E, Direction.NNE, Direction.SSE };
    ...
}
```
This is *not* a state attribute of the particles because it is not a [`ParticleAttribute`][11].
Constants and hyper-parameters like this should always be `static` and `readonly`.

When the leader decides that a movement should be performed, it can simply determine a random index of the array.
It then has to create a Message object and send it on the global circuit:
```csharp
public override void ActivateBeep()
{
    PinConfiguration pc = GetCurrentPinConfiguration(); // Get a PinConfiguration instance
    pc.SetToGlobal(0);                                  // Collect all pins in partition set 0
    SetPlannedPinConfiguration(pc);                     // Commit to use this pin configuration

    if (isLeader)  // Only the leader should run this code
    {
        if (Random.Range(0.0f, 1.0f) < 0.5f)
        {
            // Decided to move => Determine the direction
            int dirIdx = Random.Range(0, movementDirs.Length);
            Direction moveDir = movementDirs[dirIdx];
            Log.Debug("Leader decided to move in direction " + moveDir);

            // Send the direction using a Message
            DemoDirectionMsg msg = new DemoDirectionMsg(movementDirs[dirIdx]);
            pc.SendMessageOnPartitionSet(0, msg);
        }
    }
}
```
Note that the leader will choose a direction even if the particles are expanded, in which case they should still contract into their tail and the chosen direction will be ignored.


We can check whether we have received a Message on a partition set using the [`ReceivedMessageOnPartitionSet`][12] method.
If a Message was received, it can be accessed using [`GetReceivedMessageOfPartitionSet`][13].
Because some more changes are necessary to make the algorithm work, we will still only move in the East direction for now.
```csharp
public override void ActivateMove()
{
    PinConfiguration pc = GetCurrentPinConfiguration();
    if (pc.ReceivedMessageOnPartitionSet(0))  // Check for received Message
    {
        // Received a Message => Read direction
        Message msg = pc.GetReceivedMessageOfPartitionSet(0);
        Direction moveDir = ((DemoDirectionMsg)msg).dir;  // Typecast to our Message type
        Debug.Log("Received Message with direction " + moveDir);

        // Now perform the movement (TODO)
        if (IsContracted())  // Expand East if contracted
            Expand(Direction.E);
        else                 // Contract into tail if expanded
            ContractTail();
    }
}
```
Because [`GetReceivedMessageOfPartitionSet`][13] returns an object of type [`Message`][6], we need to cast it to our own Message type `DemoDirectionMsg` to access the stored direction.



## Releasing Bonds




TODO

- More movement directions
	- Decide for random direction
	- Create custom Message to send direction
	- Setup bonds to allow other movement directions

[1]: xref:Global.Log
[2]: xref:Global.Log.Debug(System.String)
[3]: xref:Global.Log.Entry(System.String)
[4]: xref:Global.Log.Warning(System.String)
[5]: xref:Global.Log.Error(System.String)
[6]: xref:Global.Message
[7]: xref:Global.Direction
[8]: xref:Global.Message.Copy
[9]: xref:Global.Message.Equals(Message)
[10]: xref:Global.Message.GreaterThan(Message)
[11]: xref:Global.ParticleAttribute`1
[12]: xref:Global.PinConfiguration.ReceivedMessageOnPartitionSet(System.Int32)
[13]: xref:Global.PinConfiguration.GetReceivedMessageOfPartitionSet(System.Int32)
