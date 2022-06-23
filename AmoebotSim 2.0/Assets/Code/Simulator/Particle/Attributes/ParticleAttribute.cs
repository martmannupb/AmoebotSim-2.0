using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParticleAttribute
{

    public abstract string ToString();
    public abstract string ToString_ParameterName();
    public abstract string ToString_ParameterValue();
    public abstract void UpdateParameterValue(string value);
    public abstract System.Type GetAttributeType();

}
