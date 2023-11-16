# Model Reference: Collisions and Line Drawer

![Collisions](~/images/collision.png "A collision caused by the green particles contracting West and the blue particles expanding. The blue and orange lines indicate the colliding edges and the arrows show the edge movements.")

As explained on the [Joint Movement extension page](~/amoebot_model/jm.md), there can be movement problems that occur between the start and the end configuration of a movement.
Even if no particles or objects end up on the same grid positions, *collisions* can still occur, meaning that bonds, particles or objects intersect during the movement.
The simulator will check for such collisions as long as this functionality is enabled (the button with this icon: <img src="~/images/view_collision_smaller.png" alt="Collision Check Icon" title="Collision Check Icon" height="25"/>).
Every algorithm should avoid collisions like these just like regular movement conflicts.



## The Line Drawer

The collision checker uses a separate utility to draw the lines and arrows indicating the colliding edges.
The [`LineDrawer`][1] is a singleton script attached to the `LineDrawer` GameObject.
It provides simple methods for drawing lines and arrows in arbitrary colors and displaying them on top of the particle system, optionally for a limited time.
The [`AddLine(Vector2 start, Vector2 end, Color color, bool arrow, float width, float arrowWidth)`][2] method creates a new line between the global grid coordinates `start` and `end`, rendered in the given `color`.
If the `arrow` parameter is `true`, the line will end with an arrow tip.
The `width` and `arrowWidth` parameters can be used to scale the width of the line and the width of the arrow tip, respectively
By calling the [`SetTimer(float duration)`][3] method, a timer of `duration` seconds can be set.
When the specified time expires, all current lines and arrows will be removed.

This drawing utility can be useful for visualizing certain structures (like the goal shape of a shape formation algorithm) in Initialization Mode, or it can be used by [status info methods](status_info.md) to display additional information at runtime (as demonstrated by the line formation algorithm, which uses the line drawer to draw the edges of the current spanning tree).



[1]: xref:AS2.UI.LineDrawer
[2]: xref:AS2.UI.LineDrawer.AddLine(Vector2,Vector2,Color,System.Boolean)
[3]: xref:AS2.UI.LineDrawer.SetTimer(System.Single)
