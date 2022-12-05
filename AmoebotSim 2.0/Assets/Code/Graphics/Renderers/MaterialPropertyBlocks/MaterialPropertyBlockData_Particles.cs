using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Graphics
{

    public class MaterialPropertyBlockData_Particles : MaterialPropertyBlockData
    {

        // Data
        private Color property_color;
        private float property_isExpanding;
        private float property_expansionPercentage;
        private float property_expansionPercentage2;
        private Vector3 property_animationOffset;
        private Vector3 property_animationStaticOffset;
        private float property_expansionMesh;
        private float property_width;

        // Values
        private float outterRingWidthDefault = MaterialDatabase.material_circular_particleComplete.GetFloat("_OutterRingWidth");
        private float outterRingWidthPercentage = 1f;

        // Data Conversion
        private float[] globalDirToExpansionMeshMap = new float[] { 3, 2, 1, 0, 5, 4 };

        public MaterialPropertyBlockData_Particles()
        {
            Init();
        }

        protected override void Init()
        {
            // Apply to block
            ApplyToBlock();
        }

        public void ApplyColor(Color color)
        {
            property_color = color;
            propertyBlock.SetColor("_InputColor", color);
        }

        public void ApplyUpdatedValues(bool isExpanding, int visualExpansionDir, float animation_expansionPercentage1, float animation_expansionPercentage2, Vector3 animation_offset)
        {
            property_isExpanding = isExpanding ? 1f : 0f;
            property_expansionPercentage = animation_expansionPercentage1;
            property_expansionPercentage2 = animation_expansionPercentage2;
            property_animationOffset = animation_offset;
            property_expansionMesh = globalDirToExpansionMeshMap[(visualExpansionDir + 6) % 6]; // % for the -1 values
                                                                                                // Apply
            ApplyToBlock();
        }

        public void ApplyOutterCircleWidthPercentage(float p)
        {
            propertyBlock.SetFloat("_OutterRingWidth", outterRingWidthDefault * p);
            outterRingWidthPercentage = p;
        }

        public float GetCurrentOutterCircleWidthPercentage()
        {
            return outterRingWidthPercentage;
        }

        public void ApplyConnectorValues(float animation_expansionPercentage1, float animation_expansionPercentage2, Vector3 animation_offset, Vector3 animation_staticOffset)
        {
            property_expansionPercentage = animation_expansionPercentage1;
            property_expansionPercentage2 = animation_expansionPercentage2;
            property_animationOffset = animation_offset;
            property_animationStaticOffset = animation_staticOffset;
            //property_width = width;
            // Apply
            ApplyToBlock();
        }

        public void ApplyAnimationTimestamp(float triggerTime, float animationLength)
        {
            propertyBlock.SetFloat("_AnimTriggerTime", triggerTime);
            propertyBlock.SetFloat("_AnimDuration", animationLength);
        }

        private void ApplyToBlock()
        {
            // Apply to block
            //propertyBlock.SetColor("_InputColor", property_color);
            //propertyBlock.SetFloat("_IsExpanding", property_isExpanding);
            propertyBlock.SetFloat("_AnimPercentage1", property_expansionPercentage);
            propertyBlock.SetFloat("_AnimPercentage2", property_expansionPercentage2);
            propertyBlock.SetVector("_AnimOffset", property_animationOffset);
            propertyBlock.SetVector("_AnimStaticOffset", property_animationStaticOffset);
            //propertyBlock.SetFloat("_ExpansionMesh", property_expansionMesh);
            //propertyBlock.SetFloat("_Width", property_width);
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }

} // namespace AS2.Graphics
