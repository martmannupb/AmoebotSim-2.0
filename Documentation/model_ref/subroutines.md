# Subroutines

Algorithms for the amoebot model often use other algorithms as primitives, such as leader election, boundary detection, binary operations etc.
In the simulator, this can be accomplished using *subroutines*.
A subroutine can be viewed as a black-box that contains attributes and activation logic but provides a simplified interface for the algorithms using it.
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

        public SubLEParticle(Particle p) : base(p)
        {
            firstRound = CreateAttributeBool("First round", true);
            sle = new SubLeaderElection(p);
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
We add a single `bool` attribute `firstRound` because the subroutine has to be set up once (this can be done in any round and even multiple times to reset the subroutine).

TODO

[1]: xref:AS2.Subroutines.LeaderElection.SubLeaderElection
[2]: xref:AS2.Sim.Particle
