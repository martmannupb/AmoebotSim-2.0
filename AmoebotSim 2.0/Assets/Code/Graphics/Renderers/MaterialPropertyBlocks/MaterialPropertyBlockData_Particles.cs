using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Contains a <c>UnityEngine.MaterialPropertyBlock</c> instance
    /// for the particle renderer. Used for updating the block.
    /// </summary>
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
        private float outterRingWidthDefault = MaterialDatabase.material_circular_particleComplete.GetFloat("_OuterRingWidth");
        private float outerRingWidthPercentage = 1f;

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

        /// <summary>
        /// Applies the given color to the property block.
        /// </summary>
        /// <param name="color">The new color to be applied.</param>
        public void ApplyColor(Color color)
        {
            property_color = color;
            propertyBlock.SetColor("_InputColor", color);
        }

        /// <summary>
        /// Applies the given animation parameters to the property block.
        /// </summary>
        /// <param name="isExpanding">Whether the particle is currently
        /// expanding or contracting.</param>
        /// <param name="visualExpansionDir">Global direction of the
        /// expansion or contraction movement.</param>
        /// <param name="animation_expansionPercentage1">Start percentage
        /// of the animation.</param>
        /// <param name="animation_expansionPercentage2">End percentage of
        /// the animation.</param>
        /// <param name="animation_offset">Local movement direction of
        /// the animation.</param>
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

        /// <summary>
        /// Applies the given float value to the outer circle width property.
        /// </summary>
        /// <param name="p">The new outer circle width percentage.</param>
        public void ApplyOuterCircleWidthPercentage(float p)
        {
            propertyBlock.SetFloat("_OuterRingWidth", outterRingWidthDefault * p);
            outerRingWidthPercentage = p;
        }

        /// <summary>
        /// Gets the current value of the outer circle width property.
        /// </summary>
        /// <returns>The current outer circle width percentage.</returns>
        public float GetCurrentOuterCircleWidthPercentage()
        {
            return outerRingWidthPercentage;
        }

        /// <summary>
        /// Applies the given connector animation property values
        /// to the property block.
        /// </summary>
        /// <param name="animation_expansionPercentage1">Start percentage
        /// of the animation.</param>
        /// <param name="animation_expansionPercentage2">End percentage
        /// of the animation.</param>
        /// <param name="animation_offset"></param>
        /// <param name="animation_staticOffset"></param>
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

        /// <summary>
        /// Applies the given animation timestamp data to the
        /// property block.
        /// </summary>
        /// <param name="triggerTime">The new animation trigger time.</param>
        /// <param name="animationLength">The new animation length.</param>
        public void ApplyAnimationTimestamp(float triggerTime, float animationLength)
        {
            propertyBlock.SetFloat("_AnimTriggerTime", triggerTime);
            propertyBlock.SetFloat("_AnimDuration", animationLength);
        }

        /// <summary>
        /// Applies animation percentage and animation offset properties.
        /// </summary>
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

}