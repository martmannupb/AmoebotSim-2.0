using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace AS2.ShapeContainment
{

    /// <summary>
    /// Types of convex shapes that can be checked
    /// by a containment procedure.
    /// </summary>
    public enum ShapeType
    {
        TRIANGLE = 0,
        PARALLELOGRAM = 1,
        TRAPEZOID = 2,
        PENTAGON = 3,
        HEXAGON = 4
    }

    /// <summary>
    /// Serializable container for shapes with additional information.
    /// Contains constituent info for star convex shapes and dependency
    /// tree info for snowflakes.
    /// </summary>
    [Serializable]
    public class ShapeContainer
    {
        /// <summary>
        /// Data container for a star convex shape's constituent information.
        /// </summary>
        //     __ Direction H
        //      /\
        //     /
        //      _____
        //     /     \
        //  d /       \
        //   /        /
        //  /________/ c     ---> Direction W
        //       a
        // a2 = a + c
        // a3 = a + 1
        [Serializable]
        public class Constituent
        {
            public ShapeType shapeType = ShapeType.TRIANGLE;
            public Direction directionW = Direction.NONE;
            public Direction directionH = Direction.NONE;
            public int a = 0;
            public int d = 0;
            public int c = 0;
            public int a2 = 0;
            public int a3 = 0;
        }

        /// <summary>
        /// Data container for a snowflake dependency tree's
        /// child information.
        /// </summary>
        [Serializable]
        public class DTreeChild
        {
            /// <summary>
            /// The array index of the child.
            /// </summary>
            public int childIdx;
            /// <summary>
            /// The direction in which the child is located.
            /// </summary>
            public int direction;
            /// <summary>
            /// The distance at which the child is located.
            /// </summary>
            public int distance;
            /// <summary>
            /// The number of 60 degree counter-clockwise rotations of
            /// the child shape relative to its parent.
            /// </summary>
            public int rotation;
        }

        /// <summary>
        /// Data container for a snowflake's dependency tree
        /// node information.
        /// </summary>
        [Serializable]
        public class DTreeNode
        {
            /// <summary>
            /// Array of size 6 giving the length of each "arm" in the
            /// direction indicated by the index.
            /// </summary>
            public int[] arms;

            /// <summary>
            /// Composition edges defining this node's children.
            /// </summary>
            public DTreeChild[] children;
        }

        /// <summary>
        /// The shape represented by this container.
        /// </summary>
        public Shape shape;
        /// <summary>
        /// The constituents of the shape, if it is star convex.
        /// </summary>
        public Constituent[] constituents;
        /// <summary>
        /// Dependency tree of the snowflake, if the shape is a snowflake.
        /// This a list of nodes, where each node is identified by its index,
        /// and the nodes are in a topological order. The last node in the
        /// array represents the complete shape (the root node).
        /// </summary>
        public DTreeNode[] dependencyTree;

        /// <summary>
        /// Reads a shape from a JSON file, including its constituents
        /// and/or dependency tree, if it has those. The shape is not
        /// checked for consistency.
        /// </summary>
        /// <param name="path">The path to the JSON file.</param>
        /// <returns>The <see cref="Shape"/> encoded in the given
        /// JSON file, or <c>null</c> if the file could not be
        /// found or decoded.</returns>
        public static ShapeContainer ReadFromJson(string path)
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                ShapeContainer sc = JsonUtility.FromJson<ShapeContainer>(json);
                return sc;
            }
            catch (Exception e)
            {
                Log.Error("Unable to read shape from JSON file: " + e);
            }
            return null;
        }
    }

} // namespace AS2.ShapeContainment
