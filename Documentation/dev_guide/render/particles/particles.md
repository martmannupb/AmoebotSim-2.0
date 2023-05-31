# Dev Guide: Particle Render Batches

As outlined before, the [`RendererParticles`][1] class manages one [`RendererParticles_RenderBatch`][2] instance for each occurring combination of particle color and number of pins per side.
The render batch class itself is then responsible for rendering all particles with a specific color and number of pins.
For this purpose, it stores a list of all registered particles matching these properties as well as a number of TRS matrices for each particle.
The matrices are stored in lists of arrays of size 1023 so that each array can be submitted to a single instanced draw call.
New arrays are added to the lists if necessary.
The class stores 12 of these lists because multiple components have to be rendered for the different view types.


## Particle Components

Rendering a single particle involves drawing several components, some of which are only shown in certain view modes.
The base component that is always shown is the particle's *body*.
In the hexagonal view modes, the body consists of a solid black outline, pins distributed on the particle's edges, and the colored internal area.
In graph view mode, it consists of a solid gray circle with a colored ring around it.

<img src="~/images/render_particle_body_hex.png" alt="Contracted particle body (hexagonal)" title="Contracted particle body (hexagonal)" height="225"/> <img src="~/images/render_particle_body_round.png" alt="Contracted particle body (hexagonal, round)" title="Contracted particle body (hexagonal, round)" height="225"/> <img src="~/images/render_particle_body_graph.png" alt="Contracted particle body (triangular graph)" title="Contracted particle body (triangular graph)" height="225"/>

If the particle is expanded, the shader for the hexagonal view modes automatically computes the connection between the particle's head and its tail.
In graph view mode, this connection is not created by the shader:

<img src="~/images/render_particle_body_exp_hex.png" alt="Expanded particle body (hexagonal)" title="Expanded particle body (hexagonal)" height="125"/> <img src="~/images/render_particle_body_exp_round.png" alt="Expanded particle body (hexagonal, roung)" title="Expanded particle body (hexagonal, round)" height="125"/> <img src="~/images/render_particle_body_exp_graph.png" alt="Expanded particle body (triangular graph)" title="Expanded particle body (triangular graph)" height="125"/>

That is why the second component is the *connector* of expanded, expanding or contracting particles in the graph view mode.
It is simply a black rectangle that is drawn on a Z layer beneath the particle body:

<img src="~/images/render_particle_exp_connector.png" alt="Expanded particle connector (triangular graph)" title="Expanded particle connector (triangular graph)" width="200"/>

The third and final component is the *pin overlay*, which is only required in the hexagonal view modes and when the circuit visualization is enabled.
It is the same as the particle body, except that it only contains the pins, not the particle outline and not the colored internal area.
Its purpose is to be rendered on a Z layer on top of the circuits (which are rendered on top of the particle body by a different rendering class) so that the pins remain visible on top of the lines.

<img src="~/images/render_particle_pins_hex.png" alt="Contracted particle pins (hexagonal)" title="Contracted particle pins (hexagonal)" height="225"/> <img src="~/images/render_particle_pins_round.png" alt="Contracted particle pins (hexagonal, round)" title="Contracted particle pins (hexagonal, round)" height="225"/>

These images show how the particle body, circuits and the pins are layered to produce the desired effect:

<img src="~/images/render_z_layers_particle.png" alt="Components on different Z layers" title="Components on different Z layers" height="200"/> <img src="~/images/render_z_layers_particle_flat.png" alt="Result in orthographic view" title="Result in orthographic view" height="200"/>

They also show the beep highlights that are drawn on top of the circuit lines and the partition set handles.

### Rendering

Each component is rendered using a specific material on a simple quad mesh.
For the particle connector in graph view, the material has a solid color and the mesh is shaped to exactly match the connector.
For the particle body and the pin overlay, the meshes are rectangles that are large enough to fit an expanded particle.
This way, the material has enough space to draw a contracted or expanded particle and the animated transitions between the expansion states.
The meshes are created by the [`MeshCreator_CircularView`][3] and [`MeshCreator_HexagonalView`][4] classes, using some of the static constants defined in the [`RenderSystem`][5].

The materials are created by the [`TextureCreator`][6] class.
This class creates copies of existing materials and modifies their parameters to achieve the desired appearance.
For the particle body and the pin overlay in the hexagonal view modes, copies of `Resources/Materials/HexagonalView/HexagonCombinedMat` are used.
This material is explained in detail on the [shader example page](shader_example.md).
It basically takes some textures that describe the particle outline and the connecting piece as well as a fill color as parameters and uses them to render a particle that is contracted, expanded, or anywhere inbetween.
The texture describing the particle's outline must already contain the pins, so the [`TextureCreator`][6] creates a copy of the base hexagon texture (which does not have any pins) and draws the required pins directly into the texture.
For the pin overlay, it draws the pins into a transparent texture in the same manner and replaces the connector texture by a transparent one.

<img src="~/images/Hexagon1_1024.png" alt="Base hexagon texture (without pins)" title="Base hexagon texture (without pins)" height="200"/> <img src="~/images/Rectangle1_1024.png" alt="Base hexagonal connector texture" title="Base hexagonal connector texture" height="200"/>

If the circuit visualization is disabled, the [`RendererParticles_RenderBatch`][2] requests a new body material from the [`TextureCreator`][6] which does not contain the pins, and it stops rendering the pin overlay altogether.
The [`TextureCreator`][6] caches the textures and materials it creates to avoid generating the same things multiple times.

For the graph view, the `Resources/Materials/CircularView/ConnectorMat` and `Resources/Materials/CircularView/ParticleMat` materials are used to render the connector and the particle body, respectively.
Just like the hexagonal material, these materials have parameters specifying the current expansion percentage to control the animation.
The material for the connector is special in that it uses the shader's vertex output to shift the quad mesh's vertices for the animation.
The vertices that should be moving are marked in UV channel 1 by the [`MeshCreator_CircularView`][3] (the [shader example page](shader_example.md) explains this in more detail).



## Matrices

As mentioned above, the [`RendererParticles_RenderBatch`][2] stores 12 TRS matrices per particle.
This is because there are 3 components (body, pin overlay, graph view connector) and each component has 4 versions, one for each possible movement phase (contracted, expanded, contracting and expanding).
Each of the used materials has parameters controlling its animation state: The start time of the animation, the duration, and the expansion percentages at the start and the end of the animation.
The shader will interpolate between the start and end percentages within the given time frame.
Setting the start and end percentage to the same value disables the animation, i.e., setting both to 0 represents the contracted state and setting both to 1 represents the expanded state.

Recall that a [`RendererParticles_RenderBatch`][2] instance renders all particles with the same color and number of pins.
Within this set of particles, there may be some of every movement state.
These particles have to be rendered in separate draw calls because their materials use different animation parameters.
However, because we want to draw as many objects as possible with little overhead, and since the movement state of a particle may change very frequently, collecting particles with the same movement state into separate batches is not an option.
Instead, we render each particle 4 times, once for each movement state, but only one of the states gets the correct TRS matrix.
Whenever a particle's visualization state is updated, which usually happens in each frame, its matrices are reset to a position that is not visible (because its Z layer is behind the camera) and only the matrices matching the particle's movement state are updated to the appropriate position.
This way, we can submit all matrices in the minimum number of instanced draw calls, and only the correct version of each particle will be visible.

Each of the groups of matrices requires an appropriate property block that lets the material display the corresponding movement phase/expansion state.
For this purpose, the [`MaterialPropertyBlockData_Particles`][7] class is used.
This class is a wrapper around a single Unity `MaterialPropertyBlock` which contains material parameters specific to the particle materials.
It allows changing values easily without having to know the exact names of the parameters in the materials.



[1]: xref:AS2.Visuals.RendererParticles
[2]: xref:AS2.Visuals.RendererParticles_RenderBatch
[3]: xref:AS2.Visuals.MeshCreator_CircularView
[4]: xref:AS2.Visuals.MeshCreator_HexagonalView
[5]: xref:AS2.Visuals.RenderSystem
[6]: xref:AS2.Visuals.TextureCreator
[7]: xref:AS2.Visuals.MaterialPropertyBlockData_Particles
