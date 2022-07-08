using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representation of an attribute that is part of a particle's state.
/// <para>
/// <see cref="ParticleAlgorithm"/> subclasses should use instances of
/// <see cref="ParticleAttribute{T}"/> to represent their state
/// variables. Only these attributes are displayed and editable in the
/// simulation UI and recorded in a simulation history. They also provide
/// correct read and write behavior in synchronous rounds, i.e., if a
/// <see cref="ParticleAttribute{T}"/> is read by the particle it belongs to,
/// the value will be the most recently written one, but if another
/// particle reads its value, the value from the previous round will be
/// returned.
/// </para>
/// <para>
/// <see cref="ParticleAttribute{T}"/>s are created using the factory
/// methods provided in the <see cref="ParticleAlgorithm"/> base class
/// (see <see cref="ParticleAlgorithm.CreateAttributeInt(string, int)"/> etc.).
/// For example, an integer attribute is declared and initialized with the
/// value <c>3</c> as follows:
/// <code>
/// public class MyAlgorithm : ParticleAlgorithm {
///     public ParticleAttribute<![CDATA[<int>]]> myAttr;
///     public MyAlgorithm(Particle p) : base(p) {
///         myAttr = CreateAttributeInt("Fancy display name", 3);
///     }
/// }
/// </code>
/// After that, the value of a <see cref="ParticleAttribute{T}"/> variable
/// be read as if it was a variable of type <typeparamref name="T"/>, even
/// if the variable of another particle is read. To write the value, however,
/// the <see cref="SetValue(T)"/> method must be used.
/// </para>
/// </summary>
/// <typeparam name="T">The type of values the attribute stores.</typeparam>
public abstract class ParticleAttribute<T> : ParticleAttributeBase
{
    public ParticleAttribute(Particle particle, string name) : base(particle, name) { }

    public abstract T GetValue();

    public abstract void SetValue(T value);

    public static implicit operator T(ParticleAttribute<T> attr) => attr.GetValue();
}
