# Simulator Usage: Simulation Mode

Pressing the "Start" button in [Init Mode](init.md) leads you to the second main mode of the simulation environment, which is *Simulation Mode*.
In Simulation Mode, the computation of the particle system that was set up in Init Mode is simulated in *rounds*, as explained on the [reference pages](~/model_ref/rounds.md).
The state of the particle system can be inspected in every round, even if the simulation has already progressed further than the currently viewed round, thanks to the history feature.
Additionally, particle states can be modified in the latest system state and the last simulated rounds can be deleted, providing a way to play out different scenarios based on manual input.

As mentioned in the [Init Mode guide](init.md), the UI in Simulation Mode is very similar to the Init Mode UI:
![Simulation Mode UI Overview](~/images/sim_mode_overview.png "Simulation Mode UI Overview")
On this page, we only consider the functionality that has not been explained on the Init Mode page.


## Bottom Bar

![Bottom Bar](~/images/bottom_bar.png "Bottom Bar")

The Bottom Bar is only enabled in Simulation Mode.
It contains all UI elements used to control the simulation progress.

On the left side of the bar is the round speed slider.
This slider controls how much time passes between two rounds while the simulation is playing.
The interval can be adjusted in steps, with the maximum and minimum values being 4s and 0s, respectively.
Setting the interval to 0s will run the simulation as fast as possible but may make the application unresponsive.

The three buttons in the middle of the bar are the Play/Pause button (in the middle) and the Step buttons.
The Play/Pause button toggles between the playing and the paused simulator state.
While playing, the simulation will automatically progress one round at a time with the speed set using the round speed slider.
With the left and right Step buttons, the simulation state can be moved back and forward one round while the simulation is paused.
However, it can only be moved in the range of already simulated rounds.
If stepping in a direction is not possible, the corresponding Step button is disabled.

The right side of the bar contains additional round controls and displays the current and maximum round.
The slider indicates the currently selected round relative to the range of available rounds.
It can be used to quickly scroll through the history while the simulation is paused.
If the toggle button to the left of the slider is disabled, the simulation state will only be updated when the slider is released.
If it is enabled, the state will already be updated while the slider is being moved, which makes navigation easier but may be less performant.
If the Play button is pressed while a round before the latest round is selected, the simulator will first step through the remaining history before continuing to simulate new rounds.

The button left of the toggle is the Cut button.
Pressing it will delete all simulated rounds after the currently selected round, i.e., cut off the end of the history.
It can be used to restart the simulation (if pressed in round 0) or to replay a part of an algorithm that includes random behavior.
This can be useful if some error only occurs when certain random decisions are made because the simulation can easily be repeated until these conditions are met.


## Top Bar

![Top Bar](~/images/top_bar_sim.png "Top Bar")

The Top Bar shows various changes compared to Init Mode.
Minor changes include that the Save button (second button in the left group) now works and the Settings button (second button in the right group) is now enabled.
The Settings button is explained further below.

### Tool Menu

In the middle-left menu (the tool menu), the tools specific to Init Mode have disappeared and one new tool is visible.
The Selection tool works just like in Init Mode: Clicking on a particle opens the Particle Panel and clicking anywhere else in the Central Area closes it again.

<img src="~/images/pset_move_tool.png" alt="Partition Set Move Tool" title="Partition Set Move Tool" width="400" align="right"/>

The new tool is the Partition Set Move tool.
In algorithms that use circuits with non-singleton partition sets, this tool can be used to temporarily move around partition sets with the mouse.
Non-singleton and non-empty partition sets are visualized as small circles inside of the particles, with lines connecting the circle to the pins contained in the partition set.
These circles can be grabbed and dragged around using the left mouse button while the Partition Set Move tool is selected.
However, changing the partition set placement mode (see below) or moving to a different round will discard all partition set movements made with this tool.
For more information on partition sets, please refer to the corresponding [reference page](~/model_ref/pin_cfgs.md).

<img src="~/images/view_menu_sim.png" alt="View Menu" title="View Menu" width="150" align="right"/>

### View Menu

The view menu has not changed relative to Init Mode, but most of its effects can only be seen in Simulation Mode.
The first button cycles through the three different view modes:

Hexagonal View <img src="~/images/view_hex.png" alt="Hexagonal View" title="Hexagonal View" height="25"/>  | Rounded Hexagonal View <img src="~/images/view_circHex.png" alt="Rounded Hexagonal View" title="Rounded Hexagonal View" height="25"/> | Graph View <img src="~/images/view_circ.png" alt="Graph View" title="Graph View" height="25"/>
:---------------------------------------------------------------------------------------------------------:|:-------------------------------------------------------------------------------------------------------------------------------------:|:---------------------------------------------------------------------------------------------------:
<img src="~/images/view_mode_hex.png" alt="Hexagonal View Mode" title="Hexagonal View Mode" height="225"/> | <img src="~/images/view_mode_round.png" alt="Round Hexagonal View Mode" title="Round Hexagonal View Mode" height="225"/>              | <img src="~/images/view_mode_graph.png" alt="Graph View Mode" title="Graph View Mode" height="225"/>

The second button cycles through four placement modes for [partition sets](~/model_ref/pin_cfgs.md):


Default Mode <img src="~/images/view_pSetPos_def_v.png" alt="Default Mode Icon" title="Default Mode Icon" height="25"/> | Disk mode <img src="~/images/view_pSetPos_auto2D_v.png" alt="Disk Mode Icon" title="Disk Mode Icon" height="25"/> | Circle Mode <img src="~/images/view_pSetPos_auto_v.png" alt="Circle Mode Icon" title="Circle Mode Icon" height="25"/> | Line Mode <img src="~/images/view_pSetPos_line_v.png" alt="Line Mode Icon" title="Line Mode Icon" height="25"/>
:----------------------------------------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------------------------:|:---------------------------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------------------:
<img src="~/images/pset_mode_default.png" alt="Default Mode" title="Default Mode" height="175"/>                        | <img src="~/images/pset_mode_disk.png" alt="Automatic Disk Mode" title="Automatic Disk Mode" height="175"/>       | <img src="~/images/pset_mode_circle.png" alt="Automatic Circle Mode" title="Automatic Circle Mode" height="175"/>     | <img src="~/images/pset_mode_line.png" alt="Line Mode" title="Line Mode" height="175"/>

The default mode prioritizes custom partition set placements defined in the algorithm code and falls back to the second mode if no position was set.
The second mode is automatic disk placement.
It distributes the partition sets inside a circle according to the average positions of the pins they contain.
Afterwards, it uses a variant of [LLoyd's algorithm](https://en.wikipedia.org/wiki/Lloyd%27s_algorithm) to move partition sets that are too close further apart from each other, nudging them in a random direction if their positions are equal.
The third mode is automatic circle placement.
It is similar to disk placement but it distributes the partition sets directly on the circle instead of the area inside of the circle.
Finally, the fourth mode is line placement.
It simply distributes the partition sets evenly on a straight line in the center of the particle.
The line is vertical in contracted particles and orthogonal to the expansion direction in expanded particles.
The algorithm shown above (Boundary Test) defines custom partition set positions, which is why the default mode placement differs from disk placement.

The third and fourth button simply toggle the circuit and bond visualization on and off:

**Circuits and bonds**                                                                                           | **Circuits and no bonds**
:---------------------------------------------------------------------------------------------------------------:|:---------------------------------------------------------------------------------------------------------------------:
<img src="~/images/+circuits+bonds.png" alt="Circuits and bonds" title="Circuits and bonds" height="250"/>       | <img src="~/images/+circuits-bonds.png" alt="Circuits and no bonds" title="Circuits and no bonds" height="250"/>
**No circuits and bonds**                                                                                        | **No circuits and no bonds**
<img src="~/images/-circuits+bonds.png" alt="No circuits and bonds" title="No circuits and bonds" height="250"/> | <img src="~/images/-circuits-bonds.png" alt="No circuits and no bonds" title="No circuits and no bonds" height="250"/>

TODO







- Parts that have changed
	- Settings Panel
		- Opened by clicking Settings button (gear icon)
		- Animations On/Off:
			- Toggles movement animations
		- Beep Repeat On/Off:
			- Toggles repeated beeps while simulation is paused
			- _Deprecated_
		- Beep Repeat Time (s):
			- Time between beep repetitions
			- _Deprecated_
		- Fullscreen:
			- Toggles between fullscreen and windowed mode
			- Only works if application is built
		- Camera Angle:
			- Changes rotation angle of the viewport in 30° steps
		- Compass Ov. Arrows:
			- For particle overlay showing compass directions
			- Toggles between arrows and text
		- Circuit Border:
			- Toggles black border of pin connections between particles
		- Circular Ring:
			- Toggles colored ring around particles in graph view mode
		- Anti Aliasing:
			- Changes anti-aliasing steps
		- UI Animations:
			- Toggles sliding animation of Randomization side bar
- Parts that have not changed
	- Particle Panel
		- Available in both modes by selecting a particle using the Selection tool
		- But shows attributes now instead of init parameters
			- Same algorithm (Line Formation) but different content!
		- Chirality and compass dir cannot be changed any more
		- Attributes cannot be changed when not in the latest round
	- Log Panel
		- Exactly the same
- Hotkeys










(put image here when everything is implemented)

The simulation mode is the inverse of the initialization mode. Here you can simulate a particle algorithm with the currenty instantiated particles.

## Bottom Bar

The bottom bar serves as the instance to control the simulation progress. You can play/pause the simulation, set the speed of the simulation and see the current round and even scroll back in time with the integrated simulation history slider on the right.

## Top Bar

The top bar offers various functions grouped into four sub-menus which are explained in this section.

### File Menu (Left)

The file menu offers options like the starting of the init mode, the saving and loading of simulation states or the ability to make screenshots of the simulator.

### Selection Menu (Middle Left)

The "Selection" tool opens the particle panel after a click on the particle (explained below). Additionally, you also have the possibility to manually change the placement of partition sets.

### View Menu (Middle Right)

This menu is used to control the various views of the simulator. E.g you have the option to show/hide circuits in some of the views, you can toggle bonds on and off, show the background grid to see the coordinates of each particle or scroll through the different view modes. The "UI" button hides the world space UI overlay which shows particle attributes in the view and can be triggered in the particle panel by a click on an attribute. Another button is used to switch through the different partition set view modes: The default mode priorizes code overrides of partition set positions that can be set via code, then there is automatic positioning of partition sets based on their local pin positions, finally you have the option to set the partition set positions in a straight (mostly horizontal) line. Also, there is another button to show the grid positions of particles. We recommend you just press the buttons and see what they do, you cannot really do something wrong here.

### Setting Menu (Right)

The setting menu supports the centering of the camera to the particles (which is helpful if you lost track of the particles because of funny camera movement behavior). There is a settings panel you can open to set various flags and parameters. You can exit the sim by the "X" button on the top right.

## Particle Panel

The particle panel is opened by pressing on a particle with the highlighted "Selection" tool. It works both in simulation and in init mode and displayed the state of the particle with its position and attributes. An attribute can be clicked to display it in the world space UI overlay (opens a box over each particle that shows the attribute's state for this particle in the current round). If the attribute is clicked and held for some time, you can apply this attribute to all other particles. You can change the attribute for the currently selected particle in the dropdown/toggle/textfield/other.

## Hotkeys

Currently we support several hotkeys, you can change the keys in the UIHandler class. Here is a short overview over the currently set hotkeys:

- Right Ctrl + Arrow Keys : Camera Movement
- Space : Play/Pause
- PageUp OR Shift + Right Arrow Key : Step Forward
- PageDown OR Shift + Left Arrow Key : Step Back
- Shift + H : Hide UI
- H : Show UI
- Shift + C : Center Camera
- Shift + V : Screenshot
- Shift + S : Save Sim State
- Shift + O : Open Sim State
- Shift + Q : Exit
