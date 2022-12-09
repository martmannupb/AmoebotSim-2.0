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
Theoretically, information that can be encoded in $k$ bits can be sent in $k$ rounds by reserving one round for transmitting each bit (technically, $2k$ rounds are needed because beeps can only be sent in the beep rounds).
It is possible to implement such a method by using a counter attribute and some other attributes to store the received information and the data that still has to be sent.
But because this is tedious to implement and slows down the simulation, and since sending more complex data is a rather common requirement, the simulator provides an abstraction that allows the transmission of constant-size data packages, called *Messages*, in a single round.

TODO


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
