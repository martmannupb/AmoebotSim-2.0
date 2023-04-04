using System.Collections;
using System.Collections.Generic;
using AS2.Visuals;
using UnityEngine;

namespace AS2
{

    /// <summary>
    /// A collection of constants and utility methods for geometric calculations.
    /// </summary>
    public static class AmoebotFunctions
    {
        /// <summary>
        /// The vertical distance between two rows of the triangular grid
        /// if the edge length is <c>1</c>. This is equal to the height
        /// of a triangle.
        /// Can be calculated as <c>sin(60)</c> or <c>sqrt(3)/2</c>.
        /// </summary>
        public static readonly float rowDistVert = Mathf.Sin(Mathf.Deg2Rad * 60f);
        /// <summary>
        /// The distance between a hexagon's center and each of its sides if
        /// the triangular grid scale (triangle side length) is <c>1</c>.
        /// </summary>
        public static readonly float hexRadiusMinor = 0.5f;
        /// <summary>
        /// The distance between a hexagon's center and each of its corners
        /// if the triangular grid scale (triangle side length) is <c>1</c>.
        /// This is the same as the side length of the hexagon.
        /// </summary>
        public static readonly float hexRadiusMajor = 0.5f / Mathf.Cos(Mathf.Deg2Rad * 30f);
        /// <summary>
        /// Half of <see cref="hexRadiusMajor"/>.
        /// </summary>
        public static readonly float hexRadiusMajor2 = 0.25f / Mathf.Cos(Mathf.Deg2Rad * 30f);

        /// <summary>
        /// Translates grid coordinates into Cartesian (world) coordinates.
        /// </summary>
        /// <param name="gridPosX">The grid x coordinate.</param>
        /// <param name="gridPosY">The grid y coordinate.</param>
        /// <returns>The world coordinates corresponding to
        /// <paramref name="gridPosX"/> and <paramref name="gridPosY"/>.</returns>
        public static Vector2 GridToWorldPositionVector2(float gridPosX, float gridPosY)
        {
            // Grid y coordinate influences world x coordinate
            return new Vector2(gridPosX + 0.5f * gridPosY, gridPosY * rowDistVert);
        }

        /// <summary>
        /// Translates grid coordinates into Cartesian (world) coordinates.
        /// </summary>
        /// <param name="gridPos">The grid coordinates.</param>
        /// <returns>The world coordinates corresponding to
        /// <paramref name="gridPos"/>.</returns>
        public static Vector2 GridToWorldPositionVector2(Vector2 gridPos)
        {
            return GridToWorldPositionVector2(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Translates grid coordinates into Cartesian (world) coordinates
        /// with a z component set to <c>0</c>.
        /// </summary>
        /// <param name="gridPosX">The grid x coordinate.</param>
        /// <param name="gridPosY">The grid y coordinate.</param>
        /// <returns>A 3-dimensional vector where the first two components are
        /// the he world coordinates corresponding to <paramref name="gridPosX"/>
        /// and <paramref name="gridPosY"/>, and the third component is <c>0</c>.</returns>
        public static Vector3 GridToWorldPositionVector3(float gridPosX, float gridPosY)
        {
            Vector2 centerPos = GridToWorldPositionVector2(gridPosX, gridPosY);
            return new Vector3(centerPos.x, centerPos.y, 0f);
        }

        /// <summary>
        /// Translates grid coordinates into Cartesian (world) coordinates
        /// with a z component.
        /// </summary>
        /// <param name="gridPosX">The grid x coordinate.</param>
        /// <param name="gridPosY">The grid y coordinate.</param>
        /// <returns>A 3-dimensional vector where the first two components are
        /// the he world coordinates corresponding to <paramref name="gridPosX"/>
        /// and <paramref name="gridPosY"/>, and the third component has the value
        /// <paramref name="z"/>.</returns>
        public static Vector3 GridToWorldPositionVector3(float gridPosX, float gridPosY, float z)
        {
            Vector2 centerPos = GridToWorldPositionVector2(gridPosX, gridPosY);
            return new Vector3(centerPos.x, centerPos.y, z);
        }

        /// <summary>
        /// Translates grid coordinates into Cartesian (world) coordinates
        /// with a z component set to <c>0</c>.
        /// </summary>
        /// <param name="gridPos">The grid coordinates.</param>
        /// <returns>A 3-dimensional vector where the first two components are
        /// the he world coordinates corresponding to <paramref name="gridPos"/>
        /// and the third component is <c>0</c>.</returns>
        public static Vector3 GridToWorldPositionVector3(Vector2 gridPos)
        {
            return GridToWorldPositionVector2(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Translates grid coordinates into Cartesian (world) coordinates
        /// with a z component.
        /// </summary>
        /// <param name="gridPos">The grid coordinates.</param>
        /// <returns>A 3-dimensional vector where the first two components are
        /// the he world coordinates corresponding to <paramref name="gridPos"/>
        /// and the third component has the value <paramref name="z"/>.</returns>
        public static Vector3 GridToWorldPositionVector3(Vector2 gridPos, float z)
        {
            return GridToWorldPositionVector3(gridPos.x, gridPos.y, z);
        }

        /// <summary>
        /// Translates Cartesian (world) coordinates into the
        /// nearest integer grid coordinates.
        /// </summary>
        /// <param name="worldPosition">The world coordinates.</param>
        /// <returns>The grid coordinates corresponding to
        /// <paramref name="worldPosition"/>, rounded to the
        /// nearest integer.</returns>
        public static Vector2Int WorldToGridPosition(Vector2 worldPosition)
        {
            float gridPosY = worldPosition.y / rowDistVert;
            float gridPosX = worldPosition.x - 0.5f * gridPosY;
            return new Vector2Int(Mathf.RoundToInt(gridPosX), Mathf.RoundToInt(gridPosY));
        }

        /// <summary>
        /// Same as <see cref="WorldToGridPosition(Vector2)"/> but without
        /// rounding the result to the nearest integer.
        /// </summary>
        /// <param name="worldPosition">The world coordinates.</param>
        /// <returns>The grid coordinates corresponding to
        /// <paramref name="worldPosition"/>.</returns>
        public static Vector2 WorldToGridPositionF(Vector2 worldPosition)
        {
            float gridPosY = worldPosition.y / rowDistVert;
            float gridPosX = worldPosition.x - 0.5f * gridPosY;
            return new Vector2(gridPosX, gridPosY);
        }

        /// <summary>
        /// Calculates the world coordinates of the grid node
        /// closest to the given world coordinates.
        /// </summary>
        /// <param name="worldPosition">The world coordinates.</param>
        /// <returns>The world coordinates of the grid node closest
        /// to <paramref name="worldPosition"/>.</returns>
        public static Vector2 WorldPositionToNearestNodePosition(Vector2 worldPosition)
        {
            return GridToWorldPositionVector2(WorldToGridPosition(worldPosition));
        }

        #region Deprecated constant methods

        /// <summary>
        /// Returns the vertical distance between two rows in the
        /// triangular grid if the edge length is <c>1</c>.
        /// </summary>
        /// <remarks>
        /// Deprecated. Use <see cref="rowDistVert"/> instead.
        /// </remarks>
        /// <returns>The distance between two rows in the
        /// triangular grid.</returns>
        public static float HeightDifferenceBetweenRows()
        {
            return rowDistVert;
        }

        /// <summary>
        /// The distance between the center of a hexagon and
        /// each of its sides if the triangular grid scale
        /// (triangle side length) is <c>1</c>.
        /// </summary>
        /// <remarks>
        /// Deprecated. Use <see cref="hexRadiusMinor"/> instead.
        /// </remarks>
        /// <returns>The distance between the center of a
        /// hexagon and each of its sides.</returns>
        public static float HexVertex_XValue()
        {
            return 0.5f;
        }

        /// <summary>
        /// The distance between the center of a hexagon and each
        /// of its corners if the triangular grid scale (triangle
        /// side length) is <c>1</c>.
        /// </summary>
        /// <remarks>
        /// Deprecated. Use <see cref="hexRadiusMajor"/> instead.
        /// </remarks>
        /// <returns>The distance between the center of a
        /// hexagon and each of its corners.</returns>
        public static float HexVertex_YValueTop()
        {
            return 0.5f / Mathf.Cos(Mathf.Deg2Rad * 30f);
        }

        /// <summary>
        /// The vertical distance between the center of a hexagon
        /// and the top corners of its vertical sides, if the
        /// triangular grid scale (triangle side length) is <c>1</c>.
        /// </summary>
        /// <remarks>
        /// Deprecated. Use <see cref="hexRadiusMajor2"/> instead.
        /// </remarks>
        /// <returns>The vertical distance between the center of
        /// a hexagon and the top corners of its vertical sides.</returns>
        public static float HexVertex_YValueSides()
        {
            return 0.5f * Mathf.Tan(Mathf.Deg2Rad * 30f);
        }

        #endregion

        /// <summary>
        /// Checks whether the two given grid nodes are adjacent to each other.
        /// </summary>
        /// <param name="node1">The first grid node.</param>
        /// <param name="node2">The second grid node.</param>
        /// <returns><c>true</c> if and only if <paramref name="node1"/>
        /// and <paramref name="node2"/> are adjacent to each other.</returns>
        public static bool AreNodesNeighbors(Vector2Int node1, Vector2Int node2)
        {
            if (node1.y == node2.y - 1 && (node1.x == node2.x || node1.x == node2.x + 1)) return true;
            if (node1.y == node2.y + 1 && (node1.x == node2.x || node1.x == node2.x - 1)) return true;
            if (node1.y == node2.y && Mathf.Abs(node1.x - node2.x) == 1) return true;

            return false;
        }

        /// <summary>
        /// Returns the grid coordinates of the given grid node's neighbor
        /// in the specified direction.
        /// </summary>
        /// <param name="pos">The reference grid position.</param>
        /// <param name="dir">The direction of the neighbor,
        /// represented as an int. <c>0</c> means East and direction
        /// values increase in counter-clockwise direction.</param>
        /// <returns>The grid coordinates of the node neighboring
        /// <paramref name="pos"/> in direction <paramref name="dir"/>.</returns>
        public static Vector2Int GetNeighborPosition(Vector2Int pos, int dir)
        {
            return pos + GetNeighborPositionOffset(dir);
        }

        /// <summary>
        /// Returns the grid coordinates of the given grid node's neighbor
        /// in the specified direction.
        /// </summary>
        /// <param name="pos">The reference grid position.</param>
        /// <param name="dir">The direction of the neighbor.
        /// Secondary directions are mapped to cardinal directions.</param>
        /// <returns>The grid coordinates of the node neighboring
        /// <paramref name="pos"/> in direction <paramref name="dir"/>.</returns>
        public static Vector2Int GetNeighborPosition(Vector2Int pos, Direction dir)
        {
            return pos + GetNeighborPositionOffset(dir.ToInt());
        }

        /// <summary>
        /// Computes the grid unit vector pointing in the indicated direction.
        /// </summary>
        /// <param name="dir">The direction into which the vector should point,
        /// represented as an int. <c>0</c> means East and direction
        /// values increase in counter-clockwise direction.</param>
        /// <returns>A grid unit vector pointing in direction
        /// <paramref name="dir"/>.</returns>
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
            Vector2 topRight = new Vector2(hexRadiusMinor, hexRadiusMajor2);
            Vector2 bottomRight = new Vector2(hexRadiusMinor, -hexRadiusMajor2);
            Vector2 pinPosNonRotated = Vector2.zero;
            if (viewType == ViewType.Hexagonal) pinPosNonRotated = bottomRight + linePos * (topRight - bottomRight);
            else if (viewType == ViewType.HexagonalCirc) pinPosNonRotated = Quaternion.Euler(0f, 0f, -30f + linePos * 60f) * new Vector2(hexRadiusMinor, 0f);
            pinPosNonRotated *= particleScale;
            Vector2 pinPos = Quaternion.Euler(0f, 0f, 60f * pinDef.globalDir) * pinPosNonRotated;
            return pinPos;
        }
    }

} // namespace AS2
