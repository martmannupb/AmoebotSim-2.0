# Model Reference: Collisions and Line Drawer

![Collisions](~/images/collision.png "A collision caused by the green particles contracting West and the blue particles expanding. The blue and orange lines indicate the colliding edges and the arrows show the edge movements.")

As explained on the [Joint Movement extension page](~/amoebot_model/jm.md), there can be movement problems that occur between the start and the end configuration of a movement.
Even if no particles or objects end up on the same grid positions, *collisions* can still occur, meaning that bonds, particles or objects intersect during the movement.
The simulator will check for such collisions as long as this functionality is enabled (the button with this icon: <img src="~/images/view_collision_smaller.png" alt="Collision Check Icon" title="Collision Check Icon" height="25"/>).
Every algorithm should avoid collisions like these just like regular movement conflicts.



## The Line Drawer

The collision checker uses a separate utility to draw the lines and arrows indicating the colliding edges.
The [`CollisionLineDrawer`][1] is a singleton script attached to the `LineDrawer` GameObject.
It provides simple methods for drawing lines and arrows in arbitrary colors and displaying them for a limited time.
The [`AddLine(Vector2 start, Vector2 end, Color color, bool arrow)`][2] method creates a new line between the global grid coordinates `start` and `end`, rendered in the given `color`.
If the `arrow` parameter is `true`, the line will end with an arrow tip.
By calling the [`SetTimer(float duration)`][3] method, a timer of `duration` seconds can be set.
When the specified time expires, all current lines and arrows will be removed.

This drawing utility is not very useful in Amoebot algorithms because the Amoebots have no information on their global positions.
However, it can be useful for visualizing certain structures in Initialization Mode, as demonstrated by the second collision test algorithm.



[1]: xref:AS2.UI.CollisionLineDrawer
[2]: xref:AS2.UI.CollisionLineDrawer.AddLine(Vector2,Vector2,Color,System.Boolean)
[3]: xref:AS2.UI.CollisionLineDrawer.SetTimer(System.Single)
