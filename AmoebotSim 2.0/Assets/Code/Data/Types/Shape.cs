using System;
using System.Collections;
using System.Collections.Generic;
using AS2.UI;
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

        // Convex hull parameters f, d, b, a, c, e
        private int[] convexHullParams = new int[6];
        // We keep all of the possible inequality values ready to avoid computing them too often
        // Also as binary strings
        // First index: rotation
        // Second index: Inequality left side
        // a + b
        // a + c
        // b + d
        // a + b + c
        // a + b + c
        private int[][] convexHullInequalities = new int[][] { new int[5], new int[5], new int[5], new int[5], new int[5], new int[5] };
        private string[][] convexHullInequalityStrings = new string[][] { new string[5], new string[5], new string[5], new string[5], new string[5], new string[5] };
        // Distances of the origin from the convex hull sides
        // Order is f, d, b, a, c, e
        private int[] convexHullDistances = new int[6];
        private string[] convexHullDistanceStrings = new string[6];

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
                        Log.Warning("Shape has duplicate edge: (" + nodes[e1.u].x + ", " + nodes[e1.u].y + ") -- (" + nodes[e1.v].x + ", " + nodes[e1.v].y + ")");
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
                    Log.Warning("Shape has invalid edge: (" + nodes[e.u].x + ", " + nodes[e.u].y + ") -/- (" + nodes[e.v].x + ", " + nodes[e.v].y + ")");
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
        /// Generates the convex hull of this shape and stores all of
        /// its parameters. These include the side lengths a, b, c, d
        /// and the sums a + b, a + c, b + d, a + b + c and a + b + d
        /// for all 6 possible rotations, both as integers and as
        /// binary strings.
        /// </summary>
        public void GenerateConvexHull()
        {
            // Find the minimum and maximum coordinates
            Node node = nodes[0];
            int minx = node.x;
            int maxx = node.x;
            int miny = node.y;
            int maxy = node.y;
            int minxy = node.x + node.y;
            int maxxy = node.x + node.y;

            foreach (Node n in nodes)
            {
                if (n.x < minx)
                    minx = n.x;
                if (n.x > maxx)
                    maxx = n.x;
                if (n.y < miny)
                    miny = n.y;
                if (n.y > maxy)
                    maxy = n.y;
                if (n.x + n.y < minxy)
                    minxy = n.x + n.y;
                if (n.x + n.y > maxxy)
                    maxxy = n.x + n.y;
            }

            // Compute intersection points of the half-planes
            //Vector2Int bottomLeft = new Vector2Int(minxy - miny, miny);
            //Vector2Int bottomRight = new Vector2Int(maxx, miny);
            //Vector2Int middleLeft = new Vector2Int(minx, minxy - minx);
            //Vector2Int middleRight = new Vector2Int(maxx, maxxy - maxx);
            //Vector2Int topLeft = new Vector2Int(minx, maxy);
            //Vector2Int topRight = new Vector2Int(maxxy - maxy, maxy);
            //int a = bottomRight.x - bottomLeft.x;
            //int b = middleLeft.y - bottomLeft.y;
            //int c = middleRight.y - bottomRight.y;
            //int d = topLeft.y - middleLeft.y;
            //int e = topRight.y - middleRight.y;
            //int f = topRight.x - topLeft.x;

            // Compute convex shape parameters
            int a = maxx - (minxy - miny);
            int b = minxy - minx - miny;
            int c = maxxy - maxx - miny;
            int d = maxy - (minxy - minx);
            int e = maxy - (maxxy - maxx);
            int f = maxxy - maxy - minx;

            convexHullParams[0] = f;
            convexHullParams[1] = d;
            convexHullParams[2] = b;
            convexHullParams[3] = a;
            convexHullParams[4] = c;
            convexHullParams[5] = e;

            // Inequalities
            for (int r = 0; r < 6; r++)
            {
                convexHullInequalities[r][0] = convexHullParams[(3 + 6 - r) % 6] + convexHullParams[(2 + 6 - r) % 6];   // a + b
                convexHullInequalities[r][1] = convexHullParams[(3 + 6 - r) % 6] + convexHullParams[(4 + 6 - r) % 6];   // a + c
                convexHullInequalities[r][2] = convexHullParams[(2 + 6 - r) % 6] + convexHullParams[(1 + 6 - r) % 6];   // b + d
                convexHullInequalities[r][3] = convexHullInequalities[r][0] + convexHullParams[(4 + 6 - r) % 6];        // a + b + c
                convexHullInequalities[r][4] = convexHullInequalities[r][0] + convexHullParams[(1 + 6 - r) % 6];        // a + b + d
            }

            for (int r = 0; r < 6; r++)
            {
                for (int i = 0; i < 5; i++)
                {
                    convexHullInequalityStrings[r][i] = IntToBinary(convexHullInequalities[r][i]);
                }
            }

            // Distances between origin and bounding half-planes
            int distA = -miny;
            int distB = -minxy;
            int distC = maxx;
            int distD = -minx;
            int distE = maxxy;
            int distF = maxy;
            convexHullDistances[0] = distF;
            convexHullDistances[1] = distD;
            convexHullDistances[2] = distB;
            convexHullDistances[3] = distA;
            convexHullDistances[4] = distC;
            convexHullDistances[5] = distE;
            for (int i = 0; i < 6; i++)
            {
                convexHullDistanceStrings[i] = IntToBinary(convexHullDistances[i]);
            }
        }

        /// <summary>
        /// Provides the left side of the desired
        /// inequality for the given rotation.
        /// <para>
        /// The inequalities are:
        /// <list type="number">
        /// <item>a + b</item>
        /// <item>a + c</item>
        /// <item>b + d</item>
        /// <item>a + b + c</item>
        /// <item>a + b + d</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="rotation">The rotation of the shape to consider.</param>
        /// <param name="inequality">The index of the inequality.</param>
        /// <returns>The left side's value of the desired inequality.</returns>
        public int GetConvHullInequality(int rotation, int inequality)
        {
            return convexHullInequalities[rotation][inequality];
        }

        /// <summary>
        /// Same as <see cref="GetConvHullInequality(int, int)"/> but returns
        /// the value as a binary string.
        /// </summary>
        /// <param name="rotation">The rotation of the shape to consider.</param>
        /// <param name="inequality">The index of the inequality.</param>
        /// <returns>A binary string representation of the desired inequality.</returns>
        public string GetConvHullInequalityString(int rotation, int inequality)
        {
            return convexHullInequalityStrings[rotation][inequality];
        }

        /// <summary>
        /// Provides the shape origin's distances to its sides.
        /// The distances are ordered f, d, b, a, c, e.
        /// </summary>
        /// <param name="rotation">The rotation of the shape.</param>
        /// <param name="side">The index of the desired side.</param>
        /// <returns>The distance between the shape's origin and the specified side.</returns>
        public int GetConvexHullDistance(int rotation, int side)
        {
            return convexHullDistances[(side + 6 - rotation) % 6];
        }

        /// <summary>
        /// Same as <see cref="GetConvexHullDistance(int, int)"/> but returns
        /// the value as a binary string.
        /// </summary>
        /// <param name="rotation">The rotation of the shape.</param>
        /// <param name="side">The index of the desired side.</param>
        /// <returns>The distance between the shape's origin and the specified side
        /// as a binary string.</returns>
        public string GetConvexHullDistanceString(int rotation, int side)
        {
            return convexHullDistanceStrings[(side + 6 - rotation) % 6];
        }

        /// <summary>
        /// Draws the shape using the <see cref="LineDrawer"/> utility.
        /// Does not clear the line drawer or set a timer.
        /// </summary>
        /// <param name="pos">The origin position of the shape.</param>
        /// <param name="rotation">The number of 60 degree counter-clockwise
        /// rotations around the shape's origin.</param>
        /// <param name="scale">The shape's scale factor.</param>
        public void Draw(Vector2Int pos, int rotation = 0, int scale = 1)
        {
            if (scale < 1)
                scale = 1;

            LineDrawer ld = LineDrawer.Instance;
            // Draw each edge
            foreach (Edge e in edges)
            {
                Vector2Int n1 = nodes[e.u];
                Vector2Int n2 = nodes[e.v];
                if (rotation != 0)
                {
                    n1 = AmoebotFunctions.RotateVector(n1, rotation);
                    n2 = AmoebotFunctions.RotateVector(n2, rotation);
                }

                n1 *= scale;
                n2 *= scale;

                ld.AddLine(n1 + pos, n2 + pos, Color.blue, false, 1, 1, -0.1f);
            }
            // "Fill" each face with some crossing lines
            Color faceColor = new Color(0, 0.8f, 0);
            foreach (Face f in faces)
            {
                Vector2Int n1 = nodes[f.u];
                Vector2Int n2 = nodes[f.v];
                Vector2Int n3 = nodes[f.w];
                if (rotation != 0)
                {
                    n1 = AmoebotFunctions.RotateVector(n1, rotation);
                    n2 = AmoebotFunctions.RotateVector(n2, rotation);
                    n3 = AmoebotFunctions.RotateVector(n3, rotation);
                }
                n1 *= scale;
                n2 *= scale;
                n3 *= scale;
                Vector2 mid12 = n1 + n2;
                Vector2 mid13 = n1 + n3;
                Vector2 mid23 = n2 + n3;
                mid12 /= 2f;
                mid13 /= 2f;
                mid23 /= 2f;
                ld.AddLine(n1 + pos, mid23 + pos, faceColor);
                ld.AddLine(n2 + pos, mid13 + pos, faceColor);
                ld.AddLine(n3 + pos, mid12 + pos, faceColor);
            }

            // Also draw the origin
            foreach (Node n in nodes)
            {
                if (n.x == 0 && n.y == 0)
                {
                    ld.AddLine(n + pos, n + pos, Color.yellow, false, 1.55f, 1, -0.2f);
                    break;
                }
            }
        }

        /// <summary>
        /// Draws the shape's convex hull using the <see cref="LineDrawer"/>
        /// utility. Does not clear the line drawer or set a timer. Lines
        /// are drawn on top of the base shape. Make sure to call
        /// <see cref="GenerateConvexHull"/> before calling this.
        /// </summary>
        /// <param name="pos">The origin position of the shape.</param>
        /// <param name="rotation">The number of 60 degree counter-clockwise
        /// rotations around the shape's origin.</param>
        /// <param name="scale">The shape's scale factor.</param>
        public void DrawConvexHull(Vector2Int pos, int rotation = 0, int scale = 1)
        {
            if (scale < 1)
                scale = 1;

            LineDrawer ld = LineDrawer.Instance;

            // f, d, b, a, c, e
            Vector2Int bottomLeft = new Vector2Int(convexHullDistances[3] - convexHullDistances[2], -convexHullDistances[3]);
            Vector2Int[] corners = new Vector2Int[6];
            corners[0] = bottomLeft;
            for (int i = 1; i < 6; i++)
            {
                corners[i] = corners[i - 1] + AmoebotFunctions.unitVectors[i - 1] * convexHullParams[(i + 2) % 6];
            }
            for (int i = 0; i < 6; i++)
            {
                corners[i] = AmoebotFunctions.RotateVector(corners[i], rotation) * scale + pos;
            }
            for (int i = 0; i < 6; i++)
            {
                ld.AddLine(corners[i], corners[(i + 1) % 6], Color.cyan, false, 1.25f, 1, -0.15f);
            }
        }

        /// <summary>
        /// Draws the shape's traversal using the <see cref="LineDrawer"/> utility.
        /// Does not clear the line drawer or set a timer.
        /// </summary>
        /// <param name="pos">The origin position of the shape.</param>
        /// <param name="rotation">The number of 60 degree counter-clockwise
        /// rotations around the shape's origin.</param>
        /// <param name="scale">The shape's scale factor.</param>
        public void DrawTraversal(Vector2Int pos, int rotation = 0, int scale = 1)
        {
            if (traversal is null)
                return;

            LineDrawer ld = LineDrawer.Instance;
            int n = traversal.Count;
            for (int i = 0; i < n; i++)
            {
                Edge e = edges[traversal[i]];
                Vector2Int n1 = nodes[e.u];
                Vector2Int n2 = nodes[e.v];
                if (rotation != 0)
                {
                    n1 = AmoebotFunctions.RotateVector(n1, rotation);
                    n2 = AmoebotFunctions.RotateVector(n2, rotation);
                }
                Vector2 to = n2 - n1;
                n1 *= scale;
                to *= (scale - 0.2f);

                float frac = (float)i / n;
                Color color = new Color(1.0f - frac, 1.0f - frac, frac);

                ld.AddLine(n1 + pos, n1 + to + pos, color, true, 1, 1, -0.1f * (1.0f - frac));
            }
        }

        /// <summary>
        /// Checks whether this edge has any incident faces and returns
        /// their respective third corner node.
        /// </summary>
        /// <param name="edge">The index of the edge to consider.</param>
        /// <param name="cornerLeft">The index of the "left" face's corner node, or <c>-1</c>.</param>
        /// <param name="cornerRight">The index of the "right" face's corner node, or <c>-1</c>.</param>
        /// <returns><c>true</c> if and only if the given edge has an incident face.</returns>
        public bool GetEdgeFaceCorners(int edge, out int cornerLeft, out int cornerRight)
        {
            Edge e = edges[edge];
            cornerLeft = -1;
            cornerRight = -1;

            foreach (Face f in faces)
            {
                List<int> faceNodes = new List<int>() { f.u, f.v, f.w };
                if (faceNodes.Contains(e.u) && faceNodes.Contains(e.v))
                {
                    // This face contains this edge!
                    // Find the third node
                    faceNodes.Remove(e.u);
                    faceNodes.Remove(e.v);
                    int w = faceNodes[0];
                    // Find out whether it is the "left" or the "right" corner
                    Vector2Int edgeVec = (Vector2Int)nodes[e.v] - nodes[e.u];
                    Vector2Int splitVec = (Vector2Int)nodes[w] - nodes[e.u];
                    if (AmoebotFunctions.RotateVector(edgeVec, 1) == splitVec)
                    {
                        cornerLeft = w;
                    }
                    else
                    {
                        cornerRight = w;
                    }
                }
            }

            return cornerLeft != -1 || cornerRight != -1;
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

        private string IntToBinary(int num)
        {
            if (num == 0)
                return "0";

            string s = "";
            while (num > 0)
            {
                s += (num & 1) > 0 ? '1' : '0';
                num >>= 1;
            }

            return s;
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

        /// <summary>
        /// Generates a triangle, parallelogram, trapezoid or pentagon shape
        /// with the given side lengths. <paramref name="a"/> is the base side
        /// length, <paramref name="d"/> is the height and <paramref name="c"/>
        /// is the length of the part that is cut off from the pentagon.
        /// Set <paramref name="d"/> = <paramref name="a"/> and
        /// <paramref name="c"/> = 0 to get a triangle.
        /// Set <paramref name="c"/> = 0 to get a trapezoid and
        /// <paramref name="c"/> = <paramref name="d"/> to get
        /// a parallelogram.
        /// <para>
        /// Also generates a traversal path for the shape.
        /// </para>
        /// </summary>
        /// <param name="a">The base side length of the shape.</param>
        /// <param name="d">The height of the shape. Must not be larger
        /// than <paramref name="a"/>.</param>
        /// <param name="c">The side length of the triangle that is
        /// cut off on the right side. Must not be larger than
        /// <paramref name="d"/> and less than <paramref name="a"/>.</param>
        /// <returns>A convex shape with a traversal path.</returns>
        public static Shape GenSimpleConvexShape(int a, int d, int c)
        {
            Shape s = new Shape();

            if (a < 1 || d < 0 || c < 0 || a + c - d < 0)
            {
                Log.Error("Invalid shape parameters");
                return null;
            }
            
            // For simplicity with indices
            a += 1;
            d += 1;
            c += 1;

            int f = a + c - d - 1;
            int idx = 0;
            int lastLineLength = -1;
            for (int x = 0; x < a; x++)
            {
                int len = x < f ? d : (d - (x - f));
                for (int y = 0; y < len; y++)
                {
                    // Add node
                    s.nodes.Add(new Node(x, y));

                    int idxBelow = -1;
                    int idxLeft = -1;
                    int idxAbove = -1;

                    // Add edges
                    // Down
                    if (y > 0)
                    {
                        idxBelow = idx - 1;
                        s.edges.Add(new Edge(idxBelow, idx));
                    }
                    // Left
                    if (x > 0)
                    {
                        idxLeft = idx - lastLineLength;
                        // Same level
                        s.edges.Add(new Edge(idxLeft, idx));
                        // Up
                        if (y < lastLineLength - 1)
                        {
                            idxAbove = idxLeft + 1;
                            s.edges.Add(new Edge(idxAbove, idx));
                        }
                    }

                    // Add faces
                    if (idxBelow != -1 && idxLeft != -1)
                        s.faces.Add(new Face(idxBelow, idx, idxLeft));
                    if (idxLeft != -1 && idxAbove != -1)
                        s.faces.Add(new Face(idxLeft, idx, idxAbove));

                    idx++;
                }
                lastLineLength = len;
            }

            if (s.IsConsistent())
                s.GenerateTraversal();
            else
            {
                Log.Error("Shape generation failed: Shape is inconsistent");
                return null;
            }

            return s;
        }

        /// <summary>
        /// Generates a hexagon shape from the given side length parameters.
        /// Also generates a traversal path for the shape.
        /// </summary>
        /// <param name="a">The base side length of the shape.</param>
        /// <param name="d">The "top left" side length.</param>
        /// <param name="c">The "bottom right" side length.</param>
        /// <param name="b">The "bottom left" side length.</param>
        /// <returns>A hexagon shape with a traversal path.</returns>
        public static Shape GenHexagon(int a, int d, int c, int b)
        {
            Shape s = new Shape();

            if (a < 0 || d < 0 || c < 0 || b < 0)
            {
                Log.Error("Invalid shape parameters");
                return null;
            }

            int e = b + d - c;
            int f = a + c - d;
            int middleLength = e <= d ? a + b : d + f;

            // For simplicity with indices
            middleLength += 1;
            int[] middleIndices = new int[middleLength];
            b += 1;
            d += 1;

            int idx = 0;
            int lastLineLengthTop = -1;
            int lastLineLengthBot = -1;
            for (int x = 0; x < middleLength; x++)
            {
                // Top side first
                int lenTop = x < f ? d : (d - (x - f));
                for (int y = 0; y < lenTop; y++)
                {
                    // Add node
                    s.nodes.Add(new Node(x, y));
                    if (y == 0)
                        middleIndices[x] = idx;

                    int idxBelow = -1;
                    int idxLeft = -1;
                    int idxAbove = -1;

                    // Add edges
                    // Down
                    if (y > 0)
                    {
                        idxBelow = idx - 1;
                        s.edges.Add(new Edge(idxBelow, idx));
                    }
                    // Left
                    if (x > 0)
                    {
                        idxLeft = y == 0 ? middleIndices[x - 1] : idx - (lastLineLengthTop + lastLineLengthBot) + 1;
                        // Same level
                        s.edges.Add(new Edge(idxLeft, idx));
                        // Up
                        if (y < lastLineLengthTop - 1)
                        {
                            idxAbove = idxLeft + 1;
                            s.edges.Add(new Edge(idxAbove, idx));
                        }
                    }

                    // Add faces
                    if (idxBelow != -1 && idxLeft != -1)
                        s.faces.Add(new Face(idxBelow, idx, idxLeft));
                    if (idxLeft != -1 && idxAbove != -1)
                        s.faces.Add(new Face(idxLeft, idx, idxAbove));

                    idx++;
                }

                // Now bottom side (everything is mirrored)
                int lenBot = x < a ? b : (b - (x - a));
                for (int z = 0; z > -lenBot; z--)
                {
                    if (z != 0)
                    {
                        // Add node
                        s.nodes.Add(new Node(x - z, z));

                        int idxBelow = -1;
                        int idxLeft = -1;
                        int idxAbove = -1;

                        // Add edges
                        // Down
                        idxBelow = z == -1 ? middleIndices[x] : (idx - 1);
                        s.edges.Add(new Edge(idxBelow, idx));
                        // Left
                        if (x > 0)
                        {
                            idxLeft = idx - (lastLineLengthBot + lenTop) + 1;
                            // Same level
                            s.edges.Add(new Edge(idxLeft, idx));
                            // Up
                            if (z > -lastLineLengthBot + 1)
                            {
                                idxAbove = idxLeft + 1;
                                s.edges.Add(new Edge(idxAbove, idx));
                            }
                        }

                        // Add faces
                        if (idxBelow != -1 && idxLeft != -1)
                            s.faces.Add(new Face(idxBelow, idx, idxLeft));
                        if (idxLeft != -1 && idxAbove != -1)
                            s.faces.Add(new Face(idxLeft, idx, idxAbove));

                        idx++;
                    }
                    else if (x > 0)
                    {
                        // Special case: First layer below the middle line
                        int myIdx = middleIndices[x];
                        // Add one edge and one face
                        int prev = middleIndices[x - 1];
                        int nbrBot = prev + lastLineLengthTop;
                        s.edges.Add(new Edge(myIdx, nbrBot));
                        s.faces.Add(new Face(prev, nbrBot, myIdx));
                    }
                }
                lastLineLengthTop = lenTop;
                lastLineLengthBot = lenBot;
            }

            if (s.IsConsistent())
                s.GenerateTraversal();
            else
            {
                Log.Error("Shape generation failed: Shape is inconsistent");
                return null;
            }

            return s;
        }
    }

} // namespace AS2.ShapeContainment
