using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererBackground
{

    // Data _____
    // Hexagonal
    // Circular
    private List<Mesh> mesh_circ_bgHor = new List<Mesh>();
    private List<Mesh> mesh_circ_bgDiaBLTR = new List<Mesh>();
    private List<Mesh> mesh_circ_bgDiaBRTL = new List<Mesh>();
    private List<Matrix4x4> matrix_circ_bgHor = new List<Matrix4x4>();
    private List<Matrix4x4> matrix_circ_bgDia = new List<Matrix4x4>(); // same for both line types




    public void Render(ViewType viewType)
    {
        switch (viewType)
        {
            case ViewType.Hexagonal:
                Render_Hexagonal();
                break;
            case ViewType.Circular:
                Render_Circular();
                break;
            default:
                break;
        }
    }

    private void Render_Hexagonal()
    {
        throw new System.NotImplementedException();
    }

    private void Render_Circular()
    {
        // Camera
        Vector2 camPosBL = CameraUtils.MainCamera_WorldPosition_BottomLeft();
        Vector2 camPosTR = CameraUtils.MainCamera_WorldPosition_TopRight();
        Vector2 screenSize = camPosTR - camPosBL;
        float widthHeightRatio = screenSize.y / screenSize.x;

        // 1. Background
        // We need to adjust the bounds so the grid ist evenly placed on the screen
        Vector2 bgPosBL = camPosBL + new Vector2(-10f - screenSize.y * widthHeightRatio * 1.5f, -10f);
        Vector2 bgPosTR = camPosTR + new Vector2(10f + screenSize.y * widthHeightRatio * 1.5f, 10f);
        int amountDiagonalMeshes = Mathf.CeilToInt(bgPosTR.x - bgPosBL.x + 2) / RenderSystem.const_amountOfLinesPerMesh + 1;
        int amountHorizontalMeshes = (int)(camPosTR.y - camPosBL.y + 4) / RenderSystem.const_amountOfLinesPerMesh + 1;
        // Calc pos of first mesh
        Vector2Int firstBgGridPos = AmoebotFunctions.GetGridPositionFromWorldPosition(bgPosBL);
        Vector3 firstBgMeshPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(firstBgGridPos);
        firstBgMeshPos.z = RenderSystem.zLayer_background;
        // Add diagonal meshes
        while (mesh_circ_bgDiaBLTR.Count < amountDiagonalMeshes)
        {
            mesh_circ_bgDiaBLTR.Add(MeshCreator_CircularView.GetMesh_BGLinesTopRightBottomLeft());
            mesh_circ_bgDiaBRTL.Add(MeshCreator_CircularView.GetMesh_BGLinesTopLeftBottomRight());
            matrix_circ_bgDia.Add(new Matrix4x4());
        }
        // Add horizontal meshes
        while(mesh_circ_bgHor.Count < amountHorizontalMeshes)
        {
            mesh_circ_bgHor.Add(MeshCreator_CircularView.GetMesh_BGLinesHorizontal());
            matrix_circ_bgHor.Add(new Matrix4x4());
        }
        // Render
        for (int i = 0; i < amountHorizontalMeshes; i++)
        {
            matrix_circ_bgHor[i] = Matrix4x4.TRS(firstBgMeshPos + i * new Vector3(0f, RenderSystem.const_amountOfLinesPerMesh, 0f), Quaternion.identity, Vector3.one);
            Graphics.DrawMesh(mesh_circ_bgHor[i], matrix_circ_bgHor[i], MaterialDatabase.material_circular_bgLines, 0);
        }
        for (int i = 0; i < amountDiagonalMeshes; i++)
        {
            matrix_circ_bgDia[i] = Matrix4x4.TRS(firstBgMeshPos + i * new Vector3(RenderSystem.const_amountOfLinesPerMesh, 0f, 0f), Quaternion.identity, Vector3.one);
            Graphics.DrawMesh(mesh_circ_bgDiaBLTR[i], matrix_circ_bgDia[i], MaterialDatabase.material_circular_bgLines, 0);
            Graphics.DrawMesh(mesh_circ_bgDiaBRTL[i], matrix_circ_bgDia[i], MaterialDatabase.material_circular_bgLines, 0);
        }
    }
}
