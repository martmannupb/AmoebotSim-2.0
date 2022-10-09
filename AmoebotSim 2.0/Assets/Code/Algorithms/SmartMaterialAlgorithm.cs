using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartMaterialParticle : ParticleAlgorithm
{
    private ParticleAttribute<bool> firstRound;
    private ParticleAttribute<int> hexType;
    private ParticleAttribute<bool> onLeftEdge;
    private ParticleAttribute<bool> onRightEdge;
    private ParticleAttribute<bool> onTopEdge;
    private ParticleAttribute<bool> onBotEdge;
    private ParticleAttribute<Direction> headDirection;

    public SmartMaterialParticle(Particle p, int[] genericParams) : base(p)
    {
        if (IsContracted())
        {
            Log.Error("Cannot determine role in smart material; must be expanded at initialization");
            return;
        }

        if (genericParams.Length < 1)
        {
            Log.Error("Not enough generic parameters: Require at least 1");
            return;
        }

        int d = HeadDirection().ToInt();
        SetMainColor(ColorData.Circuit_Colors[d]);

        firstRound = CreateAttributeBool("First Round", true);
        hexType = CreateAttributeInt("Hexagon Type", genericParams[0]);
        onLeftEdge = CreateAttributeBool("On Left Edge", false);
        onRightEdge = CreateAttributeBool("On Right Edge", false);
        onTopEdge = CreateAttributeBool("On Top Edge", false);
        onBotEdge = CreateAttributeBool("On Bottom Edge", false);
        headDirection = CreateAttributeDirection("Head Direction", HeadDirection());
    }

    public override int PinsPerEdge => 0;

    public static new string Name => "Smart Material";

    public override void ActivateBeep()
    {
        // Determine our role/position in the parallelogram in the first round
        if (firstRound)
        {
            Direction headDir = HeadDirection();

            // For each edge of the parallelogram, check if we are there
            Direction leftDir = headDir.Rotate60(1);
            Direction rightDir = headDir.Rotate60(-2);
            Direction topDir = headDir;
            Direction botDir = headDir.Opposite();
            // Left edge (should always have a neighbor here, actually)
            if (!HasNeighborAt(leftDir, true) || GetNeighborAt(leftDir, true).HeadDirection() != headDir)
            {
                onLeftEdge.SetValue(true);
            }
            // Right edge (should never have a neighbor here)
            if (!HasNeighborAt(rightDir, true) || GetNeighborAt(rightDir, true).HeadDirection() != headDir)
            {
                onRightEdge.SetValue(true);
            }
            // Top edge
            if (!HasNeighborAt(topDir, true) || GetNeighborAt(topDir, true).HeadDirection() != headDir)
            {
                onTopEdge.SetValue(true);
            }
            // Bottom edge
            if (!HasNeighborAt(botDir, false) || GetNeighborAt(botDir, false).HeadDirection() != headDir)
            {
                onBotEdge.SetValue(true);
            }

            firstRound.SetValue(false);
        }
    }

    public override void ActivateMove()
    {
        // Do nothing in the first round, we first have to find out about our position in the parallelogram
        if (firstRound)
            return;

        // Set bonds according to expansion state, position in the parallelogram, and hexagon type
        if (IsExpanded())
        {
            Direction headDir = headDirection;
            // Some bonds must always be removed
            ReleaseBond(headDir.Rotate60(2), true);
            ReleaseBond(headDir.Rotate60(-1), false);
            // Bottom particles release two bonds depending on hexagon type and position on left edge
            if (onBotEdge)
            {
                if (hexType == 1 || !onLeftEdge)
                    ReleaseBond(headDir.Opposite(), false);
                if (!onLeftEdge)
                    ReleaseBond(headDir.Rotate60(2), false);
            }
            // Left edge particles release all head bonds and maybe some tail bonds
            if (onLeftEdge)
            {
                ReleaseBond(headDir.Rotate60(1), true);
                ReleaseBond(headDir.Rotate60(2), true);
                if (!onBotEdge)
                    ReleaseBond(headDir.Rotate60(2), false);
                if (!onBotEdge || hexType == 2)
                    ReleaseBond(headDir.Rotate60(1), false);
            }
            // Top-right corner has to release one bond
            if (onTopEdge && onRightEdge)
                ReleaseBond(headDir.Rotate60(-1), true);

            ContractTail();
        }
        else
        {
            Direction headDir = headDirection;
            // Bottom particles release two bonds depending on hexagon type and position on left edge
            if (onBotEdge)
            {
                if (hexType == 1 || !onLeftEdge)
                    ReleaseBond(headDir.Opposite());
                if (!onLeftEdge)
                    ReleaseBond(headDir.Rotate60(2));
            }
            // Left edge particles release bonds depending on type
            if (onLeftEdge)
            {
                if (!onBotEdge)
                    ReleaseBond(headDir.Rotate60(2));
                if (!onBotEdge || hexType == 2)
                    ReleaseBond(headDir.Rotate60(1));
            }
            // Right edge must release both bonds
            if (onRightEdge)
            {
                ReleaseBond(headDir.Rotate60(-1));
                ReleaseBond(headDir.Rotate60(-2));
            }

            // Mark a bond to allow the expansion
            MarkBond(headDir.Rotate60(-1));

            Expand(headDir);
        }
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
        if (NumGenericParameters() < 1)
            AddGenericParameter();

        PlaceHexagon(Vector2Int.zero, scale);
        PlaceHexagon(new Vector2Int(-2 * scale, -2 * scale), scale, true);
        PlaceHexagon(new Vector2Int(0, -6 * scale), scale);
        PlaceHexagon(new Vector2Int(4 * scale, -8 * scale), scale, true);
        PlaceHexagon(new Vector2Int(6 * scale, -6 * scale), scale);
        PlaceHexagon(new Vector2Int(4 * scale, -2 * scale), scale, true);
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
                    InitializationParticle ip = AddParticle(pos, expansionDir);
                    ip.genericParams[0] = type2 ? 2 : 1;
                    pos = ParticleSystem_Utils.GetNbrInDir(pos, rowDir);
                }
                startPos = ParticleSystem_Utils.GetNbrInDir(startPos, expansionDir, 2);
            }
        }
    }
}
