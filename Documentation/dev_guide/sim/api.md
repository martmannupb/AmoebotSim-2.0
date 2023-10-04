# Developer Guide: User API

An integral part of the simulation environment is the user interface for programming particle algorithms.
This API provides all functionality necessary for controlling the particle behavior while preventing access to the simulator's internals as far as possible.
It also includes a system for writing custom particle system generation methods, allowing users to define precisely what the initial configuration should look like.

This page explains some technical details of how these two parts of the API are realized.



## Particle Algorithm API

The particle algorithm API is defined by the [`ParticleAlgorithm`][1] class.
It defines all properties and methods available to the algorithm developer by inheritance:
Algorithms are always defined as subclasses of [`ParticleAlgorithm`][1].
The class only has a single data member, which is a reference to the [`Particle`][2] controlled by the algorithm.
Since this member is private, it cannot be accessed by any subclass.
There is a 1:1 relation between the [`ParticleAlgorithm`][1] and the [`Particle`][2] class, i.e., instances of this class are always created in pairs and the link between them is never broken.
The [`Particle`][2] instance represents the particle in the system and stores its data while the [`ParticleAlgorithm`][1] instance defines its behavior.

All methods in the API are directly passed through to the [`Particle`][2] or the [`ParticleSystem`][3] class, where they are handled accordingly.
For most methods, it is first checked whether the particle is currently active (i.e., being activated) using its [`isActive`][4] flag.
This flag is set to `true` by the particle system before its activation method is called and reset to `false` afterwards.
With this system, it is prevented that a particle calls API methods on its neighbors, because only one particle is active at a time.
Similarly, the system's [`InMovePhase`][5] and [`InBeepPhase`][6] flags are used to ensure that API methods are called in the correct activation method, i.e., expansions and contractions should not be triggered in the beep activation.

Many of the method calls do not immediately take effect in the particle's state but only *schedule* an action that should be carried out *after* all particles have been activated.
For example, any movement scheduled by a particle must be delayed until all movement activations have been simulated because the particle's neighbors should not see the changed position until the next round.
This feature is explained in more detail on the [round simulation page](round_sim.md).


### Attributes

As explained on the [attributes reference page](~/model_ref/attrs.md), the state of a particle algorithm is defined by its *attributes*.
Attributes are similar to fields of a class, but they are created differently and provide some additional functionality.
We use a hierarchy of classes to implement all required features of the attributes:

<img src="~/images/particle_attribute_hierarchy.png" alt="Particle Attribute Hierarchy" title="Particle Attribute Hierarchy" width="600"/>

The [`IParticleAttribute`][7] interface defines how all attributes can be used internally by the system.
It is implemented by the non-abstract classes in the hierarchy, all of which are derived from several abstract classes defining their features.
The interface is not generic (it does not specify a type parameter), but it declares a [`GetAttributeType`][8] method so the type of the attribute is still available.

At the top of the hierarchy, the abstract [`ParticleAttributeBase`][9] class defines the most common fields of all attributes, a [`Particle`][2] reference and the attribute's name.
Immediately below that, the abstract and generic [`ParticleAttribute<T>`][10] class declares the methods available to the algorithm developer.
The attribute fields of the algorithm class always have the type [`ParticleAttribute<T>`][10], where `T` is the type of value that the attribute stores.
Accordingly, all `CreateAttributeXYZ` methods of the [`ParticleAlgorithm`][1] class have a specification of [`ParticleAttribute<T>`][10] as their return type, even though they return objects of the types at the bottom of the hierarchy.

One level below this, the abstract and generic [`ParticleAttributeWithHistory<T>`][11] class adds functionality that should only be available to the internal system class, not the algorithm developer.
This includes the history and save/load features, some parts of the [`IParticleAttribute`][7] interface and some additional methods used to implement the round simulation.

Finally, the `ParticleAttribute_XYZ` classes at the bottom of the hierarchy specialize the above classes for the supported attribute data types.
These classes are not abstract or generic and they implement the required functionality that is specific to their data types.
For example, the [`ParticleAttribute_PinConfiguration`][12] class uses an alternative method of storing its history of pin configurations, which is only necessary for this type of attribute.

Internally, the [`Particle`][2] class uses a list of [`IParticleAttribute`][7]s to manage its attributes.
When the algorithm calls an attribute creation method, the static [`ParticleAttributeFactory`][13] class creates an object for the requested type and adds it to the particle's list of attributes.
The class also ensures that attributes are only created in the algorithm's constructor (using the [`Particle`][2]'s [`inConstructor`][14] flag) and their names do not collide with the reserved attribute names `Chirality` and `Compass Dir`.
These names are reserved so that the particle's chirality and compass direction can be accessed like regular attributes, even though they have a different internal representation.

The attribute classes additionally have some special features implementing simulation-specific functionality, which is discussed on the [round simulation page](round_sim.md).



## System Initialization API

Algorithm developers can use the custom initialization feature to define the initial system configuration manually and to add custom parameters to the particles.
The main part of this feature is the abstract [`InitializationMethod`][15] class.
It is similar to the [`ParticleAlgorithm`][1] class in that the developer creates a subclass of [`InitializationMethod`][15] to implement the custom initialization procedure.
The class provides several interface methods like [`AddParticle(...)`][16] and [`PlaceParallelogram(...)`][17] that can be used by the subclass's `Generate()` method to define the initial particle system.
All of these interface methods directly refer to the [`ParticleSystem`][3] to apply the corresponding changes to the current system.

In Init Mode, the particle system uses the [`InitializationParticle`][18] class instead of [`Particle`][2] to represent its particles.
More precisely, it actually uses the [`OpenInitParticle`][19] class, which inherits from [`InitializationParticle`][18] but provides more direct access to its internal data, making it easier to manipulate.
The [`InitializationParticle`][18] class is abstract and provides the interface for the developer.
It is a simplified version of the main [`Particle`][2] class, lacking all functionality that is not required in Init Mode.
Objects of this type act as placeholders in the initialization system that are replaced by proper particles when the simulation starts.

The [`InitializationParticle`][18] class uses its own list of attributes to represent the initialization parameters.
They are initialized to match the parameters of the selected algorithm's `Init(...)` method and they can be updated using the particle's [`SetAttribute(string name, object value)`][20] and [`SetAttributes(object[] values)`][21] methods in the custom initialization code.
When the simulation starts, the values of these attributes are passed to the `Init(...)` method's parameters of each new particle.
This task is performed by the [`ParticleFactory`][22] class.
Many of the features in this system, like creating attributes according to the `Init(...)` method's parameters, are handled by the reflection helper methods, which are explained on [their own page](reflection.md).





[1]: xref:AS2.Sim.ParticleAlgorithm
[2]: xref:AS2.Sim.Particle
[3]: xref:AS2.Sim.ParticleSystem
[4]: xref:AS2.Sim.Particle.isActive
[5]: xref:AS2.Sim.ParticleSystem.InMovePhase*
[6]: xref:AS2.Sim.ParticleSystem.InBeepPhase*
[7]: xref:AS2.Sim.IParticleAttribute
[8]: xref:AS2.Sim.IParticleAttribute.GetAttributeType
[9]: xref:AS2.Sim.ParticleAttributeBase
[10]: xref:AS2.Sim.ParticleAttribute`1
[11]: xref:AS2.Sim.ParticleAttributeWithHistory`1
[12]: xref:AS2.Sim.ParticleAttribute_PinConfiguration
[13]: xref:AS2.Sim.ParticleAttributeFactory
[14]: xref:AS2.Sim.Particle.inConstructor
[15]: xref:AS2.InitializationMethod
[16]: xref:AS2.InitializationMethod.AddParticle(Vector2Int,AS2.Direction,AS2.Initialization.Chirality,AS2.Initialization.Compass)
[17]: xref:AS2.InitializationMethod.PlaceParallelogram(Vector2Int,AS2.Direction,System.Int32,System.Boolean,System.Int32,AS2.Initialization.Chirality,AS2.Initialization.Compass)
[18]: xref:AS2.Sim.InitializationParticle
[19]: xref:AS2.Sim.OpenInitParticle
[20]: xref:AS2.Sim.InitializationParticle.SetAttribute(System.String,System.Object)
[21]: xref:AS2.Sim.InitializationParticle.SetAttributes(System.Object[])
[22]: xref:AS2.Sim.ParticleFactory
