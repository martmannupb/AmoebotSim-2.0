# Dev Guide: Unity Rendering Basics

To understand how the render system works and why it is necessary to put so much effort into optimizing its structure, we must first get a basic understanding of how rendering works in Unity (and computer graphics in general).
This page goes through the basic concepts that are most relevant to the simulation environment's render system.


## Meshes

<img src="~/images/cube_mesh.png" alt="Cube Mesh" title="Cube Mesh" width="450" align="center"/>

The *shape* of an object that should be rendered is defined by a [*mesh*](https://docs.unity3d.com/Manual/mesh-introduction.html).
A mesh is geometrical data consisting of a set of vertices in 3D space and a set of triangles between these vertices.
The triangles are also called *faces*.
In very simple terms, when an image is rendered, the render engine casts rays from a camera object into the 3D world and determines which face is hit first by each ray.
Every ray starts at the location of a pixel and the first face hit by the ray is used to determine the color of the pixel.
Thus, the positions of the objects in the scene determine whether an object is rendered in front of another.
If a ray does not hit any faces, the pixel will get a default background color, which is white in the simulation environment (in many 3D applications, the default background is a "skybox").

### 2D World and Camera

Since the Amoebot model is two-dimensional, we do not need to use all of Unity's 3D rendering capabilities.
Instead, we use an *orthographic* camera, which means that the rays cast by this camera are parallel, causing objects to appear in the same size no matter how far away they are from the camera.
Additionally, all meshes used by the simulator are perfectly flat, making the dynamic generation of meshes and the creation of shaders relatively simple.

The camera is oriented such that its rays are cast along the global Z axis.
The rendered objects in the scene are all aligned to the X-Y-plane, meaning that they are orthogonal to the Z axis.
Thus, an object's Z coordinate (or *Z layer*) determines whether it is rendered in front of or behind other objects.
The camera has a default Z coordinate of $-10$ and points in the positive Z direction.
This means that objects on larger Z layers are drawn further in the background.
Objects on Z layers less than $-10$ are behind the camera and are thus not in view.

### Additional Mesh Data

Meshes usually contain more data than just vertices and triangles.
One kind of additional data that is often used are so-called *UV coordinates*.
UV coordinates are simply vectors that are associated with each vertex of the mesh.
There can be multiple UV channels, i.e., multiple vectors associated with each vertex, and each channel can have a different dimension.
The first UV channel (channel 0) is most commonly used.
It usually stores two-dimensional vectors that are used to map 2D textures to a mesh.
For this, the 2D vectors are interpreted as coordinates on (or around) the unit square.
We then have a [*UV map*](https://en.wikipedia.org/wiki/UV_mapping) that projects the mesh to a flat surface: Each vertex and, by extension, each triangle has a position on the 2D plane.
If we now add color data (a *texture*) to the plane, every vertex and every point on each face has some associated color data.
Usually, image textures are mapped onto the unit square and then repeated infinitely to fill the plane.
This color information can then be used to color the mesh.
For 2D objects, the mapping is straightforward since the vertices are already on a plane - it usually suffices to scale the mesh so that it fits into the unit square.
For 3D objects, a common way to explain UV mapping is to think of the mesh as a paper model that is cut and then pressed onto a flat surface.
Oftentimes, however, a UV mapping results in scaling and shearing of some triangles, which is not covered by the paper analogy.

UV data does not have to be used for UV mapping, though.
It can be used for any purpose that suits the application.
For example, one dimension of a UV channel can be used as a "weight" associated with each vertex to determine how much a vertex is affected by some operation.
This is very useful for custom shaders that can access the UV data directly (see below).


## Shaders

<img src="~/images/shadergraph_example.png" alt="A shader defined using shadergraph" title="A shader defined using shadergraph" width="450"/>

When a ray cast by the render engine hits a face, a color value must be calculated.
The program that calculates this color is called a [*shader*](https://en.wikipedia.org/wiki/Shader).
Shaders are often highly optimized to run on GPUs, allowing the colors of many pixels to be calculated in parallel.
Every time a ray hits a face of a mesh, the shader associated with the mesh is called to calculate an RGBA color, using the position of the ray cast hit on the mesh as an input.
If the alpha value of the resulting color is less than 1, the ray is cast further and the resulting color is mixed with the first color, creating a transparency effect.
This way, an object can be seen "through" another object even if the object's mesh is completely occluded by the other's.
Shaders can even change the visible position of vertices without changing the object's position or its mesh, enabling more complex effects like simple animations.

Shaders in Unity (and in most other 3D applications) can be defined using a visual node-based language that is much easier to use than writing code.
Unity's version of this is called Shadergraph (see image above).
This system is explained on a [separate page](particles/shader_example.md), using the main particle shader of the simulator as example.



TODO




## Materials

- Shaders are assets that only exist once in the project
	- Changing the parameters of a shader would change the result for all objects using this shader
- We want to be able to reuse a shader with different parameters
- A *material* is basically a collection of parameters associated with a shader
	- Materials are also assets that only exist once
	- But multiple materials can use the same shader with different parameters
	- Materials are assigned to objects, not the shaders themselves
	- Example: All materials for the tool overlays use the same shader, just with different colors
- Unity can render many copies of a mesh in different positions efficiently if they all share the same material
- There is an extra mechanism that allows rendering meshes with the same base material but different parameters
	- A MaterialPropertyBlock is a collection of parameters without an associated shader
	- It can be passed to a render call to temporarily overwrite the shader parameters stored in a material
		- This only affects the meshes rendered in that call
- A lot of optimization work aims at grouping objects that use the same mesh and material together to draw them in parallel using the GPU
	- The [`DrawMeshInstanced`][2] method allows up to 1023 instances to be drawn simultaneously


## Matrices and Instanced Drawing

- Usually, rendering is handled implicitly because rendered objects are GameObjects with attached Mesh and Material components that are rendered automatically
- But rendering hundreds or thousands of GameObjects quickly becomes inefficient and does not offer the flexibility we need, so we issue the render calls manually
- The basic method call used to submit an object to the render engine programmatically is [`Graphics.DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, ..., MaterialPropertyBlock properties, ...)`][1].
	- `mesh` is the mesh to be rendered
	- `matrix` is a 4x4 matrix containing translation, rotation and scaling data
		- It defines where the `mesh` is located, how it is oriented and how it is scaled
		- This is usually the information that needs to be updated and/or animated most often
	- `material` is the material used for shading the `mesh`
	- `properties` is an optional block of material properties overriding the `material`'s default parameters
- Unity is able to process multiple render calls in parallel using the GPU, if they use the same mesh and material
	- For this, we call [`Graphics.DrawMeshInstanced(Mesh mesh, ..., Material material, Matrix4x4[] matrices, ..., MaterialPropertyBlock properties, ...)`][2] instead
		- `mesh`, `material` and `properties` have the same purpose as before
		- Instead of one matrix, we supply an array of matrices
		- Up to 1023 instances of a mesh can be drawn this way
	- This is much more efficient than calling [`DrawMesh(...)`][1] multiple times
- In the rendering code, we try to submit render calls in _batches_ as often as possible
	- I.e., used instanced draw calls for as many matrices at once as possible
	- Need some complex structures to accomplish this




TODO

[1]: https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html
[2]: https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html

