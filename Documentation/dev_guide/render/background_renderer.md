# Dev Guide: Background Renderer

- [`RendererBackground`][1] class is responsible for rendering the background grid
- Two types of background: Hexagonal and triangular (graph)
- Rendering approach:
	- Camera's background color is white
	- Only need to draw a bunch of lines with uniform color for both view types
	- Generate meshes for these lines
	- Each mesh contains multiple lines that form a repeating pattern
	- Draw multiple instances of each mesh
		- Enough instances to fill the screen
		- Use efficient instanced drawing
- [`MeshCreator_CircularView`][2] and [`MeshCreator_HexagonalView`][3] are used to generate the meshes
	- These classes also generate meshes for the particles
	- Graph view: Have horizontal and two types of diagonal lines
	- Hexagonal view: Have a row of hexagons
	- Number of lines/hexagons is chosen large enough to always cover the whole screen
		- Set in the [`RenderSystem`][4]
- [`InstancedDrawer`][5] class used to render the lines
	- Manages a list of matrices
	- Each matrix contains translation, rotation and scaling data
	- Unity's `DrawMeshInstanced` method can be used to draw copies of a mesh many times with different matrices efficiently
	- In each render call, the [`RendererBackground`][1] recomputes the matrices and passes them to the [`InstancedDrawer`][5]
		- Number of matrices changes with camera size
- Materials are `Resources/Materials/HexagonalView/HexagonGridMat` for hexagonal grid and `Resources/Materials/CircularView/BGMaterial` for triangular graph
	- Basically the same materials, just solid opaque colors



[1]: xref:AS2.Visuals.RendererBackground
[2]: xref:AS2.Visuals.MeshCreator_CircularView
[3]: xref:AS2.Visuals.MeshCreator_HexagonalView
[4]: xref:AS2.Visuals.RenderSystem
[5]: xref:AS2.Visuals.InstancedDrawer
