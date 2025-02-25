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

Since the amoebot model is two-dimensional, we do not need to use all of Unity's 3D rendering capabilities.
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

<img src="~/images/shadergraph_example.png" alt="A shader defined using shadergraph" title="A shader defined using shadergraph" width="500"/>

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


## Materials

Oftentimes, an application has multiple objects that should look very similar but not exactly the same, like the particles in the simulator that should all have similar shapes but can have different colors.
Unfortunately, we cannot simply give each object a copy of the same shader and change the copy's parameters since this would have a significant performance impact.
Every time the render engine encounters an object with a different shader than the previous object, it has to issue a new render call to the GPU, which is a very expensive operation.
Additionally, we want to be able to modify the original shader and have those changes affect all other users of the shader as well.
The solution to this are so-called *materials*.

Shaders are assets that only exist once in the project but that can be used by multiple objects.
Changing the shader asset affects the appearance of all objects using this shader.
A shader can also define parameters that change the shader's behavior and output.
For example, a shader could have a color parameter that controls the base color of the rendered object.
A *material* is essentially a collection of parameters associated with a shader.
Instead of assigning shaders to objects, we assign materials to objects, allowing us to use the same shader to render multiple objects with different shader parameters.
A good example for this are the tool overlays marking positions in Init Mode: They use materials with the same shader but different color parameters.
Changing the opacity in the shader would affect the opacity of all tool overlays.

<img src="~/images/shader_tool_add.png" alt="Add tool is green" title="Add tool is green" width="200"/> <img src="~/images/shader_tool_remove.png" alt="Remove tool is red" title="Remove tool is red" width="200"/> <img src="~/images/shader_tool_move.png" alt="Move tool is purple" title="Move tool is purple" width="200"/>

A very useful feature of materials in Unity is that objects that share the same material can be batched together and rendered with much fewer render calls to the GPU than if all objects had their own materials.
Thus, a lot of optimization work aims at grouping objects with the same material and, in many cases, even the same mesh, so that they can be rendered together (see below for more details).

There is also a mechanism that allows rendering objects with the same base material but different parameters for cases where material parameters need to be modified dynamically or creating separate materials would not be feasible.
A `MaterialPropertyBlock` is a collection of parameter values that can override a material's parameters for a single render call.
Using property blocks is much more efficient than creating new materials at runtime


## Matrices and Instanced Drawing

In many Unity applications, rendering is handled implicitly because rendered objects are GameObjects with attached Mesh and Material components that are rendered automatically.
But rendering hundreds or thousands of GameObjects quickly becomes inefficient and difficult to organize, especially when only a few distinct shapes have to be drawn with slightly different properties.
Thus, most of the rendering in the simulator is handled manually.

The basic method call used to submit an object to the render engine programmatically is [`Graphics.DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, ..., MaterialPropertyBlock properties, ...)`][1].
Here, `mesh` is the mesh defining the shape to be rendered, `material` is the material defining the shader and its parameters, and `properties` is a property block that can temporarily override some of these parameters.
The `matrix` parameter is a $4 \times 4$ matrix that contains translation, rotation and scaling data (also called a TRS matrix).
This matrix corresponds to the `Transform` component that every GameObject has and it defines where the `mesh` is located in 3D space, how it is rotated, and its scale.

If we want to draw multiple copies of a mesh with the same material and property block but different TRS matrices, Unity offers a method that can submit many of these render calls simultaneously using *instancing*.
Instancing means that the GPU only requires one copy of the mesh and material data to draw many copies of the same mesh in different places efficiently.
For this, we call [`Graphics.DrawMeshInstanced(Mesh mesh, ..., Material material, Matrix4x4[] matrices, ..., MaterialPropertyBlock properties, ...)`][2], which takes a whole array of TRS matrices instead of just a single one.
This method can draw up to 1023 instances of a mesh in a single call, which is much more efficient than calling [`DrawMesh(...)`][1] multiple times.
In the rendering code, we try to submit render calls in such *batches* as often as possible by grouping objects with the same mesh and material.
To accomplish this, we use the classes described in the rest of this Dev Guide section.



[1]: https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html
[2]: https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html
