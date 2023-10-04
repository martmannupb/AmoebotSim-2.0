# Developer Guide: Reflection

A convenient feature of the simulator is that particle algorithms do not have to be registered manually.
The developer simply creates a new algorithm file using the template and the system will automatically detect the algorithm and make it available in the UI.
This system is implemented using reflection.

The [`AlgorithmManager`][1] class uses C#'s `System.Reflection.Assembly` class to detect and manage all available [`ParticleAlgorithm`][2] subclasses.
It finds the display name, the constructor, the `Init` method and the referenced initialization class of each algorithm and stores all algorithms in a dictionary.
Algorithm classes that have duplicate names or no constructor with the correct signature are rejected.
The class also defines a list of excluded algorithms in which the names of algorithms that should not be shown in the UI can be specified.
The [`AlgorithmManager`][1] uses a singleton pattern, meaning that one static instance is created when the application starts and is thereafter available to all other classes.
It is mainly used by the [`ParticleFactory`][3] to create and initialize particles when a simulation starts.

A very similar system exists for [`InitializationMethod`][5] subclasses: The [`InitializationMethodManager`][4] follows the same approach as the [`AlgorithmManager`][1].
It provides the mapping from initialization method names to the actual classes that is needed to associate algorithms with their initialization methods.
It also uses reflection to get the parameter list of the initializer's `Generate` method so that the parameters can be displayed in the Initialization Mode UI.



[1]: xref:AS2.AlgorithmManager
[2]: xref:AS2.Sim.ParticleAlgorithm
[3]: xref:AS2.Sim.ParticleFactory
[4]: xref:AS2.InitializationMethodManager
[5]: xref:AS2.InitializationMethod
