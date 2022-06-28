using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererParticles
{

    // Data
    private Dictionary<Particle, ParticleGraphicalData> particleToparticleGraphicalDataMap = new Dictionary<Particle, ParticleGraphicalData>();

    // Graphics
    private List<Matrix4x4[]> particleMatrices = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesInner = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesExpanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesExpandedInner = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleConnectionMatrices = new List<Matrix4x4[]>();

    // Precalculated Data _____
    // Meshes
    private Mesh defaultQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    private Mesh defaultQuadLeftSidePivot = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    // Matrix TRS Params
    private float particleConnectedWidth = 0.1f;
    Quaternion quaternion_horLeftParticleConnection;
    Vector3 scale_horLeftParticleConnection;
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
        quaternion_horLeftParticleConnection = Quaternion.Euler(0f, 0f, 180f) * Quaternion.identity;
        quaternion_diaTopLeftParticleConnection = Quaternion.Euler(0f, 0f, 120f) * Quaternion.identity;
        quaternion_diaTopRightParticleConnection = Quaternion.Euler(0f, 0f, 60f) * Quaternion.identity;
        scale_horLeftParticleConnection = new Vector3(1f, particleConnectedWidth, 1f);
        float diagonalConnectionLength = Mathf.Sqrt(0.5f * 0.5f + AmoebotFunctions.HeightDifferenceBetweenRows() * AmoebotFunctions.HeightDifferenceBetweenRows());
        scale_diaTopLeftParticleConnection = new Vector3(diagonalConnectionLength, particleConnectedWidth, 1f);
        scale_diaTopRightParticleConnection = new Vector3(diagonalConnectionLength, particleConnectedWidth, 1f);
    }

    public void Particle_Connect(ParticleGraphicalData graphicalData)
    {
        particleToparticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
    }

    public bool Particle_Disconnect(ParticleGraphicalData graphicalData)
    {
        if(particleToparticleGraphicalDataMap.ContainsKey(graphicalData.particle) == false)
        {
            Log.Error("RendererParticles: Particle_Disconnect: Key not found!");
            return false;
        }
        particleToparticleGraphicalDataMap.Remove(graphicalData.particle);
        
        // Remove particle from visual structure
        ...
    }

    public bool Particle_Add(ParticleGraphicalData particleGraphicalData)
    {
        if (particleToparticleGraphicalDataMap.ContainsKey(particleGraphicalData.particle)) return false;

        int listNumber = 0;
        int listID = 0;
        if (((particleGraphicalData.Count + 1) % maxArraySize) == 1)
        {
            Matrix4x4[] particleMatrix = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixInner = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixExpanded = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleMatrixExpandedInner = new Matrix4x4[maxArraySize];
            Matrix4x4[] particleConnectionMatrix = new Matrix4x4[maxArraySize];
            for (int i = 0; i < maxArraySize; i++)
            {
                particleMatrix[i] = matrixTRS_zero;
                particleMatrixExpanded[i] = matrixTRS_zero;
                particleConnectionMatrix[i] = matrixTRS_zero;
            }
            particleMatrices.Add(particleMatrix);
            particleMatricesInner.Add(particleMatrixInner);
            particleMatricesExpanded.Add(particleMatrixExpanded);
            particleMatricesExpandedInner.Add(particleMatrixExpandedInner);
            particleConnectionMatrices.Add(particleConnectionMatrix);
        }
        listNumber = particleMatrices.Count - 1;
        listID = particleGraphicalData.Count % maxArraySize;

        particleGraphicalData.Add(particle, new ParticleGraphicalData(this, particle, listNumber, listID));
        return true;

    }

    public void Particle_Remove(Particle particle)
    {
        if (particleToparticleGraphicalDataMap.ContainsKey(particle)) particleToparticleGraphicalDataMap.Remove(particle);

        throw new System.NotImplementedException();
        // We would need to implement the removal of the graphics here, but let us say for the prototype we do not need this.
    }

    public void ParticleMoved(Particle particle)
    {
        if (particleToparticleGraphicalDataMap.ContainsKey(particle) == false)
        {
            Debug.Log("Error: ParticleMoved: Particle not found.");
        }
        particleToparticleGraphicalDataMap[particle].Moved();
    }

    public void UpdateMatrix(ParticleGraphicalData graphicalData)
    {
        particleMatrices[graphicalData.graphics_list][graphicalData.graphics_id] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.position1.x, graphicalData.position1.y), Quaternion.identity, Vector3.one);
        particleMatricesInner[graphicalData.graphics_list][graphicalData.graphics_id] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.position1.x, graphicalData.position1.y), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));
        if (graphicalData.isExpanded)
        {
            // Expanded
            particleMatricesExpanded[graphicalData.graphics_list][graphicalData.graphics_id] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.position2.x, graphicalData.position2.y), Quaternion.identity, Vector3.one);
            particleMatricesExpandedInner[graphicalData.graphics_list][graphicalData.graphics_id] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.position2.x, graphicalData.position2.y), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));

            Vector2Int particleConnectorPosGrid = Vector2Int.zero;
            Quaternion particleConnectorRot = Quaternion.identity;
            Vector3 particleConnectorScale = Vector3.one;
            if (graphicalData.expansionDir >= 0 && graphicalData.expansionDir <= 2)
            {
                // position2 is the node from which the connection originates
                particleConnectorPosGrid = graphicalData.position2;
                switch (graphicalData.expansionDir)
                {
                    case 0:
                        particleConnectorRot = quaternion_horLeftParticleConnection;
                        particleConnectorScale = scale_horLeftParticleConnection;
                        break;
                    case 1:
                        particleConnectorRot = quaternion_diaTopLeftParticleConnection;
                        particleConnectorScale = scale_diaTopLeftParticleConnection;
                        break;
                    case 2:
                        particleConnectorRot = quaternion_diaTopRightParticleConnection;
                        particleConnectorScale = scale_diaTopRightParticleConnection;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                // position2 is the node from which the connection originates
                particleConnectorPosGrid = graphicalData.position1;
                switch (graphicalData.expansionDir)
                {
                    case 3:
                        particleConnectorRot = quaternion_horLeftParticleConnection;
                        particleConnectorScale = scale_horLeftParticleConnection;
                        break;
                    case 4:
                        particleConnectorRot = quaternion_diaTopLeftParticleConnection;
                        particleConnectorScale = scale_diaTopLeftParticleConnection;
                        break;
                    case 5:
                        particleConnectorRot = quaternion_diaTopRightParticleConnection;
                        particleConnectorScale = scale_diaTopRightParticleConnection;
                        break;
                    default:
                        break;
                }
            }
            Vector3 particleConnectorPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(particleConnectorPosGrid.x, particleConnectorPosGrid.y);

            particleConnectionMatrices[graphicalData.graphics_list][graphicalData.graphics_id] = Matrix4x4.TRS(particleConnectorPos, particleConnectorRot, particleConnectorScale);
        }
        else
        {
            // Contracted
            particleMatricesExpanded[graphicalData.graphics_list][graphicalData.graphics_id] = matrixTRS_zero;
            particleMatricesExpandedInner[graphicalData.graphics_list][graphicalData.graphics_id] = matrixTRS_zero;
            particleConnectionMatrices[graphicalData.graphics_list][graphicalData.graphics_id] = matrixTRS_zero;
        }
    }

    public void RenderAmoebots()
    {
        for (int i = 0; i < particleMatrices.Count; i++)
        {
            // Test
            //Graphics.DrawMeshInstanced(defaultQuad, 0, material_particle, new Matrix4x4[] { Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(10f, 10f, 1f)) });

            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particle, particleMatrices[i]);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particleCenter, particleMatricesInner[i]);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particle, particleMatricesExpanded[i]);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particleCenter, particleMatricesExpandedInner[i]);
            Graphics.DrawMeshInstanced(defaultQuadLeftSidePivot, 0, MaterialDatabase.material_circular_particleConnector, particleConnectionMatrices[i]);
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
        throw new System.NotImplementedException();
    }

    private void Render_Circular()
    {
        
    }

}