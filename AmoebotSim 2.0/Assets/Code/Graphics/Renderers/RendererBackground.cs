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
    /// Renders the background grid. Both the hexagonal grid and the graph view are supported.
    /// </summary>
    public class RendererBackground
    {

        // Data _____
        // Hexagonal
        private Mesh mesh_hex_hexGrid = MeshCreator_HexagonalView.GetMesh_HexagonGridLine();
        private InstancedDrawer instancedDrawer_hexGrid = new InstancedDrawer();
        // Circular
        private Mesh mesh_circ_bgHor = MeshCreator_CircularView.GetMesh_BGLinesHorizontal();
        private Mesh mesh_circ_bgDiaBLTR = MeshCreator_CircularView.GetMesh_BGLinesTopRightBottomLeft();
        private Mesh mesh_circ_bgDiaBRTL = MeshCreator_CircularView.GetMesh_BGLinesTopLeftBottomRight();
        private InstancedDrawer instancedDrawer_circGrid_horLines = new InstancedDrawer();
        private InstancedDrawer instancedDrawer_cricGrid_diaLines = new InstancedDrawer();


        /// <summary>
        /// Renders the background based on the current view.
        /// Should be called in every frame.
        /// </summary>
        /// <param name="viewType">The view type that is
        /// used to render the particle system. Influences
        /// which type of background is rendered.</param>
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

        /// <summary>
        /// Renders the hexagonal grid background with instanced drawing.
        /// </summary>
        private void Render_Hexagonal()
        {
            // Get camera bounding box
            Vector2 camLowest = CameraUtils.GetLowestXYCameraWorldPositions();
            Vector2 camHighest = CameraUtils.GetHightestXYCameraWorldPositions();

            // Add margin around the bounding box
            Vector2 bgPosBL = camLowest + new Vector2(-3f, -10f);
            Vector2 bgPosTR = camHighest + new Vector2(3f, 10f);
            // Compute number of rows based on bounding box size
            Vector2Int screenSizeForBGAdjusted = new Vector2Int((int)(bgPosTR.x - bgPosBL.x), (int)(bgPosTR.y - bgPosBL.y));
            int amountLines = Mathf.CeilToInt(screenSizeForBGAdjusted.y / AmoebotFunctions.rowDistVert);

            // Determine the positions of the row meshes
            // Every other row is offset by the grid vector (0, 1) relative
            // to the preceding row
            Vector2Int firstBgGridPos = AmoebotFunctions.WorldToGridPosition(bgPosBL);
            Vector2Int secondBgGridPos = firstBgGridPos + new Vector2Int(0, 1);
            Vector3 firstBgMeshPos = AmoebotFunctions.GridToWorldPositionVector2(firstBgGridPos);
            Vector3 secondBgMeshPos = AmoebotFunctions.GridToWorldPositionVector2(secondBgGridPos);
            firstBgMeshPos.z = RenderSystem.zLayer_background;
            secondBgMeshPos.z = RenderSystem.zLayer_background;

            // Add updated matrices for the hex row meshes to the instanced drawer
            instancedDrawer_hexGrid.ClearMatrices();
            Vector3 pos;
            float heightDiff = AmoebotFunctions.rowDistVert;
            for (int i = 0; i < amountLines; i++)
            {
                if (i % 2 == 0)
                    pos = firstBgMeshPos + new Vector3(0f, heightDiff * i, 0f);
                else
                    pos = secondBgMeshPos + new Vector3(0f, heightDiff * (i - 1), 0f);
                instancedDrawer_hexGrid.AddMatrix(Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one));
            }

            // Render all rows
            instancedDrawer_hexGrid.Draw(mesh_hex_hexGrid, MaterialDatabase.material_hexagonal_bgHex);
        }

        /// <summary>
        /// Renders the triangular grid background with instanced drawing.
        /// </summary>
        private void Render_Circular()
        {
            // Get camera bounding box
            Vector2 camLowest = CameraUtils.GetLowestXYCameraWorldPositions();
            Vector2 camHighest = CameraUtils.GetHightestXYCameraWorldPositions();
            Vector2 screenSize = camHighest - camLowest;
            float widthHeightRatio = screenSize.y / screenSize.x;

            // Compute number of lines based on the bounds
            // We need to adjust the bounds so the grid is evenly placed on the screen
            Vector2 bgPosBL = camLowest + new Vector2(-10f - screenSize.y * widthHeightRatio * 1.5f, -10f);
            Vector2 bgPosTR = camHighest + new Vector2(10f + screenSize.y * widthHeightRatio * 1.5f, 10f);
            int amountDiagonalMeshes = Mathf.CeilToInt(bgPosTR.x - bgPosBL.x + 2) / RenderSystem.const_amountOfLinesPerMesh + 1;
            int amountHorizontalMeshes = (int)(camHighest.y - camLowest.y + 4) / RenderSystem.const_amountOfLinesPerMesh + 1;

            // Determine the base positions of the meshes
            Vector2Int firstBgGridPos = AmoebotFunctions.WorldToGridPosition(bgPosBL);
            Vector3 firstBgMeshPos = AmoebotFunctions.GridToWorldPositionVector2(firstBgGridPos);
            firstBgMeshPos.z = RenderSystem.zLayer_background;

            // Build the updated matrices and hand them to the instanced drawers
            instancedDrawer_circGrid_horLines.ClearMatrices();
            instancedDrawer_cricGrid_diaLines.ClearMatrices();
            for (int i = 0; i < amountHorizontalMeshes; i++)
            {
                instancedDrawer_circGrid_horLines.AddMatrix(Matrix4x4.TRS(firstBgMeshPos + i * new Vector3(0f, RenderSystem.const_amountOfLinesPerMesh, 0f), Quaternion.identity, Vector3.one));
            }
            for (int i = 0; i < amountDiagonalMeshes; i++)
            {
                instancedDrawer_cricGrid_diaLines.AddMatrix(Matrix4x4.TRS(firstBgMeshPos + i * new Vector3(RenderSystem.const_amountOfLinesPerMesh, 0f, 0f), Quaternion.identity, Vector3.one));
            }

            // Render all lines
            instancedDrawer_circGrid_horLines.Draw(mesh_circ_bgHor, MaterialDatabase.material_circular_bgLines);
            instancedDrawer_cricGrid_diaLines.Draw(mesh_circ_bgDiaBLTR, MaterialDatabase.material_circular_bgLines);
            instancedDrawer_cricGrid_diaLines.Draw(mesh_circ_bgDiaBRTL, MaterialDatabase.material_circular_bgLines);
        }
    }
}
