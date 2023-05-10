using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Contains a <c>MaterialPropertyBlock</c> instance for
    /// the circuit system. Used for updating the block.
    /// </summary>
    public class MaterialPropertyBlockData_Circuits : MaterialPropertyBlockData
    {

        // Data
        private Color color;

        public MaterialPropertyBlockData_Circuits()
        {
            Init();
        }

        protected override void Init()
        {

        }

        /// <summary>
        /// Applies the given color to the property block.
        /// </summary>
        /// <param name="color">The new color to be applied.</param>
        public void ApplyColor(Color color)
        {
            this.color = color;
            propertyBlock.SetColor("_InputColor", color);
        }

        /// <summary>
        /// Applies the given secondary color to the property block.
        /// </summary>
        /// <param name="color">The new secondary color to be applied.</param>
        public void ApplyColorSecondary(Color color)
        {
            this.color = color;
            propertyBlock.SetColor("_InputColorSecondary", color);
        }

        /// <summary>
        /// Applies the given texture to the property block.
        /// </summary>
        /// <param name="tex">The new texture to be applied.</param>
        public void ApplyTexture(Texture tex)
        {
            propertyBlock.SetTexture("_Texture2D", tex);
        }

        /// <summary>
        /// Applies the given animation timestamp to the property block.
        /// </summary>
        /// <param name="triggerTime">The time at which the animation was triggered.</param>
        /// <param name="animationLength">The length of an animation.</param>
        public void ApplyAnimationTimestamp(float triggerTime, float animationLength)
        {
            propertyBlock.SetFloat("_AnimTriggerTime", triggerTime);
            propertyBlock.SetFloat("_AnimDuration", animationLength);
        }

        /// <summary>
        /// Applies the given alpha percentages to the property block.
        /// </summary>
        /// <param name="alphaBeforeAnimation">The alpha percentage before the animation.</param>
        /// <param name="alphaAfterAnimation">The alpha percentage after the animation.</param>
        public void ApplyAlphaPercentagesToBlock(float alphaBeforeAnimation, float alphaAfterAnimation)
        {
            // Apply to block
            propertyBlock.SetFloat("_AnimAlpha1", alphaBeforeAnimation);
            propertyBlock.SetFloat("_AnimAlpha2", alphaAfterAnimation);
        }

        /// <summary>
        /// Applies the given movement timestamp to the property block.
        /// </summary>
        /// <param name="movementTriggerTime">The time at which the movement has started.</param>
        /// <param name="movementDuration">The duration of a movement.</param>
        public void ApplyMovementTimestamp(float movementTriggerTime, float movementDuration)
        {
            propertyBlock.SetFloat("_MovementTriggerTime", movementTriggerTime);
            propertyBlock.SetFloat("_MovementDuration", movementDuration);
        }

        /// <summary>
        /// Applies the given movement offset to the property block.
        /// </summary>
        /// <param name="movementOffset">The new movement offset vector.</param>
        public void ApplyMovementOffset(Vector2 movementOffset)
        {
            propertyBlock.SetVector("_MovementOffset", movementOffset);
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }

}
