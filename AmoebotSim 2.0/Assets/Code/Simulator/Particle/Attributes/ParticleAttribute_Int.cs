using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing integer values.
/// </summary>
public class ParticleAttribute_Int : ParticleAttribute<int>, IParticleAttribute
{
    private int value;

    public ParticleAttribute_Int(Particle particle, string name, int value = 0) : base(particle, name)
    {
        this.value = value;
    }

    public override void SetValue(int value)
    {
        this.value = value;
    }

    public Type GetAttributeType()
    {
        return System.Type.GetType("int");
    }

    public override string ToString()
    {
        return "ParticleAttribute (int) with name " + name + " and value " + value;
    }

    public string ToString_AttributeName()
    {
        return name;
    }

    public string ToString_AttributeValue()
    {
        return value.ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        // TODO: Handle exception?
        this.value = int.Parse(value);
    }

    public override int GetValue()
    {
        return value;
    }

    // Conversion operator
    // This allows ParticleAttribute_Int objects to be readable like normal ints
    //public static implicit operator int(ParticleAttribute_Int attr) => attr.value;
}
