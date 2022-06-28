using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AmoebotFunctions
{
    public static Vector2 CalculateAmoebotCenterPositionVector2(int gridPosX, int gridPosY)
    {
        //height difference: sin(60 / 180 * pi)
        //width difference: 0.5
        float heightDiff = Mathf.Sin(Mathf.PI * 60f / 180f);
        return new Vector2(gridPosX + 0.5f * gridPosY, gridPosY * heightDiff);
    }

    public static Vector2 CalculateAmoebotCenterPositionVector2(Vector2Int gridPos)
    {
        return CalculateAmoebotCenterPositionVector2(gridPos.x, gridPos.y);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(int gridPosX, int gridPosY)
    {
        Vector2 centerPos = CalculateAmoebotCenterPositionVector2(gridPosX, gridPosY);
        return new Vector3(centerPos.x, centerPos.y, 0f);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(int gridPosX, int gridPosY, float z)
    {
        Vector2 centerPos = CalculateAmoebotCenterPositionVector2(gridPosX, gridPosY);
        return new Vector3(centerPos.x, centerPos.y, z);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(Vector2Int gridPos)
    {
        return CalculateAmoebotCenterPositionVector2(gridPos.x, gridPos.y);
    }

    public static Vector3 CalculateAmoebotCenterPositionVector3(Vector2Int gridPos, float z)
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
}
