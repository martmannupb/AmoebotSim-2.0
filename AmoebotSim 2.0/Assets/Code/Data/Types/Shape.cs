using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AS2.ShapeContainment
{

    /// <summary>
    /// Generic representation of a shape for shape containment algorithms.
    /// <para>
    /// A shape description consists of a list of nodes, a list of edges
    /// and a list of faces. Nodes are global grid coordinates, edges are
    /// pairs of node indices and faces are triples of node indices.
    /// </para>
    /// <para>
    /// Call <see cref="GenerateTraversal"/> to create a traversal that can
    /// be used by amoebots to construct the shape (this might invert some
    /// edges).
    /// </para>
    /// <para>
    /// The class is serializable by Unity's <c>JsonUtility</c>.
    /// </para>
    /// </summary>
    [Serializable]
    public class Shape
    {
        [Serializable]
        public struct Node
        {
            public int x;
            public int y;

            public Node(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public static implicit operator Vector2Int(Node n) => new Vector2Int(n.x, n.y);
        }

        [Serializable]
        public struct Edge
        {
            public int u;
            public int v;

            public Edge(int u, int v)
            {
                this.u = u;
                this.v = v;
            }
        }

        [Serializable]
        public struct Face
        {
            public int u;
            public int v;
            public int w;

            public Face(int u, int v, int w)
            {
                this.u = u;
                this.v = v;
                this.w = w;
            }
        }

        public List<Node> nodes = new List<Node>();
        public List<Edge> edges = new List<Edge>();
        public List<Face> faces = new List<Face>();

        public List<int> traversal;

        /// <summary>
        /// Checks whether this shape is internally consistent.
        /// A shape is not consistent if it is empty, contains no origin, contains
        /// a duplicate node, edge or face, contains an edge or face that has
        /// invalid indices or references non-neighboring nodes, does not contain
        /// all edges of a face, or if it is not connected.
        /// </summary>
        /// <returns><c>true</c> if and only if the shape is internally
        /// consistent.</returns>
        public bool IsConsistent()
        {
            // Search for node (0, 0) (must have origin)
            bool haveOrigin = false;
            foreach (Node n in nodes)
            {
                if (n.x == 0 && n.y == 0)
                {
                    haveOrigin = true;
                    break;
                }
            }
            if (!haveOrigin)
            {
                Log.Warning("Shape has no origin");
                return false;
            }

            // Search for duplicates
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Node n1 = nodes[i];
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    Node n2 = nodes[j];
                    if (n1.x == n2.x && n1.y == n2.y)
                    {
                        Log.Warning("Shape has duplicate node");
                        return false;
                    }
                }
            }

            for (int i = 0; i < edges.Count - 1; i++)
            {
                Edge e1 = edges[i];
                for (int j = i + 1; j < edges.Count; j++)
                {
                    Edge e2 = edges[j];
                    if (e1.u == e2.u && e1.v == e2.v || e1.u == e2.v && e1.v == e2.u)
                    {
                        Log.Warning("Shape has duplicate edge");
                        return false;
                    }
                }
            }

            for (int i = 0; i < faces.Count - 1; i++)
            {
                Face f1 = faces[i];
                HashSet<int> nodes1 = new HashSet<int>();
                nodes1.Add(f1.u);
                nodes1.Add(f1.v);
                nodes1.Add(f1.w);
                for (int j = i + 1; j < faces.Count; j++)
                {
                    Face f2 = faces[j];
                    if (nodes1.Contains(f2.u) && nodes1.Contains(f2.v) && nodes1.Contains(f2.w))
                    {
                        Log.Warning("Shape has duplicate face");
                        return false;
                    }
                }
            }

            // Check if all edges make sense
            foreach (Edge e in edges)
            {
                if (e.u < 0 || e.u >= nodes.Count || e.v < 0 || e.v >= nodes.Count)
                {
                    Log.Warning("Shape has edge with invalid index");
                    return false;
                }

                if (!AmoebotFunctions.AreNodesNeighbors(nodes[e.u], nodes[e.v]))
                {
                    Log.Warning("Shape has invalid edge");
                    return false;
                }
            }

            // Check if all faces make sense
            foreach (Face f in faces)
            {
                if (f.u < 0 || f.u >= nodes.Count || f.v < 0 || f.v >= nodes.Count || f.w < 0 || f.w >= nodes.Count)
                {
                    Log.Warning("Shape has face with invalid index");
                    return false;
                }

                if (!AmoebotFunctions.AreNodesNeighbors(nodes[f.u], nodes[f.v]) ||
                    !AmoebotFunctions.AreNodesNeighbors(nodes[f.u], nodes[f.w]) ||
                    !AmoebotFunctions.AreNodesNeighbors(nodes[f.v], nodes[f.w]))
                {
                    Log.Warning("Shape has invalid face");
                    return false;
                }

                if (!ContainsEdge(f.u, f.v) || !ContainsEdge(f.u, f.w) || !ContainsEdge(f.v, f.w))
                {
                    Log.Warning("Shape has incomplete face");
                    return false;
                }
            }

            // Check for connectivity
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                visited.Add(current);
                foreach (Edge e in edges)
                {
                    if (e.u == current && !visited.Contains(e.v) && !queue.Contains(e.v))
                    {
                        queue.Enqueue(e.v);
                    }
                    else if (e.v == current && !visited.Contains(e.u) && !queue.Contains(e.u))
                    {
                        queue.Enqueue(e.u);
                    }
                }
            }
            if (visited.Count < nodes.Count)
            {
                Log.Warning("Shape is not connected");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates a traversal (list of edge indices) of the
        /// whole shape that starts at the origin. Some edges
        /// may be reversed in the process.
        /// <para>
        /// Make sure to call <see cref="IsConsistent"/> before
        /// generating a traversal.
        /// </para>
        /// </summary>
        public void GenerateTraversal()
        {
            HashSet<int> visitedNodes = new HashSet<int>();
            HashSet<int> usedEdges = new HashSet<int>();
            traversal = new List<int>();

            // Add origin to visited nodes
            int origin = -1;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].x == 0 && nodes[i].y == 0)
                {
                    origin = i;
                    break;
                }
            }
            if (origin == -1)
            {
                Log.Warning("Shape has no origin");
                return;
            }
            visitedNodes.Add(origin);

            while (usedEdges.Count < edges.Count)
            {
                bool foundEdge = false;

                for (int i = 0; i < edges.Count; i++)
                {
                    Edge e = edges[i];
                    if (usedEdges.Contains(i))
                    {
                        continue;
                    }

                    if (visitedNodes.Contains(e.u))
                    {
                        foundEdge = true;
                    }
                    else if (visitedNodes.Contains(e.v))
                    {
                        // Reverse the edge
                        e = new Edge(e.v, e.u);
                        edges[i] = e;
                        foundEdge = true;
                    }

                    if (foundEdge)
                    {
                        traversal.Add(i);
                        usedEdges.Add(i);
                        visitedNodes.Add(e.v);
                        break;
                    }
                }

                if (!foundEdge)
                {
                    Log.Warning("Failed to add edge to traversal");
                    return;
                }
            }
        }

        /// <summary>
        /// Checks whether the shape contains the given edge
        /// in any direction.
        /// </summary>
        /// <param name="u">The first end point of the edge.</param>
        /// <param name="v">The second end point of the edge.</param>
        /// <returns><c>true</c> if and only if the shape contains
        /// an (undirected) edge between nodes <paramref name="u"/>
        /// and <paramref name="v"/>.</returns>
        private bool ContainsEdge(int u, int v)
        {
            foreach (Edge e in edges)
            {
                if (e.u == u && e.v == v || e.u == v && e.v == u)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reads a shape from a JSON file. The shape is not
        /// checked for consistency.
        /// </summary>
        /// <param name="path">The path to the JSON file.</param>
        /// <returns>The <see cref="Shape"/> encoded in the given
        /// JSON file, or <c>null</c> if the file could not be
        /// found or decoded.</returns>
        public static Shape ReadFromJson(string path)
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                Shape shape = JsonUtility.FromJson<Shape>(json);
                return shape;
            }
            catch (Exception e)
            {
                Log.Error("Unable to read shape from JSON file: " + e);
            }
            return null;
        }
    }

} // namespace AS2.ShapeContainment
