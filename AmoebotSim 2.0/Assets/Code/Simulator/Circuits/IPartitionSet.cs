using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Developer API for partition sets.
/// <para>
/// This is part of the pin configuration API, see
/// <see cref="IPinConfiguration"/>.
/// </para>
/// <para>
/// A partition set is a set of connected pins within
/// a pin configuration. It belongs to an object
/// implementing the <see cref="IPinConfiguration"/>
/// interface and may be changed through methods of
/// this API or through the pin configuration itself.
/// <br/>
/// See also <seealso cref="IPin"/>.
/// </para>
/// </summary>
public interface IPartitionSet
{
    /// <summary>
    /// The <see cref="IPinConfiguration"/> to which this
    /// partition set belongs.
    /// </summary>
    IPinConfiguration PinConfiguration
    {
        get;
    }

    /// <summary>
    /// The ID of this partition set.
    /// <para>
    /// Partition set IDs range from <c>0</c> to
    /// <c><see cref="IPinConfiguration.NumPins"/> - 1</c>.
    /// </para>
    /// </summary>
    int Id
    {
        get;
    }

    /// <summary>
    /// Checks if this partition set is empty.
    /// </summary>
    /// <returns><c>true</c> if and only if this partition
    /// set does not contain any pins.</returns>
    bool IsEmpty();

    /// <summary>
    /// Returns the IDs of all pins contained in this
    /// partition set.
    /// <para>
    /// See also <seealso cref="GetPins"/>.
    /// </para>
    /// </summary>
    /// <returns>An array containing the IDs of all pins
    /// contained in this partition set, in ascending
    /// order.</returns>
    int[] GetPinIds();

    /// <summary>
    /// Returns all pins contained in this partition set.
    /// <para>
    /// See also <seealso cref="GetPinIds"/>.
    /// </para>
    /// </summary>
    /// <returns>An array containing all pins contained
    /// in this partition set, ordered by their IDs.</returns>
    IPin[] GetPins();

    /// <summary>
    /// Checks if the specified pin is contained in this
    /// partition set.
    /// <para>
    /// See also <seealso cref="ContainsPin(IPin)"/>.
    /// </para>
    /// </summary>
    /// <param name="pinId">The ID of the pin to be checked.</param>
    /// <returns><c>true</c> if and only if this partition set
    /// contains a pin with ID <paramref name="pinId"/>.</returns>
    bool ContainsPin(int pinId);

    /// <summary>
    /// Checks if the given pin is contained in this
    /// partition set.
    /// <para>
    /// See also <seealso cref="ContainsPin(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="pin">The pin to be checked.</param>
    /// <returns><c>true</c> if and only if this partition set
    /// contains a pin with the same ID as the givne pin.</returns>
    bool ContainsPin(IPin pin);

    // TODO: Add bool return value to indicate what happened?

    /// <summary>
    /// Adds the specified pin to this partition set.
    /// <para>
    /// If the pin is not already contained in this set, it is
    /// removed from the set it is currently contained in.
    /// </para>
    /// <para>
    /// See also <seealso cref="AddPin(IPin)"/>.
    /// </para>
    /// </summary>
    /// <param name="pinId">The ID of the pin to be added.</param>
    void AddPin(int pinId);

    /// <summary>
    /// Adds the given pin to this partition set.
    /// <para>
    /// If the pin is not already contained in this set, it is
    /// removed from the set it is currently contained in.
    /// </para>
    /// <para>
    /// See also <seealso cref="AddPin(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="pin">The pin to be added.</param>
    void AddPin(IPin pin);

    /// <summary>
    /// Adds the specified pins to this partition set.
    /// <para>
    /// Has the same effect as calling <see cref="AddPin(int)"/>
    /// multiple times, but may be more efficient.
    /// </para>
    /// <para>
    /// See also <seealso cref="AddPins(IPin[])"/>.
    /// </para>
    /// </summary>
    /// <param name="pinIds">An array containing the IDs of
    /// the pins to be added.</param>
    void AddPins(int[] pinIds);

    /// <summary>
    /// Adds the given pins to this partition set.
    /// <para>
    /// Has the same effect as calling <see cref="AddPin(IPin)"/>
    /// multiple times, but may be more efficient.
    /// </para>
    /// <para>
    /// See also <seealso cref="AddPins(int[])"/>.
    /// </para>
    /// </summary>
    /// <param name="pins">An array containing the pins
    /// to be added.</param>
    void AddPins(IPin[] pins);

    /// <summary>
    /// Removes the specified pin from this partition set.
    /// <para>
    /// If the pin is currently contained in this partition
    /// set, it is added to its own singleton partition set.
    /// </para>
    /// <para>
    /// See also <seealso cref="RemovePin(IPin)"/>.
    /// </para>
    /// </summary>
    /// <param name="pinId">The ID of the pin to be removed.</param>
    void RemovePin(int pinId);

    /// <summary>
    /// Removes the given pin from this partition set.
    /// <para>
    /// If the pin is currently contained in this partition
    /// set, it is added to its own singleton partition set.
    /// </para>
    /// <para>
    /// See also <seealso cref="RemovePin(int)"/>.
    /// </para>
    /// </summary>
    /// <param name="pin">The pin to be removed.</param>
    void RemovePin(IPin pin);

    /// <summary>
    /// Removes the specified pins from this partition set.
    /// <para>
    /// Has the same effect as calling <see cref="RemovePin(int)"/>
    /// multiple times, but may be more efficient.
    /// </para>
    /// <para>
    /// See also <seealso cref="RemovePins(IPin[])"/>.
    /// </para>
    /// </summary>
    /// <param name="pinIds">An array containing the IDs of
    /// the pins to be removed.</param>
    void RemovePins(int[] pinIds);

    /// <summary>
    /// Removes the given pins from this partition set.
    /// <para>
    /// Has the same effect as calling <see cref="RemovePin(IPin)"/>
    /// multiple times, but may be more efficient.
    /// </para>
    /// <para>
    /// See also <seealso cref="RemovePins(int[])"/>.
    /// </para>
    /// </summary>
    /// <param name="pins">An array containing the pins to
    /// be removed.</param>
    void RemovePins(IPin[] pins);

    /// <summary>
    /// Merges the specified partition set into this one.
    /// </summary>
    /// <param name="otherId">The ID of the partition set whose
    /// pins should be moved to this set.</param>
    void Merge(int otherId);

    /// <summary>
    /// Merges the given partition set into this one.
    /// </summary>
    /// <param name="other">The partition set whose pins should
    /// be moved to this set.</param>
    void Merge(IPartitionSet other);
}
