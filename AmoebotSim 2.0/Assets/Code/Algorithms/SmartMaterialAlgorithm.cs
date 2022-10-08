using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartMaterialParticle : ParticleAlgorithm
{
    public SmartMaterialParticle(Particle p, int[] genericParams) : base(p)
    {

    }

    public override int PinsPerEdge => 0;

    public static new string Name => "Smart Material";

    public override void ActivateBeep()
    {
        // Don't do anything
    }

    public override void ActivateMove()
    {
        // TODO
    }
}


public class SmartMaterialGenerator : InitializationMethod
{
    public SmartMaterialGenerator(ParticleSystem system) : base(system)
    {

    }

    public static new string Name => "Smart Material";

    public void Generate()
    {
        Log.Debug("I generate a system!");
    }

    private void PlaceHexagon(Vector2Int position, int scale)
    {
        // Position is always the innermost particle of the top right parallelogram
    }
}
