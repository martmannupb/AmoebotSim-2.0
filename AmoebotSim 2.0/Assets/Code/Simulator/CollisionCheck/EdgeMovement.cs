// This file belongs to the AmoebotSim 2.0 project, a simulator for the
// geometric amoebot model with reconfigurable circuits and joint movements.
//
// Copyright (c) 2025 AmoebotSim 2.0 Authors.
//
// Licensed under the MIT License. See LICENSE file in the root directory for details.


using System.Collections.Generic;
using UnityEngine;

namespace AS2.Sim
{

    /// <summary>
    /// Represents the movement of a length 1 edge
    /// during one round for the collision check.
    /// <para>
    /// If the edge belongs to an expanding or contracting
    /// particle, its start and end points will coincide
    /// either before or after the movement.
    /// </para>
    /// <para>
    /// Use the pooling methods <see cref="Create(Vector2Int, Vector2Int, Vector2Int, Vector2Int)"/>
    /// and <see cref="Release(EdgeMovement)"/> to avoid allocating new memory
    /// for every new edge.
    /// </para>
    /// </summary>
    public class EdgeMovement
    {
        /// <summary>
        /// The start position before the movement.
        /// </summary>
        public Vector2Int start1;
        /// <summary>
        /// The end position before the movement.
        /// </summary>
        public Vector2Int end1;
        /// <summary>
        /// The start position after the movement.
        /// </summary>
        public Vector2Int start2;
        /// <summary>
        /// The end position after the movement.
        /// </summary>
        public Vector2Int end2;

        private EdgeMovement(Vector2Int s1, Vector2Int e1, Vector2Int s2, Vector2Int e2)
        {
            start1 = s1;
            end1 = e1;
            start2 = s2;
            end2 = e2;
        }

        public void Reset(Vector2Int s1, Vector2Int e1, Vector2Int s2, Vector2Int e2)
        {
            start1 = s1;
            end1 = e1;
            start2 = s2;
            end2 = e2;
        }

        public void Reset()
        {
            start1 = Vector2Int.zero;
            end1 = Vector2Int.zero;
            start2 = Vector2Int.zero;
            end2 = Vector2Int.zero;
        }

        /// <summary>
        /// Calculates the movement of the edge's start position.
        /// </summary>
        /// <returns>The vector pointing from the edge's start
        /// position before the movement to its position after
        /// the movement.</returns>
        public Vector2Int StartTranslation()
        {
            return start2 - start1;
        }

        /// <summary>
        /// Calculates the movement of the edge's end position.
        /// </summary>
        /// <returns>The vector pointing from the edge's end
        /// position before the movement to its position after
        /// the movement.</returns>
        public Vector2Int EndTranslation()
        {
            return end2 - end1;
        }

        public override string ToString()
        {
            return start1 + "-" + end1 + "  -->  " + start2 + "-" + end2;
        }

        // Pooling

        private static Stack<EdgeMovement> pool = new Stack<EdgeMovement>();

        /// <summary>
        /// Creates an instance with the given points using pooling.
        /// </summary>
        /// <param name="s1">The start location before the movement.</param>
        /// <param name="e1">The end location before the movement.</param>
        /// <param name="s2">The start location after the movement.</param>
        /// <param name="e2">The end location after the movement.</param>
        /// <returns>A newly initialized instance.</returns>
        public static EdgeMovement Create(Vector2Int s1, Vector2Int e1, Vector2Int s2, Vector2Int e2)
        {
            if (pool.Count > 0)
            {
                EdgeMovement em = pool.Pop();
                em.Reset(s1, e1, s2, e2);
                return em;
            }
            else
            {
                return new EdgeMovement(s1, e1, s2, e2);
            }
        }

        /// <summary>
        /// Releases the given instance into the pool.
        /// Use this when the instance is not used any more to
        /// recycle the allocated memory.
        /// </summary>
        /// <param name="em">The instance to be reinserted
        /// into the pool.</param>
        public static void Release(EdgeMovement em)
        {
            pool.Push(em);
        }
    }

} // namespace AS2.Sim
