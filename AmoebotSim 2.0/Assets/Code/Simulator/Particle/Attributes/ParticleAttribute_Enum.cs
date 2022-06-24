using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Foo { FOO, BAR, BAZ }

public class ParticleAttribute_Enum<T> : ParticleAttribute where T : System.Enum
{
    private T value;

    public ParticleAttribute_Enum(string name, T initialValue) : base(name)
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
        //throw new System.NotImplementedException();
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
        this.value = (T)Enum.Parse(this.value.GetType(), value);
    }
}
