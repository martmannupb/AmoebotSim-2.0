# Joint Movements

![Here we see the configuration of some connected Amoebots performing a joint expansion.](~/images/joint_movement.gif "Joint Movements")

In the original amoebot model, expansions and contractions could only happen into empty space resp. left an empty node. One exception was the common 'handover', where one contracted particle expanded into one expanded particle, forcing it to contract. Still, these handovers were not different from contraction and expansion in two consecutive rounds. With the joint movement extension, we can define a connection between two amoebots as a 'bond', marking it as more or less static. This way, the amoebot can be able to expand or contract into some direction and push or pull the connected structure into the direction of its own movement.


In the basic Amoebot model, the maximum distance an Amoebot can move per round is a small constant:
An Amoebot can expand in one round and contract in the next, after which it has moved at most one grid node in any direction.
This restriction might limit the speed at which movement or shape formation problems can be solved, especially for larger systems.
The *joint movement* extension adds a mechanism for the Amoebots to push and pull each other through so-called *bonds*, allowing them to cover much greater distances in a single round.



## Model Description

The following text and figures are taken from [*Shape Formation and Locomotion with Joint Movements in the Amoebot Model*][1] by Padalkin, Kumar and Scheideler (arXiv, 2023), and were edited to match the model implemented by the simulator.

In the *joint movement extension*, the way the Amoebots move is altered.
The idea behind the extension is to allow Amoebots to push and pull other Amoebots.
The necessary coordination of such movements can be provided by the reconfigurable circuit extension (see the [previous page](circuits.md) for details).

In the following, we formalize and extend the joint movement extension.
Let $S$ denote the set of all Amoebots and $G_S = (S, E_S)$ denote the subgraph of $G_\Delta$ induced by $S$).
Joint movements are performed in two steps.

In the first step, the Amoebots remove *bonds* from $G_S$ as follows.
Each Amoebot can decide to release an arbitrary subset of its currently incident bonds in $G_S$.
A bond is removed iff either of the Amoebots at the endpoints releases the bond.
However, an expanded Amoebot cannot release the bond connecting its occupied nodes.
Let $E_R \subseteq E_S$ denote the set of the remaining bonds, and $G_R = (S, E_R)$ the resulting graph.

We require that $G_R$ is connected since otherwise, disconnected parts might float apart.
We say that a *connectivity conflict* occurs iff $G_R$ is not connected.
Whenever a connectivity conflict occurs, the Amoebot structure transitions into an undefined state such that we become unable to make any statements about the structure.
> [!NOTE]
> In the simulator, transitioning into such an undefined state usually resets the round simulation.

In the second step, each Amoebot may perform one of the following movements within the time period $[0,1]$.

**TODO**



[1]: https://doi.org/10.48550/arXiv.2305.06146
