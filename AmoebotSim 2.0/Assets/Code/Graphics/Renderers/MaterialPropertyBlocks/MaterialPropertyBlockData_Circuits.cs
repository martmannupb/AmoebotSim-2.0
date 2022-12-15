using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

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

        public void ApplyColor(Color color)
        {
            this.color = color;
            propertyBlock.SetColor("_InputColor", color);
        }

        public void ApplyTexture(Texture tex)
        {
            propertyBlock.SetTexture("_Texture2D", tex);
        }

        public void ApplyAnimationTimestamp(float triggerTime, float animationLength)
        {
            propertyBlock.SetFloat("_AnimTriggerTime", triggerTime);
            propertyBlock.SetFloat("_AnimDuration", animationLength);
        }

        public void ApplyAlphaPercentagesToBlock(float alphaBeforeAnimation, float alphaAfterAnimation)
        {
            // Apply to block
            propertyBlock.SetFloat("_AnimAlpha1", alphaBeforeAnimation);
            propertyBlock.SetFloat("_AnimAlpha2", alphaAfterAnimation);
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }

}