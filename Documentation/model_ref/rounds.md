# Model Reference: Round Simulation

This page explains how the synchronous amoebot execution model is realized in the simulator.
As defined on the [amoebot model pages](~/amoebot_model/home.md), the computation of an amoebot structure progresses in *synchronous rounds*.
In each round, all amoebots are activated simultaneously, performing computations based on the same *snapshot* of the structure.
When all amoebots are finished, their states are updated according to their computations and the actions they chose to take during their activation are carried out.
Thus, a round is either a *look-compute-move* cycle or a *look-compute-beep* cycle, depending on what kind of actions are allowed in that round.
In the mathematical model, the structure alternates between these two types of rounds.
In a look-compute-move round, the amoebots can modify their bonds and perform movements, while in a look-compute-beep round, they can modify their pin configurations and send beeps on the resulting circuits.



## Round Simulation Overview

In the simulator, one simulation round comprises both a look-compute-move round *and* the subsequent look-compute-beep round.
We call them the *movement phase* and the *beep phase*.
Thus, when the simulation is paused, the next step will always be a movement phase.
However, the phases are still handled separately and from the perspective of the amoebots, the grouping of the phases has no consequences except that the simulation always starts with a movement phase.

![Simulation phases](~/images/round_sim_phases.png "Simulation phases")

Every other aspect of the execution model is implemented according to the mathematical definition.
During their activations, the amoebots only have access to the structure's state at the beginning of the phase (the "snapshot"), creating the illusion that they are activated simultaneously, even though the simulator activates them sequentially.
Even if an amoebot $p$ alters its public state attributes and its neighbor $q$ is activated afterwards by the simulator, the neighbor will not be able to access $p$'s new attribute values until the next phase.
If $q$ reads the state attributes of $p$, it will only read the values from before $p$ was activated.

The same holds for the actions the amoebots can take to communicate over circuits or to perform joint movements.
Such actions are evaluated and applied after all amoebots have been activated.
The process of a simulation round can be summarized as follows:
1. All amoebots' movement activations are executed
2. The amoebot states are updated and their movements are performed
	- First the bonds are set up, then the movements are simulated based on the bonds
3. All amoebots' beep activations are executed
4. The amoebot states are updated, circuits are computed and beeps and Messages are delivered

If an error occurs during any of these steps, be it an exception thrown by the algorithm or system code or a movement conflict, the round simulation is aborted and the previous simulation state is restored.



## Activation Methods

In the mathematical model, amoebots are finite state machines, whose state transitions are instantaneous and can trigger a set of predefined actions.
To avoid having to define all states and their transitions explicitly, the behavior of amoebots is defined using *activation methods* in the simulator.
As explained in the [algorithm development guide](~/user_guide/dev/home.md), amoebot algorithms are implemented as classes that inherit from the [`ParticleAlgorithm`][1] class.
Such an algorithm class overrides the [`ActivateMove`][2] and the [`ActivateBeep`][3] method to define an amoebot's behavior in the two phases.
When the amoebot is activated, one of the two methods is executed, depending on which phase is currently being simulated.
Within each method, the amoebot's behavior is defined using C# code and the [`ParticleAlgorithm`][1] API.

It is possible to leave one of the two activation methods empty in case an algorithm does not use any movements or circuit communication.
Leaving both methods empty results in an algorithm that does not do anything.
If both methods are implemented, they should be treated as activations for separate rounds, i.e., changing the amoebot's state in the movement activation method will have an effect on the subsequent beep activation.


### Particle Computation

Apart from planning movement and communication actions, the computational capabilities of an amoebot are the same in both activation methods.
The main parts of the computation are generally reading and writing the amoebot's own attributes, looking for neighbors and reading their public attributes, and control logic.
You can read more about attributes on the [Particle Attributes reference page](attrs.md).
Most of these computations will be dependent on received beeps or Messages or have the purpose of determining which action has to be taken.
This is where the two activation methods are different.


### Movement Activation

In the movement phase, the [`ActivateMove`][2] method is called once on each amoebot.
This method is responsible for setting up the amoebot's bonds and scheduling its movement.
At the beginning of the phase, all possible bonds are active.
The amoebot can release and mark bonds using the [`ReleaseBond`][4] and [`MarkBond`][5] methods.
Movements are scheduled using the [`Expand`][6], [`ContractTail`][7], [`ContractHead`][8], [`PushHandover`][9], [`PullHandoverTail`][10] and [`PullHandoverHead`][11] methods.
The amoebot does not know how its neighbors have set their bonds or which movements they have scheduled and the movements are applied at the end of the phase, after all amoebots have been activated.
For more details about bonds and joint movements, please refer to the corresponding [reference page](bonds_jm.md).

### Beep Activation

The [`ActivateBeep`][3] method is the [`ActivateMove`][2] method's counterpart that is called during the beep phase.
In this method, the amoebot can set up its pin configuration and send beeps and Messages.
The [`PinConfiguration`][12] class provides most of the API for this purpose.
A detailed explanation of how pin configurations work can be found on the [Pin Configuration reference page](pin_cfgs.md).

After all amoebots have been activated, their pin configurations are used to construct the circuits, which are then used to deliver the beeps and Messages to the amoebots.
It should be noted here that beeps and Messages sent during the beep phase can be read during the next movement phase and also during the following beep phase *if the amoebot did not perform a movement*.



[1]: xref:AS2.Sim.ParticleAlgorithm
[2]: xref:AS2.Sim.ParticleAlgorithm.ActivateMove
[3]: xref:AS2.Sim.ParticleAlgorithm.ActivateBeep
[4]: xref:AS2.Sim.ParticleAlgorithm.ReleaseBond(AS2.Direction,System.Boolean)
[5]: xref:AS2.Sim.ParticleAlgorithm.MarkBond(AS2.Direction,System.Boolean)
[6]: xref:AS2.Sim.ParticleAlgorithm.Expand(AS2.Direction)
[7]: xref:AS2.Sim.ParticleAlgorithm.ContractTail
[8]: xref:AS2.Sim.ParticleAlgorithm.ContractHead
[9]: xref:AS2.Sim.ParticleAlgorithm.PushHandover(AS2.Direction)
[10]: xref:AS2.Sim.ParticleAlgorithm.PullHandoverTail(AS2.Direction)
[11]: xref:AS2.Sim.ParticleAlgorithm.PullHandoverHead(AS2.Direction)
[12]: xref:AS2.Sim.PinConfiguration
