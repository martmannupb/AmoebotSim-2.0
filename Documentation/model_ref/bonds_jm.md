# Model Reference: Bonds and Joint Movements

This page explains how movements in general and *joint movements* in particular are implemented in the simulation environment.
Please refer to the [Joint Movement Extension](~/amoebot_model/jm.md) page for details on the mathematical model.



## Bonds

Bonds represent connections between particles that define the "physical" structure of the system and allow particles to perform joint movements.
A bond fixes an edge between two neighboring particles and can be interpreted as a physical connection that prevents the particles from moving independently of each other, similar to the connection between the head and tail of an expanded particle.

At the beginning of a round, all possible bonds are present, i.e., there is a bond on every edge that is incident to two particles.
In the movement phase, particles can *release* bonds to remove them from the system, and perform individual movements.
It is enough for one of the two bonded particles to release the bond, but it is generally recommended to write algorithms in such a way that both particles agree on releasing a bond to prevent unwanted releases.
After all particles have been activated, the released bonds are removed and the scheduled movements are performed with the remaining bonds.
Finally, new bonds are created for the new positions of the particles and the process repeats in the next round (after the beep phase).

![Bonds in the simulator](~/images/bonds_vis.png "Bonds in the simulator")
In the simulator, bonds are visualized as light red lines between the particles.

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
	If there are no other conflicts, a bond of the moving part can merge into a bond of the stationary part that lies on the destination edge of the moving bond (see Figure 2c on the [Joint Movements page](~/amoebot_model/jm.md)).
- If the particle expands, it can *mark* some of its bonds to move with its head.
	Unmarked bonds will simply stay at the particle's tail.
	The bond pointing directly in the expansion direction will always move with the head and the bond exactly opposite of the expansion direction will always stay at the tail.
- Handover movements only allow the bond between the two movement partners to move:
	The pushing and the pulling particle must agree on the handover movement and the bond of the pushing particle that points into the push direction must be active.
	This bond will move with both moving parts of the two particles so that they stay connected, and it is allowed to rotate to match their new positions.
	Any other bonds on the moving part of the pulling particle are *transferred* to the head of the pushing particle without moving relative to the two stationary parts.
	The bonds at the stationary parts of the particles cannot move either, even if they are marked.
> [!NOTE]
> When a bond "moves" or "stays" relative to the stationary part of a particle, this means that the
> part of the neighboring particle at the other end of the bond behaves the same way. It is possible
> that the movement of this bond is interpreted differently from the neighbor particle's perspective.


### Illustration

The animations below illustrate the movement types.

<img src="~/images/jm_expansion.gif" alt="Expansion animation" title="Expansion animation" width="350" align="right"/>
This is the basic expansion movement, it shows the head of the expanding particle moving in the East (E) direction.
The bond in the NNE direction is marked, meaning that it moves together with the expanding particle's head.
The bond in the expansion direction (E) is always marked automatically because it cannot stay at the particle's tail without a conflict.
The bond in the West (W) direction, which is the opposite of the expansion direction, can never be marked for the same reason.

<br style="clear:both" />

-----

<img src="~/images/jm_contraction.gif" alt="Contraction animation" title="Contraction animation" width="350" align="right"/>
This animation shows the basic contraction movement.
The green particle contracts and pulls all bonded neighbors of the moving part with it.
It could be contracting into its head or into its tail - note that the resulting system is the same in both cases, up to a translation of one unit on the global grid.

<br style="clear:both" />

-----

<img src="~/images/jm_handover.gif" alt="Handover animation" title="Handover animation" width="350" align="right"/>
Finally, this animation illustrates the handover movement.
The blue particle performs a push handover while the green particle performs a pull handover.
All bonds other than the one connecting the two moving particles maintain their relative position to the moving particles' stationary parts.
This means that the bond in the NNW direction is transferred from the green to the blue particle.

<br style="clear:both" />


### Joint Movements

By performing a movement and setting up its bonds appropriately, a particle is able to move its bonded neighbors relative to its own position, regardless of their individual movements.
If these neighbors, in turn, do the same to their neighbors, some particles can be moved more than one position away from their original grid position, relative to other particles as well as the global coordinates.
Without the joint movement extension, this kind of movement would not be possible.
It allows the particles to move across greater distances through coordinated movements, but it may require additional coordination effort.

#### The Anchor Particle

In theory, the final position of a particle system after performing joint movements is only defined in terms of the particle positions relative to each other.
The global positions of the particles are not uniquely defined: Imagine two particles, say one on the left and one on the right, and each of them tries to push the other one away by expanding into the direction of the neighbor.
The final system will have the shape of four occupied nodes in a horizontal line, but how far should each particle move in the global coordinate system?
What if there are more particles trying to push each other away?

Because questions like this need a definitive answer in the simulator, we define an *anchor particle*.
At any time, the particle system has exactly one anchor particle which "anchors" the whole system to the global grid.
All movements are performed relative to this particle's position.
If the anchor performs a movement, its stationary part will always keep its global position.
Even if another particle tries to push the anchor away, the resulting movement will only push the particle away from the anchor.
However, no particle in the system is able to know the difference because the particles do not have access to their global coordinates.
This system only has the purpose of making the outcome of a joint movement uniquely defined.
In some cases, it may also be used to create a specific visualization by changing the anchor during the simulation.

<img src="~/images/expansion_1.gif" alt="Expansion with anchor left" title="Expansion with anchor left" height="175"/> <img src="~/images/expansion_2.gif" alt="Expansion with anchor right" title="Expansion with anchor right" height="175"/>

In the two animations above, all three particles perform an expansion in the East (E) direction.
The only difference between the two cases is that in the left case, the leftmost particle is the anchor (marked in green) and in the right case, the rightmost particle is the anchor.
On the right side, it might look like two of the particles are expanding in the West (W) direction, but this is only due to the joint movement.
From their local views, they are pushing their neighbors away while expanding their head in the East (E) direction.


### Conflicts

If movements are not coordinated properly, *movement conflicts* can occur.
The simplest kind of conflict is caused by multiple particles moving onto the same grid node.
This can happen if two particles expand onto the same node or if a particle is pushed into another one, even without moving on its own.

Other conflicts are caused by the bonds of neighboring particles not agreeing with their individual movements.
For example, if two expanded particles are connected by three bonds and one of them tries to contract, its own bonds would try crossing each other, which is not allowed (see Figure 2e on the [Joint Movements page](~/amoebot_model/jm.md)).
In a similar situation where there are only two bonds between the two expanded particles, such that each of them has one bond at its head and one at its tail, it is impossible for one of them to contract while the other one stays expanded.

Any conflict of these types will cause the round simulation to be aborted.
In any algorithm that uses particle movements, it is essential that movement conflicts are prevented since the particles cannot react to them when they occur.

> [!NOTE]
> "Collisions" that arise from particles moving through each other (but not necessarily ending up in an invalid position) are not handled by the system yet.
> However, this kind of conflict should also be avoided because such movements would not be possible in a physical environment.



## Implementation

There are various methods for scheduling movements and releasing bonds in the algorithm API.
Simple movements can be scheduled by calling [`Expand(Direction d)`][1], [`ContractTail`][2] and [`ContractHead`][3], handovers are scheduled with [`PushHandover(Direction d)`][4], [`PullHandoverTail(Direction d)`][5] and [`PullHandoverHead(Direction d)`][6], where `d` is the expansion direction for expansion and push movements or the direction of the pushing neighbor for pull handovers.

To release or mark bonds, the [`ReleaseBond(Direction d, bool head)`][7] and [`MarkBond(Direction d, bool head)`][8] methods are used.
The parameters `d` and `head` indicate the local direction of the bond and whether the bond is at the particle's head, respectively.
All of these methods can only be called in the [`ActivateMove`][9] method.

For convenience, the [`UseAutomaticBonds`][10] method can be called to avoid joint movement behavior.
Calling this method will automatically set the bonds such that neighbors are not pulled by a contraction or pushed by an expansion (if possible), and it will block warning messages caused by disagreeing bond releases.
This can be used to perform movements like in the original Amoebot model without the joint movement extension.
However, the system must still remain connected by bonds at all times.

Bonds can also be hidden from the visualization without releasing them by calling the [`HideBond(Direction d, bool head)`][11] method.
This will hide the selected bond for the next round, even if the bonded neighbor particle does not hide the bond as well.



[1]: xref:AS2.Sim.ParticleAlgorithm.Expand(AS2.Direction)
[2]: xref:AS2.Sim.ParticleAlgorithm.ContractTail
[3]: xref:AS2.Sim.ParticleAlgorithm.ContractHead
[4]: xref:AS2.Sim.ParticleAlgorithm.PushHandover(AS2.Direction)
[5]: xref:AS2.Sim.ParticleAlgorithm.PullHandoverTail(AS2.Direction)
[6]: xref:AS2.Sim.ParticleAlgorithm.PullHandoverHead(AS2.Direction)
[7]: xref:AS2.Sim.ParticleAlgorithm.ReleaseBond(AS2.Direction,System.Boolean)
[8]: xref:AS2.Sim.ParticleAlgorithm.MarkBond(AS2.Direction,System.Boolean)
[9]: xref:AS2.Sim.ParticleAlgorithm.ActivateMove
[10]: xref:AS2.Sim.ParticleAlgorithm.UseAutomaticBonds
[11]: xref:AS2.Sim.ParticleAlgorithm.HideBond(AS2.Direction,System.Boolean)
