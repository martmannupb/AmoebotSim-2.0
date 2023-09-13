# Dev Guide: The Render System

The render system renders the background grid, the particles, their circuits and bonds, the objects and some additional UI overlays to the screen.
This must be done efficiently to keep the application responsive even while simulating large particle systems.
Achieving this requires some effort and the render system's code is therefore quite complex.
This part of the Developer Guide gives an overview of the render system's structure, roughly explains the most important high-level concepts and points to the places where the visualization could be changed or extended.
Its main purpose is to provide some information for users who want to change the visualization so that they do not have to read through the entire rendering code.

This page gives an outline of the render system's structure.
The individual parts of the system are explained in more detail on their own pages.


## General structure

<img src="~/images/render_system_overview.png" alt="Render System Overview" title="Render System Overview" width="600" align="center"/>

The [`RenderSystem`][1] class is the main container of the render system and forms the root of a tree structure.
One instance of this class is created by the application's main class, [`AmoebotSimulator`][2], when the application starts.
In every `Update` call, the [`AmoebotSimulator`][2] calls the [`RenderSystem`][1]'s [`Render()`][3] method, which triggers a cascade of similar method calls through the entire render system hierarchy (represented by the arrows in the image above).
The [`RenderSystem`][1] creates and manages one instance of each of the four render classes, the [`RendererUI`][4], [`RendererBackground`][5], [`RendererObjects`][7] and [`RendererParticles`][6].
These classes receive the [`Render()`][3] call of the [`RenderSystem`][1] and handle the rendering or pass the call to subordinate classes.

Apart from that, the [`RenderSystem`][1] class contains most of the render parameters and constants.
These include the current visualization type (hexagonal, round hexagonal or graph), animation times, line widths, radii and scale values, and Z layers of all components.
The Z layers define the order in which the rendered objects are drawn on top of each other (more about this can be found on the [rendering basics page](rendering_basics.md)).
Some of these values can be modified to easily change the visualization, like the animation times, for example.
The class stores most of these parameters as public static fields, which makes them accessible from everywhere.
Almost all of the render system's classes use these values to influence the render result.

The next page explains the [basics of rendering in Unity](rendering_basics.md).
The concepts explained on that page are relevant to all of the following pages, which explain the four render classes in more detail.



[1]: xref:AS2.Visuals.RenderSystem
[2]: xref:AS2.AmoebotSimulator
[3]: xref:AS2.Visuals.RenderSystem.Render
[4]: xref:AS2.Visuals.RendererUI
[5]: xref:AS2.Visuals.RendererBackground
[6]: xref:AS2.Visuals.RendererParticles
[7]: xref:AS2.Visuals.RendererObjects
