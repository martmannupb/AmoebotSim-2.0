using System;

namespace Simulator
{

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
        /// <returns><c>true</c> if and only if the given string was parsed
        /// successfully.</returns>
        bool UpdateAttributeValue(string value);

        /// <summary>
        /// Returns the type of the attribute's value.
        /// </summary>
        /// <returns>The type of value stored in the attribute.</returns>
        Type GetAttributeType();

        /// <summary>
        /// Resets the attribute's intermediate value state. Must be called
        /// after each simulated round.
        /// </summary>
        void ResetIntermediateValue();

        object GetObjectValue();

        /// <summary>
        /// Returns a serializable object representing the attribute's data.
        /// </summary>
        /// <returns>An instance of a subclass of
        /// <see cref="ParticleAttributeSaveDataBase"/> matching the
        /// attribute's type.
        /// </returns>
        ParticleAttributeSaveDataBase GenerateSaveData();

        /// <summary>
        /// Loads the attribute data from the given serializable object and
        /// resets this attribute to the state encoded in that data.
        /// </summary>
        /// <param name="data">A serializable representation of a
        /// particle attribute state.</param>
        /// <returns><c>true</c> if and only if the state update was
        /// successful.</returns>
        bool RestoreFromSaveData(ParticleAttributeSaveDataBase data);

        // TEMPORARY FOR DEBUGGING

        void Print();
    }

} // namespace Simulator
