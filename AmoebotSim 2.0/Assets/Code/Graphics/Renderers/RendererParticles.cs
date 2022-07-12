using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererParticles
{

    public Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch> propertiesToRenderBatchMap = new Dictionary<RendererParticles_RenderBatch.PropertyBlockData, RendererParticles_RenderBatch>();


    // Data _____
    // Particles
    private Dictionary<IParticleState, ParticleGraphicsAdapterImpl> particleToParticleGraphicalDataMap = new Dictionary<IParticleState, ParticleGraphicsAdapterImpl>();
    // Graphics
    // Circle
    private List<Matrix4x4[]> particleMatricesCircle = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircleInner = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircleExpanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircleExpandedInner = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesCircleConnectionMatrices = new List<Matrix4x4[]>();
    // Hexagon
    private List<Matrix4x4[]> particleMatricesHex_Contracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesHex_Expanded1 = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesHex_Expanded2 = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesHex_ContractedToExpanded = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesHex_ExpandedToContracted = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesHexBG = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> particleMatricesHexBGExpanded = new List<Matrix4x4[]>();

    private List<Matrix4x4> particleMatricesSingle_Position1 = new List<Matrix4x4>();
    private List<Matrix4x4> particleMatricesSingle_Position2 = new List<Matrix4x4>();
    private List<MaterialPropertyBlockData_HexParticles> particlePropertyBlockSingle_Position1 = new List<MaterialPropertyBlockData_HexParticles>();
    private List<MaterialPropertyBlockData_HexParticles> particlePropertyBlockSingle_Position2 = new List<MaterialPropertyBlockData_HexParticles>();

    // MaterialPropertyBlocks
    private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_Contracted;
    private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_Expanded;
    private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_ContractedToExpanded;
    private MaterialPropertyBlockData_HexParticles propertyBlock_HexParticles_ExpandedToContracted;

    // Precalculated Data _____
    // Meshes
    private Mesh defaultQuad = Engine.Library.MeshConstants.getDefaultMeshQuad();
    private Mesh defaultQuadLeftSidePivot = Engine.Library.MeshConstants.getDefaultMeshQuad(new Vector2(0f, 0.5f));
    private Mesh defaultHexagon = MeshCreator_HexagonalView.GetMesh_BaseExpansionHexagon();
    private Mesh defaultHexagonCenter = MeshCreator_HexagonalView.GetMesh_BaseHexagonBackground();
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

    // Settings _____
    private bool useInstancedDrawing = true;
    private Color defaultColor = MaterialDatabase.material_circular_particleComplete.GetColor("_InputColor");

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
        // New System ___________________________________________________________

        RendererParticles_RenderBatch.PropertyBlockData block = new RendererParticles_RenderBatch.PropertyBlockData(graphicalData.graphics_color);
        // Add particle to existing/new RenderBatch
        if (propertiesToRenderBatchMap.ContainsKey(block))
        {
            // RenderBatch does already exist
            // Add particle to batch
            propertiesToRenderBatchMap[block].Particle_Add(graphicalData);
        }
        else
        {
            // RenderBatch does not exist
            // Create RenderBatch, add particle
            RendererParticles_RenderBatch renderBatch = new RendererParticles_RenderBatch(block);
            propertiesToRenderBatchMap.Add(block, renderBatch);
            renderBatch.Particle_Add(graphicalData);
        }



        // Old System ___________________________________________________________

        if (particleToParticleGraphicalDataMap.ContainsKey(graphicalData.particle)) return false;

        particleMatricesSingle_Position1.Add(matrixTRS_zero);
        particleMatricesSingle_Position2.Add(matrixTRS_zero);
        MaterialPropertyBlockData_HexParticles propertyBlock = new MaterialPropertyBlockData_HexParticles();
        propertyBlock.ApplyUpdatedValues(false, 0, 0f, 0f);
        particlePropertyBlockSingle_Position1.Add(new MaterialPropertyBlockData_HexParticles());
        propertyBlock = new MaterialPropertyBlockData_HexParticles();
        propertyBlock.ApplyUpdatedValues(true, 3, 1f, 1f);
        particlePropertyBlockSingle_Position2.Add(new MaterialPropertyBlockData_HexParticles());

        if (((particleToParticleGraphicalDataMap.Count + 1) % maxArraySize) == 1)
        {
            // Matrices
            particleMatricesCircle.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircleInner.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircleExpanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircleExpandedInner.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesCircleConnectionMatrices.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHex_Contracted.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHex_Expanded1.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHex_Expanded2.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHex_ContractedToExpanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHex_ExpandedToContracted.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHexBG.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            particleMatricesHexBGExpanded.Add(Engine.Library.MatrixConstants.GetMatrix4x4Array(maxArraySize, matrixTRS_zero));
            // Property Blocks
            propertyBlock_HexParticles_Contracted = new MaterialPropertyBlockData_HexParticles();
            propertyBlock_HexParticles_Contracted.ApplyUpdatedValues(false, 0, 0f, 0f);
            propertyBlock_HexParticles_Expanded = new MaterialPropertyBlockData_HexParticles();
            propertyBlock_HexParticles_Expanded.ApplyUpdatedValues(true, 3, 1f, 1f); // gap to the right so that we can rotate the mesh
            propertyBlock_HexParticles_ContractedToExpanded = new MaterialPropertyBlockData_HexParticles();
            propertyBlock_HexParticles_ContractedToExpanded.ApplyUpdatedValues(true, 3, 0f, 1f);
            propertyBlock_HexParticles_ExpandedToContracted = new MaterialPropertyBlockData_HexParticles();
            propertyBlock_HexParticles_ExpandedToContracted.ApplyUpdatedValues(true, 3, 1f, 0f);
        }
        graphicalData.graphics_listNumber = particleMatricesCircle.Count - 1;
        graphicalData.graphics_listID = particleToParticleGraphicalDataMap.Count % maxArraySize;
        graphicalData.graphics_globalID = particleToParticleGraphicalDataMap.Count;

        particleToParticleGraphicalDataMap.Add(graphicalData.particle, graphicalData);
        return true;
    }

    public void Particle_Remove(IParticleState particle)
    {
        if (particleToParticleGraphicalDataMap.ContainsKey(particle)) particleToParticleGraphicalDataMap.Remove(particle);

        throw new System.NotImplementedException();
        // We would need to implement the removal of the graphics here, but let us say for the prototype we do not need this.
    }

    public bool UpdateParticleColor(ParticleGraphicsAdapterImpl gd, Color oldColor, Color color)
    {
        if (oldColor == color) return false;

        // Remove particle from old RenderBatch
        propertiesToRenderBatchMap[new RendererParticles_RenderBatch.PropertyBlockData(oldColor)].Particle_Remove(gd);

        // Add particle to new RenderBatch
        gd.graphics_color = color;
        Particle_Add(gd);

        return true;
    }

    public void UpdateMatrix(ParticleGraphicsAdapterImpl graphicalData)
    {
        particleMatricesCircle[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
        particleMatricesCircleInner[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));
        particleMatricesHexBG[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.ZLayer_particlesBG), Quaternion.identity, Vector3.one);
        particleMatricesHexBGExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.ZLayer_particlesBG), Quaternion.identity, Vector3.one);

        particleMatricesHex_Contracted[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
        particleMatricesHex_Expanded1[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
        particleMatricesHex_Expanded2[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
        particleMatricesHex_ContractedToExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
        particleMatricesHex_ExpandedToContracted[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;

        switch (graphicalData.state_cur.movement)
        {
            case ParticleGraphicsAdapterImpl.ParticleMovement.Contracted:
                particleMatricesHex_Contracted[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                
                particleMatricesSingle_Position1[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particleMatricesSingle_Position2[graphicalData.graphics_globalID] = matrixTRS_zero;
                particlePropertyBlockSingle_Position1[graphicalData.graphics_globalID].ApplyUpdatedValues(false, graphicalData.state_cur.globalExpansionDir, 0f, 0f);
                particlePropertyBlockSingle_Position2[graphicalData.graphics_globalID].ApplyUpdatedValues(false, graphicalData.state_cur.globalExpansionDir, 0f, 0f);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Expanded:
                float rotationDegrees = graphicalData.state_cur.globalExpansionDir * 60f;
                particleMatricesHex_Expanded1[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees), Vector3.one);
                particleMatricesHex_Expanded2[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees + 180f), Vector3.one);

                particleMatricesSingle_Position1[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particleMatricesSingle_Position2[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particlePropertyBlockSingle_Position1[graphicalData.graphics_globalID].ApplyUpdatedValues(true, (graphicalData.state_cur.globalExpansionDir + 3) % 6, 1f, 1f);
                particlePropertyBlockSingle_Position2[graphicalData.graphics_globalID].ApplyUpdatedValues(true, graphicalData.state_cur.globalExpansionDir, 1f, 1f);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Expanding:
                float rotationDegrees2 = graphicalData.state_cur.globalExpansionDir * 60f;
                particleMatricesHex_ContractedToExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees2), Vector3.one);
                particleMatricesHex_Expanded2[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees2 + 180f), Vector3.one);

                particleMatricesSingle_Position1[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particleMatricesSingle_Position2[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particlePropertyBlockSingle_Position1[graphicalData.graphics_globalID].ApplyUpdatedValues(true, (graphicalData.state_cur.globalExpansionDir + 3) % 6, 0f, 1f);
                particlePropertyBlockSingle_Position2[graphicalData.graphics_globalID].ApplyUpdatedValues(true, graphicalData.state_cur.globalExpansionDir, 1f, 1f);
                break;
            case ParticleGraphicsAdapterImpl.ParticleMovement.Contracting:
                float rotationDegrees3 = graphicalData.state_prev.globalExpansionDir * 60f;
                particleMatricesHex_Expanded1[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees3), Vector3.one);
                particleMatricesHex_ExpandedToContracted[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity * Quaternion.Euler(0f, 0f, rotationDegrees3 + 180f), Vector3.one);

                particleMatricesSingle_Position1[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position1.x, graphicalData.state_cur.position1.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particleMatricesSingle_Position2[graphicalData.graphics_globalID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
                particlePropertyBlockSingle_Position1[graphicalData.graphics_globalID].ApplyUpdatedValues(true, (graphicalData.state_prev.globalExpansionDir + 3) % 6, 1f, 1f);
                particlePropertyBlockSingle_Position2[graphicalData.graphics_globalID].ApplyUpdatedValues(true, graphicalData.state_prev.globalExpansionDir, 1f, 0f);
                break;
            default:
                break;
        }

        if (graphicalData.state_cur.isExpanded)
        {
            // Expanded
            particleMatricesCircleExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, Vector3.one);
            particleMatricesCircleExpandedInner[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(AmoebotFunctions.CalculateAmoebotCenterPositionVector3(graphicalData.state_cur.position2.x, graphicalData.state_cur.position2.y, RenderSystem.zLayer_particles), Quaternion.identity, new Vector3(innerParticleScaleFactor, innerParticleScaleFactor, 1f));

            Vector2Int particleConnectorPosGrid = Vector2Int.zero;
            Quaternion particleConnectorRot = Quaternion.identity;
            Vector3 particleConnectorScale = Vector3.one;
            if (graphicalData.state_cur.globalExpansionDir >= 0 && graphicalData.state_cur.globalExpansionDir <= 2)
            {
                // position2 is the node from which the connection originates
                particleConnectorPosGrid = graphicalData.state_cur.position2;
                switch (graphicalData.state_cur.globalExpansionDir)
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
                particleConnectorPosGrid = graphicalData.state_cur.position1;
                switch (graphicalData.state_cur.globalExpansionDir)
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

            particleMatricesCircleConnectionMatrices[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = Matrix4x4.TRS(particleConnectorPos, particleConnectorRot, particleConnectorScale);
        }
        else
        {
            // Contracted
            particleMatricesCircleExpanded[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
            particleMatricesCircleExpandedInner[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
            particleMatricesCircleConnectionMatrices[graphicalData.graphics_listNumber][graphicalData.graphics_listID] = matrixTRS_zero;
        }
    }

    public void Render(ViewType viewType)
    {
        bool useNewSystem = true;

        // New System _______________________________________________________
        if(useNewSystem) foreach (var item in propertiesToRenderBatchMap.Values)
        {
            item.Render(viewType);
        }

        // Old System _______________________________________________________
        if(!useNewSystem) switch (viewType)
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
        float triggerTime = Time.timeSinceLevelLoad;
        float animationLength = Mathf.Clamp(RenderSystem.const_hexagonalAnimationDurationDef, 0f, Time.fixedDeltaTime);

        if (RenderSystem.flag_newRound)
        {
            // Update PropertyBlocks (for Animations)
            propertyBlock_HexParticles_Contracted.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_HexParticles_Expanded.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_HexParticles_ContractedToExpanded.ApplyAnimationTimestamp(triggerTime, animationLength);
            propertyBlock_HexParticles_ExpandedToContracted.ApplyAnimationTimestamp(triggerTime, animationLength);
        }

        if(useInstancedDrawing)
        {
            for (int i = 0; i < particleMatricesCircle.Count; i++)
            {
                int listLength;
                if (i == particleMatricesCircle.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
                else listLength = maxArraySize;

                Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_Contracted[i], listLength, propertyBlock_HexParticles_Contracted.propertyBlock);
                Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_Expanded1[i], listLength, propertyBlock_HexParticles_Expanded.propertyBlock);
                Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_Expanded2[i], listLength, propertyBlock_HexParticles_Expanded.propertyBlock);
                Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_ContractedToExpanded[i], listLength, propertyBlock_HexParticles_ContractedToExpanded.propertyBlock);
                Graphics.DrawMeshInstanced(defaultHexagon, 0, MaterialDatabase.material_hexagonal_particleExpansion, particleMatricesHex_ExpandedToContracted[i], listLength, propertyBlock_HexParticles_ExpandedToContracted.propertyBlock);

                //Graphics.DrawMeshInstanced(defaultHexagonCenter, 0, MaterialDatabase.material_hexagonal_particleCenter, particleMatricesHexBG[i], listLength);
                //Graphics.DrawMeshInstanced(defaultHexagonCenter, 0, MaterialDatabase.material_hexagonal_particleCenter, particleMatricesHexBGExpanded[i], listLength);
            }
        }
        else
        {

            for (int i = 0; i < particleMatricesSingle_Position1.Count; i++)
            {
                if (RenderSystem.flag_newRound)
                {
                    // Update PropertyBlocks (for Animations)
                    particlePropertyBlockSingle_Position1[i].ApplyAnimationTimestamp(triggerTime, animationLength);
                    particlePropertyBlockSingle_Position2[i].ApplyAnimationTimestamp(triggerTime, animationLength);
                }

                Graphics.DrawMesh(defaultHexagon, particleMatricesSingle_Position1[i], MaterialDatabase.material_hexagonal_particleExpansion, 0, null, 0, particlePropertyBlockSingle_Position1[i].propertyBlock);
                Graphics.DrawMesh(defaultHexagon, particleMatricesSingle_Position2[i], MaterialDatabase.material_hexagonal_particleExpansion, 0, null, 0, particlePropertyBlockSingle_Position2[i].propertyBlock);
            }
        }
    }

    private void Render_Circular()
    {
        for (int i = 0; i < particleMatricesCircle.Count; i++)
        {
            // Test
            //Graphics.DrawMeshInstanced(defaultQuad, 0, material_particle, new Matrix4x4[] { Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(10f, 10f, 1f)) });
            
            int listLength;
            if (i == particleMatricesCircle.Count - 1) listLength = particleToParticleGraphicalDataMap.Count % maxArraySize;
            else listLength = maxArraySize;

            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particle, particleMatricesCircle[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particleCenter, particleMatricesCircleInner[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particle, particleMatricesCircleExpanded[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuad, 0, MaterialDatabase.material_circular_particleCenter, particleMatricesCircleExpandedInner[i], listLength);
            Graphics.DrawMeshInstanced(defaultQuadLeftSidePivot, 0, MaterialDatabase.material_circular_particleConnector, particleMatricesCircleConnectionMatrices[i], listLength);
        }
    }

}