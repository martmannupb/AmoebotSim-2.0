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


<img src="~/images/settings_panel.png" alt="Settings Panel" title="Settings Panel" width="200" align="right"/>

## Settings Panel

The Settings Panel is opened by pressing the Top Bar button with the gear icon, which is the second-last button in the Top Bar.
It will appear on the right side of the UI, where the Initialization Panel is in Init Mode.
The panel contains various general settings, mainly for modifying the visualization.

**Animations On/Off**  
This setting toggles the movement animations on and off.
Animations make the particle movements much easier to understand, but the simulation might run faster with animations turned off.

**Beep Repeat On/Off** and **Beep Repeat Time (s)**  
*Deprecated*

**Fullscreen**  
When the application is built and run as a standalone outside the Unity Editor, this setting can be used to toggle between fullscreen and windowed mode.
It is recommended to run the application in fullscreen mode.
This setting has no effect when the simulator runs in the Unity Editor.

**Camera Angle**  
This slider controls the rotation angle of the viewport.
Moving the slider to the right rotates the camera counter-clockwise in 30 degree steps.

**Compass ov. Arrows**  
This setting changes the visualization of compass directions in the particle overlay showing the compasses of all particles.
When it is enabled, the compass directions are displayed as arrows.
When it is disabled, the directions are represented by their textual names (like E, NNE, etc.) instead.

**Circuit Border**  
Toggles a thin black border for the circuit connections between particles.
For some circuit colors, this makes the circuits much easier to see.
The border is not visible if a beep is sent on the circuit.

**Circular Ring**  
This setting toggles the ring around the particles in graph view mode (see view modes above).
Turning the rings off removes the color information from the particles but may be useful for creating screenshots with a particular style.

**Anti Aliasing**  
Controls the level of Multi-Sample Anti-aliasing performed by the GPU.
*Does not seem to have a visible effect*

**UI Animations**  
Toggles the sliding animation of the Randomization bar in the Particle Panel on and off.


<img src="~/images/particle_panel_sim.png" alt="Particle Panel (Simulation Mode)" title="Particle Panel (Simulation Mode)" width="200" align="right"/>

## Particle Panel

The Particle Panel is available by clicking a particle with the Selection tool activated, both in Simulation and Init Mode.
The difference between the two modes is that the panel now displays the [*particle attributes*](~/model_ref/attrs.md) instead of the particle's initialization parameters.
Note that although the same algorithm as in the Init Mode example is selected (Line Formation), the Particle Panel displays different content.

The only content that is displayed in both modes are the chirality and compass direction, which are neither initialization parameters nor particle attributes.
In Simulation Mode, their values cannot be changed anymore.
The attribute values as well as the Anchor state (the little button left of the "Particle" text) can only be edited when the simulation state is in the latest round of the history.
If you want to change a particle's state in an earlier round, you will need to cut off the rest of the history.


## Hotkeys

The simulation environment provides several hotkeys so that its functions can be accessed more easily.
Here is a short overview of the currently set hotkeys:

Hotkey                             | Function
-----------------------------------|----------------------
Right Ctrl + Arrow Keys or WASD    | Camera Movement
Space                              | Play/Pause
PageUp or Shift + Right Arrow Key  | Step Forward
PageDown or Shift + Left Arrow Key | Step Back
H                                  | Hide/Show UI
Shift + C                          | Center Camera
Shift + V                          | Take Screenshot
Shift + S                          | Save Simulation State
Shift + O                          | Load Simulation State
Shift + Q                          | Exit Simulator

If you want to modify the hotkeys, this can be done in the [`UIHandler`][1] class found in `Assets/Code/Graphics/UI/UIHandler.cs`.
You can read more about this class and the UI system implementation on the [Dev Guide pages](~/dev_guide/ui.md).


[1]: xref:AS2.UI.UIHandler
