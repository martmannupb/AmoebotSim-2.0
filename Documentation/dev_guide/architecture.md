# Dev Guide: Architecture Overview

This page gives a rough overview of the project's architecture, its main components and their interactions.

AmoebotSim 2.0 has three main components:
- The Simulator
- The Render System
- The User Interface


## The Simulator

The [Simulator](sim/home.md) component handles all of the simulation logic.
It maintains the current state as well as the state history of the particle system, executes the round simulation logic including validity checks, and it provides the API for developing Amoebot algorithms.

It consists of a collection of classes representing the Amoebot particles and their properties, managed by a single instance of the [`ParticleSystem`][1] class.
Every action performed by any particle is registered and executed by this instance.
The class also provides the interface for the other components to access and modify the current simulation state.


## The Render System

The [Render System](render_system.md) is responsible for displaying the simulation state on the screen.
It stores the graphical data of the things that need to be visualized in a complex system of data structures which allows Unity to render a large number of animated particles efficiently.

In general, most of the objects handled by the Simulator have a counterpart that represents them in the Render System.
The Simulator pushes graphics updates into these representations after every round and the Render System pulls the required information from there.


## The User Interface

The [User Interface](ui.md) provides graphical widgets that display additional information and allow the user to interact with the system intuitively by pressing buttons, moving sliders, scrolling with the mouse wheel, etc.
It also handles keyboard inputs and preprocesses all user input before passing it to the correct part of the system.

When the state of the Simulator changes, e.g., when it switches between Initialization and Simulation Mode, the UI enables or disables widgets based on what user inputs are allowed for the current state.
To make particles selectable, the UI has to communicate with the Render System to find out where the particles are placed on the screen, and to make them editable, it has to tell the Simulator how to change a particle's state.


## System Setup

In Unity, an application is organized into *Scenes* with different content.
AmoebotSim 2.0 only has a single Scene, called `Simulator`.
This Scene contains the UI layers, the camera, the Algorithm Generator utility, and a GameObject called `Simulator`.
GameObjects can have various components attached to them that define their behavior (like rigidbody physics, animation rigs, player controllers etc.) and provide ways to access the GameObjects in code.
Custom components can be written as classes that inherit from Unity's [`MonoBehaviour`](https://docs.unity3d.com/Manual/class-MonoBehaviour.html) class.

The `Simulator` GameObject has one such component, which is of type [`AmoebotSimulator`][2].
This is a singleton class that manages the entire simulator and connects it to the Unity runtime environment by implementing some of the special `MonoBehaviour` methods:
- `Start()`: This method is called on each `MonoBehaviour` before the first frame of the application is computed.
	The [`AmoebotSimulator`][2] uses it to retrieve references to the other components of the simulator, connect them, and open the Initialization Mode.
- `Update()`: This method is called once per frame on each `MonoBehaviour`.
	In the simulator, it simply triggers the Render System to render the current system state.
- `FixedUpdate()`: In contrast to `Update()`, which may be called in irregular intervals due to varying render times, this method is called on each `MonoBehaviour` in *fixed* intervals.
	This interval can be customized by changing the `Time.fixedDeltaTime` value.
	The simulator uses this to control the simulation speed by running one simulation step in each call of `FixedUpdate()` (if the simulation is currently playing) and changing the fixed interval depending on the desired simulation speed.

Thus, the behavior of the Render System and the Simulator is mainly driven by the [`AmoebotSimulator`][2] instance.

The various UI handlers are independent `MonoBehaviour`s that are attached to the UI GameObjects.
The [`AmoebotSimulator`][2] only establishes a basic connection between the main UI handler and the other components.

The [`AlgorithmGenerator`][3] is implemented as a `MonoBehaviour` script attached to an otherwise empty GameObject.
However, because it should be run in the Unity Editor while the application is not running, it has the `ExecuteInEditMode` annotation.
Additionally, the [`AlgorithmGeneratorEditor`][4] class, which inherits from Unity's `Editor` class, references the [`AlgorithmGenerator`][3] and defines its custom Inspector layout including the `Generate...` button and the input fields.



[1]: xref:AS2.Sim.ParticleSystem
[2]: xref:AS2.AmoebotSimulator
[3]: xref:AS2.AlgorithmGenerator
[4]: xref:AS2.AlgorithmGeneratorEditor
