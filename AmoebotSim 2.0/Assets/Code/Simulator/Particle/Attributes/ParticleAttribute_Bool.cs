using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing boolean values.
/// </summary>
public class ParticleAttribute_Bool : ParticleAttributeWithHistory<bool>, IParticleAttribute
{
    public ParticleAttribute_Bool(Particle particle, string name, bool value = false) : base(particle, name)
    {
        history = new ValueHistory<bool>(value, particle.system.CurrentRound);
    }

    public override bool GetValue()
    {
        return history.GetValueInRound(particle.system.PreviousRound);
    }

    public override bool GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
        }
        return history.GetMarkedValue();
    }

    public override void SetValue(bool value)
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
        }
        history.RecordValueInRound(value, particle.system.CurrentRound);
    }

    public override string ToString()
    {
        return "ParticleAttribute (bool) with name " + name + " and value " + ToString_AttributeValue();
    }

    public string ToString_AttributeValue()
    {
        return history.GetMarkedValue().ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        if (bool.TryParse(value, out bool parsedVal))
        {
            SetValue(parsedVal);
        }
        else
        {
            throw new System.ArgumentException("Cannot convert " + value + " to bool attribute.");
        }
    }
}
