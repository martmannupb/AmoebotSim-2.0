# Dev Guide: Unity Rendering Basics

- Need to explain the fundamentals of how rendering in Unity works
	- Or at least how it is used in the simulation environment

## Meshes

- The shape of an object is defined by a [mesh](https://docs.unity3d.com/Manual/mesh-introduction.html).
- A mesh is geometrical data consisting of a set of vertices in 3D space and a set of triangles between the vertices
	- Triangles are also called faces
- When an image is rendered, the render engine casts a ray through each pixel of the screen and checks which face it hits first (extremely simplified)
	- The position of the objects determines which is rendered in front
	- Because we use an orthographic camera to achieve a 2D effect, all of our objects are flat, which simplifies things
	- The axis along which the rays are cast is the global Z axis
	- Every object is orthogonal to the Z axis
	- An object's Z position (Z layer) determines which objects are drawn in front of it and which are drawn behind it
- Meshes also contain additional data
	- UV coordinates
		- UV coordinates are simply vectors that are associated with each vertex of the mesh
		- There are multiple UV channels, i.e., multiple vectors associated with each vertex
		- UV channel 0 is most commonly used
			- UV data is usually a 2D vector for each vertex, but higher dimensional vectors can be used as well
		- The main application of UV coordinates is the mapping of 2D textures to a mesh
			- The 2D vectors in a UV channel are interpreted as coordinates on (or around) the unit square
			- We then have a "UV map" that projects the mesh to a flat surface
				- Note that the triangles are thereby mapped to the surface as well
			- If the unit square now contains color data (a texture), every vertex and every point on each face has some associated color data
			- This information can be used to give colors to a mesh
			- The concept of UV maps is easier for 2D objects
				- For 3D objects, a common way to explain it is to think of the mesh as a paper model that is cut and pressed onto a flat surface (although this does not account for potential scaling or shearing of the triangles)
	- Normals
		- Each vertex of a mesh also has a 3D normal vector
		- The normal vector of a vertex points away from the object's surface at the vertex position
		- This data is used to calculate how light is reflected off the object
			- Example: Perfectly round surfaces cannot be created with meshes (the mesh would need to have infinitely many vertices)
			- But if the normal vectors are interpolated between the vertices, light bouncing off the object behaves as if the object was perfectly round, giving the illusion of a smooth surface even though the mesh has sharp edges
		- Explained in more detail [here](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html)
		- *This is not relevant for the simulator but interesting to know*
			- *And it could help understanding the rest of the rendering system*


## Shaders

- When the ray cast by the render engine hits a face, a color value must be calculated
- The program that calculates this color is called a *shader*
	- Shaders can often be highly optimized to run on GPUs
- There is a way to program shaders with code but the most common way to implement a shader is *shadergraph*
	- This is a node-based visual environment for editing shaders without having to write complex code
	- The output of the shaders used in the simulator has two components
		- The "Vertex" component returns 3 3D vectors (explained later)
		- The "Fragment" component returns an RGB color and an alpha value
	- A shader is run once every time a ray hits a face of a mesh
		- The resulting "Fragment" data is used to color the corresponding pixel
		- If the alpha value is less than 1, the ray is cast further and the resulting color is mixed with the first color to create a transparency effect
	- Inputs to the shader can come from the object itself (like its position in the world), mesh data (like the UV coordinates of the currently shaded point) or custom shader parameters like colors, floats, textures, etc.
	- Nodes represent operations that can be applied to data
		- Nodes have input and output sockets with associated data types (similar to parameters and return values of methods)
	- *Maybe create a page explaining some of the shaders (like hexagon expansion)*


## Materials

- Shaders are assets that only exist once in the project
	- Changing the parameters of a shader would change the result for all objects using this shader
- We want to be able to reuse a shader with different parameters
- A *material* is basically a collection of parameters associated with a shader
	- Materials are also assets that only exist once
	- But multiple materials can use the same shader with different parameters
	- Materials are assigned to objects, not the shaders themselves
- Unity can render many copies of a mesh in different positions efficiently if they all share the same material
- There is an extra mechanism that allows rendering meshes with the same base material but different parameters
	- A MaterialPropertyBlock is a collection of parameters without an associated shader
	- It can be passed to a render call to temporarily overwrite the shader parameters stored in a material
		- This only affects the meshes rendered in that call
- A lot of optimization work aims at grouping objects that use the same mesh and material together to draw them in parallel using the GPU





TODO
