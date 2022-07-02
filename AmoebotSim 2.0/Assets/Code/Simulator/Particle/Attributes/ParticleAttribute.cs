using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representation of an attribute that is part of a particle's state.
/// <para>
/// <see cref="ParticleAlgorithm"/> subclasses should use instances of
/// <see cref="ParticleAttribute"/> subclasses to represent their state
/// variables. Only these attributes are displayed and editable in the
/// simulation UI and recorded in a simulation history. They also provide
/// correct read and write behavior in synchronous rounds, i.e., if a
/// <see cref="ParticleAttribute"/> is read by the particle it belongs to,
/// the value will be the most recently written one, but if another
/// particle reads its value, the value from the previous round will be
/// returned.
/// </para>
/// <para>
/// The conventional way to setup a <see cref="ParticleAttribute"/> in a
/// <see cref="ParticleAlgorithm"/> subclass is the following:
/// <code>
/// public class MyAlgorithm : ParticleAlgorithm {
///     public ParticleAttribute_TYPE myAttr;
///     public MyAlgorithm(Particle p) : base(p) {
///         myAttr = new ParticleAttribute_TYPE(this, "Fancy display name", INITIAL_VALUE);
///     }
/// }
/// </code>
/// After that, the value of a <c>ParticleAttribute_TYPE</c> variable can
/// be read as if it was a variable of type <c>TYPE</c>, even if the
/// variable of another particle is read. To write the value, however,
/// the <c>SetValue</c> method must be used.
/// </para>
/// </summary>
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
    public abstract string ToString_AttributeName();
    public abstract string ToString_AttributeValue();
    public abstract void UpdateAttributeValue(string value);
    public abstract System.Type GetAttributeType();
}
