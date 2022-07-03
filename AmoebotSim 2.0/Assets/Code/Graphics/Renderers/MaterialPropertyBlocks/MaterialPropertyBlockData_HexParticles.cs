using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialPropertyBlockData_HexParticles : MaterialPropertyBlockData
{

    // Data
    private Color property_color = MaterialDatabase.material_hexagonal_particleExpansion.GetColor("_InputColor");
    private float property_isExpanding;
    private float property_expansionPercentage;
    private float property_expansionPercentage2;
    private float property_expansionMesh;

    // Data Conversion
    private float[] globalDirToExpansionMeshMap = new float[] { 3, 2, 1, 0, 5, 4 };

    public MaterialPropertyBlockData_HexParticles()
    {
        Init();
    }

    protected override void Init()
    {
        // Apply to block
        ApplyToBlock();
    }

    public void UpdateValue(bool isExpanding, int visualExpansionDir, float animation_expansionPercentage1, float animation_expansionPercentage2)
    {
        property_isExpanding = isExpanding ? 1f : 0f;
        property_expansionPercentage = animation_expansionPercentage1;
        property_expansionPercentage2 = animation_expansionPercentage2;
        property_expansionMesh = globalDirToExpansionMeshMap[(visualExpansionDir + 6) % 6]; // % for the -1 values
    }

    public void ApplyAnimationTimestamp(float triggerTime, float animationLength)
    {
        propertyBlock.SetFloat("_AnimTriggerTime", triggerTime);
        propertyBlock.SetFloat("_AnimDuration", animationLength);
    }

    public void ApplyToBlock()
    {
        // Apply to block
        propertyBlock.SetColor("_InputColor", property_color);
        propertyBlock.SetFloat("_IsExpanding", property_isExpanding);
        propertyBlock.SetFloat("_ExpansionPercentage", property_expansionPercentage);
        propertyBlock.SetFloat("_ExpansionPercentage2", property_expansionPercentage2);
        propertyBlock.SetFloat("_ExpansionMesh", property_expansionMesh);
    }

    public override void Reset()
    {
        throw new System.NotImplementedException();
    }
}
