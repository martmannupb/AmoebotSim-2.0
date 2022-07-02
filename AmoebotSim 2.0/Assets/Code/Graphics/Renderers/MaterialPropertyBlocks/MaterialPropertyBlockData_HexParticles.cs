using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialPropertyBlockData_HexParticles : MaterialPropertyBlockData
{

    // Defaults
    private Color def_color = MaterialDatabase.material_hexagonal_particleExpansion.GetColor("_InputColor");
    private float def_isExpanding = 0f; // bool is a float here
    private float def_expansionPercentage = 0f;
    private float def_expansionMesh = 0f;

    // Data
    private float property_isExpanding;
    private float property_expansionPercentage;
    private float property_expansionMesh;

    // Data Conversion
    private float[] globalDirToExpansionMeshMap = new float[] { 3, 2, 1, 0, 5, 4 };

    public MaterialPropertyBlockData_HexParticles()
    {
        Init();
    }

    protected override void Init()
    {
        // Set default material properties (not expanded + color)
        property_isExpanding = def_isExpanding;
        property_expansionPercentage = def_expansionPercentage;
        property_expansionMesh = def_expansionMesh;

        // Apply to block
        ApplyToBlock();
    }

    public void UpdateValue(bool isExpanding, int visualExpansionDir)
    {
        property_isExpanding = isExpanding ? 1f : 0f;
        property_expansionPercentage = 1f;
        property_expansionMesh = globalDirToExpansionMeshMap[(visualExpansionDir + 6) % 6]; // % for the -1 values
    }

    public void ApplyToBlock()
    {
        // Apply to block
        propertyBlock.SetColor("_InputColor", def_color);
        propertyBlock.SetFloat("_IsExpanding", property_isExpanding);
        propertyBlock.SetFloat("_ExpansionPercentage", property_expansionPercentage);
        propertyBlock.SetFloat("_ExpansionMesh", property_expansionMesh);
    }

    public override void Reset()
    {
        throw new System.NotImplementedException();
    }
}
