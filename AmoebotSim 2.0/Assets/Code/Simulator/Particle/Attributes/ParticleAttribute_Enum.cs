using System;
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
        if (particle.system.InMovePhase || !hasIntermediateVal)
        {
            return history.GetValueInRound(particle.system.PreviousRound);
        }
        else
        {
            return intermediateVal;
        }
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
        if (particle.system.InMovePhase)
        {
            hasIntermediateVal = true;
            intermediateVal = value;
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

    public bool UpdateAttributeValue(string value)
    {
        if (Enum.TryParse(typeof(T), value, true, out object parsedVal))
        {
            history.RecordValueInRound((T)parsedVal, particle.system.CurrentRound);
            return true;
        }
        else
        {
            Debug.LogWarning("Cannot convert " + value + " to enum attribute of type " + GetType());
            return false;
        }
    }

    /// <summary>
    /// Implementation of <see cref="ParticleAttributeWithHistory{T}.GenerateSaveData"/>.
    /// Generates data specifically for enum attributes, which includes the name of the
    /// enum type so that it can be restored when loading.
    /// </summary>
    /// <returns>A serializable representation of the attribute's state.</returns>
    public override ParticleAttributeSaveDataBase GenerateSaveData()
    {
        ParticleAttributeEnumSaveData data = new ParticleAttributeEnumSaveData();
        data.name = name;
        data.enumType = typeof(T).FullName;
        data.history = history.GenerateSaveDataString();
        return data;
    }

    /// <summary>
    /// Implementation of <see cref="ParticleAttributeWithHistory{T}.RestoreFromSaveData(ParticleAttributeSaveDataBase)"/>.
    /// Uses additional information stored in specific enum attribute save data
    /// to restore the correct enum type.
    /// </summary>
    /// <param name="data">A serializable representation of a
    /// particle attribute state.</param>
    /// <returns><c>true</c> if and only if the state update was successful.</returns>
    public override bool RestoreFromSaveData(ParticleAttributeSaveDataBase data)
    {
        ParticleAttributeEnumSaveData myData = data as ParticleAttributeEnumSaveData;
        if (myData is null || Type.GetType(myData.enumType) != typeof(T))
        {
            Debug.LogError("Save data for enum has incompatible type, aborting particle attribute restoration.");
            return false;
        }
        // Try to convert all values into enum types
        ValueHistorySaveData<T> enumHistory = new ValueHistorySaveData<T>();
        enumHistory.values = new List<T>(myData.history.values.Count);
        foreach (string enumValStr in myData.history.values)
        {
            object enumVal;
            if (Enum.TryParse(typeof(T), enumValStr, true, out enumVal))
            {
                enumHistory.values.Add((T)enumVal);
            }
            else
            {
                Debug.LogError("Unknown saved enum value '" + enumValStr + "', aborting particle attribute restoration.");
                return false;
            }
        }

        enumHistory.rounds = myData.history.rounds;
        enumHistory.lastRound = myData.history.lastRound;

        history = new ValueHistory<T>(enumHistory);
        return true;
    }
}
