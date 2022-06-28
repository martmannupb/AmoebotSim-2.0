using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCreator_CircularView
{

    /// <summary>
    /// Creates a mesh for the horizontal background lines.
    /// </summary>
    /// <returns></returns>
    public static Mesh GetMesh_BGLinesHorizontal()
    {
        // Mesh Data
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];
        int[] triangles = new int[6 * RenderSystem.const_amountOfLinesPerMesh];
        Vector2[] uv = new Vector2[4 * RenderSystem.const_amountOfLinesPerMesh];
        Vector3[] normals = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];

        // Variables
        float width = RenderSystem.const_circularViewBGLineWidth;
        float widthHalf = width / 2f;
        float length = RenderSystem.const_circularViewBGLineLength;

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            vertices[0 + 4 * i] = new Vector3(0f, widthHalf + i * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            vertices[1 + 4 * i] = new Vector3(0f, -widthHalf + i * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            vertices[2 + 4 * i] = new Vector3(length, widthHalf + i * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            vertices[3 + 4 * i] = new Vector3(length, -widthHalf + i * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            uv[0 + 4 * i] = new Vector2(0f, 0f);
            uv[1 + 4 * i] = new Vector2(1f, 0f);
            uv[2 + 4 * i] = new Vector2(0f, 1f);
            uv[3 + 4 * i] = new Vector2(1f, 1f);
            normals[0 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[1 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[2 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[3 + 4 * i] = new Vector3(0f, 0f, 1f);
        }

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            triangles[0 + 6 * i] = 4 * i + 0;
            triangles[1 + 6 * i] = 4 * i + 2;
            triangles[2 + 6 * i] = 4 * i + 1;
            triangles[3 + 6 * i] = 4 * i + 1;
            triangles[4 + 6 * i] = 4 * i + 2;
            triangles[5 + 6 * i] = 4 * i + 3;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.SetNormals(normals);

        return mesh;
    }

    /// <summary>
    /// Creates a mesh for the diagonal background lines from bottom right to top left.
    /// </summary>
    /// <returns></returns>
    public static Mesh GetMesh_BGLinesTopLeftBottomRight()
    {
        // Mesh Data
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];
        int[] triangles = new int[6 * RenderSystem.const_amountOfLinesPerMesh];
        Vector2[] uv = new Vector2[4 * RenderSystem.const_amountOfLinesPerMesh];
        Vector3[] normals = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];

        // Variables
        float width = RenderSystem.const_circularViewBGLineWidth;
        float widthHalf = width / 2f;
        //float length = RenderSystem.const_circularViewBGLineLength;
        float dist = width / Mathf.Sin(60 * Mathf.Deg2Rad);
        float distHalf = dist / 2f;

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            vertices[0 + 4 * i] = new Vector3(-distHalf + i, 0f, 0f);
            vertices[1 + 4 * i] = new Vector3(distHalf + i, 0f, 0f);
            vertices[2 + 4 * i] = new Vector3(-distHalf - RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            vertices[3 + 4 * i] = new Vector3(distHalf - RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            uv[0 + 4 * i] = new Vector2(0f, 0f);
            uv[1 + 4 * i] = new Vector2(1f, 0f);
            uv[2 + 4 * i] = new Vector2(0f, 1f);
            uv[3 + 4 * i] = new Vector2(1f, 1f);
            normals[0 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[1 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[2 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[3 + 4 * i] = new Vector3(0f, 0f, 1f);
        }

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            triangles[0 + 6 * i] = 4 * i + 0;
            triangles[1 + 6 * i] = 4 * i + 2;
            triangles[2 + 6 * i] = 4 * i + 1;
            triangles[3 + 6 * i] = 4 * i + 1;
            triangles[4 + 6 * i] = 4 * i + 2;
            triangles[5 + 6 * i] = 4 * i + 3;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.SetNormals(normals);

        return mesh;
    }

    /// <summary>
    /// Creates a mesh for the diagonal background lines from bottom left to top right.
    /// </summary>
    /// <returns></returns>
    public static Mesh GetMesh_BGLinesTopRightBottomLeft()
    {
        // Mesh Data
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];
        int[] triangles = new int[6 * RenderSystem.const_amountOfLinesPerMesh];
        Vector2[] uv = new Vector2[4 * RenderSystem.const_amountOfLinesPerMesh];
        Vector3[] normals = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];

        // Variables
        float width = RenderSystem.const_circularViewBGLineWidth;
        float widthHalf = width / 2f;
        //float length = RenderSystem.const_circularViewBGLineLength;
        float dist = width / Mathf.Sin(60 * Mathf.Deg2Rad);
        float distHalf = dist / 2f;

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            vertices[0 + 4 * i] = new Vector3(-distHalf + i, 0f, 0f);
            vertices[1 + 4 * i] = new Vector3(distHalf + i, 0f, 0f);
            vertices[2 + 4 * i] = new Vector3(-distHalf + RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            vertices[3 + 4 * i] = new Vector3(distHalf + RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.HeightDifferenceBetweenRows(), 0f);
            uv[0 + 4 * i] = new Vector2(0f, 0f);
            uv[1 + 4 * i] = new Vector2(1f, 0f);
            uv[2 + 4 * i] = new Vector2(0f, 1f);
            uv[3 + 4 * i] = new Vector2(1f, 1f);
            normals[0 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[1 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[2 + 4 * i] = new Vector3(0f, 0f, 1f);
            normals[3 + 4 * i] = new Vector3(0f, 0f, 1f);
        }

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            triangles[0 + 6 * i] = 4 * i + 0;
            triangles[1 + 6 * i] = 4 * i + 2;
            triangles[2 + 6 * i] = 4 * i + 1;
            triangles[3 + 6 * i] = 4 * i + 1;
            triangles[4 + 6 * i] = 4 * i + 2;
            triangles[5 + 6 * i] = 4 * i + 3;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);
        mesh.SetNormals(normals);

        return mesh;
    }

}
