# Developer Guide: History

One of the main features of the simulation environment is the ability to rewind a simulation and review every round at any time.
To accomplish this, the simulator must be able to restore any previous system state completely, so that it can be displayed correctly and the simulation can be restarted from that state if desired.


## Decentralized Approach

An obvious straight-forward way to implement the history feature would be to create a copy of the whole simulation state for each round and store all copies in a central data structure.
In the simulation environment, the history feature is implemented in a decentralized way, meaning that there is no such central place in which the entire history is stored.
Instead, every part of the simulator maintains its own state history and implements an interface that allows it to restore any previous state.
The reason for this is that from one round to the next, many parts of the simulator state typically remain unchanged, which allows us to improve memory efficiency by avoiding unnecessary copies of identical values.

### The [`IReplayHistory`][1] Interface

Every class of the simulator that needs to maintain a state history implements the [`IReplayHistory`][1] interface.
This interface defines a state history as a sequence of states for a range of consecutive rounds.
The history starts in some round with index $i$ and stores a state for each round $i,i+1,\ldots,i+l-1$, where $l$ is the current length of the state sequence.

An object implementing the interface always has a *marker* that indicates the currently selected round.
The marker can be moved to any round $\geq i$, which will change the object's state to the one recorded for that round.
If the object's state changes while the marker is in any round $\geq i + l-1$, a new entry will be recorded and the history will be extended to the current location of the marker, copying the previous state to fill the gap if there is one.
If the object is in a *tracking* state, adding a new entry for some round $\geq i + l-1$ will automatically move the marker forward to that round.

The history can also be cut off at the marker, meaning that all recorded states after the marker are removed.

### Nesting Histories

The history interface can also be implemented by more complex classes that have several sub-histories.
To accomplish this, every call to a method from the [`IReplayHistory`][1] interface is passed to all sub-histories so that their states are synchronized.
The [`ParticleSystem`][2] class is a good example for this:
When the selected round changes in Simulation Mode (e.g. by calling [`SetMarkerToRound(int round)`][3]), the system will call the same method on all particles and all other histories that make up the system's state.
In turn, the particles forward the calls to their own histories as part of updating their states.


## Storing value histories

To store a sequence of values for consecutive rounds efficiently, we only need to record the rounds in which the value *changes*.
Consider the following example of a range of round indices and corresponding integer values:
```csharp
rounds: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
values: [3, 3, 4, 1, 1, 1, 7, 7, 3, 5]
```
We can store this sequence in a more compact format as follows:
```csharp
changeRounds: [0, 2, 3, 6, 8, 9]
values:       [3, 4, 1, 7, 3, 5]
```
This works under the conditions that the range of rounds is consecutive and the values are comparable.
The value for a given round can be obtained by performing a binary search on the `changeRounds` to find the largest recorded round that is less than or equal to the requested round and returning the value with the same list index as that round.
For example, when the round `5` is queried, the binary search will find the recorded round `3` at list index `2`, leading to the correct value `1`.

Note, however, that this system still allows a value to be stored multiple times, like the value `3` in the example.
For very large data types, we can avoid this by keeping a record of all encountered values and only storing their indices, at the cost of additional runtime for inserting new records:
```csharp
rounds: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
values: [A, A, B, B, B, C, B, B, A, C]
=> compact format:
changeRounds: [0, 2, 5, 6, 8, 9]
indices:      [0, 1, 2, 1, 0, 2]
encountered:  [A, B, C]
```

The generic [`ValueHistory<T>`][4] class implements this system for data types that provide an equality check.
For an example of the system for more complex objects, see the [`ValueHistoryPinConfiguration`][5] class, which stores compressed versions of pin configurations.



[1]: xref:AS2.Sim.IReplayHistory
[2]: xref:AS2.Sim.ParticleSystem
[3]: xref:AS2.Sim.ParticleSystem.SetMarkerToRound(System.Int32)
[4]: xref:AS2.Sim.ValueHistory`1
[5]: xref:AS2.Sim.ValueHistoryPinConfiguration
