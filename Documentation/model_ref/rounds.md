# Model Reference: Round Simulation

This page explains how the synchronous Amoebot execution model is realized in the simulator.
As defined on the [Amoebot model pages](~/amoebot_model/home.md), the computation of an Amoebot particle system progresses in *synchronous rounds*.
In each round, all particles are activated simultaneously, performing computations based on the same *snapshot* of the system.
When all particles are finished, their states are updated according to their computations and the actions they chose to take during their activation are carried out.
Thus, a round is either a *look-compute-move* cycle or a *look-compute-beep* cycle, depending on what kind of actions are allowed in that round.
In the theoretical model, the system alternates between these two types of rounds.
In a look-compute-move round, the particles can modify their bonds and perform movements, while in a look-compute-beep round, they can modify their pin configurations and send beeps on the resulting circuits.


## Round Simulation Overview

In the simulator, one simulation round comprises both a look-compute-move round *and* the subsequent look-compute-beep round.
We call them the *movement phase* and the *beep phase*.
Thus, when the simulation is paused, the next step will always be a movement phase.
However, the phases are still handled separately and from the perspective of the particles, the grouping of the phases has no consequences except that the simulation always starts with a movement phase.

Every other aspect of the execution model is implemented according to the theoretical definition.
During their activations, the particles only have access to the system's state at the beginning of the phase (the "snapshot"), creating the illusion that they are activated simultaneously, even though the simulator activates them sequentially.
Even if a particle $p$ alters its public state attributes and its neighbor $q$ is activated afterwards by the simulator, the neighbor will not be able to access $p$'s new attribute values until the next phase.
If $q$ reads the state attributes of $p$, it will only read the values from before $p$ was activated.

The same holds for the actions the particles can take to communicate over circuits or to perform joint movements.
Such actions are evaluated and applied after all particles have been activated.
The process of a simulation round can be summarized as follows:
1. All particles' movement activations are executed
2. The particle states are updated and their movements are performed
	- First the bonds are set up, then the movements are simulated based on the bonds
3. All particles' beep activations are executed
4. The particle states are updated, circuits are computed and beeps and Messages are delivered

If an error occurs during any of these steps, be it an exception thrown by the algorithm or system code or a movement conflict, the round simulation is aborted and the previous simulation state is restored.


## Activation Methods

In the theoretical model, Amoebots are finite state machines, whose state transitions are instantaneous and can trigger a set of predefined actions.
To avoid having to define all states and their transitions explicitly, the behavior of Amoebot particles is defined using *activation methods* in the simulator.
As explained in the [algorithm development guide](~/user_guide/dev/home.md), Amoebot algorithms are implemented as classes that inherit from the [`ParticleAlgorithm`][1] class.
Such an algorithm class overrides the [`ActivateMove`][2] and the [`ActivateBeep`][3] method to define a particle's behavior in the two phases.
When the particle is activated, one of the two methods is executed, depending on which phase is currently being simulated.
Within each method, the particle's behavior is defined using C# code and the [`ParticleAlgorithm`][1] API.

### Particle Computation

Apart from planning movement and communication actions, the computational capabilities of a particle are the same in both activation methods.
The main parts of the general computation are generally reading and writing the particle's own attributes, looking for neighbors and reading their public attributes, and control logic.
You can read more about attributes on the [Particle Attributes reference page](attrs.md).
Most of these computations will be dependent on received beeps or Messages or have the purpose of determining which action has to be taken.
This is where the two activation methods are different.

### Movement Activation

In the movement phase, the [`ActivateMove`][2] method is called once on each particle.
This method is responsible for setting up the particle's bonds and scheduling its movement.
At the beginning of the phase, all possible bonds are active.
The particle can release and mark bonds using the [`ReleaseBond`][4] and [`MarkBond`][5] methods.
Movements are scheduled using the [`Expand`][6], [`ContractTail`][7], [`ContractHead`][8], [`PushHandover`][9], [`PullHandoverTail`][10] and [`PullHandoverHead`][11] methods.
The particle does not know how its neighbors have set their bonds or which movements they have scheduled and the movements are applied at the end of the phase, after all particles have been activated.
For more details about bonds and joint movements, please refer to the corresponding [reference page](bonds_jm.md).

### Beep Activation

The [`ActivateBeep`][3] method is the [`ActivateMove`][2] method's counterpart that is called during the beep phase.
In this method, the particle can set up its pin configuration and send beeps and Messages.
The [`PinConfiguration`][12] class provides most of the API for this purpose.
A detailed explanation of how pin configurations work can be found on the [Pin Configuration reference page](pin_cfgs.md).

After all particles have been activated, their pin configurations are used to construct the circuits, which are then used to deliver the beeps and Messages to the particles.
It should be noted here that beeps and Messages sent during the beep phase can be read during the next movement phase and also during the following beep phase *if the particle did not perform a movement*.



[1]: xref:Global.ParticleAlgorithm
[2]: xref:Global.ParticleAlgorithm.ActivateMove
[3]: xref:Global.ParticleAlgorithm.ActivateBeep
[4]: xref:Global.ParticleAlgorithm.ReleaseBond(Direction,System.Boolean)
[5]: xref:Global.ParticleAlgorithm.MarkBond(Direction,System.Boolean)
[6]: xref:Global.ParticleAlgorithm.Expand(Direction)
[7]: xref:Global.ParticleAlgorithm.ContractTail
[8]: xref:Global.ParticleAlgorithm.ContractHead
[9]: xref:Global.ParticleAlgorithm.PushHandover(Direction)
[10]: xref:Global.ParticleAlgorithm.PullHandoverTail(Direction)
[11]: xref:Global.ParticleAlgorithm.PullHandoverHead(Direction)
[12]: xref:Global.PinConfiguration
