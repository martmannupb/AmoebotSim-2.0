# Developer Guide: Round Simulation

This page explains the mechanics behind the round simulation.
One simulated round consists of two phases: The movement phase and the beep phase.
In the theoretical model, these phases are mostly treated as separate rounds, each consisting of a *look* phase, a *compute* phase and an *act* phase, during which the particle either moves or sends beeps via its circuits.
Since this separation was a late addition to the model, the simulator runs the *look-compute-move* cycle (called *movement phase*) and the *look-compute-beep* cycle (called *beep phase*) sequentially within a single round.
However, to the particle algorithm, the two phases appear like separate rounds so that the system matches the theoretical model from an algorithm developer's point of view.

Each of the two phases has roughly the same structure:
We first activate all particles and store their actions, then we apply the effects of their scheduled actions, checking whether they are valid, and finally, we store the final validated result and perform a cleanup for the next round.
After both phases are finished, we push the collected graphical information to the render system.
If a conflict occurs during this whole process, we reset the simulation state to the previous round.
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
If we encounter any particle that has not been processed in the BFS, then there exists no bond path from the anchor particle to that particle, so we have to throw an exception because the bond structure is disconnected, which is not allowed.
The exception handling code will revert the simulation state to the previous round, undoing the state changes that were applied so far.
If no disconnected particle is encountered, we finally replace the particle map with the updated positions.


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
The `ActivateParticlesBeep()` method simply calls [`ActivateBeep()`][12] on all particles and handles some exceptions that might occur by turning them into the appropriate exception types.
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





**TODO**





- Whole procedure is handled by the particle system
    - `SimulateRound()` is called regularly by the main class
    - Movement activation
    - Joint movement simulation
    - Joint movement cleanup
    - Between phases
    - Beep phase
        - Run beep activation method on all particles
        - Apply new pin configuration to each particle
        - Compute circuits and send beeps and messages
            - Again perform a BFS on particle system
            - For each unprocessed particle:
                - For each non-empty partition set:
                    - Go through all already processed neighbors
                    - If the PS is connected to a PS of a processed neighbor: Add the PS to the related circuit
                    - If this occurs multiple times: Merge all encountered circuits
                    - If this does not occur at all: Create a new circuit
            - Circuits are identified by IDs
                - Merging works with a tree structure
                - Each circuit has a number of children
                - When a circuit is merged with another, the children are transferred to the larger circuit
                    - All circuits have a reference to their root
                - If a beep is sent on one of the merged circuits, the resulting circuit will send a beep
                - Similar for messages (but highest priority message is taken)
            - Give colors to circuits (round-robin color selection)
            - Distribute beeps and messages to all particles on each circuit
                - Go through all particles and their partition sets again
                - Also set graphical info while doing this
        - Finally store beeps and messages in history and reset data for next round
    - After both phases, push graphical updates and reset internal data for next round
    - Also check if the simulation has finished
- Snapshot illusion
    - In the model, all particles are activated simultaneously
        - They all operate on the same snapshot of the system
        - Their actions take effect after the activations
        - No particle can see what another particle has done in that round
    - We need to simulate this behavior somehow
    - Movement actions
        - Movements are stored as actions
        - Movement results are only applied at the end of the phase, after they were checked for validity
        - Particle map is also only updated at the end, so neighbors stay the same throughout the round
    - Beeps and messages
        - Only delivered at the end of the round
        - Similar to movement actions
        - They are kept throughout the movement phase for simplicity
            - They are not reset after the movements (it is another round so they *should* technically be reset)
            - No reason not to keep them
    - Attributes
        - Attributes are more difficult
        - Particles may want to include their attributes in calculations, which means they need to use the updated value immediately
            - But other particles should not see these updated values
        - Provide `GetValue` and `GetCurrentValue` methods
        - Only available to the particle itself (must be active to access current value)
        - One more problem: History
            - We simulate two rounds at once, only the last value should be kept
            - Want to access value from the snapshot and updated value
            - Cannot revert to previous round's value in beep phase because previous round was actually the move phase
            - Need an intermediate variable















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



