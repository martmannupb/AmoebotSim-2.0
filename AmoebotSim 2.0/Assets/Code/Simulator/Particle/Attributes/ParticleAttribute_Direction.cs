using System;
using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing direction values.
/// </summary>
public class ParticleAttribute_Direction : ParticleAttributeWithHistory<Direction>, IParticleAttribute
{
    public ParticleAttribute_Direction(Particle particle, string name, Direction value = Direction.NONE) : base(particle, name)
    {
        history = new ValueHistory<Direction>(value, particle.system.CurrentRound);
    }

    public override Direction GetValue()
    {
        return history.GetValueInRound(particle.system.PreviousRound);
    }

    public override Direction GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
        }
        return history.GetMarkedValue();
    }

    public override void SetValue(Direction value)
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
        }
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
        if (Enum.TryParse(typeof(Direction), value, out object parsedVal))
        {
            SetValue((Direction)parsedVal);
        }
        else
        {
            throw new System.ArgumentException("Cannot convert " + value + " to direction attribute.");
        }
    }

    //public override ParticleAttributeSaveDataBase GenerateSaveData()
    //{
    //    ParticleAttributeSaveData<Direction> data = new ParticleAttributeSaveData<Direction>();
    //    data.name = name;
    //    data.history = history.GenerateSaveData();
    //    return data;
    //}

    ///// <summary>
    ///// Implementation of <see cref="ParticleAttributeWithHistory{T}.RestoreFromSaveData(ParticleAttributeSaveDataBase)"/>.
    ///// Additionally checks if all stored values are valid directions.
    ///// </summary>
    ///// <param name="data">A serializable representation of a
    ///// particle attribute state.</param>
    ///// <returns><c>true</c> if and only if the state update was successful.</returns>
    //public override bool RestoreFromSaveData(ParticleAttributeSaveDataBase data)
    //{
    //    ParticleAttributeSaveData<Direction> myData = data as ParticleAttributeSaveData<Direction>;
    //    if (myData is null)
    //    {
    //        Debug.LogError("Save data has incompatible type, aborting particle attribute restoration.");
    //        return false;
    //    }
    //    history = new ValueHistory<Direction>(myData.history);
    //    return true;
    //}
}
