using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

// Copyright ©
// Part of the personal code library of Tobias Maurer (tobias.maurer.it@web.de).
// Usage by any current or previous members the University of paderborn and projects associated with the University or programmable matter is permitted.

namespace AS2.Visuals.Library {


    // Meshes

    public static class MeshConstants {

        /// <summary>
        /// Returns a copy the default mesh with definable length, z coordinate and pivot.
        /// </summary>
        /// <param name="length">The length of a side.</param>
        /// <param name="z">z coordinate.</param>
        /// <param name="pivot">Pivot must be between (0,0) and (1,1).</param>
        /// <returns></returns>
        public static Mesh getDefaultMeshQuad(float length, float z, Vector2 pivot) {
            Mesh mesh = new Mesh();
            mesh.vertices = getDefaultVerticesForQuad(length, z, pivot);
            mesh.triangles = getDefaultTrianglesForQuad();
            mesh.uv = getDefaultUVMapForQuad();
            mesh.normals = getDefaultNormalsForQuad();
            return mesh;
        }

        /// <summary>
        /// Returns a copy the default mesh with definable pivot.
        /// </summary>
        /// <param name="pivot">Pivot must be between (0,0) and (1,1).</param>
        /// <returns></returns>
        public static Mesh getDefaultMeshQuad(Vector2 pivot) {
            return getDefaultMeshQuad(1f, 0f, pivot);
        }

        /// <summary>
        /// Returns a copy the default mesh with length 1 and pivot (0.5, 0.5).
        /// </summary>
        /// <returns></returns>
        public static Mesh getDefaultMeshQuad() {
            return getDefaultMeshQuad(1f, 0f, new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Returns a default mesh with the given bound radius. Useful for shaders with vertex offsets.
        /// </summary>
        /// <param name="boundRadius">The manually set bounds of the mesh.</param>
        /// <returns></returns>
        public static Mesh getDefaultMeshQuad(float boundRadius)
        {
            Mesh mesh = getDefaultMeshQuad();
            return AddManualBoundsToMesh(mesh, Vector3.zero, boundRadius, true);
        }

        /// <summary>
        /// Scales a mesh around the pivot. From the scaleVector each coordinate is multiplied with the corresponding coordinates from the mesh vertices.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="scaleVector"></param>
        /// <returns></returns>
        public static Mesh scaleMeshAroundPivot(Mesh mesh, Vector3 scaleVector)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(scaleVector.x * vertices[i].x, scaleVector.y * vertices[i].y, scaleVector.z * vertices[i].z);
            }
            mesh.vertices = vertices;

            return mesh;
        }

        /// <summary>
        /// Offsets the mesh's vertices by a certain vector.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="offsetVector"></param>
        /// <returns></returns>
        public static Mesh offsetMeshVertices(Mesh mesh, Vector3 offsetVector)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i] + offsetVector;
            }
            mesh.vertices = vertices;

            return mesh;
        }

        /// <summary>
        /// Adds some manual bounds to the mesh. Useful for shaders with vertex offsets.
        /// </summary>
        /// <param name="mesh">The mesh to edit.</param>
        /// <param name="boundPivotWorldSpace">The pivot of the cubic bounds.</param>
        /// <param name="boundRadius">The radius of the cubic bounds.</param>
        /// <returns></returns>
        public static Mesh AddManualBoundsToMesh(Mesh mesh, Vector3 boundPivotWorldSpace, float boundRadius, bool flatZ = false)
        {
            mesh.bounds = new Bounds(boundPivotWorldSpace, new Vector3(boundRadius * 2, boundRadius * 2, flatZ ? 0.01f : boundRadius * 2));
            return mesh;
        }

        /// <summary>
        /// Returns the default Quad Vertices with definable length, z coordinate and pivot. (0,0); (1,0); (0,1); (1,1) for length 1 and pivot (0,0). Pivot must be between (0,0) and (1,1).
        /// </summary>
        /// <param name="length">The length of a side.</param>
        /// <param name="pivot">Pivot must be between (0,0) and (1,1).</param>
        /// <param name="z">z coordinate.</param>
        /// <returns></returns>
        public static Vector3[] getDefaultVerticesForQuad(float length, float z, Vector2 pivot) {

            if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f) {
                Debug.LogError("getDefaultVerticesForQuad: Pivot must be between (0,0) and (1,1).");
                return null;
            }

            return new Vector3[] { new Vector3(-length * pivot.x, -length * pivot.y, z), new Vector3(length - length * pivot.x, -length * pivot.y, z), new Vector3(-length * pivot.x, length - length * pivot.y, z), new Vector3(length - length * pivot.x, length - length * pivot.y, z) };
        }

        /// <summary>
        /// Returns the default Quad Vertices with length 1 and custom pivot.
        /// </summary>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public static Vector3[] getDefaultVerticesForQuad(Vector2 pivot) {

            if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f) {
                Debug.LogError("getDefaultVerticesForQuad: Pivot must be between (0,0) and (1,1).");
                return null;
            }

            return new Vector3[] { new Vector3(-pivot.x, -pivot.y, 0f), new Vector3(1f - pivot.x, -pivot.y, 0f), new Vector3(-pivot.x, 1f - pivot.y, 0f), new Vector3(1f - pivot.x, 1f - pivot.y, 0f) };
        }

        /// <summary>
        /// Returns the default Quad Vertices with length 1 and pivot (0.5,0.5).
        /// </summary>
        /// <returns></returns>
        public static Vector3[] getDefaultVerticesForQuad() {
            return new Vector3[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
        }

        /// <summary>
        /// Returns the default triangles fitting to the other methods in this class! (0, 2, 1, 1, 2, 3)
        /// </summary>
        /// <returns></returns>
        public static int[] getDefaultTrianglesForQuad() {
            return new int[] { 0, 2, 1, 1, 2, 3 };
        }

        /// <summary>
        /// Returns the default UV map for a texture. (0,0), (1,0), (0,1), (1,1).
        /// </summary>
        /// <returns></returns>
        public static Vector2[] getDefaultUVMapForQuad() {
            return new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        }

        /// <summary>
        /// Returns the default normals for a quad. (Vector3.up x4).
        /// </summary>
        /// <returns></returns>
        public static Vector3[] getDefaultNormalsForQuad() {
            return new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        }

        /// <summary>
        /// Returns the default Quad Grid Vertices with definable size, quad length, z coordinate and pivot in the lower left quad. (0,1); (1,1); (0,0); (1,0) for length 1 and pivot (0,0). Pivot must be between (0,0) and (1,1).
        /// </summary>
        /// <param name="gridWidth">gridSize*gridSize = Size of the grid.</param>
        /// <param name="lengthQuad">The length of one quad.</param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3[] getDefaultVerticesForQuadGrid(int gridSize, float length, float z, Vector2 pivot) {
            if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f) {
                Debug.LogError("getDefaultVerticesForQuad: Pivot must be between (0,0) and (1,1).");
                return null;
            }
            if (gridSize <= 0) {
                Debug.LogError("getDefaultVerticesGridForQuad: gridSize must be greater than 0!");
                return null;
            }

            Vector3[] grid = new Vector3[(gridSize + 1) * (gridSize + 1)];
            // Precalc
            float xOffset = -length * pivot.x;
            float yOffset = -length * pivot.y;
            // Loop
            int gridIndex = 0;
            for (int y = 0; y <= gridSize; y++) {
                for (int x = 0; x <= gridSize; x++, gridIndex++) {
                    grid[gridIndex] = new Vector3(x * length + xOffset, y * length + yOffset, z);
                }
            }
            return grid;
        }

        /// <summary>
        /// Returns the default triangles for the quad grid starting from the bottom left for each row up to the top right.
        /// </summary>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static int[] getDefaultTrianglesForQuadGrid(int gridSize) {
            if (gridSize <= 0) {
                Debug.LogError("getDefaultTrianglesForQuadGrid: gridSize must be greater 0!");
                return null;
            }

            int[] triangles = new int[6 * gridSize * gridSize];
            int trianglesIndex = 0;
            for (int y = 0; y < gridSize; y++) {
                for (int x = 0; x < gridSize; x++, trianglesIndex += 6) {
                    triangles[trianglesIndex] = y * (gridSize + 1) + x;
                    triangles[trianglesIndex + 1] = (y + 1) * (gridSize + 1) + x;
                    triangles[trianglesIndex + 2] = y * (gridSize + 1) + x + 1;
                    triangles[trianglesIndex + 3] = triangles[trianglesIndex + 2];
                    triangles[trianglesIndex + 4] = triangles[trianglesIndex + 1];
                    triangles[trianglesIndex + 5] = (y + 1) * (gridSize + 1) + x + 1;
                }
            }
            return triangles;
        }

        /// <summary>
        /// Returns the default uv map that fits to the default vertices for the quad grid.
        /// </summary>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static Vector2[] getDefaultUVMapForQuadGrid(int gridSize) {
            if (gridSize <= 0) {
                Debug.LogError("getDefaultUVMapForQuadGrid: gridSize must be greater 0!");
                return null;
            }

            Vector2[] uv = new Vector2[(gridSize + 1) * (gridSize + 1)];
            // Loop
            int uvIndex = 0;
            for (int y = 0; y <= gridSize; y++) {
                for (int x = 0; x <= gridSize; x++, uvIndex++) {
                    uv[uvIndex] = new Vector3(x / (float)gridSize, y / (float)gridSize);
                }
            }
            return uv;
        }

        // Independant Vertices in Quad Grid --------------------------------------

        /// <summary>
        /// Returns Quad Grid Vertices where each Quad does not share vertices with the next Quad. Has the option for multiple of the same vertices on top of each other (e.g. for blending multiple textures on top of each other). Includes definable size, quad length, z coordinate and pivot in the lower left quad. (0,1); (1,1); (0,0); (1,0) for length 1 and pivot (0,0). Pivot must be between (0,0) and (1,1).
        /// Note: For example if we have amountOfQuadsOnTopOfEachOther=2: We draw the first Quad in the bottom left, then the same Quad again, then the next Quad to the right, then again this one, ...
        /// </summary>
        /// <param name="gridSize"></param>
        /// <param name="length"></param>
        /// <param name="z"></param>
        /// <param name="pivot"></param>
        /// <param name="amountOfQuadsOnTopOfEachOther"></param>
        /// <returns></returns>
        public static Vector3[] getVerticesForIndependantQuadGrid(int gridSize, float length, float z, Vector2 pivot, int amountOfQuadsOnTopOfEachOther) {
            
            if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f) {
                Debug.LogError("getVerticesForIndependantQuadGrid: Pivot must be between (0,0) and (1,1).");
                return null;
            }
            if (gridSize <= 0) {
                Debug.LogError("getVerticesForIndependantQuadGrid: gridSize must be greater than 0!");
                return null;
            }
            if(amountOfQuadsOnTopOfEachOther <= 0) {
                Debug.LogError("getVerticesForIndependantQuadGrid: amountOfQuadsOnTopOfEachOther must be at least 1");
            }
            
            Vector3[] grid = new Vector3[gridSize * gridSize * 4 * amountOfQuadsOnTopOfEachOther]; // 4 Vertices for each Quad * the amount of stacked Quads
            // Precalc
            float xOffset = -length * pivot.x;
            float yOffset = -length * pivot.y;
            // Loop
            int gridIndex = 0;
            int stackedQuads = 0;
            for (int y = 0; y <= gridSize; y++) {
                for (int x = 0; x <= gridSize; x++) {
                    for (stackedQuads = 0; stackedQuads < amountOfQuadsOnTopOfEachOther; stackedQuads++, gridIndex+=4) {
                        // Get one Quad
                        grid[gridIndex] = new Vector3(x * length + xOffset, y * length + yOffset, z);
                        grid[gridIndex + 1] = new Vector3(x * length + length + xOffset, y * length + yOffset, z);
                        grid[gridIndex + 2] = new Vector3(x * length + xOffset, y * length + length + yOffset, z);
                        grid[gridIndex + 3] = new Vector3(x * length + length + xOffset, y * length + length + yOffset, z);
                    }
                }
            }
            return grid;
        }

        // Custom Quad Grid ---------------------------------------------

        public static Mesh getCustomIndependantQuadGrid(float length, float z, Vector2 pivot, int sizeX, int sizeY) {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[4 * sizeX * sizeY];
            int[] triangles = new int[6 * sizeX * sizeY];
            Vector2[] uv = new Vector2[4 * sizeX * sizeY];
            Vector3[] normals = new Vector3[2 * sizeX * sizeY];

            int verticesIndex = 0;
            int triangleIndex = 0;
            int normalIndex = 0;
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    // Calc
                    // Vertices
                    vertices[verticesIndex] = new Vector3(x * length - pivot.x * length, y * length - pivot.y * length, z);
                    vertices[verticesIndex + 1] = new Vector3(x * length + (1f - pivot.x) * length, y * length - pivot.y * length, z);
                    vertices[verticesIndex + 2] = new Vector3(x * length - pivot.x * length, y * length + (1f - pivot.y) * length, z);
                    vertices[verticesIndex + 3] = new Vector3(x * length + (1f - pivot.x) * length, y * length + (1f - pivot.y) * length, z);
                    // Triangles
                    triangles[triangleIndex] = verticesIndex;
                    triangles[triangleIndex + 1] = verticesIndex + 2;
                    triangles[triangleIndex + 2] = verticesIndex + 1;
                    triangles[triangleIndex + 3] = verticesIndex + 1;
                    triangles[triangleIndex + 4] = verticesIndex + 2;
                    triangles[triangleIndex + 5] = verticesIndex + 3;
                    // UVs
                    uv[verticesIndex] = new Vector2(0f, 0f);
                    uv[verticesIndex + 1] = new Vector2(1f, 0f);
                    uv[verticesIndex + 2] = new Vector2(0f, 1f);
                    uv[verticesIndex + 3] = new Vector2(1f, 1f);
                    // Normals
                    normals[normalIndex] = new Vector3(0f, 0f, 1f);
                    normals[normalIndex + 1] = new Vector3(0f, 0f, 1f);

                    // Increment indices
                    verticesIndex += 4;
                    triangleIndex += 6;
                    normalIndex += 2;
                }
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.normals = normals;
            return mesh;
        }

        public static Vector3[] getCustomIndependantQuadGrid_Vertices(float length, float z, Vector2 pivot, int sizeX, int sizeY) {
            Vector3[] vertices = new Vector3[4 * sizeX * sizeY];
            int verticesIndex = 0;
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    // Calc
                    // Vertices
                    vertices[verticesIndex] = new Vector3(x * length - pivot.x * length, y * length - pivot.y * length, z);
                    vertices[verticesIndex + 1] = new Vector3(x * length + (1f - pivot.x) * length, y * length - pivot.y * length, z);
                    vertices[verticesIndex + 2] = new Vector3(x * length - pivot.x * length, y * length + (1f - pivot.y) * length, z);
                    vertices[verticesIndex + 3] = new Vector3(x * length + (1f - pivot.x) * length, y * length + (1f - pivot.y) * length, z);
                    // Increment
                    verticesIndex += 4;
                }
            }
            return vertices;
        }

        public static int[] getCustomIndependantQuadGrid_Triangles(float length, float z, Vector2 pivot, int sizeX, int sizeY) {
            int[] triangles = new int[6 * sizeX * sizeY];

            int verticesIndex = 0;
            int triangleIndex = 0;
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    // Calc
                    // Triangles
                    triangles[triangleIndex] = verticesIndex;
                    triangles[triangleIndex + 1] = verticesIndex + 2;
                    triangles[triangleIndex + 2] = verticesIndex + 1;
                    triangles[triangleIndex + 3] = verticesIndex + 1;
                    triangles[triangleIndex + 4] = verticesIndex + 2;
                    triangles[triangleIndex + 5] = verticesIndex + 3;
                    // Increment
                    verticesIndex += 4;
                    triangleIndex += 6;
                }
            }
            return triangles;
        }

        public static Vector2[] getCustomIndependantQuadGrid_UVs(float length, float z, Vector2 pivot, int sizeX, int sizeY) {
            Vector2[] uv = new Vector2[4 * sizeX * sizeY];
            int verticesIndex = 0;
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    // Calc
                    // UVs
                    uv[verticesIndex] = new Vector2(0f, 0f);
                    uv[verticesIndex + 1] = new Vector2(1f, 0f);
                    uv[verticesIndex + 2] = new Vector2(0f, 1f);
                    uv[verticesIndex + 3] = new Vector2(1f, 1f);

                    // Increment indices
                    verticesIndex += 4;
                }
            }
            return uv;
        }

        public static Vector3[] getCustomIndependantQuadGrid_Normals(float length, float z, Vector2 pivot, int sizeX, int sizeY) {
            Vector3[] normals = new Vector3[2 * sizeX * sizeY];
            int normalIndex = 0;
            for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    // Calc
                    // Normals
                    normals[normalIndex] = new Vector3(0f, 0f, 1f);
                    normals[normalIndex + 1] = new Vector3(0f, 0f, 1f);
                    // Increment
                    normalIndex += 2;
                }
            }
            return normals;
        }

    }


    public static class VectorConstants
    {
        /// <summary>
        /// Combines two Vector2 into one Vector4 (vec1.x, vec1.y, vec2.x, vec2.y).
        /// </summary>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public static Vector4 Combine2Vector2s(Vector2 vector1, Vector2 vector2)
        {
            return new Vector4(vector1.x, vector1.y, vector2.x, vector2.y);
        }

        /// <summary>
        /// Rotates a vector by a certain degree.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static Vector2 Rotate(Vector2 vector, float degrees)
        {
            return RotateRadians(vector, degrees * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Rotates a vector by radians.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static Vector2 RotateRadians(Vector2 vector, float radians)
        {
            var ca = Mathf.Cos(radians);
            var sa = Mathf.Sin(radians);
            return new Vector2(ca * vector.x - sa * vector.y, sa * vector.x + ca * vector.y);
        }
    }


    public static class MatrixConstants
    {
        
        public static Matrix4x4[] GetMatrix4x4Array(int size, Matrix4x4 initialValue)
        {
            Matrix4x4[] matrix = new Matrix4x4[size];
            for (int i = 0; i < size; i++)
            {
                matrix[i] = initialValue;
            }
            return matrix;
        }

    }


    public static class InterpolationConstants
    {

        public static float OneMinus(float percentage)
        {
            return 1.0f - percentage;
        }

        public static float EaseIn(float percentage)
        {
            return percentage * percentage;
        }

        public static float EaseOut(float percentage)
        {
            return OneMinus(EaseIn(OneMinus(percentage)));
        }

        public static float Lerp(float s1, float s2, float percentage)
        {
            return (s1 + (s2 - s1) * percentage);
        }

        public static float SmoothLerp(float percentage)
        {
            return Lerp(EaseIn(percentage), EaseOut(percentage), percentage);
        }

    }


    public static class DegreeConstants
    {
        /// <summary>
        /// Returns the coordinates for a given degree and radius.
        /// 0 degrees are east, counterclockwise rotation.
        /// </summary>
        /// <param name="degree"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Vector2 DegreeToCoordinate(double degree, double radius)
        {
            // Convert degree to radian
            double radian = degree * Math.PI / 180;

            // Calculate the x and y coordinates using sine and cosine
            double x = radius * Math.Cos(radian);
            double y = radius * Math.Sin(radian);

            // Return the calculated coordinates as a Vector2
            return new Vector2((float)x, (float)y);
        }

        /// <summary>
        /// Returns the coordinates for a given degree and radius.
        /// 0 degrees are east, counterclockwise rotation.
        /// 0 degrees can be offset by the degree offset (e.g. a degreeOffset of 90 makes 0 degrees point up).
        /// </summary>
        /// <param name="degree"></param>
        /// <param name="radius"></param>
        /// <param name="degreeOffset"></param>
        /// <returns></returns>
        public static Vector2 DegreeToCoordinate(double degree, double radius, float degreeOffset)
        {
            return DegreeToCoordinate(degree + degreeOffset, radius);
        }

        /// <summary>
        /// Returns the degree and the radius of the given coordinate.
        /// 0 degrees are east, counterclockwise rotation. Ranges from >180 to <=180;
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public static Tuple<double, double> CoordinateToDegree(Vector2 coordinate)
        {
            // Calculate the radius of the circle using the Pythagorean theorem
            double radius = Math.Sqrt(coordinate.x * coordinate.x + coordinate.y * coordinate.y);

            // Calculate the angle in radians using the arctangent function
            double radian = Math.Atan2(coordinate.y, coordinate.x);

            // Convert radian to degree
            double degree = radian * 180 / Math.PI;

            // Return the calculated angle in degrees and radius as a tuple
            return Tuple.Create(degree, radius);
        }

        /// <summary>
        /// Returns the degree and the radius of the given coordinate.
        /// 0 degrees are east, counterclockwise rotation. Ranges from >=0 to <360;
        /// 0 degrees can be offset by the degree offset (e.g. a degreeOffset of 90 subtracts this offset from the resulting degree).
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="degreeOffset"></param>
        /// <returns></returns>
        public static Tuple<double, double> CoordinateToDegree(Vector2 coordinate, float degreeOffset)
        {
            Tuple<double, double> t = CoordinateToDegree(coordinate);
            return Tuple.Create((t.Item1 - degreeOffset + 360f) % 360f, t.Item2);
        }

        /// <summary>
        /// Calculates the signed orthogonal distance from a line to a point. Well, at least I hope the sign is correct.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="linePoint1">First point of the line.</param>
        /// <param name="linePoint2">Second point of the line that should not be the first point.</param>
        /// <returns></returns>
        public static float ManuallyImplementedSignedOrthogonalDistancesOfPointToLineFromAToB(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            Vector2 lineDirection = (linePoint2 - linePoint1).normalized;
            Vector2 lineDirectionRot90Deg = Quaternion.Euler(0f, 0f, 90f) * lineDirection;
            //Vector2 linePoint1ToPoint = point - linePoint1;
            float distToC = ((point.x - linePoint1.x) * (-linePoint2.y + linePoint1.y) + (point.y - linePoint1.y) * (linePoint2.x - linePoint1.x)) / lineDirectionRot90Deg.magnitude;
            return distToC;
        }

    }

}