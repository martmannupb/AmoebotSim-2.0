using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParticleAttribute
{
    protected Particle particle;
    protected string name;

    public ParticleAttribute(Particle particle, string name)
    {
        this.particle = particle;
        if (particle != null)
            particle.AddAttribute(this);
        this.name = name;
    }

    public abstract override string ToString();
    public abstract string ToString_ParameterName();
    public abstract string ToString_ParameterValue();
    public abstract void UpdateParameterValue(string value);
    public abstract System.Type GetAttributeType();
}
