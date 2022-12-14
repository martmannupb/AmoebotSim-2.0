# Model Reference: Directions and Compasses

Particles in the Amoebot model live on a two-dimensional plane that is organized by the equilateral triangular grid, as explained in the [Amoebot model reference](~/amoebot_model/home.md).
On this plane, there are infinitely many directions, but due to the triangular grid, a small set of directions is much more relevant for the particles than all other directions.
This page explains what these directions are and how the particles perceive them in their local perspective.


## Directions

In the triangular grid graph, every node has six neighbors that are evenly spaced out at $60^\circ$ angles around the node.
Due to this structure, we have three axes, at $60^\circ$ to each other, such that every edge in the grid graph is parallel to one of the axes.
Each of the axes defines two directions in the plane.
We orientate the grid such that one of the axes is horizontal and call the direction going to the right side *East* and the direction going to the left *West*.
Accordingly, the four directions defined by the other two axes are called *North-North East*, *North-North West*, *South-South West* and *South-South East*.
We call these six directions the *primary* or *cardinal directions* and abbreviate them with E, NNE, NNW, W, SSW, SSE, starting with East and going around in $60^\circ$ steps in counter-clockwise direction.
Using the same order, we identify the cardinal directions with the integers $0,\ldots,5$.
The cardinal directions are those directions in which the particles can perform movements and find their neighbors, because every (directed) edge of the grid graph points in a cardinal direction.

Additionally, we define a second set of directions by rotating the cardinal directions by $30^\circ$.
These *secondary directions* lie exactly between the cardinal directions and are named *East-North East*, *North*, *West-North West*, *West-South West*, *South* and *East-South East*, abbreviated ENE, N, WNW, WSW, S, ESE, and also identified by $0,\ldots,5$ in that order.







- There are 6 primary (cardinal) directions and 6 secondary directions in the triangular grid
	- The cardinal directions point along the edges of the grid, the secondary directions lie exactly between the cardinal directions
	- The angle between two adjacent cardinal or adjacent secondary directions is 60°
	- The angle between adjacent cardinal and secondary directions is 30°
	- Cardinal directions are defined to be East, North-North East, North-North West, West, South-South West, South-South East
		- Abbreviated as E, NNE, NNW, W, SSW, SSE and identified by integers 0, ..., 5
	- Secondary directions are, analogously, defined as ENE, N, WNW, WSW, S, ESE
		- They are also identified by integers 0, ..., 5 in this order
- Directions are represented by the `Direction` enum in the code
	- The `DirectionHelpers` class provides several helpers for computing and transforming directions
	- The special `NONE` value represents no direction
		- Every operation with `NONE` results in either `NONE` or `-1` (if the result of the operation is an `int`)
- Every particle has a local *compass*
	- The compass defines how the particle's local directions correspond to the global directions
	- The *compass direction* of a particle is the *global* direction that the particle believes to be *East*
	- The `ParticleSystem_Utils` class provides helper methods for converting between local and global directions
		- The *chirality* of a particle has to be taken into account for the conversion
		- If the particle has an inverted chirality, its local directions are mirrored, i.e., its local NNE direction will lie 60° in *clockwise* direction from its local E direction when translated into global directions
- A particle *does not know* its compass direction
	- Nor does it have a way to find out
- Particles in a system can establish a common compass direction using a *compass alignment algorithm*
	- The example algorithm uses 2 pins and establishes a common compass direction with high probability
	- Particles have to actively rotate directions if their compass does not agree with the established compass
		- They also have to take their chirality into account if a common chirality was established beforehand
- Particle systems can be initialized with different compass direction settings
	- All particles can have the same compass directions
	- Compass directions can be random
	- Compasses of individual particles can be changed manually
- The compass direction of a particle cannot be changed while the simulation is running, it must be set during initialization
