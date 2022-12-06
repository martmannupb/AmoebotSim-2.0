# Algorithm Development: Implementation Walkthrough

On this page, we will demonstrate how to implement a simple algorithm from scratch.
The focus of this demo is showing how conceptual ideas for an algorithm are translated into code for the simulation environment.
If you have set up the simulator according to the [Installation Guide](~/installation_guide/home.md), you can follow the walkthrough and experiment by changing the code along the way.

## A Simple Demo Algorithm

Before we can start writing code, we need a clear idea of what the algorithm should do.
This includes the behavior of the Amoebots as well as the system setup:
What is the initial configuration?
Do the particles have a common chirality and/or compass orientation?
Do they communicate using circuits and how many pins do they need?
These questions should be answered before we start with the implementation.

![!Demo Algorithm](~/images/demo_final.gif "The desired algorithm behavior")

For this example, we want the Amoebots to communicate using circuits and to perform simple joint movements.
To keep it simple, we will assume common chirality and compass alignment.
The initial configuration of the system will be a line of particles parallel to the West-East axis.
The idea for the algorithm is the following:
The system starts with a single, arbitrary particle that is marked as the *leader*.
In each round, the leader randomly decides whether a movement should be performed or not.
If it decides to perform a movement, it will send a beep on the global circuit (which has to be established first).
All particles receiving a beep will perform a movement:
If they are contracted, they will expand in the East direction and if they are expanded, they will contract into their tail.

From this description of the algorithm, we can already deduce that we will need a custom initialization method to place the particles and determine a leader, and that a single pin will be sufficient for this simple communication.
Due to the common compass orientation, all particles know where the East direction is, which simplifies the movements (although it is possible to lift that assumption and still achieve a very similar behavior!).

## Creating the Algorithm File

To start implementing the algorithm, we first need to create a new algorithm file.
Since this has already been explained on the [Algorithm Creation page](creation.md), we will only go through it briefly:

![Algorithm Generator](~/images/demo_generator.png "Generating the new algorithm file")

We create a new algorithm class called `DemoParticle`, call the algorithm "Demo Algorithm" and give it only a single pin.
The new file is created in the `Assets/Code/Algorithms` folder and contains a blank algorithm template.
The algorithm has a default generation method and can already be run in the simulator, but since we have not added any behavioral code yet, the particles will stay idle.

There are two components that need to be implemented next:
The Amoebot behavior and the system initialization.
Because we cannot test the behavior without a proper initialization, we will start with the custom initialization method and then implement the behavior.

## Implementing the System Initialization

Because custom system initialization is optional, the relevant code is commented out in the algorithm template.
As a result, our algorithm has a default initialization method which will place a number of particles randomly in such a way that they are still connected.

![Default generation method](~/images/demo_default_init.png "Default initialization method")

The method has parameters to control how many particles are placed and how likely it is for positions to be unoccupied, as well as settings for the chirality and compass orientation of the particles.
Our new initialization method does not need parameters, but we will make the number of particles adjustable to demonstrate how this works.

### Enabling Custom Initialization

To start with, we uncomment the `DemoParticleInitializer` class at the bottom of the template file as well as the `GenerationMethod` property of the `DemoParticle` class and enter the name of the initializer class:

```csharp
public class DemoParticle : ParticleAlgorithm
{
    ...
    public static new string GenerationMethod => typeof(DemoParticleInitializer).FullName;
    ...
}

public class DemoParticleInitializer : InitializationMethod
{
    public DemoParticleInitializer(ParticleSystem system) : base(system) { }

    // This method implements the system generation
    // Its parameters will be shown in the UI and they must have default values
    public void Generate(/* Parameters with default values */)
    {
        // The parameters of the Init() method can be set as particle attributes here
    }
}
```

If we save the file and run the simulator now, the Initialization Panel will not display any parameters and no particles will be placed when our Demo Algorithm is selected:
![Init Mode after uncommenting the initialization method](~/images/demo_init_empty.png "Init Mode after uncommenting the initialization method")

### Placing the Particles

Next, we will add an initialization parameter for the number of particles and actually place the particles.
The parameter can simply be added as a parameter of the `Generate` method.
It only needs to have a default value so that the system can already be initialized when the algorithm is selected.
Thus, we simply add the parameter `int numParticles = 10`.

To place the particles, we could use the [`AddParticle`][1] method, which is available inside the initializer class and allows us to place single particles at arbitrary positions.
However, we also have access to the [`PlaceParallelogram`][2] method, which simplifies our task: A line of $n$ particles is a parallelogram of length $n$ and height $1$.
This method requires the global start position, the lengthwise direction and the length of the parallelogram as parameters.
As start position, we choose the origin, which has the coordinates $(0, 0)$ and is represented as a `Vector2Int`, Unity's data type for two-dimensional integer vectors.
The zero vector even has a shorthand, accessed by `Vector2Int.zero`.
The lengthwise direction of the parallelogram must be the direction in which we want to place our line, which is the East direction, represented by [`Direction.E`][3].
Finally, the length of the parallelogram should be the length of the line, i.e., the number of particles we just added as a parameter.
All other parameters can be left at their default values because the default height is $1$ and the chirality and compass orientation are aligned with the global chirality and compass by default.

The `Generate` method now looks as follows:

```csharp
public void Generate(int numParticles = 10)
{
    PlaceParallelogram(Vector2Int.zero, Direction.E, numParticles);
}
```

With these changes, the Initialization Panel now displays the `numParticles` parameter and we can control how many particles are placed:
![Init Mode with new parameter](~/images/demo_init_param.png "Init Mode with custom parameter and particle placement")

### Electing a Leader

Currently, all particles placed by the generation method are identical.
In order to turn one particle into the leader, we first need to add a state attribute telling a particle whether it is the leader, and then we have to initialize this attribute differently for one particle than for the others.

We will use a Boolean value to mark the leader.
To do this, we add a new particle attribute of type `bool`, give it a display name and initialize it to `false`:
```csharp
public class DemoParticle : ParticleAlgorithm
{
    ...
    public ParticleAttribute<bool> isLeader;
    ...
    public DemoParticle(Particle p) : base(p)
    {
        isLeader = CreateAttributeBool("Is Leader", false);

        SetMainColor(ColorData.Particle_Blue);
    }
    ...
}
```
Now, every particle has a `bool` attribute with the initial value `false`.
The attribute is `public` so that a particle can check whether its neighbor is the leader or not (this is not necessary for the algorithm though).
It will also be displayed in the Particle Panel during the simulation, where its value can be edited and displayed for all particles simultaneously.
To learn more about attributes, please refer to the [Particle Attribute reference page](~/model_ref/attrs.md).
Note that we also set the color of the particle to [`ColorData.Particle_Blue`][5].
This is optional, but it makes the algorithm more interesting to look at and colors can be very useful for visualizing different states or roles of the particles.
The [`ColorData`][4] class contains several standard colors for particles.

Next, we need to determine a leader and initialize its `isLeader` attribute to `true`.
This is where the `Init` method of the particle class becomes relevant.
It allows us to change attribute values based on parameters that can be passed to individual particles by the generation method or even manually through the UI.

First, we uncomment the `Init` method and add a `bool` parameter called `leader` with a default value of `false`.
Inside the `Init` method, we set the `isLeader` attribute to `true` if the `leader` parameter is `true`:
```csharp
public void Init(bool leader = false)
{
    if (leader)
    {
        isLeader.SetValue(true);
        SetMainColor(ColorData.Particle_Yellow);
    }
}
```
We also change the leader particle's color so that it can be distinguished from the other particles visually.

By adding the `leader` parameter to the `Init` method, we have implicitly created an attribute for the [`InitializationParticles`][6] placed by the generation method.
In the Initialization Mode, all particles are placed as [`InitializationParticles`][6], which can be moved, deleted and edited freely.
When the "Start" button is pressed, the [`InitializationParticles`][6] are turned into proper particles and their attributes are passed as parameters to the `Init` method of those particles.
Thus, we could now manually determine a leader by selecting one of the particles in the Init Mode, setting its `leader` attribute to `true` and pressing "Start":
![Selecting a leader manually](~/images/demo_init_manual_leader.png "Selecting a leader manually in Init Mode")
![Selecting a leader manually](~/images/demo_init_manual_leader_2.png "Selecting a leader manually in Init Mode")

However, we want this to be done automatically by the generation method.
For this, we first need to find a random particle after placing the line in the `Generate` method of the initialization class.
The [`GetParticles`][7] method returns an array containing all [`InitializationParticles`][6] that have been placed so far.
We can use the length of this array and Unity's `Random.Range(int min, int maxExclusive)` method to get a random particle.
Having selected a particle, we can set its `leader` attribute by calling its [`SetAttribute`][8] method:
```csharp
public void Generate(int numParticles = 10)
{
    PlaceParallelogram(Vector2Int.zero, Direction.E, numParticles);

    InitializationParticle[] particles = GetParticles();
    if (particles.Length > 0)
    {
        int randIdx = Random.Range(0, particles.Length);
        particles[randIdx].SetAttribute("leader", true);
    }
}
```
The [`SetAttribute`][8] method gets the name of the `Init` method's parameter and its value.
Note that it is *not* the name of the final particle's attribute (`isLeader`)!

If we save the file and run the simulator now, the generation method will automatically determine a random leader each time we generate the system by clicking "Generate".
The selected leader particle knows that it is the leader because its `isLeader` attribute is set to `true`.

## Implementing the Algorithm Behavior

TODO


[1]: xref:Global.InitializationMethod.AddParticle(Vector2Int,Direction,Initialization.Chirality,Initialization.Compass)
[2]: xref:Global.InitializationMethod.PlaceParallelogram(Vector2Int,Direction,System.Int32,System.Boolean,System.Int32,Initialization.Chirality,Initialization.Compass)
[3]: xref:Global.Direction.E
[4]: xref:Global.ColorData
[5]: xref:Global.ColorData.Particle_Blue
[6]: xref:Global.InitializationParticle
[7]: xref:Global.InitializationMethod.GetParticles
[8]: xref:Global.InitializationParticle.SetAttribute(System.String,System.Object)
