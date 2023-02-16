# Developer Guide: The Simulator

The Simulator component is a collection of classes that work together to represent the simulation state and provide various features like round simulation, history management, the algorithm API, saving and loading of simulation states, and more.
Because most features depend on several different classes, we explain the system by going through its features instead of going through the classes.

The core of the simulator is the [`ParticleSystem`][1] class.
The [`AmoebotSimulator`][2] creates one instance of this class when the application starts and uses it to manage all simulation features until the application is stopped.
In most cases, the public methods and properties defined by this class should be the only way of modifying the simulation system.


## Simulator Modes

At any time, the simulator is in one of two modes: The **Initialization Mode** or the **Simulation Mode**.
While in Initialization Mode (in short Init Mode), it is possible to add particles to the system, remove or relocate existing particles and edit their parameters based on the currently selected algorithm.
This can all be done manually by the user through the UI or automatically by an algorithm's generation method.

When the simulator is in Simulation Mode, particles cannot be added or removed anymore, and they can only be edited in the latest round.
The system state evolves as the particle activations and movements are simulated, and previous system states can be reviewed due to the history feature.

### Implementation

The [`ParticleSystem`][1] provides several methods for changing between the modes:
The [`InitializationModeStarted(string selectedAlgo)`][3] method is used to change from Simulation Mode to Init Mode.
The ongoing simulation is then stored in a temporary file using the Save/Load feature and the selected algorithm's default generation method is used to generate a system of particles as a starting point.
If [`InitializationModeAborted()`][4] is called, the simulator switches back to Simulation Mode and reloads the previously stored simulation state.
The current initialization system is discarded in this case.
Finally, if [`InitializationModeFinished(string selectedAlgo)`][5] is called, the current particle system is used to instantiate a new particle system in which all particles are initialized with the selected algorithm using the parameters stored in the initialization particles.
The system then resets the round counter to 0 and starts a new simulation in Simulation Mode.

In Init Mode, there are several simple methods like [`SetSelectedAlgorithm(string algoName)`][6] and [`GenerateParticles(string methodName, object[] parameters)`][7] that directly correspond to the available UI buttons and tools, allowing the user to easily modify the initialization system.


## Simulation State

The main task of the simulator is to represent the state of the particle system, both during initialization and during the actual simulation.
In both modes, the system uses two simple data structures to store instances of a class that represents an Amoebot particle.
The first data structure is a list that stores all particles in the system and the second is a map using grid coordinates as keys and particles as values.
Expanded particles occupy two grid nodes, so they appear twice in the map.

### Rounds

During Simulation Mode, the system keeps track of the round numbers using simple integer counters.
A simulation always starts in round 0 and the counter is incremented with each simulated round.
There are two properties storing the first ([`EarliestRound`][8]) and the last ([`LatestRound`][9]) simulated round, which define the range of valid round indices.
The current round is also represented by two properties, [`CurrentRound`][10] and [`PreviousRound`][11].
Outside of a round simulation (i.e., to the outside of the system), they always have the same value.
The difference is that [`CurrentRound`][10] is incremented at the start of a round simulation and [`PreviousRound`][11] is incremented at the end, which is very convenient for some calculations during the simulation.

### Particles

The rest of the system state is stored in the particle instances themselves.
There are two different classes, one for each mode: [`Particle`][12] for the Simulation Mode and [`InitializationParticle`][13] for Init Mode.
Both of them store the basic particle state information:
- The particle's global grid position, both for its head and its tail (which are equal for contracted particles)
- The current global expansion direction if the particle is expanded
- The particle's chirality, `true` meaning that it matches the global counter-clockwise rotation direction
- The particle's compass direction, represented as the global direction that the particle believes to be East

In Init Mode, the chirality and compass direction of a particle can be set freely and its position and expansion state can be changed as well; this is not possible in Simulation Mode.
Internally, the system uses the [`OpenInitParticle`][14] class to access the particle's data more freely.
The abstract [`InitializationParticle`][13] class just serves as an interface for custom generation algorithms.
It is the simulator's responsibility to ensure that the particles' positions match the occupied positions in the system's map whenever it is not currently simulating a round.

The particle classes contain much more information than what is listed here.
However, most of it is related to more specific features and thus, it is explained elsewhere.


**TODO**:
- History feature
	- Idea for value histories
	- `IReplayHistory` interface
	- Decentralized approach
- Save/Load feature
	- Serializable objects can be translated to and from JSON using Unity's JSON utility
	- Implement combination of `GenerateSaveData` and `InitializeFromSaveData` methods
	- Make the entire particle system serializable
	- Standalone File Browser to select files
- Algorithm API
	- Attributes
	- Method calls handled by simulator
	- Scheduling actions and storing them temporarily until the round simulation
	- Initialization API
		- Methods provided by abstract generator class
	- System initialization
		- Particle placeholders
		- Particle factory
		- Link between particle and algorithm
- Round simulation
	- Only rough overview
	- Start each phase by activating all particles
	- Illusion of simultaneous activation and working on a snapshot
	- Perform movements first, then activate again and find circuits, finally send beeps and messages
- Circuits
	- Each particle has a PinConfiguration
		- Use local labels, int IDs
		- Hierarchy of small classes
		- History uses compressed version
	- Flags for current and planned PC
	- Data structures for planned and received beeps and Messages
	- Circuit discovery
		- BFS
		- Merging circuits
		- Messages
- Joint Movements
	- Bond representation and detection
	- Movement types
	- BFS and JM offset propagation
- Reflection
	- Singleton classes for finding all existing algorithms
- Error handling
	- Several custom exception types
	- Some of them can be handled by the simulator
	- If an error occurs that can be handled: Undo the current simulation progress and reset to the previous round




[1]: xref:AS2.Sim.ParticleSystem
[2]: xref:AS2.AmoebotSimulator
[3]: xref:AS2.Sim.ParticleSystem.InitializationModeStarted(System.String)
[4]: xref:AS2.Sim.ParticleSystem.InitializationModeAborted
[5]: xref:AS2.Sim.ParticleSystem.InitializationModeFinished(System.String)
[6]: xref:AS2.Sim.ParticleSystem.SetSelectedAlgorithm(System.String)
[7]: xref:AS2.Sim.ParticleSystem.GenerateParticles(System.String,System.Object[])
[8]: xref:AS2.Sim.ParticleSystem.EarliestRound
[9]: xref:AS2.Sim.ParticleSystem.LatestRound
[10]: xref:AS2.Sim.ParticleSystem.CurrentRound
[11]: xref:AS2.Sim.ParticleSystem.PreviousRound
[12]: xref:AS2.Sim.Particle
[13]: xref:AS2.Sim.InitializationParticle
[14]: xref:AS2.Sim.OpenInitParticle
