using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererBackground
{

    // Data _____
    // Hexagonal
    private Mesh mesh_hex_hexGrid = MeshCreator_HexagonalView.GetMesh_HexagonGridLine();
    private List<Matrix4x4> matrix_hex_hexGridPos = new List<Matrix4x4>();
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
            case ViewType.HexagonalCirc:
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
        // Camera
        Vector2 camPosBL = CameraUtils.MainCamera_WorldPosition_BottomLeft();
        Vector2 camPosTR = CameraUtils.MainCamera_WorldPosition_TopRight();
        Vector2 camLowest = CameraUtils.GetLowestXYCameraWorldPositions();
        Vector2 camHighest = CameraUtils.GetHightestXYCameraWorldPositions();
        Vector2 screenSize = camHighest - camLowest;

        // 1. Background
        Vector2 bgPosBL = camLowest + new Vector2(-3f, -10f);
        Vector2 bgPosTR = camHighest + new Vector2(3f, 10f);
        Vector2Int screenSizeForBGAdjusted = new Vector2Int((int)(bgPosTR.x - bgPosBL.x), (int)(bgPosTR.y - bgPosBL.y));
        int amountLines = Mathf.CeilToInt(screenSizeForBGAdjusted.y / AmoebotFunctions.HeightDifferenceBetweenRows());

        // Calc pos of first mesh
        Vector2Int firstBgGridPos = AmoebotFunctions.GetGridPositionFromWorldPosition(bgPosBL);
        Vector2Int secondBgGridPos = firstBgGridPos + new Vector2Int(0, 1);
        Vector3 firstBgMeshPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(firstBgGridPos);
        Vector3 secondBgMeshPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(secondBgGridPos);
        firstBgMeshPos.z = RenderSystem.zLayer_background;
        secondBgMeshPos.z = RenderSystem.zLayer_background;
        // Add hex line meshes
        while (matrix_hex_hexGridPos.Count < amountLines)
        {
            matrix_hex_hexGridPos.Add(new Matrix4x4());
        }
        // Update Matrices
        Vector3 pos;
        float heightDiff = AmoebotFunctions.HeightDifferenceBetweenRows();
        for (int i = 0; i < amountLines; i++)
        {
            if (i % 2 == 0) pos = firstBgMeshPos + new Vector3(0f, heightDiff * i, 0f);
            else pos = secondBgMeshPos + new Vector3(0f, heightDiff * (i-1), 0f);
            matrix_hex_hexGridPos[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        }
        // Render
        //Graphics.DrawMeshInstanced(mesh_hex_hexGrid, 0, MaterialDatabase.material_hexagonal_bgHex, matrix_hex_hexGridPos, amountLines);
        for (int i = 0; i < amountLines; i++)
        {
            Graphics.DrawMesh(mesh_hex_hexGrid, matrix_hex_hexGridPos[i], MaterialDatabase.material_hexagonal_bgHex, 0);
        }
    }

    private void Render_Circular()
    {
        // Camera
        Vector2 camPosBL = CameraUtils.MainCamera_WorldPosition_BottomLeft();
        Vector2 camPosTR = CameraUtils.MainCamera_WorldPosition_TopRight();
        Vector2 camLowest = CameraUtils.GetLowestXYCameraWorldPositions();
        Vector2 camHighest = CameraUtils.GetHightestXYCameraWorldPositions();
        Vector2 screenSize = camHighest - camLowest;
        float widthHeightRatio = screenSize.y / screenSize.x;

        // 1. Background
        // We need to adjust the bounds so the grid ist evenly placed on the screen
        Vector2 bgPosBL = camLowest + new Vector2(-10f - screenSize.y * widthHeightRatio * 1.5f, -10f);
        Vector2 bgPosTR = camHighest + new Vector2(10f + screenSize.y * widthHeightRatio * 1.5f, 10f);
        int amountDiagonalMeshes = Mathf.CeilToInt(bgPosTR.x - bgPosBL.x + 2) / RenderSystem.const_amountOfLinesPerMesh + 1;
        int amountHorizontalMeshes = (int)(camHighest.y - camLowest.y + 4) / RenderSystem.const_amountOfLinesPerMesh + 1;
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
