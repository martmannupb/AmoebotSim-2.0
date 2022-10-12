using UnityEngine;

/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing float values.
/// </summary>
public class ParticleAttribute_Float : ParticleAttributeWithHistory<float>, IParticleAttribute
{
    public ParticleAttribute_Float(Particle particle, string name, float value = 0f) : base(particle, name)
    {
        history = new ValueHistory<float>(value, particle.system.CurrentRound);
    }

    public override float GetValue()
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

    public override float GetValue_After()
    {
        if (!particle.isActive)
        {
            throw new System.InvalidOperationException("Particles are not allowed to read other particles' updated states!");
        }
        return history.GetMarkedValue();
    }

    public override void SetValue(float value)
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
        return "ParticleAttribute (float) with name " + name + " and value " + ToString_AttributeValue();
    }

    public string ToString_AttributeValue()
    {
        return history.GetMarkedValue().ToString();
    }

    public bool UpdateAttributeValue(string value)
    {
        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedVal))
        {
            history.RecordValueInRound(parsedVal, particle.system.CurrentRound);
            return true;
        }
        else
        {
            Debug.LogWarning("Cannot convert " + value + " to float attribute.");
            return false;
        }
    }
}
