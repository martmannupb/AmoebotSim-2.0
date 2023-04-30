# Dev Guide: Particle Shader Example

- This page explains how a shader is defined using shadergraph
	- We use the hexagonal particle shader as an example
	- This is one of the more complex shaders
	- If you understand how this shader works, you will be able to figure out how the others work as well
- The material using this shader is `Resources/Materials/HexagonalView/HexagonCombinedMat.mat`
	- Selecting it will reveal both the shader and its parameters in the Inspector window
	<img src="~/images/shader_inspector1.png" alt="Material Inspector" title="Material Inspector" width="300" align="right"/>
	- The used shader is `Resources/Shaders/Hexagonal/HexExpansionShaderExtended.shadergraph`
	- Clicking the `Edit...` button in the top right corner (or double-clicking the `.shadergraph` file) opens the shadergraph editor window (you can detach it from the Unity Editor by dragging the tab away, then you can maximize it)
	<img src="~/images/shader_editor.png" alt="Shadergraph Editor" title="Shadergraph Editor"/>
		- You can pan around using the middle mouse button or Alt + Left Click and zoom using the scroll wheel
	- Most shadergraphs will have a big note containing a documentation comment
- Conceptual overview
	- First, explain how the shader works in general, then present how it is implemented
	- We first need to know what mesh we will be working with
	<img src="~/images/hex_particle_mesh.png" alt="Hexagon Particle Mesh" title="Hexagon Particle Mesh" width="350"/>
	- The mesh is a simple quad of width 3 and height 2
		- It has been created by the [`MeshCreator_HexagonalView`][1]
		- The pivot is exactly 0.5 units left of the center, i.e., between the left and middle third
		- In this position, the center of the particle's body should be drawn
		- When the particle is expanded, the other part of its body should be drawn one unit to the right, i.e., between the middle and the right third
			- The quad rotates around its pivot, so if it is rotated by a multiple of 60 degrees, the right part of the expanded particle will line up with the corresponding neighbor node because it is exactly one unit away from the pivot
		- The UV coordinates of the vertices are $(0,0)$, $(1,0)$, $(1,1)$, $(0,1)$, starting at the bottom left and going in counter-clockwise direction
			- This means that textures will be stretched to fit the entire quad
			- We will have to counteract this later on
	- The main inputs of the shader are the `InputColor` and five textures (see Inspector image above)
		- `InputColor` will be the fill color
			- The color set by the particle algorithm will later override this through a property block
		- The textures define the shapes that should be drawn
			- Two textures for the stationary part (on the left)
			- Two textures for the moving part (moving to the right, indicated by the suffix `2`)
			- One texture for the connecting piece between the particles
			- The textures have a transparent background, the shape borders are black, and their fill color is white
			- When the application is running, the first four textures are replaced by textures that contain pins
				- Or by round textures, also with pins
				- These are created by the [`TextureCreator`][2]
				- Thus, the shader can render the hexagonal and rounded hexagonal particles
				- If an algorithm with a different number of pins is loaded (or the circuit overlay is disabled), a new copy of the material that uses updated textures is created
		- Animation timing data (see below)
			- Trigger time: The time at which the animation starts
			- Duration: The duration of an animation
			- Percentage 1 and 2: Start and end expansion percentage
			- The shader will play an animation that interpolates the expansion percentage from value 1 to value 2, starting at the trigger time and taking the specified duration
		- Other inputs:
		<img src="~/images/shader_inspector2.png" alt="Shader Parameters" title="Shader Parameters" width="300"/>
			- Most other inputs are for fine-tuning the scaling and positions of the textures
	- We will render three things onto the mesh
		- First, we draw the non-moving part of the particle using the first texture (ignore the 100P version for now)
			- This part is always drawn in the same place, at the origin of the quad mesh
		- Second, we draw the moving part of the particle using the second texture
			- This part has to be offset to the right according to the current progress of the animation
		- Third, we draw the connection piece between the two particles
			- The scale of this piece is controlled such that its left side is in the center of the non-moving part and its right side is in the center of the moving part
	- Finally, the three parts have to be mixed somehow
		- To calculate the color value at the current position, we take the _maximum value_ of the three parts
		- I.e., the combined color has the maximum R, G, B and alpha values of the three calculated colors
	- To get the final output, we multiply the combined color with the input color
		- This turns white parts into the input color and transparent/black parts stay black/transparent
- Implementation
	- Compute scaling for hexagon textures
		<img src="~/images/shader_scaling.png" alt="Texture Scaling" title="Texture Scaling" width="600"/>
		- We first turn the $(x, y)$ vector `TilingScale` into the vector $(1/x, 1/y)$, using the `Divide` node for the divisions and `Split` and `Combine` nodes to access the input vector's components individually and combine them into a vector again
		- Then we multiply the result by the global scale (`Scale`) to obtain the scaling vector $(s_x, s_y)$ for the hexagon textures
		- The `UV` node returns the current UV coordinates (of channel 0), which we will need to place the textures correctly
	- Load the hexagon textures
		- "Loading textures" is not accurate, it should rather be "sampling the textures" because we are computing the color value of a single pixel
		<img src="~/images/shader_load_tex1.png" alt="Sampling the first texture" title="Sampling the first texture" width="600"/>
		- The `Sample Texture 2D` node returns the color value of the texture plugged into its `Texture` input, sampled at the given UV coordinates
		- If we plug in the UV coordinates returned by the `UV` node, the texture will be stretched to match the whole quad mesh (as mentioned before)
		- We will use our scaling vector $(s_x, s_y)$ to scale the UV coordinates, which results in scaling the texture on the mesh
			- The `TilingScale` input is set to $(1.5, 1)$ by default, which matches the aspect ratio of the mesh
			- This means that the hexagon texture (which is square) will not be stretched by the UV projection
			- Since the scaling pivot is the bottom left of the texture by default, we also set the `ScalePivotOffset` to $(0.5,0.5)$, scaling the texture around its center
			- Thus, the texture will be placed in the middle of the quad and its top and bottom sides will touch the quad's top and bottom (since it was not scaled on the Y axis)
		- We also want to move the texture to the left, so that it does not appear in the center of the mesh but at its origin position
			- The `MeshRelativeUVOffset` defines this offset
			- Its default value is $(-1/6,0)$, which makes sense because we want to move the hexagon half a unit to the left
				- Since we are moving it in the quad's UV space, where one unit on the X axis equals 3 world units, we have to divide the half world unit by 3
		- The `TextureScaleAndOffset` node is actually a custom shader subgraph which can be viewed by double-clicking the node
			- It modifies the given original UV coordinates by applying the given scale around the given scale pivot position and offsetting the coordinates by the given offset
			- The resulting UV coordinates are then used to sample the `TextureHexagon` and `TextureHexagon100P` input textures
			- We will later decide which of the values to use
	- Animating the second texture
		- The texture for the moving part is sampled in the same way as the first texture, only using a different relative offset
		<img src="~/images/shader_load_tex2.png" alt="Sampling the second texture" title="Sampling the second texture" width="600"/>
		- The `LerpingMovementSubgraph` is also a custom shader subgraph that uses the given animation parameters to compute an animated offset
		- If $p$ is `AnimPercentage1`, $q$ is `AnimPercentage2` and $v$ is `AnimMeshRelativeUVOffset`, then the subgraph computes a factor $\alpha$ that is interpolated between $p$ and $q$ and returns $\alpha \cdot v$
			- Internally, it uses the `Time` node to access the current time
			- If the `AnimTriggerTime` has not been reached yet, $\alpha$ will be $p$
			- If the time already exceeds `AnimTriggerTime` + `AnimDuration`, $\alpha$ will be $q$
			- Otherwise, $\alpha$ is interpolated smoothly between $p$ and $q$, i.e., it will start at $p$, accelerate until it reaches the half-way point between $p$ and $q$, and then decelerate until it reaches $q$
			- The smoothed interpolation is handled by the `SmoothLerp` node, which uses custom shader functions defined in `Assets/Code/hlsl/SmoothLerp.hlsl`
		- The resulting offset is added to the `MeshRelativeUVOffset` (the offset that is applied to the first texture)
			- Using $(1/3,0)$ as `AnimMeshRelativeUVOffset` leads to the correct position for $\alpha = 1$
		- The type and direction of the movement can be controlled by setting $p$ and $q$ correctly:
			- $p = q = 0$ means the particle is contracted (no animation)
			- $p = q = 1$ means the particle is expanded (no animation)
			- $p = 0, q = 1$ means the particle expands
			- $p = 1, q = 0$ means the particle contracts
	- Animating the connector texture
		- The node tree for sampling the connector texture is very similar to the one for the second texture
		<img src="~/images/shader_load_tex3.png" alt="Sampling the connector texture" title="Sampling the connector texture" width="600"/>
		- The main difference is that instead of animating the offset, we animate the scale
		- We use the `LerpingMovementSubgraph` again to interpolate between the two given animation percentages, but the result is multiplied by 1 this time, meaning that we only get the value $\alpha$ as output
		- $\alpha$ is then used to linearly interpolate between the scaling vectors $(\varepsilon,s)$ and $(1,s)$, where $\varepsilon$ is a small value greater than 0 and $s$ is the given global `Scale` factor
		- The scaling of the connector texture is also different
			- The actual connector image inside the texture (`Resources/Images/Hexagons/HQ/Rectangle1_1024.png`) takes exactly the central third of the texture's width
			- Stretching the texture on the X axis to fit the quad mesh does not distort the image and it leads to the connector's two ends being placed exactly where the centers of the particle parts are
			- Using a scale pivot of $(1/3,1/2)$ means that the texture will be scaled up towards the right by the animation and the `Scale` factor only changes the connector's width
	- Choosing the correct hexagon texture
		- After loading two textures for the non-moving and for the moving part, we decide which one to use

		<img src="~/images/shader_anim_percentage.png" alt="Animation percentage" title="Animation percentage" height="225"/> <img src="~/images/shader_branch.png" alt="Selecting the texture" title="Selecting the texture" height="225"/>
		- We first calculate the smoothed animation percentage using the `AnimationPercentage` subgraph
			- This is the same calculation as done by the `LerpingMovementSubgraph` but without the extra multiplication
		- The result is then compared to 1 and 0 using two `Comparison` nodes
		- Using a logical `Or` node, we combine the comparison results, obtaining `True` if the animation percentage is 0 or 1 and `False` otherwise
		- This predicate is then used in two `Branch` nodes to decide which texture sample should be used
			- If the animation percentage is 0 or 1, we use the texture with the `100P` suffix, otherwise, we use the normal texture
	- Combining the colors
		- After sampling the two textures and the connector piece, we merge their color values to obtain the final output
		<img src="~/images/shader_combine.png" alt="Combining the samples" title="Combining the samples" width="600"/>
		- We use two `ExpansionMergeSubgraph` nodes to combine the three sampled values and multiply the result with the `InputColor`
			- Because the input textures are black and white, this will replace the white parts with the `InputColor`
		- The resulting color is returned as the fragment shader output
			- The alpha channel has to be supplied in a separate socket
		- The `ExpansionMergeSubgraph` is another custom subgraph that simply takes the maximum value in each channel of the two given colors
		<img src="~/images/shader_merge.png" alt="ExpansionMergeSubgraph" title="ExpansionMergeSubgraph" width="600"/>
		- This last step achieves the smooth expansion and contraction effect that would otherwise be very difficult to achieve
			- The white color of the textures overrides their black borders, which results in a combined shape with a white fill color and a continuous black border




TODO



[1]: xref:AS2.Visuals.MeshCreator_HexagonalView
[2]: xref:AS2.Visuals.TextureCreator

[3]: xref:AS2.Visuals.MeshCreator_CircularView
[4]: xref:AS2.Visuals.RendererParticles
[5]: xref:AS2.Visuals.RendererParticles_RenderBatch
