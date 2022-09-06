using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RendererUI
{

    // References
    private AmoebotSimulator sim;

    // Input
    private InputManager input;
    private InputController inputController;

    // Graphical Data
    public Mesh mesh_baseHexagonBackground;
    public Material material_hexagonSelectionOverlay;
    public Material material_hexagonAddOverlay;
    public Material material_hexagonRemoveOverlay;
    public Material material_hexagonMoveOverlay;

    // State
    private ParticleSelectionState selectionState = ParticleSelectionState.NoSelection;
    private bool selectedParticle_expanded = false;
    private Vector2Int selectedParticle_pos1;
    private Vector2Int selectedParticle_pos2;

    public enum ParticleSelectionState
    {
        NoSelection, Selected, Moving, Add
    }

    public RendererUI(AmoebotSimulator sim, InputController inputController)
    {
        // Init References and Input
        this.sim = sim;
        this.input = InputManager.CreateInstance();
        this.inputController = inputController;

        // Add Callbacks
        this.input.clickActionEvent += ClickActionCallback;

        // Init Data
        this.mesh_baseHexagonBackground = MeshCreator_HexagonalView.GetMesh_BaseHexagonBackground();
        this.material_hexagonSelectionOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonSelectionMaterial;
        this.material_hexagonAddOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonAddMaterial;
        this.material_hexagonRemoveOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonRemoveMaterial;
        this.material_hexagonMoveOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonMoveMaterial;
    }

    public void ClickActionCallback(ClickAction action)
    {
        // Get Selection Data
        Vector2 mouseWorldPos = CameraUtils.MainCamera_Mouse_WorldPosition();
        Vector2Int mouseWorldField = AmoebotFunctions.GetGridPositionFromWorldPosition(AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(mouseWorldPos));
        UIHandler.UITool activeTool = sim.uiHandler.activeTool;
        Particle state_particleUnderPointer = null;
        bool state_pointerOverMap = EventSystem.current.IsPointerOverGameObject() == false;
        if (state_pointerOverMap)
        {
            // Show Particle Selection Overlay
            sim.system.TryGetParticleAt(mouseWorldField, out state_particleUnderPointer);
        }

        switch (action.clickButton)
        {
            case ClickAction.ClickButton.LeftMouse:
                switch (action.clickType)
                {
                    case ClickAction.ClickType.Click:
                        if(action.ongoing == false)
                        {
                            // Click has been executed
                            Log.Debug("Click!");
                            if (state_pointerOverMap)
                            {
                                if (state_particleUnderPointer != null)
                                {
                                    // Particle has been clicked
                                    switch (activeTool)
                                    {
                                        case UIHandler.UITool.Standard:
                                            // Select particle, open particle panel
                                            // Pause Simulation
                                            sim.PauseSim();
                                            // Update State
                                            selectionState = ParticleSelectionState.Selected;
                                            selectedParticle_expanded = state_particleUnderPointer.IsExpanded();
                                            selectedParticle_pos1 = state_particleUnderPointer.Head();
                                            selectedParticle_pos2 = state_particleUnderPointer.Tail();
                                            // Open Particle Panel
                                            if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.ShowParticlePanel(state_particleUnderPointer);
                                            break;
                                        case UIHandler.UITool.Add:
                                            break;
                                        case UIHandler.UITool.Remove:
                                            break;
                                        case UIHandler.UITool.Move:
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else
                                {
                                    // Empty Node has been clicked
                                    if(activeTool == UIHandler.UITool.Standard)
                                    {
                                        // Reset selection, close particle panel (if open)
                                        // Update State
                                        selectionState = ParticleSelectionState.NoSelection;
                                        // Close particle panel
                                        if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.ExitParticlePanel();
                                    }
                                }
                            }
                        }
                        break;
                    case ClickAction.ClickType.Drag:
                        // Drag has been executed
                        if(action.ongoing)
                        {
                            // Drag ongoing
                            Log.Debug("Dragging...");
                            currentlyDragging = true;
                            if(activeTool == UIHandler.UITool.Add)
                            {
                                // Sim must be paused to add particles
                                if(sim.running == false)
                                {
                                    // Validity Check (either one empty field or two empty fields next to each other selected)
                                    Vector2Int node1 = AmoebotFunctions.GetGridPositionFromWorldPosition(AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(action.positionStart));
                                    Vector2Int node2 = AmoebotFunctions.GetGridPositionFromWorldPosition(AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(action.positionTarget));
                                    Particle p1 = null;
                                    Particle p2 = null;
                                    sim.system.TryGetParticleAt(node1, out p1);
                                    sim.system.TryGetParticleAt(node2, out p2);
                                    if (node1 == node2 && p1 == null)
                                    {
                                        // Show Add Overlay (for single particle)
                                        Vector3 worldAbsPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(node1);
                                        Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                    }
                                    else if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && p1 == null && p2 == null)
                                    {
                                        // Show Add Overlay (for extended particle)
                                        Vector3 worldAbsPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(node1);
                                        Vector3 worldAbsPos2 = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(node2);
                                        Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                        Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos2 + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                    }
                                    else
                                    {
                                        // Show nothing (drag is too far apart or one node has a particle)
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Drag finished
                            Log.Debug("Drag finished.");
                        }
                        break;
                    default:
                        break;
                }
                break;
            case ClickAction.ClickButton.MiddleMouse:
                break;
            case ClickAction.ClickButton.RightMouse:
                break;
            default:
                break;
        }
    }
    private bool currentlyDragging = false;
    public void Render(ViewType viewType)
    {
        // Get Selection Data
        Vector2 mouseWorldPos = CameraUtils.MainCamera_Mouse_WorldPosition();
        Vector2Int mouseWorldField = AmoebotFunctions.GetGridPositionFromWorldPosition(AmoebotFunctions.NearestHexFieldWorldPositionFromWorldPosition(mouseWorldPos));
        UIHandler.UITool activeTool = sim.uiHandler.activeTool;
        Particle state_particleUnderPointer = null;
        bool state_pointerOverMap = EventSystem.current.IsPointerOverGameObject() == false;
        if (state_pointerOverMap)
        {
            // Show Particle Selection Overlay
            sim.system.TryGetParticleAt(mouseWorldField, out state_particleUnderPointer);
        }

        // Process Mouse Inputs (might execute the callbacks in this class)
        currentlyDragging = false;
        inputController.ManUpdate();

        // Particle Selection UI
        if (sim.system != null)
        {
            switch (activeTool)
            {
                case UIHandler.UITool.Standard:
                    if (state_pointerOverMap)
                    {
                        // Show Particle Selection Overlay
                        if (state_particleUnderPointer != null)
                        {
                            // Render Head Overlay
                            Vector3 worldPos_head = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(state_particleUnderPointer.Head());
                            Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                            // Render Tail Overlay
                            if (state_particleUnderPointer.IsExpanded())
                            {
                                Vector3 worldPos_tail = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(state_particleUnderPointer.Tail());
                                Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                            }
                        }
                    }
                    break;
                case UIHandler.UITool.Add:
                    if(state_pointerOverMap)
                    {
                        // Sim must be paused to add particles
                        if(sim.running == false)
                        {
                            if (state_particleUnderPointer == null && currentlyDragging == false)
                            {
                                // Empty Field Selected + Not dragging
                                // Show Add Overlay (for single particle)
                                Vector3 worldAbsPos = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(mouseWorldField);
                                Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                            }
                        }
                    }
                    break;
                case UIHandler.UITool.Remove:
                    if (state_pointerOverMap)
                    {
                        // Show Particle Selection Overlay
                        if (state_particleUnderPointer != null)
                        {
                            // Render Head Overlay
                            Vector3 worldPos_head = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(state_particleUnderPointer.Head());
                            Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonRemoveOverlay, 0);
                            // Render Tail Overlay
                            if (state_particleUnderPointer.IsExpanded())
                            {
                                Vector3 worldPos_tail = AmoebotFunctions.CalculateAmoebotCenterPositionVector3(state_particleUnderPointer.Tail());
                                Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonRemoveOverlay, 0);
                            }
                        }
                    }
                    break;
                case UIHandler.UITool.Move:
                    break;
                default:
                    break;
            }
            switch (selectionState)
            {
                case ParticleSelectionState.NoSelection:
                    break;
                case ParticleSelectionState.Selected:
                    break;
                case ParticleSelectionState.Moving:
                    break;
                case ParticleSelectionState.Add:
                    break;
                default:
                    break;
            }
        }
    }

}