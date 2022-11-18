# Developer Guide

TODO






## The Render System

The Render System for the simulator consists of a system to render the elements in the view (class RenderSystem) and the User Interface (which is shown on top of the view). For the UI there are multiple MonoBehavior implementations which handle all of the interface functions. The RenderSystem class holds instances of RendererBackground (rendering of the circular/hexagonal background grid), RendererUI (display of integrated overlays for the selection panel), and RendererParticles (rendering of the particles, circuits and bonds). While RendererBackground and RendererUI are simply an instance of one class, RendererParticles has a more complex structure.

### Rendering of Particles

The RendererParticles class handles the display of particles, animations, circuits and bonds. The whole system has kind of a tree-like structure which is shown here:

- RendererParticles
	- RendererParticles_RenderBatch (multiple instances stored in a map with key RendererParticles_RenderBatch.PropertyBlockData)
	- RendererCircuitsAndBonds
		- RendererCircuits_Instance (two instances which are alternating in rendering)
			- RendererCircuits_RenderBatch (multiple instances stored in a map with key RendererCircuits_RenderBatch.PropertyBlockData)
			- RendererCircuitPins_RenderBatch (multiple instances stored in a map with key RendererCircuitPins_RenderBatch.PropertyBlockData)

There are two things a renderer must be able to do: It needs to be able to update the data so that new information can be displayed, and it also needs to be able to draw the stored data efficiently to the screen. The drawing works as follows: When drawing, we call a Draw(..) method in the RenderSystem. This is done once per Frame by the use of the Update() method of a MonoBehavior (AmoebotSimulator). This Draw(..) method is passed through the tree-like render structure until it arrives at the RenderBatches (for particles, circuits and circuit pins). There we have built arrays and matrices which hold the information that needs to be displayed, ready to be used by Unity's Graphics.DrawMesh(..) and Graphics.DrawMeshInstances(..) methods. The updating of the system is working via the ParticleGraphicsAdapterImpl objects. Each particle that should be displayed has one instance of this class and can use the methods defined in IParticleGraphicsAdapter to send data to the renderer (movements, circuit updates, bond updates, ...). From there the data arrives at the RendererParticles class and is processed accordingly. Over the IParticleState interface which is accessible over the same object, the renderer can itself pull information from the particle in case it is needed. For consistency reasons, we decided to rather go for pull updates, so the IParticleState has become less important over the course of development of the system.

### Advanced Rendering (Shader Graph, Materials, MaterialPropertyBlocks)

For the display of animations, there needs to be a method to calculate these movements via shaders or transformations. To be able to produce nice animations, we have used ShaderGraph to create multiple shaders used in the simulator. The process looks like this: You start by creating the shader in ShaderGraph, then you create one or multiple materials with the attached shader where you can set shader parameters. The materials are then loaded via code by the class MaterialDatabase and can be used in combination with mesh and position data by all variants of Graphics.DrawMesh(..) to render something on the screen. An advanced feature you can use with this are MaterialPropertyBlocks. These objects are used to set shader parameters via code very efficiently (much faster than editing the material properties itself). This is very practical for animations and changing shader behavior during runtime and was used in the various RenderBatch classes of the RenderSystem.

# User Interface

The User Interface has been created directly in the Unity editor. You see it in the hierarchy view where you can also find the MonoBehavior instances with the references to all the UI elements in the scene. The UI must be able to process inputs from the user, react to those display valid feedback at all times. Also the interactions between the systems have to be implemented in a way so that there cannot occur any exceptions that break the simulator. To avoid losing control over too many interwined systems, the UI has a modular structure. We have a system for the basic UI (UIHandler), the particle panel on the left (ParticleUIHandler), the init mode (InitializationUIHandler), the panel for the setting (SettingsUIHandler) and the log (LogUIHandler). The handlers themselves have many references to other parts of the simulator (mostly ParticleSystem and RenderSystem and the other handlers), since input needs to be processed and feedback from these systems is shown to the user. E.g. the WorldSpaceUIHandler registers every particle once the particle registers to the RendererParticles class over a call to the ParticleGraphicsAdapterImpl class, this reference is removed once the particle removes itself from the renderer. This way the WorldSpaceUIHandler can request data from the particle itself when it needs to draw or update the world space overlay used to show current particle attribute data.