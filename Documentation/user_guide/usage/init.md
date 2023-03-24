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
Particles can be selected by left-clicking on a particle and deselected by left-clicking anywhere else in the Central Area.


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

TODO


- Top Bar
	- General tool bar visible in both modes (but with slightly different content)
	- Organized into 4 blocks
	- Left block
		- First button enters Init Mode from Simulation Mode (no use in Init Mode)
		- Second and third button are for saving and loading simulation states (saving only works in Simulation Mode)
		- Fourth button is for taking screenshots
	- Middle left block (tool menu)
		- First 4 buttons are tool selection
			- Selection tool: Select particles to open the Particle Panel
			- Add tool: Add new particles by clicking or clicking and dragging (for expanded particles)
			- Remove tool: Remove particles by clicking
			- Move tool: Select a particle and then place it somewhere else
		- Next 2 elements are dropdown menus
			- Select chirality and compass direction for particles placed with the Add tool
	- Middle right block (view menu)
		- Several options for modifying the visualization
		- First button cycles through particle visualization modes
			- Hexagonal
			- Hexagonal grid with round particles
			- Graph view (legacy)
		- Second button cycles through partition set visualization modes (more relevant in Simulation Mode)
		- Third and fourth button toggle circuits and bonds (also only visible in Simulation Mode)
		- "UI" button disables global overlay
		- "#" button toggles grid coordinate overlay
	- Right block
		- Camera button centers the camera to the particles
		- Gear button opens the Settings menu (only works in Simulation Mode)
			- _Not sure why this only works in Sim Mode_
				- Maybe because Init Panel would have to be disabled?
		- "X" button closes the application
- Log Panel
	- Shows log messages
	- Disappears after a few seconds
	- Button in the top right corner allows saving the log as a text file







(put image here when the init mode is finished)

The initialization mode (init mode) is opened by pressed the button on the top left. It serves as panel to place and initialize the system's particles by the use of generation algorithms. The exact generation algorithm used for every particle algorithm is defined in the particle algorithm class. 

## Usage

1. Algorithm Selection: Firstly, you start at the top of the initialization panel and choose the particle algorithm you want to execute in the dropdown.

2. Particle Generation: When the algorithm has been chosen, the parameters for the particle algorithm's generation algorithm are displayed below the dropdown. Additionally the system automatically generates a particle environment with the default settings you see there. If you want another setup, you can change the parameters and generate the system again. There are also options to load or save a system environment in this step or after step 3.

3. Final Adjustments: You can make additional adjustments to the environment by using the selection modes in the top bar. By default the "Selection" tool is highlighted, it opens the overview over the parameters (particle panel) of each particle after a click on the particle. In the particle panel you can set the attributes of each particle manually and even set a value for all particles in the system by pressing and holding the button for a short amount of time. The "Add" tool lets you manually add particles to the system, the "Remove" tool does the inverse. The simulator also supports a "Move" tool which can be used to change the placement of each particle.

4. Starting the Simulation: When you are happy with the environment, you can start the algorithm with the button below. This closes the initialization mode and starts the execution of the algorithm.