# AmoebotSim 2.0 Repository

**Implementation for the Master's Thesis: On The Shape Containment Problem within the Amoebot Model with Reconfigurable Circuits, written by Matthias Artmann**

This branch contains the program code belonging to the thesis, consisting of implementations of the major algorithms in the *AmoebotSim 2.0* simulation environment and a helper script for creating input shapes.



## Simulator

The `AmoebotSim 2.0/` directory contains the main simulator project's source code and the implemented algorithms.
The `docs.zip` archive within the `Documentation/` subdirectory contains the HTML documentation of the simulator, which can be viewed by opening the `index.html` file in a web browser.
Follow the *Installation Guide* pages to install Unity and setup the simulator project.
After installing Unity and cloning the git repository, checkout the `master_thesis` branch instead of the `master` branch.
To run simulations, follow the instructions on the *User Guide* pages of the documentation.
(The hotkey for hiding the UI has been changed from "H" to "Shift + H" to avoid problems when typing in the simulator.
This change is not reflected in the User Guide.)


### Implemented Algorithms

When the simulator is running, the algorithm can be selected from the *Algorithm* dropdown in the Initialization Panel on the right-hand side.
The main algorithms implemented for the thesis are identified by the `SC` prefix for "Shape Containment".

- `SC Convex System` implements the solution for convex amoebot structures from Chapter 3.
	You can select the type of convex shape formed by the amoebots as well as its dimensions (using the notation from the thesis) and rotation in the Initialization Panel.
	The target shape is set in the `shape` input field.
	If the `fromFile` option is checked, the input field expects the name of a JSON file in the `AmoebotSim 2.0/Assets/Shapes/` directory.
	If the option is not checked, a JSON string describing the shape can be entered directly into the input field (e.g., after copying it to the clipboard using the shape creator).
	Since the algorithm works for arbitrary shapes, any input shape can be chosen.
	When the `Generate` button is clicked, a preview of the shape and its convex hull is shown.
- `SC Convex Shapes` implements the solution for convex shapes in arbitrary amoebot structures from Chapter 4.
	Here, the target shape is defined by the convex shape type and its dimensions, and the amoebot structure is generated randomly based on the number of amoebots and a hole probability.
	The additional parameters can be used to fine-tune the distribution of amoebots.
- `SC Star Convex Shapes` and `SC Star Convex Shapes V2` both implement the algorithm for star convex shapes from Chapter 5.
	The only difference is that the first version iterates over the constituent shapes in its outer loop and each shape's rotations in the inner loop, whereas the `V2` version iterates over rotations in the outer loop and the constituent shapes in the inner loop.
	The `V2` version is generally a lot faster because successful placements are identified earlier and the remaining rotations are skipped.
	This algorithm only accepts star convex shapes as input, which contain additional meta-information on the constituent shapes.
	Such shapes can be created with the shape creator tool by using its star convex shapes mode.
- `SC Snowflakes` implements the snowflake algorithm from Chapter 6.
	It only accepts snowflake shapes, as can be created by the shape creator in snowflake mode.
	If the input shape is star convex, you can check the `isStarConvex` option to use a binary search instead of a linear search for the maximum scale factor.
	Checking the `fillShape` option ensures that the target shape at the given scale and rotation is covered by amoebots.
- `SC General Solution` implements the improved general solution from Chapter 7.
	It accepts any input shape and will compute a traversal path that ends at the shape's origin.
	The path is computed using a heuristic method that tries to minimize the length of the path but which might not be optimal for every shape.
	It is guaranteed to traverse every edge at most twice, however.
	The generated traversal path is displayed on top of the shape preview when the `drawTraversal` option is checked.
	The edges range from black to white and become smaller the later they appear in the traversal

While running any of these algorithms, you can select an amoebot and click the `Display Shape` button in the Particle Panel on the left-hand side to display the target shape again, placed at the selected amoebot.

Additionally, the `Hexagon Test` algorithm implements a part of an algorithm that was cut from the thesis due to time constraints.
The algorithm works for shapes that are just outlines of convex shapes without any nodes, edges or faces on the inside and places them around subsets of holes in the amoebot structure.
In the implementation, a single inner boundary and its extreme points as well as a hexagon outline are given.
The algorithm finds all placements of the hexagon shape where the hole in the amoebot structure is contained within the shape.
This part was planned to be the placement search procedure in a larger algorithm that tests all subsets of inner boundaries and runs a binary search on each subset using this placement search.

The source code for these algorithms can be found in the `AmoebotSim 2.0/Assets/Code/Algorithms/` directory.
In the order listed above, the algorithms are implemented in `SCConvexSystem.cs`, `SCConvexShapes.cs`, `SCStarConvexShapes.cs`, `SCStarConvexShapes_v2.cs`, `SCSnowflakes.cs` and `SCGeneral.cs`.
The subroutines used by these implementations can be found under `AmoebotSim 2.0/Assets/Code/Subroutines/` and the classes handling the shape representation can be found in `Shape.cs` and `ShapeContainer.cs` under `AmoebotSim 2.0/Assets/Code/Data/Types/`.



## Shape Creator

The `shape_creator/` directory contains a utility app written with Pygame that provides a graphical interface for creating input shapes for the simulator.
Instructions on how to setup Pygame and use the shape creator can be found in the script file itself.
After generating a JSON file describing a shape, you can move the file to the `AmoebotSim 2.0/Assets/Shapes/` directory to make it available in the simulator.
This also works while the simulator is running.
Star convex shapes and snowflakes must be created in the corresponding modes, otherwise they will lack the meta-information (consituents of star convex shapes and dependency trees of snowflakes) required by the algorithm.
Shapes from all modes are accepted by the algorithms supporting arbitrary shapes.
The current mode of the app is printed to the terminal in which it is running.
