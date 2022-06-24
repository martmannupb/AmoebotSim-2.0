using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAttribute_Int : ParticleAttribute
{
    private int value;

    public ParticleAttribute_Int(string name, int value = 0) : base(name)
    {
        this.value = value;
    }

    public override Type GetAttributeType()
    {
        return System.Type.GetType("int");
    }

    public override string ToString()
    {
        return "ParticleAttribute (int) with name " + name + " and value " + value;
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
        this.value = int.Parse(value);
    }

    // Conversion operators
    // This allows ParticleAttribute_Int objects to be readable like normal ints
    public static implicit operator int(ParticleAttribute_Int attr) => attr.value;
    //public static implicit operator ParticleAttribute_Int(int value) => new ParticleAttribute_Int("", value);
    // Second one also requires copy constructor (should not be needed)
    //public ParticleAttribute_Int(ParticleAttribute_Int other) : base(other.name)
    //{
    //    this.value = other.value;
    //}
}
