# Dev Guide: User Interface

The User Interface (UI) provides detailed information about the system state as well as control elements such as buttons, dropdown menus, sliders and input fields.
Its purpose is to make the application interactive by giving the user the tools needed to configure and run simulations as easily as possible.

The UI is implemented using Unity's GameObject-based UI system (known as [uGUI][1]).
This means that the visible parts of the UI are composed of GameObjects in the Scene and their behavior is implemented by MonoBehaviour scripts attached to the GameObjects.
As can be seen in the Hierarchy window, there are three top-level UI GameObjects, each with its own [Canvas][2] component allowing its children to be drawn as UI elements.
The `UI` GameObject contains most of the interface while the `UI (World Space)` GameObject handles dynamic text objects that are displayed on top of each particle and the `UI (World Space Background)` GameObject draws the visible grid coordinates when this option is enabled.
The `UI` GameObject is by far the most complex because it is the root element of a large tree structure defining the layout of the entire interface.
This structure makes heavy use of layout groups to ensure that the UI scales well with different screen resolutions and aspect ratios, even when the content of some UI elements is created dynamically.


## UI Behavior Handlers

As mentioned before, the behavior of the UI is implemented in MonoBehaviour scripts attached to the UI GameObjects.
Since the UI is separated into mostly independent parts, each of these parts has its own MonoBehaviour, even though most of them are attached to the top-level `UI` GameObject.
In the following, each MonoBehaviour is described briefly.

### [`UIHandler`][3]

Controls the top and bottom bars and handles keyboard shortcuts.

This handler enables or disables the control elements in the top and bottom bars depending on the current mode.
While in Simulation Mode, it updates the round slider and the simulation control buttons in the bottom bar based on the progress of the simulation.
Whenever a button is pressed, this handler decides how to react and triggers the corresponding action where it is required.
It also listens for key combinations to allow alternative ways of controlling the system.

### [`ParticleUIHandler`][4]

Controls the Particle Panel on the left side, which appears when a particle is selected in Initialization or Simulation Mode.

The Particle Panel shows the location and expansion state as well as the chirality, compass direction and attributes of the currently selected particle.
In Init Mode, the particles have initialization parameters instead of attributes, and the chirality and compass direction are editable.
In Simulation Mode, the attributes can only be edited while in the latest round and the chirality and compass direction are not editable.
The UI handler enables or disables the parameters/attributes according to these rules.

Every time a new particle is selected, the handler dynamically builds the list of parameters/attributes.
This is necessary because the number and types of the attributes are not known before the application starts and can change when a different algorithm is selected.
Each attribute is represented by a UI element that contains the attribute name as a text and its value as a toggle button, dropdown menu, slider or input field, depending on the attribute's type.
These elements are created as subclasses of the [`UISetting`][8] class, which provides one subclass for each supported type of attribute.
The corresponding subclass then instantiates a GameObject prefab for the UI element and inserts it under the correct layout element so that it becomes a part of the scrollable area in the Particle Panel.
A similar procedure is used to add buttons to the Randomization Panel, which extends from the right side of the Particle Panel and has two buttons for each attribute.
The UI prefabs can be found in the `Assets/Resources/UI/Prefabs/Settings` folder and accessed from code using the static [`UIDatabase`][9] class.

### [`InitializationUIHandler`][5]

Controls the Initialization Panel on the right side, which is shown while in Initialization Mode.

The Initialization Panel contains the dropdown menu in which the algorithm can be selected and the parameters of the associated system generation method.
The UI elements for the parameters are created in a similar way to the attributes in the Particle Panel.
The information which algorithms exist, what their generation methods are and which parameters they need is taken from the reflection interfaces [`AlgorithmManager`][10] and [`InitializationMethodManager`][11].

### [`SettingsUIHandler`][6]

Controls the Settings Panel, which can be opened by pressing the gear icon in the top right corner during Simulation Mode.

This handler also uses the [`UISetting`][8] workflow to create the UI elements for the settings, even though they do not have to be created dynamically.
To control the animation of the Randomization Panel, the handler finds the [`ParticleUIExtensionSmoothLerp`][12] script attached to the Particle Panel and enables or disables it based on the current setting.

### [`LogUIHandler`][7]

Controls the Log Panel at the bottom of the central area.

The handler is used by the static [`Log`][13] class to display log messages on the screen.
It ensures that the number of log entries that are displayed does not grow too large by deleting older log messages.
The log entries are instantiated using GameObject prefabs which have a [`SizeFitter`][14] script attached to them, allowing multi-line log messages to be stretched to an appropriate size.
The handler also takes care of hiding the Log Panel if it has not been interacted with for a few seconds.

### [`WorldSpaceUIHandler`][15]

Controls the UI overlay that displays data on top of individual particles.

When an attribute is clicked in the ParticlePanel, that attribute's value is displayed on top of each particle.
This handler takes care of creating, placing and updating these UI elements.
Each of the elements is instantiated from a prefab that contains a simple text box to display an attribute's value as a string.
Special symbols like arrows for displaying chirality and directions are special characters that are enabled by changing the font of the text box.
The handler uses a pooling mechanism to avoid creating and destroying the text elements unnecessarily.

### [`WorldSpaceBackgroundUIHandler`][16]

Controls the UI overlay that displays the coordinates of all visible unoccupied grid cells.

When the top bar button labeled "#" is clicked, the grid coordinates of all unoccupied cells are displayed on the screen.
This handler is responsible for creating and positioning the text elements for this purpose.
Again, the UI elements are created from GameObject prefabs.
To find the correct positions and avoid creating too many objects, the handler uses the static [`CameraUtils`][17] class.
Additionally, it disables all camera movements while the coordinates are being displayed because updating the coordinates is very resource intensive and can lead to freezes if too many coordinates are displayed at the same time.



TODO


- UI code uses Actions for reacting to changes
	- Actions connected to button click events
	- Actions triggered elsewhere in the simulator when something changes
	- Special handlers for detecting when a button is being pressed for a time
	- Use examples to show what this means

- Input management
	- Input handlers attached to the main camera
		- Process mouse inputs that are not blocked by UI elements (like panning and zooming)
	- MouseController
	- ButtonHoldTrigger
	- Main `UIHandler` processes keyboard shortcuts




The User Interface has been created directly in the Unity editor.
You see it in the hierarchy view where you can also find the MonoBehavior instances with the references to all the UI elements in the scene.
The UI must be able to process inputs from the user, react to those display valid feedback at all times.
Also the interactions between the systems have to be implemented in a way so that there cannot occur any exceptions that break the simulator.
To avoid losing control over too many interwined systems, the UI has a modular structure.
We have a system for the basic UI (UIHandler), the particle panel on the left (ParticleUIHandler), the init mode (InitializationUIHandler), the panel for the setting (SettingsUIHandler) and the log (LogUIHandler).
The handlers themselves have many references to other parts of the simulator (mostly ParticleSystem and RenderSystem and the other handlers), since input needs to be processed and feedback from these systems is shown to the user.
E.g. the WorldSpaceUIHandler registers every particle once the particle registers to the RendererParticles class over a call to the ParticleGraphicsAdapterImpl class, this reference is removed once the particle removes itself from the renderer.
This way the WorldSpaceUIHandler can request data from the particle itself when it needs to draw or update the world space overlay used to show current particle attribute data.




[1]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/index.html
[2]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UICanvas.html
[3]: xref:AS2.UI.UIHandler
[4]: xref:AS2.UI.ParticleUIHandler
[5]: xref:AS2.UI.InitializationUIHandler
[6]: xref:AS2.UI.SettingsUIHandler
[7]: xref:AS2.UI.LogUIHandler
[8]: xref:AS2.UI.UISetting
[9]: xref:AS2.UIDatabase
[10]: xref:AS2.AlgorithmManager
[11]: xref:AS2.InitializationMethodManager
[12]: xref:AS2.UI.ParticleUIExtensionSmoothLerp
[13]: xref:AS2.Log
[14]: xref:AS2.UI.SizeFitter
[15]: xref:AS2.UI.WorldSpaceUIHandler
[16]: xref:AS2.UI.WorldSpaceBackgroundUIHandler
[17]: xref:AS2.CameraUtils
