using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererParticles_RenderBatch
{
    
    // Data _____
    // Particles
    private Dictionary<IParticleState, ParticleGraphicsAdapterImpl> particleToParticleGraphicalDataMap = new Dictionary<IParticleState, ParticleGraphicsAdapterImpl>();
    private List<ParticleGraphicsAdapterImpl> graphicalDataList = new List<ParticleGraphicsAdapterImpl>();
    // Graphics
    // Circle
    private List<Matrix4x4[]> particleMatricesCircle_Contracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_Expanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_Expanding = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_Contracting = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Contracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Expanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Expanding = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Contracting = new List<Matrix4x4[]>();
    // PropertyBlocks
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_contracted = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_expanded = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_expanding = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_contracting = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_connector_contracted = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_connector_expanded = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_connector_expanding = new MaterialPropertyBlockData_CircParticles();
    private MaterialPropertyBlockData_CircParticles propertyBlock_circle_connector_contracting = new MaterialPropertyBlockData_CircParticles();







    //// Hexagon
    //private List<Matrix4x4[]> particleMatricesHex_Contracted = new List<Matrix4x4[]>();
    //private List<Matrix4x4[]> particleMatricesHex_Expanded1 = new List<Matrix4x4[]>();
    //private List<Matrix4x4[]> particleMatricesHex_Expanded2 = new List<Matrix4x4[]>();
    //private List<Matrix4x4[]> particleMatricesHex_ContractedToExpanded = new List<Matrix4x4[]>();
    //private List<Matrix4x4[]> particleMatricesHex_ExpandedToContracted = new List<Matrix4x4[]>();

    //private List<Matrix4x4> particleMatricesSingle_Position1 = new List<Matrix4x4>();
    //private List<Matrix4x4> particleMatricesSingle_Position2 = new List<Matrix4x4>();
    //private List<MaterialPropertyBlockData_HexParticles> particlePropertyBlockSingle_Position1 = new List<MaterialPropertyBlockData_HexParticles>();
    //private List<MaterialPropertyBlockData_HexParticles> particlePropertyBlockSingle_Position2 = new List<MaterialPropertyBlockData_HexParticles>();

    //// MaterialPropertyBlocks
    //private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_Contracted;
    //private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_Expanded;
    //private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_ContractedToExpanded;
    //private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_ExpandedToContracted;

    // Precalculated Data _____
    // Meshes
    private Mesh defaultQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    private Mesh mesh_circle_particle = MeshCreator_CircularView.GetMesh_ParticleOptimized();
    private Mesh mesh_circle_particleConnector = MeshCreator_CircularView.GetMesh_ParticleConnector();
    private Mesh defaultQuadLeftSidePivot = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    private Mesh defaultHexagon = MeshCreator_HexagonalView.GetMesh_BaseExpansionHexagon();
    private Mesh defaultHexagonCenter = MeshCreator_HexagonalView.GetMesh_BaseHexagonBackground();
    // Matrix TRS Params
    //static float diagonalConnectionLength = Mathf.Sqrt(0.5f * 0.5f + AmoebotFunctions.HeightDifferenceBetweenRows() * AmoebotFunctions.HeightDifferenceBetweenRows());
    static private float particleConnectedWidth = 0.1f;
    static Quaternion quaternion_horRightParticleConnection = Quaternion.Euler(0f, 0f, 0f) * Quaternion.identity;
    //static Vector3 scale_horRightParticleConnection = new Vector3(1f, particleConnectedWidth, 1f);
    static Quaternion quaternion_diaTopLeftParticleConnection = Quaternion.Euler(0f, 0f, 120f) * Quaternion.identity;
    //static Vector3 scale_diaTopLeftParticleConnection = new Vector3(diagonalConnectionLength, particleConnectedWidth, 1f);
    static Quaternion quaternion_diaTopRightParticleConnection = Quaternion.Euler(0f, 0f, 60f) * Quaternion.identity;
    //static Vector3 scale_diaTopRightParticleConnection = new Vector3(diagonalConnectionLength, particleConnectedWidth, 1f);
    static Quaternion quaternion_right = Quaternion.Euler(0f, 0f, 0f) * Quaternion.identity;
    static Quaternion quaternion_topRight = Quaternion.Euler(0f, 0f, 60f) * Quaternion.identity;
    static Quaternion quaternion_topLeft = Quaternion.Euler(0f, 0f, 120f) * Quaternion.identity;
    static Quaternion quaternion_left = Quaternion.Euler(0f, 0f, 180f) * Quaternion.identity;
    static Quaternion quaternion_botLeft = Quaternion.Euler(0f, 0f, 240f) * Quaternion.identity;
    static Quaternion quaternion_botRight = Quaternion.Euler(0f, 0f, 300f) * Quaternion.identity;
    // Defaults
    Matrix4x4 matrixTRS_zero = Matrix4x4.TRS(new Vector3(float.MaxValue / 2f, float.MaxValue / 2f, 0f), Quaternion.identity, Vector3.zero);
    const int maxArraySize = 1023;
    float innerParticleScaleFactor = 0.75f;

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public Color color;

        public PropertyBlockData(Color color)
        {
            this.color = color;
        }
    }

    public RendererParticles_RenderBatch(PropertyBlockData properties)
    {
        this.properties = properties;

        Init();
    }

    public void Init()
    {
        // Circle PropertyBlocks
        propertyBlock_circle_contracted.ApplyColor(properties.color);
        propertyBlock_circle_expanded.ApplyColor(properties.color);
        propertyBlock_circle_expanding.ApplyColor(properties.color);
        propertyBlock_circle_contracting.ApplyColor(properties.color);
        propertyBlock_circle_contracted.ApplyUpdatedValues(false, 0, 0f, 0f, Vector3.right);
        propertyBlock_circle_expanded.ApplyUpdatedValues(true, 0, 1f, 1f, Vector3.right);
        propertyBlock_circle_expanding.ApplyUpdatedValues(true, 0, 0f, 1f, Vector3.right);
        propertyBlock_circle_contracting.ApplyUpdatedValues(true, 0, 1f, 0f, Vector3.right);
        propertyBlock_circle_connector_contracted.ApplyConnectorValues(0f, 0f, Vector3.right, Vector3.left);
        propertyBlock_circle_connector_expanded.ApplyConnectorValues(1f, 1f, Vector3.right, Vector3.left);
        propertyBlock_circle_connector_expanding.ApplyConnectorValues(0f, 1f, Vector3.right, Vector3.left);
        propertyBlock_circle_connector_contracting.ApplyConnectorValues(1f, 0f, Vector3.right, Vector3.left);

        // Hexagon PropertyBlocks
        // ..
    }

    public bool Particle_Add(ParticleGraphicsAdapterImpl graphicalData)
    {
        if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle)) return false;

        if ((particleToParticleGraphicalDataMap.Count % maxArraySize) == 0)
        {
            // Create Matrix Arrays
            particleMatricesCircle_Contracted.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_Expanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_Expanding.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_Contracting.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Contracted.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Expanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Expanding.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Contracting.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));

        }

        // References
        graphicalData.graphics_listNumber = particleMatricesCircle_Contracted.Count - 1;
        graphicalData.graphics_listID = particleToParticleGraphicalDataMap.Count % maxArraySize;
        graphicalData.graphics_globalID = particleToParticleGraphicalDataMap.Count;
        graphicalData.graphics_color = properties.color;
        graphicalData.graphics_colorRenderer = this;
        //Log.Debug("_____Add:    Color: " + properties.color + ", ListNumber: " + graphicalData.graphics_listNumber + ", ListID: " + graphicalData.graphics_listID);

        // Register Particle
        particleToParticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
        graphicalDataList.Add(graphicalData);
        return true;
    }

    public bool Particle_Remove(ParticleGraphicsAdapterImpl graphicalData)
    {
        if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle) == false) return false;

        // Unset Reference
        graphicalData.graphics_colorRenderer = null;

        // Move last particle to the place of this particle
        ParticleGraphicsAdapterImpl lastParticleGraphicalData = graphicalDataList[graphicalDataList.Count - 1];
        int movedParticleIndex = graphicalDataList.IndexOf(graphicalData);
        graphicalDataList[movedParticleIndex] = lastParticleGraphicalData;
        bool canListBeDeleted = graphicalData.graphics_listID == 0 && lastParticleGraphicalData.graphics_listID == 0;
        // Update References
        int original_listNumber = lastParticleGraphicalData.graphics_listNumber;
        int original_listID = lastParticleGraphicalData.graphics_listID;
        lastParticleGraphicalData.graphics_listNumber = movedParticleIndex / maxArraySize;
        lastParticleGraphicalData.graphics_listID = movedParticleIndex % maxArraySize;
        lastParticleGraphicalData.graphics_globalID = movedParticleIndex;
        // Copy Matrices of lastParticleGraphicalData from original index to moved index
        CutAndCopyMatrices(original_listNumber, original_listID, lastParticleGraphicalData.graphics_listNumber, lastParticleGraphicalData.graphics_listID);

        //Log.Debug("_____Remove:     Color: " + properties.color + ", ListNumber: " + graphicalData.graphics_listNumber + ", ListID: " + graphicalData.graphics_listID);
        // Unregister Particle
        graphicalDataList.RemoveAt(graphicalDataList.Count - 1);
        particleToParticleGraphicalDataMap.Remove(graphicalData.particle);
        // Lists became smaller, possibly delete list entry
        if (canListBeDeleted)
        {
            // Particle was only element in the last list
            // Delete list
            int indexOfLastList = particleMatricesCircle_Contracted.Count - 1;

            particleMatricesCircle_Contracted.RemoveAt(indexOfLastList);
            particleMatricesCircle_Expanded.RemoveAt(indexOfLastList);
            particleMatricesCircle_Expanding.RemoveAt(indexOfLastList);
            particleMatricesCircle_Contracting.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Contracted.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Expanded.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Expanding.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Contracting.RemoveAt(indexOfLastList);

            // ..
        }

        return true;
    }

    private void CutAndCopyMatrices(int orig_ListNumber, int orig_ListID, int moved_ListNumber, int moved_ListID)
    {
        // 1. Move Matrices
        particleMatricesCircle_Contracted[moved_ListNumber][moved_ListID] = particleMatricesCircle_Contracted[orig_ListNumber][orig_ListID];
        particleMatricesCircle_Expanded[moved_ListNumber][moved_ListID] = particleMatricesCircle_Expanded[orig_ListNumber][orig_ListID];
        particleMatricesCircle_Expanding[moved_ListNumber][moved_ListID] = particleMatricesCircle_Expanding[orig_ListNumber][orig_ListID];
        particleMatricesCircle_Contracting[moved_ListNumber][moved_ListID] = particleMatricesCircle_Contracting[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Contracted[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Contracted[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Expanded[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Expanded[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Expanding[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Expanding[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Contracting[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Contracting[orig_ListNumber][orig_ListID];

        // 2. Reset Matrices
        ResetMatrices(orig_ListNumber, orig_ListID);
    }

    private void ResetMatrices(int listNumber, int listID)
    {
        particleMatricesCircle_Contracted[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_Expanded[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_Expanding[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_Contracting[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Contracted[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Expanded[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Expanding[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Contracting[listNumber][listID] = matrixTRS_zero;
    }
    
    public void UpdateMatrix(ParticleGraphicsAdapterImpl gd)
    {
        // Reset Matrices
        ResetMatrices(gd.graphics_listNumber, gd.graphics_listID);
        //Log.Debug("UPDATE: Color: "+properties.color+", ListNumber: " + gd.graphics_listNumber + ", ListID: " + gd.graphics_listID + ", List Count: "+ particleMatricesCircle_Contracted.Count+", Array Count: ");

        // Calculate Rotation
        Quaternion rotation = Quaternion.identity;
        if (gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanded || gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding)
        {
            rotation = Quaternion.Euler(0f, 0f, 60f * gd.state_cur.globalExpansionDir) * Quaternion.identity;
        }
        else if (gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting)
        {
            rotation = Quaternion.Euler(0f, 0f, (60f * gd.state_prev.globalExpansionDir + 180f) % 360f) * Quaternion.identity;
        }

        switch (gd.state_cur.movement)
        {
            case ParticleGraphicsAdapterImpl.ParticleMovement.Contracted:
                particleMatricesCircle_Contracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), rotation, Vector3.one);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Expanded:
                particleMatricesCircle_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), rotation, Vector3.one);
                particleMatricesCircle_ConnectionMatrices_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles + 0.1f), rotation, Vector3.one);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Expanding:
                particleMatricesCircle_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), rotation, Vector3.one);
                particleMatricesCircle_ConnectionMatrices_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles + 0.1f), rotation, Vector3.one);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Contracting:
                particleMatricesCircle_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), rotation, Vector3.one);
                particleMatricesCircle_ConnectionMatrices_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles + 0.1f), rotation, Vector3.one);
                break;
            default:
                break;
        }




        //particleMatricesCircle[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //particleMatricesCircleInner[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));
        //particleMatricesHexBG[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.ZLayer_particlesBG), Quaternion.identity, Vector3.one);
        //particleMatricesHexBGExpanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.ZLayer_particlesBG), Quaternion.identity, Vector3.one);

        //particleMatricesHex_Contracted[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //particleMatricesHex_Expanded1[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //particleMatricesHex_Expanded2[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //particleMatricesHex_ContractedToExpanded[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //particleMatricesHex_ExpandedToContracted[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;

        //switch (gd.state_cur.movement)
        //{
        //    case ParticleGraphicsAdapterImpl.ParticleMovement.Contracted:
        //        particleMatricesHex_Contracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                
        //        particleMatricesSingle_Position1[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particleMatricesSingle_Position2[gd.graphics_globalID] = matrixTRS_zero;
        //        particlePropertyBlockSingle_Position1[gd.graphics_globalID].ApplyUpdatedValues(false, gd.state_cur.globalExpansionDir, 0f, 0f);
        //        particlePropertyBlockSingle_Position2[gd.graphics_globalID].ApplyUpdatedValues(false, gd.state_cur.globalExpansionDir, 0f, 0f);
        //        break;
        //    case ParticleGraphicsAdapterImpl.ParticleMovement.Expanded:
        //        float rotationDegrees = gd.state_cur.globalExpansionDir * 60f;
        //        particleMatricesHex_Expanded1[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees), Vector3.one);
        //        particleMatricesHex_Expanded2[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees + 180f), Vector3.one);

        //        particleMatricesSingle_Position1[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particleMatricesSingle_Position2[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particlePropertyBlockSingle_Position1[gd.graphics_globalID].ApplyUpdatedValues(true, (gd.state_cur.globalExpansionDir + 3) % 6, 1f, 1f);
        //        particlePropertyBlockSingle_Position2[gd.graphics_globalID].ApplyUpdatedValues(true, gd.state_cur.globalExpansionDir, 1f, 1f);
        //        break;
        //    case ParticleGraphicsAdapterImpl.ParticleMovement.Expanding:
        //        float rotationDegrees2 = gd.state_cur.globalExpansionDir * 60f;
        //        particleMatricesHex_ContractedToExpanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees2), Vector3.one);
        //        particleMatricesHex_Expanded2[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees2 + 180f), Vector3.one);

        //        particleMatricesSingle_Position1[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particleMatricesSingle_Position2[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particlePropertyBlockSingle_Position1[gd.graphics_globalID].ApplyUpdatedValues(true, (gd.state_cur.globalExpansionDir + 3) % 6, 0f, 1f);
        //        particlePropertyBlockSingle_Position2[gd.graphics_globalID].ApplyUpdatedValues(true, gd.state_cur.globalExpansionDir, 1f, 1f);
        //        break;
        //    case ParticleGraphicsAdapterImpl.ParticleMovement.Contracting:
        //        float rotationDegrees3 = gd.state_prev.globalExpansionDir * 60f;
        //        particleMatricesHex_Expanded1[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees3), Vector3.one);
        //        particleMatricesHex_ExpandedToContracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees3 + 180f), Vector3.one);

        //        particleMatricesSingle_Position1[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particleMatricesSingle_Position2[gd.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //        particlePropertyBlockSingle_Position1[gd.graphics_globalID].ApplyUpdatedValues(true, (gd.state_prev.globalExpansionDir + 3) % 6, 1f, 1f);
        //        particlePropertyBlockSingle_Position2[gd.graphics_globalID].ApplyUpdatedValues(true, gd.state_prev.globalExpansionDir, 1f, 0f);
        //        break;
        //    default:
        //        break;
        //}

        //if (gd.state_cur.isExpanded)
        //{
        //    // Expanded
        //    particleMatricesCircleExpanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        //    particleMatricesCircleExpandedInner[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));

        //    Vector2Int particleConnectorPosGrid = Vector2Int.zero;
        //    Quaternion particleConnectorRot = Quaternion.identity;
        //    Quaternion particleRotation = Quaternion.identity;
        //    Vector3 particleConnectorScale = Vector3.one;
        //    if (gd.state_cur.globalExpansionDir >= 0 && gd.state_cur.globalExpansionDir <= 2)
        //    {
        //        // position2 is the node from which the connection originates
        //        particleConnectorPosGrid = gd.state_cur.position2;
        //        switch (gd.state_cur.globalExpansionDir)
        //        {
        //            case 0: // right expansion
        //                particleConnectorRot = quaternion_horRightParticleConnection;
        //                particleRotation = quaternion_right;
        //                particleConnectorScale = scale_horRightParticleConnection;
        //                break;
        //            case 1: // top right expansion
        //                particleConnectorRot = quaternion_diaTopRightParticleConnection;
        //                particleRotation = quaternion_topRight;
        //                particleConnectorScale = scale_diaTopRightParticleConnection;
        //                break;
        //            case 2: // top left expansion
        //                particleConnectorRot = quaternion_diaTopLeftParticleConnection;
        //                particleRotation = quaternion_topLeft;
        //                particleConnectorScale = scale_diaTopLeftParticleConnection;
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        // position2 is the node from which the connection originates
        //        particleConnectorPosGrid = gd.state_cur.position1;
        //        switch (gd.state_cur.globalExpansionDir)
        //        {
        //            case 3: // left expansion
        //                particleConnectorRot = quaternion_horRightParticleConnection;
        //                particleRotation = quaternion_left;
        //                particleConnectorScale = scale_horRightParticleConnection;
        //                break;
        //            case 4: // bottom left
        //                particleConnectorRot = quaternion_diaTopRightParticleConnection;
        //                particleRotation = quaternion_botLeft;
        //                particleConnectorScale = scale_diaTopRightParticleConnection;
        //                break;
        //            case 5: // bottom right
        //                particleConnectorRot = quaternion_diaTopLeftParticleConnection;
        //                particleRotation = quaternion_botRight;
        //                particleConnectorScale = scale_diaTopLeftParticleConnection;
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    Vector3 particleConnectorPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(particleConnectorPosGrid.x, particleConnectorPosGrid.y);

        //    particleMatricesCircleConnectionMatrices[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particleConnectorPos, particleConnectorRot, particleConnectorScale);
        //}
        //else
        //{
        //    // Contracted
        //    particleMatricesCircleExpanded[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //    particleMatricesCircleExpandedInner[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //    particleMatricesCircleConnectionMatrices[gd.graphics_listNumber][gd.graphics_listID] = matrixTRS_zero;
        //}
    }

    public void Render(ViewType viewType)
    {
        // 1. Update Properties
        float triggerTime = Time.timeSinceLevelLoad;
        float animationLength = Mathf.Clamp(RenderSystem.const_hexagonalAnimationDurationDef, 0f, Time.fixedDeltaTime);
        if (RenderSystem.flag_newRound)
        {
            // Update PropertyBlocks Timestamps (for Animations)
            propertyBlock_circle_contracted.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_expanded.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_expanding.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_contracting.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_connector_contracted.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_connector_expanded.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_connector_expanding.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_circle_connector_contracting.ApplyAnimationTimestamp(triggerTime, animationLength);
        }

        // 2. Render
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
        for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
        {
            int listLength;
            if (i == particleMatricesCircle_Contracted.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            //Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_Contracted[i], listLength, propertyBlock_HexParticles_Contracted.propertyBlock);
            //Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_Expanded1[i], listLength, propertyBlock_HexParticles_Expanded.propertyBlock);
            //Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_Expanded2[i], listLength, propertyBlock_HexParticles_Expanded.propertyBlock);
            //Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_ContractedToExpanded[i], listLength, propertyBlock_HexParticles_ContractedToExpanded.propertyBlock);
            //Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_ExpandedToContracted[i], listLength, propertyBlock_HexParticles_ExpandedToContracted.propertyBlock);

            //Graphics.DrawMeshInstanced(defaultHexagonCenter, 0, MaterialDatabase.material_hexagonal_particleCenter, particleMatricesHexBG[i], listLength);
            //Graphics.DrawMeshInstanced(defaultHexagonCenter, 0, MaterialDatabase.material_hexagonal_particleCenter, particleMatricesHexBGExpanded[i], listLength);
        }
    }

    private void Render_Circular()
    {
        // Connectors
        for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
        {
            // List Length
            int listLength;
            if (i == particleMatricesCircle_Contracted.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            // Particle Connectors
            Graphics.DrawMeshInstanced(mesh_circle_particleConnector, 0, MaterialDatabase.material_circular_particleCompleteConnector, particleMatricesCircle_ConnectionMatrices_Expanded[i], listLength, propertyBlock_circle_connector_expanded.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_circle_particleConnector, 0, MaterialDatabase.material_circular_particleCompleteConnector, particleMatricesCircle_ConnectionMatrices_Expanding[i], listLength, propertyBlock_circle_connector_expanding.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_circle_particleConnector, 0, MaterialDatabase.material_circular_particleCompleteConnector, particleMatricesCircle_ConnectionMatrices_Contracting[i], listLength, propertyBlock_circle_connector_contracting.propertyBlock);
        }

        // Particles
        for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
        {
            // List Length
            int listLength;
            if (i == particleMatricesCircle_Contracted.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            // Particles
            Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Contracted[i], listLength, propertyBlock_circle_contracted.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Expanded[i], listLength, propertyBlock_circle_expanded.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Expanding[i], listLength, propertyBlock_circle_expanding.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_circle_particle, 0, MaterialDatabase.material_circular_particleComplete, particleMatricesCircle_Contracting[i], listLength, propertyBlock_circle_contracting.propertyBlock);
        }
    }

}