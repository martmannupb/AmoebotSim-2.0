
/// <summary>
/// <see cref="ParticleAttribute{T}"/> subclass representing integer values.
/// </summary>
public class ParticleAttribute_Int : ParticleAttributeWithHistory<int>, IParticleAttribute
{
    public ParticleAttribute_Int(Particle particle, string name, int value = 0) : base(particle, name)
    {
        history = new ValueHistory<int>(value, particle.system.CurrentRound);
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
        history.RecordValueInRound(value, particle.system.CurrentRound);
    }

    public override string ToString()
    {
        return "ParticleAttribute (int) with name " + name + " and value " + ToString_AttributeValue();
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
            throw new System.ArgumentException("Cannot convert " + value + " to int attribute.");
        }
    }
}
