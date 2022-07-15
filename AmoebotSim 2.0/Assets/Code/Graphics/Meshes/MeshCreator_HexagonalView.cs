using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCreator_HexagonalView
{

    public static Mesh GetMesh_BaseHexagonBackground()
    {
        Mesh mesh = new Mesh();

        // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
        Vector3 topLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 top = new Vector3(0f, AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 topRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottomLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottom = new Vector3(0f, -AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 bottomRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);

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
    /// Creates a mesh for the base hexagon.
    /// </summary>
    /// <returns></returns>
    public static Mesh GetMesh_BaseHexagon()
    {
        /**
         * UV0 - Base
           UV1.x = Expansion Mesh
           UV2.xy = Contraction Offset Clockwise
           UV3.xy = Contraction Offset Counterclockwise
         */

        Mesh mesh = new Mesh();

        // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
        Vector3 topLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 top = new Vector3(0f, AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 topRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottomLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottom = new Vector3(0f, -AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 bottomRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);

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


        Vector3[] vertices = new Vector3[4 * 6];
        int[] triangles = new int[6 * 6];
        Vector2[] uv = new Vector2[4 * 6];
        Vector2[] uv2 = new Vector2[4 * 6];
        Vector2[] uv3 = new Vector2[4 * 6];
        Vector2[] uv4 = new Vector2[4 * 6];
        Vector3[] normals = new Vector3[4 * 6];

        Vector4[] uv3V4 = new Vector4[4 * 6];
        Vector4[] uv4V4 = new Vector4[4 * 6];

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

        // Triangles _____
        for (int i = 0; i < 6; i++)
        {
            triangles[i * 6 + 0] = i * 4 + 0;
            triangles[i * 6 + 1] = i * 4 + 2;
            triangles[i * 6 + 2] = i * 4 + 1;

            triangles[i * 6 + 3] = i * 4 + 1;
            triangles[i * 6 + 4] = i * 4 + 2;
            triangles[i * 6 + 5] = i * 4 + 3;
        }

        // UV0s _____
        for (int i = 0; i < 6; i++)
        {
            uv[i * 4 + 0] = new Vector2(0f, 0f);
            uv[i * 4 + 1] = new Vector2(1f, 0f);
            uv[i * 4 + 2] = new Vector2(0f, 1f);
            uv[i * 4 + 3] = new Vector2(1f, 1f);
        }

        // UV1s _____
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                uv2[i * 4 + j] = new Vector2(i, 0);
            }
        }

        // UV2s _____
        // Left
        uv3[0] = Vector2.zero;
        uv3[1] = Vector2.zero;
        uv3[2] = bottomLeftO - topLeftO;
        uv3[3] = bottomLeftI - topLeftI;
        // Top Left
        uv3[4] = Vector2.zero;
        uv3[5] = Vector2.zero;
        uv3[6] = topLeftO - topO;
        uv3[7] = topLeftI - topI;
        // Top Right
        uv3[8] = Vector2.zero;
        uv3[9] = Vector2.zero;
        uv3[10] = topO - topRightO;
        uv3[11] = topI - topRightI;
        // Right
        uv3[12] = Vector2.zero;
        uv3[13] = Vector2.zero;
        uv3[14] = topRightO - bottomRightO;
        uv3[15] = topRightI - bottomRightI;
        // Bottom Right
        uv3[16] = Vector2.zero;
        uv3[17] = Vector2.zero;
        uv3[18] = bottomRightO - bottomO;
        uv3[19] = bottomRightI - bottomI;
        // Bottom Left
        uv3[20] = Vector2.zero;
        uv3[21] = Vector2.zero;
        uv3[22] = bottomO - bottomLeftO;
        uv3[23] = bottomI - bottomLeftI;

        // UV3s _____
        // Left
        uv4[0] = topLeftO - bottomLeftO;
        uv4[1] = topLeftI - bottomLeftI;
        uv4[2] = Vector2.zero;
        uv4[3] = Vector2.zero;
        // Top Left
        uv4[4] = topO - topLeftO;
        uv4[5] = topI - topLeftI;
        uv4[6] = Vector2.zero;
        uv4[7] = Vector2.zero;
        // Top Right
        uv4[8] = topRightO - topO;
        uv4[9] = topRightI - topI;
        uv4[10] = Vector2.zero;
        uv4[11] = Vector2.zero;
        // Right
        uv4[12] = bottomRightO - topRightO;
        uv4[13] = bottomRightI - topRightI;
        uv4[14] = Vector2.zero;
        uv4[15] = Vector2.zero;
        // Bottom Right
        uv4[16] = bottomO - bottomRightO;
        uv4[17] = bottomI - bottomRightI;
        uv4[18] = Vector2.zero;
        uv4[19] = Vector2.zero;
        // Bottom Left
        uv4[20] = bottomLeftO - bottomO;
        uv4[21] = bottomLeftI - bottomI;
        uv4[22] = Vector2.zero;
        uv4[23] = Vector2.zero;

        // UV3 Combined
        for (int i = 0; i < 24; i++)
        {
            uv3V4[i] = new Vector4(uv3[i].x, uv3[i].y, uv4[i].x, uv4[i].y);
        }

        // UV4
        //for (int i = 0; i < 6; i++)
        //{
        //    int l1 = (i + 6 - 1) % 6;
        //    int l2 = (i + 6 - 2) % 6;
        //    int r1 = (i + 1) % 6;
        //    int r2 = (i + 2) % 6;

        //    // x,y: ExpansionVector1, z,a: ExpansionVector2

        //    // DownO
        //    // -1
        //    int index = l1 * 4;
        //    Vector2 expVec1 = new Vector2(vertices[i * 4 + 0].x - vertices[index + 0].x, vertices[i * 4 + 0].y - vertices[index + 0].y);
        //    // -2
        //    index = l2 * 4;
        //    Vector2 expVec2 = new Vector2(vertices[i * 4 + 0].x - vertices[index + 0].x, vertices[i * 4 + 0].y - vertices[index + 0].y);
        //    // UV
        //    uv4V4[i * 4 + 0] = new Vector4(expVec1.x, expVec1.y, expVec2.x, expVec2.y);

        //    // DownI
        //    // -1
        //    // -2
        //    // UV
        //    uv4V4[i * 4 + 1] = new Vector4(expVec1.x, expVec1.y, expVec2.x, expVec2.y);

        //    // UpO
        //    // -1
        //    // -2
        //    // UV
        //    uv4V4[i * 4 + 2] = new Vector4(expVec1.x, expVec1.y, expVec2.x, expVec2.y);

        //    // UpI
        //    // -1
        //    // -2
        //    // UV
        //    uv4V4[i * 4 + 3] = new Vector4(expVec1.x, expVec1.y, expVec2.x, expVec2.y);
        //}

        // Normals _____
        for (int i = 0; i < 4 * 6; i++)
        {
            normals[i] = Vector3.up;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.uv2 = uv2;
        //mesh.uv3 = uv3;
        //mesh.uv4 = uv4;
        mesh.SetUVs(2, uv3V4);
        mesh.normals = normals;

        return mesh;
    }

    /// <summary>
    /// Creates a mesh for the base expansion hexagon (for use with the newest HexagonExpansionMaterial).
    /// </summary>
    /// <returns></returns>
    public static Mesh GetMesh_BaseExpansionHexagon()
    {
        Mesh mesh = new Mesh();

        // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
        Vector3 topLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 top = new Vector3(0f, AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 topRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottomLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottom = new Vector3(0f, -AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 bottomRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);

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
        uv3[0] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftO - bottomLeftO);
        uv3[1] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftI - bottomLeftI);
        uv3[2] = Engine.Library.VectorConstants.Combine2Vector2s(bottomLeftO - topLeftO, Vector2.zero);
        uv3[3] = Engine.Library.VectorConstants.Combine2Vector2s(bottomLeftI - topLeftI, Vector2.zero);
        // Top Left
        uv3[4] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topO - topLeftO);
        uv3[5] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topI - topLeftI);
        uv3[6] = Engine.Library.VectorConstants.Combine2Vector2s(topLeftO - topO, Vector2.zero);
        uv3[7] = Engine.Library.VectorConstants.Combine2Vector2s(topLeftI - topI, Vector2.zero);
        // Top Right
        uv3[8] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightO - topO);
        uv3[9] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightI - topI);
        uv3[10] = Engine.Library.VectorConstants.Combine2Vector2s(topO - topRightO, Vector2.zero);
        uv3[11] = Engine.Library.VectorConstants.Combine2Vector2s(topI - topRightI, Vector2.zero);
        // Right
        uv3[12] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightO - topRightO);
        uv3[13] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightI - topRightI);
        uv3[14] = Engine.Library.VectorConstants.Combine2Vector2s(topRightO - bottomRightO, Vector2.zero);
        uv3[15] = Engine.Library.VectorConstants.Combine2Vector2s(topRightI - bottomRightI, Vector2.zero);
        // Bottom Right
        uv3[16] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomO - bottomRightO);
        uv3[17] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomI - bottomRightI);
        uv3[18] = Engine.Library.VectorConstants.Combine2Vector2s(bottomRightO - bottomO, Vector2.zero);
        uv3[19] = Engine.Library.VectorConstants.Combine2Vector2s(bottomRightI - bottomI, Vector2.zero);
        // Bottom Left
        uv3[20] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftO - bottomO);
        uv3[21] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftI - bottomI);
        uv3[22] = Engine.Library.VectorConstants.Combine2Vector2s(bottomO - bottomLeftO, Vector2.zero);
        uv3[23] = Engine.Library.VectorConstants.Combine2Vector2s(bottomI - bottomLeftI, Vector2.zero);
        // Corner Triangles
        uv3[24] = Vector4.zero;
        uv3[25] = Vector4.zero;
        uv3[26] = Vector4.zero;
        uv3[27] = Vector4.zero;
        uv3[28] = Vector4.zero;
        uv3[29] = Vector4.zero;
        // Background (here the clockwise contraction offsets are defined)
        Vector2 conVector1cl =  topLeft - bottomLeft;
        Vector2 conVector2cl = top - topLeft;
        uv3[30] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 0f), Engine.Library.VectorConstants.Rotate(conVector2cl, 0f));
        uv3[31] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 300f), Engine.Library.VectorConstants.Rotate(conVector2cl, 300f));
        uv3[32] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 240f), Engine.Library.VectorConstants.Rotate(conVector2cl, 240f));
        uv3[33] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 180f), Engine.Library.VectorConstants.Rotate(conVector2cl, 180f));
        uv3[34] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 120f), Engine.Library.VectorConstants.Rotate(conVector2cl, 120f));
        uv3[35] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 60f), Engine.Library.VectorConstants.Rotate(conVector2cl, 60f));

        // UV3s _____
        // Left (Left to Right)
        Vector2 expVector1bot = bottomO - bottomLeftO;
        Vector2 expVector2bot = (bottomRight - bottomLeft) - (bottomO - bottomLeftO);
        Vector2 expVector1top = topO - topLeftO;
        Vector2 expVector2top = (topRight - topLeft) - (topO - topLeftO);
        uv4[0] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 0f), Engine.Library.VectorConstants.Rotate(expVector2bot, 0f));
        uv4[1] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 0f), Engine.Library.VectorConstants.Rotate(expVector2bot, 0f));
        uv4[2] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 0f), Engine.Library.VectorConstants.Rotate(expVector2top, 0f));
        uv4[3] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 0f), Engine.Library.VectorConstants.Rotate(expVector2top, 0f));
        // Top Left
        uv4[4] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 300f), Engine.Library.VectorConstants.Rotate(expVector2bot, 300f));
        uv4[5] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 300f), Engine.Library.VectorConstants.Rotate(expVector2bot, 300f));
        uv4[6] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 300f), Engine.Library.VectorConstants.Rotate(expVector2top, 300f));
        uv4[7] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 300f), Engine.Library.VectorConstants.Rotate(expVector2top, 300f));
        // Top Right
        uv4[8] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 240f), Engine.Library.VectorConstants.Rotate(expVector2bot, 240f));
        uv4[9] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 240f), Engine.Library.VectorConstants.Rotate(expVector2bot, 240f));
        uv4[10] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 240f), Engine.Library.VectorConstants.Rotate(expVector2top, 240f));
        uv4[11] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 240f), Engine.Library.VectorConstants.Rotate(expVector2top, 240f));
        // Right (Right to Left)
        uv4[12] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 180f), Engine.Library.VectorConstants.Rotate(expVector2bot, 180f));
        uv4[13] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 180f), Engine.Library.VectorConstants.Rotate(expVector2bot, 180f));
        uv4[14] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 180f), Engine.Library.VectorConstants.Rotate(expVector2top, 180f));
        uv4[15] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 180f), Engine.Library.VectorConstants.Rotate(expVector2top, 180f));
        // Bottom Right
        uv4[16] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 120f), Engine.Library.VectorConstants.Rotate(expVector2bot, 120f));
        uv4[17] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 120f), Engine.Library.VectorConstants.Rotate(expVector2bot, 120f));
        uv4[18] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 120f), Engine.Library.VectorConstants.Rotate(expVector2top, 120f));
        uv4[19] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 120f), Engine.Library.VectorConstants.Rotate(expVector2top, 120f));
        // Bottom Left
        uv4[20] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 60f), Engine.Library.VectorConstants.Rotate(expVector2bot, 60f));
        uv4[21] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 60f), Engine.Library.VectorConstants.Rotate(expVector2bot, 60f));
        uv4[22] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 60f), Engine.Library.VectorConstants.Rotate(expVector2top, 60f));
        uv4[23] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 60f), Engine.Library.VectorConstants.Rotate(expVector2top, 60f));
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
        uv4[30] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 0f), Engine.Library.VectorConstants.Rotate(conVector2cc, 0f));
        uv4[31] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 300f), Engine.Library.VectorConstants.Rotate(conVector2cc, 300f));
        uv4[32] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 240f), Engine.Library.VectorConstants.Rotate(conVector2cc, 240f));
        uv4[33] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 180f), Engine.Library.VectorConstants.Rotate(conVector2cc, 180f));
        uv4[34] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 120f), Engine.Library.VectorConstants.Rotate(conVector2cc, 120f));
        uv4[35] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 60f), Engine.Library.VectorConstants.Rotate(conVector2cc, 60f));

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
    /// Creates a mesh for the base expansion hexagon (for use with the newest HexagonExpansionMaterial).
    /// </summary>
    /// <returns></returns>
    public static Mesh GetMesh_CombinedExpansionRectangle()
    {
        Mesh mesh = new Mesh();

        // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
        Vector3 topLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 top = new Vector3(0f, AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 topRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottomLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottom = new Vector3(0f, -AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 bottomRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);

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
        uv3[0] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftO - bottomLeftO);
        uv3[1] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topLeftI - bottomLeftI);
        uv3[2] = Engine.Library.VectorConstants.Combine2Vector2s(bottomLeftO - topLeftO, Vector2.zero);
        uv3[3] = Engine.Library.VectorConstants.Combine2Vector2s(bottomLeftI - topLeftI, Vector2.zero);
        // Top Left
        uv3[4] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topO - topLeftO);
        uv3[5] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topI - topLeftI);
        uv3[6] = Engine.Library.VectorConstants.Combine2Vector2s(topLeftO - topO, Vector2.zero);
        uv3[7] = Engine.Library.VectorConstants.Combine2Vector2s(topLeftI - topI, Vector2.zero);
        // Top Right
        uv3[8] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightO - topO);
        uv3[9] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, topRightI - topI);
        uv3[10] = Engine.Library.VectorConstants.Combine2Vector2s(topO - topRightO, Vector2.zero);
        uv3[11] = Engine.Library.VectorConstants.Combine2Vector2s(topI - topRightI, Vector2.zero);
        // Right
        uv3[12] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightO - topRightO);
        uv3[13] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomRightI - topRightI);
        uv3[14] = Engine.Library.VectorConstants.Combine2Vector2s(topRightO - bottomRightO, Vector2.zero);
        uv3[15] = Engine.Library.VectorConstants.Combine2Vector2s(topRightI - bottomRightI, Vector2.zero);
        // Bottom Right
        uv3[16] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomO - bottomRightO);
        uv3[17] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomI - bottomRightI);
        uv3[18] = Engine.Library.VectorConstants.Combine2Vector2s(bottomRightO - bottomO, Vector2.zero);
        uv3[19] = Engine.Library.VectorConstants.Combine2Vector2s(bottomRightI - bottomI, Vector2.zero);
        // Bottom Left
        uv3[20] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftO - bottomO);
        uv3[21] = Engine.Library.VectorConstants.Combine2Vector2s(Vector2.zero, bottomLeftI - bottomI);
        uv3[22] = Engine.Library.VectorConstants.Combine2Vector2s(bottomO - bottomLeftO, Vector2.zero);
        uv3[23] = Engine.Library.VectorConstants.Combine2Vector2s(bottomI - bottomLeftI, Vector2.zero);
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
        uv3[30] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 0f), Engine.Library.VectorConstants.Rotate(conVector2cl, 0f));
        uv3[31] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 300f), Engine.Library.VectorConstants.Rotate(conVector2cl, 300f));
        uv3[32] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 240f), Engine.Library.VectorConstants.Rotate(conVector2cl, 240f));
        uv3[33] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 180f), Engine.Library.VectorConstants.Rotate(conVector2cl, 180f));
        uv3[34] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 120f), Engine.Library.VectorConstants.Rotate(conVector2cl, 120f));
        uv3[35] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cl, 60f), Engine.Library.VectorConstants.Rotate(conVector2cl, 60f));

        // UV3s _____
        // Left (Left to Right)
        Vector2 expVector1bot = bottomO - bottomLeftO;
        Vector2 expVector2bot = (bottomRight - bottomLeft) - (bottomO - bottomLeftO);
        Vector2 expVector1top = topO - topLeftO;
        Vector2 expVector2top = (topRight - topLeft) - (topO - topLeftO);
        uv4[0] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 0f), Engine.Library.VectorConstants.Rotate(expVector2bot, 0f));
        uv4[1] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 0f), Engine.Library.VectorConstants.Rotate(expVector2bot, 0f));
        uv4[2] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 0f), Engine.Library.VectorConstants.Rotate(expVector2top, 0f));
        uv4[3] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 0f), Engine.Library.VectorConstants.Rotate(expVector2top, 0f));
        // Top Left
        uv4[4] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 300f), Engine.Library.VectorConstants.Rotate(expVector2bot, 300f));
        uv4[5] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 300f), Engine.Library.VectorConstants.Rotate(expVector2bot, 300f));
        uv4[6] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 300f), Engine.Library.VectorConstants.Rotate(expVector2top, 300f));
        uv4[7] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 300f), Engine.Library.VectorConstants.Rotate(expVector2top, 300f));
        // Top Right
        uv4[8] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 240f), Engine.Library.VectorConstants.Rotate(expVector2bot, 240f));
        uv4[9] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 240f), Engine.Library.VectorConstants.Rotate(expVector2bot, 240f));
        uv4[10] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 240f), Engine.Library.VectorConstants.Rotate(expVector2top, 240f));
        uv4[11] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 240f), Engine.Library.VectorConstants.Rotate(expVector2top, 240f));
        // Right (Right to Left)
        uv4[12] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 180f), Engine.Library.VectorConstants.Rotate(expVector2bot, 180f));
        uv4[13] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 180f), Engine.Library.VectorConstants.Rotate(expVector2bot, 180f));
        uv4[14] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 180f), Engine.Library.VectorConstants.Rotate(expVector2top, 180f));
        uv4[15] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 180f), Engine.Library.VectorConstants.Rotate(expVector2top, 180f));
        // Bottom Right
        uv4[16] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 120f), Engine.Library.VectorConstants.Rotate(expVector2bot, 120f));
        uv4[17] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 120f), Engine.Library.VectorConstants.Rotate(expVector2bot, 120f));
        uv4[18] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 120f), Engine.Library.VectorConstants.Rotate(expVector2top, 120f));
        uv4[19] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 120f), Engine.Library.VectorConstants.Rotate(expVector2top, 120f));
        // Bottom Left
        uv4[20] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 60f), Engine.Library.VectorConstants.Rotate(expVector2bot, 60f));
        uv4[21] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1bot, 60f), Engine.Library.VectorConstants.Rotate(expVector2bot, 60f));
        uv4[22] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 60f), Engine.Library.VectorConstants.Rotate(expVector2top, 60f));
        uv4[23] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(expVector1top, 60f), Engine.Library.VectorConstants.Rotate(expVector2top, 60f));
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
        uv4[30] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 0f), Engine.Library.VectorConstants.Rotate(conVector2cc, 0f));
        uv4[31] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 300f), Engine.Library.VectorConstants.Rotate(conVector2cc, 300f));
        uv4[32] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 240f), Engine.Library.VectorConstants.Rotate(conVector2cc, 240f));
        uv4[33] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 180f), Engine.Library.VectorConstants.Rotate(conVector2cc, 180f));
        uv4[34] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 120f), Engine.Library.VectorConstants.Rotate(conVector2cc, 120f));
        uv4[35] = Engine.Library.VectorConstants.Combine2Vector2s(Engine.Library.VectorConstants.Rotate(conVector1cc, 60f), Engine.Library.VectorConstants.Rotate(conVector2cc, 60f));

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

    public static Mesh GetMesh_CombinedExpansionHexagon()
    {
        // Create Hexagonal Mesh 1
        Mesh m1 = GetMesh_BaseExpansionHexagon();
        CombineInstance combineInstanceM1 = new CombineInstance();
        combineInstanceM1.mesh = m1;
        combineInstanceM1.subMeshIndex = 0;
        combineInstanceM1.transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        // Create Hexagonal Mesh 2
        Mesh m2 = GetMesh_BaseExpansionHexagon();
        CombineInstance combineInstanceM2 = new CombineInstance();
        combineInstanceM2.mesh = m2;
        combineInstanceM2.subMeshIndex = 0;
        combineInstanceM2.transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        // Create Hexagonal Mesh 3
        throw new System.NotImplementedException();
        Mesh mCenter = GetMesh_CombinedExpansionRectangle(); // todo: update this 
        CombineInstance combineInstanceMCenter = new CombineInstance();
        combineInstanceMCenter.mesh = mCenter;
        combineInstanceMCenter.subMeshIndex = 0;
        combineInstanceMCenter.transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        // Combine Meshes
        Mesh mesh = new Mesh();
        mesh.CombineMeshes(new CombineInstance[] { combineInstanceM1, combineInstanceM2, combineInstanceMCenter }, true, false, false);
    }

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
            Vector3 topLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
            Vector3 top = new Vector3(0f, AmoebotFunctions.HexVertex_YValueTop(), 0f);
            Vector3 topRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
            Vector3 bottomLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);
            Vector3 bottom = new Vector3(0f, -AmoebotFunctions.HexVertex_YValueTop(), 0f);
            Vector3 bottomRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);

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

            // TEST ZERO -------------------------------------------------------
            //// Left
            //vertices[0 + i * 24] = Vector3.zero;
            //vertices[1 + i * 24] = Vector3.zero;
            //vertices[2 + i * 24] = Vector3.zero;
            //vertices[3 + i * 24] = Vector3.zero;
            //// Top Left
            //vertices[4 + i * 24] = Vector3.zero;
            //vertices[5 + i * 24] = Vector3.zero;
            //vertices[6 + i * 24] = Vector3.zero;
            //vertices[7 + i * 24] = Vector3.zero;
            //// Top Right
            //vertices[8 + i * 24] = Vector3.zero;
            //vertices[9 + i * 24] = Vector3.zero;
            //vertices[10 + i * 24] = Vector3.zero;
            //vertices[11 + i * 24] = Vector3.zero;
            //// Right
            //vertices[12 + i * 24] = Vector3.zero;
            //vertices[13 + i * 24] = Vector3.zero;
            //vertices[14 + i * 24] = Vector3.zero;
            //vertices[15 + i * 24] = Vector3.zero;
            //// Bottom Right
            //vertices[16 + i * 24] = Vector3.zero;
            //vertices[17 + i * 24] = Vector3.zero;
            //vertices[18 + i * 24] = Vector3.zero;
            //vertices[19 + i * 24] = Vector3.zero;
            //// Bottom Left
            //vertices[20 + i * 24] = Vector3.zero;
            //vertices[21 + i * 24] = Vector3.zero;
            //vertices[22 + i * 24] = Vector3.zero;
            //vertices[23 + i * 24] = Vector3.zero;
            // -------------------------------------------------------

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
