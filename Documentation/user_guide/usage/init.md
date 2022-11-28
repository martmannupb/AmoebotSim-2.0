# Simulator Usage: Initialization Mode

(put image here when the init mode is finished)

The initialization mode (init mode) is opened by pressed the button on the top left. It serves as panel to place and initialize the system's particles by the use of generation algorithms. The exact generation algorithm used for every particle algorithm is defined in the particle algorithm class. 

## Usage

1. Algorithm Selection: Firstly, you start at the top of the initialization panel and choose the particle algorithm you want to execute in the dropdown.

2. Particle Generation: When the algorithm has been chosen, the parameters for the particle algorithm's generation algorithm are displayed below the dropdown. Additionally the system automatically generates a particle environment with the default settings you see there. If you want another setup, you can change the parameters and generate the system again. There are also options to load or save a system environment in this step or after step 3.

3. Final Adjustments: You can make additional adjustments to the environment by using the selection modes in the top bar. By default the "Selection" tool is highlighted, it opens the overview over the parameters (particle panel) of each particle after a click on the particle. In the particle panel you can set the attributes of each particle manually and even set a value for all particles in the system by pressing and holding the button for a short amount of time. The "Add" tool lets you manually add particles to the system, the "Remove" tool does the inverse. The simulator also supports a "Move" tool which can be used to change the placement of each particle.

4. Starting the Simulation: When you are happy with the environment, you can start the algorithm with the button below. This closes the initialization mode and starts the execution of the algorithm.