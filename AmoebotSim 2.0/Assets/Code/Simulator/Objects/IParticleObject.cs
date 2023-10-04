using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

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

        /// <summary>
        /// Checks whether this object is currently the anchor.
        /// <para>
        /// Note that this information is already available in
        /// the same round in which the object was made the
        /// anchor (unlike, e.g., particle attributes). This
        /// depends on the order in which the particles are
        /// activated, which the particles can neither control
        /// nor find out about.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if and only if the object is
        /// the system's anchor.</returns>
        public abstract bool IsAnchor();

        /// <summary>
        /// Turns this object into the system's anchor.
        /// </summary>
        public abstract void MakeAnchor();

        /// <summary>
        /// Triggers this object to release all bonds to
        /// other objects.
        /// </summary>
        public abstract void ReleaseBonds();
    }

} // namespace AS2.Sim
