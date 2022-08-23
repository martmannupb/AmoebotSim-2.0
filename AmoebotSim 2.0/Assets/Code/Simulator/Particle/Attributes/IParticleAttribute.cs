using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface defining how particle attributes can be used
/// by the System and the UI.
/// </summary>
public interface IParticleAttribute : IReplayHistory
{
    /// <summary>
    /// String representation of the attribute and its
    /// current state.
    /// </summary>
    /// <returns>A string containing the attribute's type, its
    /// display name, and its current value.</returns>
    string ToString();

    /// <summary>
    /// The attribute's name to be displayed in the UI.
    /// </summary>
    /// <returns>The attribute's display name.</returns>
    string ToString_AttributeName();

    /// <summary>
    /// String representation of the attribute's current value.
    /// </summary>
    /// <returns>A string containing the attribute's current value.</returns>
    string ToString_AttributeValue();

    /// <summary>
    /// Updates the attribute's value to the one represented by the given string.
    /// </summary>
    /// <param name="value">String representation of the new value.</param>
    void UpdateAttributeValue(string value);

    /// <summary>
    /// Returns the type of the attribute's value.
    /// </summary>
    /// <returns>The type of value stored in the attribute.</returns>
    Type GetAttributeType();

    /// <summary>
    /// Returns a serializable object representing the attribute's data.
    /// </summary>
    /// <returns>An instance of a subclass of
    /// <see cref="ParticleAttributeSaveDataBase"/> matching the
    /// attribute's type.
    /// </returns>
    ParticleAttributeSaveDataBase GenerateSaveData();

    bool RestoreFromSaveData(ParticleAttributeSaveDataBase data);

    void Print();
}
