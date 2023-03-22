# Dev Guide: Input Handling

This page outlines how user input is handled by the simulation environment.
Most of the standard user input like clicking buttons or dragging sliders is already handled by the UI system, which is explained on [its own page][1].


## Keyboard Shortcuts

Some of the functionality provided by the UI buttons can also be accessed through keyboard shortcuts, as listed in the [User Guide][2].
These shortcuts are implemented in the [`UIHandler`][3] class, which is a MonoBehaviour script attached to the `UI` GameObject.
As such, the [`UIHandler`][3] has access to Unity's [`Input`][4] system, which provides access to all mouse or keyboard input made in the current frame.
The [`UIHandler`][3]'s private `ProcessInputs` method is called in each frame and simply uses a list of `if` statements checking whether any hotkeys are pressed.
If this is the case, the corresponding handler method is called just as if the button was pressed.

The shortcuts can be modified (or new ones added) easily by changing these `if` statements (the documentation page in the [User Guide][2] should then be updated as well).
However, one should be cautious not to interfere with the Unity Editor's shortcuts, which mostly use the Ctrl key.
For example, the combination Ctrl + P will enter or exit Play Mode, so this shortcut cannot be used in the simulator if it should be run from the Editor.
If the simulator is supposed to be built and run outside of the Editor, most shortcuts should be fine.


## Camera Movement

The central viewport can be moved around by clicking and dragging with the right or middle mouse button and the zoom level can be changed with the scroll wheel.
This is implemented as follows:

The camera is a GameObject called `Main Camera` in the `Simulator` Scene.
It is oriented in such a way that the global X axis is the camera's horizontal axis, the global Y axis is the camera's vertical axis, and the global Z axis is orthogonal to the viewing plane, pointing into the screen, i.e., objects with a larger Z coordinate are further in the background of the rendered image.
The camera's projection mode is set to Orthographic, which means that the rendered image is not affected by perspective:
The Z location of the camera and the objects only determines which objects are in front of the camera, but the objects' visible size is not affected by their distance to the camera.
This gives the rendered image the desired 2D look and simplifies the camera movement.
To move the viewport around, we only need to translate the `Main Camera` GameObject along the X and Y axes.

To change the zoom level, the *orthographic size* of the camera has to be changed.
The orthographic size is simply the size of the area rendered by the camera - a bigger size appears to be zoomed out and a smaller size appears to be zoomed in.

This movement functionality is implemented by the [`MouseController`][5], which is a MonoBehaviour script attached to the `Main Camera` GameObject.
In each frame, it queries the [`Input`][4] system to obtain the current state of the mouse and determines whether the cursor is being moved while the right or middle mouse button is held down.
It then translates the camera according to its current rotation and the distance and direction of the cursor's movement.
Additionally, it stores how far the camera has moved in the frame and continues to move the camera more slowly in the same direction in the next frame if no mouse dragging is detected.
This makes the camera movement fade out smoothly if the mouse button is released while moving the cursor.
A similar mechanism is used for the zoom level, which uses the scroll wheel instead of cursor movement and changes the camera's orthographic size instead of its X and Y coordinates.

As an alternative to dragging with the mouse, the [`MouseController`][5] also checks for the arrow keys and the WASD keys being pressed with Right Ctrl held down to allow moving the camera without using the mouse.



TODO

- InputController is also attached to camera
	- Packages mouse state info and sends it to InputHandler
	- InputHandler converts mouse state into mouse actions and forwards them to the InputManager
	- InputManager does nothing except providing click action event to which listeners can be added from elsewhere
		- RendererUI is the only subscriber
		- Reacts to click and drag events, implementing the tools



## Selecting Particles




[1]: ui.md
[2]: ~/user_guide/usage/sim.md
[3]: xref:AS2.UI.UIHandler
[4]: https://docs.unity3d.com/ScriptReference/Input.html
[5]: xref:AS2.MouseController
