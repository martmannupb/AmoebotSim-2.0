# Model Reference: Neighbor Discovery

One of the basic abilities of Amoebot particles is detecting neighbor particles and reading their states.
This page explains how this is done in algorithm code.

The simplest way to check whether there is a neighbor particle in a specific position is the [`HasNeighborAt(Direction d, bool head)`][1] method.
Given the direction and an optional head or tail flag specifying an adjacent position, this method will return `true` if there is a particle at that position.
In addition to just finding neighbors, a particle can also read its neighbors' state attributes.
For this, you first have to get a reference to the neighbor using the [`GetNeighborAt(Direction d, bool head)`][2] method, and then you need to typecast the reference to the type of your algorithm class to access its attributes.
For example:
```csharp
public class MyAlgorithm : ParticleAlgorithm {
    public ParticleAttribute<int> myInt;  // We want to read this attribute of our neighbor
    ...
    public override void ActivateBeep() {
        ...
        // Check if we have a neighbor in East direction
        if (HasNeighborAt(Direction.E)) {
            // Get the neighbor reference and typecast it
            MyAlgorithm nbr = (MyAlgorithm)GetNeighborAt(Direction.E);
            // Returns myInt.GetValue()
            int nbrAttribute = nbr.myInt;
        }
        ...
    }
}
```
When reading a neighbor's attributes, only their values from the beginning of the current round are available, even if that particle changes the values in this round.
In other words, you cannot call [`GetCurrentValue()`][10] on attributes of neighboring particles (see the [attributes reference page](attrs.md) for details).

If the neighboring particle is expanded, it might be of interest whether the neighboring part is the particle's head or tail.
To find out, you can use the [`IsHeadAt(Direction dir, bool head)`][3] and [`IsTailAt(Direction dir, bool head)`][4] methods.
These are especially helpful for planning handover movements.


### Advanced Neighbor Discovery

In some cases, it is necessary to find neighbors with a specific property, find the first neighbor in a specific region or even all neighbors at once.
The algorithm API provides several advanced methods that can be useful in such cases.
They all make use of the [`Neighbor<T>`][5] struct, which contains not just a neighbor reference, but also the direction and head/tail information identifying the neighbor's location.
Since it is generic, the neighbor reference already has the correct type, meaning that the typecast is not necessary when using this struct.
All of the advanced neighbor discovery methods use this struct as return types.

Here is a short overview of the advanced discovery methods (you can read more about them in the API documentation):
- [`FindFirstNeighbor<T>(out Neighbor<T> nbr, Direction startDir, bool startAtHead, bool withChirality, int maxNumber)`][6]:  
	This method searches neighbor positions starting at the location specified by `startDir` and `startAtHead`, and moving around the particle with or against its chirality.
	If it encounters a neighbor while checking these positions, it returns this neighbor as the `nbr` output parameter and returns `true`.
	The method only checks at most `maxNumber` positions.
- [`FindFirstNeighborWithProperty<T>(System.Func<T, bool> prop, out Neighbor<T> nbr, Direction startDir, bool startAtHead, bool withChirality, int maxNumber)`][7]:  
	If you want to find a neighbor with a specific property, you can use this method and specify the property using the `prop` parameter.
	The property can be defined as a lambda expression, e.g., `(MyAlgorithm p) => { return p.myInt == 42; }`.
	Calling the method with this expression will find the first particle whose `myInt` attribute has a value of `42`, using the same search method as the previous method.
- [`FindNeighbors<T>(Direction startDir, bool startAtHead, bool withChirality, int maxSearch, int maxReturn)`][8]:  
	This method returns a list of all neighbors found while traversing the neighboring directions as before.
	It will abort the search after checking `maxSearch` positions or finding `maxReturn` neighbors.
	The order of the neighbors in the list is the order in which they were found.
- [`FindNeighborsWithProperty<T>(System.Func<T, bool> prop, Direction startDir, bool startAtHead, bool withChirality, int maxSearch, int maxReturn)`][9]:  
	The same as the above method, but it only returns neighbors satisfying the property `prop`.




[1]: xref:AS2.Sim.ParticleAlgorithm.HasNeighborAt(AS2.Direction,System.Boolean)
[2]: xref:AS2.Sim.ParticleAlgorithm.GetNeighborAt(AS2.Direction,System.Boolean)
[3]: xref:AS2.Sim.ParticleAlgorithm.IsHeadAt(AS2.Direction,System.Boolean)
[4]: xref:AS2.Sim.ParticleAlgorithm.IsTailAt(AS2.Direction,System.Boolean)
[5]: xref:AS2.Sim.Neighbor`1
[6]: xref:AS2.Sim.ParticleAlgorithm.FindFirstNeighbor``1(AS2.Sim.Neighbor{``0}@,AS2.Direction,System.Boolean,System.Boolean,System.Int32)
[7]: xref:AS2.Sim.ParticleAlgorithm.FindFirstNeighborWithProperty``1(System.Func{``0,System.Boolean},AS2.Sim.Neighbor{``0}@,AS2.Direction,System.Boolean,System.Boolean,System.Int32)
[8]: xref:AS2.Sim.ParticleAlgorithm.FindNeighbors``1(AS2.Direction,System.Boolean,System.Boolean,System.Int32,System.Int32)
[9]: xref:AS2.Sim.ParticleAlgorithm.FindNeighborsWithProperty``1(System.Func{``0,System.Boolean},AS2.Direction,System.Boolean,System.Boolean,System.Int32,System.Int32)
[10]: xref:AS2.Sim.ParticleAttribute`1.GetCurrentValue
