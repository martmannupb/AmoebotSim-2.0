using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererParticles
{

    // Data _____
    // Particles
    private Dictionary<IParticleState, ParticleGraphicsAdapterImpl> particleToParticleGraphicalDataMap = new Dictionary<IParticleState, ParticleGraphicsAdapterImpl>();
    // Graphics
    private List<Matrix4x4[]> particleMatrices = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesInner = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesExpanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesExpandedInner = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesBG = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleConnectionMatrices = new List<Matrix4x4[]>();

    // Precalculated Data _____
    // Meshes
    private Mesh defaultQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    private Mesh defaultQuadLeftSidePivot = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    private Mesh defaultHexagon = MeshCreator_HexagonalView.GetMesh_BaseHexagon();
    private Mesh defaultHexagonCenter = MeshCreator_HexagonalView.GetMesh_BaseHexagonCenter();
    // Matrix TRS Params
    private float particleConnectedWidth = 0.1f;
    Quaternion quaternion_horRightParticleConnection;
    Vector3 scale_horRightParticleConnection;
    Quaternion quaternion_diaTopLeftParticleConnection;
    Vector3 scale_diaTopLeftParticleConnection;
    Quaternion quaternion_diaTopRightParticleConnection;
    Vector3 scale_diaTopRightParticleConnection;
    // Defaults
    Matrix4x4 matrixTRS_zero = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero);
    const int maxArraySize = 1023;
    float innerParticleScaleFactor = 0.75f;

    public RendererParticles()
    {
        Init();
    }

    public void Init()
    {
        // Calculate Matrix Params
        quaternion_horRightParticleConnection = Quaternion.Euler(0f, 0f, 0f) * Quaternion.identity;
        quaternion_diaTopLeftParticleConnection = Quaternion.Euler(0f, 0f, 120f) * Quaternion.identity;
        quaternion_diaTopRightParticleConnection = Quaternion.Euler(0f, 0f, 60f) * Quaternion.identity;
        scale_horRightParticleConnection = new Vector3(1f, particleConnectedWidth, 1f);
        float diagonalConnectionLength = Mathf.Sqrt(0.5f * 0.5f + AmoebotFunctions.HeightDifferenceBetweenRows() * AmoebotFunctions.HeightDifferenceBetweenRows());
        scale_diaTopLeftParticleConnection = new Vector3(diagonalConnectionLength, particleConnectedWidth, 1f);
        scale_diaTopRightParticleConnection = new Vector3(diagonalConnectionLength, particleConnectedWidth, 1f);
    }

    public bool Particle_Add(ParticleGraphicsAdapterImpl graphicalData)
    {
        if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle)) return false;

        if (((particleToParticleGraphicalDataMap.Count + 1) % maxArraySize) == 1)
        {
            Matrix4x4[] particleMatrix = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixInner = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixExpanded = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixExpandedInner = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixBG = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleConnectionMatrix = new Matrix4x4[maxArraySize];
            for (int i = 0; i < maxArraySize; i++)
            {
                particleMatrix[i] = matrixTRS_zero;
                particleMatrixInner[i] = matrixTRS_zero;
                particleMatrixExpanded[i] = matrixTRS_zero;
                particleMatrixExpandedInner[i] = matrixTRS_zero;
                particleMatrixBG[i] = matrixTRS_zero;
                particleConnectionMatrix[i] = matrixTRS_zero;
            }
            particleMatrices.Add(particleMatrix);
            particleMatricesInner.Add(particleMatrixInner);
            particleMatricesExpanded.Add(particleMatrixExpanded);
            particleMatricesExpandedInner.Add(particleMatrixExpandedInner);
            particleMatricesBG.Add(particleMatrixBG);
            particleConnectionMatrices.Add(particleConnectionMatrix);
        }
        graphicalData.graphics_listNumber = particleMatrices.Count - 1;
        graphicalData.graphics_listID = particleToParticleGraphicalDataMap.Count % maxArraySize;

        particleToParticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
        return true;
    }

    public void Particle_Remove(IParticleState particle)
    {
        if (particleToParticleGraphicalDataMap.ContainsKey(particle)) particleToParticleGraphicalDataMap.Remove(particle);

        throw new System.NotImplementedException();
        // We would need to implement the removal of the graphics here, but let us say for the prototype we do not need this.
    }

    public void UpdateMatrix(ParticleGraphicsAdapterImpl graphicalData)
    {
        particleMatrices[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.stored_position1.x, graphicalData.stored_position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        particleMatricesInner[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.stored_position1.x, graphicalData.stored_position1.y, RenderSystem.zLayer_particles), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));
        particleMatricesBG[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.stored_position1.x, graphicalData.stored_position1.y, RenderSystem.ZLayer_particlesBG), Quaternion.identity, Vector3.one);
        if (graphicalData.stored_isExpanded)
        {
            // Expanded
            particleMatricesExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.stored_position2.x, graphicalData.stored_position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
            particleMatricesExpandedInner[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.stored_position2.x, graphicalData.stored_position2.y, RenderSystem.zLayer_particles), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));

            Vector2Int particleConnectorPosGrid = Vector2Int.zero;
            Quaternion particleConnectorRot = Quaternion.identity;
            Vector3 particleConnectorScale = Vector3.one;
            if (graphicalData.stored_globalExpansionDir >= 0 && graphicalData.stored_globalExpansionDir <= 2)
            {
                // position2 is the node from which the connection originates
                particleConnectorPosGrid = graphicalData.stored_position2;
                switch (graphicalData.stored_globalExpansionDir)
                {
                    case 0: // right expansion
                        particleConnectorRot = quaternion_horRightParticleConnection;
                        particleConnectorScale = scale_horRightParticleConnection;
                        break;
                    case 1: // top right expansion
                        particleConnectorRot = quaternion_diaTopRightParticleConnection;
                        particleConnectorScale = scale_diaTopRightParticleConnection;
                        break;
                    case 2: // top left expansion
                        particleConnectorRot = quaternion_diaTopLeftParticleConnection;
                        particleConnectorScale = scale_diaTopLeftParticleConnection;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                // position2 is the node from which the connection originates
                particleConnectorPosGrid = graphicalData.stored_position1;
                switch (graphicalData.stored_globalExpansionDir)
                {
                    case 3: // left expansion
                        particleConnectorRot = quaternion_horRightParticleConnection;
                        particleConnectorScale = scale_horRightParticleConnection;
                        break;
                    case 4: // bottom left
                        particleConnectorRot = quaternion_diaTopRightParticleConnection;
                        particleConnectorScale = scale_diaTopRightParticleConnection;
                        break;
                    case 5: // bottom right
                        particleConnectorRot = quaternion_diaTopLeftParticleConnection;
                        particleConnectorScale = scale_diaTopLeftParticleConnection;
                        break;
                    default:
                        break;
                }
            }
            Vector3 particleConnectorPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(particleConnectorPosGrid.x, particleConnectorPosGrid.y);

            particleConnectionMatrices[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(particleConnectorPos, particleConnectorRot, particleConnectorScale);
        }
        else
        {
            // Contracted
            particleMatricesExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
            particleMatricesExpandedInner[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
            particleConnectionMatrices[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
        }
    }

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
        for (int i = 0; i < particleMatrices.Count; i++)
        {
            int listLength;
            if (i == particleMatrices.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            Graphics.DrawMeshInstanced(defaultHexagonCenter, 0, MaterialDatabase.material_hexagonal_particleCenter, particleMatricesBG[i]);
            Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particle, particleMatrices[i]);
            //Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatrices[i]);
        }
    }

    private void Render_Circular()
    {
        for (int i = 0; i < particleMatrices.Count; i++)
        {
            // Test
            //Graphics.DrawMeshInstanced(defaultQuad, 0, material_particle, new Matrix4x4[] { Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(10f, 10f, 1f)) });
            
            int listLength;
            if (i == particleMatrices.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particle, particleMatrices[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particleCenter, particleMatricesInner[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particle, particleMatricesExpanded[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particleCenter, particleMatricesExpandedInner[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuadLeftSidePivot, 0, MaterialDatabase.material_circular_particleConnector, particleConnectionMatrices[i], listLength);
        }
    }

}