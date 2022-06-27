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
        // We do not know yet if we are at a hexagon center, to avoid rounding errors we check a small square
        if (Mathf.Abs(Mathf.Round(gridPosX) - gridPosX) < 0.05f && Mathf.Abs(Mathf.Round(gridPosY) - gridPosY) < 0.05f) return new Vector2Int(Mathf.RoundToInt(gridPosX), Mathf.RoundToInt(gridPosY));
        else
        {
            return GetGridPositionFromWorldPosition(NearestHexFieldWorldPositionFromWorldPosition(worldPosition)); // please work :)
        }
    }

    public static Vector2 NearestHexFieldWorldPositionFromWorldPosition(Vector2 worldPosition)
    {
        float nearestX1 = Mathf.Round(worldPosition.x * 2f) / 2f;
        float nearestX2 = nearestX1 < worldPosition.x ? nearestX2 = nearestX1 + 0.5f : nearestX2 = nearestX1 - 0.5f;
        float nearestY1 = RoundTo(worldPosition.y, HeightDifferenceBetweenRows());
        float nearestY2 = nearestY1 < worldPosition.y ? nearestY2 = RoundTo(nearestY1 + HeightDifferenceBetweenRows(), HeightDifferenceBetweenRows()) : RoundTo(nearestY1 - HeightDifferenceBetweenRows(), HeightDifferenceBetweenRows());
        float minX = Mathf.Min(nearestX1, nearestX2);
        float maxX = Mathf.Max(nearestX1, nearestX2);
        float minY = Mathf.Min(nearestY1, nearestY2);
        float maxY = Mathf.Max(nearestY1, nearestY2);
        bool evenRow = Mathf.RoundToInt(minY / HeightDifferenceBetweenRows()) % 2 == 0;
        Vector2 pos1;
        Vector2 pos2;
        if(evenRow)
        {
            // min row is even
            // x values are multiples of 1
            if(minX % 1f == 0f)
            {
                pos1 = new Vector2(minX, minY);
                pos2 = new Vector2(maxX, maxY);
            }
            else if(maxX % 1f == 0f)
            {
                pos1 = new Vector2(maxX, minY);
                pos2 = new Vector2(minX, maxY);
            }
            else
            {
                Log.Error("NearestHexFieldFromWorldPosition: E1; minX: " + minX + ", maxX: " + maxX + ", minY: " + minY + ", maxY: " + maxY + ", input: " + worldPosition);
                throw new System.ArithmeticException();
            }
        }
        else
        {
            // min row is odd
            // x values are multiples of 1 + 0.5
            if(minX % 1f == 0.5f)
            {
                pos1 = new Vector2(minX, minY);
                pos2 = new Vector2(maxX, maxY);
            }
            else if(maxX % 1f == 0.5f)
            {
                pos1 = new Vector2(maxX, minY);
                pos2 = new Vector2(minX, maxY);
            }
            else
            {
                Log.Error("NearestHexFieldFromWorldPosition: E2; minX: " + minX + ", maxX: " + maxX + ", minY: " + minY + ", maxY: " + maxY + ", input: " + worldPosition);
                throw new System.ArithmeticException();
            }
        }

        if(Vector2.Distance(worldPosition, pos1) < Vector2.Distance(worldPosition, pos2))
        {
            return pos1;
        }
        else
        {
            return pos2;
        }
    }

    private static float RoundTo(float value, float multipleOf)
    {
        return Mathf.Round(value / multipleOf) * multipleOf;
    }
}
