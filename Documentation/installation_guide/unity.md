# Installation Guide

## Installing Unity

The simulation environment is developed for **Unity version 2021.3.3f1**.
If this particular version is not available for your platform, a later 2021.x version should work as well.

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
	- Find version 2021.3.3f1 (or later if this is not available for your platform) and click the green "Unity Hub" button
	- The website should now display this version in Unity Hub
- In the "Add modules" window, select the correct modules:
	- Under "Dev Tools", select Microsoft Visual Studio or another supported IDE that works on your platform
	- Under "Platforms", select the Build Support option for your platform (Dedicated Server Build is not required)
	- Also select "Documentation"
- Click "Install" and wait for the installation to finish
	> [!NOTE]
	> When installing Microsoft Visual Studio, you may be prompted to select "Workloads" to install. Please select the "Game development with Unity" Workload.

### 3. Get a Unity License

- It may be necessary that you create a Unity profile and obtain a license to proceed
- You can do both from the Unity Hub, by clicking the profile button in the top-left corner and selecting "Manage licenses"  
	<img src="~/images/unity_licenses.png" alt="Manage licenses" title="Manage licenses from Unity Hub" width="250"/>
- If you are prompted to log in before managing your licenses, press the "Sign in" button
	- This will redirect you to the Unity login page, where you can log into your Unity profile or register a new profile by creating a Unity ID
- After signing in, you can press the "Add license" button in the Unity Hub and select "Get a free personal license" (see also [this support page][4])
	- The free personal license grants access to all Unity features required to work with the simulator

When Unity has been installed successfully, you can move on to [setting up the simulator](sim.md).

[1]: https://unity.com/download
[2]: https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux
[3]: https://unity3d.com/get-unity/download/archive?_ga=2.168415331.391564407.1667555100-1263457702.1655223585
[4]: https://support.unity.com/hc/en-us/articles/211438683-How-do-I-activate-my-license-
