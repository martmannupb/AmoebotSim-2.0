using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Creates the meshes for the circular view.
    /// </summary>
    public static class MeshCreator_CircularView
    {

        /// <summary>
        /// Creates a mesh for the horizontal background lines.
        /// </summary>
        /// <returns>A <see cref="UnityEngine.Mesh"/> containing
        /// <see cref="RenderSystem.const_amountOfLinesPerMesh"/> horizontal
        /// lines for the background grid.</returns>
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
                vertices[0 + 4 * i] = new Vector3(0f, widthHalf + i * AmoebotFunctions.rowDistVert, 0f);
                vertices[1 + 4 * i] = new Vector3(0f, -widthHalf + i * AmoebotFunctions.rowDistVert, 0f);
                vertices[2 + 4 * i] = new Vector3(length, widthHalf + i * AmoebotFunctions.rowDistVert, 0f);
                vertices[3 + 4 * i] = new Vector3(length, -widthHalf + i * AmoebotFunctions.rowDistVert, 0f);
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
        /// <returns>A <see cref="UnityEngine.Mesh"/> containing
        /// <see cref="RenderSystem.const_amountOfLinesPerMesh"/> diagonal lines from
        /// the top left to the bottom right for the background grid.</returns>
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
            float dist = width / Mathf.Sin(60 * Mathf.Deg2Rad);
            float distHalf = dist / 2f;

            for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
            {
                vertices[0 + 4 * i] = new Vector3(-distHalf + i, 0f, 0f);
                vertices[1 + 4 * i] = new Vector3(distHalf + i, 0f, 0f);
                vertices[2 + 4 * i] = new Vector3(-distHalf - RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.rowDistVert, 0f);
                vertices[3 + 4 * i] = new Vector3(distHalf - RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.rowDistVert, 0f);
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
        /// <returns>A <see cref="UnityEngine.Mesh"/> containing
        /// <see cref="RenderSystem.const_amountOfLinesPerMesh"/> diagonal lines from
        /// the top right to the bottom left for the background grid.</returns>
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
                vertices[2 + 4 * i] = new Vector3(-distHalf + RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.rowDistVert, 0f);
                vertices[3 + 4 * i] = new Vector3(distHalf + RenderSystem.const_circularViewBGLineLength * 0.5f + i, RenderSystem.const_circularViewBGLineLength * AmoebotFunctions.rowDistVert, 0f);
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
        /// Creates a mesh for a particle.
        /// This mesh has bigger boundaries to not get frustrum culled. The positions of the circles are calculated in the shader.
        /// </summary>
        /// <returns></returns>
        public static Mesh GetMesh_Particle()
        {
            // Mesh Data
            Mesh mesh = Library.MeshConstants.getDefaultMeshQuad(3f, 0f, new Vector2(0.5f, 0.5f));

            return mesh;
        }

        /// <summary>
        /// Creates the mesh for the particles in the circular graph view.
        /// </summary>
        /// <returns></returns>
        public static Mesh GetMesh_ParticleOptimized()
        {
            Mesh mesh = Library.MeshConstants.getDefaultMeshQuad(2f, 0f, new Vector2(0.25f, 0.5f));
            Vector3[] vertices = mesh.vertices;
            vertices[0] = new Vector3(vertices[0].x, vertices[0].y * 0.5f, vertices[0].z);
            vertices[1] = new Vector3(vertices[1].x, vertices[1].y * 0.5f, vertices[1].z);
            vertices[2] = new Vector3(vertices[2].x, vertices[2].y * 0.5f, vertices[2].z);
            vertices[3] = new Vector3(vertices[3].x, vertices[3].y * 0.5f, vertices[3].z);
            mesh.vertices = vertices;

            return mesh;
        }

        /// <summary>
        /// Creates a mesh for particle connector.
        /// </summary>
        /// <returns></returns>
        public static Mesh GetMesh_ParticleConnector()
        {
            // Mesh Data
            Mesh mesh = Library.MeshConstants.getDefaultMeshQuad(1f, 0f, new Vector2(0f, 0.5f));
            Vector2[] uv2 = new Vector2[4];

            // UV2.x = 0 means this vertex will move
            uv2[0] = new Vector2(1f, 0f);
            uv2[1] = new Vector2(0f, 0f);
            uv2[2] = new Vector2(1f, 0f);
            uv2[3] = new Vector2(0f, 0f);

            mesh.uv2 = uv2;

            return mesh;
        }

    }

}