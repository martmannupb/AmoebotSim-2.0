using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Wrapper for a <see cref="UnityEngine.MaterialPropertyBlock"/>.
    /// Can contain methods that dynamically set its values.
    /// </summary>
    public abstract class MaterialPropertyBlockData
    {

        public MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        /// <summary>
        /// Initializes the property block with default values.
        /// </summary>
        protected abstract void Init();

        /// <summary>
        /// Resets the property block to its default values.
        /// </summary>
        public abstract void Reset();

    }

}
