# Dev Guide: Particle Rendering

The [`RendererParticles`][1] class is the root of a sub-tree responsible for rendering the particles as well as their bonds and circuits.
Since this is a very complex system with too many features to cover in detail, the next pages focus on the main components and roughly explain the other parts where they are relevant.

<img src="~/images/particle_render_system_overview.png" alt="Particle render system subtree" title="Particle render system subtree" width="600"/>

The class itself only manages its child renderers and some additional data, and forwards the render calls and other operations to the corresponding classes.
The purpose of the `*_RenderBatch` classes is to render all objects that can be batched together, i.e., objects with the same visual properties like color or animation type.
To organize this, they define property block data structs that are used as keys for identifying the render batch indices.
For example, the [`RendererParticles_RenderBatch`][2] class defines the [`RendererParticles_RenderBatch.PropertyBlockData`][3] struct, which contains a color and the number of pins per side.
The [`RendererParticles`][1] has a dictionary that maps instances of this struct to [`RendererParticles_RenderBatch`][2] instances.
Whenever a particle is added to the render system, the [`RendererParticles`][1] checks if a render batch for the particle's color and number of pins already exists and adds the particle to the batch.
If no batch exists yet, it creates a new batch and adds a new entry to the dictionary.
All future particles with the same color and number of pins will be rendered by that batch.

A similar approach is used for the circuit render batch classes, but the system is more complex.
This part is handled by a single [`RendererCircuitsAndBonds`][4] instance that acts as the root of another sub-tree.
Because bonds are technically rendered the same way as bonds, it makes sense to handle these two components together.

In addition to the render batches, the [`RendererParticles`][1] also keeps track of all particles that are registered in the render system.
They are stored in a dictionary that maps [`IParticleState`][5]s to [`ParticleGraphicsAdapterImpl`][6]s.
The [`IParticleState`][5] interface is implemented by the particle class and allows the render system to read some general properties of the particles, such as its position, expansion state, compass direction and chirality, and even to change some of the properties.
The [`ParticleGraphicsAdapterImpl`][6] is a class containing all visualization-specific data associated with a particle.
Every particle has one instance of this class that it can use to push visualization updates to the render system.
The [`RendererParticles`][1] has methods for adding and removing particles that can be accessed through these interfaces.


[1]: xref:AS2.Visuals.RendererParticles
[2]: xref:AS2.Visuals.RendererParticles_RenderBatch
[3]: xref:AS2.Visuals.RendererParticles_RenderBatch.PropertyBlockData
[4]: xref:AS2.Visuals.RendererCircuitsAndBonds
[5]: xref:AS2.Visuals.IParticleState
[6]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl

