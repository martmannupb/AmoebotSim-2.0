# User Guide: Configuration

The simulation environment has a configuration file that can be used to customize the appearance and the default values of some settings.
It is a JSON file called `config.json` which is located in the `Assets` folder of the Unity project.
Editing the configuration file is not necessary to use the simulator and we recommend that new users leave it as it is until there is a reason to change the settings (like adjusting default settings to match the algorithm under development).

## Editing the Configuration

<img src="~/images/config_inspector.png" alt="Configuration Editor" title="Configuration Editor in the Inspector window" width="300" align="right"/>

The easiest way to modify the configuration file is by using the `Configuration Editor` GameObject.
When this object is selected in the Hierarchy window, the Inspector will display controls for all configuration options, allowing you to easily inspect and modify their values.
The buttons at the bottom of the Inspector window allow you to load the current configuration file into the Inspector, save the current settings into the file, or reset the current settings to their default values.
Additionally, hovering over the option names will display helpful tooltips describing purpose of each option.

The configuration file has two parts.
The first part is labeled "Settings Menu" and contains options that are also available in the Settings Panel while the simulator is running in Simulation Mode.
These configuration options define the initial values of those settings.
They will be modified if the "Save Settings" button is pressed in the Settings Panel.

The second part is labeled "Additional Configuration" and contains data that is not available in the Settings Panel.
Here, you will find mostly visualization options like the color of the hexagonal amoebot border and the colors of beep highlights.
You can also edit the available colors for circuits and define a custom set of colors available when implementing an amoebot algorithm.


## Modifying the Available Options

For advanced users, it might be interesting to customize the configuration file by adding new options.
This is possible by editing the [`Assets/Code/SaveLoad/Config/ConfigData.cs`][1] file.
Adding new members to the [`AdditionalConfiguration`][2] class will automatically make these options available in the Inspector.
You can add tooltips using the `[Tooltip("text")]` attribute, as demonstrated by the existing options.
Only serializable data types are allowed as options, other types will not be recognized by the JSON utility.
The initial values set in this class serve as the default values for the options, so make sure to use reasonable values, even if you intend setting the values manually later.

Note that once the options have been added to the class and the file has been saved, Unity needs to recompile the script before the new options appear in the Inspector window.
To add the options to the actual configuration file, you still have to press the "Save Config" button at the bottom of the Inspector.

At runtime, you can access the new options through the static [`Config`][3] class.
Its [`ConfigData`][4] attribute gives you direct access to the configuration data that is loaded when the application starts.
The data can even be modified and written back to the config file using the [`Config`][3] interface.



[1]: xref:AS2.ConfigData
[2]: xref:AS2.ConfigData.AdditionalConfiguration
[3]: xref:AS2.Config
[4]: xref:AS2.Config.ConfigData
