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

For this example, we want the Amoebots to communicate using circuits and to perform basic joint movements.
To keep it simple, we will assume common chirality and compass alignment.
The initial configuration of the system will be a line of particles parallel to the West-East axis.
The idea for the algorithm is the following:
The system starts with a single, arbitrary particle that is marked as the *leader*.
In each round, the leader randomly decides whether a movement should be performed or not.
If it decides to perform a movement, it will send a beep on the global circuit (which has to be established first).
All particles receiving a beep will perform a movement:
If they are contracted, they will expand in the East direction and if they are expanded, they will contract into their tail.
If some of these terms seem unfamiliar, you can refer to the [Model Reference pages](~/model_ref/home.md) for more information.

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

To start with, we uncomment the `DemoInitializer` class at the bottom of the template file as well as the `GenerationMethod` property of the `DemoParticle` class and enter the name of the initializer class:

```csharp
public class DemoParticle : ParticleAlgorithm
{
    ...
    // If the algorithm has a special generation method, specify its full name here
    public static new string GenerationMethod => typeof(DemoInitializer).FullName;
    ...
}

public class DemoInitializer : InitializationMethod
{
    public DemoInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

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
Now, every particle has a `bool` attribute called `isLeader` with the initial value `false`.
The attribute is `public` so that a particle can check whether its neighbor is the leader or not (this is not necessary for the algorithm though).
It will also be displayed as "Is Leader" in the Particle Panel during the simulation, where its value can be edited and displayed for all particles simultaneously.
To learn more about attributes, please refer to the [Particle Attribute reference page](~/model_ref/attrs.md).
Note that we also set the color of the particle to [`ColorData.Particle_Blue`][5].
This is optional, but it makes the algorithm more interesting to look at and colors can be very useful for visualizing different states or roles of the particles.
The [`ColorData`][4] class contains several standard colors for particles.

Next, we need to determine a leader and set its `isLeader` attribute to `true`.
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
        SetMainColor(ColorData.Particle_Green);
    }
}
```
We also change the leader particle's color so that it can be distinguished from the other particles visually.

By adding the `leader` parameter to the `Init` method, we have implicitly created an attribute for the [`InitializationParticles`][6] placed by the generation method.
In the Initialization Mode, all particles are placed as [`InitializationParticles`][6], which can be moved, deleted and edited freely.
When the "Start" button is pressed, the [`InitializationParticles`][6] are turned into proper particles and their attributes are passed as parameters to the `Init` method of those particles.
Thus, we can now manually determine a leader by selecting one of the particles in the Init Mode, setting its `leader` attribute to `true` and pressing "Start":
![Selecting a leader manually](~/images/demo_init_manual_leader.png "Selecting a leader manually in Init Mode")
![Selected leader during simulation](~/images/demo_init_manual_leader_2.png "Manually selected leader in Simulation Mode")

However, we want this to be done automatically by the initialization method.
For this, we first need to find a random particle from the line we placed in the `Generate` method of the initialization class.
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

Now that the system is initialized correctly, we can implement the particle activation methods to achieve the desired behavior.
This is what the algorithm should do:
- The leader should decide randomly whether to perform a movement
- If a movement shall be performed, the leader will send a beep on the global circuit (which has to be established first)
- Every particle will perform a movement if it receives a beep
	- Expansion to the East direction when contracted, contraction into the tail if expanded

We will extend the algorithm incrementally until this behavior is achieved.

### Deciding Randomly When to Move

We want the leader particle to decide randomly whether or not the system should move in each simulation round.
For now, we will simulate a coin toss for the random decision.
Unity's `Random.Range(float min, float max)` method returns a uniformly random value between `min` and `max`, which means that the probability of a value in the range $[0,1]$ being less than $0.5$ will be $0.5$.
There are other ways to simulate a coin toss, but this approach allows us to change the movement probability later.
Because the leader should send a beep when it decides to move, it makes sense to put this code into the beep activation method:
```csharp
public override void ActivateBeep()
{
    if (isLeader)  // Only the leader should run this code
    {
        if (Random.Range(0.0f, 1.0f) < 0.5f)
        {
            // Decided to move => Send a beep on the global circuit
        }
    }
}
```

### Setting Up the Communication

The beep has to be received by all particles in the system.
Before sending a beep, we need to create a circuit that connects the particles.
Due to the line structure of this system, we could set up a circuit that connects all particles along the line by putting the two pins in East and West direction into one partition set.
However, it is easier to simply put all pins into one partition set, because the other pins are not used for anything else anyway.
The [`PinConfiguration`][9] class has a [`SetToGlobal(int ps)`][10] method that will put all pins into the partition set with index `ps`.
Thus, setting up the circuit is as simple as calling this method in every beep activation:
```csharp
public override void ActivateBeep()
{
    PinConfiguration pc = GetCurrentPinConfiguration(); // Get a PinConfiguration instance
    pc.SetToGlobal(0);                                  // Collect all pins in partition set 0
    SetPlannedPinConfiguration(pc);                     // Commit to use this pin configuration

    ...
}
```
We use the partition set with ID $0$ to hold the pins.
Note that it may not be necessary to set up a new pin configuration in each round: If no movement is performed in a round, the particles keep their pin configurations and can reuse them in the next round.
However, there is no disadvantage in setting the pin configuration explicitly in each round.
You can read more about the pin configuration system on the [Pin Configuration reference page](~/model_ref/pin_cfgs.md).

Now, the leader can use its partition set $0$ to send a beep if it decides to move:
```csharp
public override void ActivateBeep()
{
    PinConfiguration pc = GetCurrentPinConfiguration(); // Get a PinConfiguration instance
    pc.SetToGlobal(0);                                  // Collect all pins in partition set 0
    SetPlannedPinConfiguration(pc);                     // Commit to use this pin configuration

    if (isLeader)  // Only the leader should run this code
    {
        if (Random.Range(0.0f, 1.0f) < 0.5f)
        {
            // Decided to move => Send a beep on the global circuit
            pc.SendBeepOnPartitionSet(0);
        }
    }
}
```

If we run the algorithm now, the particles will set up the global circuit and the leader will irregularly send a beep:

![The established global circuit](~/images/demo_circuit_setup.png "The established global circuit")
![Irregular beeps](~/images/demo_circuit_beep.png "Irregular beeps indicated by the white flashes")

The visual representation of the pin configurations clearly shows that all pins of each particle are contained in a single partition set and that all partition sets are connected in a global circuit.
When the leader sends a beep, its partition set is highlighted with a white dot and all connection lines of the circuit are flashing white.

### Performing the Movements

The algorithm is almost finished now, we only need to get the particles moving when they receive a beep.
As explained in the [Round Simulation reference](~/model_ref/rounds.md) and according to the circuit model, beeps and messages sent in the beep phase can only be received in the following movement phase (the only exception is that they can still be received in the next beep phase if the particle has not expanded or contracted in the move phase).
Thus, we have to check for received beeps in the movement activation method:
```csharp
public override void ActivateMove()
{
    PinConfiguration pc = GetCurrentPinConfiguration();
    if (pc.ReceivedBeepOnPartitionSet(0))
    {
        // Received a beep => Perform movement
    }
}
```
We can use partition set $0$ because we have set up this partition set to contain the pins in the previous beep phase.
In the first simulation round (recall that the simulation starts with a move phase), partition set $0$ contains only a single pin, but because no beeps have been sent at that point, no beeps can be received.
Note that we do not have to distinguish between the leader and the other particles because the leader will receive the beep on partition set $0$ just like any other particle on the circuit.

We want contracted particles to expand East and expanded particles to contract into their tail.
The expansion status of a particle can be checked with the [`IsExpanded`][11] and [`IsContracted`][12] methods.
Simple movements are performed using the [`Expand(Direction d)`][13] and [`ContractTail`][14] or [`ContractHead`][15] methods.
Using these methods to perform our desired movements is straightforward:
```csharp
public override void ActivateMove()
{
    PinConfiguration pc = GetCurrentPinConfiguration();
    if (pc.ReceivedBeepOnPartitionSet(0))
    {
        // Received a beep => Perform movement
        if (IsContracted())  // Expand East if contracted
            Expand(Direction.E);
        else                 // Contract into tail if expanded
            ContractTail();
    }
}
```
We do not have to care about bonds because there are no bonds that could hinder our movements.
In fact, releasing any of the existing bonds would break the connectivity of the system and lead to an error.
Please refer to the [Bonds and Joint Movements reference](~/model_ref/bonds_jm.md) for a more detailed explanation of bonds.

With the movements in place, our demo algorithm is complete!
Here is the final code:
```csharp
using AS2.Sim;
using UnityEngine;

namespace AS2.Algos.Demo
{

    public class DemoParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "Demo Algorithm";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // If the algorithm has a special generation method, specify its full name here
        public static new string GenerationMethod => typeof(DemoInitializer).FullName;

        // Declare attributes here
        public ParticleAttribute<bool> isLeader;

        public DemoParticle(Particle p) : base(p)
        {
            isLeader = CreateAttributeBool("Is Leader", false);

            SetMainColor(ColorData.Particle_Blue);
        }

        // Implement this if the particles require special initialization
        // The parameters will be converted to particle attributes for initialization
        public void Init(bool leader = false)
        {
            if (leader)
            {
                isLeader.SetValue(true);
                SetMainColor(ColorData.Particle_Green);
            }
        }

        // The movement activation method
        public override void ActivateMove()
        {
            PinConfiguration pc = GetCurrentPinConfiguration();
            if (pc.ReceivedBeepOnPartitionSet(0))
            {
                // Received a beep => Perform movement
                if (IsContracted())  // Expand East if contracted
                    Expand(Direction.E);
                else                 // Contract into tail if expanded
                    ContractTail();
            }
        }

        // The beep activation method
        public override void ActivateBeep()
        {
            PinConfiguration pc = GetCurrentPinConfiguration(); // Get a PinConfiguration instance
            pc.SetToGlobal(0);                                  // Collect all pins in partition set 0
            SetPlannedPinConfiguration(pc);                     // Commit to use this pin configuration

            if (isLeader)  // Only the leader should run this code
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    // Decided to move => Send a beep on the global circuit
                    pc.SendBeepOnPartitionSet(0);
                }
            }
        }
    }

    // Use this to implement a generation method for this algorithm
    // Its class name must be specified as the algorithm's GenerationMethod
    public class DemoInitializer : InitializationMethod
    {
        public DemoInitializer(AS2.Sim.ParticleSystem system) : base(system) { }

        // This method implements the system generation
        // Its parameters will be shown in the UI and they must have default values
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
    }

} // namespace AS2.Algos.Demo
```

## Next Steps

Congratulations!
If you completed this walkthrough successfully, you are now able to start developing your own Amoebot algorithms.
You can use the API documentation and the reference pages to learn more about AmoebotSim 2.0 and try out your own ideas.
However, there are several features of the simulation environment that have not been discussed in this guide and that might be useful, especially for more complex algorithms.
The [Advanced Features guide](advanced.md) demonstrates some of these features by extending the demo algorithm we developed in this walkthrough.



[1]: xref:AS2.InitializationMethod.AddParticle(Vector2Int,AS2.Direction,AS2.Initialization.Chirality,AS2.Initialization.Compass)
[2]: xref:AS2.InitializationMethod.PlaceParallelogram(Vector2Int,AS2.Direction,System.Int32,System.Boolean,System.Int32,AS2.Initialization.Chirality,AS2.Initialization.Compass)
[3]: xref:AS2.Direction.E
[4]: xref:AS2.ColorData
[5]: xref:AS2.ColorData.Particle_Blue
[6]: xref:AS2.Sim.InitializationParticle
[7]: xref:AS2.InitializationMethod.GetParticles
[8]: xref:AS2.Sim.InitializationParticle.SetAttribute(System.String,System.Object)
[9]: xref:AS2.Sim.PinConfiguration
[10]: xref:AS2.Sim.PinConfiguration.SetToGlobal(System.Int32)
[11]: xref:AS2.Sim.ParticleAlgorithm.IsExpanded
[12]: xref:AS2.Sim.ParticleAlgorithm.IsContracted
[13]: xref:AS2.Sim.ParticleAlgorithm.Expand(AS2.Direction)
[14]: xref:AS2.Sim.ParticleAlgorithm.ContractTail
[15]: xref:AS2.Sim.ParticleAlgorithm.ContractHead
