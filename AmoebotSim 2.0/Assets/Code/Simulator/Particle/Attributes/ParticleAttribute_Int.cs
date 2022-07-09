using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing integer values.
/// </summary>
public class ParticleAttribute_Int : ParticleAttributeWithHistory<int>, IParticleAttribute
{
    private int Value
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

    public ParticleAttribute_Int(Particle particle, string name, int value = 0) : base(particle, name)
    {
        history = new ValueHistory<int>(value, particle.system.CurrentRound);
    }

    public override void SetValue(int value)
    {
        Value = value;
    }

    public Type GetAttributeType()
    {
        return System.Type.GetType("int");
    }

    public override string ToString()
    {
        return "ParticleAttribute (int) with name " + name + " and value " + Value;
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
        Value = int.Parse(value);
    }

    public override int GetValue()
    {
        return Value;
    }
}
