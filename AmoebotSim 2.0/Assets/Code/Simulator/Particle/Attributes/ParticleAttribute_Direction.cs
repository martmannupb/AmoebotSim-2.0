using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute"/> subclass representing direction values.
/// Only values in the set {-1,0,1,2,3,4,5} are permitted.
/// </summary>
public class ParticleAttribute_Direction : ParticleAttributeWithHistory<int>, IParticleAttribute
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
            CheckValue(value);
            history.RecordValueInRound(value, particle.system.CurrentRound);
        }
    }

    public ParticleAttribute_Direction(Particle particle, string name, int value = 0) : base(particle, name)
    {
        CheckValue(value);
        history = new ValueHistory<int>(value, particle.system.CurrentRound);
    }

    private void CheckValue(int val)
    {
        if (val < -1 || val > 5)
        {
            throw new System.ArgumentOutOfRangeException("Direction must be value between -1 and 5, got " + val);
        }
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
        return "ParticleAttribute (direction) with name " + name + " and value " + Value;
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
