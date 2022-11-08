# Installation Guide

## Simulator Setup

After [installing Unity](unity.md), you can setup the simulator project.

### 1. Import the Project

- Clone the repository (TODO: Link) into a new folder
- Add the project to Unity Hub
	- Open Unity Hub and select "Open" > "Add project from disk" in the "Projects" tab
	- Find the folder containing the repository, select the subfolder `AmoebotSim 2.0` and click "Add project"
	- The project should now be listed in Unity Hub with the correct Editor version number next to it
- Click the project to open it in the Unity Editor (this may take a few minutes)

### 2. Test the Project

- When the Editor has finished importing the project, press the Play button in the middle of the top bar to run the simulator
	![Play Button Image](../images/play_button.png)
- The simulator should now display an example algorithm
- Click the Play button again to close the simulator

### 3. Open the IDE

- Find the "Project" explorer window in the Editor (at the bottom if the Default layout is used; you can change the layout using the dropdown menu in the top-right corner)
- Under "Assets", open the folder `Code/Algorithms` and double-click one of the C# files
- Your installed IDE should now start and open the file
- If your IDE does not open, go to "Edit" > "Preferences...", select the "External Tools" tab and find your IDE in the "External Script Editor" dropdown

You can now start editing the code in your IDE.
Whenever you save your changes, the Unity project is compiled automatically and you can try out the simulator by pressing the Play button in the Editor.


### Next Steps

If you want to learn more about the Amoebot model and the particular version of the model implemented by the simulator, visit the [Amoebot Model](../amoebot_model/home.md) pages.

To learn how to implement your own Amoebot algorithms, read the [User Guide](../user_guide/home.md).
