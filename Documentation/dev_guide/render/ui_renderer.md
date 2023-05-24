# Dev Guide: UI Renderer

The [`RendererUI`][1] class draws the colored overlays for all tools available in Initialization and Simulation Mode.

<img src="~/images/shader_tool_add.png" alt="Add tool overlay" title="Add tool overlay" height="150"/> <img src="~/images/shader_tool_move_2.png" alt="Move tool overlay" title="Move tool overlay" height="150"/> <img src="~/images/shader_tool_pset_move.png" alt="Partition Set Move tool overlay" title="Partition Set Move tool overlay" height="150"/>

Because the class reacts directly to user input and tracks the state of the tools, it additionally triggers the tools' effects by passing the detected input to the corresponding handler.
To detect these inputs, the class uses the [`InputManager`][2] system which is briefly described on the [Input Handling page](~/dev_guide/input.md).
It is the only class that uses this system.
Most of the code in the class is a case distinction that determines what kind of input has been made and how the current tool should react to that input.

The hexagonal overlays are rendered using a simple hexagon mesh created by the [`MeshCreator_HexagonalView`][3] class.
The mesh already has the correct size and is simply placed on the grid position that is currently selected.
If multiple positions are selected (e.g., when an expanded particle is clicked using the Select tool), multiple copies of the mesh are drawn.
Because the number of overlay elements that have to be drawn in each frame is very small, no instancing is used.
All materials for the UI elements can be found in `Resources/Materials/HexagonalView/UI`.
The hexagonal overlay materials use the simple `Resources/Shaders/Basic/ColorTransparent` shader to render the hexagon meshes with transparent colors.

Since the Partition Set Move tool requires other shapes to be drawn and other parts of the simulator to be accessed, the [`RendererUI`][1] class delegates this functionality to the [`PSetDragHandler`][4] class.
Its structure is very similar to that of the [`RendererUI`][1], but it uses a simple quad mesh instead of a hexagon to render the overlay.
The associated material uses the `Resources/Shaders/Basic/TextureColorizationGraph` shader, which takes an RGBA texture and an input color and multiplies the color into the texture.
Because the material uses a texture with a white circle in the middle and a transparent background (which can be found in `Resources/Images/Icons`), the final shape that is drawn to the screen is a circle instead of a rectangle, even though the mesh is rectangular.
The [`PSetDragHandler`][4] uses some data coming from deep within the particle and circuit rendering system in order to apply the partition set position changes.



[1]: xref:AS2.Visuals.RendererUI
[2]: xref:AS2.UI.InputManager
[3]: xref:AS2.Visuals.MeshCreator_HexagonalView
[4]: xref:AS2.Visuals.PSetDragHandler
