using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2
{

    public static class MaterialDatabase
    {

        // Base
        public static Material material_color = Resources.Load<Material>(FilePaths.path_materials + "Base/WhiteMat");

        // Circular View
        // deprecated
        public static Material material_circular_bgLines = Resources.Load<Material>(FilePaths.path_materials + "CircularView/BGMaterial");
        public static Material material_circular_particleComplete = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleMat");
        public static Material material_circular_particleCompleteConnector = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ConnectorMat");
        // Old System
        //public static Material material_circular_particle = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleColorMat");
        //public static Material material_circular_particleCenter = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleCenterMat");
        //public static Material material_circular_particleConnector = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleConnectorMat");

        // Hexagonal View
        public static Material material_hexagonal_bgHex = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonGridMat");
        public static Material material_hexagonal_particleCombined = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonCombinedMat");
        // Old System
        //public static Material material_hexagonal_particle = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonDefaultMat");
        //public static Material material_hexagonal_particleExpansion = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonExpansionMat");
        //public static Material material_hexagonal_particleCenter = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonCenterDefaultMat");

        // Circuits
        public static Material material_circuit_line = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitMat");
        public static Material material_circuit_line_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitMatWithMovement");
        public static Material material_circuit_lineConnector = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitConnectorMat");
        public static Material material_circuit_lineConnector_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitConnectorMatWithMovement");
        public static Material material_circuit_pin = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/PinMat");
        public static Material material_circuit_pin_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/PinMatWithMovement");
        // Beeps
        public static Material material_circuit_beep = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BeepMat");
        public static Material material_circuit_beepPaused = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BeepPausedMat");
        public static Material material_circuit_pin_beep = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/PinBeepMat"); // not used
        // Bonds
        public static Material material_bond_lineHexagonal = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BondHexMat");
        public static Material material_bond_lineHexagonal_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BondHexMatWithMovement");
        public static Material material_bond_lineCircular = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BondCircMat");
        public static Material material_bond_lineCircular_movement = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BondCircMatWithMovement");

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
