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





- Core is the `ParticleSystem` class
	- `AmoebotSimulator` creates an instance of this class and keeps it around forever
	- Maintains the entire system state
	- Executes the round simulation logic
	- Provides API for changing the system behavior
	- Also handles algorithm API calls
	- Complex system of several smaller systems working together
	- Subsystems will be explained in more detail on their own pages
- Reflection for discovering Amoebot algorithms and generation methods
	- Separate simpleton classes use reflection to find out which algorithms exist and provide an interface for accessing their info and instantiating them


[1]: xref:AS2.Sim.ParticleSystem
[2]: xref:AS2.AmoebotSimulator
[3]: xref:AS2.Sim.ParticleSystem.InitializationModeStarted(System.String)
[4]: xref:AS2.Sim.ParticleSystem.InitializationModeAborted()
[5]: xref:AS2.Sim.ParticleSystem.InitializationModeFinished(System.String)
[6]: xref:AS2.Sim.ParticleSystem.SetSelectedAlgorithm(System.String)
[7]: xref:AS2.Sim.ParticleSystem.GenerateParticles(System.String,System.Object[])
