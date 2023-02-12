# Developer Guide: Simulation State

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
