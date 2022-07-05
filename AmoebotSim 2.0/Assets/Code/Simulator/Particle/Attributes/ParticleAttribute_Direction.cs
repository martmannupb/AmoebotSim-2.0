using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing direction values.
/// Only values in the set {-1,0,1,2,3,4,5} are permitted.
/// </summary>
public class ParticleAttribute_Direction : ParticleAttribute
{
    private int value;

    public ParticleAttribute_Direction(ParticleAlgorithm particle, string name, int value = 0) : base(particle, name)
    {
        CheckValue(value);
        this.value = value;
    }

    private void CheckValue(int val)
    {
        if (val < -1 || val > 5)
        {
            throw new System.ArgumentOutOfRangeException("Direction must be value between 0 and 5, got " + val);
        }
    }

    public void SetValue(int value)
    {
        CheckValue(value);
        this.value = value;
    }

    public override Type GetAttributeType()
    {
        return System.Type.GetType("int");
    }

    public override string ToString()
    {
        return "ParticleAttribute (direction) with name " + name + " and value " + value;
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
        int val = int.Parse(value);
        CheckValue(val);
        this.value = val;
    }

    // Conversion operator
    // This allows ParticleAttribute_Int objects to be readable like normal ints
    public static implicit operator int(ParticleAttribute_Direction attr) => attr.value;
}
