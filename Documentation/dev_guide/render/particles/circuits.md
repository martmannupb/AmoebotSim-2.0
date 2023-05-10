# Dev Guide: Circuit and Bonds Renderer

- The [`RendererParticles`][1] class manages a [`RendererCircuitsAndBonds`][2] instance to handle the rendering of everything that is added to the particle bodies
	<img src="~/images/circuit_renderer_all.png" alt="Circuit Renderer example" title="Circuit Renderer example" width="600"/>  
	<img src="~/images/circuit_renderer_bonds_cropped.png" alt="Bonds" title="Bonds" width="200"/> <img src="~/images/circuit_renderer_lines_cropped.png" alt="Circuit lines" title="Circuit lines" width="200"/> <img src="~/images/circuit_renderer_circles_cropped.png" alt="Beep highlights and partition set handles" title="Beep highlights and partition set handles" width="200"/>
	- Bonds
	- Circuit lines inside and outside the particles
		- With beep highlights
	- Internal "pins" representing partition sets
		- Including highlights for beep origins
		- Also, the little circles to cover up the gaps at the corners of the circuit lines
	- Note that the actual pins on the borders of the particles are already rendered by the particle renderer

- The [`RendererCircuitsAndBonds`][2] class only serves as a container for the [`RendererCircuits_Instance`][3] class, which handles the rendering management
	- Its ([`RendererCircuits_Instance`][3]'s) role is similar to the [`RendererParticles`][1] class, which manages the [`RendererParticles_RenderBatch`][4] instances
	- The [`RendererCircuitsAndBonds`][2] has two instances of this class
		- One of the instances is always being rendered while the other one can be modified
		- Once all changes are applied, the two instances are swapped
		- This was originally implemented to allow updating circuit and bond data over multiple frames, but it turned out to be unnecessary
- [`RendererCircuits_Instance`][3]
	- Similar to [`RendererParticles`][1]
	- Has maps from `PropertyBlock`s to `XY_RenderBatch` classes
		- One map from [`RendererCircuits_RenderBatch.PropertyBlockData`][5] to [`RendererCircuits_RenderBatch`][6] and one map from [`RendererCircuitPins_RenderBatch.PropertyBlockData`][7] to [`RendererCircuitPins_RenderBatch`][8]
		- The first one renders rectangles to represent lines
			- Circuit connection lines inside and outside of particles, bonds
		- The second one renders circles representing partition set handles
			- Partition set handles and colored circles at the junctions of circuit lines in expanded particles, covering the gaps
	- Main purpose is managing the render batches and providing helper data structures and methods
		- Defines the [`ParticleCircuitData`][9] struct
			- Contains the circuit data corresponding to a single particle
			- [`PSetInnerPinRef`][10] struct is used to represent the inner "pins"/handles used to represent partition sets and to locate circuit line connectors
		- [`GDRef`][11] struct is used to bundle references to graphical data with indexing information for a single object (line or pin/handle)
	- When circuit or bond data is added to the instance, it creates the corresponding line and circle objects and moves them to the correct render batch
		- New batches are created if necessary
		- The placement of partition set handles is computed here, using the [`DistributionCircleLine`][12] and [`DistributionCircleArea`][13] classes


- The render batch classes are [`RendererCircuits_RenderBatch`][6] (for lines) and [`RendererCircuitPins_RenderBatch`][7] (for partition set handles and pin beep highlights)
	- [`MaterialPropertyBlockData_Circuits`][14] instances store properties that are common among all objects drawn by one batch
	- Each class stores TRS matrices (just like [`RendererParticles_RenderBatch`][4])
		- [`RendererCircuitPins_RenderBatch`][7] distinguishes between partition set handles and connector pins
	- Both classes define their own `PropertyBlockData` struct as a mapping key
		- All objects with the same `PropertyBlockData` are rendered by the same batch
	- The `Init()` method of each class initializes the materials and other parameters according to the given properties
	- Both implement the [`IGenerateDynamicMesh`][14] interface, meaning that they generate meshes at runtime
	- Animations are handled by updating animation time stamps
		- They are updated in each frame so that changes to the animation speed take effect immediately
	- The classes are overall very similar to [`RendererCircuits_RenderBatch`][6]
		- But less complex



TODO




[1]: xref:AS2.Visuals.RendererParticles
[2]: xref:AS2.Visuals.RendererCircuitsAndBonds
[3]: xref:AS2.Visuals.RendererCircuits_Instance
[4]: xref:AS2.Visuals.RendererParticles_RenderBatch
[5]: xref:AS2.Visuals.RendererCircuits_RenderBatch.PropertyBlockData
[6]: xref:AS2.Visuals.RendererCircuits_RenderBatch
[7]: xref:AS2.Visuals.RendererCircuitPins_RenderBatch.PropertyBlockData
[8]: xref:AS2.Visuals.RendererCircuitPins_RenderBatch
[9]: xref:AS2.Visuals.RendererCircuits_Instance.ParticleCircuitData
[10]: xref:AS2.Visuals.RendererCircuits_Instance.ParticleCircuitData.PSetInnerPinRef
[11]: xref:AS2.Visuals.RendererCircuits_Instance.GDRef
[12]: xref:AS2.Visuals.DistributionCircleLine
[13]: xref:AS2.Visuals.DistributionCircleArea
[14]: xref:AS2.Visuals.IGenerateDynamicMesh
