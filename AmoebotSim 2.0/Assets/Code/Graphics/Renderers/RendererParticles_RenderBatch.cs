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
    // Matrices
    private List<Matrix4x4[]> particleMatricesCircle_Contracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_Expanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_Expanding = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_Contracting = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesPins_Contracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesPins_Expanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesPins_Expanding = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesPins_Contracting = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Contracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Expanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Expanding = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircle_ConnectionMatrices_Contracting = new List<Matrix4x4[]>();
    // Joint Movements
    private List<Vector3[]> particlePositionOffsets_jointMovementsInv = new List<Vector3[]>();
    private List<ParticleGraphicsAdapterImpl[]> particleReferences = new List<ParticleGraphicsAdapterImpl[]>();
    // PropertyBlocks
    private MaterialPropertyBlockData_Particles propertyBlock_circle_contracted = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_expanded = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_expanding = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_contracting = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_contracted = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_expanded = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_expanding = new MaterialPropertyBlockData_Particles();
    private MaterialPropertyBlockData_Particles propertyBlock_circle_connector_contracting = new MaterialPropertyBlockData_Particles();
    // Materials
    private Material circuitHexPinMaterial;
    private Material circuitHexCircPinMaterial;
    private Material hexagonWithPinsMaterial;
    private Material hexagonCircWithPinsMaterial;


    // Precalculated Data _____
    // Meshes
    private Mesh defaultQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    private Mesh mesh_circle_particle = MeshCreator_CircularView.GetMesh_ParticleOptimized();
    private Mesh mesh_circle_particleConnector = MeshCreator_CircularView.GetMesh_ParticleConnector();
    private Mesh mesh_hex_particle = MeshCreator_HexagonalView.GetMesh_MergingExpansionHexagon();
    private Mesh defaultQuadLeftSidePivot = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    private Mesh defaultHexagon = MeshCreator_HexagonalView.GetMesh_BaseExpansionHexagon();
    private Mesh defaultHexagonCenter = MeshCreator_HexagonalView.GetMesh_BaseHexagonBackground();
    // Matrix TRS Params
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

    // Dynamic Data _____
    private float curAnimationLength = 0f;
    private float jmInterpolation = 0f;

    // Settings _____
    public PropertyBlockData properties;

    /// <summary>
    /// An extendable struct that functions as the key for the mapping of particles to their render class.
    /// </summary>
    public struct PropertyBlockData
    {
        public Color color;
        public int pinsPerSide;

        public PropertyBlockData(Color color, int pinsPerMesh)
        {
            this.color = color;
            this.pinsPerSide = pinsPerMesh;
        }
    }

    public RendererParticles_RenderBatch(PropertyBlockData properties)
    {
        this.properties = properties;

        Init();
    }

    public void Init()
    {
        // PropertyBlocks
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

        // Circuit Pins
        // Generate Material
        circuitHexPinMaterial = TextureCreator.GetPinBorderMaterial(properties.pinsPerSide, ViewType.Hexagonal);
        circuitHexCircPinMaterial = TextureCreator.GetPinBorderMaterial(properties.pinsPerSide, ViewType.HexagonalCirc);
        hexagonWithPinsMaterial = TextureCreator.GetHexagonWithPinsMaterial(properties.pinsPerSide, ViewType.Hexagonal);
        hexagonCircWithPinsMaterial = TextureCreator.GetHexagonWithPinsMaterial(properties.pinsPerSide, ViewType.HexagonalCirc);
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
            particleMatricesPins_Contracted.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesPins_Expanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesPins_Expanding.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesPins_Contracting.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Contracted.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Expanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Expanding.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircle_ConnectionMatrices_Contracting.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particlePositionOffsets_jointMovementsInv.Add(new Vector3[maxArraySize]);
            particleReferences.Add(new ParticleGraphicsAdapterImpl[maxArraySize]);
        }

        // References
        graphicalData.graphics_listNumber = particleMatricesCircle_Contracted.Count - 1;
        graphicalData.graphics_listID = particleToParticleGraphicalDataMap.Count % maxArraySize;
        graphicalData.graphics_globalID = particleToParticleGraphicalDataMap.Count;
        graphicalData.graphics_color = properties.color;
        graphicalData.graphics_colorRenderer = this;

        // Register Particle
        particleToParticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
        graphicalDataList.Add(graphicalData);
        UpdateMatrix(graphicalData, false);

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
        // Update References
        int original_listNumber = lastParticleGraphicalData.graphics_listNumber;
        int original_listID = lastParticleGraphicalData.graphics_listID;
        lastParticleGraphicalData.graphics_listNumber = movedParticleIndex / maxArraySize;
        lastParticleGraphicalData.graphics_listID = movedParticleIndex % maxArraySize;
        lastParticleGraphicalData.graphics_globalID = movedParticleIndex;
        // Copy Matrices of lastParticleGraphicalData from original index to moved index
        CutAndCopyMatrices(original_listNumber, original_listID, lastParticleGraphicalData.graphics_listNumber, lastParticleGraphicalData.graphics_listID);

        // Unregister Particle
        graphicalDataList.RemoveAt(graphicalDataList.Count - 1);
        particleToParticleGraphicalDataMap.Remove(graphicalData.particle);
        // Lists became smaller, possibly delete list entry
        if (graphicalDataList.Count % maxArraySize == 0)
        {
            // Particle was only element in the last list
            // Delete list
            int indexOfLastList = particleMatricesCircle_Contracted.Count - 1;

            particleMatricesCircle_Contracted.RemoveAt(indexOfLastList);
            particleMatricesCircle_Expanded.RemoveAt(indexOfLastList);
            particleMatricesCircle_Expanding.RemoveAt(indexOfLastList);
            particleMatricesCircle_Contracting.RemoveAt(indexOfLastList);
            particleMatricesPins_Contracted.RemoveAt(indexOfLastList);
            particleMatricesPins_Expanded.RemoveAt(indexOfLastList);
            particleMatricesPins_Expanding.RemoveAt(indexOfLastList);
            particleMatricesPins_Contracting.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Contracted.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Expanded.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Expanding.RemoveAt(indexOfLastList);
            particleMatricesCircle_ConnectionMatrices_Contracting.RemoveAt(indexOfLastList);
            particlePositionOffsets_jointMovementsInv.RemoveAt(indexOfLastList);
            particleReferences.RemoveAt(indexOfLastList);

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
        particleMatricesPins_Contracted[moved_ListNumber][moved_ListID] = particleMatricesPins_Contracted[orig_ListNumber][orig_ListID];
        particleMatricesPins_Expanded[moved_ListNumber][moved_ListID] = particleMatricesPins_Expanded[orig_ListNumber][orig_ListID];
        particleMatricesPins_Expanding[moved_ListNumber][moved_ListID] = particleMatricesPins_Expanding[orig_ListNumber][orig_ListID];
        particleMatricesPins_Contracting[moved_ListNumber][moved_ListID] = particleMatricesPins_Contracting[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Contracted[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Contracted[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Expanded[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Expanded[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Expanding[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Expanding[orig_ListNumber][orig_ListID];
        particleMatricesCircle_ConnectionMatrices_Contracting[moved_ListNumber][moved_ListID] = particleMatricesCircle_ConnectionMatrices_Contracting[orig_ListNumber][orig_ListID];
        particlePositionOffsets_jointMovementsInv[moved_ListNumber][moved_ListID] = particlePositionOffsets_jointMovementsInv[orig_ListNumber][orig_ListID];
        particleReferences[moved_ListNumber][moved_ListID] = particleReferences[orig_ListNumber][orig_ListID];

        // 2. Reset Matrices
        ResetMatrices(orig_ListNumber, orig_ListID);
    }

    private void ResetMatrices(int listNumber, int listID)
    {
        particleMatricesCircle_Contracted[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_Expanded[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_Expanding[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_Contracting[listNumber][listID] = matrixTRS_zero;
        particleMatricesPins_Contracted[listNumber][listID] = matrixTRS_zero;
        particleMatricesPins_Expanded[listNumber][listID] = matrixTRS_zero;
        particleMatricesPins_Expanding[listNumber][listID] = matrixTRS_zero;
        particleMatricesPins_Contracting[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Contracted[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Expanded[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Expanding[listNumber][listID] = matrixTRS_zero;
        particleMatricesCircle_ConnectionMatrices_Contracting[listNumber][listID] = matrixTRS_zero;
        particlePositionOffsets_jointMovementsInv[listNumber][listID] = Vector3.zero;
        particleReferences[listNumber][listID] = null;
    }
    
    public void UpdateMatrix(ParticleGraphicsAdapterImpl gd, bool executeJointMovement)
    {
        Vector3 particle_position1world = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position1.x, gd.state_cur.position1.y, RenderSystem.zLayer_particles);
        Vector3 particle_position2world = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(gd.state_cur.position2.x, gd.state_cur.position2.y, RenderSystem.zLayer_particles);
        Vector3 pin_position1world = new Vector3(particle_position1world.x, particle_position1world.y, RenderSystem.zLayer_pins);
        Vector3 pin_position2world = new Vector3(particle_position2world.x, particle_position2world.y, RenderSystem.zLayer_pins);

        if (executeJointMovement)
        {
            // Interpolate
            Vector3 interpolatedOffset;
            if (RenderSystem.animationsOn) interpolatedOffset = (1f - jmInterpolation) * particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID];
            else interpolatedOffset = 0f * particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID];
            particle_position1world = particle_position1world + interpolatedOffset;
            particle_position2world = particle_position2world + interpolatedOffset;
            pin_position1world = new Vector3(particle_position1world.x, particle_position1world.y, RenderSystem.zLayer_pins);
            pin_position2world = new Vector3(particle_position2world.x, particle_position2world.y, RenderSystem.zLayer_pins);
        }
        else
        {
            // Reset Matrices
            ResetMatrices(gd.graphics_listNumber, gd.graphics_listID);
            particleReferences[gd.graphics_listNumber][gd.graphics_listID] = gd;

            // Joint Movements
            if (gd.state_cur.jointMovementState.isJointMovement)
            {
                // Calc Joint Movement Position Offset
                //Vector2Int jointMovementPositionOffsetInv = gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding ? gd.state_prev.position2 - gd.state_cur.position2 : gd.state_prev.position1 - gd.state_cur.position1;
                Vector2Int jointMovementPositionOffsetInv = new Vector2Int(-gd.state_cur.jointMovementState.jointExpansionOffset.x, -gd.state_cur.jointMovementState.jointExpansionOffset.y);
                // Set World Offset
                Vector3 absPositionOffsetInv = AmoebotFunctions.CalculateAmoebotCenterPositionVector2(jointMovementPositionOffsetInv);
                particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID] = absPositionOffsetInv;
            }
            else
            {
                particlePositionOffsets_jointMovementsInv[gd.graphics_listNumber][gd.graphics_listID] = Vector3.zero;
            }
        }
        
        // Calculate Rotation
        Quaternion rotation = Quaternion.identity;
        if (gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanded || gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding)
        {
            rotation = Quaternion.Euler(0f, 0f, 60f * gd.state_cur.globalExpansionOrContractionDir) * Quaternion.identity;
        }
        else if (gd.state_cur.movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting)
        {
            // Find Rotation Dir
            //Vector2Int curPosWithoutJointMovements1 = gd.state_cur.jointMovementState.isJointMovement ? gd.state_cur.position1 - gd.state_cur.jointMovementState.jointExpansionOffset : gd.state_cur.position1;
            //Vector2Int posOffset1 = curPosWithoutJointMovements1 - gd.state_prev.position1;
            //Vector2Int posOffset2 = curPosWithoutJointMovements1 - gd.state_prev.position2;
            //Vector2Int offsetVector = Vector2Int.zero;
            //bool contractsIntoPos1 = false;
            //if (posOffset1 != Vector2Int.zero && posOffset2 != Vector2Int.zero) Log.Error("Particle Render Batch: UpdateMatrix: PosOffsets are both not zero!");
            //else if (posOffset1 != Vector2Int.zero)
            //{
            //    offsetVector = posOffset1;
            //    contractsIntoPos1 = true;
            //}
            //else if (posOffset2 != Vector2Int.zero)
            //{
            //    offsetVector = posOffset2;
            //    contractsIntoPos1 = false;
            //}
            //else Log.Error("Particle Render Batch: UpdateMatrix: Both PosOffsets are zero!");
            //int expansionDir = 0;
            //if (offsetVector == new Vector2Int(1, 0)) expansionDir = 0;
            //else if (offsetVector == new Vector2Int(0, 1)) expansionDir = 1;
            //else if (offsetVector == new Vector2Int(-1, 1)) expansionDir = 2;
            //else if (offsetVector == new Vector2Int(-1, 0)) expansionDir = 3;
            //else if (offsetVector == new Vector2Int(0, -1)) expansionDir = 4;
            //else if (offsetVector == new Vector2Int(1, -1)) expansionDir = 5;
            //else Log.Error("Particle Render Batch: UpdateMatrix: offset vector (" + offsetVector.x + "," + offsetVector.y + ") is too big!");
            //if (contractsIntoPos1) expansionDir = (expansionDir + 3) % 6;
            // Apply Rotation
            rotation = Quaternion.Euler(0f, 0f, (60f * gd.state_cur.globalExpansionOrContractionDir) % 360f) * Quaternion.identity;
        }
        // Update Matrices
        ParticleGraphicsAdapterImpl.ParticleMovement movement = gd.state_cur.movement;
        if(RenderSystem.animationsOn == false)
        {
            if (movement == ParticleGraphicsAdapterImpl.ParticleMovement.Contracting) movement = ParticleGraphicsAdapterImpl.ParticleMovement.Contracted;
            else if (movement == ParticleGraphicsAdapterImpl.ParticleMovement.Expanding) movement = ParticleGraphicsAdapterImpl.ParticleMovement.Expanded;
        }
        switch (movement)
        {
            case ParticleGraphicsAdapterImpl.ParticleMovement.Contracted:
                particleMatricesCircle_Contracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position1world, rotation, Vector3.one);
                particleMatricesPins_Contracted[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position1world, rotation, Vector3.one);
                WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position1world);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Expanded:
                particleMatricesCircle_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world, rotation, Vector3.one);
                particleMatricesPins_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position2world, rotation, Vector3.one);
                particleMatricesCircle_ConnectionMatrices_Expanded[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world + new Vector3(0f, 0f, 0.1f), rotation, Vector3.one);
                WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position2world);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Expanding:
                particleMatricesCircle_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world, rotation, Vector3.one);
                particleMatricesPins_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position2world, rotation, Vector3.one);
                particleMatricesCircle_ConnectionMatrices_Expanding[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position2world + new Vector3(0f, 0f, 0.1f), rotation, Vector3.one);
                WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position2world);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Contracting:
                particleMatricesCircle_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position1world, rotation, Vector3.one);
                particleMatricesPins_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(pin_position1world, rotation, Vector3.one);
                particleMatricesCircle_ConnectionMatrices_Contracting[gd.graphics_listNumber][gd.graphics_listID] = Matrix4x4.TRS(particle_position1world + new Vector3(0f, 0f, 0.1f), rotation, Vector3.one);
                WorldSpaceUIHandler.instance.ParticleUpdate(gd.particle, particle_position1world);
                break;
            default:
                break;
        }

        
    }

    public void Render(ViewType viewType)
    {
        // 1. Update Properties
        curAnimationLength = Mathf.Clamp(RenderSystem.data_hexagonalAnimationDuration, 0f, Time.fixedDeltaTime);
        if (RenderSystem.flag_particleRoundOver)
        {
            RenderSystem.animation_animationTriggerTimestamp = Time.timeSinceLevelLoad;
            // Update PropertyBlocks Timestamps (for Animations)
            propertyBlock_circle_contracted.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_expanded.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_expanding.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_contracting.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_connector_contracted.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_connector_expanded.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_connector_expanding.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            propertyBlock_circle_connector_contracting.ApplyAnimationTimestamp(RenderSystem.animation_animationTriggerTimestamp, curAnimationLength);
            RenderSystem.data_particleMovementFinishedTimestamp = RenderSystem.animation_animationTriggerTimestamp + curAnimationLength;
        }

        // 2. Update Joint Movement Positions
        // Update Interpolation Value
        jmInterpolation = Engine.Library.InterpolationConstants.SmoothLerp(RenderSystem.animation_curAnimationPercentage);
        // Update all JM Positions
        for (int i = 0; i < particlePositionOffsets_jointMovementsInv.Count; i++)
        {
            Vector3[] offsets = particlePositionOffsets_jointMovementsInv[i];
            for (int j = 0; j < offsets.Length; j++)
            {
                Vector3 offset = offsets[j];
                if(offset != Vector3.zero)
                {
                    // We have a joint movement
                    // Interpolate
                    UpdateMatrix(particleReferences[i][j], true);
                }
            }
        }

        // 3. Render
        switch (viewType)
        {
            case ViewType.Hexagonal:
            case ViewType.HexagonalCirc:
                Render_Hexagonal(viewType);
                break;
            case ViewType.Circular:
                Render_Circular();
                break;
            default:
                break;
        }
    }

    private void Render_Hexagonal(ViewType viewType)
    {
        for (int i = 0; i < particleMatricesCircle_Contracted.Count; i++)
        {
            int listLength;
            if (i == particleMatricesCircle_Contracted.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            // Particles (previous mat: MaterialDatabase.material_hexagonal_particleCombined)
            Material mat = viewType == ViewType.Hexagonal ? hexagonWithPinsMaterial : hexagonCircWithPinsMaterial;
            Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Contracted[i], listLength, propertyBlock_circle_contracted.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Expanded[i], listLength, propertyBlock_circle_expanded.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Expanding[i], listLength, propertyBlock_circle_expanding.propertyBlock);
            Graphics.DrawMeshInstanced(mesh_hex_particle, 0, mat, particleMatricesCircle_Contracting[i], listLength, propertyBlock_circle_contracting.propertyBlock);
            // Pins
            if (RenderSystem.flag_showCircuitView)
            {
                Material matCirc = viewType == ViewType.Hexagonal ? circuitHexPinMaterial : circuitHexCircPinMaterial;
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Contracted[i], listLength, propertyBlock_circle_contracted.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Expanded[i], listLength, propertyBlock_circle_expanded.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Expanding[i], listLength, propertyBlock_circle_expanding.propertyBlock);
                Graphics.DrawMeshInstanced(mesh_hex_particle, 0, matCirc, particleMatricesPins_Contracting[i], listLength, propertyBlock_circle_contracting.propertyBlock);
            }
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