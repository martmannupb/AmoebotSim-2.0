using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaterialDatabase
{

    // Base
    public static Material material_color = Resources.Load<Material>(FilePaths.path_materials + "Base/WhiteMat");

    // Circular View
    // deprecated
    public static Material material_circular_bgLines = Resources.Load<Material>(FilePaths.path_materials + "CircularView/BGMaterial");
    public static Material material_circular_particle = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleColorMat");
    public static Material material_circular_particleCenter = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleCenterMat");
    public static Material material_circular_particleConnector = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleConnectorMat");
    // new system
    public static Material material_circular_particleComplete = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleMat");
    public static Material material_circular_particleCompleteConnector = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ConnectorMat");

    // Hexagonal View
    public static Material material_hexagonal_bgHex = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonGridMat");
    public static Material material_hexagonal_particle = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonDefaultMat");
    public static Material material_hexagonal_particleExpansion = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonExpansionMat");
    public static Material material_hexagonal_particleCenter = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonCenterDefaultMat");
    // new system
    public static Material material_hexagonal_particleCombined = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonCombinedMat");

    // Circuits
    public static Material material_circuit_line = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitMat");
    public static Material material_circuit_lineConnector = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/CircuitConnectorMat");
    public static Material material_circuit_pin = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/PinMat");
    // Beeps
    public static Material material_circuit_beep = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/BeepMat");
    public static Material material_circuit_pin_beep = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/Circuits/PinBeepMat");

    // UI
    public static Material material_hexagonal_ui_baseHexagonSelectionMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonSelectionMaterial");
    public static Material material_hexagonal_ui_baseHexagonAddMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonAddMaterial");
    public static Material material_hexagonal_ui_baseHexagonRemoveMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonRemoveMaterial");
    public static Material material_hexagonal_ui_baseHexagonMoveMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonMoveMaterial");
    public static Material material_hexagonal_ui_baseHexagonMoveSelectionMaterial = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/UI/HexagonMoveSelectionMaterial");

}
