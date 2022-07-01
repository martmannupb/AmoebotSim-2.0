using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialPropertyBlockData_HexParticles : MaterialPropertyBlockData
{

    // Params
    private int arraySize;

    // Defaults
    private Color def_color = MaterialDatabase.material_hexagonal_particleExpansion.GetColor("_InputColor");
    private float def_isExpanding = 0f; // bool is a float here
    private float def_expansionPercentage = 0f;
    private float def_expansionMesh = 0f;

    // Data
    private float[] propertyArray_isExpanding;
    private float[] propertyArray_expansionPercentage;
    private float[] propertyArray_expansionMesh;

    // Data Conversion
    private float[] globalDirToExpansionMeshMap = new float[] { 3, 2, 1, 0, 5, 4 };

    public MaterialPropertyBlockData_HexParticles(int arraySize)
    {
        this.arraySize = arraySize;

        Init();
    }

    protected override void Init()
    {
        // Set default material properties (not expanded + color)
        propertyArray_isExpanding = new float[arraySize];
        propertyArray_expansionPercentage = new float[arraySize];
        propertyArray_expansionMesh = new float[arraySize];
        for (int i = 0; i < arraySize; i++)
        {
            propertyArray_isExpanding[i] = def_isExpanding;
            propertyArray_expansionPercentage[i] = def_expansionPercentage;
            propertyArray_expansionMesh[i] = def_expansionMesh;
        }

        // Apply to block
        ApplyToBlock();
    }

    public void UpdateValue(int arrayPosition, bool isExpanding, int globalExpansionDir)
    {
        propertyArray_isExpanding[arrayPosition] = isExpanding ? 1f : 0f;
        propertyArray_expansionPercentage[arrayPosition] = 100f * propertyArray_isExpanding[arrayPosition];
        propertyArray_expansionMesh[arrayPosition] = globalDirToExpansionMeshMap[(globalExpansionDir + 6) % 6]; // % for the -1 values
    }

    public void ApplyToBlock()
    {
        // Apply to block
        propertyBlock.SetColor("_InputColor", def_color);
        propertyBlock.SetFloatArray("_IsExpanding", propertyArray_isExpanding);
        propertyBlock.SetFloatArray("_ExpansionPercentage", propertyArray_expansionPercentage);
        propertyBlock.SetFloatArray("_ExpansionMesh", propertyArray_expansionMesh);
    }

    public override void Reset()
    {
        throw new System.NotImplementedException();
    }
}
