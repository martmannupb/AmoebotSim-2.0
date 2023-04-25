# Dev Guide: Particle Render Batches

- The [`RendererParticles`][1] class stores one [`RendererParticles_RenderBatch`][2] instance for each combination of color and number of pins per side
- This render batch is responsible for rendering all particles with that color and number of pins
- It also stores a dictionary of all registered particles
- It stores a number of matrices for each particle
	- The matrices are stored in lists of arrays
	- Each array has size 1023 so that it can be rendered in a single instanced draw call
	- New arrays are added to the lists if necessary
	- There is one matrix for the particle's body (head and tail), one for the "bridge"/"connector" in the graph view and an additional one for the transparent pin overlay
		- Pins are drawn twice so that circuit lines lie under pins but above particles
	- Each matrix exists 4 times
		- Once for each movement phase (contracted, expanded, contracting and expanding)
- Todo:
	- Write about the property blocks
	- Mention which meshes and materials are used



TODO




[1]: xref:AS2.Visuals.RendererParticles
[2]: xref:AS2.Visuals.RendererParticles_RenderBatch

[6]: xref:AS2.AmoebotSimulator
[3]: xref:AS2.Visuals.RenderSystem.Render
[4]: xref:AS2.Visuals.RendererUI
[5]: xref:AS2.Visuals.RendererBackground
