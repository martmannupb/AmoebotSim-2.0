using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AS2.Visuals;

namespace AS2
{

    /// <summary>
    /// Contains references to all of the materials used in the
    /// simulation environment. All of the materials are loaded
    /// when the application starts.
    /// </summary>
    public static class MaterialDatabase
    {

        /// <summary>
        /// Sets the render queue values of all materials to fix render
        /// layering issues.
        /// <para>
        /// Note that this does not actually work because Unity resets
        /// copied materials sometimes (we could not find a way to
        /// circumvent this). Instead, these render queue values are
        /// also set manually in the material assets. The only materials
        /// that require an update of the render queue in the code are
        /// the hexagon pin material with the invisible hexagon, as
        /// created by <see cref="TextureCreator.GetPinBorderMaterial(int, ViewType)"/>,
        /// and the pin beep origin highlights.
        /// </para>
        /// </summary>
        public static void SetRenderQueues()
        {
            int q_background = RenderSystem.renderQueue_background;
            int q_bonds = RenderSystem.renderQueue_bonds;
            int q_object_ui = RenderSystem.renderQueue_object_ui;
            int q_objects = RenderSystem.renderQueue_objects;
            int q_particles = RenderSystem.renderQueue_particles;
            int q_circuits = RenderSystem.renderQueue_circuits;
            int q_circuitBeeps = RenderSystem.renderQueue_circuitBeeps;
            int q_pins = RenderSystem.renderQueue_pins;
            int q_overlays = RenderSystem.renderQueue_overlays;

            // Background materials
            material_circular_bgLines.renderQueue = q_background;
            material_hexagonal_bgHex.renderQueue = q_background;

            // Bond materials
            material_bond_lineCircular_movement.renderQueue = q_bonds;
            material_bond_lineHexagonal_movement.renderQueue = q_bonds;

            // Object UI material
            material_object_ui.renderQueue = q_object_ui;

            // Object material
            material_object_base.renderQueue = q_objects;

            // Particle materials
            material_circular_particleComplete.renderQueue = q_particles;
            material_hexagonal_particleCombined.renderQueue = q_particles;
            material_circular_particleCompleteConnector.renderQueue = q_particles;

            // Circuit materials
            material_circuit_line_movement.renderQueue = q_circuits;
            material_circuit_lineConnector_movement.renderQueue = q_circuits;

            material_circuit_beep.renderQueue = q_circuitBeeps;
            material_circuit_beepPaused.renderQueue = q_circuitBeeps;

            // Pin materials
            material_circuit_pin_movement.renderQueue = q_pins;

            // UI overlay materials
            material_hexagonal_ui_baseHexagonSelectionMaterial.renderQueue = q_overlays;
            material_hexagonal_ui_baseHexagonAddMaterial.renderQueue = q_overlays;
            material_hexagonal_ui_baseHexagonRemoveMaterial.renderQueue = q_overlays;
            material_hexagonal_ui_baseHexagonMoveMaterial.renderQueue = q_overlays;
            material_hexagonal_ui_baseHexagonMoveSelectionMaterial.renderQueue = q_overlays;
            material_circuit_ui_pSetHoverMaterial.renderQueue = q_overlays;
            material_circuit_ui_pSetDragMaterial.renderQueue = q_overlays;
        }

        // Circular View
        public static Material material_circular_bgLines = Resources.Load<Material>(FilePaths.path_materials + "CircularView/BGMaterial");
        public static Material material_circular_particleComplete = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleMat");
        public static Material material_circular_particleCompleteConnector = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ConnectorMat");

        // Hexagonal View
        public static Material material_hexagonal_bgHex = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonGridMat");
        public static Material material_hexagonal_particleCombined = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonCombinedMat");

        // Circuits
        public static Material material_circuit_line_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitMatWithMovement");
        public static Material material_circuit_lineConnector_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitConnectorMatWithMovement");
        public static Material material_circuit_pin_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/PinMatWithMovement");
        
        // Beeps
        public static Material material_circuit_beep = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BeepMat");
        public static Material material_circuit_beepPaused = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BeepPausedMat");

        // Bonds
        public static Material material_bond_lineHexagonal_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BondHexMatWithMovement");
        public static Material material_bond_lineCircular_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BondCircMatWithMovement");

        // Objects
        public static Material material_object_base = Resources.Load<Material>(FilePaths.path_materials + "Base/ObjectMat");
        public static Material material_object_ui = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/ObjectSelectionMaterial");

        // UI
        public static Material material_hexagonal_ui_baseHexagonSelectionMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonSelectionMaterial");
        public static Material material_hexagonal_ui_baseHexagonAddMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonAddMaterial");
        public static Material material_hexagonal_ui_baseHexagonRemoveMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonRemoveMaterial");
        public static Material material_hexagonal_ui_baseHexagonMoveMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonMoveMaterial");
        public static Material material_hexagonal_ui_baseHexagonMoveSelectionMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonMoveSelectionMaterial");
        public static Material material_circuit_ui_pSetHoverMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/PSetHoverMaterial");
        public static Material material_circuit_ui_pSetDragMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/PSetDragMaterial");

    }

} // namespace AS2
