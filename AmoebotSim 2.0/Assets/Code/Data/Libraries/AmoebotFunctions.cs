using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AmoebotFunctions
{
    // TODO: Put constant values into constants
    public static Vector2 CalculateAmoebotCenterPositionVector2(float gridPosX, float gridPosY)
    {
        //height difference: sin(60 / 180 * pi)
        //width difference: 0.5
        float heightDiff = Mathf.Sin(Mathf.PI * 60f / 180f);
        return new Vector2(gridPosX + 0.5f * gridPosY, gridPosY * heightDiff);
    }

    public static Vector2 CalculateAmoebotCenterPositionVector2(Vector2 gridPos)
    {
        return CalculateAmoebotCenterPositionVector2(gridPos.x, gridPos.y);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(float gridPosX, float gridPosY)
    {
        Vector2 centerPos = CalculateAmoebotCenterPositionVector2(gridPosX, gridPosY);
        return new Vector3(centerPos.x, centerPos.y, 0f);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(float gridPosX, float gridPosY, float z)
    {
        Vector2 centerPos = CalculateAmoebotCenterPositionVector2(gridPosX, gridPosY);
        return new Vector3(centerPos.x, centerPos.y, z);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(Vector2 gridPos)
    {
        return CalculateAmoebotCenterPositionVector2(gridPos.x, gridPos.y);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(Vector2 gridPos, float z)
    {
        return CalculateAmoebotCenterPositionVector3(gridPos.x, gridPos.y, z);
    }

    public static float HeightDifferenceBetweenRows()
    {
        return Mathf.Sin(Mathf.PI * 60f / 180f);
    }

    public static float HexVertex_XValue()
    {
        return 0.5f;
    }

    public static float HexVertex_YValueTop()
    {
        return 0.5f / Mathf.Cos(Mathf.Deg2Rad * 30f);
    }

    public static float HexVertex_YValueSides()
    {
        return 0.5f * Mathf.Tan(Mathf.Deg2Rad * 30f);
    }

    public static Vector2Int GetGridPositionFromWorldPosition(Vector2 worldPosition)
    {
        float sinVal = Mathf.Sin(Mathf.PI * 60f / 180f);
        float gridPosY = worldPosition.y / sinVal;
        float gridPosX = worldPosition.x - 0.5f * gridPosY;
        return new Vector2Int(Mathf.RoundToInt(gridPosX), Mathf.RoundToInt(gridPosY));
    }

    public static Vector2 NearestHexFieldWorldPositionFromWorldPosition(Vector2 worldPosition)
    {
        return CalculateAmoebotCenterPositionVector2(GetGridPositionFromWorldPosition(worldPosition));
    }

    private static float RoundTo(float value, float multipleOf)
    {
        return Mathf.Round(value / multipleOf) * multipleOf;
    }

    public static bool AreNodesNeighbors(Vector2Int node1, Vector2Int node2)
    {
        if (node1.y == node2.y - 1 && (node1.x == node2.x || node1.x == node2.x + 1)) return true;
        if (node1.y == node2.y + 1 && (node1.x == node2.x || node1.x == node2.x - 1)) return true;
        if (node1.y == node2.y && Mathf.Abs(node1.x - node2.x) == 1) return true;

        return false;
    }

    /// <summary>
    /// Returns the neighbor's position. Dir is starting from the east (0) in cc orientation.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static Vector2Int GetNeighborPosition(Vector2Int pos, int dir)
    {
        return pos + GetNeighborPositionOffset(dir);
    }

    /// <summary>
    /// Return the neighbor's position offset. Dir is starting from the east (0) in cc orientation.
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public static Vector2Int GetNeighborPositionOffset(int dir)
    {
        switch (dir)
        {
            case 0:
                return new Vector2Int(1, 0);
            case 1:
                return new Vector2Int(0, 1);
            case 2:
                return new Vector2Int(-1, 1);
            case 3:
                return new Vector2Int(-1, 0);
            case 4:
                return new Vector2Int(0, -1);
            case 5:
                return new Vector2Int(1, -1);
            default:
                return new Vector2Int(int.MaxValue, int.MaxValue);
        }
    }

    // Circuits ===============

    /// <summary>
    /// Calculates the relativ pin position in world space relativ to the particle center based on the abstract pin position definition,
    /// the number of particles, the scale and the viewType.
    /// </summary>
    /// <param name="pinDef"></param>
    /// <param name="pinsPerSide"></param>
    /// <param name="particleScale"></param>
    /// <param name="viewType"></param>
    /// <returns></returns>
    public static Vector2 CalculateRelativePinPosition(ParticlePinGraphicState.PinDef pinDef, int pinsPerSide, float particleScale, ViewType viewType)
    {
        float linePos = (pinDef.dirID + 1) / (float)(pinsPerSide + 1);
        Vector2 topRight = new Vector2(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides());
        Vector2 bottomRight = new Vector2(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides());
        Vector2 pinPosNonRotated = Vector2.zero;
        if (viewType == ViewType.Hexagonal) pinPosNonRotated = bottomRight + linePos * (topRight - bottomRight);
        else if (viewType == ViewType.HexagonalCirc) pinPosNonRotated = Quaternion.Euler(0f, 0f, -30f + linePos * 60f) * new Vector2(AmoebotFunctions.HexVertex_XValue(), 0f);
        pinPosNonRotated *= particleScale;
        Vector2 pinPos = Quaternion.Euler(0f, 0f, 60f * pinDef.globalDir) * pinPosNonRotated;
        return pinPos;
    }
}
