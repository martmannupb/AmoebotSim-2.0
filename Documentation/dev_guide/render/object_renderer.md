# Dev Guide: Object Renderer

The [`RendererObjects`][1] class is responsible for rendering the [objects](~/model_ref/objects.md) in the system.
For this, every object contains an instance of the [`ObjectGraphicsAdapter`][2] class which represents the object in the render system.
This instance has to be registered in the [`RendererObjects`][1] in order to be displayed.

Once an object is registered, the [`ObjectGraphicsAdapter`][2] generates a mesh for the object and updates it every time the object is changed.
The mesh consists of four triangles for every hexagon in the object and additional triangles between the hexagons at the borders of the object.
These are necessary because the vertices at the borders are placed closer to their hexagon's center so that there is a gap between an object and neighboring particles or other objects.

![Object mesh](~/images/object_mesh.png "The mesh generated for an object")

In order to render the objects with different colors, the [`RendererObjects`][1] maintains a dictionary of material property blocks using colors as keys.
Every time an object is registered or a registered object changes its color, a corresponding property block is created or taken from the dictionary.
The material used to render the objects is `Resources/Materials/Base/ObjectMat.mat`.
It is a very simple material giving the object mesh a solid color.

When the [`Render`][3] method of the [`RendererObjects`][2] is called, it renders each object individually, using the TRS matrix computed by the object's [`ObjectGraphicsAdapter`][2].
If animations are enabled, the matrix uses an interpolated position between the previous and current object position.



[1]: xref:AS2.Visuals.RendererObjects
[2]: xref:AS2.Visuals.ObjectGraphicsAdapter
[3]: xref:AS2.Visuals.RendererObjects.Render
