# Welcome to the **AmoebotSim 2.0** Documentation

AmoebotSim 2.0 is a simulation environment for an extended version of the [geometric amoebot model](amoebot_model/basics.md) that supports communication via [reconfigurable circuits](amoebot_model/circuits.md) as well as [joint movement operations](amoebot_model/jm.md).

It is designed to facilitate the development of algorithms for this model by providing a platform on which algorithms can be tested in various scenarios, inspected at runtime and visualized easily.
The simulator's main features are:
- An extensive API for implementing custom amoebot algorithms using C# code
- A graphical user interface that provides a visual representation of the amoebot system and that lets you control the simulation, inspect and edit the amoebot particle states, adjust the visualization, save and load simulation states, and more
- A history functionality that allows you to review all steps of the simulation
- A detailed documentation that explains the concepts behind the code as well as the API



## Getting Started

If you are new to the amoebot model, we recommend visiting the [Amoebot Model](amoebot_model/home.md) pages, which explain the theoretical foundation and the basic technical terms.
These pages also reference the original publications in which the amoebot model and its extensions were introduced.

If you would like to install the simulator, visit the [Installation Guide](installation_guide/home.md).
After the installation, you can run the simulator and view the example algorithms by following the [first part of the User Guide](user_guide/usage/home.md).

The [second part of the User Guide](user_guide/dev/home.md) provides a detailed description of how new algorithms are created and implemented using an example walkthrough.

Visit the [Model Reference](model_ref/home.md) pages to deepen your understanding of the underlying concepts and how they are realized in the simulator.
These pages are designed to be used as a reference while implementing your algorithms.
The [API Reference](api/index.md) contains all of the code documentation that should also be accessible through your IDE.

If you want to modify or extend the simulator itself, the [Developer Guide](dev_guide/home.md) provides a rough outline of the simulator's architecture and should give you an idea of what needs to be changed for your purpose.



## Acknowledgements

AmoebotSim 2.0 was created at the [Theory of Distributed Systems Group][3] at [Paderborn University][4].
The simulator was designed and implemented by Tobias Maurer (rendering and GUI) and Matthias Artmann (simulation and API) under the supervision of Andreas Padalkin and Daniel Warner.



## Citation

If you use AmoebotSim 2.0 for your research, please cite the project as follows:

> Matthias Artmann, Tobias Maurer, Andreas Padalkin, Daniel Warner. "AmoebotSim 2.0: A Visual Simulation Environment for the Amoebot Model with Reconfigurable Circuits and Joint Movements", available online at https://github.com/martmannupb/AmoebotSim-2.0, 2025.



## Contact

If you'd like to get into contact with us, feel free to send an E-Mail to matthias.artmann@uni-paderborn.de.
To learn more about our research on programmable matter, visit our [University homepage][6].




[1]: https://dl.acm.org/doi/10.1145/2612669.2612712
[2]: https://arxiv.org/abs/2105.05071v1
[3]: https://cs.uni-paderborn.de/en/ti
[4]: https://www.uni-paderborn.de/en/
[5]: https://arxiv.org/abs/2305.06146
[6]: https://cs.uni-paderborn.de/en/ti/forschung/forschungsprojekte/programmable-matter
