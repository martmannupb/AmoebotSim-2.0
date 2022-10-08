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

    public void Generate(int scale)
    {
        Log.Debug("I generate a system!");
        PlaceHexagon(Vector2Int.zero, scale);
    }

    private void PlaceHexagon(Vector2Int position, int scale, bool type2 = false)
    {
        // Position is always the innermost particle of the top right parallelogram

        // We build 3 parallelograms starting at 3 different locations and going in 3 different directions
        Vector2Int[] startPositions = new Vector2Int[] { position,
            position + ParticleSystem_Utils.DirectionToVector(Direction.W),
            position + ParticleSystem_Utils.DirectionToVector(Direction.SSW) };
        Direction[] expansionDirs;
        Direction[] rowDirs;

        if (type2)
        {
            // First parallelogram goes to the right and up
            expansionDirs = new Direction[] { Direction.NNE, Direction.W, Direction.SSE };
            rowDirs = new Direction[] { Direction.SSE, Direction.NNE, Direction.W };
        }
        else
        {
            // First parallelogram goes up and left
            expansionDirs = new Direction[] { Direction.NNW, Direction.SSW, Direction.E };
            rowDirs = new Direction[] { Direction.E, Direction.NNW, Direction.SSW };
        }

        for (int i = 0; i < 3; i++)
        {
            Vector2Int startPos = startPositions[i];
            Direction expansionDir = expansionDirs[i];
            Direction rowDir = rowDirs[i];
            for (int row = 0; row < scale; row++)
            {
                Vector2Int pos = startPos;
                for (int col = 0; col < scale * 2; col++)
                {
                    AddParticle(pos, expansionDir);
                    pos = ParticleSystem_Utils.GetNbrInDir(pos, rowDir);
                }
                startPos = ParticleSystem_Utils.GetNbrInDir(startPos, expansionDir, 2);
            }
        }
    }
}
