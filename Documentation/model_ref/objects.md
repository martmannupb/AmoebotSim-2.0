# Model Reference: Objects

![Objects](~/images/objects.png "Objects highlighted in gray. Note that the blue object is not directly connected to any particles")

A simple extension of our Amoebot model is the addition of passive *objects*.
An object is a structure that occupies a connected set of grid nodes.
Bonds allow particles to connect to objects and move them around using the joint movement mechanism.
Objects also form bonds to each other.
A particle can trigger a neighboring object to release all its bonds to other objects.
This is the only way to remove bonds between objects.

Every object has an integer ID that can be read by the particles, but cannot be changed.
This value does not have to be unique among the objects in the system.
It is meant to be a way of classifying different types of objects.
Each object also has a color that can be changed but not read by the particles.
The color is supposed to provide some visual information on the state of an algorithm, e.g., particles can change the color of an object to indicate that it has been moved to the desired location.

We still require that the particle system remains connected at all times, regardless of whether or not there are objects present in the system.
Additionally, every object must stay connected to the particle system through bonds, either directly or indirectly, at all times.



## Implementation

To use objects in your algorithms, you need to know how to place objects during initialization and how to make particles interact with the objects.


### Initialization

To create a new object in the `Generate(...)` method of your [`InitializationMethod`][1] subclass, simply call the [`CreateObject(Vector2Int pos, int identifier)`][2] method.
The method returns an instance of the [`ParticleObject`][3] class representing a new object that only occupies the grid position `pos`.
You can add positions to the object by calling the [`AddPosition(Vector2Int pos)`][4] method on it.
Once you are finished adding positions, call the [`AddObjectToSystem(ParticleObject obj)`][5] method to register the object in the system.
This will create a *copy* of the given object `obj`, meaning that modifying `obj` after calling this method will *not* modify the registered object.
However, you can move your object to another location using the [`MovePosition(Vector2Int offset)`][6] or [`MoveTo(Vector2Int newPos)`][7] method, and then add another copy of the object to the system.
This makes it easy to add many objects of the same shape in different locations.

Example:
```csharp
public void Generate()
{
    // Create a new object at the origin
    ParticleObject o = CreateObject(Vector2Int.zero);
    // Add six occupied positions around the center
    o.AddPosition(new Vector2Int(1, 0));
    o.AddPosition(new Vector2Int(0, 1));
    o.AddPosition(new Vector2Int(-1, 1));
    o.AddPosition(new Vector2Int(-1, 0));
    o.AddPosition(new Vector2Int(0, -1));
    o.AddPosition(new Vector2Int(1, -1));
    // Add the object to the system
    AddObjectToSystem(o);

    // Create a copy with an offset and a different color
    o.MovePosition(new Vector2Int(4, 1));
    o.Color = Color.blue;
    AddObjectToSystem(obj);

    // Create a second copy at a specific location and with
    // different shape, color and identifier
    o.MoveTo(new Vector2Int(-4, 0));
    o.AddPositionRel(new Vector2Int(1, 1));
    o.AddPositionRel(new Vector2Int(-1, -1));
    o.Color = Color.red;
    o.Identifier = 42;
    AddObjectToSystem(obj);
}
```
The above code generates the following objects:
![Object initialization example](~/images/objects_init.png "Three objects placed at different locations in the system by the code above")


### Particle Interaction

Once there are objects in the system, particles can interact with them.
The simplest interaction is checking whether there is an object occupying some neighboring node.
Just like the [`HasNeighborAt(Direction d, bool head)`][8] method checks for a neighboring particle, the [`HasObjectAt(Direction d, bool head)`][9] method checks for a neighboring object in direction `d`, relative to the particle's head if `head` is true and the particle is expanded.
Similarly, the [`GetObjectAt(Direction d, bool head)`][10] method returns a reference to the object at the given neighbor position.

Having found an object, a particle can read its identifier and change its color using the corresponding members.
To turn an object into the system's anchor, the particle can either call [`MakeAnchor()`][11] on the object reference itself, or use [`MakeObjectAnchor(Direction d, bool head)`][12] to specify the neighbor object by its location.
Following the same pattern, a particle can make a neighboring object release all bonds to other objects by calling [`ReleaseBonds()`][13] on the reference or calling [`TriggerObjectBondRelease(Direction d, bool head)`][14].
Releasing an object's bonds works just like releasing individual bonds of a particle.
The bonds will be removed for the current movement phase and reestablished at the beginning of the next round.
It is not possible to specify which bonds of an object are released; it will always release all bonds to all connected objects.




[1]: xref:AS2.InitializationMethod
[2]: xref:AS2.InitializationMethod.CreateObject(Vector2Int,System.Int32)
[3]: xref:AS2.Sim.ParticleObject
[4]: xref:AS2.Sim.ParticleObject.AddPosition(Vector2Int)
[5]: xref:AS2.InitializationMethod.AddObjectToSystem(ParticleObject)
[6]: xref:AS2.Sim.ParticleObject.MovePosition(Vector2Int)
[7]: xref:AS2.Sim.ParticleObject.MoveTo(Vector2Int)
[8]: xref:AS2.Sim.ParticleAlgorithm.HasNeighborAt(AS2.Direction,System.Boolean)
[9]: xref:AS2.Sim.ParticleAlgorithm.HasObjectAt(AS2.Direction,System.Boolean)
[10]: xref:AS2.Sim.ParticleAlgorithm.GetObjectAt(AS2.Direction,System.Boolean)
[11]: xref:AS2.Sim.IParticleObject.MakeAnchor
[12]: xref:AS2.Sim.ParticleAlgorithm.MakeObjectAnchor(AS2.Direction,System.Boolean)
[13]: xref:AS2.Sim.IParticleObject.ReleaseBonds
[14]: xref:AS2.Sim.ParticleAlgorithm.TriggerObjectBondRelease(AS2.Direction,System.Boolean)
