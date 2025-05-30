// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Creates the meshes for the hexagonal views.
    /// </summary>
    public static class MeshCreator_HexagonalView
    {

        /// <summary>
        /// Returns a mesh for a base hexagon in the standard size.
        /// </summary>
        /// <returns>A simple mesh with 6 vertices and 4 triangles
        /// forming a hexagon. The origin is at the center of
        /// the hexagon.</returns>
        public static Mesh GetMesh_BaseHexagonBackground()
        {
            Mesh mesh = new Mesh();

            // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
            Vector3 topLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 top = new Vector3(0f, AmoebotFunctions.hexRadiusMajor, 0f);
            Vector3 topRight = new Vector3(AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 bottomLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 bottom = new Vector3(0f, -AmoebotFunctions.hexRadiusMajor, 0f);
            Vector3 bottomRight = new Vector3(AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);

            // Parameters
            float scale = RenderSystem.const_hexagonalScale;

            Vector3[] vertices = new Vector3[6];
            int[] triangles = new int[12];
            Vector2[] uv = new Vector2[6];
            Vector3[] normals = new Vector3[6];

            // Vertices _____
            // Left
            vertices[0] = bottomLeft * scale;
            vertices[1] = topLeft * scale;
            vertices[2] = top * scale;
            vertices[3] = topRight * scale;
            vertices[4] = bottomRight * scale;
            vertices[5] = bottom * scale;

            // Triangles _____
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 0;
            triangles[4] = 2;
            triangles[5] = 3;
            triangles[6] = 0;
            triangles[7] = 3;
            triangles[8] = 4;
            triangles[9] = 0;
            triangles[10] = 4;
            triangles[11] = 5;

            // UV0s _____
            uv[0] = bottomLeft.normalized;
            uv[1] = topLeft.normalized;
            uv[2] = top.normalized;
            uv[3] = topRight.normalized;
            uv[4] = bottomRight.normalized;
            uv[5] = bottom.normalized;

            // Normals _____
            for (int i = 0; i < 6; i++)
            {
                normals[i] = Vector3.up;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.normals = normals;

            return mesh;
        }

        /// <summary>
        /// DEPRECATED.
        /// <para>
        /// Creates a mesh for the base expansion hexagon
        /// (for use with the newest HexagonExpansionMaterial).
        /// </para>
        /// </summary>
        /// <returns></returns>
        public static Mesh GetMesh_BaseExpansionHexagon()
        {
            Mesh mesh = new Mesh();

            // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
            Vector3 topLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 top = new Vector3(0f, AmoebotFunctions.hexRadiusMajor, 0f);
            Vector3 topRight = new Vector3(AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 bottomLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 bottom = new Vector3(0f, -AmoebotFunctions.hexRadiusMajor, 0f);
            Vector3 bottomRight = new Vector3(AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);

            // Parameters
            float scale = RenderSystem.const_hexagonalScale;
            float hexagonWidth = RenderSystem.const_hexagonalBorderWidth;
            float halfHexagonWidth = hexagonWidth / 2f;

            // Hexagon Vertex Coordinates
            Vector3 topLeftI = topLeft * (scale - halfHexagonWidth);
            Vector3 topLeftO = topLeft * (scale + halfHexagonWidth);
            Vector3 topI = top * (scale - halfHexagonWidth);
            Vector3 topO = top * (scale + halfHexagonWidth);
            Vector3 topRightI = topRight * (scale - halfHexagonWidth);
            Vector3 topRightO = topRight * (scale + halfHexagonWidth);
            Vector3 bottomRightI = bottomRight * (scale - halfHexagonWidth);
            Vector3 bottomRightO = bottomRight * (scale + halfHexagonWidth);
            Vector3 bottomI = bottom * (scale - halfHexagonWidth);
            Vector3 bottomO = bottom * (scale + halfHexagonWidth);
            Vector3 bottomLeftI = bottomLeft * (scale - halfHexagonWidth);
            Vector3 bottomLeftO = bottomLeft * (scale + halfHexagonWidth);


            Vector3[] vertices = new Vector3[4 * 6 + 3 * 2 + 6];
            int[] triangles = new int[6 * 6 + 2 * 3 + 4 * 3];
            Vector2[] uv = new Vector2[vertices.Length];
            Vector2[] uv2 = new Vector2[vertices.Length];
            Vector4[] uv3 = new Vector4[vertices.Length];
            Vector4[] uv4 = new Vector4[vertices.Length];
            Vector3[] normals = new Vector3[vertices.Length];

            // Vertices _____
            // Left
            vertices[0] = bottomLeftO;
            vertices[1] = bottomLeftI;
            vertices[2] = topLeftO;
            vertices[3] = topLeftI;
            // Top Left
            vertices[4] = topLeftO;
            vertices[5] = topLeftI;
            vertices[6] = topO;
            vertices[7] = topI;
            // Top Right
            vertices[8] = topO;
            vertices[9] = topI;
            vertices[10] = topRightO;
            vertices[11] = topRightI;
            // Right
            vertices[12] = topRightO;
            vertices[13] = topRightI;
            vertices[14] = bottomRightO;
            vertices[15] = bottomRightI;
            // Bottom Right
            vertices[16] = bottomRightO;
            vertices[17] = bottomRightI;
            vertices[18] = bottomO;
            vertices[19] = bottomI;
            // Bottom Left
            vertices[20] = bottomO;
            vertices[21] = bottomI;
            vertices[22] = bottomLeftO;
            vertices[23] = bottomLeftI;
            // Corner Triangle 1
            vertices[24] = bottomLeftO;
            vertices[25] = new Vector3(-1, 0f, 0f) + bottomRightI;
            vertices[26] = bottomLeftI;
            // Corner Triangle 2
            vertices[27] = topLeftO;
            vertices[28] = topLeftI;
            vertices[29] = new Vector3(-1, 0f, 0f) + topRightI;
            // Background
            vertices[30] = bottomLeft + new Vector3(0f, 0f, 0.1f);
            vertices[31] = topLeft + new Vector3(0f, 0f, 0.1f);
            vertices[32] = top + new Vector3(0f, 0f, 0.1f);
            vertices[33] = topRight + new Vector3(0f, 0f, 0.1f);
            vertices[34] = bottomRight + new Vector3(0f, 0f, 0.1f);
            vertices[35] = bottom + new Vector3(0f, 0f, 0.1f);

            // Triangles _____
            // Background
            triangles[0] = 30;
            triangles[1] = 31;
            triangles[2] = 32;
            triangles[3] = 30;
            triangles[4] = 32;
            triangles[5] = 33;
            triangles[6] = 30;
            triangles[7] = 33;
            triangles[8] = 34;
            triangles[9] = 30;
            triangles[10] = 34;
            triangles[11] = 35;
            for (int i = 0; i < 6; i++)
            {
                triangles[12 + i * 6 + 0] = i * 4 + 0;
                triangles[12 + i * 6 + 1] = i * 4 + 2;
                triangles[12 + i * 6 + 2] = i * 4 + 1;

                triangles[12 + i * 6 + 3] = i * 4 + 1;
                triangles[12 + i * 6 + 4] = i * 4 + 2;
                triangles[12 + i * 6 + 5] = i * 4 + 3;
            }
            // Corner Triangles
            triangles[12 + 36] = 24;
            triangles[12 + 37] = 25;
            triangles[12 + 38] = 26;
            triangles[12 + 39] = 27;
            triangles[12 + 40] = 28;
            triangles[12 + 41] = 29;

            // UV0s _____
            for (int i = 0; i < 6; i++)
            {
                uv[i * 4 + 0] =
                uv[i * 4 + 1] = new Vector2(1f, 0f);
                uv[i * 4 + 2] = new Vector2(0f, 1f);
                uv[i * 4 + 3] = new Vector2(1f, 1f);
            }
            // Corner Triangles
            uv[24] = new Vector2(0f, 0.5f);
            uv[25] = new Vector2(1f, 1f);
            uv[26] = new Vector2(1f, 0f);
            uv[27] = new Vector2(0f, 0.5f);
            uv[28] = new Vector2(1f, 1f);
            uv[29] = new Vector2(1f, 0f);
            // Background
            uv[30] = bottomLeft.normalized;
            uv[31] = topLeft.normalized;
            uv[32] = top.normalized;
            uv[33] = topRight.normalized;
            uv[34] = bottomRight.normalized;
            uv[35] = bottom.normalized;

            // UV1s _____
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    uv2[i * 4 + j] = new Vector2(i, 0);
                }
            }
            // Corner Triangles
            uv2[24] = new Vector2(6f, 0f);
            uv2[25] = new Vector2(6f, 0f);
            uv2[26] = new Vector2(6f, 0f);
            uv2[27] = new Vector2(6f, 0f);
            uv2[28] = new Vector2(6f, 0f);
            uv2[29] = new Vector2(6f, 0f);
            // Background (y value defines vertex number)
            uv2[30] = new Vector2(7f, 0f);
            uv2[31] = new Vector2(7f, 1f);
            uv2[32] = new Vector2(7f, 2f);
            uv2[33] = new Vector2(7f, 3f);
            uv2[34] = new Vector2(7f, 4f);
            uv2[35] = new Vector2(7f, 5f);

            // UV2s _____
            // Left
            uv3[0] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftO - bottomLeftO);
            uv3[1] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftI - bottomLeftI);
            uv3[2] = Library.VectorConstants.Combine2Vector2s(bottomLeftO - topLeftO, Vector2.zero);
            uv3[3] = Library.VectorConstants.Combine2Vector2s(bottomLeftI - topLeftI, Vector2.zero);
            // Top Left
            uv3[4] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topO - topLeftO);
            uv3[5] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topI - topLeftI);
            uv3[6] = Library.VectorConstants.Combine2Vector2s(topLeftO - topO, Vector2.zero);
            uv3[7] = Library.VectorConstants.Combine2Vector2s(topLeftI - topI, Vector2.zero);
            // Top Right
            uv3[8] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightO - topO);
            uv3[9] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightI - topI);
            uv3[10] = Library.VectorConstants.Combine2Vector2s(topO - topRightO, Vector2.zero);
            uv3[11] = Library.VectorConstants.Combine2Vector2s(topI - topRightI, Vector2.zero);
            // Right
            uv3[12] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightO - topRightO);
            uv3[13] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightI - topRightI);
            uv3[14] = Library.VectorConstants.Combine2Vector2s(topRightO - bottomRightO, Vector2.zero);
            uv3[15] = Library.VectorConstants.Combine2Vector2s(topRightI - bottomRightI, Vector2.zero);
            // Bottom Right
            uv3[16] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomO - bottomRightO);
            uv3[17] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomI - bottomRightI);
            uv3[18] = Library.VectorConstants.Combine2Vector2s(bottomRightO - bottomO, Vector2.zero);
            uv3[19] = Library.VectorConstants.Combine2Vector2s(bottomRightI - bottomI, Vector2.zero);
            // Bottom Left
            uv3[20] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftO - bottomO);
            uv3[21] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftI - bottomI);
            uv3[22] = Library.VectorConstants.Combine2Vector2s(bottomO - bottomLeftO, Vector2.zero);
            uv3[23] = Library.VectorConstants.Combine2Vector2s(bottomI - bottomLeftI, Vector2.zero);
            // Corner Triangles
            uv3[24] = Vector4.zero;
            uv3[25] = Vector4.zero;
            uv3[26] = Vector4.zero;
            uv3[27] = Vector4.zero;
            uv3[28] = Vector4.zero;
            uv3[29] = Vector4.zero;
            // Background (here the clockwise contraction offsets are defined)
            Vector2 conVector1cl = topLeft - bottomLeft;
            Vector2 conVector2cl = top - topLeft;
            uv3[30] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 0f), Library.VectorConstants.Rotate(conVector2cl, 0f));
            uv3[31] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 300f), Library.VectorConstants.Rotate(conVector2cl, 300f));
            uv3[32] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 240f), Library.VectorConstants.Rotate(conVector2cl, 240f));
            uv3[33] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 180f), Library.VectorConstants.Rotate(conVector2cl, 180f));
            uv3[34] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 120f), Library.VectorConstants.Rotate(conVector2cl, 120f));
            uv3[35] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 60f), Library.VectorConstants.Rotate(conVector2cl, 60f));

            // UV3s _____
            // Left (Left to Right)
            Vector2 expVector1bot = bottomO - bottomLeftO;
            Vector2 expVector2bot = (bottomRight - bottomLeft) - (bottomO - bottomLeftO);
            Vector2 expVector1top = topO - topLeftO;
            Vector2 expVector2top = (topRight - topLeft) - (topO - topLeftO);
            uv4[0] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 0f), Library.VectorConstants.Rotate(expVector2bot, 0f));
            uv4[1] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 0f), Library.VectorConstants.Rotate(expVector2bot, 0f));
            uv4[2] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 0f), Library.VectorConstants.Rotate(expVector2top, 0f));
            uv4[3] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 0f), Library.VectorConstants.Rotate(expVector2top, 0f));
            // Top Left
            uv4[4] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 300f), Library.VectorConstants.Rotate(expVector2bot, 300f));
            uv4[5] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 300f), Library.VectorConstants.Rotate(expVector2bot, 300f));
            uv4[6] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 300f), Library.VectorConstants.Rotate(expVector2top, 300f));
            uv4[7] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 300f), Library.VectorConstants.Rotate(expVector2top, 300f));
            // Top Right
            uv4[8] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 240f), Library.VectorConstants.Rotate(expVector2bot, 240f));
            uv4[9] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 240f), Library.VectorConstants.Rotate(expVector2bot, 240f));
            uv4[10] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 240f), Library.VectorConstants.Rotate(expVector2top, 240f));
            uv4[11] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 240f), Library.VectorConstants.Rotate(expVector2top, 240f));
            // Right (Right to Left)
            uv4[12] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 180f), Library.VectorConstants.Rotate(expVector2bot, 180f));
            uv4[13] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 180f), Library.VectorConstants.Rotate(expVector2bot, 180f));
            uv4[14] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 180f), Library.VectorConstants.Rotate(expVector2top, 180f));
            uv4[15] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 180f), Library.VectorConstants.Rotate(expVector2top, 180f));
            // Bottom Right
            uv4[16] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 120f), Library.VectorConstants.Rotate(expVector2bot, 120f));
            uv4[17] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 120f), Library.VectorConstants.Rotate(expVector2bot, 120f));
            uv4[18] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 120f), Library.VectorConstants.Rotate(expVector2top, 120f));
            uv4[19] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 120f), Library.VectorConstants.Rotate(expVector2top, 120f));
            // Bottom Left
            uv4[20] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 60f), Library.VectorConstants.Rotate(expVector2bot, 60f));
            uv4[21] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 60f), Library.VectorConstants.Rotate(expVector2bot, 60f));
            uv4[22] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 60f), Library.VectorConstants.Rotate(expVector2top, 60f));
            uv4[23] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 60f), Library.VectorConstants.Rotate(expVector2top, 60f));
            // Corner Triangles
            uv4[24] = Vector4.zero;
            uv4[25] = Vector4.zero;
            uv4[26] = Vector4.zero;
            uv4[27] = Vector4.zero;
            uv4[28] = Vector4.zero;
            uv4[29] = Vector4.zero;
            // Background (here the counterclockwise contraction offsets are defined)
            Vector2 conVector1cc = bottom - bottomLeft;
            Vector2 conVector2cc = bottomRight - bottom;
            uv4[30] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 0f), Library.VectorConstants.Rotate(conVector2cc, 0f));
            uv4[31] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 300f), Library.VectorConstants.Rotate(conVector2cc, 300f));
            uv4[32] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 240f), Library.VectorConstants.Rotate(conVector2cc, 240f));
            uv4[33] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 180f), Library.VectorConstants.Rotate(conVector2cc, 180f));
            uv4[34] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 120f), Library.VectorConstants.Rotate(conVector2cc, 120f));
            uv4[35] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 60f), Library.VectorConstants.Rotate(conVector2cc, 60f));

            // Normals _____
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.uv2 = uv2;
            mesh.SetUVs(2, uv3);
            mesh.SetUVs(3, uv4);
            mesh.normals = normals;

            return mesh;
        }

        /// <summary>
        /// DEPRECATED.
        /// <para>
        /// Creates a mesh for the base expansion hexagon
        /// (for use with the newest HexagonExpansionMaterial).
        /// </para>
        /// </summary>
        /// <returns></returns>
        public static Mesh GetMesh_CombinedExpansionRectangle()
        {
            Mesh mesh = new Mesh();

            // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
            Vector3 topLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 top = new Vector3(0f, AmoebotFunctions.hexRadiusMajor, 0f);
            Vector3 topRight = new Vector3(AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 bottomLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);
            Vector3 bottom = new Vector3(0f, -AmoebotFunctions.hexRadiusMajor, 0f);
            Vector3 bottomRight = new Vector3(AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);

            // Parameters
            float scale = RenderSystem.const_hexagonalScale;
            float hexagonWidth = RenderSystem.const_hexagonalBorderWidth;
            float halfHexagonWidth = hexagonWidth / 2f;

            // Hexagon Vertex Coordinates
            Vector3 topLeftI = topLeft * (scale - halfHexagonWidth);
            Vector3 topLeftO = topLeft * (scale + halfHexagonWidth);
            Vector3 topI = top * (scale - halfHexagonWidth);
            Vector3 topO = top * (scale + halfHexagonWidth);
            Vector3 topRightI = topRight * (scale - halfHexagonWidth);
            Vector3 topRightO = topRight * (scale + halfHexagonWidth);
            Vector3 bottomRightI = bottomRight * (scale - halfHexagonWidth);
            Vector3 bottomRightO = bottomRight * (scale + halfHexagonWidth);
            Vector3 bottomI = bottom * (scale - halfHexagonWidth);
            Vector3 bottomO = bottom * (scale + halfHexagonWidth);
            Vector3 bottomLeftI = bottomLeft * (scale - halfHexagonWidth);
            Vector3 bottomLeftO = bottomLeft * (scale + halfHexagonWidth);


            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[6];
            Vector2[] uv = new Vector2[vertices.Length];
            Vector2[] uv2 = new Vector2[vertices.Length];
            Vector4[] uv3 = new Vector4[vertices.Length];
            Vector4[] uv4 = new Vector4[vertices.Length];
            Vector3[] normals = new Vector3[vertices.Length];

            // Vertices _____
            // Rectangle
            vertices[0] = new Vector3(0f, bottomLeftO.y, 0f);
            vertices[1] = new Vector3(1f, bottomLeftO.y, 0f);
            vertices[2] = new Vector3(0f, topLeftO.y, 0f);
            vertices[3] = new Vector3(1f, topLeftO.y, 0f);

            // Triangles _____
            // Background
            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 1;
            triangles[4] = 2;
            triangles[5] = 3;

            // UV0s _____
            uv[0] = new Vector2(0f, 0f);
            uv[1] = new Vector2(1f, 0f);
            uv[2] = new Vector2(0f, 1f);
            uv[3] = new Vector2(1f, 1f);

            // UV1s _____
            //todo // set the rest correctly
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    uv2[i * 4 + j] = new Vector2(i, 0);
                }
            }
            // Corner Triangles
            uv2[24] = new Vector2(6f, 0f);
            uv2[25] = new Vector2(6f, 0f);
            uv2[26] = new Vector2(6f, 0f);
            uv2[27] = new Vector2(6f, 0f);
            uv2[28] = new Vector2(6f, 0f);
            uv2[29] = new Vector2(6f, 0f);
            // Background (y value defines vertex number)
            uv2[30] = new Vector2(7f, 0f);
            uv2[31] = new Vector2(7f, 1f);
            uv2[32] = new Vector2(7f, 2f);
            uv2[33] = new Vector2(7f, 3f);
            uv2[34] = new Vector2(7f, 4f);
            uv2[35] = new Vector2(7f, 5f);

            // UV2s _____
            // Left
            uv3[0] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftO - bottomLeftO);
            uv3[1] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftI - bottomLeftI);
            uv3[2] = Library.VectorConstants.Combine2Vector2s(bottomLeftO - topLeftO, Vector2.zero);
            uv3[3] = Library.VectorConstants.Combine2Vector2s(bottomLeftI - topLeftI, Vector2.zero);
            // Top Left
            uv3[4] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topO - topLeftO);
            uv3[5] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topI - topLeftI);
            uv3[6] = Library.VectorConstants.Combine2Vector2s(topLeftO - topO, Vector2.zero);
            uv3[7] = Library.VectorConstants.Combine2Vector2s(topLeftI - topI, Vector2.zero);
            // Top Right
            uv3[8] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightO - topO);
            uv3[9] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightI - topI);
            uv3[10] = Library.VectorConstants.Combine2Vector2s(topO - topRightO, Vector2.zero);
            uv3[11] = Library.VectorConstants.Combine2Vector2s(topI - topRightI, Vector2.zero);
            // Right
            uv3[12] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightO - topRightO);
            uv3[13] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightI - topRightI);
            uv3[14] = Library.VectorConstants.Combine2Vector2s(topRightO - bottomRightO, Vector2.zero);
            uv3[15] = Library.VectorConstants.Combine2Vector2s(topRightI - bottomRightI, Vector2.zero);
            // Bottom Right
            uv3[16] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomO - bottomRightO);
            uv3[17] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomI - bottomRightI);
            uv3[18] = Library.VectorConstants.Combine2Vector2s(bottomRightO - bottomO, Vector2.zero);
            uv3[19] = Library.VectorConstants.Combine2Vector2s(bottomRightI - bottomI, Vector2.zero);
            // Bottom Left
            uv3[20] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftO - bottomO);
            uv3[21] = Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftI - bottomI);
            uv3[22] = Library.VectorConstants.Combine2Vector2s(bottomO - bottomLeftO, Vector2.zero);
            uv3[23] = Library.VectorConstants.Combine2Vector2s(bottomI - bottomLeftI, Vector2.zero);
            // Corner Triangles
            uv3[24] = Vector4.zero;
            uv3[25] = Vector4.zero;
            uv3[26] = Vector4.zero;
            uv3[27] = Vector4.zero;
            uv3[28] = Vector4.zero;
            uv3[29] = Vector4.zero;
            // Background (here the clockwise contraction offsets are defined)
            Vector2 conVector1cl = topLeft - bottomLeft;
            Vector2 conVector2cl = top - topLeft;
            uv3[30] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 0f), Library.VectorConstants.Rotate(conVector2cl, 0f));
            uv3[31] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 300f), Library.VectorConstants.Rotate(conVector2cl, 300f));
            uv3[32] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 240f), Library.VectorConstants.Rotate(conVector2cl, 240f));
            uv3[33] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 180f), Library.VectorConstants.Rotate(conVector2cl, 180f));
            uv3[34] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 120f), Library.VectorConstants.Rotate(conVector2cl, 120f));
            uv3[35] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cl, 60f), Library.VectorConstants.Rotate(conVector2cl, 60f));

            // UV3s _____
            // Left (Left to Right)
            Vector2 expVector1bot = bottomO - bottomLeftO;
            Vector2 expVector2bot = (bottomRight - bottomLeft) - (bottomO - bottomLeftO);
            Vector2 expVector1top = topO - topLeftO;
            Vector2 expVector2top = (topRight - topLeft) - (topO - topLeftO);
            uv4[0] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 0f), Library.VectorConstants.Rotate(expVector2bot, 0f));
            uv4[1] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 0f), Library.VectorConstants.Rotate(expVector2bot, 0f));
            uv4[2] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 0f), Library.VectorConstants.Rotate(expVector2top, 0f));
            uv4[3] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 0f), Library.VectorConstants.Rotate(expVector2top, 0f));
            // Top Left
            uv4[4] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 300f), Library.VectorConstants.Rotate(expVector2bot, 300f));
            uv4[5] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 300f), Library.VectorConstants.Rotate(expVector2bot, 300f));
            uv4[6] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 300f), Library.VectorConstants.Rotate(expVector2top, 300f));
            uv4[7] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 300f), Library.VectorConstants.Rotate(expVector2top, 300f));
            // Top Right
            uv4[8] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 240f), Library.VectorConstants.Rotate(expVector2bot, 240f));
            uv4[9] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 240f), Library.VectorConstants.Rotate(expVector2bot, 240f));
            uv4[10] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 240f), Library.VectorConstants.Rotate(expVector2top, 240f));
            uv4[11] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 240f), Library.VectorConstants.Rotate(expVector2top, 240f));
            // Right (Right to Left)
            uv4[12] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 180f), Library.VectorConstants.Rotate(expVector2bot, 180f));
            uv4[13] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 180f), Library.VectorConstants.Rotate(expVector2bot, 180f));
            uv4[14] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 180f), Library.VectorConstants.Rotate(expVector2top, 180f));
            uv4[15] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 180f), Library.VectorConstants.Rotate(expVector2top, 180f));
            // Bottom Right
            uv4[16] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 120f), Library.VectorConstants.Rotate(expVector2bot, 120f));
            uv4[17] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 120f), Library.VectorConstants.Rotate(expVector2bot, 120f));
            uv4[18] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 120f), Library.VectorConstants.Rotate(expVector2top, 120f));
            uv4[19] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 120f), Library.VectorConstants.Rotate(expVector2top, 120f));
            // Bottom Left
            uv4[20] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 60f), Library.VectorConstants.Rotate(expVector2bot, 60f));
            uv4[21] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1bot, 60f), Library.VectorConstants.Rotate(expVector2bot, 60f));
            uv4[22] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 60f), Library.VectorConstants.Rotate(expVector2top, 60f));
            uv4[23] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(expVector1top, 60f), Library.VectorConstants.Rotate(expVector2top, 60f));
            // Corner Triangles
            uv4[24] = Vector4.zero;
            uv4[25] = Vector4.zero;
            uv4[26] = Vector4.zero;
            uv4[27] = Vector4.zero;
            uv4[28] = Vector4.zero;
            uv4[29] = Vector4.zero;
            // Background (here the counterclockwise contraction offsets are defined)
            Vector2 conVector1cc = bottom - bottomLeft;
            Vector2 conVector2cc = bottomRight - bottom;
            uv4[30] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 0f), Library.VectorConstants.Rotate(conVector2cc, 0f));
            uv4[31] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 300f), Library.VectorConstants.Rotate(conVector2cc, 300f));
            uv4[32] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 240f), Library.VectorConstants.Rotate(conVector2cc, 240f));
            uv4[33] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 180f), Library.VectorConstants.Rotate(conVector2cc, 180f));
            uv4[34] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 120f), Library.VectorConstants.Rotate(conVector2cc, 120f));
            uv4[35] = Library.VectorConstants.Combine2Vector2s(Library.VectorConstants.Rotate(conVector1cc, 60f), Library.VectorConstants.Rotate(conVector2cc, 60f));

            // Normals _____
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.uv2 = uv2;
            mesh.SetUVs(2, uv3);
            mesh.SetUVs(3, uv4);
            mesh.normals = normals;

            return mesh;
        }

        /// <summary>
        /// The base mesh for the rendering of the hexagonal particles
        /// (Quad that is scaled and offset so that the pivot is at one grid position).
        /// </summary>
        /// <returns>A quad mesh large enough to contain an expanded hexagonal
        /// particle if its origin is placed on a grid node.</returns>
        public static Mesh GetMesh_MergingExpansionHexagon()
        {
            Mesh mesh = Library.MeshConstants.getDefaultMeshQuad(1f, 0f, new Vector2(0.5f, 0.5f));
            mesh = Library.MeshConstants.scaleMeshAroundPivot(mesh, new Vector3(3f, 2f, 1f));
            mesh = Library.MeshConstants.offsetMeshVertices(mesh, new Vector3(0.5f, 0f, 0f));
            return mesh;
        }

        /// <summary>
        /// A mesh that is a line of hexagons which form a grid.
        /// Put multiple next to each other to form the background for the hexagonal views.
        /// Every second row must be offset by the grid vector <c>(0, 1)</c>.
        /// </summary>
        /// <returns>A <c>UnityEngine.Mesh</c> containing a line of
        /// <see cref="RenderSystem.const_hexagonalBGHexLineAmount"/> hexagons
        /// for the hexagonal background grid.</returns>
        public static Mesh GetMesh_HexagonGridLine()
        {
            Mesh mesh = new Mesh();

            // Parameters
            float scale = RenderSystem.const_hexagonalScale;
            float hexagonWidth = RenderSystem.const_hexagonalBorderWidth;
            float halfHexagonWidth = hexagonWidth / 2f;
            int hexAmount = RenderSystem.const_hexagonalBGHexLineAmount;

            Vector3[] vertices = new Vector3[hexAmount * 4 * 6];
            int[] triangles = new int[hexAmount * 6 * 6];
            Vector2[] uv = new Vector2[hexAmount * 4 * 6];
            Vector3[] normals = new Vector3[hexAmount * 4 * 6];

            for (int i = 0; i < hexAmount; i++)
            {
                // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
                Vector3 topLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
                Vector3 top = new Vector3(0f, AmoebotFunctions.hexRadiusMajor, 0f);
                Vector3 topRight = new Vector3(AmoebotFunctions.hexRadiusMinor, AmoebotFunctions.hexRadiusMajor2, 0f);
                Vector3 bottomLeft = new Vector3(-AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);
                Vector3 bottom = new Vector3(0f, -AmoebotFunctions.hexRadiusMajor, 0f);
                Vector3 bottomRight = new Vector3(AmoebotFunctions.hexRadiusMinor, -AmoebotFunctions.hexRadiusMajor2, 0f);

                // Displacement
                Vector3 displacement = new Vector3(i, 0f, 0f);

                // Hexagon Vertex Coordinates
                Vector3 topLeftI = topLeft * (scale - halfHexagonWidth) + displacement;
                Vector3 topLeftO = topLeft * (scale + halfHexagonWidth) + displacement;
                Vector3 topI = top * (scale - halfHexagonWidth) + displacement;
                Vector3 topO = top * (scale + halfHexagonWidth) + displacement;
                Vector3 topRightI = topRight * (scale - halfHexagonWidth) + displacement;
                Vector3 topRightO = topRight * (scale + halfHexagonWidth) + displacement;
                Vector3 bottomRightI = bottomRight * (scale - halfHexagonWidth) + displacement;
                Vector3 bottomRightO = bottomRight * (scale + halfHexagonWidth) + displacement;
                Vector3 bottomI = bottom * (scale - halfHexagonWidth) + displacement;
                Vector3 bottomO = bottom * (scale + halfHexagonWidth) + displacement;
                Vector3 bottomLeftI = bottomLeft * (scale - halfHexagonWidth) + displacement;
                Vector3 bottomLeftO = bottomLeft * (scale + halfHexagonWidth) + displacement;

                // Vertices _____
                // Left
                vertices[0 + i * 24] = bottomLeftO;
                vertices[1 + i * 24] = bottomLeftI;
                vertices[2 + i * 24] = topLeftO;
                vertices[3 + i * 24] = topLeftI;
                // Top Left
                vertices[4 + i * 24] = topLeftO;
                vertices[5 + i * 24] = topLeftI;
                vertices[6 + i * 24] = topO;
                vertices[7 + i * 24] = topI;
                // Top Right
                vertices[8 + i * 24] = topO;
                vertices[9 + i * 24] = topI;
                vertices[10 + i * 24] = topRightO;
                vertices[11 + i * 24] = topRightI;
                // Right
                vertices[12 + i * 24] = topRightO;
                vertices[13 + i * 24] = topRightI;
                vertices[14 + i * 24] = bottomRightO;
                vertices[15 + i * 24] = bottomRightI;
                // Bottom Right
                vertices[16 + i * 24] = bottomRightO;
                vertices[17 + i * 24] = bottomRightI;
                vertices[18 + i * 24] = bottomO;
                vertices[19 + i * 24] = bottomI;
                // Bottom Left
                vertices[20 + i * 24] = bottomO;
                vertices[21 + i * 24] = bottomI;
                vertices[22 + i * 24] = bottomLeftO;
                vertices[23 + i * 24] = bottomLeftI;

                // Triangles _____
                for (int j = 0; j < 6; j++)
                {
                    triangles[i * 36 + j * 6 + 0] = i * 24 + j * 4 + 0;
                    triangles[i * 36 + j * 6 + 1] = i * 24 + j * 4 + 2;
                    triangles[i * 36 + j * 6 + 2] = i * 24 + j * 4 + 1;

                    triangles[i * 36 + j * 6 + 3] = i * 24 + j * 4 + 1;
                    triangles[i * 36 + j * 6 + 4] = i * 24 + j * 4 + 2;
                    triangles[i * 36 + j * 6 + 5] = i * 24 + j * 4 + 3;
                }

                // UV0s _____
                for (int j = 0; j < 6 * hexAmount; j++)
                {
                    uv[j * 4 + 0] = new Vector2(0f, 0f);
                    uv[j * 4 + 1] = new Vector2(1f, 0f);
                    uv[j * 4 + 2] = new Vector2(0f, 1f);
                    uv[j * 4 + 3] = new Vector2(1f, 1f);
                }

                // Normals _____
                for (int j = 0; j < 4 * 6 * hexAmount; j++)
                {
                    normals[j] = Vector3.up;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.normals = normals;

            return mesh;
        }

    }

}
