using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MaterialDatabase
{

    // Circular View
    // deprecated
    public static Material material_circular_bgLines = Resources.Load<Material>(FilePaths.path_materials + "CircularView/BGMaterial");
    public static Material material_circular_particle = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleColorMat");
    public static Material material_circular_particleCenter = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleCenterMat");
    // new system
    public static Material material_circular_particleComplete = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleMat");
    public static Material material_circular_particleConnector = Resources.Load<Material>(FilePaths.path_materials + "CircularView/ParticleConnectorMat");
    
    // Hexagonal View
    public static Material material_hexagonal_bgHex = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonGridMat");
    public static Material material_hexagonal_particle = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonDefaultMat");
    public static Material material_hexagonal_particleExpansion = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonExpansionMat");
    public static Material material_hexagonal_particleCenter = Resources.Load<Material>(FilePaths.path_materials + "HexagonalView/HexagonCenterDefaultMat");



    // Hexagonal View

}
