using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCreator_CircularView_BGLines
{

    /// <summary>
    /// Creates a mesh for the background ...
    /// </summary>
    /// <returns></returns>
    public Mesh GetMesh_BGLinesHorizontal()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];
        int[] triangles = new int[6 * RenderSystem.const_amountOfLinesPerMesh];
        Vector2[] uv = new Vector2[4 * RenderSystem.const_amountOfLinesPerMesh];
        Vector3[] normals = new Vector3[4 * RenderSystem.const_amountOfLinesPerMesh];

        for (int i = 0; i < RenderSystem.const_amountOfLinesPerMesh; i++)
        {
            //vertices[0 + 4 * RenderSystem.const_amountOfLinesPerMesh] = new Vector3(,0f,0f)
            // todo...
        }
            throw new System.NotImplementedException();
    }

}
