# Simulator Usage: Unity Editor Overview

Since AmoebotSim 2.0 is developed with Unity, you will need a basic understanding of the Unity Editor to use it.
There are much more in-depth tutorials on the [official Unity tutorial websites](https://learn.unity.com/tutorials), but the following short overview should suffice for most use cases.

> [!NOTE]
> The screenshots on the User Guide pages were taken in an older Unity version. The UI layout in newer versions may be slightly different.


## The Editor Interface

After opening the simulator project through Unity Hub and opening the *Simulator* Scene, you should see the following window:

![Editor Window Overview](~/images/editor_overview.png "The Unity Editor with Default layout")

If your Editor looks different, use the dropdown menu in the top-right corner and select the "Default" layout.
You can resize and move the windows around to fit your needs.
In case you accidentally close one of the tabs or windows, you can reopen it using the "Window" > "General" menu.


### Scene and Game View

This window displays either the *Scene View* or the *Game View*, depending on which tab is selected in the top-left corner of the window.

The *Scene View* shows the UI elements of the simulator as well as everything else that is visible in the application before it runs.
It is primarily used by developers who want to work on the simulation environment and is of little importance to the users of the simulator.

The *Game View* shows the graphical output of the simulator and allows the user to interact with the application's UI while it is running.
The window will automatically switch to the Game View when the Play button (directly above the window) is pressed.
The toolbar at the top of the Game View allows you to emulate a screen size and resolution other than that of your screen.
We recommend using the "Full HD (1920x1080)" setting to ensure that all UI elements are visible.

![Editor Game View](~/images/editor_game_view.png "Game View with Full HD resolution")


### Project Explorer and Console Window

This window displays a file explorer or a console window that lists log messages and exception notifications.
The Project window displays the content of the `Assets` folder by default.
For users, the `Assets/Code` folder is the most relevant as it contains the source code of the simulator and all Amoebot algorithms.
Double-clicking on any C# file in this directory will open the file in your installed IDE.


### Hierarchy Window

The Hierarchy window displays the content of the current Scene.
[Scenes](https://docs.unity3d.com/Manual/CreatingScenes.html) are used in Unity to organize different parts of the application into manageable chunks, for example, by putting each level of a game into its own Scene.
AmoebotSim 2.0 has only a single Scene, called *Simulator*.

The content of a Scene is a hierarchy of [*GameObjects*](https://docs.unity3d.com/Manual/GameObjects.html).
GameObjects are entities in the application that can provide a certain functionality, interact with each other and display things on the screen.
The only GameObject in the *Simulator* Scene that is relevant to users is the **Algorithm Generator**.
Please refer to the [algorithm creation guide](../dev/creation.md) for more information on this GameObject.


### Inspector Window

When a GameObject is selected in the Hierarchy window, its *Components* are shown in the Inspector.
The Components of a GameObject define its properties and functionality.
For example, every GameObject has a *Transform* Component defining its position, rotation and scale in the 3-dimensional game world.
Some C# scripts can be added to GameObjects as Components.
The **Algorithm Generator** GameObject has the `AlgorithmGenerator.cs` script attached as its only Component (apart from its Transform), giving it the functionality to create new algorithm files.

![Editor Inspector](~/images/editor_inspector.png "The Inspector window with the Algorithm Generator GameObject selected")

If you select a C# script in the Project window, the Inspector window will show a preview of the file's content.
For users, the Inspector is only relevant for algorithm creation and for previewing file content before opening it in the IDE.


### Next Steps

Read the [Initialization Mode](init.md) and the [Simulation Mode guide](sim.md) to learn about the simulator's UI and run your first simulations.
