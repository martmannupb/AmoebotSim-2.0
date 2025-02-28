# Algorithm Development: Creating a New Algorithm

## Basics

Amoebot algorithms are programmed from the perspective of individual amoebots, i.e., the algorithms do not have access to a global view of the structure.
Every amoebot in the structure runs a separate instance of the algorithm, which defines its entire state and controls its behavior.

The simulation runs in *synchronous rounds*, as described on the [amoebot model pages](~/amoebot_model/home.md).
Each simulation round is split into two phases: The *movement phase* and the *communication (beep) phase*.
In each phase, all amoebots are activated simultaneously, allowing them to perform computations and movement or communication actions based on the system's state at the beginning of the phase.
The amoebot behavior during each of the phases is implemented in two methods called [ActivateMove][6] and [ActivateBeep][7], which are the most important parts of any algorithm.
All changes made during the activations take effect *at the end of the phase*.
Because all amoebots are activated at the same time, they are unaware of any changes made by their neighbors until the next phase.
The round computation process is explained in more detail on the [reference pages](~/model_ref/rounds.md).


## The Algorithm Generator

Every algorithm is contained in a single C# file within the `Assets/Code/Algorithms` directory.
To simplify the task of creating a new algorithm file, the simulator project provides a utility for creating new algorithms from the Unity Editor, the **Algorithm Generator**.

To access the Algorithm Generator, simply expand the *Simulator* Scene in the Hierarchy window and select the *Algorithm Generator* GameObject.
In the Inspector window, you should now see the Algorithm Generator Component displaying several input fields and a *Generate...* button:

![Algorithm Generator Component](~/images/editor_alg_gen.png "The Algorithm Generator Component")

You can now start editing the values in the input fields:

- *Algo Name*: This is the basic internal identification of your algorithm.
	It is used to name the classes belonging to the algorithm, a namespace that contains these classes and the file containing all your algorithm code.
	It must be a valid C# identifier and unique among all algorithms.
- *Class Name*: This will be the name of the class defining your algorithm.
	It has to be a valid C# class name that does not already exist in the project.
	If you do not enter a name, the default name will be the algorithm base name with the suffix "Particle".
- *Display Name*: This field defines the name that will be displayed in the algorithm selection field during runtime.
	It can be an arbitrary string but it still has to be unique.
	If you leave this field empty, the base name will be used.
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

### Namespaces

The first two lines of the file include the [`AS2.Sim`][10] and the `UnityEngine` namespace.
[`AS2.Sim`][10] contains classes and methods belonging to the algorithm and simulation API that are necessary for implementing an amoebot algorithm, like the [`ParticleAlgorithm`][1] class.
The `UnityEngine` namespace provides the Unity Editor logging system (accessed using `Debug.Log`) and random number generation (via `Random.Range`, for example).
See [Advanced Features](advanced.md) for details.
The third line includes the static [`Constants`][12] class, giving access to some constant values helping to make the code easier to read.

The rest of the code is contained in a namespace called `AS2.Algos.<YourAlgoBaseName>`.
Every amoebot algorithm should be contained in its own namespace within `AS2.Algos` to separate it from the other algorithms.
Being contained in the [`AS2`][11] namespace provides access to all the relevant classes and methods of the algorithm API.
It is safe to rename the algorithm's namespace later.

### The Algorithm Class

The largest part of the file is the algorithm class: A class with the name you entered in the Algorithm Generator which inherits from [ParticleAlgorithm][1].
The [ParticleAlgorithm][1] class is the base class for all amoebot algorithms and provides the API for defining the amoebot's state and behavior.


#### Properties

```csharp
public static new string Name => "<YOUR DISPLAY NAME>";
```

The static `Name` property defines the display name of the algorithm.
It must be unique among all algorithms in the project.


```csharp
public override int PinsPerEdge => <INT>;
```

The `PinsPerEdge` property defines the number of pins on each edge/port of an amoebot.
Although this property is not marked `static`, it must be the same for all instances and it must not change at runtime.


```csharp
public static new string GenerationMethod => typeof(<GENERATION METHOD CLASS>).FullName;
```

The [`GenerationMethod`][13] property is optional.
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

The constructor of an amoebot algorithm must have a single parameter of type [Particle][2], which must be passed to the base class constructor.
This object represents the amoebot that will be controlled by the algorithm in the simulation.
Currently, there is no further use for this object, but it may be extended to provide additional information for special purposes.

The most important code that must be put into the constructor is the initialization of the amoebot's state attributes.
The attributes are always declared without an initial value and must be initialized in the constructor.
To learn more about attributes, refer to the [Particle Attribute reference pages](~/model_ref/attrs.md) (these will also be linked in the walkthrough part of this guide).

Finally, you can set the initial color of the amoebot by calling the [SetMainColor][3] method.
The [ColorData][4] class provides a set of default amoebot colors.
If the color is not set explicitly, the amoebot will be rendered in black.

> [!WARNING]
> The constructor should *not* make assumptions about the amoebot's current expansion state or other properties that may change during the simulation.
> This is because the constructor is also used to initialize the amoebot when loading a save file, in which case it may be in a position it could not have at the beginning of the simulation.
> If the amoebot requires initialization based on its starting position and expansion state, use the `Init` method.


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

The [IsFinished][5] method can be implemented if the algorithm allows amoebots to reach a state in which they become completely passive.
If all amoebots in the structure return `true` after a simulation round, the simulation is considered complete.
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

The [ActivateMove][6] and [ActivateBeep][7] methods implement the behavior of the amoebot.
Each of them is called exactly once in every simulation round.
Please refer to the [Round Simulation reference page](~/model_ref/rounds.md) for a more detailed explanation of how these methods are called and what they are supposed (and allowed) to do.
This topic will also be considered in the [Implementation Walkthrough](demo.md).


### The Initializer Class

The algorithm template also contains a second class, which can be used to define a custom initialization procedure.
This class inherits from [InitializationMethod][8], which is the base class for all classes implementing initialization procedures.
The constructor must have a single parameter of type [ParticleSystem][9] and pass it to the base constructor.
The full name of this initialization class must be returned by the [`GenerationMethod`][13] property of the algorithm class that uses this initialization procedure.


```csharp
public void Generate(<PARAMETERS WITH DEFAULT VALUES>)
{
    // Place particles and set Init() parameters here
}
```

The initialization class must implement a `Generate` method with parameters of simple types and with default values.
These parameters will be displayed in the Initialization Mode UI when the algorithm referencing this initialization class is selected.
The `Generate` method must place the amoebots in the structure and set their chirality and compass direction.
It can also set their parameters for the `Init` method.
The [Implementation Walkthrough](demo.md) provides an example for how this system is used.



[1]: xref:AS2.Sim.ParticleAlgorithm
[2]: xref:AS2.Sim.Particle
[3]: xref:AS2.Sim.ParticleAlgorithm.SetMainColor(Color)
[4]: xref:AS2.ColorData
[5]: xref:AS2.Sim.ParticleAlgorithm.IsFinished
[6]: xref:AS2.Sim.ParticleAlgorithm.ActivateMove
[7]: xref:AS2.Sim.ParticleAlgorithm.ActivateBeep
[8]: xref:AS2.InitializationMethod
[9]: xref:AS2.Sim.ParticleSystem
[10]: xref:AS2.Sim
[11]: xref:AS2
[12]: xref:AS2.Constants
[13]: xref:AS2.Sim.ParticleAlgorithm.GenerationMethod
