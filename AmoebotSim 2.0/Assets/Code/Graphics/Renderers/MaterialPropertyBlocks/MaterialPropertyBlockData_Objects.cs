using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Contains a <c>MaterialPropertyBlock</c> instance for
    /// rendering objects.
    /// </summary>
    public class MaterialPropertyBlockData_Objects : MaterialPropertyBlockData
    {

        // Data
        private Color color;
        public Color Color
        {
            get;
        }

        public MaterialPropertyBlockData_Objects()
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

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }
    }

}
