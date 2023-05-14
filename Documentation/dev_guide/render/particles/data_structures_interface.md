# Dev Guide: Particle Rendering Data Structures and Interface

- Still need to explain how visualization data gets from simulator/particles to the render system
- Various data structures are used to group information that is needed together somewhere
- The particle class used by the simulator implements the [`IParticleState`][1] interface
	- Provides basic state information (like position and expansion state), but also graphical information (like color)
	- And gives access to an [`IParticleGraphicsAdapter`][2] instance
	- Every particle has a [`ParticleGraphicsAdapterImpl`][3] object that serves as an interface to the render system
		- The simulator can push graphics updates using this object
			- [`AddParticle`][4] and [`RemoveParticle`][5] methods are used to add or remove particles to/from the render system
			- Registered particles must be updated using the [`Update(ParticleMovementState)`][6] method after each round
				- The [`ParticleMovementState`][7] and [`ParticleJointMovementState`][8] structs store information on a particle's movement between two rounds
				- These are converted into [`PositionSnap`][9] structs, which contain the same data with additional rendering-specific info like a timestamp and animation flag
			- Bonds and circuits are updated separately by calling the [`BondUpdate(ParticleBondGraphicState)`][10] and [`CircuitUpdate(ParticlePinGraphicState)`][11] methods
			- [`ParticleBondGraphicState`][12] is a simple struct that contains visual information on a single bond
				- Particle system must make sure that bonds are not added twice (adding bond from one side is enough)
			- [`ParticlePinGraphicState`][13] is more complex since it represents a particle's entire pin configuration
				- Data belonging to individual partition sets is stored in [`PSetData`][14] instances
					- Each instance has its own [`GraphicalData`][15] instance which stores all of the relevant material properties and render batch indices so that changes made to a partition set can be forwarded directly to the corresponding render batch
				- Individual pins are identified using the [`PinDef`][16] struct

- Additional helpers and data structures
	- The [`RendererCircuits_Instance`][17] class manages the partition set placement
		- It calculates the relaxed partition set positions using the [`CircleDistributionCircleLine`][18] and [`CircleDistributionCircleArea`][19] classes
		- They use the custom [`RandomNumberGenerator`][20] as a source for pseudo-randomness
			- Resetting it before each execution of the relaxation algorithm ensures deterministic results (otherwise the partition set positions can move randomly)
	- The class also defines its own helper data structures like [`ParticleCircuitData`][21]
		- It groups information on the circuits of one particle that is needed by the instance in particular
		- Uses the [`PSetInnerPinRef`][22] struct to refer to partition set handles and circuit line connectors
		- The [`GDRef`][23] struct groups information belonging to a single rendered element (line or circle) that is often needed together
	- [`RenderBatchIndex`][24] struct stores a combination of indices identifying a render batch
		- Used when a reference from the rendered object to its render batch is needed (e.g., to apply changes directly)
	- The [`MaterialDatabase`][25] is a static class that provides references to all materials used by the render system
	- The static [`TextureCreator`][26] class provides methods for generating textures and materials for particles
		- This allows generating textures for an arbitrary number of pins per edge
		- It also stores the generated textures and materials to avoid regenerating them (which takes a lot of time)



TODO



[1]: xref:AS2.Visuals.IParticleState
[2]: xref:AS2.Visuals.IParticleGraphicsAdapter
[3]: xref:AS2.Visuals.IParticleGraphicsAdapterImpl
[4]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.AddParticle(AS2.Visuals.ParticleMovementState)
[5]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.RemoveParticle
[6]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.Update(AS2.Visuals.ParticleMovementState)
[7]: xref:AS2.Visuals.ParticleMovementState
[8]: xref:AS2.Visuals.ParticleJointMovementState
[9]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.PositionSnap
[10]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.BondUpdate(AS2.Visuals.ParticleBondGraphicState)
[11]: xref:AS2.Visuals.ParticleGraphicsAdapterImpl.CircuitUpdate(AS2.Visuals.ParticlePinGraphicState)
[12]: xref:AS2.Visuals.ParticleBondGraphicState
[13]: xref:AS2.Visuals.ParticlePinGraphicState
[14]: xref:AS2.Visuals.ParticlePinGraphicState.PSetData
[15]: xref:AS2.Visuals.ParticlePinGraphicState.PSetData.GraphicalData
[16]: xref:AS2.Visuals.ParticlePinGraphicState.PinDef
[17]: xref:AS2.Visuals.RendererCircuits_Instance
[18]: xref:AS2.Visuals.CircleDistributionCircleLine
[19]: xref:AS2.Visuals.CircleDistributionCircleArea
[20]: xref:AS2.RandomNumberGenerator
[21]: xref:AS2.Visuals.RendererCircuits_Instance.ParticleCircuitData
[22]: xref:AS2.Visuals.RendererCircuits_Instance.ParticleCircuitData.PSetInnerPinRef
[23]: xref:AS2.Visuals.RendererCircuits_Instance.GDRef
[24]: xref:AS2.Visuals.RenderBatchIndex
[25]: xref:AS2.MaterialDatabase
[26]: xref:AS2.Visuals.TextureCreator
