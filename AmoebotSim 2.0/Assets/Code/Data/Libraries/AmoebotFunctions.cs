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

        // Rotation matrices for counter-clockwise rotations in 60 degree steps
        public static readonly Quaternion rotation_0 = Quaternion.Euler(0f, 0f, 0f);
        public static readonly Quaternion rotation_60 = Quaternion.Euler(0f, 0f, 60f);
        public static readonly Quaternion rotation_120 = Quaternion.Euler(0f, 0f, 120f);
        public static readonly Quaternion rotation_180 = Quaternion.Euler(0f, 0f, 180f);
        public static readonly Quaternion rotation_240 = Quaternion.Euler(0f, 0f, 240f);
        public static readonly Quaternion rotation_300 = Quaternion.Euler(0f, 0f, 300f);
        /// <summary>
        /// Rotation matrices for counter-clockwise rotations in 60 degree steps.
        /// The matrix at index i rotates by <c>i * 60</c> degrees.
        /// </summary>
        public static readonly Quaternion[] rotationMatrices = new Quaternion[] {
            rotation_0, rotation_60, rotation_120, rotation_180, rotation_240, rotation_300
        };

        // Caches for relative pin positions
        // List index is number of pins per side - 1, first array index
        // is direction, second array index is edge offset
        private static List<Vector2[][]> pinPositionCacheHex = new List<Vector2[][]>();
        private static List<Vector2[][]> pinPositionCacheRound = new List<Vector2[][]>();

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
        /// <para>
        /// Uses the cube coordinate transformation explained in
        /// https://www.redblobgames.com/grids/hexagons/#rounding.
        /// </para>
        /// </summary>
        /// <param name="worldPosition">The world coordinates.</param>
        /// <returns>The grid coordinates corresponding to
        /// <paramref name="worldPosition"/>, rounded to the
        /// nearest grid cell.</returns>
        public static Vector2Int WorldToGridPosition(Vector2 worldPosition)
        {
            // First convert to float grid coordinates and add one more coordinate
            Vector3 gridPosF = WorldToGridPositionF(worldPosition);
            // Third coordinate is the negative sum of the first two, such that x + y + z = 0
            // (cube coordinates)
            gridPosF.z = -gridPosF.x - gridPosF.y;
            // Round each coordinate to the nearest int
            Vector3Int rounded = new Vector3Int(Mathf.RoundToInt(gridPosF.x), Mathf.RoundToInt(gridPosF.y), Mathf.RoundToInt(gridPosF.z));
            // Compute absolute differences between float and rounded values
            float diffX = Mathf.Abs(gridPosF.x - rounded.x);
            float diffY = Mathf.Abs(gridPosF.y - rounded.y);
            float diffZ = Mathf.Abs(gridPosF.z - rounded.z);
            // Coordinates may have to be changed depending on which difference is the largest
            if (diffX > diffY && diffX > diffZ)
                rounded.x = -rounded.y - rounded.z;
            else if (diffY > diffZ)
                rounded.y = -rounded.x - rounded.z;

            return new Vector2Int(rounded.x, rounded.y);
        }

        /// <summary>
        /// Same as <see cref="WorldToGridPosition(Vector2)"/> but without
        /// rounding the result to the nearest grid cell.
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
        /// Calculates the relative pin position in world space
        /// relative to the particle center based on the abstract
        /// pin position definition, the number of pins, the
        /// scale and the view type.
        /// </summary>
        /// <param name="pinDef">The pin whose position to calculate.</param>
        /// <param name="pinsPerSide">The number of pins per side.</param>
        /// <param name="viewType">The view type used to place the pins.</param>
        /// <returns>A vector representing the pin's coordinates relative
        /// to the particle's center. Will be <c>(0, 0)</c> if the
        /// <paramref name="viewType"/> is not <see cref="ViewType.Hexagonal"/>
        /// or <see cref="ViewType.HexagonalCirc"/>.</returns>
        public static Vector2 CalculateRelativePinPosition(ParticlePinGraphicState.PinDef pinDef, int pinsPerSide, ViewType viewType)
        {
            if (pinsPerSide > pinPositionCacheHex.Count)
                AdvancePinPositionCaches(pinsPerSide);

            if (viewType == ViewType.Hexagonal)
                return pinPositionCacheHex[pinsPerSide - 1][pinDef.globalDir][pinDef.dirID];
            else if (viewType == ViewType.HexagonalCirc)
                return pinPositionCacheRound[pinsPerSide - 1][pinDef.globalDir][pinDef.dirID];
            else
                return Vector2.zero;
        }

        /// <summary>
        /// Adds entries to the pin position caches such that the
        /// desired number of pins per side is included.
        /// </summary>
        /// <param name="maxPinsPerSide">The desired number of pins
        /// per side. After this, both caches will have length at
        /// least <paramref name="maxPinsPerSide"/>.</param>
        private static void AdvancePinPositionCaches(int maxPinsPerSide)
        {
            Vector2 topRight = new Vector2(hexRadiusMinor, hexRadiusMajor2);
            Vector2 bottomRight = new Vector2(hexRadiusMinor, -hexRadiusMajor2);
            Vector2 pinPosNonRotatedHex;
            Vector2 pinPosNonRotatedRound;

            for (int pps = pinPositionCacheHex.Count + 1; pps <= maxPinsPerSide; pps++)
            {
                Vector2[][] positionsHex = new Vector2[6][];
                Vector2[][] positionsRound = new Vector2[6][];

                // Calculate the vectors for rotation 0
                Vector2[] dirVecsHex = new Vector2[pps];
                Vector2[] dirVecsRound = new Vector2[pps];

                for (int offset = 0; offset < pps; offset++)
                {
                    float linePos = (offset + 1) / (float)(pps + 1);
                    pinPosNonRotatedHex = bottomRight + linePos * (topRight - bottomRight);
                    pinPosNonRotatedRound = Quaternion.Euler(0f, 0f, -30f + linePos * 60f) * new Vector2(hexRadiusMinor, 0f);
                    pinPosNonRotatedHex *= RenderSystem.global_particleScale;
                    pinPosNonRotatedRound *= RenderSystem.global_particleScale;
                    dirVecsHex[offset] = pinPosNonRotatedHex;
                    dirVecsRound[offset] = pinPosNonRotatedRound;
                }

                positionsHex[0] = dirVecsHex;
                positionsRound[0] = dirVecsRound;

                // The other vectors are just rotated copies
                for (int dir = 1; dir < 6; dir++)
                {
                    Vector2[] rotatedHex = new Vector2[pps];
                    Vector2[] rotatedRound = new Vector2[pps];
                    for (int offset = 0; offset < pps; offset++)
                    {
                        rotatedHex[offset] = rotationMatrices[dir] * dirVecsHex[offset];
                        rotatedRound[offset] = rotationMatrices[dir] * dirVecsRound[offset];
                    }
                    positionsHex[dir] = rotatedHex;
                    positionsRound[dir] = rotatedRound;
                }

                pinPositionCacheHex.Add(positionsHex);
                pinPositionCacheRound.Add(positionsRound);
            }
        }
    }

} // namespace AS2
