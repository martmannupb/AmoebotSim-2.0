# Dev Guide: Particle Rendering

- The [`RendererParticles`][1] class is the root of a sub-tree responsible for rendering the particles as well as bonds and circuits
	- This is a very complex system with far too many features to explain everything in detail
- [`RendererParticles`][1] manages its child renderers and forwards the render calls
	- A dictionary containing [`RendererParticles_RenderBatch`][2] instances indexed by [`RendererParticles_RenderBatch.PropertyBlockData`][3] structs
		- Each render batch renders all particles with the same properties
		- [`PropertyBlockData`][3] contains color and number of pins per side
			- I.e., all particles with the same color and the same number of pins per side are rendered by the same batch
	- A single [`RendererCircuitsAndBonds`][4] instance
		- This is also the root of a sub-tree that handles the rendering of all bonds and circuits in the system
		- (Covered on separate pages)
- The class also keeps track of all currently registered particles
	- Stored in a dictionary mapping [`IParticleState`][5] to [`ParticleGraphicsAdapterImpl`][6]
	- [`IParticleState`][5] is implemented by the particle class and allows the render system to read some general properties of the particle (like its position, expansion state, compass direction and chirality) and even change some of its properties
	- [`ParticleGraphicsAdapterImpl`][6] is a class containing all visualization-specific data associated with a particle
		- Every particle has one instance of this class that it can use to push visualization updates to the rendering system





TODO





[1]: xref:AS2.Visuals.RendererParticles
[2]: xref:AS2.Visuals.RendererParticles_RenderBatch
[3]: xref:AS2.Visuals.RendererParticles_RenderBatch.PropertyBlockData
[4]: xref:AS2.Visuals.RendererCircuitsAndBonds
[5]: xref:AS2.Visuals.IParticleState
[6]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl


[7]: xref:AS2.AmoebotSimulator
[8]: xref:AS2.Visuals.RenderSystem.Render
[9]: xref:AS2.Visuals.RendererUI
[10]: xref:AS2.Visuals.RendererBackground
[11]: xref:AS2.Visuals.RenderSystem
