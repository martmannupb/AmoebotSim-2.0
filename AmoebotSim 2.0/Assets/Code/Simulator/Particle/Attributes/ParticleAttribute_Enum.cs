using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ParticleAttribute_Enum<T> : ParticleAttribute where T : System.Enum
{
    private T value;

    public ParticleAttribute_Enum(ParticleAlgorithm particle, string name, T initialValue) : base(particle, name)
    {
        value = initialValue;
    }

    public override Type GetAttributeType()
    {
        return value.GetType();
    }
    
    public override string ToString()
    {
        string s = "ParticleAttribute_Enum of type " + value.GetType().Name + " with values ";
        string[] names = Enum.GetNames(value.GetType());
        Array values = Enum.GetValues(value.GetType());
        int i = 0;
        foreach (int val in values)
        {
            s += names[i] + "=" + val + " ";
            i++;
        }
        s += ", current value is " + ToString_ParameterValue();
        return s;
    }

    public override string ToString_ParameterName()
    {
        return name;
    }

    public override string ToString_ParameterValue()
    {
        return value.ToString();
    }

    public override void UpdateParameterValue(string value)
    {
        // TODO: Handle exception?
        this.value = (T)Enum.Parse(this.value.GetType(), value);
    }

    public static implicit operator T(ParticleAttribute_Enum<T> attr) => attr.value;
}
