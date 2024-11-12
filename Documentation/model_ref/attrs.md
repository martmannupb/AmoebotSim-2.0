# Model Reference: Particle Attributes

As defined on the [Amoebot model pages](~/amoebot_model/home.md), Amoebot particles are finite state machines.
This means that the set of possible particle states is finite and the behavior of a particle is defined by transitions between these states.
While this is sufficient for the mathematical model, it is not convenient for developing and programming algorithms.



## State Representation

In the simulator, we define the *behavior* of a particle using methods in a C# class, as explained on the [Round Simulation reference page](rounds.md).
The *state* of a particle is defined using *particle attributes*.
An attribute has a unique name and a data type, and all particles running the same algorithm have the same, fixed set of attributes.
Thus, the state of a particle is simply the combination of its current attribute values, its [pin configuration](pin_cfgs.md) and its expansion state (technically, its color must also be counted).
Since the computation of a particle is based on the current state of the system, including the particle's own state, the code in the activation methods implicitly defines the state transitions by computing the next state.

![State represented by attributes](~/images/state_attributes.png "State represented by attributes")



## Implementation

Particle attributes are implemented as subclasses of the generic [`ParticleAttribute<T>`][1] class.
There are subclasses for the types `int`, `bool`, [`Direction`][2], `enum`, `float`, `string` and [`PinConfiguration`][3].
The subclass for `enum` is generic itself and allows you to have attributes for arbitrary `enum` types.
[`Direction`][2] is also an `enum` but it has its own attribute type.

As opposed to regular member variables, particle attributes provide several useful features specifically for the simulation.
To start with, all attributes except those of type [`PinConfiguration`][3] are displayed in the UI's Particle Panel, where their values can be viewed and edited during the simulation.
In addition to this, the history of each attribute is stored, such that all rounds of the simulation can be reviewed arbitrarily and the simulation can even be restarted from any round by restoring the previous particle states.
If the state of a particle depends on anything that is not part of the API (like regular member variables), this feature may not work anymore.
Note that editing attribute values is not possible while reviewing any round other than the latest as this would change the history (and we do not allow time travel).

Similarly, particle attributes enable the saving and loading of simulation states because they can be serialized.
This feature may also break if any non-API state representation is used since that part of the state will not be saved or loaded.

> [!WARNING]
> The state of a particle should be exclusively defined by attributes and other API fields like the particle's color or [subroutines](subroutines.md).
> Other means of state representation like simple member variables, static members etc. *should not be used* unless you know exactly what you are doing.

Finally, particle attributes provide special access functionality to ensure that particles can work with their own attributes easily while other particles cannot see the changed attribute values in the same round.
In the mathematical model, all particles are activated simultaneously and their computations are based on the system state at the beginning of the round.
In the simulator, the particles are activated sequentially, which means that the system state already changes before all particles have been activated.
The particle attributes ensure that reading another particle's attribute values always results in their *old* values from the beginning of the round, even if that particle has already changed its attributes internally.


### Using Particle Attributes

Particle attributes are member variables of type [`ParticleAttribute<T>`][1] that are initialized by one of the *attribute creation methods*.
There is one creation method for each type of particle attribute, e.g., [`CreateAttributeInt`][4].
Each creation method takes the unique name of the attribute and its initial value as parameters.
It is important that the declaration and initialization of an attribute are separated.
The initialization must take place in the constructor of the algorithm class:
```csharp
public class MyAlgoParticle : ParticleAlgorithm
{
    ...
    // Declare attributes here
    public ParticleAttribute<int> myIntAttr;

    public MyAlgoParticle(Particle p) : base(p)
    {
    	// Initialize attributes here
        myIntAttr = CreateAttributeInt("My Int Attribute", 42);
    }
    ...
}
```

The creation method for the `enum` type is generic so that attributes for arbitrary `enum` types can be created:
```csharp
public enum MyEnum { ONE, TWO, THREE }
...
public ParticleAttribute<MyEnum> myEnumAttr;
...
myEnumAttr = CreateAttributeEnum<MyEnum>("My Enum Attr", MyEnum.ONE);
```

Once initialized, the value of an attribute can be read like a regular variable: The attribute instance is implicitly converted to its value's type.
This will always return the attribute's value from the *beginning of the current round*.
The explicit way of reading this value is the [`GetValue`][5] method.
Whenever another particle reads the attribute, this value will be returned.

However, it may sometimes be convenient to update the value of an attribute and then read the updated value in the same round, i.e., in the same activation method.
To access the *latest* value of an attribute, the [`GetCurrentValue`][6] method must be used.
This method can only be called by the particle to which the attribute belongs.

To write a new attribute value, the [`SetValue`][7] method must be used.
The following example demonstrates reading and writing an integer attribute:
```csharp
public ParticleAttribute<int> myIntAttr;

public MyAlgoParticle(Particle p) : base(p)
{
    myIntAttr = CreateAttributeInt("My Int", 42);
}

public override void ActivateMove()
{
    // First activation
    int i0 = myIntAttr;                   // i0 = 42
    int i1 = myIntAttr.GetCurrentValue(); // i1 = 42 (Value not updated yet)
    myIntAttr.SetValue(17);               // Update attribute value
    int i2 = myIntAttr;                   // i2 = 42 (value from the beginning of the round)
    int i3 = myIntAttr.GetCurrentValue(); // i3 = 17 (updated value)

    // Read neighbor particle's attribute value
    MyAlgoParticle nbr = (MyAlgoParticle)GetNeighborAt(Direction.E); // Find a neighbor
    int i4 = nbr.myIntAttr; // i4 = 42 (value from the beginning of the round)
    int i5 = nbr.myIntAttr.GetCurrentValue(); // Not allowed! Leads to exception
}
```



[1]: xref:AS2.Sim.ParticleAttribute`1
[2]: xref:AS2.Direction
[3]: xref:AS2.Sim.PinConfiguration
[4]: xref:AS2.Sim.ParticleAlgorithm.CreateAttributeInt(System.String,System.Int32)
[5]: xref:AS2.Sim.ParticleAttribute`1.GetValue
[6]: xref:AS2.Sim.ParticleAttribute`1.GetCurrentValue
[7]: xref:AS2.Sim.ParticleAttribute`1.SetValue(`0)
