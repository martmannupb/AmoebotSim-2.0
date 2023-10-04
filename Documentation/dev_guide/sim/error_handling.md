# Developer Guide: Error Handling

There are many places in the simulator where errors can occur.
While not all errors can be avoided, some can be handled without crashing the application, ideally allowing the algorithm developer to understand what caused the error.
In particular, this applies to errors caused by particles performing invalid actions.

To represent common types of errors, we use custom exception classes, as shown in the image below.
![Exception types](~/images/exception_types.png "Exception types")
Green exception types can be handled, red types cannot be handled and yellow types can only sometimes be handled without crashing.

The base exception type is [`AmoebotSimException`][1].
It represents all types of errors that are detected and categorized by the simulator.
Only these exceptions may be handled without crashing, every other type of exception will usually lead to some inconsistent state from which the application cannot recover.

We categorize the exceptions into [`SimulatorExceptions`][2] and [`ParticleExceptions`][3].
[`SimulatorExceptions`][2] are thrown when an error occurs anywhere in the simulator code, especially during the round simulation or initialization.
They are categorized further into [`SimulationExceptions`][4] and [`SimulatorStateExceptions`][5].
A [`SimulationException`][4] is thrown when an error occurs during the round simulation that is caused by some inconsistency in the particle system's actions.
For example, joint movement conflicts or a disconnection of the system detected during the movement simulation cause [`SimulationExceptions`][4] to be thrown.
They can be handled by resetting the system state to the previous round and sometimes, information on where the error occurred can be given (e.g., the position of the first encounted disconnected particle).
The other type of [`SimulatorException`][2], the [`SimulatorStateException`][5], cannot be handled.
This type of exception is typically thrown outside of the round simulation when some inconsistency in the application's own state is detected, such as an unknown algorithm name being selected, or an invalid action is attempted, like trying to step one round forward when the system is already in the latest round.

The [`ParticleExceptions`][3] are also categorized further into two subtypes.
[`AlgorithmExceptions`][6] represent errors occurring directly in the algorithm code that are not related to the particle's behavior.
For example, if a division by 0 occurs in a particle's activation method, the exception thrown by this operation will be converted into an [`AlgorithmException`][6], which can be handled by resetting the round.
Any error caused by a particle attempting to perform an invalid action is represented by an [`InvalidActionException`][7].
This mainly covers invalid movement operations like trying to expand when the particle is already expanded.
All types of [`ParticleExceptions`][3] can be handled by resetting the simulation state to the previous round.
Additionally, these exception types store the particle that caused the error so that the developer can identify the cause of the problem more easily.
In many cases, it is even possible to print the stack trace showing the exact location in the algorithm code where the exception was thrown.



[1]: xref:AS2.Sim.AmoebotSimException
[2]: xref:AS2.Sim.SimulatorException
[3]: xref:AS2.Sim.ParticleException
[4]: xref:AS2.Sim.SimulationException
[5]: xref:AS2.Sim.SimulatorStateException
[6]: xref:AS2.Sim.AlgorithmException
[7]: xref:AS2.Sim.InvalidActionException
