# Model Reference: General Information

This page contains some general hints and other things to keep in mind while developing algorithms with AmoebotSim 2.0.
Some of the topics mentioned on this page are explained in more detail on the other reference pages.


### Programming Amoebot Behavior

The most important thing to keep in mind when writing Amoebot algorithm code is that you are programming the behavior of *a single particle*.
Every particle in the system will run its own, independent copy of the algorithm.
To make different particles behave differently, you need to find some distinction allowing the particles to determine which way they should behave.
This distinction can be the locations and states of the particle's neighbors, received beeps or messages, a random coin toss, attribute values passed as parameters at initialization time, or any combination of these.
After such a distinction was made, the particles can transition to different states (by changing their attribute values) to maintain a distinction and perform different actions over multiple rounds.
It is important to keep track of which particles will run which code sections and to try covering all possible cases that could lead to particles not behaving as they should.

While developing an algorithm and especially when debugging, it is helpful to consider single particles in isolation and determine what information is available to them from their local view.
If the particles can have different compass directions and chiralities, there can be drastic differences between the local perceptions of two particles viewing the same situation.

It is often necessary to synchronize the particles, for example to coordinate operations that take several rounds to complete.
The easiest way to implement such a synchronization is to use an integer attribute as a round counter.
This counter can be incremented at the end of each activation, until it wraps around to $0$ after a predetermined number of rounds.
Depending on the value of the counter, different code can be executed, leading to different behavior in each round.
Because all particles are activated in each round, every particle will have the same counter value, so their actions are synchronized.


### Head, Tail and Directions

Some aspects of the simulator that might be difficult to get used to are the orientation of expanded particles and the way neighbor positions are identified.
For an expanded particle, one of its two occupied nodes is called the *head* and the other one is called the *tail*.
When a particle expands in some direction $d$, the node that lies in direction $d$ will be occupied by the head while the node that was already occupied becomes the tail.
After that, $d$ is called the *head direction* or *expansion direction* of the particle.
Note that there might be a difference between the *global* and the *local* head direction, depending on the particle's compass direction.

It is important to keep track of the orientation of an expanded particle because its two parts have different neighbors and are also perceived differently by their neighboring particles.
In addition to that, the distinction between head and tail is crucial for identifying neighbor positions and pins.
While a simple direction is enough to identify a neighbor position of a contracted particle, expanded particles additionally require the information whether the neighbor is at the particle's head or its tail.
This system is used wherever neighbor positions are required, i.e., looking for neighbor particles, marking and releasing bonds, scheduling movements, and identifying pins.
It is therefore important to get comfortable with this concept to prevent unnecessary problems while implementing Amoebot algorithms.


### API Documentation

All of the API code of AmoebotSim 2.0 has extensive code documentation that explains how the classes and methods are used.
After opening the C# project in Visual Studio directly from the Unity Editor (by double-clicking a `.cs` file), the documentation should be available automatically.
It can be viewed by hovering over a class or method name and it should also appear in auto-completion suggestions.
If that is not the case, the API documentation can also be viewed on the [API documentation pages](~/api/index.md) or directly in the source code.
The online documentation pages in combination with the in-line API documentation should provide everything you need to develop and simulate complex algorithms using AmoebotSim 2.0.
