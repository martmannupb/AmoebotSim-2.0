# Dev Guide: Particle Shader Example

As mentioned on the [Unity Rendering Basics page](~/dev_guide/render/rendering_basics.md), *shaders* are programs that define the appearance of objects by computing the color values of pixels.
When the ray cast through a pixel by the render engine hits a face of an object, a shader is invoked to compute the pixel's color.
Shaders are often highly optimized to run on GPUs, allowing many pixels to be computed in parallel.
This page explains the fundamentals of how shaders are designed in Unity.

There is a way to program shaders with code, using the *High Level Shader Language (HLSL)*, which is the native shader language to be used with DirectX.
However, a more convenient way to define shaders is [*Shader Graph*](https://docs.unity3d.com/Manual/shader-graph.html).
This is a node-based visual environment for editing shaders without having to write complex code.
The nodes in this environment represent operations that can be applied to data.
Nodes have input and output sockets with associated data types, similar to parameters and return values of methods in code.
Connecting an output socket of one node with an input socket of another node by simply dragging a line causes data to flow from the first node to the second.
There are shader output nodes that only have input sockets and shader input nodes that only have output sockets (apart from optional input sockets for parameters).

A shader is run once every time a ray hits a face of a mesh.
The data flow starts at the input nodes and travels from node to node until it reaches the output nodes.
In our examples, the output nodes have a "Vertex" component and a "Fragment" component.
The Fragment component returns an RGB color and an alpha value that is used directly to color the current pixel.
The Vertex component can be used to change the visible geometry of the object.
This output node is evaluated once for every vertex of an object before the actual rendering starts.
We only use this functionality in the `Resources/Materials/CircularView/ConnectorMat` material, which renders the rectangular connector between the parts of an expanded particle in graph view.
It moves one side of a simple rectangle mesh to animate the expansion or contraction movement.
The Vertex output can also change a vertex's normal and tangent vectors, changing the interaction of light with the object.
This feature is not used by the simulator.

Inputs to a shader can come from the current object itself (like its position in the world), mesh data (like the UV coordinates of the currently shaded point), or from custom shader parameters like colors, floats and textures.
Such custom parameters are controlled by the material that references the shader, whereas other parameters depend on the object that is hit by the ray.


## Shader Example

In the following, we go through one of the most complex shaders in the simulator to explain the ideas behind implementing a shader and to explain how the particle textures are used for rendering particles in the hexagonal views.
The example shader we use here is `Resources/Shaders/HexExpansionShaderExtended`, which is used by the `Resources/Materials/HexagonalView/HexagonCombinedMat` material.
If you understand how this shader works, you will be able to figure out how the other shaders work as well.

<img src="~/images/shader_inspector1.png" alt="Material Inspector" title="Material Inspector" width="300" align="right"/>
Selecting the material in the Project window will reveal both the shader and its parameters in the Inspector window.
Clicking the `Edit...` button in the top right corner (or double-clicking the `.shadergraph` file) opens the Shader Graph Editor window.
You can detach the window from the Unity Editor by dragging the tab away, then it can be maximized.

<img src="~/images/shader_editor.png" alt="Shader Graph Editor" title="Shader Graph Editor"/>

In the Shader Graph Editor window, you can pan around by clicking and dragging with the middle mouse button or Alt + left mouse button, and you can zoom using the scroll wheel.
Most shader graphs in this project will have a big note containing a documentation text just like in the image above.


### Conceptual Overview

We first explain how the shader works in general and then present how the concepts are implemented using shader graph nodes.
To start with, the mesh that we will be working with is a simple quad which is 3 units wide and 2 units high.

<img src="~/images/hex_particle_mesh.png" alt="Hexagon Particle Mesh" title="Hexagon Particle Mesh" width="350"/>

It is created by the [`MeshCreator_HexagonalView`][1] in such a way that its pivot is exactly $0.5$ units left of the center, i.e., between the left third and the middle third.
In this position, the center of the particle's body should be drawn.
When the particle is expanded, the other part of its body should be drawn one unit to the right, i.e., between the middle third and the right third.
The quad mesh rotates around its pivot, so if it is rotated by a multiple of 60 degrees, the right part of the expanded particle will line up exactly with the corresponding neighbor node because the edge length of the background is also one unit.

The UV coordinates of the quad's vertices are $(0,0)$, $(1,0)$, $(1,1)$, $(0,1)$, starting at the bottom left and going in counter-clockwise direction.
This means that textures in the unit square will be stretched to fit the entire quad.
We will need a way to counteract this stretching when it comes to placing textures on the mesh later on.

#### Inputs

The main inputs of the shader are the `InputColor` and five textures, as can be seen in the Inspector (see image above).
The `InputColor` parameter will be the fill color.
A color set by the particle algorithm will override this through the property block of a [`RendererParticles_RenderBatch`][3].
The input textures define the shapes that make up the particle body (or the pin positions if a pin overlay is rendered).
Two textures are used for the stationary part of the particle (at the mesh's pivot), two textures are used for the other part, indicated by the suffix `2`, and one texture is used for the connecting piece between the particles.
All textures have a transparent background, the shape's borders are solid black, and their fill color is solid white.
When the application is running, the first four textures are replaced by textures containing pins.
In the round hexagonal view mode, the body shape is also replaced by a circle.
This is all done by the [`TextureCreator`][2] and it allows the shader to render particles both in the hexagonal and the round hexagonal view mode.
The [`TextureCreator`][2] also creates a new copy of the material every time an algorithm with a different number of pins is loaded.

<img src="~/images/shader_inspector2.png" alt="Shader Parameters" title="Shader Parameters" width="300" align="right"/>

The next interesting set of inputs are the animation controls, indicated by the `Anim` prefix.
The trigger time is the time stamp at which the animation starts and the duration determines how long the animation should take from start to finish.
In this time frame, the animation percentage is interpolated smoothly between the two given percentages, where a percentage of $0$ means that the particle is contracted and $1$ means the particle is expanded.

Most of the other inputs are for fine-tuning the scaling and positions of the textures.

#### Composition

To draw the particle body, we will render three things onto the mesh.
First, we draw the non-moving part of the particle using one of the first two textures (we can ignore the `100P` version for now).
This part is always drawn in the same place, at the origin of the quad mesh.
Next, we draw the moving part of the particle using one of the second pair of textures.
This part has to be offset to the right according to the current animation percentage.
Third, we draw the connection piece between the two particles.
We will have to control the scale of this piece such that its left side is in the non-moving part and its right side is in the moving part.

<img src="~/images/shader_example_body1.png" alt="Non-moving body part" title="Non-moving body part" height="100"/> <img src="~/images/shader_example_body2.png" alt="Moving body part" title="Moving body part" height="100"/> <img src="~/images/shader_example_body3.png" alt="Connector part" title="Connector part" height="100"/>

Finally, the three parts have to be mixed somehow.
For this, to calculate the color value at a given pixel, we take the *maximum color value* of the three rendered parts.
This means that the combined color has the maximum R, G, B and alpha values of the three calculated colors.
To get the final output, we also multiply this combined color with the input color.
This turns white parts into the input color and black or transparent parts stay black or transparent.


### Implementation

We will now explain how these ideas are implemented using Shader Graph.

#### Body Texture Scaling and Placement

We start by computing scaling factors for the body textures.

<img src="~/images/shader_scaling.png" alt="Texture Scaling" title="Texture Scaling" width="600"/>

Here, we first turn the $(x, y)$ input vector `TilingScale` into the vector $(1/x, 1/y)$ using the `Divide` node for the divisions and `Split` and `Combine` nodes to access the vector's components individually and combine them into a vector again.
We then multiply the result by the global scale factor `Scale` to obtain the scaling factor $(s_x, s_y)$ for the hexagon textures.
Later, we will see which values we use for the inputs to counteract the texture stretching mentioned before.
The `UV` node that can also be seen above returns the current UV coordinates (channel 0), which we will need to place the textures correctly on the mesh.

Next, we "load" (or rather "sample") the textures for the non-moving part of the particle.

<img src="~/images/shader_load_tex1.png" alt="Sampling the first texture" title="Sampling the first texture" width="600"/>

The `Sample Texture 2D` node returns the color value of the texture connected to its `Texture` input socket, sampled at the given UV coordinates.
As mentioned above, if we plug in the UV coordinates returned by the `UV` node, the texture will be stretched to match the whole quad mesh.

<img src="~/images/shader_example_unscaled.png" alt="Body texture with default UV coordinates" title="Body texture with default UV coordinates" width="300"/>

To counteract this, we use our scaling vector $(s_x, s_y)$ to scale the UV coordinates, which is equivalent to scaling the texture on the mesh.
First, the `TilingScale` input is set to $(1.5, 1)$, which matches the aspect ratio of the mesh (recall that the quad is 3 units wide and 2 units high).
This means that the square hexagon texture will not be stretched by the UV projection if we multiply the UV coordinates by these values.
However, since the scaling pivot is in the bottom left of the texture by default, we also set the `ScalePivotOffset` input to $(0.5, 0.5)$, moving the scaling pivot to the texture's center.
Thus, the texture will be placed in the middle of the quad and its top and bottom sides will touch the quad's top and bottom (note that the texture was not scaled on the Y axis).

<img src="~/images/shader_example_scaled.png" alt="Body texture with scaled UV coordinates" title="Body texture with scaled UV coordinates" width="300"/>

We also want to move the texture to the left, so that it does not appear in the center of the mesh but at its origin position.
The `MeshRelativeUVOffset` input is used to define this offset.
Its value is $(-1/6, 0)$, which makes sense because we want to move the hexagon half a unit to the left.
Since we are moving it in the quad's UV space, where one unit on the X axis equals 3 world units, we have to divide the half world unit by $3$.

<img src="~/images/shader_example_scaled_offset.png" alt="Body texture with scaled UV coordinates and offset" title="Body texture with scaled UV coordinates and offset" width="300"/>

All of these values are plugged into the `TextureScaleAndOffset` node, which is a custom shader subgraph (a shader graph that can be reused in other shader graphs).
Subgraphs can be opened in the Shader Graph Editor by double-clicking their node.
This subgraph modifies the given original UV coordinates by applying the given scale around the given scale pivot position and offsetting the coordinates by the given offset, resulting in exactly the changes we described above.
Back in the main shader, the resulting UV coordinates are then used to sample the `TextureHexagon` and `TextureHexagon100P` input textures.
We will later decide which one of the two sampled colors to use.

#### Body Texture Animation

The textures for the moving part of the particle's body are sampled in the same way as the first textures, only using a different relative offset that is based on the animation percentage.

<img src="~/images/shader_load_tex2.png" alt="Sampling the second texture" title="Sampling the second texture" width="600"/>

The `LerpingMovementSubgraph` is a custom shader subgraph that uses the provided animation parameters to compute an animated offset.
If $p$ is `AnimPercentage1`, $q$ is `AnimPercentage2` and $v$ is `AnimMeshRelativeUVOffset`, then the subgraph computes a factor $\alpha$ that is interpolated between $p$ and $q$ and returns $\alpha \cdot v$.
Internally, the subgraph uses the `Time` input node to access the current time.
If the `AnimTriggerTime` has not been reached yet (i.e., before the animation starts), $\alpha$ will be $p$.
If the time already exceeds `AnimTriggerTime` + `AnimDuration` (i.e., after the animation ends), $\alpha$ will be $q$.
Otherwise, $\alpha$ is interpolated smoothly between $p$ and $q$, i.e., it will start at $p$, accelerate until it reaches the half-way point between $p$ and $q$, and then decelerate until it reaches $q$.
The smooth interpolation is handled by the `SmoothLerp` node, which uses custom shader functions defined in `Assets/Code/hlsl/SmoothLerp.hlsl` using HLSL.

The resulting offset $\alpha \cdot v$ is then added to the `MeshRelativeUVOffset`, which is the offset that we also applied to the first texture.
If set set `AnimMeshRelativeUVOffset` to $(1/3,0)$, this leads to the correct expanded position when $\alpha = 1$.
This way, setting $p$ and $q$ correctly, we can control the type and direction of the movement animation:
$p = q = 0$ means that the particle is contracted and does not move.
Similaraly, for $p = q = 1$, the particle is expanded and does not move.
For $p = 0, q = 1$, the particle expands, and for $p = 1, q = 0$, the particle contracts.
The [`RendererParticles_RenderBatch`][3] can later use property blocks to set these parameters to achieve the correct animation type.

#### Connector Texture Animation

The node tree for sampling the connector texture is very similar to the one for the moving body texture:

<img src="~/images/shader_load_tex3.png" alt="Sampling the connector texture" title="Sampling the connector texture" width="600"/>

The main difference is that instead of animating the offset, we animate the scale.
We use the `LerpingMovementSubgraph` again to interpolate between the two given animation percentages, but the result is not scaled by a vector this time.
Instead, we scale the result by the constant float $1$, meaning that the subgraph's output will be $\alpha$.
This value is then used to linearly interpolate between the scaling vector $(\varepsilon, s)$ and $(1, s)$, where $\varepsilon$ is a small value greater than $0$ and $s$ is the given global `Scale` factor.

The scaling of the connector texture works as follows:
The actual connector image inside the texture (`Resources/Images/Hexagons/HQ/Rectangle1_1024.png`) takes exactly the central third of the square texture's width.
Thus, stretching the texture on the X axis to fit the quad mesh does not distort the image - it even leads to the connector's two ends being placed exactly where the centers of the two parts of the expanded particle are (exactly the borders of the middle third).
Using a scale pivot point of $(1/3, 1/2)$ therefore means that the texture will be scaled up towards the right by the animation.
The global `Scale` factor then only changes the connector's width.

#### Choosing the Hexagon Texture

After sampling the two textures for the non-moving and the moving part, we decide which one to use.

<img src="~/images/shader_anim_percentage.png" alt="Animation percentage" title="Animation percentage" height="225"/> <img src="~/images/shader_branch.png" alt="Selecting the texture" title="Selecting the texture" height="225"/>

For this, we first calculate the smoothed animation percentage again, using the `AnimationPercentage` subgraph.
This is the same calculation as done by the `LerpingMovementSubgraph` but without the extra multiplication.
The result is then compared to $1$ and to $0$ using two `Comparison` nodes.
Using a logical `Or` node, we combine the comparison results, obtaining a `True` result if the animation percentage is exactly $1$ or exactly $0$, and `False` otherwise.
This result is used as the predicate of two `Branch` nodes to decide which texture sample should be used.
If the animation percentage is $1$ or $0$, we use the textures with the `100P` suffix, otherwise, we use the normal textures, allowing us to use different textures during the animation than when the particle is static.

> [!NOTE]
> Originally, this feature was used to hide the pins during the movement animations, but it was decided later that the pins should also be visible during the animation.
> The feature was kept in the shader to allow changing this later on, if necessary.


#### Combining the Colors

Having sampled the two textures and the connector piece, we can now merge their color values to obtain the final output.

<img src="~/images/shader_combine.png" alt="Combining the samples" title="Combining the samples" width="600"/>

To this end, we first use two `ExpansionMergeSubgraph` nodes to combine the three sampled color values.
This is another custom subgraph that simply returns the maximum value in each channel (R, G, B and alpha) of the two given colors.

<img src="~/images/shader_merge.png" alt="ExpansionMergeSubgraph" title="ExpansionMergeSubgraph" width="600"/>

We then multiply the resulting color with the `InputColor` to obtain the final result.
Because the input textures are black and white, this simply replaces the white parts of the textures with the `InputColor`.
The result is then returned as the Fragment shader output.

This last step achieves the smooth expansion and contraction effect that would be very difficult to achieve outside of a shader.
The white color of the textures overrides their black borders in the maximum operation, resulting in a combined shape with a white fill color and a continuous black border that is composed of all three textures.




[1]: xref:AS2.Visuals.MeshCreator_HexagonalView
[2]: xref:AS2.Visuals.TextureCreator
[3]: xref:AS2.Visuals.RendererParticles_RenderBatch
