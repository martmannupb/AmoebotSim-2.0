# Installation Guide

## Installing Unity

The simulation environment is developed for **Unity version 2021.3.3f1**.

To install this version, you can roughly follow the instructions on the [Unity download page][1].
The steps are described in more detail below.

### 1. Install Unity Hub

The Unity Hub is an application that manages your Unity installs and projects.

- For *Windows and Mac*: Download the correct installer from the [download page][1] and run it.
- For *Linux*: Follow the instructions on the [Linux installation page][2].

### 2. Install Unity Editor and an IDE

- Launch Unity Hub
- Download and install Unity version **2021.3.3f1**:
	- Open the [download archive][3]
	- Find version 2021.3.3f1 and click the green "Unity Hub" button
	- The website should now display this version in Unity Hub
- In the "Add modules" window, select the correct modules:
	- Under "Dev Tools", select Microsoft Visual Studio or another supported IDE that works on your platform
	- Under "Platforms", select the Build Support option for your platform (Dedicated Server Build is not required)
	- Also select "Documentation"
- Click "Install" and wait for the installation to finish
	> [!NOTE]
	> When installing Microsoft Visual Studio, you may be prompted to select "Workloads" to install. Please select the "Game development with Unity" Workload.

When Unity has been installed successfully, you can move on to [setting up the simulator](sim.md).

[1]: https://unity.com/download
[2]: https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux
[3]: https://unity3d.com/get-unity/download/archive?_ga=2.168415331.391564407.1667555100-1263457702.1655223585
