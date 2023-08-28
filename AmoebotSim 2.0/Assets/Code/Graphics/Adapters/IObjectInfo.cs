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

        public Vector2Int Position { get; }

        public int Size { get; }

        public int Identifier { get; set; }

        public Color Color { get; set; }
    }

} // namespace AS2.Visuals
