using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererUI
{

    // References
    private ParticleSystem map;

    public Mesh mesh_particleOverlay_contracted;
    public Mesh mesh_particleOverlay_expanded;

    public Mesh mesh_baseHexagonBackground;
    public Material material_hexagonSelectionOverlay;

    public RendererUI()
    {
        // Contracted Mesh - A mesh that covers one particle
        this.mesh_particleOverlay_contracted = Engine.Library.MeshConstants.getDefaultMeshQuad();
        // Expanded Mesh - We want a mesh that is pivoted on the current coordinate and that covers the next field to the right (so we can rotate the mesh correctly)
        this.mesh_particleOverlay_expanded = Engine.Library.MeshConstants.getDefaultMeshQuad();
        this.mesh_particleOverlay_expanded = Engine.Library.MeshConstants.scaleMeshAroundPivot(this.mesh_particleOverlay_expanded, new Vector3(2f, 1f, 1f));
        this.mesh_particleOverlay_expanded = Engine.Library.MeshConstants.offsetMeshVertices(this.mesh_particleOverlay_expanded, new Vector3(0.5f, 0f, 0f));

        // 
        this.mesh_baseHexagonBackground = MeshCreator_HexagonalView.GetMesh_BaseHexagonBackground();
        this.material_hexagonSelectionOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonSelectionMaterial;
    }

    public void AddReferenceToMap(ParticleSystem map)
    {
        this.map = map;
    }

    public void Render(ViewType viewType)
    {
        Vector2 camPos = CameraUtils.MainCamera_Mouse_WorldPosition();
        Vector2Int camWorldField = AmoebotFunctions.GetGridPositionFromWorldPosition(AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(camPos));

        if (map != null)
        {
            // Find out if we are over a UI or non-UI element
            // (look at code from other project)

            // Show Particle Selection Overlay
            Particle p;
            map.TryGetParticleAt(camWorldField, out p);
            if(p != null)
            {
                // Render Head Overlay
                Vector3 worldPos_head = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(p.Head());
                Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                // Render Tail Overlay
                if (p.IsExpanded())
                {
                    Vector3 worldPos_tail = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(p.Tail());
                    Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                }
            }
        }
    }

}