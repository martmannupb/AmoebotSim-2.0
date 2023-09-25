# Developer Guide: Round Simulation

This page explains the mechanics behind the round simulation.
One simulated round consists of two phases: The movement phase and the beep phase.
In the theoretical model, these phases are mostly treated as separate rounds, each consisting of a *look* phase, a *compute* phase and an *act* phase, during which the particle either moves or sends beeps via its circuits.
Since this separation was a late addition to the model, the simulator runs the *look-compute-move* cycle (called *movement phase*) and the *look-compute-beep* cycle (called *beep phase*) sequentially within a single round.
However, to the particle algorithm, the two phases appear like separate rounds so that the system matches the theoretical model from an algorithm developer's point of view.

![Simulation phases](~/images/round_sim_phases.png "Simulation phases")

Each of the two phases has roughly the same structure:
We first activate all particles and store their actions, then we apply the effects of their scheduled actions, checking whether they are valid, and finally, we store the final validated result and perform a cleanup for the next round.
After both phases are finished, we push the collected graphical information to the render system.
If a conflict occurs somewhere in this process, we reset the simulation state to the previous round.
A conflict is a situation in which the actions performed by a particle are invalid or conflict with another particle's actions.
Not all errors can be handled in this way, some will still lead to an exception being thrown.



## Simulation of a Round

The whole round simulation procedure is handled by the [`ParticleSystem`][1] class.
Its [`SimulateRound()`][2] method is called regularly by the main [`AmoebotSimulator`][3] class and handles the entire round simulation using multiple private helper methods.
In the following, we walk through the main steps of the simulation method, explaining the most relevant concepts and details.


### Movement Activation

After checking whether a round simulation can be started in the current simulation state and incrementing the [`CurrentRound`][4] and [`LatestRound`][5] counters, we first run the movement activations by calling the `ActivateParticlesMove()` method.
This method loops through all particles and processes each one by calling its [`ActivateMove()`][6] method and computing some additional movement and bond information.

The first piece of information is the particle's *movement origin*.
The origin of a particle is the part that keeps its position during a movement, as seen from the particle's local view.
For example, when a particle expands, its head moves to a new location, so its tail is the origin.
For a contraction movement, the target is the origin, i.e., if the particle contracts into its head, then the head is the origin.
Lastly, for non-moving particles, it does not matter which part is the origin because all parts of the body keep their relative positions.
The movement origin is important for determining the behavior of the bonds when the joint movements are simulated.

Next, the final bond setup of the particle is calculated and stored in a format that uses global directions rather than the local directions in which the bonds are defined.
If the particle uses automatic bonds, its scheduled movement is used to determine which bonds need to be released (basically, all non-origin bonds must be released if the particle contracts).
Additionally, the automatic bond restrictions caused by the scheduled movement are applied afterwards (e.g., if the particle expands, the bond pointing in expansion direction must be marked).
At the end of this step, the bonds stored for this particle are consistent and complete in the sense that they match the particle's scheduled movement and do not need to be processed any further.

The last piece of information computed in this method is the particle's *movement offset*.
This is the global grid vector by which the moving part of the particle is translated relative to its origin.
If the particle does not perform a movement, the offset is the zero vector.
Just like the origin, this information is used for the simulation of the joint movements later on.

![Origin and movement offset](~/images/round_sim_origin.png "Origin and movement offset")

The method returns `true` if any particle has released a bond or scheduled a movement.
If it returns `false`, the entire joint movement simulation can be skipped because no particle in the system has moved.


### Joint Movement Simulation

The next step is to use the scheduled movements of all particles to determine their joint movements.
This is done by the `SimulateJointMovements()` method.
If no particle has scheduled a movement, we call `ComputeBondsStatic()` instead, which only computes the visualization data (that is a byproduct of `SimulateJointMovements()`) without simulating any movements.

The idea of the `SimulateJointMovements()` method is to perform a Breadth-First-Search on the particle system, starting at the anchor particle and using the active bonds as edges.
When a particle is visited, its *joint movement offset* is calculated, which is the global grid vector by which the particle's origin is translated in the joint movement.
This offset is then used to calculate the offset of the particle's bonded neighbors, such that the offset is propagated through the bond structure.
To implement this, we use a simple queue data structure and give each particle a flag ([`queuedForJMProcessing`][7]) indicating whether it is already queued.

Each particle $p$ in the queue is processed as follows:
First, we find all bonded neighbors of $p$, along with some additional information like whether the neighbor is the particle's head or tail etc.
We then process each neighbor $q$ individually and based on the number of bonds we have to that neighbor.
This is the most complex part of the whole procedure because there are so many different cases:
If both $p$ and $q$ are contracted, there can be only a single bond between them (this is the easiest case).
If exactly one of the two particles is expanded, there can be one or two bonds between them, and the bond can belong to the expanded particle's head or its tail, or more appropriately, to its origin or its moving part, if it has scheduled a movement.
If both $p$ and $q$ are expanded, there can be up to three bonds between them and the relation between their origins is also more complicated.
The main part of the `SimulateJointMovements()` method is a case distinction that handles all of these different cases, some of them using extra helper methods.

The result of this computation is an offset vector that describes the movement of $q$'s origin relative to the origin of $p$.
Adding this vector to $p$'s joint movement offset results in the joint movement offset of $q$.
However, this is only the offset according to $p$'s bonds and movement.
It is possible that another one of $q$'s bonded neighbors induces a different offset, in which case there is a conflict in the particle system's joint movement, which cannot be resolved.
Thus, when the computation of $q$'s offset is complete, we continue as follows:
If $q$ is not already in the queue, it does not yet have an offset.
In this case, we simply assign the computed offset to $q$ and enqueue the particle.
Otherwise, if it is already in the queue, we compare its current offset to the newly computed one.
If they are different, we throw an exception because joint movement conflicts cannot be resolved and the simulation has to be aborted.

The following image shows an example of the joint movement simulation for three expanding particles:
![Joint movement offset calculation](~/images/round_sim_expansion.png "Joint movement offset calculation")
The numbers in the image indicate the order in which the particles are processed by the BFS.

Finally, we determine the new global position of $p$ and add it to the temporary particle map.
If the position is already occupied, we throw an exception because there are conflicting particle movements.
This step concludes the processing of each particle during the BFS.
Along the way, we have also computed the start and end position of every active and visible bond and stored the information in the particle's list of [`ParticleBondGraphicState`][8]s.
Because we only process each bond from one particle's point of view, no bonds are added twice to the rendering system.

After the BFS, we iterate over all particles in the system to apply the movements locally and to check for connectivity.
For each particle, we update its internal state by applying its movement offset and expansion state change according to the movement it has performed.
Additionally, the pin configuration of a particle is reset if it has performed a movement because its current pin configuration does not match its expansion state anymore.
> [!NOTE]
> This also means that if a particle does not perform a movement in the movement phase, it can still read its received beeps and messages in the following beep phase.
> Although this does not exactly match the theoretical model, it is kept like this because it may be useful for algorithms that don't use any movements and it does not violate the theoretical restrictions because the beeps and messages could simply be stored manually by the particle.

If we encounter any particle that has not been processed in the BFS, then there exists no bond path from the anchor particle to that particle, so we have to throw an exception because the bond structure is disconnected, which is not allowed.
The exception handling code will revert the simulation state to the previous round, undoing the state changes that were applied so far.
If no disconnected particle is encountered, we finally replace the particle map with the updated positions.

#### Handling of Objects

Objects in the particle system are handled as special cases.
If a bond belonging to the current particle $p$ connects $p$ to an object $o$, the following happens:

First, we compute the offset that $p$'s joint movement and its local movement impose on the bond and the object $o$.
If $o$ has already received a joint movement offset, we compare this offset to the one belonging to $p$.
In the case that they are not the same, we have to report a joint movement conflict, just like for non-matching particle offsets.

If $o$ has not yet received an offset, we assign the offset computed for $p$ and move on to the offset propagation procedure, implemented by the `PropagateObjectOffset` method.
This procedure starts a separate BFS starting at the given object and traversing only other objects that are connected by bonds.
Because the objects cannot perform any local movements, all connected objects must have the same movement offset.
Thus, if we encounter an object with a different offset, we report a joint movement conflict.
If no conflicts occur, all objects connected to $o$ will receive the same offset.
The bonds between the objects are assigned to the graphical information of $p$ (this way, we do not have to add more graphical information to the objects).

If the system's anchor is an object instead of a particle, we start the joint movement BFS at the first particle in the system's particle list.
After the movement simulation, we find the offset $v$ of the anchor object and subtract $v$ from all joint movement offsets of all particles, objects and bonds in the system.
Additionally, if some object has not received a joint movement offset during the simulation, we have to throw an exception because the object is not connected to the rest of the system.


### Between the Phases

After the successful joint movement simulation (or static bond computation in case no particles have moved), the `FinishMovementInfo()` method is called.
This method calls [`StoreAndResetMovementInfo()`][9] on every particle in the system, which causes the particles to record their bond and joint movement data in their internal histories and reset them for the next round.

Afterward, the `UpdateNeighborCaches()` method updates the neighbor caches of all particles according to the new positions.
The neighbor cache is a simple array of [`Particle`][10] references indexed by global labels.
It is used to avoid frequent neighbor position calculations and particle map lookups, especially in the remaining round simulation.

The `SetupPinGraphicState()` method then initializes the circuit visualization data of each particle.
This data is stored in an instance of the [`ParticlePinGraphicState`][11] class, which contains not only information on a particle's internal circuit setup but also its external connections to neighboring particles.
This external information depends on which neighboring nodes of a particle are occupied and the movements of the neighbor particles.
If a neighbor performs a movement relative to the particle, its circuit connection has to fade in instead of being visible during the whole animation.
All of this information is computed in `SetupPinGraphicState()` between the movement and the beep phase.


### Beep Activation

Like the movement phase, the beep phase starts with the particle activations.
The `ActivateParticlesBeep()` method simply calls [`ActivateBeep()`][12] on all particles and handles some exceptions that might occur by turning them into the appropriate exception types and rethrowing them.
In their activations, particles can set a new planned pin configuration and schedule beeps and messages.
Processing these scheduled actions is the main task of the beep phase simulation.

The [`ApplyNewPinConfigurations()`][13] method calls [`ApplyPlannedPinConfiguration()`][14] on all particles.
This causes the particles to update their pin configurations if they have planned a new configuration.
If no pin configuration was planned by a particle, it retains the configuration it had after the movement phase, which is either the configuration from before the movement phase (in case no movement was made) or a default configuration matching the current expansion state.


### Circuit Computation

In the next step, the updated pin configurations are used to construct the circuits and distribute the scheduled beeps and messages.
This is handled by the `DiscoverCircuits(...)` method, which uses an approach similar to `SimulateJointMovements()`.
The idea is to run another BFS on the particle system, using simple neighborhood instead of bonds for connections, and "grow" the circuits in the process.

For this, we use the [`Circuit`][15] class to represent circuits that can grow over time.
Each [`Circuit`][15] has a unique integer ID, a list of child circuits and a root parent.
Instances of the [`SysPartitionSet`][16] class use the integer ID to identify the circuit they belong to.
Adding a [`SysPartitionSet`][16] to a circuit is as simple as setting the ID correctly (and incrementing the counter of the circuit).
When a [`SysPartitionSet`][16] happens to connect two existing circuits, the circuits have to be merged.
To merge one [`Circuit`][15] instance into another, the one with fewer partition sets becomes a child of the larger circuit, also transferring all of its children.
The [`SysPartitionSets`][16] still point to the [`Circuits`][15] they were originally assigned to, but these [`Circuits`][15] now refer to the bigger circuit as their root parent.

The [`Circuit`][15] class also stores a beep flag, a message and a color override.
Whenever a partition set is added to a circuit or two circuits are merged, these values are updated according to the following rules:
If any involved object has an active beep flag (i.e., a beep was sent on the circuit/partition set), the resulting circuit will have an active beep flag.
If any involved object has a scheduled message, the resulting circuit will have the message with the highest priority of the merging objects.
The priorities are compared using the [`GreaterThan`][17] method of the [`Message`][18] class.
This means that for messages sent on the same circuit, only the highest priority message gets through.
Finally, if the larger circuit does not have a color override yet and the merging circuit/partition set has a color override, then the resulting circuit will have the same color override.
Thus, only the first encountered color override is applied.

![Merging circuits](~/images/circuit_merge.png "Merging circuits")

This merging circuit data structure is used to construct the circuits during the BFS.
In this procedure, each particle $p$ is processed as follows:
First, we find all neighbors of $p$ that have already been processed.
The partition sets of these neighbors have already been assigned to circuits which will have to be considered in the procedure.
We also enqueue the neighbors that have not been processed and not enqueued yet.
Next, we iterate through all non-empty partition sets of $p$ and assign them to circuits.
For each partition set $s$, we iterate through its pins and find the neighboring partition set connected to that pin, if one exists.
At the first processed neighbor that is found for $s$, we add $s$ to the circuit to which the neighbor's partition set belongs.
If we encounter another neighboring partition set while processing the remaining pins, we merge that partition set's circuit with the first circuit because $s$ connects the two circuits.
If we do not encounter any neighboring partition sets that have already been processed, we create a new circuit that only contains $s$ (this is the case when the first particle is processed, for example).
In essence, we collect all partition sets $s$ is connected to and merge their circuits.

After this BFS procedure, every non-empty partition set of every particle has been assigned to a circuit (note that the particle structure is connected because the movement phase enforces this, which means that every particle is reached by the BFS).
We then assign a color to each circuit without a color override.
For this, we iterate over all created circuits, but we only process the circuits marked as roots, because only these represent the complete circuits.
If such a root circuit does not have a color override, we assign one of the colors from [`ColorData.Circuit_Colors`][19], which is an array of colors that look good together while not being too similar.
Each circuit gets a new color until we wrap around and start repeating colors.

The next step is to deliver the sent beeps and messages.
While computing the circuits, every root circuit has received an active beep flag if one of its partition sets has sent a beep, and the highest priority message of all messages sent by its partition sets, if any.
We now iterate through all particles and all partition sets of each particle again.
For each partition set, we simply copy the beep and message info stored in the root parent of its assigned circuit.
Due to the way the circuits were constructed, the root parent is always the complete circuit that contains the partition set, so the information stored in that circuit is correct.
During this iteration, we also complete the graphical circuit information stored in each particle's [`ParticlePinGraphicState`][11].
This information includes the partition set placement mode in the particle's head and tail and the individual partition set data like the pins it contains, its color and its beep and message status.

The final step of the beep phase is the `FinishBeepAndMessageInfo()` method.
In this method, we call [`StoreBeepsAndMessages()`][20] and [`ResetPlannedBeepsAndMessages`][21] on all particles, causing them to record their sent and received beeps and messages in their internal histories and reset their planned beeps and messages in preparation for the next round.


### Round Finalization

After the actual simulation of the two phases, only a few steps are left to finish the round.
The first step is pushing visualization updates to the render system.
This is done by the `UpdateAllParticleVisuals(...)` method, which calls the various graphics update methods for all particles in the right order and notifies the render system about the finished updates.
The same method is also called to push updates after changing the current round.

Next, the `CleanupAfterRound()` method is called to reset all helper data in the particles, like flags, scheduled actions and temporary graphics info.
This is important because it ensures that before each round simulation or similar simulation state udpate, all of this helper data is in the same default state and ready to be used.

The last step of a round simulation is checking whether the algorithm has finished.
This is checked by the `HasSimulationFinished()` method, which calls [`IsFinished()`][22] on all particles and returns `true` if every particle in the system returns `true`.
If this is the case, we record the round in which the algorithm finished and cause the simulation to pause.
It can still be resumed afterwards, but in most cases, the algorithm will not do anything in the following rounds.



## Snapshot Illusion

There is one aspect of the round simulation that should be addressed in particular.
In the theoretical model, all particles are activated simultaneously in each of the two phases:
They all run their *look* procedure at the same time, taking a snapshot of the current system state on which their subsequent computations are based.
This means that their actions only take effect after all activations are finished and that no particle can detect another particle's actions until the next phase.
We need to simulate this behavior even though we activate the particles sequentially.

Two of the systems used to achieve this have already been explained above:
For the movement simulation, we store each particle's planned movement and apply the movement actions after all activations are finished.
Thus, during the activations, all particles keep their positions, regardless of what movement they scheduled or how they defined their bonds.
Similarly, the pin configurations are updated after the activations and the beeps and messages are delivered at the end of the round simulation.

What has not been mentioned yet is the internal state of the particles, i.e., the particle attributes.
In each of the two activation methods, the particles are able to modify their own state by changing their attribute values.
Moreover, they can even use the updated attribute values for further calculations in the same round, making it easier to write the algorithm code.
At the same time, other particles should not be able to read the updated values until the next round.
These two features become even more complicated to combine when we simulate two phases within a single round.

The [`ParticleAttribute`][23] class defines a simple [`GetValue`][24] method returning the attribute's "snapshot value", i.e., its value at the beginning of the current phase.
It also defines a [`GetCurrentValue`][25] method which returns the attribute's latest value.
This method is only available to the particle owning the attribute so that other particles cannot read this value.
The [`Particle`][10] class's [`isActive`][26] flag is used to check whether the attribute's owner is the particle that is currently being activated.

However, there is one more problem that has to be solved:
Let an attribute $a$ start with value $x$ in round $i$.
If $a$'s value is updated to $x'$ during the movement activation, the value $x$ is still available because it is stored in the history for round $i-1$.
Thus, the "snapshot value" $x$ is readily available if a neighboring particle wants to read $a$.
But since we also simulate the beep phase in the same round $i$, we run into a problem:
If $a$'s value is updated again in the beep phase, say to value $x''$, this new value overwrites the previous value $x'$ because it takes the same spot in the attribute's history (round $i$).
Thus, the new "snapshot value" $x'$ would be lost without a measure to keep it.
For this purpose, the particle attribute classes have an [`intermediateVal`][27] field which they use to store the value $x'$.



[1]: xref:AS2.Sim.ParticleSystem
[2]: xref:AS2.Sim.ParticleSystem.SimulateRound
[3]: xref:AS2.AmoebotSimulator
[4]: xref:AS2.Sim.ParticleSystem.CurrentRound
[5]: xref:AS2.Sim.ParticleSystem.LatestRound
[6]: xref:AS2.Sim.ParticleAlgorithm.ActivateMove
[7]: xref:AS2.Sim.Particle.queuedForJMProcessing
[8]: xref:AS2.Visuals.ParticleBondGraphicState
[9]: xref:AS2.Sim.Particle.StoreAndResetMovementInfo
[10]: xref:AS2.Sim.Particle
[11]: xref:AS2.Visuals.ParticlePinGraphicState
[12]: xref:AS2.Sim.ParticleAlgorithm.ActivateBeep
[13]: xref:AS2.Sim.ParticleSystem.ApplyNewPinConfigurations
[14]: xref:AS2.Sim.Particle.ApplyPlannedPinConfiguration
[15]: xref:AS2.Sim.Circuit
[16]: xref:AS2.Sim.SysPartitionSet
[17]: xref:AS2.Sim.Message.GreaterThan(AS2.Sim.Message)
[18]: xref:AS2.Sim.Message
[19]: xref:AS2.ColorData.Circuit_Colors
[20]: xref:AS2.Sim.Particle.StoreBeepsAndMessages
[21]: xref:AS2.Sim.Particle.ResetPlannedBeepsAndMessages
[22]: xref:AS2.Sim.ParticleAlgorithm.IsFinished
[23]: xref:AS2.Sim.ParticleAttribute`1
[24]: xref:AS2.Sim.ParticleAttribute`1.GetValue
[25]: xref:AS2.Sim.ParticleAttribute`1.GetCurrentValue
[26]: xref:AS2.Sim.Particle.isActive
[27]: xref:AS2.Sim.ParticleAttributeWithHistory`1.intermediateVal
