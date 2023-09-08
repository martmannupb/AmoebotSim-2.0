using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.Visuals
{

    /// <summary>
    /// Interface implemented by objects so that they can be
    /// selected and modified through the UI.
    /// </summary>
    public interface IObjectInfo
    {
        /// <summary>
        /// Computes the set of grid nodes occupied by the object.
        /// </summary>
        /// <returns>The global coordinates of all grid nodes
        /// occupied by the object.</returns>
        public ICollection<Vector2Int> OccupiedPositions();

        /// <summary>
        /// Checks whether the given global grid position is
        /// adjacent to the object.
        /// </summary>
        /// <param name="pos">The global grid position to check.</param>
        /// <returns><c>true</c> if and only if <paramref name="pos"/>
        /// is adjacent to at least one position occupied by the
        /// object but is not occupied itself.</returns>
        public bool IsNeighborPosition(Vector2Int pos);

        /// <summary>
        /// Adds the given global grid position to the object.
        /// Ideally, the position should be adjacent to the
        /// object. Disconnected objects will prevent the
        /// simulation from being started.
        /// </summary>
        /// <param name="pos">The global grid position to be added.</param>
        public void AddPosition(Vector2Int pos);

        /// <summary>
        /// Removes the given global grid position from the object, if
        /// the object remains connected and non-empty.
        /// <para>
        /// Note that the object's origin position will change if the
        /// node at the origin is removed.
        /// </para>
        /// </summary>
        /// <param name="pos">The global grid position to be removed.</param>
        /// <returns><c>true</c> if and only if the given position
        /// was successfully removed from the object.</returns>
        public bool RemovePosition(Vector2Int pos);

        /// <summary>
        /// Moves the object to the given global coordinates.
        /// </summary>
        /// <param name="newPos">The global grid coordinates
        /// to which the object should be moved.</param>
        /// <returns><c>true</c> if and only if the object
        /// was moved successfully.</returns>
        public bool MoveToPosition(Vector2Int newPos);

        /// <summary>
        /// Removes the object from the particle system and
        /// the render system.
        /// </summary>
        public void RemoveFromSystem();

        /// <summary>
        /// Checks whether the object occupies a
        /// connected set of grid nodes if the given node
        /// is removed.
        /// </summary>
        /// <param name="removePosition">The global grid
        /// position that should be removed from the object.
        /// This position is not considered as occupied in
        /// the connectivity check.</param>
        /// <returns><c>true</c> if and only if the
        /// object is still a connected shape after removing
        /// the given position.</returns>
        public bool IsConnected(Vector2Int removePosition);

        /// <summary>
        /// Checks whether this object is currently the anchor.
        /// </summary>
        /// <returns><c>true</c> if and only if the object
        /// is the anchor.</returns>
        public bool IsAnchor();

        /// <summary>
        /// Turns this object into the anchor of the system.
        /// </summary>
        public void MakeAnchor();

        public Vector2Int Position { get; }

        public int Size { get; }

        public int Identifier { get; set; }

        public Color Color { get; set; }
    }

} // namespace AS2.Visuals
