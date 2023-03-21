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


## Event Listeners

A crucial part of UI development is connecting the visible UI elements like buttons and sliders to the code that should be executed when the user interacts with these elements.
Additionally, some UI elements may have to be updated automatically when something changes elsewhere in the simulation environment.
There are three main ways in which this is accomplished in the simulator.

The easiest way to setup a connection between a UI element and code is to establish a reference directly in the Unity Editor.
GameObjects that represent interactable UI elements have Components with Event properties which are displayed in the Inspector, where references to public methods of MonoBehaviour scripts can be added.
For example, the Exit button in the top right corner of the UI is represented by a GameObject called `Exit`.
The Inspector shows that this GameObject has a Component of type Button, which has an Event labeled "On Click ()".
Below this Event, there is a list of references to methods that should be called when the Event is triggered.
In our Scene, there is only a single entry in this list, referencing the [`Button_ExitPressed`][18] method of the [`UIHandler`][3] script, which is attached to the `UI` GameObject:

<img src="~/images/button_on_click.png" alt="The Button Component and On Click () Event of the Exit Button" title="The Button Component and On Click () Event of the Exit Button" width="450"/>

References like this can also be added directly from the code, which is necessary if the UI element is created at runtime.
In this case, the referenced method also does not need to be a public method in a MonoBehaviour script.
An example for this are the [`UISetting`][8] subclasses: They instantiate UI GameObjects from prefabs and then add private methods as listeners by calling `AddListener` and declaring an implicit `delegate` function.
```csharp
toggle.onValueChanged.AddListener(delegate { OnValueChanged(); });
```
In this example, `toggle` is a reference to the `Toggle` Component of a toggle button and `OnValueChanged` is a private method of the [`UISetting_Toggle`][19] class.

The [`UISetting`][8] subclasses also demonstrate the third way of reacting to events, which is the C# [`Action<T>`][20] class.
An object of type `Action<T>` represents a function that takes a single argument of type `T` and which does not return anything.
There are also variants of actions with multiple parameters.
Using the `+` operator, more than one function can be added to an action, effectively turning the list of functions assigned to the action into listeners.
The [`UISetting`][8] subclasses use such actions to "expose" the listener of their control element.
For example, the [`UISetting_Toggle`][19] class has a public action of type `Action<string, bool>`.
This action is called with the name of the setting and the current value of the toggle button as parameters whenever the toggle button is pressed.
This allows users of the [`UISetting_Toggle`][19] class to react to changes of the toggle button by simply adding a listener to the public action.
The [`ParticleUIHandler`][4] uses this approach extensively for the particle attributes.

Another use case of the `Action<T>` approach is the [`EventDatabase`][21].
This is a static class containing several static actions that represent events which can trigger reactions in multiple places, like the simulation being started or stopped.
The [`ButtonHoldTrigger`][22] class has a more specialized role:
It helps implementing the feature that pressing and holding the name of a particle attribute for two seconds applies its value to all particles by providing the [`Action<float> mouseClickEvent`][23].
This action is triggered when the button is released, with the float parameter getting the number of seconds for which the button was pressed.
The [`UISetting`][8] subclasses, in turn, provide an action that is only triggered when the [`mouseClickEvent`][23] is triggered with a parameter value $\geq 2$, encapsulating the duration check.






TODO



- Input management
	- Input handlers attached to the main camera
		- Process mouse inputs that are not blocked by UI elements (like panning and zooming)
	- MouseController
	- Main `UIHandler` processes keyboard shortcuts
	- *Put this into a separate file*





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
[18]: xref:AS2.UI.UIHandler.Button_ExitPressed
[19]: xref:AS2.UI.UISetting_Toggle
[20]: https://learn.microsoft.com/en-us/dotnet/api/system.action-1?view=net-8.0
[21]: xref:AS2.EventDatabase
[22]: xref:AS2.UI.ButtonHoldTrigger
[23]: xref:AS2.UI.ButtonHoldTrigger.mouseClickEvent
