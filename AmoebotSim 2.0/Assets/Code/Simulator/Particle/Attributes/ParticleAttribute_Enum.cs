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
public class ParticleAttribute_Enum<T> : ParticleAttributeWithHistory<T>, IParticleAttribute where T : System.Enum
{
    private T Value
    {
        get
        {
            return particle.isActive ? history.GetMarkedValue() : history.GetValueInRound(particle.system.PreviousRound);
        }
        set
        {
            if (!particle.isActive)
            {
                throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
            }
            history.RecordValueInRound(value, particle.system.CurrentRound);
        }
    }

    public ParticleAttribute_Enum(Particle particle, string name, T initialValue) : base(particle, name)
    {
        history = new ValueHistory<T>(initialValue, particle.system.CurrentRound);
    }

    public override void SetValue(T value)
    {
        Value = value;
    }

    public Type GetAttributeType()
    {
        return Value.GetType();
    }
    
    public override string ToString()
    {
        string s = "ParticleAttribute_Enum of type " + Value.GetType().Name + " with values ";
        string[] names = Enum.GetNames(Value.GetType());
        Array values = Enum.GetValues(Value.GetType());
        int i = 0;
        foreach (int val in values)
        {
            s += names[i] + "=" + val + " ";
            i++;
        }
        s += ", current value is " + ToString_AttributeValue();
        return s;
    }

    public string ToString_AttributeName()
    {
        return name;
    }

    public string ToString_AttributeValue()
    {
        return Value.ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        // TODO: Handle exception?
        Value = (T)Enum.Parse(Value.GetType(), value);
    }

    public override T GetValue()
    {
        return Value;
    }
}
