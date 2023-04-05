# Simulator Usage: Initialization Mode

When the application is started using the Play Button in the Unity Editor, it opens in *Initialization Mode*.
Initialization Mode (or Init Mode) is one of the two main modes of the simulation environment, the other one being [*Simulation Mode*](sim.md).
The Init Mode is used to initialize the particle system before running a simulation.
In this mode, you can select an algorithm, place the particles, modify the particles' parameters and finally start the simulation.

The Init Mode UI is organized like this:

![Init Mode UI Overview](~/images/init_mode_overview.png "Init Mode UI Overview")

In the following, we will cover the areas that are relevant in Init Mode.
Many of the UI elements are also visible in Simulation Mode and will be covered on the corresponding page.


## Central Area

The Central Area is usually the largest part of the UI.
It shows the world grid and the particle system both in Init Mode and in Simulation Mode.
In Init Mode, its background has a slight tint and all particles are gray.
The viewport can be moved around by clicking and dragging with the right or middle mouse button and the zoom level can be changed with the scroll wheel.
Alternatively, the viewport can also be moved by holding the Right Ctrl key and pressing the arrow keys or the WASD keys.
Particles can be selected by left-clicking on a particle and deselected by left-clicking anywhere else in the Central Area.


## Log Panel

The Log Panel is at the bottom of the Central Area and displays log messages sent using the [`Log`][1] system.
It disappears automatically after a short time unless the small triangle button was used to expand it.
When it is expanded, the button in the top right corner of the Log Panel can be used to save the entire log in a text file.
The Log Panel removes older log messages if the log grows too long, but all messages will still appear in the text file.


## Bottom Bar

The Bottom Bar contains control elements for the simulation and is only enabled in Simulation Mode.
It is explained on the [next page](sim.md).


<img src="~/images/init_panel.png" alt="Initialization Panel" title="Initialization Panel" width="200" align="right"/>

## Initialization Panel

The Initialization Panel is displayed on the right side of the UI and it is only visible in Init Mode.
It contains the algorithm selection dropdown menu, a list of generation parameters and several buttons.

The dropdown menu allows you to select the algorithm that will be simulated.
Changing the selected algorithm will update the list of parameters and generate a new particle system with the default parameter values.
Every algorithm has its own list of generation parameters.
Clicking the "Generate" button will create a new system of particles using the algorithm's generation method with the current parameter values.

The "Load" and "Save" buttons allow you to load and save initialization states.
This is useful if you want to repeat a simulation several times with the same setup, e.g., to fix errors that occur only under certain conditions.
Both buttons will open a file explorer expecting a file with the `.aminit` ending.
> [!NOTE]
> It may not be possible to load a save file after changing an algorithm's name or its generation parameters.

The "Start" button will start the simulation by switching from Init Mode to Simulation Mode.
You should only press this button when you are finished with initializing the particle system.

The "Abort" button only works if Init Mode was opened again after running a simulation.
If this is the case, it will switch back to Simulation Mode and restore the last simulation that was running before Init Mode was opened.


<img src="~/images/particle_panel.png" alt="Particle Panel" title="Particle Panel" width="200" align="right"/>

## Particle Panel

The Particle Panel is visible on the left side of the UI as long as a particle is selected, both in Init and Simulation Mode.
It can be used to view and edit a particle's internal data.

The header of the Particle Panel shows the text "Particle" and two buttons.
The left button is the Anchor button.
Clicking it turns the currently selected particle into the Anchor, disabling the button.
The Anchor particle ties the system to the grid during joint movements.
Please refer to the [Joint Movement reference page](~/model_ref/bonds_jm.md) for more information on how this works.

The right button simply toggles the Randomization side bar, which is explained further below.

The text field below the header displays the selected particle's grid coordinates and its current expansion state.
If the particle is expanded, it shows the coordinates of its Head and its Tail (in that order).

The lower part of the Particle Panel contains the selected particle's chirality and compass direction (see the reference pages for more information on [chirality](~/model_ref/chirality.md) and [compass direction](~/model_ref/direction.md)), followed by its *attributes*.
The chirality and compass direction can only be edited in Init Mode.
In Simulation Mode, the Particle Panel still displays these properties but they cannot be modified.
The list of attributes following the chirality and compass direction settings may even be completely different between the two modes:
In Init Mode, the attributes represent the particle's *initialization parameters*.
When the "Start" button is pressed, every particle is initialized with its current parameter values, which are passed to the particle's `Init(...)` method.
This process is explained in more detail on the [Algorithm Development pages](~/user_guide/dev/home.md).
In Simulation Mode, the attributes represent the *particle attributes*, which define a particle's state during the simulation.
Particle attributes are explained on their own [reference page](~/model_ref/attrs.md).
They can only be modified while the simulation state is in the latest simulated round.

Each attribute has a unique name and a field that displays its current value and can be used to change the value.
This field is different depending on the attribute's type, for example, bool attributes have a toggle button and enum types have a dropdown menu, while int, float and string attributes have a simple text input field.

The name of each attribute can be clicked, which will display an overlay showing the attribute's value on top of each particle in the Central Area.
The image below shows the overlay for the "leader" attribute, which is an initialization parameter of the Line Formation algorithm.
Exactly one particle in the system is the leader.
The overlay can be disabled by pressing the "UI" button in the Top Bar.
<img src="~/images/attribute_overlay.png" alt="Attribute Overlay" title="Attribute Overlay" width="600"/>

Additionally, if the name of an attribute is pressed and held down for two seconds, its current value in the selected particle is copied to all particles in the system.

To the right of each attribute, there are two randomization buttons in the Randomization side bar.
The first button randomizes the attribute's value for the selected particle, if the attribute type supports randomization (e.g., string attributes cannot be randomized).
The second button randomizes the attribute value for *all particles*.


## Top Bar

![Top Bar](~/images/top_bar.png "Top Bar")

The Top Bar is a general tool bar that is visible both in Init and Simulation Mode, with slightly different content.
It is organized into four blocks, roughly grouping together related functionality.
Most of the buttons do not have permament effects, so you can also explore their functionality by simply clicking them.

<img src="~/images/top_bar_left.png" alt="Left Top Bar Group" title="Left Top Bar Group" width="150" align="right"/>

### Left Group

The first group, located on the far left side, contains four buttons.
The leftmost button switches from Simulation Mode back to Init Mode.
It has no effect while in Init Mode.
The second and third buttons are for saving and loading simulation states, respectively.
The load button works in Init and Simulation Mode and will open the selected simulation state directly in Simulation Mode.
The save button only works in Simulation Mode.
Both buttons open a file explorer expecting a file with the `.amalgo` ending.
The fourth button is for taking screenshots of the simulation environment and will open a file explorer expecting a PNG file.

<img src="~/images/top_bar_mid_left.png" alt="Middle-Left Top Bar Group" title="Middle-Left Top Bar Group" width="300" align="right"/>

### Middle-Left Group

The second group is the tool menu.
It contains four tool buttons and two dropdown menus with additional options.
A tool can be selected by clicking the button, which will highlight the button in red.

The first tool is the Selection tool.
It is selected by default and allows you to select particles by clicking on them, opening the Particle Panel.
This tool works both in Init and Simulation Mode.

The second tool is the Add tool.
It can be used to manually add particles to the system while in Init Mode.
With the Add tool selected, hovering over a grid cell will highlight that cell in green.
Clicking the cell will place a new contracted particle in the cell.
Clicking and dragging to one of the neighboring cells and then releasing will place a new expanded particle such that its Tail is in the first and its Head is in the second cell.
The two dropdown menus to the right determine the new particle's chirality and compass direction.
By default, both will be selected randomly.

The third tool is the Remove tool.
When the Remove tool is selected, hovering over a particle will highlight that particle in red and clicking a particle will remove it from the system.

The fourth and last tool is the Move tool.
With this tool selected, clicking a particle will highlight it in purple, after which it can be moved to a different, empty cell just like using the Add tool, but with blue cell highlighting.
Moving a particle will not change any of its internal data, including its chirality and compass direction.

Keep in mind that the particle system must be connected when starting the simulation.
Apart from that, simply adding, removing or moving particles may cause an algorithm to behave in unexpected ways if it requires particles to be initialized in a specific way.
You should only use these tools to fine-tune generated systems and if you know how the particles need to be initialized.

<img src="~/images/top_bar_mid_right.png" alt="Middle-Right Top Bar Group" title="Middle-Right Top Bar Group" width="275" align="right"/>

### Middle-Right Group

The third group is the view menu.
It contains several cycle and toggle buttons for modifying the visualization in the Central Area.
The buttons are the same in Init and Simulation Mode, but some of their effects can only be seen in Simulation Mode.
They will be explained on the [Simulation Mode page](sim.md).

The first button cycles through the three general visualization modes.
The default mode shows both the grid and the particles in a hexagonal style, the second mode renders the particles as circles in the hexagonal grid, and the third mode resembles the original AmoebotSim visualization, where the background shows the triangular grid graph and particles are displayed as solid black disks surrounded by colored circles.
In the third view mode, circuits are not displayed.

The "UI" button is only used to disable the overlay showing an attribute value for all particles, which is enabled by clicking an attribute's name in the Particle Panel.

The "#" button toggles another overlay which displays the global grid coordinates of all visible empty grid nodes.
While this overlay is active, all camera movements are disabled because updating the overlay takes too long for smooth movements.

The other buttons only make a difference in Simulation Mode and will be explained later.

<img src="~/images/top_bar_right.png" alt="Right Top Bar Group" title="Right Top Bar Group" width="150" align="right"/>

### Right Group

The fourth group, located on the far right side, contains more general utility functions.
Its first button brings the whole particle system into view by setting the camera position and the zoom level appropriately.
The second button just centers the viewport on the particle system using the center of its bounding rectangle, but it does not change the zoom level.
The third button only works in Simulation Mode and opens the Settings Panel.
The last button closes the application and is available in Init and Simulation Mode.



[1]: xref:AS2.Log
