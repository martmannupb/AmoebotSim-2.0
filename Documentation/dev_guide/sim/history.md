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
The history starts in some round with index $i$ and stores a state for each round $i,i+1,\ldots,i+l-1$, where $l$ is the number of recorded states.

An object implementing the interface always has a *marker* that indicates the currently selected round.
The marker can be moved to any round $\geq i$, which will change the object's state to the one recorded for that round.
If the object's state changes while the marker is in any round $\geq i + l-1$, a new entry will be recorded and the history will be extended to the current location of the marker.

The history can also be cut off at the marker, meaning that all recorded states after the marker are removed.

**TODO**



## Storing value histories




[1]: xref:AS2.Sim.IReplayHistory




**TODO**


This page explains how the state of the simulator is defined, how it evolves over time and how it can be changed from the outside.

- **Modes**
	- The simulator has two modes:
		- Initialization Mode
		- Simulation Mode
	- **Initialization Mode**
		- Used to define the initial state of a simulation
		- UI selects an algorithm with parameters for its generation method
		- Generation method places particle placeholders that store particle parameters
		- Particle placeholders can also be added, removed, modified manually by the user
		- There is no movement: the system only has to store the position of each particle
		- When switching to Simulation Mode, the particle placeholders are replaced by actual particles with attached algorithms
			- Algorithms are initialized using the parameters stored in the placeholders
	- **Simulation Mode**
		- Starts in round 0 with a system of particles
		- Simulating a round progresses the round counter by 1 and updates the system state
			- Particle positions and states
		- All previous states are stored in the history (implicitly)
		- Previous states can be loaded arbitrarily
		- Particles cannot be added, moved or removed manually
			- But their states can be edited in the latest round
	- Methods to change states are
		- `InitializationModeStarted(string algorithmName)`
		- `InitializationModeAborted()`
		- `InitializationModeFinished(string algorithmName)`
- **Data structures and other state variables**
	- List of particles
		- Simple list containing all particles currently in the system
		- Separate lists for Init and Simulation Mode
	- Particle maps
		- Dictionary mapping grid coordinates to particles
		- Expanded particles have two keys in the map
		- Separate maps for Init and Simulation Mode
	- Maps are updated when the round changes during simulation
		- Lists need no updates during simulation because particles cannot be added or removed
	- Round indices
		- Two indices for the earliest and latest rounds
			- Keeping track of the entire available range of rounds
		- Current and previous round indices
			- Between round simulations, these indices have the same value
			- Before a round simulation starts, the current index is incremented by one
			- At the end of a round simulation, the previous index is incremented
	- Simple flag tracks whether we are in Init Mode or not
	- `storedSimulationState` and `storedSimulationRound`
		- When entering Init Mode from Simulation Mode, the current simulation state is stored
		- `storedSimulationState` indicates whether this was the case
		- `storedSimulationRound` stores the current round at the time when the state was stored
	- `inMovePhase` and `inBeepPhase`
		- Flags indicating the current phase of the round simulation
		- They are both `false` while no round simulation is going on
	- `finished` flag
		- Indicates whether the simulation has finished or not
		- Simulation has finished as soon as all particles are finished at the end of a round
		- `finishedRound` stores the round in which the simulation finished
	- `anchorIdxHistory`
		- The anchor particle is simply identified by its index in the list of particles
		- Because it is not used very often, we use its history directly (see history later)
		- Need no history for Init Mode, so here we only use one int
	- `selectedAlgorithm`
		- The algorithm selected during Init Mode
		- Changes when the user changes the algorithm in the dropdown
