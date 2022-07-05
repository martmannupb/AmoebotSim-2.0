using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing boolean values.
/// </summary>
public class ParticleAttribute_Bool : ParticleAttribute
{
    private bool value;

    public ParticleAttribute_Bool(ParticleAlgorithm particle, string name, bool value = false) : base(particle, name)
    {
        this.value = value;
    }

    public void SetValue(bool value)
    {
        this.value = value;
    }

    public override Type GetAttributeType()
    {
        return System.Type.GetType("bool");
    }

    public override string ToString()
    {
        return "ParticleAttribute (bool) with name " + name + " and value " + value.ToString();
    }

    public override string ToString_AttributeName()
    {
        return name;
    }

    public override string ToString_AttributeValue()
    {
        return value.ToString();
    }

    public override void UpdateAttributeValue(string value)
    {
        // TODO: Handle exception?
        this.value = bool.Parse(value);
    }

    // Conversion operator
    // This allows ParticleAttribute_Int objects to be readable like normal bools
    public static implicit operator bool(ParticleAttribute_Bool attr) => attr.value;
}
