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

### Circuits

If two partition sets of neighboring particles contain a pair of connected pins, we say that the partition sets are connected.
A maximal set of connected partition sets is called a *circuit*.
We say that a particle is *on the circuit* if one of its partition sets is contained in the circuit.
If any particle on the circuit sends a beep on a partition set in the circuit, all partition sets in the circuit will receive a beep.
Note that a circuit can contain multiple partition sets of the same particle, causing them to behave as if they were merged.
If a circuit contains at least one partition set of each particle, i.e., all particles are on the circuit, we call it a *global circuit*.
An easy way to establish a global circuit is to give each particle a global pin configuration.


## Implementation

In the simulation environment, a particle's pin configuration is represented by an instance of the [`PinConfiguration`][1] class.
This class contains all pins and partition sets of a particle as instances of the [`Pin`][2] and [`PartitionSet`][3] classes, but it simply identifies them with integer IDs.
A [`PinConfiguration`][1] instance is always specific to an *expansion state*, which also defines the range of the IDs:
It is $0,\ldots,6n-1$ if the configuration is contracted and $0,\ldots,10n-1$ if it is expanded.
A contracted [`PinConfiguration`][1] instance is incompatible with an expanded [`PinConfiguration`][1] in the sense that the same IDs may not refer to the same pins.
Similarly, expanded instances with different expansion directions are also incompatible.
It is possible for partition sets to be empty, e.g., if the partition set with ID `0` contains all pins, then all other partition sets will be empty.

Individual pins are addressed by a combination of the edge on which a pin is located and the pin's index on that edge.
The index is in the range $0,\ldots,n-1$ and comes from the local pin numbering of the particle.

### Reading Pin Configurations

The [`GetCurrentPinConfiguration`][4] method returns the pin configuration from the beginning of the current phase.
If the particle has received any beeps or [Messages](messages.md) in the previous beep phase, they can be read using the returned [`PinConfiguration`][1] instance.
As outlined in the [Round Simulation reference](rounds.md), this will always work in the movement phase.
In the beep phase, the received beeps and messages can only be read if the particle has not performed a movement in the previous movement phase.
If a movement was performed, the particle's pin configuration is automatically reset to a singleton configuration and the received beeps and messages are lost.
Note that the circuit on which a beep or message was sent may not exist anymore after the movement phase, even if the particle itself did not perform any movements.
The ability to still read the received information is a convenience functionality to avoid having to store this information manually.

### Changing Pin Configurations and Sending Beeps

A particle can change its pin configuration in the beep phase.
To do so, it needs to call [`SetPlannedPinConfiguration(PinConfiguration pc)`][5], which will update the pin configuration to `pc` at the end of the phase, before using it to send beeps and messages.
The [`PinConfiguration`][1] instance `pc` must match the particle's expansion state.
This instance can either be the current pin configuration or a new one created using [`Get<Contracted|Expanded>PinConfiguration`][6].
Any [`PinConfiguration`][1] instance can be modified in several ways:
- [`SetToGlobal(int id)`][7] collects all pins in the partition set with ID `id`
- [`SetStarConfig(int offset, int id)`][8] collects all pins with edge index `offset` in the partition set with ID `id`.
	This partition set will contain exactly one pin from each edge.
	The method has several overloads for selecting the pins more flexibly.
- [`MakePartitionSet(int[] pinIds, int id)`][9] puts the pins specified by the `pinIds` array into the partition set with ID `id`.
	This method can be used to create any desired partition set.

Modifications made after [`SetPlannedPinConfiguration`][5] has been called have no effect.

After a [`PinConfiguration`][1] instance `pc` has been committed using [`SetPlannedPinConfiguration(pc)`][5], it can be used to send beeps and messages.
Simply call [`pc.SendBeepOnPartitionSet(int id)`][10] to send a beep on the partition set with ID `id` and [`pc.SendMessageOnPartitionSet(int id, Message msg)`][11] to send the message `msg` (Refer to the [Message reference page](messages.md) for more information).


## Additional Features

Particles can set the color of a partition set to influence the display color of the circuit containing that partition set.
If a [`PinConfiguration`][1] instance `pc` has been set as the planned pin configuration, [`pc.SetPartitionSetColor(int id, Color color)`][12] can be called to set the color of the partition set with ID `id`.
A circuit will take the color of the first colored partition set that is encountered while computing the circuits.
If no colored partition sets are encountered, the circuit will get a color from the circuit color pool (found in [`ColorData`][13]).

**TODO**: Mention partition set placement API

Pin configurations can also be stored in [particle attributes](attrs.md).
The special type of attribute for this purpose is created by the [`CreateAttributePinConfiguration`][14] method, which accepts `null` as an initial value.
Attributes of this type are not displayed in the UI and cannot be accessed by other particles.
Their only purpose is to save complex pin configurations so that they can be reused later without having to construct them again.
Note that stored pin configurations cannot be used to read beeps or messages.



[1]: xref:AS2.Sim.PinConfiguration
[2]: xref:AS2.Sim.Pin
[3]: xref:AS2.Sim.PartitionSet
[4]: xref:AS2.Sim.ParticleAlgorithm.GetCurrentPinConfiguration
[5]: xref:AS2.Sim.ParticleAlgorithm.SetPlannedPinConfiguration(AS2.Sim.PinConfiguration)
[6]: xref:AS2.Sim.ParticleAlgorithm.GetContractedPinConfiguration
[7]: xref:AS2.Sim.PinConfiguration.SetToGlobal(System.Int32)
[8]: xref:AS2.Sim.PinConfiguration.SetStarConfig(System.Int32,System.Int32)
[9]: xref:AS2.Sim.PinConfiguration.MakePartitionSet(System.Int32[],System.Int32)
[10]: xref:AS2.Sim.PinConfiguration.SendBeepOnPartitionSet(System.Int32)
[11]: xref:AS2.Sim.PinConfiguration.SendMessageOnPartitionSet(System.Int32,AS2.Sim.Message)
[12]: xref:AS2.Sim.PinConfiguration.SetPartitionSetColor(System.Int32,Color)
[13]: xref:AS2.ColorData
[14]: xref:AS2.Sim.ParticleAlgorithm.CreateAttributePinConfiguration(System.String,AS2.Sim.PinConfiguration)
