# Model Reference: Status Info Methods

For many algorithms, it may be useful to display more information than what can be visualized using amoebot colors and attribute overlays.
Especially information that goes beyond the local scope of a single amoebot (like a whole spanning tree, for example) can be difficult to show using just the amoebots' own code.
Additionally, it might be helpful to display a custom state summary for a specific amoebot that is selected through the UI.
The *status info method* feature provides a simple solution for all of these use cases, making your algorithms both easier to develop and easier to present and explain.


### Creating Custom Status Info Methods

A status info method is a *static* method that has the following properties:
- The method is defined by a [`ParticleAlgorithm`][1] subclass, i.e., it must belong to an algorithm
- It must have exactly two parameters: A [`ParticleSystem`][2], providing access to the entire structure of amoebots, and a [`Particle`][3] to access the currently selected amoebot (which will be `null` if no amoebot is selected)
- It must be marked with the [`StatusInfoAttribute`][4] attribute so that the simulator recognizes the method and displays a button for it

The following example shows the pattern for creating a status info method:
```csharp
public class MyAlgorithm : ParticleAlgorithm {
    ...
    [StatusInfo("Button Label", "Tooltip text", false)]
    public static void MyStatusInfo(AS2.Sim.ParticleSystem system, Particle selectedParticle) {
        // Implement custom status information here
        // Use the AS2.UI.LineDrawer to draw lines and arrows
        // Print log messages
        // Iterate over all particles using system.particles
        // Find particles at given positions using system.TryGetParticleAt(Vector2Int, out Particle)
        // Access the selected particle's state as follows:
        MyAlgorithm algo = (MyAlgorithm)selectedParticle.algorithm;
    }
    ...
}
```

<img src="~/images/particle_panel_sim.png" alt="Particle Panel with status info buttons in Simulation Mode" title="Particle Panel with status info buttons in Simulation Mode" width="200" align="right"/>

The [`StatusInfoAttribute`][4] attribute (where the `Attribute` can be omitted) has three parameters, two of which are optional.
The first parameter is the label that will be used for the button in the Particle Panel, as can be seen on the right.
The second parameter defines the tooltip message that will be displayed when hovering over the button.
By default, there is no tooltip.
And the third parameter is a `bool` that specifies whether auto-calling is initially enabled for the info method.
A status info method for which auto-calling is enabled will be called automatically every time the simulation round changes.
The attribute parameter only defines the initial behavior, you can change it in the Particle Panel by clicking the toggle next to the button at any time.

Inside the status info method, there are several ways to access and display information.
The second parameter of the method is the currently selected amoebot.
If no amoebot is selected, it will be `null`.
To access the amoebot's attributes, you have to typecast its [`algorithm`][6] member to your algorithm class, as demonstrated in the code example above.
The same method can be used to access any amoebot's attributes.
The amoebots can be accessed using the [`ParticleSystem.particles`][7] list or, for example, the [`ParticleSystem.TryGetParticleAt(Vector2Int pos, out Particle p)`][8] method.

You can use the [line drawer utility](collisions.md) to draw lines and arrows in the amoebot structure.
For more detailed information, you can use the [`Log`][5] system and print text messages with information on the whole structure or only the selected amoebot.



[1]: xref:AS2.Sim.ParticleAlgorithm
[2]: xref:AS2.Sim.ParticleSystem
[3]: xref:AS2.Sim.Particle
[4]: xref:AS2.StatusInfoAttribute
[5]: xref:AS2.Log
[6]: xref:AS2.Sim.Particle.algorithm
[7]: xref:AS2.Sim.ParticleSystem.particles
[8]: xref:AS2.Sim.ParticleSystem.TryGetParticleAt(Vector2Int,AS2.Visuals.IParticleState@)
