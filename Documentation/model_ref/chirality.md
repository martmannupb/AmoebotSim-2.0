# Model Reference: Chirality

The term *chirality* describes the local sense of rotation direction of an amoebot.
In the two-dimensional triangular grid, we define the positive rotation direction to be counter-clockwise and the negative direction to be clockwise.
This is the global standard relative to which the local chirality is defined.

If an amoebot's sense of positive rotation matches the global positive rotation direction, we say the amoebot has *counter-clockwise chirality*.
In the code, this is often represented by the Boolean value `true`.
Otherwise, we say the amoebot has *clockwise chirality*, or *inverted chirality*, represented by `false`.
An amoebot with clockwise chirality perceives rotations in the global positive direction as negative and vice-versa.
Its [compass](direction.md) is flipped, meaning that its North direction, which it perceives as being 90 degrees counter-clockwise from its East direction, actually lies 90 degrees in *clockwise* direction when viewed from the global perspective.
You can think of a clock lying flat on a table with the dial pointing up and another clock on the same table lying with the dial facing down.
Even though both clocks run normally, their hands will be rotating in opposite ways and almost all of their hour labels will point in different directions (even if you make the "12" labels point in the same direction, "11" and "1" etc. will disagree).

An amoebot *does not know* its own chirality and has no way to find it.



## Pins and Chirality Agreement

As explained in the [Pin Configuration reference](pin_cfgs.md), the pins on each edge of an amoebot are numbered in *local* counter-clockwise direction.
Therefore, the local pin labeling of an amoebot with inverted chirality differs from the local labeling of an amoebot with counter-clockwise chirality.
If an algorithm uses more than one pin for the circuit communication, this can cause problems when amoebots with different chirality try to establish a circuit.

![Pin labels depending on chirality](~/images/pin_labels_chirality.png "Pin labels depending on chirality")

However, it is possible for the amoebots to agree on a *common chirality* using a *chirality agreement algorithm*.
Such algorithms are usually based on the ability of two neighboring amoebots to determine whether or not they have the same chirality.
This can be achieved using the two outermost pins (for example): Both amoebots send a beep to their pin with the smaller label.
If they receive a beep on the *other* pin, they have the *same chirality*.
Otherwise, their chirality is different and the pins on which they have sent the beeps are connected, meaning that they will only receive a beep on that pin.
Then, coin tosses can be used to agree on one of the amoebots to change its chirality to match its neighbor's.
An amoebot can flip its own chirality by actively treating rotations in the opposite way and using its pin labels differently.
This process can be repeated until all amoebots in the structure have the same chirality.
The "Chirality & Compass Alignment" algorithm that is part of the simulator project demonstrates this technique and then also establishes a common compass orientation.
Note that the algorithm relies on randomness due to the coin tosses, which means that it is not guaranteed to terminate.
However, in practice, this generally does not cause any problems.



## Setting Chirality

The chirality of an amoebot is set during the structure initialization in the Init Mode and it cannot be changed during the simulation.
The default generation algorithm, which places the amoebots randomly but connected, has a parameter that defines the chirality.
If "Clockwise" or "CounterClockwise" is selected, all amoebots will have that same chirality setting.
If "Random" is selected, the chirality of each amoebot is chosen randomly.
For custom initialization algorithms, all API methods that place amoebots have a similar parameter.
The chirality can also be set manually in the UI while in Init Mode.
