using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonalExpansionPrototype : MonoBehaviour
{
    public float hexScale = 1f;
    public float hexBorderWidth = 0.1f;

    private Mesh mesh;
    public Material hexFullMat;
    public Material hexExpMat;

    // Start is called before the first frame update
    void Start()
    {
        CreateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, 1.1547005f, 1f)), hexExpMat, 0);
        Graphics.DrawMesh(mesh, Matrix4x4.TRS(new Vector3(-1f, 0f, 0f), Quaternion.identity, new Vector3(1f, 1.1547005f, 1f)), hexFullMat, 0);
    }

    private void CreateMesh()
    {
        /**
         * UV0 - Base
           UV1.x = Expansion Mesh
           UV2.xy = Contraction Offset Clockwise
           UV3.xy = Contraction Offset Counterclockwise
         */

        mesh = new Mesh();

        // Hexagon Coordinates (we scale these for the vertices of the 6 quads)
        Vector3 topLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 top = new Vector3(0f, AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 topRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottomLeft = new Vector3(-AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);
        Vector3 bottom = new Vector3(0f, -AmoebotFunctions.HexVertex_YValueTop(), 0f);
        Vector3 bottomRight = new Vector3(AmoebotFunctions.HexVertex_XValue(), -AmoebotFunctions.HexVertex_YValueSides(), 0f);

        // Parameters
        float scale = hexScale;
        float hexagonWidth = hexBorderWidth;
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


        Vector3[] vertices = new Vector3[4 * 6 + 3 * 2];
        int[] triangles = new int[6 * 6 + 2 * 3];
        Vector2[] uv = new Vector2[vertices.Length];
        Vector2[] uv2 = new Vector2[vertices.Length];
        Vector2[] uv3 = new Vector2[vertices.Length];
        Vector2[] uv4 = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];

        Vector4[] uv3V4 = new Vector4[vertices.Length];
        Vector4[] uv4V4 = new Vector4[vertices.Length];

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
        triangles[36] = 24;
        triangles[37] = 25;
        triangles[38] = 26;

        triangles[39] = 27;
        triangles[40] = 28;
        triangles[41] = 29;

        // UV0s _____
        for (int i = 0; i < 6; i++)
        {
            uv[i * 4 + 0] = 
            uv[i * 4 + 1] = new Vector2(1f, 0f);
            uv[i * 4 + 2] = new Vector2(0f, 1f);
            uv[i * 4 + 3] = new Vector2(1f, 1f);
        }
        uv[24] = new Vector2(0f, 0.5f);
        uv[25] = new Vector2(1f, 1f);
        uv[26] = new Vector2(1f, 0f);

        uv[27] = new Vector2(0f, 0.5f);
        uv[28] = new Vector2(1f, 1f);
        uv[29] = new Vector2(1f, 0f);

        // UV1s _____
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                uv2[i * 4 + j] = new Vector2(i, 0);
            }
        }
        uv2[24] = new Vector2(6f, 0f);
        uv2[25] = new Vector2(6f, 0f);
        uv2[26] = new Vector2(6f, 0f);
        uv2[27] = new Vector2(6f, 0f);
        uv2[28] = new Vector2(6f, 0f);
        uv2[29] = new Vector2(6f, 0f);

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

        Debug.Log(mesh.vertices);

        // MatrialPropertyBlock _____
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
    }

    // Time.timeSinceLevelLoad
}
