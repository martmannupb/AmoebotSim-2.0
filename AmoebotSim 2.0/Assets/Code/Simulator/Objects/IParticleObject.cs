using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an object in the particle system that can
/// be detected and moved around by the particles.
/// </summary>
public interface IParticleObject
{
    /// <summary>
    /// The object's identifier. Can be unique to
    /// distinguish all objects from each other or
    /// specify different types or groups of objects.
    /// </summary>
    int Identifier
    {
        get;
    }

    /// <summary>
    /// Gives the object a new color. If multiple
    /// particles set the same object's color, the one
    /// set by the last simulated particle will be used.
    /// </summary>
    /// <param name="color">The new color of the object.</param>
    public abstract void SetColor(Color color);
}
