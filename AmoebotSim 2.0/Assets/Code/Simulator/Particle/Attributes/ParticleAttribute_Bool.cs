using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing boolean values.
/// </summary>
public class ParticleAttribute_Bool : ParticleAttributeWithHistory<bool>, IParticleAttribute
{
    private bool Value
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

    public ParticleAttribute_Bool(Particle particle, string name, bool value = false) : base(particle, name)
    {
        history = new ValueHistory<bool>(value, particle.system.CurrentRound);
    }

    public override void SetValue(bool value)
    {
        Value = value;
    }

    public Type GetAttributeType()
    {
        return System.Type.GetType("bool");
    }

    public override string ToString()
    {
        return "ParticleAttribute (bool) with name " + name + " and value " + Value.ToString();
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
        Value = bool.Parse(value);
    }

    public override bool GetValue()
    {
        return Value;
    }
}
