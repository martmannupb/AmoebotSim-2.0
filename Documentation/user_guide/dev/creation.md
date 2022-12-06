# Algorithm Development: Creating a New Algorithm

## Basics

Amoebot algorithms are programmed from the perspective of individual Amoebots, i.e., the algorithms do not have access to a global view of the system.
Every Amoebot in the system runs a separate instance of the algorithm, which defines its entire state and controls its behavior.

The simulation runs in *synchronous rounds*, as described on the [Amoebot model pages](~/amoebot_model/home.md).
Each simulation round is split into two phases: The *movement phase* and the *communication (beep) phase*.
In each phase, all Amoebots are activated simultaneously, allowing them to perform computations and movement or communication actions based on the system's state at the beginning of the phase.
The Amoebot behavior during each of the phases is implemented in two methods called [ActivateMove][6] and [ActivateBeep][7], which are the most important parts of any algorithm.
All changes made during the activations take effect *at the end of the phase*.
Because all Amoebots are activated at the same time, they are unaware of any changes made by their neighbors until the next phase.
The round computation process is explained in more detail on the [reference pages](~/model_ref/rounds.md).


## The Algorithm Generator

Every algorithm is contained in a single C# file within the `Assets/Code/Algorithms` directory.
To simplify the task of creating a new algorithm file, the simulator project provides a utility for creating new algorithms from the Unity Editor, the **Algorithm Generator**.

To access the Algorithm Generator, simply expand the *Simulator* Scene in the Hierarchy window and select the *Algorithm Generator* GameObject.
In the Inspector window, you should now see the Algorithm Generator Component displaying several input fields and a *Generate...* button:

![Algorithm Generator Component](~/images/editor_alg_gen.png "The Algorithm Generator Component")

You can now start editing the values in the input fields:

- *Class Name*: This will be the name of the class defining your algorithm.
	It has to be a valid C# class name that does not already exist in the project.
	The name you enter will also be used to name the algorithm file.
- *Display Name*: This field defines the name that will be displayed in the algorithm selection field during runtime.
	It can be an arbitrary string but it still has to be unique.
	If you leave this field empty, the class name will be used.
- *Num Pins*: The number of pins per connection used by the algorithm.
	Please refer to the [Reconfigurable Circuits page](~/amoebot_model/circuits.md) if you are not familiar with the concept of pins.
	You can enter 0 if your algorithm does not use circuits at all.

You can still change all of these properties after generating your algorithm, if necessary.

Once you are finished editing the input fields, you can click the *Generate...* button and select a location in which to create the algorithm file.
In most cases, the default location (`Assets/Code/Algorithms`) and file name will be appropriate.
After a short refresh, your new algorithm file should be visible in the Project window.


## Algorithm File Structure

Find the generated algorithm file in `Assets/Code/Algorithms` using the Project window and double-click it to open it in your IDE.
The file contains a blank algorithm template in which all optional features are commented out.
In the following, we will go through the structure of this file and explain the meaning of each component.


### The Algorithm Class

The largest part of the file is the algorithm class: A class with the name you entered in the Algorithm Generator which inherits from [ParticleAlgorithm][1].
The [ParticleAlgorithm][1] class is the base class for all Amoebot algorithms and provides the API for defining the Amoebot's state and behavior.


#### Properties

```csharp
public static new string Name => "<YOUR DISPLAY NAME>";
```

The static `Name` property defines the display name of the algorithm.
It must be unique among all algorithms in the project.


```csharp
public override int PinsPerEdge => <INT>;
```

The `PinsPerEdge` property defines the number of pins on each edge/port of a particle.
Although this property is not marked `static`, it must be the same for all instances and it must not change at runtime.


```csharp
public static new string GenerationMethod => typeof(<GENERATION METHOD CLASS>).FullName;
```

The `GenerationMethod` property is optional.
If the algorithm uses a custom initialization procedure, this property must return the full class name of the class that implements the initialization method (see below).


#### Constructor and Attributes

```csharp
// Declare attributes here
// ...

public <YOURALGONAME>(Particle p) : base(p)
{
    // Initialize attributes
    // Set the default initial color (optional)
}
```

The constructor of an Amoebot algorithm must have a single parameter of type [Particle][2], which must be passed to the base class constructor.
This object represents the Amoebot that will be controlled by the algorithm in the simulation.
Currently, there is no further use for this object, but it may be extended to provide additional information for special purposes.

The most important code that must be put into the constructor is the initialization of the particle's state attributes.
The attributes are always declared without an initial value and must be initialized in the constructor.
To learn more about attributes, refer to the [Particle Attribute reference pages](~/model_ref/attrs.md) (these will also be linked in the walkthrough part of this guide).

Finally, you can set the initial color of the particle by calling the [SetMainColor][3] method.
The [ColorData][4] class provides a set of default particle colors.
If the color is not set explicitly, the particle will be rendered in black.


#### Custom Initialization

```csharp
public void Init(<PARAMETERS WITH DEFAULT VALUES>)
{
    // Update initial attribute values
}
```

If you want to implement a custom initialization procedure, you can implement a method with the name `Init` and a list of parameters with simple types and default values.
Simple types are types that can be represented as particle attributes, e.g., `int`, `bool`, `enum`, `float` and `string`.
The `Init` method will be called immediately after the constructor with the parameter values that were set in the Initialization Mode.
It should be used to override the initial attribute values set in the constructor depending on the given parameters.
Please refer to the [Implementation Walkthrough](demo.md) for details on how to setup custom initialization.


#### Activation and Termination

```csharp
public override bool IsFinished()
{
    // Return true when particle has terminated
}
```

The [IsFinished][5] method can be implemented if the algorithm allows particles to reach a state in which they become completely passive.
If all particles in the system return `true` after a simulation round, the simulation is considered complete.
Currently, this has no effect other than logging a message to notify the user.


```csharp
public override void ActivateMove()
{
    // Movement code: Update bonds and schedule movements
}

public override void ActivateBeep()
{
    // Communication code: Update pin configuration and send beeps and messages
}
```

The [ActivateMove][6] and [ActivateBeep][7] methods implement the behavior of the Amoebot.
Each of them is called exactly once in every simulation round.
Please refer to the [Round Simulation reference page](~/model_ref/rounds.md) for a more detailed explanation of how these methods are called and what they are supposed (and allowed) to do.
This topic will also be considered in the [Implementation Walkthrough](demo.md).


### The Initializer Class

The algorithm template also contains a second class, which can be used to define a custom initialization procedure.
This class inherits from [InitializationMethod][8], which is the base class for all classes implementing initialization procedures.
The constructor must have a single parameter of type [ParticleSystem][9] and pass it to the base constructor.
The full name of this initialization class must be returned by the `GenerationMethod` property of the algorithm class that uses this initialization procedure.


```csharp
public void Generate(<PARAMETERS WITH DEFAULT VALUES>)
{
    // Place particles and set Init() parameters here
}
```

The initialization class must implement a `Generate` method with parameters of simple types and with default values.
These parameters will be displayed in the Initialization Mode UI when the algorithm referencing this initialization class is selected.
The `Generate` method must place the particles in the system and set their chirality and compass direction.
It can also set their parameters for the `Init` method.
The [Implementation Walkthrough](demo.md) provides an example for how this system is used.



[1]: xref:Global.ParticleAlgorithm
[2]: xref:Global.Particle
[3]: xref:Global.ParticleAlgorithm.SetMainColor(Color)
[4]: xref:Global.ColorData
[5]: xref:Global.ParticleAlgorithm.IsFinished
[6]: xref:Global.ParticleAlgorithm.ActivateMove
[7]: xref:Global.ParticleAlgorithm.ActivateBeep
[8]: xref:Global.InitializationMethod
[9]: xref:Global.ParticleSystem
