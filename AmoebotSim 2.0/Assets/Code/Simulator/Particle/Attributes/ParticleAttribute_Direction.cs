using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing direction values.
/// Only values in the set {-1,0,1,2,3,4,5} are permitted.
/// </summary>
public class ParticleAttribute_Direction : ParticleAttributeWithHistory<int>, IParticleAttribute
{
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

    public override int GetValue()
    {
        return history.GetValueInRound(particle.system.PreviousRound);
    }

    public override int GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
        }
        return history.GetMarkedValue();
    }

    public override void SetValue(int value)
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
        }
        CheckValue(value);
        history.RecordValueInRound(value, particle.system.CurrentRound);
    }

    public override string ToString()
    {
        return "ParticleAttribute (direction) with name " + name + " and value " + ToString_AttributeValue();
    }

    public string ToString_AttributeValue()
    {
        return history.GetMarkedValue().ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        if (int.TryParse(value, out int parsedVal))
        {
            SetValue(parsedVal);
        }
        else
        {
            throw new System.ArgumentException("Cannot convert " + value + " to direction attribute.");
        }
    }

    public override bool RestoreFromSaveData(ParticleAttributeSaveDataBase data)
    {
        ParticleAttributeSaveData<int> myData = data as ParticleAttributeSaveData<int>;
        if (myData is null)
        {
            Debug.LogError("Save data has incompatible type, aborting particle attribute restoration.");
            return false;
        }
        // Make sure that all values are in the correct range
        foreach (int val in myData.history.values)
        {
            if (val < -1 || val > 5)
            {
                Debug.LogError("Save data for direction attribute has incompatible value " + val + ", aborting particle attribute restoration.");
                return false;
            }
        }
        history = new ValueHistory<int>(myData.history);
        return true;
    }
}
