# Dev Guide: Circuit and Bonds Renderer

The [`RendererParticles`][1] class manages a [`RendererCircuitsAndBonds`][2] instance to handle the rendering of all elements that are added to the basic particle bodies, namely *bonds*, *circuit lines* and *partition set handles*, including the beep highlights.
The circuit lines also include little circles placed where the bends of the lines are to cover up the gaps between the rectangles.

<img src="~/images/circuit_renderer_all.png" alt="Circuit Renderer example" title="Circuit Renderer example" width="600"/>  
<img src="~/images/circuit_renderer_bonds_cropped.png" alt="Bonds" title="Bonds" width="200"/> <img src="~/images/circuit_renderer_lines_cropped.png" alt="Circuit lines" title="Circuit lines" width="200"/> <img src="~/images/circuit_renderer_circles_cropped.png" alt="Beep highlights and partition set handles" title="Beep highlights and partition set handles" width="200"/>

As mentioned before, the structure of the circuit and bond rendering sub-tree is a bit more complex than the base particle rendering system.
In particular, there is one extra management level and there are two instances of a manager class that take turns with rendering rounds.

<img src="~/images/circuit_render_system_overview.png" alt="Circuit rendering sub-tree" title="Circuit rendering sub-tree" width="600"/>

The [`RendererCircuitsAndBonds`][2] class only serves as a container for the [`RendererCircuits_Instance`][3] class, which handles the actual rendering management.
The role of this class is similar to that of the [`RendererParticles`][1] class, which manages the [`RendererParticles_RenderBatch`][4] instances, as explained on the [particle rendering page](particles.md).
The [`RendererCircuitsAndBonds`][2] manages two instances of the [`RendererCircuits_Instance`][3].
At any time, one of the two instances is rendered while the other one can be modified.
Once all changes are applied to the instance, the two instances are swapped.
This usually happens every time a round simulation has finished.
> [!NOTE]
> This feature was originally implemented to allow updating circuit and bond data over multiple frames.
> However, the performance is good enough even when all updates are made in a single frame, so there is no mechanism for splitting the updates.


## Instances

Each [`RendererCircuits_Instance`][3] manages its own set of render batches, just like the [`RendererParticles`][1].
It also has dictionaries that map `PropertyBlock`s to `XY_RenderBatch` instances, but this time, we need two different kinds of render batches.
The [`RendererCircuits_RenderBatch`][6] class only renders rectangles that are used to draw both bonds and circuit lines, while the [`RendererCircuitPins_RenderBatch`][8] class only renders circles to draw partition set handles, beep highlights, and the circuit line caps.
Both of the render batch classes define their own dictionary keys in the form of the [`RendererCircuits_RenderBatch.PropertyBlockData`][5] and [`RendererCircuitPins_RenderBatch.PropertyBlockData`][7] structs.

The main purpose of the [`RendererCircuits_Instance`][3] is managing the render batches:
When circuit or bond data is added to the instance (as managed by the [`RendererCircuitsAndBonds`][2]), the instance creates the corresponding line and circle objects and moves them to the correct render batch.
If necessary, it creates a new batch for properties that are not yet covered by the existing batches.

The instance also provides several helper data structures and methods to organize the data that has to be handled in this sub-tree.
To bundle circuit data that belongs to a single particle, it defines the [`ParticleCircuitData`][9] struct.
This struct defines the [`PSetInnerPinRef`][10] struct to represent the partition set handles and circuit line caps so that they can be addressed individually and their positions are easily accessible.
The [`GDRef`][11] struct is used to bundle references to various graphical data with indexing information for a single object like a line or partition set handle.

Additionally, the class defines methods for placing the partition set handles, using the [`CircleDistributionCircleLine`][12] and [`CircleDistributionCircleArea`][13] helper classes.


## Render Batches

As mentioned above, the render batch classes are [`RendererCircuits_RenderBatch`][6] (rendering lines) and [`RendererCircuitPins_RenderBatch`][7] (rendering circles).
They are very similar to the [`RendererParticles_RenderBatch`][4] class that has been explained on a previous page.
Both of these circuit render batches use [`MaterialPropertyBlockData_Circuits`][14] instances to store the material properties that are common among all objects drawn by one batch.
They also store TRS matrices in lists of arrays to draw as many objects simultaneously as possible.
The `Init()` method of each render batch initializes the materials and its parameters according to the given properties.
Both of the classes also implement the [`IGenerateDynamicMesh`][14] interface, meaning that they generate meshes at runtime, using the constants defined by the [`RenderSystem`][15].
Just like the particle render batch class, the circuit render batches handle animations by updating the animation time stamps in the material property blocks.
These updates are applied in each frame so that changes to the animation speed take effect immediately.
In general, understanding how the particle render batch class [`RendererParticles_RenderBatch`][4] works is sufficient to understand how the circuit render batches work.



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
[12]: xref:AS2.Visuals.CircleDistributionCircleLine
[13]: xref:AS2.Visuals.CircleDistributionCircleArea
[14]: xref:AS2.Visuals.IGenerateDynamicMesh
[15]: xref:AS2.Visuals.RenderSystem
