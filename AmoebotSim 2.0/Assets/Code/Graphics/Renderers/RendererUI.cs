using System.Collections;
using System.Collections.Generic;
using AS2.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AS2.Visuals
{

    /// <summary>
    /// Renderer for the UI.
    /// Basically draws the overlay for the currently selected tool over the particles
    /// and forwards the current tool input to the corresponding handler.
    /// </summary>
    public class RendererUI
    {

        // References
        private AmoebotSimulator sim;

        // Input
        private InputManager input;
        private InputController inputController;

        // Helpers
        private PSetDragHandler pSetDragHandler;

        // Graphical Data
        public Mesh mesh_baseHexagonBackground;
        public Material material_hexagonSelectionOverlay;
        public Material material_hexagonAddOverlay;
        public Material material_hexagonRemoveOverlay;
        public Material material_hexagonMoveOverlay;
        public Material material_hexagonMoveSelectionOverlay;

        // State
        private bool moveToolParticleSelected = false;
        private Vector2Int moveToolParticlePosition;

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
            this.pSetDragHandler = new PSetDragHandler(sim);

            // Add Callbacks
            this.input.clickActionEvent += ClickActionCallback;

            // Init Data
            this.mesh_baseHexagonBackground = MeshCreator_HexagonalView.GetMesh_BaseHexagonBackground();
            this.material_hexagonSelectionOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonSelectionMaterial;
            this.material_hexagonAddOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonAddMaterial;
            this.material_hexagonRemoveOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonRemoveMaterial;
            this.material_hexagonMoveOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonMoveMaterial;
            this.material_hexagonMoveSelectionOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonMoveSelectionMaterial;
        }

        /// <summary>
        /// Called when a click or a drag (also ongoing drag) is executed.
        /// Handles all the actions that should be executed when this happens
        /// (e.g., selecting a particle, adding a particle etc.).
        /// </summary>
        /// <param name="action">The click or drag action that occurred.</param>
        public void ClickActionCallback(ClickAction action)
        {
            // Get Selection Data
            Vector2 mouseWorldPos = CameraUtils.MainCamera_Mouse_WorldPosition();
            Vector2Int mouseWorldField = AmoebotFunctions.WorldToGridPosition(mouseWorldPos);
            UIHandler.UITool activeTool = sim.uiHandler.activeTool;
            IParticleState state_particleUnderPointer = null;
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
                            if (action.ongoing == false)
                            {
                                // Click has been executed
                                //Log.Debug("Click!");
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
                                                if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.Open(state_particleUnderPointer);
                                                break;
                                            case UIHandler.UITool.Add:
                                                // empty
                                                break;
                                            case UIHandler.UITool.Remove:
                                                // Close Particle Panel (just in case)
                                                if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.Close();
                                                // Remove particle
                                                sim.system.RemoveParticle(state_particleUnderPointer);
                                                break;
                                            case UIHandler.UITool.Move:
                                                // Select particle for movement
                                                // Pause Simulation
                                                sim.PauseSim();
                                                // Select
                                                moveToolParticleSelected = true;
                                                moveToolParticlePosition = mouseWorldField;
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        // Empty Node has been clicked
                                        switch (activeTool)
                                        {
                                            case UIHandler.UITool.Standard:
                                                // Reset selection, close particle panel (if open)
                                                // Update State
                                                selectionState = ParticleSelectionState.NoSelection;
                                                // Close particle panel
                                                if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.Close();
                                                break;
                                            case UIHandler.UITool.Add:
                                                // Pause Simulation
                                                sim.PauseSim();
                                                // Add particle at position
                                                if (sim.uiHandler.initializationUI.IsOpen())
                                                    sim.system.AddParticleContracted(mouseWorldField, sim.uiHandler.GetDropdownValue_Chirality(), sim.uiHandler.GetDropdownValue_Compass());
                                                else
                                                    sim.system.AddParticleContracted(mouseWorldField);
                                                break;
                                            case UIHandler.UITool.Remove:
                                                // magikarp uses splash, nothing happens
                                                break;
                                            case UIHandler.UITool.Move:
                                                // If particle has been selected and can be moved, then move it
                                                if (moveToolParticleSelected)
                                                {
                                                    IParticleState selectedParticle;
                                                    sim.system.TryGetParticleAt(moveToolParticlePosition, out selectedParticle);
                                                    if (selectedParticle != null)
                                                    {
                                                        sim.system.MoveParticleToNewContractedPosition(selectedParticle, mouseWorldField);
                                                        moveToolParticleSelected = false;
                                                    }
                                                    else
                                                    {
                                                        // Should not happen
                                                        Log.Error("UIRenderer: Cached move tool particle is not accessible, it must have been deleted or moved.");
                                                    }
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                        if (activeTool == UIHandler.UITool.Standard)
                                        {

                                        }
                                    }
                                }
                            }
                            break;
                        case ClickAction.ClickType.Drag:
                            // Drag has been executed
                            Vector2Int node1 = AmoebotFunctions.WorldToGridPosition(action.positionStart);
                            Vector2Int node2 = AmoebotFunctions.WorldToGridPosition(action.positionTarget);
                            IParticleState p1 = null;
                            IParticleState p2 = null;
                            sim.system.TryGetParticleAt(node1, out p1);
                            sim.system.TryGetParticleAt(node2, out p2);
                            if (action.ongoing)
                            {
                                // Drag ongoing
                                switch (activeTool)
                                {
                                    case UIHandler.UITool.Standard:
                                        break;
                                    case UIHandler.UITool.Add:
                                        // Sim must be paused to add particles
                                        if (sim.running == false)
                                        {
                                            // Validity Check (either one empty field or two empty fields next to each other selected)
                                            if (node1 == node2 && p1 == null)
                                            {
                                                // Show Add Overlay (for single particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                            }
                                            else if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && p1 == null && p2 == null)
                                            {
                                                // Show Add Overlay (for extended particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                Vector3 worldAbsPos2 = AmoebotFunctions.GridToWorldPositionVector3(node2);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos2 + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                            }
                                            else
                                            {
                                                // Show nothing (drag is too far apart or one node has a particle)
                                            }
                                        }
                                        break;
                                    case UIHandler.UITool.Remove:
                                        break;
                                    case UIHandler.UITool.Move:
                                        // Move Overlay
                                        if (sim.running == false)
                                        {
                                            // Validity Check (either one empty field or two empty fields next to each other selected)
                                            if (node1 == node2 && p1 == null)
                                            {
                                                // Show Move Overlay (for single particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                                            }
                                            else if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && p1 == null && p2 == null)
                                            {
                                                // Show Move Overlay (for extended particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                Vector3 worldAbsPos2 = AmoebotFunctions.GridToWorldPositionVector3(node2);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos2 + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                                            }
                                            else
                                            {
                                                // Show nothing (drag is too far apart or one node has a particle)
                                            }
                                        }
                                        break;
                                    case UIHandler.UITool.PSetMove:
                                        // Partition Set Movement
                                        pSetDragHandler.DragEvent_Ongoing(action.positionStart, action.positionTarget);
                                        break;
                                    default:
                                        break;
                                }
                                currentlyDragging = true;
                            }
                            else
                            {
                                // Drag finished
                                switch (activeTool)
                                {
                                    case UIHandler.UITool.Standard:
                                        // empty
                                        break;
                                    case UIHandler.UITool.Add:
                                        if (node1 == node2)
                                        {
                                            // One node selected
                                            if (p1 == null)
                                            {
                                                // Pause Simulation
                                                sim.PauseSim();
                                                // Add particle at position
                                                if (sim.uiHandler.initializationUI.IsOpen()) sim.system.AddParticleContracted(mouseWorldField, sim.uiHandler.GetDropdownValue_Chirality(), sim.uiHandler.GetDropdownValue_Compass());
                                                else sim.system.AddParticleContracted(mouseWorldField);
                                            }
                                        }
                                        else
                                        {
                                            // Multiple nodes selected
                                            if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && p1 == null && p2 == null)
                                            {
                                                // Neighboring nodes which are empty
                                                // Pause Simulation
                                                sim.PauseSim();
                                                // Add particle at position
                                                if (sim.uiHandler.initializationUI.IsOpen()) sim.system.AddParticleExpanded(node2, node1, sim.uiHandler.GetDropdownValue_Chirality(), sim.uiHandler.GetDropdownValue_Compass());
                                                else sim.system.AddParticleExpanded(node2, node1); // mouse movement from tail to head
                                            }
                                        }
                                        break;
                                    case UIHandler.UITool.Remove:
                                        // empty
                                        break;
                                    case UIHandler.UITool.Move:
                                        // If particle has been selected and can be moved, then move it
                                        if (moveToolParticleSelected)
                                        {
                                            IParticleState selectedParticle;
                                            sim.system.TryGetParticleAt(moveToolParticlePosition, out selectedParticle);
                                            if (selectedParticle != null)
                                            {
                                                if (node1 == node2)
                                                {
                                                    // One node selected
                                                    if (p1 == null)
                                                    {
                                                        // Pause Simulation
                                                        sim.PauseSim();
                                                        // Move Particle
                                                        sim.system.MoveParticleToNewContractedPosition(selectedParticle, mouseWorldField);
                                                        moveToolParticleSelected = false;
                                                    }
                                                }
                                                else
                                                {
                                                    // Multiple nodes selected
                                                    if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && p1 == null && p2 == null)
                                                    {
                                                        // Neighboring nodes which are empty
                                                        // Pause Simulation
                                                        sim.PauseSim();
                                                        // Move Particle
                                                        sim.system.MoveParticleToNewExpandedPosition(selectedParticle, node2, node1); // mouse movement from tail to head
                                                        moveToolParticleSelected = false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // Should not happen
                                                Log.Error("UIRenderer: Cached move tool particle is not accessible, it must have been deleted or moved.");
                                            }

                                    }
                                    break;
                                case UIHandler.UITool.PSetMove:
                                    // Partition Set Movement
                                    pSetDragHandler.DragEvent_Finished(action.positionStart, action.positionTarget);
                                    break;
                                default:
                                    break;
                            }
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

        /// <summary>
        /// The render loop of the renderer.
        /// Here the mostly hexagonal overlays are drawn based on the current
        /// state of the renderer UI and the current mouse position.
        /// </summary>
        /// <param name="viewType">The current view type used to
        /// visualize the particle system.</param>
        public void Render(ViewType viewType)
        {
            // Get Selection Data
            Vector2 mouseWorldPos = CameraUtils.MainCamera_Mouse_WorldPosition();
            Vector2Int mouseWorldField = AmoebotFunctions.WorldToGridPosition(mouseWorldPos);
            UIHandler.UITool activeTool = sim.uiHandler.activeTool;
            IParticleState state_particleUnderPointer = null;
            bool state_pointerOverMap = EventSystem.current.IsPointerOverGameObject() == false;
            if (state_pointerOverMap)
            {
                // Show Particle Selection Overlay
                sim.system.TryGetParticleAt(mouseWorldField, out state_particleUnderPointer);
            }

            // Update move tool state if tool is not selected anymore or sim is running
            if (activeTool != UIHandler.UITool.Move || sim.running)
            {
                moveToolParticleSelected = false;
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
                        if (state_pointerOverMap && sim.uiHandler.particleUI.IsOpen() == false)
                        {
                            // Show Particle Selection Overlay
                            if (state_particleUnderPointer != null)
                            {
                                // Render Head Overlay
                                Vector3 worldPos_head = AmoebotFunctions.GridToWorldPositionVector3(state_particleUnderPointer.Head());
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                                // Render Tail Overlay
                                if (state_particleUnderPointer.IsExpanded())
                                {
                                    Vector3 worldPos_tail = AmoebotFunctions.GridToWorldPositionVector3(state_particleUnderPointer.Tail());
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                                }
                            }
                        }
                        else if (sim.uiHandler.particleUI.IsOpen())
                        {
                            IParticleState activeParticle = sim.uiHandler.particleUI.GetShownParticle();
                            if (activeParticle != null)
                            {
                                Vector3 worldPos_head = AmoebotFunctions.GridToWorldPositionVector3(activeParticle.Head());
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                                // Render Tail Overlay
                                if (activeParticle.IsExpanded())
                                {
                                    Vector3 worldPos_tail = AmoebotFunctions.GridToWorldPositionVector3(activeParticle.Tail());
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonSelectionOverlay, 0);
                                }
                            }
                        }
                        break;
                    case UIHandler.UITool.Add:
                        if (state_pointerOverMap)
                        {
                            // Sim must be paused to add particles
                            if (sim.running == false)
                            {
                                if (state_particleUnderPointer == null && currentlyDragging == false)
                                {
                                    // Empty Field Selected + Not dragging
                                    // Show Add Overlay (for single particle)
                                    Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(mouseWorldField);
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
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
                                Vector3 worldPos_head = AmoebotFunctions.GridToWorldPositionVector3(state_particleUnderPointer.Head());
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonRemoveOverlay, 0);
                                // Render Tail Overlay
                                if (state_particleUnderPointer.IsExpanded())
                                {
                                    Vector3 worldPos_tail = AmoebotFunctions.GridToWorldPositionVector3(state_particleUnderPointer.Tail());
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonRemoveOverlay, 0);
                                }
                            }
                        }
                        break;
                    case UIHandler.UITool.Move:
                        if (moveToolParticleSelected || (state_pointerOverMap && state_particleUnderPointer != null))
                        {
                            // Mark Selected Particle
                            IParticleState selectedParticle;
                            if (moveToolParticleSelected) sim.system.TryGetParticleAt(moveToolParticlePosition, out selectedParticle);
                            else selectedParticle = state_particleUnderPointer;
                            if (selectedParticle != null)
                            {
                                Vector3 worldPos_head = AmoebotFunctions.GridToWorldPositionVector3(selectedParticle.Head());
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_head + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveSelectionOverlay, 0);
                                // Render Tail Overlay
                                if (selectedParticle.IsExpanded())
                                {
                                    Vector3 worldPos_tail = AmoebotFunctions.GridToWorldPositionVector3(selectedParticle.Tail());
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos_tail + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveSelectionOverlay, 0);
                                }
                            }
                            else
                            {
                                // Should not happen
                                Log.Error("UIRenderer: Cached move tool particle is not accessible, it must have been deleted or moved.");
                            }
                        }
                        // Mark Moving Position
                        if (moveToolParticleSelected && currentlyDragging == false)
                        {
                            if (state_pointerOverMap && state_particleUnderPointer == null)
                            {
                                Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(mouseWorldField);
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                            }
                        }
                        break;
                    case UIHandler.UITool.PSetMove:
                        pSetDragHandler.Update(mouseWorldPos, mouseWorldField);
                        break;
                    default:
                        break;
                }
            }
        }

    }

}
