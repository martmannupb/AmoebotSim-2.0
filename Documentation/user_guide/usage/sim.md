# Simulator Usage: Simulation Mode

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

- Arrow Keys : Camera Movement
- Space : Play/Pause
- PageUp OR LCtrl + Right Arrow Key : Step Forward
- PageDown OR LCtrl + Left Arrow Key : Step Back
- LCtrl + H : Hide UI
- H : Show UI
- LCtrl + C : Center Camera
- LCtrl + V : Screenshot
- LCtrl + S : Save Sim State
- LCtrl + O : Open Sim State
- LCtrl + Q : Exit
