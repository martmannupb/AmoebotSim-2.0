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
    // This implementation internally uses a specific kind of history - it does not store SysPinConfiguration instances
    // This also means that most of the API methods have to be overridden
    private ValueHistoryPinConfiguration pcHistory;

    public ParticleAttribute_PinConfiguration(Particle particle, string name, PinConfiguration value = null) : base(particle, name)
    {
        history = null;
        pcHistory = new ValueHistoryPinConfiguration(value as SysPinConfiguration, particle.system.CurrentRound);
    }

    public override PinConfiguration GetValue()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' pin configurations!");
        }
        return pcHistory.GetValueInRound(particle.system.PreviousRound, particle);
    }

    public override PinConfiguration GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' pin configurations!");
        }
        return pcHistory.GetMarkedValue(particle);
    }

    public override void SetValue(PinConfiguration value)
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to write other particles' attributes directly!");
        }
        pcHistory.RecordValueInRound(value as SysPinConfiguration, particle.system.CurrentRound);
    }

    public override string ToString()
    {
        return "ParticleAttribute (PinConfiguration) with name " + name + " and value " + ToString_AttributeValue();
    }

    public string ToString_AttributeValue()
    {
        return pcHistory.GetMarkedValue(particle).ToString();
    }

    public void UpdateAttributeValue(string value)
    {
        throw new System.NotImplementedException();
    }

    // Override methods of ParticleAttributeWithHistory

    public override int GetFirstRecordedRound()
    {
        return pcHistory.GetFirstRecordedRound();
    }

    public override bool IsTracking()
    {
        return pcHistory.IsTracking();
    }

    public override void SetMarkerToRound(int round)
    {
        pcHistory.SetMarkerToRound(round);
    }

    public override void StepBack()
    {
        pcHistory.StepBack();
    }

    public override void StepForward()
    {
        pcHistory.StepForward();
    }

    public override int GetMarkedRound()
    {
        return pcHistory.GetMarkedRound();
    }

    public override void ContinueTracking()
    {
        pcHistory.ContinueTracking();
    }

    public override void CutOffAtMarker()
    {
        pcHistory.CutOffAtMarker();
    }

    public override void ShiftTimescale(int amount)
    {
        pcHistory.ShiftTimescale(amount);
    }

    /**
     * Some methods of IParticleAttribute interface can already be implemented here
     */

    public override ParticleAttributeSaveDataBase GenerateSaveData()
    {
        ParticleAttributePCSaveData data = new ParticleAttributePCSaveData();
        data.name = name;
        data.history = pcHistory.GeneratePCSaveData();
        return data;
    }

    public override bool RestoreFromSaveData(ParticleAttributeSaveDataBase data)
    {
        ParticleAttributePCSaveData myData = data as ParticleAttributePCSaveData;
        if (myData is null)
        {
            Debug.LogError("Save data for pin configuration attribute has incompatible type, aborting particle attribute restoration.");
            return false;
        }
        pcHistory = new ValueHistoryPinConfiguration(myData.history);
        return true;
    }
}
