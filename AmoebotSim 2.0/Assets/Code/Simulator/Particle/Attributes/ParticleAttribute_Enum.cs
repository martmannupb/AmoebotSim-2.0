using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing enum values.
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

    public override T GetValue()
    {
        return history.GetValueInRound(particle.system.PreviousRound);
    }

    public override T GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
        }
        return history.GetMarkedValue();
    }

    public override void SetValue(T value)
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
        }
        history.RecordValueInRound(value, particle.system.CurrentRound);
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

    public string ToString_AttributeValue()
    {
        return history.GetMarkedValue().ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        if (Enum.TryParse(typeof(T), value, true, out object parsedVal))
        {
            SetValue((T)parsedVal);
        }
        else
        {
            throw new System.ArgumentException("Cannot convert " + value + " to enum attribute of type " + GetType());
        }
    }

    public override ParticleAttributeSaveDataBase GenerateSaveData()
    {
        ParticleAttributeEnumSaveData data = new ParticleAttributeEnumSaveData();
        data.name = name;
        data.enumType = typeof(T).FullName;
        data.history = history.GenerateSaveDataString();
        return data;
    }
}
