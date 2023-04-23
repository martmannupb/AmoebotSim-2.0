# Dev Guide: UI Renderer

- [`RendererUI`][1] class draws the colored particle overlays for all the different tools
	- Also triggers the effects of the tools since they have to be handled somewhere
- The only class that uses the [`InputManager`][2] system (see [Input Handling page](~/dev_guide/input.md))
- Most of the code is a big case distinction determining what kind of input has been made and how to react to the input
	- Particle overlay is simple hexagon mesh with transparent material
		- Materials for tools are in `Resources/Materials/HexagonalView/UI`
- Also handles dragging of partition sets (uses the same principle)
	- Delegates this functionality to [`PSetDragHandler`][3]
		- The class does the same as [`RendererUI`][1] but only handles the dragging of partition sets
		- Has to access information of the particle rendering system to find out which partition set is clicked and to change its position




TODO


[1]: xref:AS2.Visuals.RendererUI
[2]: xref:AS2.UI.InputManager
[3]: xref:AS2.Visuals.PSetDragHandler
