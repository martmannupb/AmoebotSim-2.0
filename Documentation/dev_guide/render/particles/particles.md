# Dev Guide: Particle Render Batches

As outlined before, the [`RendererParticles`][1] class manages one [`RendererParticles_RenderBatch`][2] instance for each occurring combination of particle color and number of pins per side.
The render batch class is responsible for rendering all particles with a specific color and number of pins.
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

That is why the second component is the connector of expanded, expanding or contracting particles in the graph view mode.
It is simply a black rectangle that is drawn on a Z layer beneath the particle body:

<img src="~/images/render_particle_exp_connector.png" alt="Expanded particle connector (triangular graph)" title="Expanded particle connector (triangular graph)" width="200"/>

The third and final component is only required in the hexagonal view modes and when the circuit visualization is enabled.
It is similar to the particle body, but it only contains the pins, not the particle outline and not the colored internal area.
Its purpose is to be rendered on a Z layer on top of the circuits (which are rendered on top of the particle body by a different rendering class) so that the pins remain visible on top of the lines.

<img src="~/images/render_particle_pins_hex.png" alt="Contracted particle pins (hexagonal)" title="Contracted particle pins (hexagonal)" height="225"/> <img src="~/images/render_particle_pins_round.png" alt="Contracted particle pins (hexagonal, round)" title="Contracted particle pins (hexagonal, round)" height="225"/>

These images show how the particle body, circuits and the pins are layered to produce the desired effect:

<img src="~/images/render_z_layers_particle.png" alt="Components on different Z layers" title="Components on different Z layers" height="200"/> <img src="~/images/render_z_layers_particle_flat.png" alt="Result in orthographic view" title="Result in orthographic view" height="200"/>

They also show the beep highlights that are drawn on top of the circuit lines and the partition set handles.





- The [`RendererParticles`][1] class stores one [`RendererParticles_RenderBatch`][2] instance for each combination of color and number of pins per side
- This render batch is responsible for rendering all particles with that color and number of pins
- It also stores a dictionary of all registered particles
	- Only the particles to be rendered with this color and number of pins
- Stores a number of TRS matrices for each particle
	- The matrices are stored in lists of arrays
	- Each array has size 1023 so that it can be rendered in a single instanced draw call
	- New arrays are added to the lists if necessary


	- There is one matrix for the particle's body (head and tail), one for the "bridge"/"connector" in the graph view and an additional one for the transparent pin overlay
		- The body displays a hexagon/circle with pins and fill color in the hexagonal views and only a black circle with a colored ring in graph view mode
			- The shader used in the hexagonal view modes includes the connector part of expanded particles
			- In the graph view mode, the connector has to be rendered separately
		- Pins are drawn twice so that circuit lines lie under pins but above particles
			- Has to be done because pins are rendered on the same Z layer as the particle body  
			
			- The image shows the particle body on the lowest Z layer, the circuit connection lines above that, then the circuit beep highlights, next the pin overlay mentioned above (together with partition set handles in the middle) and finally, the partition set handle beep overlay (the little gray dot)
			- During animations, only the particle layer is rendered
			- If the circuit overlay is switched off, the texture of the particle body is replaced with a texture that has no pins and the other layers are not rendered
				- See shader example to understand which texture
		- The meshes are generated by the [`MeshCreator_CircularView`][3] and [`MeshCreator_HexagonalView`][4] classes
			- Just simple quads with enough space for a particle body (or a quad of the same size as the connector)
			- Quads are always expanded, animations happen inside the quad, handled by the shader
	- Each matrix exists 4 times
		- Once for each movement phase (contracted, expanded, contracting and expanding)
		- The same material applied to the same mesh can show a different behavior based on the supplied property block
		- Expansion states and their transition animations are implemented this way
			- There is one property block for each of the possible movement phases
			- See shader example, one property block with $p=q=0$, one with $p=0, q=1$, etc.
		- All of the matrices are rendered in each frame
			- But for each particle, only one of the four has the correct matrix
				- The others have a default matrix that places the mesh far out of sight
			- This is done for efficiency reasons
				- We only have to update the matrices and can render all particles with the same color and number of pins in just four instanced draw calls (times the number of particles divided by 1023)
				- Batching the particles by their movement phase would lead to fewer matrices but much more book keeping effort and more draw calls
- Material for hexagonal view modes:
	- `Resources/Materials/HexagonalView/HexagonCombinedMat`
		- This material is explained in the shader example
	- Basically takes some textures and uses them to render a particle that is contracted, expanded, or anywhere inbetween
		- The texture may contain pins
		- For the pin overlay, the texture contains only pins
			- And the connector is transparent
- Materials for graph view:
	- `Resources/Materials/CircularView/ConnectorMat` for the connection pieces
		- Actually uses vertex shader for the animation
			- Moving vertices of the quad are marked using UV channel 1
	- `Resources/Materials/CircularView/ParticleMat` for the particle body
	- The shaders work very similarly to the hexagon shaders but without the overlapping feature (is already computed as part of the other computations)
- The materials are created by the [`TextureCreator`][5] class
	- It creates copies of the base materials and gives them different textures
	- The textures for the hexagonal views can contain pins
		- These textures are generated by copying one of the base textures and then writing several copies of the pin texture (`Resources/Textures/PinTex`) onto it
		- The same way, transparent textures with only the pins are created
- Material property blocks are managed using the [`MaterialPropertyBlockData_Particles`][6] class
	- Wrapper around a Unity `MaterialPropertyBlock`
	- Contains shader parameters specific to the particle shaders
	- One block for each type of mesh and each expansion state



TODO




[1]: xref:AS2.Visuals.RendererParticles
[2]: xref:AS2.Visuals.RendererParticles_RenderBatch
[3]: xref:AS2.Visuals.MeshCreator_CircularView
[4]: xref:AS2.Visuals.MeshCreator_HexagonalView
[5]: xref:AS2.Visuals.TextureCreator
[6]: xref:AS2.Visuals.MaterialPropertyBlockData_Particles
