# Subroutines

Algorithms for the amoebot model often use other algorithms as primitives, such as leader election, boundary detection, binary operations etc.
In the simulator, this can be accomplished using *subroutines*.
A subroutine can be viewed as a black-box that contains state attributes and activation logic but provides a simplified interface for the algorithms using it.
An algorithm can instantiate subroutine objects and activate them in its own activation methods, allowing the subroutines to access and modify the amoebot's pin configuration, update their own states and even schedule movements.
You can use subroutines to improve the reusability of your algorithms and to organize larger algorithms into more manageable parts.



## Using Subroutines

The simulator already provides some basic subroutines, among them several variants of a [leader election algorithm][1].
We will use a simple leader election subroutine to demonstrate how subroutines are used.

First, we [create a new algorithm](~/user_guide/dev/creation.md) called `SubLE` that only uses a single pin.
The initial setup of the algorithm class looks as follows:

```csharp
using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.LeaderElection;

namespace AS2.Algos.SubLE
{
    public class SubLEParticle : ParticleAlgorithm
    {
        // This is the display name of the algorithm (must be unique)
        public static new string Name => "SubLE";

        // Specify the number of pins (may be 0)
        public override int PinsPerEdge => 1;

        // The subroutine instance
        SubLeaderElection sle;

        ParticleAttribute<bool> firstRound;
        ParticleAttribute<bool> finished;

        public SubLEParticle(Particle p) : base(p)
        {
            firstRound = CreateAttributeBool("First round", true);
            finished = CreateAttributeBool("Finished", false);
            sle = new SubLeaderElection(p);
        }

        // Implement this method if the algorithm terminates at some point
        public override bool IsFinished()
        {
            return finished;
        }
        ...
    }
} // namespace AS2.Algos.SubLE
```

To access the subroutine class, we include the corresponding namespace with `using AS2.Subroutines.LeaderElection;`.
We declare an instance of the [`SubLeaderElection`][1] class as a member variable.
This is an exception from the rule that all member variables of an algorithm should be [attributes](attrs.md).
In the constructor of the algorithm class, we simply call the constructor of the subroutine class and provide a reference to the [`Particle p`][2].
Since this will create additional attributes, it is best practice to place subroutine initializations *after* all attribute initializations.
We add a `bool` attribute `firstRound`, initialized to `true`, because the subroutine has to be set up once, and a second `bool` attribute `finished` to terminate when the subroutine is finished.

The documentation of the [`SubLeaderElection`][1] class explains how the subroutine should be used:
We first have to create a circuit that spans the set of amoebots on which the leader election should be executed.
To start the subroutine, we then call its [`Init(int partitionSet, bool controlColor, int kappa, bool startAsCandidate)`][3] method, providing the ID of the partition set belonging to the circuit as `partitionSet`.
Optionally, we can allow the subroutine to control the amoebot's color, specify the number of repetitions `kappa`, and exclude some amoebots from being candidates.
After doing this, we can start running the subroutine.
In each beep activation, we have to call [`ActivateSend()`][4], which will cause the subroutine to update its state and decide whether it sends a beep on the given partition set.
In the following activation, we call [`ActivateReceive()`][5] to process the beeps that were sent.
This particular subroutine even allows us to call [`ActivateReceive()`][5] in the same round as the initialization, before any beeps were sent.
For some subroutines, this might result in unexpected behavior.
Finally, to test whether the subroutine is finished, we call its [`IsFinished()`][6] method after the [`ActivateReceive()`][5] call.
As soon as this returns `true`, we can stop running the subroutine and one amoebot on the circuit we created at the beginning will be marked as the leader (with high probability), indicated by [`IsLeader()`][7] returning `true`.

The resulting beep activation method looks as follows:

```csharp
public class SubLEParticle : ParticleAlgorithm
{
    ...
    public override void ActivateBeep()
    {
        if (firstRound)
        {
            // Setup leader election circuit, using partition set 0
            GetNextPinConfiguration().SetToGlobal(0);
            // Initialize the subroutine
            // - 0:    communicate using partition set 0
            // - true: control the amoebot's color
            // - 3:    repeat 3 times to reduce failure probability
            // - true: every amoebot starts as a leader candidate
            sle.Init(0, true, 3, true);
            firstRound.SetValue(false);
        }

        // Let the subroutine receive beeps (even works before beeps were sent)
        sle.ActivateReceive();
        if (sle.IsFinished())
        {
            if (sle.IsLeader())
                Log.Debug("I am the leader!");
            // Terminate
            finished.SetValue(true);
            return;
        }

        // Let the subroutine send beeps
        sle.ActivateSend();
    }
}
```



## Subroutine Interface

The leader election example demonstrates the basic API structure that is used by the included subroutines and that can serve as a guideline for implementing custom subroutines.
In general, the API can be constructed as follows:

#### `Init` Method

An initialization method resets the subroutine object's state and sets all required parameters.
This allows the subroutine to be very flexible since the parameters may be used for adapting it to various situations.
It also makes the subroutine reusable because it can be reinitialized arbitrarily often.

#### Separate `ActivateReceive` and `ActivateSend`/`ActivateMove` Methods

The abstract [`Subroutine`][8] class declares an [`ActivateBeep`][9] and an [`ActivateMove`][10] method, corresponding to the activation methods of the [`ParticleAlgorithm`][11] class.
For very simple subroutines, it may be sufficient to override these methods and call them directly in the activation methods of the algorithm.
However, it can be more convenient to split the activation methods into separate parts, one that only receives beeps and messages, and one that performs some actions like modifying the pin configuration and sending beeps or performing a movement.

The advantage of such a separation is that the execution of the subroutine can be *paused*:
After calling the `ActivateReceive` method, the subroutine already knows what has to be done in the next `ActivateSend` call.
This call can then be delayed as long as desired so that the algorithm running the subroutine is free to do something else in the next rounds.
On the implementation side, this may however be more challenging and require more state attributes because the subroutine has to store intermediate information that may otherwise be used directly (e.g., the pin configuration to be set up and the beeps to send are often determined directly from the received beeps and do not need to be stored).
The decision for one of these approaches should be made based on how the subroutine will likely be used.

#### Information Retrieval

Most subroutines will produce some sort of result and should at least provide a way of finding out when the procedure has finished.
Rather than exposing the internal state attributes, the subroutine can provide methods to observe its state and results, like the [`IsFinished`][6] and [`IsLeader`][7] methods of the [`SubLeaderElection`][1] subroutine.



## Implementing Subroutines

To implement a custom subroutine, simply add a new C# script to the `Assets/Code/Subroutines` folder (right click > Create > Scripting > Empty C# Script) and create a class inheriting from [`Subroutine`][8] in a new namespace `AS2.Subroutines.<YourSubroutine>`.
The class must have a constructor that accepts a [`Particle`][2] parameter and passes it to its base constructor.
The following code can serve as a template:

```csharp
using UnityEngine;
using AS2.Sim;

namespace AS2.Subroutines.MySubroutine
{
    public class MySubroutine : Subroutine
    {
        public MySubroutine(Particle p) : base(p)
        {
            // ...
        }
    }
} // namespace AS2.Subroutines.MySubroutine

```


### State Attributes

Similar to [`ParticleAlgorithm`s][11], the state of a subroutine is defined by [`ParticleAttribute`s](attrs.md).
They are declared in the class body and must be instantiated in the constructor, as usual.
The [`Subroutine`][8] class provides a reference to the calling algorithm through the [`algo`][12] member variable, through which the [`CreateAttribute<Type>`][13] methods can be accessed.
A problem that can occur when creating the attributes are naming conflicts:
If the algorithm has already created (or will create) an attribute of the same display name as one of the subroutine's attributes, the simulator will throw an exception, since all attributes of a subroutine are registered in the calling algorithm.
Since this would make it difficult to use multiple subroutine instances of the same type, the [`Subroutine`][8] class provides a helper method called [`FindValidAttributeName(string name)`][14].
You can pass the desired attribute name to this method and it will return a name that is not taken yet by appending a number.
Additionally, you can use a prefix in attribute names that associates the attributes with your subroutine.
Here are some examples:

```csharp
public class MySubroutine : Subroutine
{
    ParticleAttribute<int> roundCounter;
    ParticleAttribute<int> partitionSetID;
    ParticleAttribute<bool> controlColor;
    ParticleAttribute<bool> finished;

    public MySubroutine(Particle p) : base(p)
    {
        roundCounter = algo.CreateAttributeInt(
            FindValidAttributeName("[MySR] Round counter"), 0);
        partitionSetID = algo.CreateAttributeInt(
            FindValidAttributeName("[MySR] Partition set ID"), 0);
        controlColor = algo.CreateAttributeBool(
            FindValidAttributeName("[MySR] Control color"), false);
        finished = algo.CreateAttributeBool(
            FindValidAttributeName("[MySR] Finished"), false);
    }
}
```
We use the prefix `[MySR]` to indicate that these attributes belong to the `MySubroutine` subroutine and call [`FindValidAttributeName`][14] to avoid naming conflicts.

The attributes should be initialized in an appropriate `Init` method that can be used to start (and restart) the subroutine:

```csharp
public class MySubroutine : Subroutine
{
    ...
    public void Init(int partitionSetID, bool controlColor = false)
    {
        this.partitionSetID.SetValue(partitionSetID);
        this.controlColor.SetValue(controlColor);
        this.roundCounter.SetValue(0);
        this.finished.SetValue(false);
    }
}
```


### Activation Methods

As explained above, a subroutine can implement the [`ActivateBeep`][9] and [`ActivateMove`][10] methods or split them into receiving and sending/acting parts, or even do both, to define its own API.
In these methods, the [`algo`][12] reference provides access to all [`ParticleAlgorithm`][11] functionality required to implement the behavior of the subroutine.
Special attention should be paid to how the state attributes of the subroutine instance are read.
It will often be the case that the `Init` method and some of the activation methods are called in the same round.
Thus, to ensure that only the latest values of the attributes are used, the [`GetCurrentValue`][15] method should be used to read all attributes by default:

```csharp
public class MySubroutine : Subroutine
{
    ...
    public void ActivateReceive()
    {
        if (algo.ReceivedBeepOnPartitionSet(partitionSetID.GetCurrentValue()))
        {
            finished.SetValue(true);
            if (controlColor.GetCurrentValue())
            {
                algo.SetMainColor(ColorData.Particle_Green);
            }
        }
        else
        {
            roundCounter.SetValue(1);
        }
    }

    public void ActivateSend()
    {
        if (!finished.GetCurrentValue() && roundCounter.GetCurrentValue() > 0)
        {
            algo.SendBeepOnPartitionSet(partitionSetID.GetCurrentValue());
        }
    }
}
```


### Nesting and Sharing Subroutines

Subroutines can be used by other subroutines, enabling complex algorithms that are composed of a hierarchy of primitives.
The principle is the same as when using subroutines in regular algorithms:
Declare a member variable, initialize it in the calling subroutine's constructor, and then call its API methods as required:

```csharp
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.LeaderElection;

namespace AS2.Subroutines.MySubroutine
{
    public class MySubroutine : Subroutine
    {
    	SubLeaderElection nestedLE;

        public MySubroutine(Particle p) : base(p)
        {
            nestedLE = new SubLeaderElection(p);
        }

        public void Init(...)
        {
            nestedLE.Init(...);
        }

        public void ActivateReceive()
        {
            nestedLE.ActivateReceive();
            ...
        }
    }
} // namespace AS2.Subroutines.MySubroutine
```

Now, consider a situation where an algorithm requires subroutines `A` and `B`, but subroutine `B` internally also uses subroutine `A`.
If we implement subroutine `B` as shown above, the resulting algorithm will contain two instances of subroutine `A` in total.
If the algorithm uses its subroutines `A` and `B` simultaneously, this cannot be avoided.
However, if it only uses one of the subroutines at a time, it can *share* its subroutine `A` with `B`, reducing the number of required instances.
This is very easy to implement:
We simply add an optional parameter to subroutine `B`'s constructor that allows the calling algorithm to provide an instance of `A`.

```csharp
using UnityEngine;
using AS2.Sim;
using AS2.Subroutines.SubA;

namespace AS2.Subroutines.SubB
{
    public class SubB : Subroutine
    {
    	SubA nestedSubA;

        public SubB(Particle p, SubA sharedSubA = null) : base(p)
        {
            if (sharedSubA is null)
                nestedSubA = new SubA(p);
            else
                nestedSubA = sharedSubA;
        }
        ...
    }
} // namespace AS2.Subroutines.SubB
```

In the calling algorithm, we first instantiate subroutine `A` and then pass the instance to subroutine `B`'s constructor:

```csharp
...
using AS2.Subroutines.SubA;
using AS2.Subroutines.SubB;

namespace AS2.Algos.MyAlgo
{
    public class MyAlgoParticle : ParticleAlgorithm
    {
        ...
        SubA subroutineA;
        SubB subroutineB;

        public MyAlgoParticle(Particle p) : base(p)
        {
            subroutineA = new SubA(p);
            subroutineB = new SubB(p, subroutineA);
        }
        ...
    }
} // namespace AS2.Algos.MyAlgo
```


### Optimizing Attribute Usage

When using subroutines, the number of state attributes belonging to an algorithm can grow very quickly, which can clutter the Particle Panel and have an impact on the simulation speed and the size of save files.
*If this becomes a problem*, there are two main approaches to reduce the number of attributes.

#### Sharing State Attributes

State attributes can be shared with subroutines just like subroutine instances themselves.
For example, if an algorithm has an integer attribute `counter` and a subroutine `A` that also uses an integer attribute such that `counter` and `A` are never used at the same time, we can use the same technique as shown above to share the attribute between the two:

```csharp
// In the subroutine file
public class SubA : Subroutine
{
	ParticleAttribute<int> subCounter;

    public SubA(Particle p, ParticleAttribute<int> sharedCounter = null) : base(p)
    {
        if (sharedCounter is null)
            subCounter = algo.CreateAttributeInt(
                FindValidAttributeName("[A] Counter"), 0);
        else
            subCounter = sharedCounter;
    }
    ...
}

// In the algorithm file
public class MyAlgoParticle : ParticleAlgorithm
{
    ...
    ParticleAttribute<int> counter;
    SubA subroutineA;

    public MyAlgoParticle(Particle p) : base(p)
    {
        counter = CreateAttributeInt("Counter", 0);
        subroutineA = new SubA(p, counter);
    }
}
```

After an execution of `A` has finished, the `counter` attribute can be used to store data again.
This same approach can also be used to share attributes between subroutines.
For example, if both subroutines `A` and `B` require an `int` attribute, an algorithm using both `A` and `B` can declare its own attribute and share it with both, as long as it never uses `A` and `B` at the same time.
This even reduces the total number of required attributes if the algorithm itself never uses this shared attribute.
In some cases, this can already help reducing the memory footprint significantly.

#### Binary Encoded Attributes

A much more drastic way of reducing the number of attributes is to encode multiple values within a single integer attribute.
In C#, the standard `int` type (or rather, [`System.Int32`][16]) is a 32-bit signed integer.
Thus, one integer can store up to 32 `bool` values at the same time, storing one value in each bit.
Since there are only 6 cardinal [`Direction`s][17] (plus the [`None`][18] direction), 3 bits are sufficient to encode one direction value.
Furthermore, most algorithms will never use very large integer values, so if the maximum value of an integer attribute is known beforehand, it can be encoded with a smaller number of bits.

To make the handling of such encodings as convenient as possible, the [`AS2.Subroutines.BinStateHelpers`][19] namespace defines the generic [`BinAttribute<T>`][20] class and several specializations for `bool`s, [`Direction`s][17], smaller `int`s, bit fields, and even custom `enum`s.
To use this system, you only need a [`ParticleAttribute<int>`][21] in which the data will be encoded.
The actual state attributes are then represented by specializations of the [`BinAttribute<T>`][20] class:

```csharp
using AS2.Sim;
using UnityEngine;
using static AS2.Constants;
using AS2.Subroutines.BinStateHelpers;

namespace AS2.Algos.MyAlgo
{
    public class MyAlgoParticle : ParticleAlgorithm
    {
        ...
        // Encodes 3 bools, an 8-bit integer and 6 directions
        // 0  1  2   3     10   11  14  17  20  23  26    29
        // x  x  x   xxxxxxxx   xxx xxx xxx xxx xxx xxx   xxx
        // B0 B1 B2  INT8       D0  D1  D2  D3  D4  D5    ---
        ParticleAttribute<int> state;

        // The wrappers of the binary encoded attributes
        BinAttributeBool bool1;
        BinAttributeBool bool2;
        BinAttributeBool bool3;
        BinAttributeInt counter;    // Can only count from 0 to 255
        BinAttributeDirection[] dirs = new BinAttributeDirection[6];

        public MyAlgoParticle(Particle p) : base(p)
        {
            state = CreateAttributeInt("State", 0);

            // Instantiate encoded attributes by assigning bit indices
            bool1 = new BinAttributeBool(state, 0);
            bool2 = new BinAttributeBool(state, 1);
            bool3 = new BinAttributeBool(state, 2);
            counter = new BinAttributeInt(state, 3, 8);
            for (int i = 0; i < 6; i++)
            {
            	// 11, 14, 17, 20, 23, 26
                dirs[i] = new BinAttributeDirection(state, 11 + 3 * i);
            }
        }
        ...
    }
} // namespace AS2.Algos.MyAlgo
```

In the example above, we use a single integer attribute `state` to encode 3 `bool`s, an 8-bit integer and 6 [`Direction`s][17], reducing the number of attributes used by the algorithm from 10 to 1.
The encoded attributes are represented by subclasses of the [`BinAttribute<T>`][20] class.
When they are instantiated in the constructor, we specify the `state` attribute in which they should be stored and the *bit index*, i.e., the position within the 32-bit integer at which the value should start.
Each `bool` requires one bit and each [`Direction`][17] requires 3 bits.
For types of variable size, we additionally specify the bit width (see [`BinAttributeInt`][22]).
It is very helpful to use an overview comment as shown above the `state` declaration to find out which bit indices are required.

The [`BinAttribute<T>`][20] subclasses provide an interface very similar to that of regular [`ParticleAttribute<T>`s][21].
They can be read using [`GetValue`][23] and [`GetCurrentValue`][24] methods and new values can be assigned using [`SetValue`][25].
Because writing to any of the encoded attributes changes the value of the underlying `state` integer, it is best practice to always call [`GetCurrentValue`][24].
See [`SubBoundaryTest`][26] for an example of both attribute encoding and subroutine sharing.

Although this is a very effective way of reducing the number of attributes, it has some drawbacks.
It is more difficult to set up than regular attributes and can make it very tedious to add and remove attributes later.
Additionally, the encoded attributes are not displayed in the Particle Panel, making it practically impossible to track or edit their values during the simulation.
And finally, while this method significantly reduces the memory requirements of the simulation and especially the save files, it may only have a small effect on the simulation performance.
Therefore, this should only be used if the memory footprint is critical or the simulation speed (especially while scrubbing through the history) is far too low.



[1]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection
[2]: xref:AS2.Sim.Particle
[3]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection.Init(System.Int32,System.Boolean,System.Int32,System.Boolean)
[4]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection.ActivateSend
[5]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection.ActivateReceive
[6]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection.IsFinished
[7]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection.IsLeader
[8]: xref:AS2.Subroutines.Subroutine
[9]: xref:AS2.Subroutines.Subroutine.ActivateBeep
[10]: xref:AS2.Subroutines.Subroutine.ActivateMove
[11]: xref:AS2.Sim.ParticleAlgorithm
[12]: xref:AS2.Subroutines.Subroutine.algo
[13]: xref:AS2.Sim.ParticleAlgorithm.CreateAttributeInt(System.String,System.Int32)
[14]: xref:AS2.Subroutines.Subroutine.FindValidAttributeName(System.String)
[15]: xref:AS2.Sim.ParticleAttribute`1.GetCurrentValue
[16]: https://learn.microsoft.com/en-us/dotnet/api/system.int32?view=net-8.0
[17]: xref:AS2.Direction
[18]: xref:AS2.Direction.NONE
[19]: xref:AS2.Subroutines.BinStateHelpers
[20]: xref:AS2.Subroutines.BinStateHelpers.BinAttribute`1
[21]: xref:AS2.Sim.ParticleAttribute`1
[22]: xref:AS2.Subroutines.BinStateHelpers.BinAttributeInt
[23]: xref:AS2.Subroutines.BinStateHelpers.BinAttribute`1.GetValue
[24]: xref:AS2.Subroutines.BinStateHelpers.BinAttribute`1.GetCurrentValue
[25]: xref:AS2.Subroutines.BinStateHelpers.BinAttribute`1.SetValue(`0)
[26]: xref:AS2.Subroutines.BoundaryTest.SubBoundaryTest
