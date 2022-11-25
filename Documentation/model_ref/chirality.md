# Model Reference: Chirality

- *Chirality* describes the sense of rotation direction of a particle
- The *global chirality* defines the positive rotation direction to be counter-clockwise and the negative rotation direction to be clockwise
- Every particle in the system has a *local chirality*
	- The value is represented by a `bool`
	- `true` chirality means that the local chirality matches the global chirality: A rotation in the positive local direction will rotate in the global positive rotation direction (which is counter-clockwise)
	- `false` chirality means that the local chirality is the opposite of the global chirality
- The particle *does not know* its own local chirality
	- Nor does it have a way to find out
- Particles in a system can establish a common chirality using a *chirality agreement algorithm*
	- The example algorithm uses 2 pins and establishes a common chirality with high probability
	- Particles have to actively flip rotation directions if their chirality does not match the established common chirality
- Particle systems can be initialized with different chirality settings
	- All particles can share a chirality
	- The chirality can be random
	- Chirality of individual particles can be set manually as well
- The chirality of a particle cannot be changed while the simulation is running, it must be set during initialization
