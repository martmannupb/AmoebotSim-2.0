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
        public Material material_hexagonAddObjectOverlay;
        public Material material_hexagonRemoveOverlay;
        public Material material_hexagonMoveOverlay;
        public Material material_hexagonMoveSelectionOverlay;
        public Material material_objectSelectionOverlay;

        // State
        private bool moveToolParticleSelected = false;
        private bool moveToolObjectSelected = false;
        private Vector2Int moveToolParticlePosition;
        private IObjectInfo moveToolSelectedObject;
        private Vector2Int moveToolObjectOffset;
        private bool addToolObjectSelected = false;
        private IObjectInfo addToolSelectedObject;


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
            this.material_hexagonAddObjectOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonAddObjectMaterial;
            this.material_hexagonRemoveOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonRemoveMaterial;
            this.material_hexagonMoveOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonMoveMaterial;
            this.material_hexagonMoveSelectionOverlay = MaterialDatabase.material_hexagonal_ui_baseHexagonMoveSelectionMaterial;
            this.material_objectSelectionOverlay = MaterialDatabase.material_object_ui;
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
            IObjectInfo state_objectUnderPointer = null;
            bool state_pointerOverMap = EventSystem.current.IsPointerOverGameObject() == false;
            if (state_pointerOverMap)
            {
                // Show Particle Selection Overlay
                bool foundParticle = sim.system.TryGetParticleAt(mouseWorldField, out state_particleUnderPointer);
                if (!foundParticle)
                    sim.system.TryGetObjectAt(mouseWorldField, out state_objectUnderPointer);
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
                                                // Close Object Panel
                                                if (ObjectUIHandler.instance != null) ObjectUIHandler.instance.Close();
                                                // Open Particle Panel
                                                if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.Open(state_particleUnderPointer);
                                                break;
                                            case UIHandler.UITool.Add:
                                                // empty
                                                break;
                                            case UIHandler.UITool.Remove:
                                                // Close Particle Panel (just in case)
                                                if (ParticleUIHandler.instance != null && ParticleUIHandler.instance.GetShownParticle() == state_particleUnderPointer)
                                                    ParticleUIHandler.instance.Close();
                                                // Remove particle
                                                sim.system.RemoveParticle(state_particleUnderPointer);
                                                break;
                                            case UIHandler.UITool.Move:
                                                // Select particle for movement (if no particle selected yet)
                                                // Otherwise move selected (expanded) particle here
                                                bool movedParticle = false;
                                                if (moveToolParticleSelected)
                                                {
                                                    IParticleState selectedParticle;
                                                    sim.system.TryGetParticleAt(moveToolParticlePosition, out selectedParticle);
                                                    if (state_particleUnderPointer == selectedParticle && selectedParticle.IsExpanded())
                                                    {
                                                        sim.system.MoveParticleToNewContractedPosition(selectedParticle, mouseWorldField);
                                                        moveToolParticleSelected = false;
                                                        movedParticle = true;
                                                    }
                                                }
                                                if (movedParticle == false)
                                                {
                                                    // Pause Simulation
                                                    sim.PauseSim();
                                                    // Select
                                                    ResetSelection();
                                                    moveToolParticleSelected = true;
                                                    moveToolParticlePosition = mouseWorldField;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else if (state_objectUnderPointer != null)
                                    {
                                        // Object has been clicked
                                        switch (activeTool)
                                        {
                                            case UIHandler.UITool.Standard:
                                                // Select object, open object panel
                                                // Pause Simulation
                                                sim.PauseSim();
                                                // Close Particle Panel
                                                if (ParticleUIHandler.instance != null) ParticleUIHandler.instance.Close();
                                                // Open Object Panel
                                                if (ObjectUIHandler.instance != null) ObjectUIHandler.instance.Open(state_objectUnderPointer);
                                                break;
                                            case UIHandler.UITool.Remove:
                                                // If Shift is held down: Remove the whole object
                                                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                                                {
                                                    // Remove the whole object
                                                    if (ObjectUIHandler.instance != null && ObjectUIHandler.instance.IsOpen() && ObjectUIHandler.instance.GetShownObject() == state_objectUnderPointer)
                                                        ObjectUIHandler.instance.Close();
                                                    // Remove object from the system
                                                    state_objectUnderPointer.RemoveFromSystem();
                                                }
                                                else
                                                    TryRemoveObject(state_objectUnderPointer, mouseWorldField, true);
                                                break;
                                            case UIHandler.UITool.Move:
                                                // Clicked object for movement
                                                // Pause Simulation
                                                sim.PauseSim();
                                                // If no object is selected yet: Select the object
                                                if (!moveToolObjectSelected || state_objectUnderPointer != moveToolSelectedObject)
                                                {
                                                    ResetSelection();
                                                    // Select
                                                    moveToolObjectSelected = true;
                                                    moveToolSelectedObject = state_objectUnderPointer;
                                                    // Remember the offset so that the object is drawn and placed
                                                    // at the right position relative to the cursor
                                                    moveToolObjectOffset = state_objectUnderPointer.Position - mouseWorldField;
                                                }
                                                // Object already selected, try to move the object here
                                                else
                                                {
                                                    if (TryMoveObject(moveToolSelectedObject, mouseWorldField + moveToolObjectOffset))
                                                        ResetSelection();
                                                }
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
                                                // Reset selection, close particle and object panel (if open)
                                                ResetSelection();
                                                break;
                                            case UIHandler.UITool.Add:
                                                // Pause Simulation (should not be necessary)
                                                sim.PauseSim();

                                                // Add particle at this position
                                                if (sim.uiHandler.initializationUI.IsOpen())
                                                    sim.system.AddParticleContracted(mouseWorldField, sim.uiHandler.GetDropdownValue_Chirality(), sim.uiHandler.GetDropdownValue_Compass());
                                                else
                                                    sim.system.AddParticleContracted(mouseWorldField);
                                                break;
                                            case UIHandler.UITool.AddObject:
                                                // Pause Simulation (should not be necessary)
                                                sim.PauseSim();

                                                // Add new object at this position
                                                sim.system.AddNewObject(mouseWorldField);
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
                                                // Same for object
                                                else if (moveToolObjectSelected)
                                                {
                                                    if (TryMoveObject(moveToolSelectedObject, mouseWorldField + moveToolObjectOffset))
                                                        ResetSelection();
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                            break;
                        case ClickAction.ClickType.Drag:
                            // Drag has been executed
                            Vector2Int node1 = AmoebotFunctions.WorldToGridPosition(action.positionStart);
                            Vector2Int node2 = AmoebotFunctions.WorldToGridPosition(action.positionTarget);
                            IParticleState p1;
                            IParticleState p2;
                            sim.system.TryGetParticleAt(node1, out p1);
                            sim.system.TryGetParticleAt(node2, out p2);
                            IObjectInfo o1;
                            IObjectInfo o2;
                            sim.system.TryGetObjectAt(node1, out o1);
                            sim.system.TryGetObjectAt(node2, out o2);
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
                                            if (node1 == node2 && p1 == null && o1 == null)
                                            {
                                                // Show Add Overlay (for single particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                            }
                                            else if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && p1 == null && p2 == null && o1 == null && o2 == null)
                                            {
                                                // Show Add Overlay (for expanded particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                Vector3 worldAbsPos2 = AmoebotFunctions.GridToWorldPositionVector3(node2);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos2 + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                            }
                                            else
                                            {
                                                // Show nothing (drag is too far apart or one node has a particle/object)
                                            }
                                        }
                                        break;
                                    case UIHandler.UITool.AddObject:
                                        // Sim must be paused to add objects
                                        if (sim.running == false)
                                        {
                                            // Don't do anything if cursor is not over the grid
                                            if (!state_pointerOverMap)
                                                break;
                                            // Check if object has already been selected
                                            if (addToolObjectSelected)
                                            {
                                                // Object already selected
                                                // If current position is empty and neighbor of selected object:
                                                // Add to the object
                                                if (state_particleUnderPointer == null)
                                                {
                                                    if (state_objectUnderPointer == null && addToolSelectedObject.IsNeighborPosition(node2))
                                                    {
                                                        addToolSelectedObject.AddPosition(mouseWorldField);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // No object selected
                                                // If the start position is an object: Select that object for expansion
                                                if (o1 != null)
                                                {
                                                    addToolSelectedObject = o1;
                                                    addToolObjectSelected = true;
                                                }
                                                // If the position is free: Add new object and start drag
                                                else if (o1 == null && p1 == null)
                                                {
                                                    addToolSelectedObject = sim.system.AddNewObject(node1);
                                                    if (addToolSelectedObject != null)
                                                        addToolObjectSelected = true;
                                                }
                                            }
                                        }
                                        break;
                                    case UIHandler.UITool.Remove:
                                        // Dragging also removes particles and objects
                                        if (state_particleUnderPointer != null)
                                        {
                                            TryRemoveParticle(state_particleUnderPointer);
                                        }
                                        else if (state_objectUnderPointer != null)
                                        {
                                            TryRemoveObject(state_objectUnderPointer, mouseWorldField);
                                        }
                                        break;
                                    case UIHandler.UITool.Move:
                                        // Move Overlay, only shown if a particle is selected
                                        if (sim.running == false && moveToolParticleSelected)
                                        {
                                            IParticleState selectedParticle;
                                            sim.system.TryGetParticleAt(moveToolParticlePosition, out selectedParticle);
                                            // Validity Check (either one empty field or two empty fields next to each other selected)
                                            // Dragging over selected particle is fine
                                            if (node1 == node2 && (p1 == null || p1 == selectedParticle) && o1 == null)
                                            {
                                                // Show Move Overlay (for single particle)
                                                Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(node1);
                                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                                            }
                                            else if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && (p1 == null || p1 == selectedParticle) && (p2 == null || p2 == selectedParticle) && o1 == null && o2 == null)
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
                                                // Add particle or object at position
                                                if (sim.uiHandler.objectUI.IsOpen())
                                                {
                                                    if (sim.uiHandler.objectUI.GetShownObject().IsNeighborPosition(mouseWorldField))
                                                        sim.uiHandler.objectUI.GetShownObject().AddPosition(mouseWorldField);
                                                }
                                                else
                                                {
                                                    if (sim.uiHandler.initializationUI.IsOpen()) sim.system.AddParticleContracted(mouseWorldField, sim.uiHandler.GetDropdownValue_Chirality(), sim.uiHandler.GetDropdownValue_Compass());
                                                    else sim.system.AddParticleContracted(mouseWorldField);
                                                }
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
                                                // Add particle or objects at position
                                                if (sim.uiHandler.objectUI.IsOpen())
                                                {
                                                    IObjectInfo obj = sim.uiHandler.objectUI.GetShownObject();
                                                    if (obj.IsNeighborPosition(node1) || obj.IsNeighborPosition(node2))
                                                    {
                                                        obj.AddPosition(node1);
                                                        obj.AddPosition(node2);
                                                    }
                                                }
                                                else
                                                {
                                                    if (sim.uiHandler.initializationUI.IsOpen()) sim.system.AddParticleExpanded(node2, node1, sim.uiHandler.GetDropdownValue_Chirality(), sim.uiHandler.GetDropdownValue_Compass());
                                                    else sim.system.AddParticleExpanded(node2, node1); // mouse movement from tail to head
                                                }
                                            }
                                        }
                                        break;
                                    case UIHandler.UITool.AddObject:
                                        // Reset object selection
                                        addToolObjectSelected = false;
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
                                                    if ((p1 == null || p1 == selectedParticle && selectedParticle.IsExpanded()) && o1 == null)
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
                                                    if (AmoebotFunctions.AreNodesNeighbors(node1, node2) && (p1 == null || p1 == selectedParticle) && (p2 == null || p2 == selectedParticle) && o1 == null && o2 == null)
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
            IObjectInfo state_objectUnderPointer = null;
            bool state_pointerOverMap = EventSystem.current.IsPointerOverGameObject() == false;
            if (state_pointerOverMap)
            {
                // Show Particle Selection Overlay
                bool foundParticle = sim.system.TryGetParticleAt(mouseWorldField, out state_particleUnderPointer);
                if (!foundParticle)
                    sim.system.TryGetObjectAt(mouseWorldField, out state_objectUnderPointer);
            }

            // Update move tool state if tool is not selected anymore or sim is running
            if (activeTool != UIHandler.UITool.Move || sim.running)
            {
                moveToolParticleSelected = false;
                moveToolObjectSelected = false;
            }
            // Reset Add Object tool if other tool is selected or sim is running
            if (activeTool != UIHandler.UITool.AddObject || sim.running)
            {
                addToolObjectSelected = false;
                addToolSelectedObject = null;
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
                        if (state_pointerOverMap && sim.uiHandler.particleUI.IsOpen() == false && sim.uiHandler.objectUI.IsOpen() == false)
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
                            // Show object selection overlay
                            else if (state_objectUnderPointer != null)
                            {
                                foreach (Vector2Int p in state_objectUnderPointer.OccupiedPositions())
                                {
                                    Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(p.x, p.y, RenderSystem.zLayer_object_ui);
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos, Quaternion.identity, Vector3.one), material_objectSelectionOverlay, 0);
                                }
                            }
                        }
                        else if (sim.uiHandler.particleUI.IsOpen())
                        {
                            // Particle Panel is open: Render highlight
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
                        else if (sim.uiHandler.objectUI.IsOpen())
                        {
                            // Object Panel is open: Render highlight
                            IObjectInfo activeObject = sim.uiHandler.objectUI.GetShownObject();
                            if (activeObject != null)
                            {
                                foreach (Vector2Int p in activeObject.OccupiedPositions())
                                {
                                    Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(p.x, p.y, RenderSystem.zLayer_object_ui);
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos, Quaternion.identity, Vector3.one), material_objectSelectionOverlay, 0);
                                }
                            }
                        }
                        break;
                    case UIHandler.UITool.Add:
                        if (state_pointerOverMap)
                        {
                            // Sim must be paused to add particles
                            // (Add tool should not be available in Simulation Mode anyway)
                            if (sim.running == false)
                            {
                                if (state_particleUnderPointer == null && state_objectUnderPointer == null && currentlyDragging == false)
                                {
                                    // Empty Field Selected + Not dragging
                                    // Show Add Overlay (for single particle)
                                    Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(mouseWorldField);
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                }
                            }
                        }
                        break;
                    case UIHandler.UITool.AddObject:
                        if (state_pointerOverMap)
                        {
                            // Sim must be paused to add objects
                            // (Add tool should not be available in Simulation Mode anyway)
                            if (sim.running == false)
                            {
                                if (state_particleUnderPointer == null)
                                {
                                    // No particle under cursor
                                    Vector3 worldAbsPos = AmoebotFunctions.GridToWorldPositionVector3(mouseWorldField);
                                    if (currentlyDragging == false)
                                    {
                                        // Not dragging: Draw preview of new object or overlay over existing object
                                        if (state_objectUnderPointer == null)
                                        {
                                            // Empty field: Show overlay for new object
                                            UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddOverlay, 0);
                                        }
                                        else
                                        {
                                            UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddObjectOverlay, 0);
                                        }
                                    }
                                    else if (addToolObjectSelected)
                                    {
                                        // Dragging with object selected
                                        // Use Add Object overlay if position is occupied by selected object or free
                                        if (state_objectUnderPointer == null || state_objectUnderPointer == addToolSelectedObject)
                                            UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldAbsPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonAddObjectOverlay, 0);
                                        // Otherwise draw nothing
                                    }
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
                            else if (state_objectUnderPointer != null)
                            {
                                // If Shift is held down: Render overlay for all nodes of the object
                                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                                {
                                    foreach (Vector2Int p in state_objectUnderPointer.OccupiedPositions())
                                    {
                                        Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(p);
                                        UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonRemoveOverlay, 0);
                                    }
                                }
                                else
                                {
                                    // Render overlay for the current node
                                    Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(mouseWorldField);
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonRemoveOverlay, 0);
                                }
                            }
                        }
                        break;
                    case UIHandler.UITool.Move:
                        if (moveToolParticleSelected || (!moveToolObjectSelected && state_pointerOverMap && state_particleUnderPointer != null))
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
                        else if (moveToolObjectSelected || (!moveToolParticleSelected && state_pointerOverMap && state_objectUnderPointer != null))
                        {
                            // Mark selected object
                            IObjectInfo selectedObj;
                            if (moveToolObjectSelected)
                                selectedObj = moveToolSelectedObject;
                            else
                                selectedObj = state_objectUnderPointer;
                            foreach (Vector2Int pos in selectedObj.OccupiedPositions())
                            {
                                Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(pos);
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveSelectionOverlay, 0);
                            }
                        }
                        // Mark Moving Position
                        if (moveToolParticleSelected && currentlyDragging == false)
                        {
                            if (state_pointerOverMap && state_particleUnderPointer == null && state_objectUnderPointer == null)
                            {
                                Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(mouseWorldField);
                                UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                            }
                        }
                        else if (moveToolObjectSelected && currentlyDragging == false)
                        {
                            if (state_pointerOverMap && state_particleUnderPointer == null && (state_objectUnderPointer == null || state_objectUnderPointer == moveToolSelectedObject))
                            {
                                // Draw whole object with offset
                                foreach (Vector2Int p in moveToolSelectedObject.OccupiedPositions())
                                {
                                    Vector2Int pos = p + moveToolObjectOffset - moveToolSelectedObject.Position + mouseWorldField;
                                    Vector3 worldPos = AmoebotFunctions.GridToWorldPositionVector3(pos);
                                    UnityEngine.Graphics.DrawMesh(mesh_baseHexagonBackground, Matrix4x4.TRS(worldPos + new Vector3(0f, 0f, RenderSystem.zLayer_ui), Quaternion.identity, Vector3.one), material_hexagonMoveOverlay, 0);
                                }
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

        /// <summary>
        /// Resets the current selection state. This should be
        /// called whenever the system is regenerated in Init Mode.
        /// </summary>
        public void ResetSelection()
        {
            ObjectUIHandler.instance?.Close();
            ParticleUIHandler.instance?.Close();
            moveToolParticleSelected = false;
            moveToolObjectSelected = false;
        }

        /// <summary>
        /// Tries to remove the given particle from the system.
        /// Should be called by the Remove tool.
        /// </summary>
        /// <param name="particle">The particle to be removed.</param>
        private void TryRemoveParticle(IParticleState particle)
        {
            // Close Particle Panel (just in case)
            if (ParticleUIHandler.instance != null && ParticleUIHandler.instance.GetShownParticle() == particle)
                ParticleUIHandler.instance.Close();
            // Remove particle
            sim.system.RemoveParticle(particle);
        }

        /// <summary>
        /// Tries to remove the given position from the given object.
        /// Should be called by the Remove tool.
        /// <para>
        /// If the object only consists of the given position, the
        /// object is removed from the object completely.
        /// </para>
        /// </summary>
        /// <param name="obj">The object under the cursor.</param>
        /// <param name="position">The position to be removed.</param>
        /// <param name="showWarning">Whether a warning message should be
        /// displayed when the part of the object cannot be removed.</param>
        private void TryRemoveObject(IObjectInfo obj, Vector2Int position, bool showWarning = false)
        {
            // Remove part of the object if possible (or remove the whole object)
            if (obj.Size > 1)
            {
                if (obj.IsConnected(position))
                {
                    obj.RemovePosition(position);
                    if (ObjectUIHandler.instance != null && ObjectUIHandler.instance.GetShownObject() == obj)
                        ObjectUIHandler.instance.RefreshObjectPanel();
                }
                else if (showWarning)
                {
                    Log.Warning("Cannot remove object position: Object would be disconnected.");
                }
            }
            else
            {
                if (ObjectUIHandler.instance != null && ObjectUIHandler.instance.IsOpen() && ObjectUIHandler.instance.GetShownObject() == obj)
                    ObjectUIHandler.instance.Close();
                // Remove object from the system
                obj.RemoveFromSystem();
            }
        }

        private bool TryMoveObject(IObjectInfo obj, Vector2Int newPos)
        {
            return obj.MoveToPosition(newPos);
        }

    }

}
