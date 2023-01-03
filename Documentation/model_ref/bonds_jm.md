# Model Reference: Bonds and Joint Movements

This page explains how movements in general and *joint movements* in particular are implemented in the simulation environment.
Please refer to the [Joint Movement Extension](~/amoebot_model/jm.md) page for details on the theoretical model.


## Bonds

Bonds represent connections between particles that define the "physical" structure of the system and allow particles to perform joint movements.
A bond fixes an edge between two neighboring particles and can be interpreted as a physical connection that prevents the particles from moving independently of each other, similar to the connection between the head and tail of an expanded particle.

At the beginning of a round, all possible bonds are present, i.e., there is a bond on every edge that is incident to two particles.
In the movement phase, particles can *release* bonds to remove them from the system, and perform individual movements.
It is enough for one of the two bonded particles to release the bond, but it is generally recommended to write algorithms in such a way that both particles agree on releasing a bond to prevent unwanted releases.
After all particles have been activated, the released bonds are removed and the scheduled movements are performed with the remaining bonds.
Finally, new bonds are created for the new positions of the particles and the process repeats in the next round (after the beep phase).

The graph induced by the bonds and particles must remain connected at all times.
If it becomes disconnected during the simulation, the round will be aborted and an error message will be logged.
In a real, physical environment, a disconnection like this could cause the particles to be separated from each other without any chance of connecting again.

### Movements

During the movement phase, particles can schedule their individual movements.
Every movement is either an *expansion* or a *contraction*.
In a handover, the pushing particle performs an expansion and the pulling particle performs a contraction.

From the local view of a particle performing a movement, one of its parts (head or tail) always stays where it is and the other one moves relative to it.
For example, when a contracted particle expands, its head moves in the expansion direction while its tail stays in place.
In a contraction, it can be either the head or the tail that moves.
The bonds of a moving particle behave according to the part they are connected to and the following rules:
- By default, a bond connected to the stationary part never moves relative to that part.
- In a contraction movement, the active bonds at the moving part are pulled towards the stationary part and connect to it at the end of the movement.
	If there are no other conflicts, a bond of the moving part can merge into a bond of the stationary part that lies on the destination edge of the moving bond.
- If the particle expands, it can *mark* some of its bonds to move with its head.
	Unmarked bonds will simply stay at the particle's tail.
	The bond pointing directly in the expansion direction will always move with the head and the bond exactly opposite of the expansion direction will always stay at the tail.
- Handover movements only allow the bond between the two movement partners to move:
	The pushing and the pulling particle must agree on the handover movement and the bond of the pushing particle that points into the push direction must be active.
	This bond will move with both moving parts of the two particles so that they stay connected, and it is allowed to rotate to match their new positions.
	Any other bonds on the moving part of the pulling particle are *transferred* to the head of the pushing particle without moving relative to the two stationary parts.
	The bonds at the stationary parts of the particles cannot move either, even if they are marked.





- Movements
	- Every individual particle movement is either an *expansion* or a *contraction* movement
		- In a handover, the pushing particle performs an expansion and the pulling particle performs a contraction
	- In the local view of a moving particle, one of its parts stays where it is and the other one moves
		- For example, when a contracted particle expands, its head moves to a new position while the tail stays in place
	- Bonds of the part that stays in place will never move relative to the particle
	- For bonds of the moving part, there are several possibilities:
		- If the particle **contracts**, the bonds are pulled with the moving part and then connect to the remaining part after the movement
			- If there are no other conflicts, moving bonds that end up in the same position as a non-moving bond will be merged into the non-moving bond
		- If the particle **expands**, it can *mark* some of its bonds to move with its head
			- Unmarked bonds will stay at the particle's tail
			- The bond pointing in the expansion direction will always be marked
			- The bond in the opposite direction can never be marked
		- **Handovers** are a special case
			- Both particles must agree on the handover, i.e., one must perform a push movement and the other must perform a pull movement
			- The bond pointing in the expansion direction of the pushing particle must be active
				- It will move and turn with the two particles to keep the connection intact
			- Any other bonds on the moving part of the pulling particle are *transferred* to the head of the pushing particle
			- Their relative position to the non-moving parts of the two particles do not change
			- Bond markings have no effect on this
	- Conflicts
		- If a *conflict* occurs during the movement phase, the round is aborted and an error is displayed
		- The simplest conflict is caused by multiple particles moving onto the same grid node
			- This can happen if two particles expand onto the same node
			- It can also happen if a particle is pushed onto another one, even without moving on its own
		- Other conflicts are caused by the bonds of neighboring particles not agreeing with their scheduled movements
			- E.g., if two expanded particles are connected by at least two bonds and only one of them tries to contract
- API
	- Bonds can be released using `ReleaseBond(Direction d, bool head)`
	- Bonds can be marked using `MarkBond(Direction d, bool head)`
	- Calling `UseAutomaticBonds()` anywhere in the `ActivateMove()` method will cause the bonds to be set automatically such that the movements from the original Amoebot model are simulated
		- Contracting particles will release all bonds of their moving part
		- Expanding particles will not mark any bonds
		- All other bonds stay active
- The anchor particle
	- One particle in the system is the *anchor*
	- This movements of this particle are simulated as if it was the only particle in the system
	- I.e., the non-moving part of it will *never* move globally
	- The anchor allows the deterministic simulation of joint movements
