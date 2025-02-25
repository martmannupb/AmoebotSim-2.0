# Developer Guide: Save and Load

Being able to save a simulation state and run it later with an updated algorithm can be a very useful feature for developing amoebot algorithms.
AmoebotSim 2.0 provides two different ways of saving and loading states:
In Initialization Mode, you can save the current system configuration, including all particle parameters, and load any saved configuration to easily reuse it for multiple simulation runs.
In Simulation Mode, you can even store the entire simulation history up to the current round, allowing you to review and replay scenarios in which problems occurred, for example.
This page explains how the Save and Load feature is implemented.


## Format

The basic idea of the Save and Load feature is to use [Unity's built-in JSON utility](https://docs.unity3d.com/2020.3/Documentation/Manual/JSONSerialization.html), which allows *serializable* objects to be easily translated to and from the textual JSON format.
We can store the JSON content in simple text files which the user can select through a file browser.

The main challenge of implementing this feature is representing the simulation and initialization state in a serializable format.
To this end, we define additional classes that "mirror" the original classes by storing their relevant data while being simple enough that they can be serialized.

![!Serialization](~/images/serialization_idea.png "Translation to and from JSON")


## Implementation

A type is *serializable* by Unity's JSON utility if it is a primitive type or if its members are serializable or lists/arrays of serializable types.
We mark a type as serializable by giving it the `Serializable` attribute:

```csharp
[Serializable]
public class MySerializableType {
    public int member1;
    public float member2;
    public bool[] member3;
    public MyOtherSerializableType member4;
    ...
}
```

The JSON utility recursively translates a serializable type into text.
This does not work for recursive data types, so we need to avoid using such types.
See [this page](https://docs.unity3d.com/Manual/script-Serialization.html) for all serialization rules.

Making the system classes that represent the simulation and initialization states serializable is not possible because their data is too complex.
It is also a bad idea because they often store the same data in multiple ways for optimized access, making the save data unnecessarily large.
Thus, we define new classes that store the data in a simplified and serializable format instead.

Every class whose state should be saved in a file defines a `GenerateSaveData()` and an `InitializeFromSaveData(...)` method (or similar).
The first method returns a serializable representation of the object's state and the second method updates the object's state to match the data in the given serializable object.
For example, the [`Particle`][1] class uses the [`ParticleStateSaveData`][2] class as its serializable representation, working with the [`GenerateSaveData`][3] and [`CreateFromSaveState`][4] methods.

Because serializable types can be nested, as explained above, we can define the serializable mirror classes as a hierarchy similar to that of the original classes.
At the top of this hierarchy are the [`SimulationStateSaveData`][5] and the [`InitializationStateSaveData`][6] classes, representing the Simulation Mode state and the Initialization Mode state, respectively.
When the `GenerateSaveData()` method is called on one of the original classes, it forwards the call to its subordinate classes to construct its own mirror object using the mirror objects of its constituents.
For example, every object that stores a history uses the [`ValueHistory`][7] class or one of its derivatives, which in turn uses the [`ValueHistorySaveData`][8] class (or one of its derivatives) as its serializable representation.

One notable combination of two features is the [`PinConfigurationSaveData`][9] class.
Because it is a very compact and simple representation of the complex pin configuration data, it is also used by the [`ValueHistoryPinConfiguration`][10] class, which uses the small representation internally to minimize the memory usage and bookkeeping effort of the history.

Finally, to select the files for storing and loading the save data, the [`FileBrowser`][11] class defines several methods for choosing files of a specific type.
Although all save data is stored in the textual JSON format, we use different file extensions to differentiate between the types:
`*.amalgo` files store simulation state data and `*.aminit` files store initialization state data.





[1]: xref:AS2.Sim.Particle
[2]: xref:AS2.ParticleStateSaveData
[3]: xref:AS2.Sim.Particle.GenerateSaveData
[4]: xref:AS2.Sim.Particle.CreateFromSaveState(AS2.Sim.ParticleSystem,AS2.ParticleStateSaveData)
[5]: xref:AS2.SimulationStateSaveData
[6]: xref:AS2.InitializationStateSaveData
[7]: xref:AS2.Sim.ValueHistory`1
[8]: xref:AS2.ValueHistorySaveData`1
[9]: xref:AS2.PinConfigurationSaveData
[10]: xref:AS2.Sim.ValueHistoryPinConfiguration
[11]: xref:AS2.FileBrowser
