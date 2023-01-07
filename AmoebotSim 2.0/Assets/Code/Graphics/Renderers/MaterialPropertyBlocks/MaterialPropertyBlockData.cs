using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Wrapper for a MaterialPropertyBlock.
    /// Can contain methods that dynamically set its values.
    /// </summary>
    public abstract class MaterialPropertyBlockData
    {

        public MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        protected abstract void Init();

        public abstract void Reset();

    }

}
