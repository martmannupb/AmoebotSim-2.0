using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing enum values.
/// <para>
/// To use this attribute, simply create an <c>enum</c> and use it
/// as the attribute's type parameter:
/// <code>
/// public enum State { IDLE, ROOT, LEADER }
/// public class MyParticleAlgo : ParticleAlgorithm {
///     public ParticleAttribute_Enum<![CDATA[<State>]]> myEnumAttr;
///     public MyParticleAlgo(Particle p) : base(p) {
///         myEnumAttr = new ParticleAttribute_Enum<![CDATA[<State>]]>(this, "Fancy display name", State.IDLE);
///     }
/// }
/// </code>
/// </para>
/// </summary>
/// <typeparam name="T">The enum type to represent.</typeparam>
public class ParticleAttribute_Enum<T> : ParticleAttribute where T : System.Enum
{
    private T value;

    public ParticleAttribute_Enum(ParticleAlgorithm particle, string name, T initialValue) : base(particle, name)
    {
        value = initialValue;
    }

    public void SetValue(T value)
    {
        this.value = value;
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
        s += ", current value is " + ToString_AttributeValue();
        return s;
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
        this.value = (T)Enum.Parse(this.value.GetType(), value);
    }

    // Conversion operator
    // This allows ParticleAttribute_Int objects to be readable like normal enum variables
    public static implicit operator T(ParticleAttribute_Enum<T> attr) => attr.value;
}
