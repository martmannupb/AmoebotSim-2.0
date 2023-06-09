# Dev Guide: Particle Rendering Interface and Data Structures

The previous pages have covered the structure of the particle rendering system and the purpose and function of the most important classes.
There are still many aspects of the render system that have not been explained but which could be useful to know when extending the system.
This page mentions a few of these, focusing on data structures and how visualization data gets from the particles in the simulator to the render system.


## Interface

The particle class used by the simulator implements the [`IParticleState`][1] interface.
This provides basic state information like the particle's position and expansion state, but also graphical information like its current color.
Its most important function for the render system, however, is giving access to an object implementing the [`IParticleGraphicsAdapter`][2] interface.
More precisely, every particle in the simulator has an instance of the [`ParticleGraphicsAdapterImpl`][3] class, which implements this interface.
This instance is the particle's representation in the render system and it allows the simulator to push graphics updates without directly accessing the render system.
The class has [`AddParticle`][4] and [`RemoveParticle`][5] methods that are used to register or deregister a particle in the render system.
Registered particles must be updated by calling the [`Update(ParticleMovementState)`][6] method when all changes have been applied, which is usually after each round.
Most of the methods available to the simulator use the [`ParticleMovementState`][7] and [`ParticleJointMovementState`][8] structs to transfer information on the particle's movement from the current round to the next.
Internally, this information is packed into [`PositionSnap`][9] structs, which contain additional rendering-specific information like a timestamp and animation flag (whether or not the movement should be animated).

The bonds and circuits of a particle are updated separately by calling the [`BondUpdate(ParticleBondGraphicState`][10] and [`CircuitUpdate(ParticlePinGraphicState`][11] methods.
Here, [`ParticleBondGraphicState`][12] is a simple struct that contains visual information on a single bond.
The bond update method must be called once for every bond in the whole system.
It is up to the simulator to ensure that every bond is accounted for and no bond is registered twice.
Due to the way the bonds are computed, the simulator can assign each bond to one of the two particles it connects, making this relatively easy to achieve.

The [`ParticlePinGraphicState`][13] class has a similar purpose but is more complex because it represents the entire pin configuration of a particle.
It defines several helper data structures to organize the information it stores.
Data belonging to individual partition sets is stored in instances of the [`PSetData`][14] class.
Each of these instances has a [`GraphicalData`][15] instance which stores all of the relevant material properties and render batch indices so that changes made to a partition set can be forwarded directly to the render batch responsible for rendering this partition set.
Similarly, individual pins are identified using the [`PinDef`][16] struct, which is a small helper containing positional information of a single pin.


## Additional Helpers and Data Structures

As mentioned before, the [`RendererCircuits_Instance`][17] class manages the partition set placement.
It calculates the relaxed partition set positions using the [`CircleDistributionCircleLine`][18] and [`CircleDistributionCircleArea`][19] helper classes.
To ensure that their results are deterministic and reproducable, these classes internally use the custom [`RandomNumberGenerator`][20] class as a source for pseudo-randomness.
By resetting the generator before each execution of the relaxation algorithm, they prevent the partition sets from jumping around randomly from one round to the next.

The [`RenderBatchIndex`][24] struct is used throughout the lower levels of the render system to identify specific objects belonging to a render batch.
The struct stores a combination of list and array index, matching the way TRS matrices are stored in the render batch classes (as lists of arrays).
It is particularly useful when an object needs to be changed after it has been assigned to a render batch, like partition set handles that can be dragged around by the user, for example.

Another class that is used by many parts of the render system is the [`MaterialDatabase`][25].
It is a static class that provides references to all materials used by the render system.

Whenever rendering assets need to be generated at runtime, the [`TextureCreator`][26], [`MeshCreator_CircularView`][27] and [`MeshCreator_HexagonalView`][28] classes are used.
The [`TextureCreator`][26] provides methods for generating textures and materials for particles.
Its main purpose is creating textures for the hexagonal and round hexagonal particle bodies by combining the base body textures with the pin texture.
The class also stores the generated textures and materials to avoid regenerating them multiple times, since this is a very expensive operation.
The mesh creator classes are used to generate meshes for the background grid as well as quad meshes for particles and circuit lines.
These meshes are generated at runtime because many of them can be modified by changing the scale constants in the [`RenderSystem`][29].



[1]: xref:AS2.Visuals.IParticleState
[2]: xref:AS2.Visuals.IParticleGraphicsAdapter
[3]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl
[4]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.AddParticle(AS2.Visuals.ParticleMovementState)
[5]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.RemoveParticle
[6]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.Update(AS2.Visuals.ParticleMovementState)
[7]: xref:AS2.Visuals.ParticleMovementState
[8]: xref:AS2.Visuals.ParticleJointMovementState
[9]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.PositionSnap
[10]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.BondUpdate(AS2.Visuals.ParticleBondGraphicState)
[11]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.CircuitUpdate(AS2.Visuals.ParticlePinGraphicState)
[12]: xref:AS2.Visuals.ParticleBondGraphicState
[13]: xref:AS2.Visuals.ParticlePinGraphicState
[14]: xref:AS2.Visuals.ParticlePinGraphicState.PSetData
[15]: xref:AS2.Visuals.ParticlePinGraphicState.PSetData.GraphicalData
[16]: xref:AS2.Visuals.ParticlePinGraphicState.PinDef
[17]: xref:AS2.Visuals.RendererCircuits_Instance
[18]: xref:AS2.Visuals.CircleDistributionCircleLine
[19]: xref:AS2.Visuals.CircleDistributionCircleArea
[20]: xref:AS2.RandomNumberGenerator
[21]: xref:AS2.Visuals.RendererCircuits_Instance.ParticleCircuitData
[22]: xref:AS2.Visuals.RendererCircuits_Instance.ParticleCircuitData.PSetInnerPinRef
[23]: xref:AS2.Visuals.RendererCircuits_Instance.GDRef
[24]: xref:AS2.Visuals.RenderBatchIndex
[25]: xref:AS2.MaterialDatabase
[26]: xref:AS2.Visuals.TextureCreator
[27]: xref:AS2.Visuals.MeshCreator_CircularView
[28]: xref:AS2.Visuals.MeshCreator_HexagonalView
[29]: xref:AS2.Visuals.RenderSystem
