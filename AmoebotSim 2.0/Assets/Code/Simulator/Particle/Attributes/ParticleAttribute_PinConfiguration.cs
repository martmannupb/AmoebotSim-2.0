using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Figure out how to display and edit this in the UI

/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing
/// pin configurations.
/// <para>
/// To write a value to a variable of this type, the
/// <see cref="SetValue(PinConfiguration)"/> method must be used.
/// It is not sufficient to apply changes to the
/// <see cref="PinConfiguration"/> instance.
/// </para>
/// </summary>
public class ParticleAttribute_PinConfiguration : ParticleAttributeWithHistory<PinConfiguration>, IParticleAttribute
{
    public ParticleAttribute_PinConfiguration(Particle particle, string name, PinConfiguration value = null) : base(particle, name)
    {
        history = new ValueHistory<PinConfiguration>(value, particle.system.CurrentRound);
    }

    public override PinConfiguration GetValue()
    {
        return ((SysPinConfiguration)history.GetValueInRound(particle.system.PreviousRound)).Copy();
    }

    public override PinConfiguration GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
        }
        return ((SysPinConfiguration)history.GetMarkedValue()).Copy();
    }

    public override void SetValue(PinConfiguration value)
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
        }
        history.RecordValueInRound(((SysPinConfiguration)value).Copy(), particle.system.CurrentRound);
    }

    public override string ToString()
    {
        return "ParticleAttribute (PinConfiguration) with name " + name + " and value " + ToString_AttributeValue();
    }

    public string ToString_AttributeValue()
    {
        return history.GetMarkedValue().ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        throw new System.NotImplementedException();
    }
}
