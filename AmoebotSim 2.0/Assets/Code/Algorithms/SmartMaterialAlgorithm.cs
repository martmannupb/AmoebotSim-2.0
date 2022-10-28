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

        int d = HeadDirection().ToInt();
        SetMainColor(ColorData.Circuit_Colors[d]);

        firstRound = CreateAttributeBool("First Round", true);
        hexType = CreateAttributeInt("Hexagon Type", 0);
        onLeftEdge = CreateAttributeBool("On Left Edge", false);
        onRightEdge = CreateAttributeBool("On Right Edge", false);
        onTopEdge = CreateAttributeBool("On Top Edge", false);
        onBotEdge = CreateAttributeBool("On Bottom Edge", false);
        headDirection = CreateAttributeDirection("Head Direction", HeadDirection());
    }

    public void Init(int hexagonType = 0)
    {
        if (hexagonType != 1 && hexagonType != 2)
        {
            Log.Error("Hexagon type is " + hexagonType + ", must be 1 or 2");
        }
        hexType.SetValue(hexagonType);
    }

    public override int PinsPerEdge => 0;

    public static new string Name => "Smart Material";

    public static new string GenerationMethod => SmartMaterialInitializer.Name;

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


public class SmartMaterialInitializer : InitializationMethod
{
    public SmartMaterialInitializer(ParticleSystem system) : base(system)
    {

    }

    public static new string Name => "Smart Material";

    public void Generate(int scale = 2, int rows = 2, int cols = 2, bool hexagonShape = false)
    {
        if (NumGenericParameters() < 1)
            AddGenericParameter();

        if (scale < 1)
        {
            Log.Warning("Scale must be at least 1");
            scale = 1;
        }

        if (rows < 1)
        {
            Log.Warning("Must have at least 1 row");
            rows = 1;
        }

        if (cols < 1)
        {
            Log.Warning("Must have at least 1 column");
            cols = 1;
        }

        if (hexagonShape)
            PlaceHexagonShape(scale, rows);
        else
            PlaceParallelogram(scale, rows, cols);

        Log.Debug("Created system has " + GetParticles().Length + " particles");
    }

    private Vector2Int GetNeighborHexPos(Vector2Int pos, Direction direction, int scale, int distance = 1)
    {
        Vector2Int nbr = pos;

        switch (direction)
        {
            case Direction.N:
                nbr.x -= distance * 2 * scale;
                nbr.y += distance * 4 * scale;
                break;
            case Direction.WNW:
                nbr.x -= distance * 4 * scale;
                nbr.y += distance * 2 * scale;
                break;
            case Direction.WSW:
                nbr.x -= distance * 2 * scale;
                nbr.y -= distance * 2 * scale;
                break;
            case Direction.S:
                nbr.x += distance * 2 * scale;
                nbr.y -= distance * 4 * scale;
                break;
            case Direction.ESE:
                nbr.x += distance * 4 * scale;
                nbr.y -= distance * 2 * scale;
                break;
            case Direction.ENE:
                nbr.x += distance * 2 * scale;
                nbr.y += distance * 2 * scale;
                break;
        }

        return nbr;
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
                    ip.SetAttribute("hexagonType", type2 ? 2 : 1);
                    pos = ParticleSystem_Utils.GetNbrInDir(pos, rowDir);
                }
                startPos = ParticleSystem_Utils.GetNbrInDir(startPos, expansionDir, 2);
            }
        }
    }

    private void PlaceParallelogram(int scale, int rows, int cols)
    {
        Vector2Int startPos = Vector2Int.zero;
        for (int row = 0; row < rows; row++)
        {
            Vector2Int colPos = startPos;
            Direction nbrDir = (row % 2 == 0) ? Direction.ESE : Direction.WSW;
            for (int col = 0; col < cols; col++)
            {
                PlaceHexagon(colPos, scale);
                PlaceHexagon(GetNeighborHexPos(colPos, nbrDir, scale), scale, true);
                colPos = GetNeighborHexPos(colPos, Direction.ESE, scale);
                colPos = GetNeighborHexPos(colPos, Direction.ENE, scale);
            }
            startPos = GetNeighborHexPos(startPos, Direction.S, scale);
            startPos = GetNeighborHexPos(startPos, nbrDir, scale);
        }
    }

    private void PlaceHexagonShape(int scale, int size)
    {
        Vector2Int startPos = Vector2Int.zero;

        // Place the first hexagon to have a central anchor position
        PlaceHexagon(startPos, scale);

        // Move the start position far enough up
        for (int i = 0; i < size - 1; i++)
        {
            startPos = GetNeighborHexPos(startPos, Direction.N, scale);
            startPos = GetNeighborHexPos(startPos, Direction.WNW, scale);
        }

        // Place the start and end row
        Vector2Int pos = startPos;
        for (int i = 0; i < size; i++)
        {
            if (pos != Vector2Int.zero)
                PlaceHexagon(pos, scale);
            Vector2Int pos2 = GetNeighborHexPos(pos, Direction.S, scale, 3 * size - 1);
            PlaceHexagon(pos2, scale, true);
            pos = GetNeighborHexPos(pos, Direction.ESE, scale);
            pos = GetNeighborHexPos(pos, Direction.ENE, scale);
        }

        // Place the rows inbetween
        pos = GetNeighborHexPos(startPos, Direction.WSW, scale);
        for (int i = 0; i < size; i++)
        {
            // Place a row of type 2 hexagons with neighboring type 1 hexagons to the South
            // If this is not the last row, we mirror the same structure downwards as well
            int num = size + 1 + i;
            Vector2Int[] startPositions;
            if (i < size - 1)
                startPositions = new Vector2Int[] { pos, GetNeighborHexPos(pos, Direction.S, scale, 3 * ((size - 1) - i)) };
            else
                startPositions = new Vector2Int[] { pos };
            foreach (Vector2Int sp in startPositions)
            {
                Vector2Int p = sp;
                for (int j = 0; j < num; j++)
                {
                    PlaceHexagon(p, scale, true);
                    Vector2Int p2 = GetNeighborHexPos(p, Direction.S, scale);
                    if (p2 != Vector2Int.zero)
                        PlaceHexagon(p2, scale);

                    p = GetNeighborHexPos(p, Direction.ESE, scale);
                    p = GetNeighborHexPos(p, Direction.ENE, scale);
                }
            }

            pos = GetNeighborHexPos(pos, Direction.S, scale);
            pos = GetNeighborHexPos(pos, Direction.WSW, scale);
        }
    }
}
