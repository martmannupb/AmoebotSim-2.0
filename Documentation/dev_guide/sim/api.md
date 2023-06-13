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
This feature is explained in more detail on the round simulation page.
**TODO**


### Attributes

As explained on the [attributes reference page](~/model_ref/attrs.md), the state of a particle algorithm is defined by its *attributes*.
Attributes are similar to fields of a class, but they are created differently and provide some additional functionality.
We use a hierarchy of classes to implement all required features of the attributes:

<img src="~/images/particle_attribute_hierarchy.png" alt="Particle Attribute Hierarchy" title="Particle Attribute Hierarchy" width="600"/>

The [`IParticleAttribute`][7] interface defines how all attributes can be used internally by the system.
It is implemented by the non-abstract classes in the hierarchy, all of which are derived from several abstract classes defining their features.

*TODO*





- Algorithm part
    - Attributes
        - Particle state is defined by its attributes (from algorithm perspective)
        - Particle class has a list of IParticleAttributes
        - Hierarchy of ParticleAttribute classes
            - Special classes for each supported type
            - User only sees the interface types
            - Special methods for creating them
            - ParticleAttributeFactory creates the attribute objects
                - Makes sure that names do not conflict with occupied names for chirality and compass direction
                - These are accessible through the same interface that attributes can be accessed, so no attributes can have these names
            - Generic ParticleAttribute class defines how users can access attributes
        - Attributes can only be created while in the algorithm's constructor
            - Special flag set by the ParticleFactory (or Particle) before and after calling algorithm constructor
        - Special features of attributes explained on round simulation page
    - Actions
        - Method calls by algorithm do not immediately change the state of the particle
        - Actions are instead stored in the particle to be processed when all particles are done
        - Again, more info on round simulation page
- Initialization part
    - Abstract InitializationMethod class provides interface
        - Users implement subclass for custom generation method
    - ParticleAlgorithm subclass references the generation method class
    - Applies operations to the ParticleSystem while in Init Mode
    - In Init Mode, the system maintains a system of InitializationParticles
        - Instead of the usual Particles
        - Act as placeholders with simpler data and interface
        - When the simulation starts, they are replaced by actual particles
        - Particles are instantiated with the selected algorithm and using the attributes stored in the InitParticles
        - ParticleFactory class is used to create and initialize them
        - The link between the Particle and the ParticleAlgorithm is established here









[1]: xref:AS2.Sim.ParticleAlgorithm
[2]: xref:AS2.Sim.Particle
[3]: xref:AS2.Sim.ParticleSystem
[4]: xref:AS2.Sim.Particle.isActive
[5]: xref:AS2.Sim.ParticleSystem.InMovePhase*
[6]: xref:AS2.Sim.ParticleSystem.InBeepPhase*
[7]: xref:AS2.Sim.IParticleAttribute

[8]: xref:AS2.ValueHistorySaveData`1
[9]: xref:AS2.PinConfigurationSaveData
[10]: xref:AS2.Sim.ValueHistoryPinConfiguration
[11]: xref:AS2.FileBrowser
