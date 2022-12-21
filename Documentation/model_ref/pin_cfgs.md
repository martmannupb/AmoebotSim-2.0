# Model Reference: Pin Configurations

The [Reconfigurable Circuits](~/amoebot_model/circuits.md) extension of the Amoebot model introduces a new communication system that allows particles to communicate using configurable structures called *circuits*.
This page describes how this communication system is represented in the simulation environment and how it is used.


## Pin Configuration Overview

The basic component of the circuit communication system is the *pin*.
Each algorithm specifies the number of pins $n$ used by the particles.
This means that every particle will have $n$ pins on each of its incident edges.
Thus, contracted particles have $6n$ pins and expanded particles have $10n$ pins overall.

The pins of two particles incident to the same edge are connected in pairs.
Each pair contains one pin of each particle.
This allows the particles to use each pair of connected pins as a communication channel.

On each edge incident to a particle, the pins are numbered $0,\ldots,n-1$ in the *local* counter-clockwise direction of the particle.
This numbering is internal: Other particles may not have the same numbering and cannot see the numbering of a neighbor directly.
Due to the way the pins of neighboring particles are connected, we can see how their pin numberings relate to each other:
- If the particles have the *same chirality*, pin $0$ of one particle will be connected to pin $n-1$ of the neighbor, pin $1$ to $n-2$, etc.
	In other words, the pin numberings of the neighbors are the exact opposite of each other.
- If the particles have a *different chirality*, the pins will share the same numbering.

### Partition Sets

The connections between the pins of different particles are established automatically as soon as the particles become neighbors.
However, a particle can also connect its own pins internally, causing them to behave like a single pin with multiple external connection points.
If any of the pins sends or receives a beep, all of them will do the same.
Any number of pins can be connected in this way.
We call a maximal set of connected pins a *partition set*.
Every pin of a particle belongs to exactly one partition set at any time.
If a pin is not connected (internally) to any other pins, we call its partition set a *singleton*.

The maximum number of partition sets is equal to the total number of pins.
Thus, we number the partition sets of a contracted particle $0,\ldots,6n-1$ and the partition sets of an expanded particle $0,\ldots,10n-1$.
The index of a partition set does not have any implications regarding its content.
We only use the numbering to easily refer to individual partition sets.

The *pin configuration* of a particle is the set of its partition sets.
It defines how the particle's pins are connected internally.
In a *singleton pin configuration*, all partition sets are singletons, i.e., there are no internal connections between any pins.
In a *global pin configuration*, all pins are contained in the same partition set.

If two partition sets of neighboring particles contain a pair of connected pins, we say that the partition sets are connected.
A maximal set of connected partition sets is called a *circuit*.
We say that a particle is *on the circuit* if one of its partition sets is contained in the circuit.
If any particle on the circuit sends a beep on a partition set in the circuit, all partition sets in the circuit will receive a beep.
Note that a circuit can contain multiple partition sets of the same particle, causing them to behave as if they were merged.
If a circuit contains at least one partition set of each particle, i.e., all particles are on the circuit, we call it a *global circuit*.






- Implementation and API
	- A particle's pin configuration is represented by an instance of the `PinConfiguration` class
	- In a `PinConfiguration` instance, all pins and partition sets have integer IDs
		- It is possible for partition sets to be empty (e.g., if partition set `0` contains all pins, all other partition sets are empty)
	- A `PinConfiguration` is always specific to an *expansion state*
		- Pin and partition set IDs are in the range $0,\ldots,6n-1$ if the configuration is contracted and $0,\ldots,10n-1$ if it is expanded
		- A `PinConfiguration` for the contracted state is not compatible with a `PinConfiguration` for an expanded state
		- Two expanded `PinConfiguration`s with different expansion directions are also incompatible
		- *Incompatible* means that the pin and partition set IDs may not refer to the same pins and partition sets in both configurations
	- The `GetCurrentPinConfiguration()` method returns the pin configuration from the beginning of the current round/phase
		- If beeps and messages were received by the particle in the previous beep phase, they can be read using the returned `PinConfiguration` instance
			- Call `ReceivedBeepOnPartitionSet(int id)`, `ReceivedMessageOnPartitionSet(int id)` and `GetReceivedMessageOfPartitionSet(int id)`
		- In the movement phase, this will always work
		- If the particle performs a movement in the movement phase, its pin configuration will be reset to a singleton and the beeps and messages received in the previous beep phase will be lost
		- Note that it is possible for the system to perform a joint movement in which not all particles perform individual movements. Even if this movement breaks the circuits from the previous round, the particles that have not moved individually keep their local pin configurations and still "remember" what they had received, for convenience
	- During the beep phase, a particle can change its pin configuration
		- To do so, it needs to call `SetPlannedPinConfiguration(PinConfiguration pc)`, which will update the pin configuration to `pc` at the end of the phase
		- The `PinConfiguration` instance `pc` must match the particle's expansion state
		- It can either be the current pin configuration or a fresh one created using `Get<Contracted|Expanded>PinConfiguration`
		- `PinConfiguration` instances can be modified in several ways
			- `SetToGlobal(int id)` will collect all pins in the partition set with ID `id`
			- `SetStarConfig(int offset, int id)` will collect all pins with offset `offset` in the partition set with ID `id`
				- This partition set will contain exactly one pin from each edge
				- The method has several overloads to select the pins more flexibly
			- `MakePartitionSet(int[] pinIds, int id)` simply puts the pins specified by `pinIds` into the partition set with ID `id`
	- Sending beeps and messages
		- After a `PinConfiguration` instance `pc` has been committed using `SetPlannedPinConfiguration`, it can be used to send beeps and messages
		- Simply call `pc.SendBeepOnPartitionSet(int id)` to send a beep on the partition set with ID `id`
		- Call `pc.SendMessageOnPartitionSet(int id, Message msg)` to send a message `msg`
			- Read the [Message reference page](messages.md) to learn how custom message types can be defined
- Additional features
	- Particles can set the color of a partition set to influence the display color of the circuit containing the partition set
		- Use `pc.SetPartitionSetColor(int id, Color color)` to set the color of the planned pin configuration `pc`
		- A circuit will take the color of the first colored partition set that is encountered while computing the circuits
		- If no colored partition sets are encountered, the circuit will get a color from the circuit color pool
	- Pin configurations can be stored in particle attributes
		- `CreateAttributePinConfiguration` will create a particle attribute that can store `PinConfiguration` instances
		- These attributes will not be displayed in the UI!
		- This feature should be used to save complex pin configurations to reuse them later so that they do not have to be computed again
		- Stored pin configurations cannot be used to read beeps or messages!
		- It is not possible to read pin configurations of neighboring particles
