using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MaterialPropertyBlockData
{

    public MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    protected abstract void Init();

    public abstract void Reset();

}
