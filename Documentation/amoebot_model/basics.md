# Geometric Amoebot Model

![A set of Amoebots which are executing a line formation algorithm](~/images/amoebotscreen.jpg "Line Formation Algorithm")

AmoebotSim 2.0 is a simulation environment for the *geometric Amoebot model* with its *reconfigurable circuits* and *joint movement* extensions.
The purpose of the simulator is to provide a platform for algorithm developers to implement and run their Amoebot algorithms, allowing them to see the algorithms in action and test them in arbitrary scenarios.
In order to develop such algorithms, a good understanding of the underlying Amoebot model is required.
This and the following pages provide formal descriptions of the basic model and its extensions implemented by the simulator.



## Model Description

In the geometric Amoebot model, the two-dimensional world is represented by the infinite regular triangular grid graph $G_\Delta = (V,E)$ (see image below).
In this graph, each node has exactly six neighbors, spread evenly around the node at unit distance.
Its structure induces six *cardinal directions* on three axes, two of which we use to define a coordinate system on the grid.
In the image below, the E-W axis and the NNE-SSW axis define the coordinates.

![The regular triangular grid graph](~/images/graph_eqt_arrows.png "The regular triangular grid graph")

An *Amoebot particle* (short "Amoebot" or "particle") is an anonymous, randomized finite state machine that occupies either one or two adjacent nodes of $G_\Delta$ at all times.
Each node of the graph can only be occupied by at most one Amoebot at a time.
An Amoebot that occupies one node is called *contracted* and an Amoebot occupying two nodes is called *expanded*.
If an Amoebot is expanded, exactly one of its two occupied nodes is called its *head* and the other occupied node is called its *tail*.
For a contracted Amoebot, its head and tail are the same node.
Amoebots occupying adjacent nodes of $G_\Delta$ are called *neighbors*.

![Amoebot particles](~/images/amoebots_labels.png "Amoebot particles")

Let $S \subset V$ be the set of grid nodes occupied by Amoebots.
We say that the particle system is *connected* if the subgraph $G(S)$ induced by $S$ is connected.
In the model version implemented in the simulator, we demand that the initial particle configuration is connected and remains connected throughout the whole simulation.


### Movements

Amoebots can move through *contractions* and *expansions*.
A contracted Amoebot occupying a node $v \in V$ can expand to any of the up to six unoccupied nodes adjacent to $v$.
After the expansion, the Amoebot is expanded and occupies both $v$ and the adjacent unoccupied node $u$ in the expansion direction, with $u$ being the Amoebot's head and $v$ being its tail.

Conversely, an expanded Amoebot occupying nodes $u, v \in V$ can contract into $u$ or into $v$.
After the contraction, the Amoebot is contracted and occupies only one of the two nodes.
We distinguish between the two possible movements by saying that the Amoebot contracts *into its tail* or *into its head*.

There is also the special *handover* movement which allows a contracted and an expanded Amoebot to expand to and contract from the same node in a single movement.
More precisely, let the contracted Amoebot $p$ occupy node $u \in V$ and let the expanded Amoebot $q$ occupy $v, w \in V$ such that $u$ and $v$ are adjacent.
Then, $p$ can perform a *push handover* to expand onto $v$ while $q$ performs a *pull handover*, contracting into $w$.
The two Amoebots must agree to perform this movement.

![Amoebot movements](~/images/movements.gif "Amoebot movements")

The animation above shows the three types of movements:
The Amoebot on the left expands in the NNE direction, while the Amoebot in the middle contracts (it could be either its head or its tail).
The two Amoebots on the right perform a handover movement.



- Amoebots have compass orientation and chirality
- Activation model
	- Fully synchronous model
	- Computation proceeds in discrete rounds
	- All Amoebots are active at the same time in each round
	- On activation, Amoebot can alter its internal state and perform actions (like movement) as a function of its current state
		- Inputs to the function are the internal state and its neighborhood
		- States of neighboring particles can also be inspected
	- State updates are applied at the end of the round
		- All particles operate on the same snapshot of the system
- Conflicts can occur
	- E.g. two particles try expanding onto the same node
	- In our model: Conflicts lead to aborting the round
	- Other models may handle them differently





The following introduction to the Amoebot Model is taken from the paper [*Coordinating Amoebots via Reconfigurable Circuits*][1] by Michael Feldmann, Andreas Padalkin, Christian Scheideler and Shlomi Dolev (2022) and was slightly edited.

**TODO: Not correct**




In the geometric amoebot model, a set of $n$ amoebots is placed on the infinite regular triangular grid graph $G_\Delta = (V,E)$.
An amoebot is an anonymous, randomized finite state machine that either occupies one or two adjacent nodes of $G_\Delta$, and every node of $G$ is occupied by at most one amoebot.
If an amoebot occupies just one node, it is called *contracted* and otherwise *expanded*, and exactly one of its occupied nodes is called its *head*.
If it is expanded, the other occupied node is called its *tail*.

An amoebot can move through *contractions* and *expansions*.
A contracted amoebot occupying node $u$ can expand into an unoccupied adjacent node $v$.
Thereafter, it occupies both $u$ and $v$.
An expanded amoebot occupying nodes $v$ and $w$ can contract into one of its occupied nodes, we say it contracts *into its head* or *into its tail*.
Thereafter, it occupies only that one node.
Additionally, two adjacent amoebots can perform a *handover* if one is contracted and the other is expanded:
Let node $u$ be occupied by the contracted amoebot, and let nodes $v$ and $w$ be occupied by the expanded amoebot such that nodes $u$ and $v$ are adjacent.
When the amoebots perform a handover, the contracted amoebot expands into node $v$ while the expanded amoebot contracts into node $w$.

Each amoebot has a *compass orientation* (it defines one of its incident edges as the Eastern direction) and a *chirality* (a sense of clockwise or counterclockwise rotation) that it can maintain as it moves, but initially the amoebots might not agree on their compass orientation and chirality.

((An amoebot can label its incident edges in the order of its sense of chirality in a consecutive fashion starting with $0$, where the edge labeled with $0$ is the edge to the East whenever it is unique and otherwise the edge of its head pointing to the East.))

We assume the fully synchronous activation model, i.e., the time is divided into synchronous rounds, and every amoebot is active in each round.
On activation, each amoebot may perform a movement and update its state as a function of its previous state.







For simplicity, we identify an amoebot by its occupied node(s). Two amoebots that occupy adjacent nodes in G are called neighbors. The set N(u) denotes the neighborhood of amoebot u. Whenever amoebot u is a neighbor of amoebot v, the canonical amoebot model allows u to execute read and write operations on the (public) memory of v. In our reconfigurable circuit extension, we will use a different way of exchanging information, but as we will see, read and write operations can be simulated with this so that our circuit model is indeed an extension of the canonical amoebot model.

An amoebot can move through contractions and expansions. A contracted amoebot occupying node u can expand into an unoccupied adjacent node v. Thereafter, it occupies both, u and v. However, if several amoebots try to expand into the same node at the same time, only one will succeed and the others will remain contracted. We assume these conflicts are arbitrarily resolved, e.g., by a adversary. An expanded amoebot occupying nodes v and w can contract into one of its occupied nodes. Thereafter, it only occupies that node. Additionally, two adjacent amoebots can perform a handover if one is contracted and the other is expanded: Let node u be occupied by the contracted amoebot, and let nodes v and w be occupied by the expanded amoebot such that nodes u and v are adjacent. The contracted amoebot expands into node v while the expanded node contracts into node w.



Each amoebot has a compass orientation (it defines one of its incident edges as the northern direction) and a chirality (a sense of clockwise or counterclockwise rotation) that it can maintain as it moves, but initially the amoebots might not agree on their compass orientation and chirality.
An amoebot can label its incident edges in the order of its sense of chirality in a consecutive fashion starting with 1, where the edge labeled with 1 is the edge to the north whenever it is unique and otherwise the edge of its head pointing to the north.

Let the amoebot structure S (subset of V) be the set of nodes occupied by the amoebots. We say that S is connected if G_S is connected, where G_S = G is the graph induced by S. In this paper, we assume that initially, S is connected and all amoebots are contracted. Also, we assume the fully synchronous activation model, i.e., the time is divided into synchronous rounds, and every amoebot is active in each round. On activation, each amoebot may perform a movement and update its state as a function of its previous state. However, if an amoebot fails to perform its movement, it remains in its previous state. In order to make each state transmission self-initiated, we have to decouple the coordination and execution of a handover from each other as follows. So far, a handover was initiated by one of the two participating amoebots. Now, we require that both amoebots perform the handover separately from each other at the same time, i.e., they have to coordinate the handover beforehand (e.g., by message transmission). Note that the synchronous activation model justifies this adjustment since it allows the amoebots to determine an exact point of time to perform a handover. The time complexity of an algorithm is measured by the number of synchronized rounds required by it.


1. [Circuits](circuits.md)
2. [Joint Movements](jm.md)



[1]: https://doi.org/10.1007/978-3-030-91081-5_34
