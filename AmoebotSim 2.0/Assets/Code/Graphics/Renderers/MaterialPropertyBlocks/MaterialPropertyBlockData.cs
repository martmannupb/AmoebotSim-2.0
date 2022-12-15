using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Graphics
{

    public abstract class MaterialPropertyBlockData
    {

        public MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        protected abstract void Init();

        public abstract void Reset();

    }

} // namespace AS2.Graphics
