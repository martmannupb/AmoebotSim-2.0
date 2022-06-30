using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ParticleAttribute
{
    protected Particle particle;
    protected string name;

    public ParticleAttribute(ParticleAlgorithm algorithm, string name)
    {
        this.name = name;
        if (algorithm != null)
            algorithm.AddAttribute(this);
    }

    public void SetParticle(Particle p)
    {
        this.particle = p;
    }

    public abstract override string ToString();
    public abstract string ToString_ParameterName();
    public abstract string ToString_ParameterValue();
    public abstract void UpdateParameterValue(string value);
    public abstract System.Type GetAttributeType();
}
