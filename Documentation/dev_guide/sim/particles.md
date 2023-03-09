# Developer Guide: Particles

**TODO**


This page explains how particles are represented in the simulator.

- The `Particle` class represents a single particle in the system during Simulation Mode
	- It contains all necessary state information as well as its own state history and additional data that is useful for the simulator
	- The class implements the `IParticleState` interface (for visualization) and the `IReplayHistory` interface (so that its state can be treated as one entity with a history)
- Particle state
	- Fixed information
		- Compass direction and chirality
	- Positional information
		- Head and tail position (global)
			- Only tail position has history
		- Expansion direction and flag with history for direction
	- List of attributes
		- See page about attributes
	- Active and marked bonds with history
		- Represented as bit vectors where indices are local edge labels
	- Communication
		- Current pin configuration and history of all configurations
		- Arrays of received and planned beeps and messages with histories
			- Indices are local partition set IDs
	- Visualization
		- Main color (particle fill color) and flag for color being set, both with history
		- Histories of joint movement and bond movement info (used to animate movements when reviewing a round)
	- Simulation data (without history)
		- `scheduledMovement`
			- The movement action scheduled by the particle algorithm in the current movement phase
		- `predictXYZ`
			- Positional properties for after the scheduled movement was performed
		- `markedForAutomaticBonds`
			- Set when the algorithm wants to have automatic bonds
		- `jmOffset`
			- The global joint movement offset imposed on this particle by its neighbors
		- `movementOffset`
			- The global offset of the moving part of this particle if it expands or contracts in this round
			- This is the particle's contribution to the `jmOffset` it applies to its neighbors
		- `isHeadOrigin`
			- Whether the particle's head is its origin during joint movements
		- `processedJointMovement` and `queuedForJMProcessing`
			- Flags for managing particles while their joint movements are being processed
		- `activeBondsGlobal` and `markedBondsGlobal`
			- Active and marked bonds used for the joint movement simulation
			- May be different from local bonds due to automatic bonds or bond restrictions
			- Global labels are easier to work with
		- `processedPinConfig` and `queuedForPinConfigProcessing`
			- Flags for managing particles while their circuits are being processed
		- `plannedPinConfiguration`
			- The pin configuration planned by the algorithm during the current beep phase
		- `isActive`
			- Is true while the particle is being activated
			- This is useful because the same API calls can have different behavior depending on whether they are called on the currently active particle or some other particle (e.g., a neighbor)

